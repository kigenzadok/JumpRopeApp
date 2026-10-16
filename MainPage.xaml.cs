using JumpRopeApp.Models;
using JumpRopeApp.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using System;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _dbService = new();
    private readonly TimerRingDrawable _ringDrawable = new();
    private readonly MotionDetectorService _motionDetector = new();

    private IDispatcherTimer? _timer;
    private bool _isRunning = false;
    private bool _isResting = false;

    private int _jumpSecs = 30;
    private int _restSecs = 15;
    private int _totalSets = 8;
    private int _rpm = 120;

    private int _currentSet = 1;
    private int _secondsRemaining = 30;
    private int _totalWorkoutSeconds = 0;
    private int _jumpCount = 0;

    public MainPage()
    {
        InitializeComponent();
        TimerGraphicsView.Drawable = _ringDrawable;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateTimerDisplay();

        // 1. Display baseline daily steps immediately (Updated label reference)
        DailyStepsTextLabel.Text = App.DailyStepCount.ToString("N0");

        // 2. Subscribe to continuous background step updates
        App.OnDailyStepsUpdated += HandleStepsUpdated;

        // 3. Subscribe to real-time motion jump events
        _motionDetector.OnJumpDetected += HandleJumpDetected;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        App.OnDailyStepsUpdated -= HandleStepsUpdated;
        _motionDetector.OnJumpDetected -= HandleJumpDetected;
        _motionDetector.StopTracking();
    }

    private void HandleStepsUpdated(int steps)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Target DailyStepsTextLabel
            DailyStepsTextLabel.Text = steps.ToString("N0");

            double distanceKm = (steps * 0.762) / 1000.0;
            DailyDistanceLabel.Text = $"{distanceKm:F1} km";

            double progress = Math.Min(1.0, steps / 10000.0);
            StepProgressBar.Progress = progress;
            DailyGoalProgressLabel.Text = $"{(int)(progress * 100)}%";
        });
    }

    private void HandleJumpDetected()
    {
        if (_isRunning && !_isResting)
        {
            _jumpCount++;
            LiveJumpsLabel.Text = _jumpCount.ToString("N0");
        }
    }

    // ==========================================
    // PRESET & TIMER CONTROLS
    // ==========================================

    public void OnPresetBeginnerClicked(object? sender, EventArgs e) => ApplyPreset(20, 10, 6, 100);
    public void OnPresetHiitClicked(object? sender, EventArgs e) => ApplyPreset(45, 15, 10, 130);
    public void OnPresetEnduranceClicked(object? sender, EventArgs e) => ApplyPreset(60, 15, 5, 110);

    private void ApplyPreset(int jump, int rest, int sets, int rpm)
    {
        if (_isRunning) return;

        JumpSecsEntry.Text = jump.ToString();
        RestSecsEntry.Text = rest.ToString();
        TotalSetsEntry.Text = sets.ToString();
        RpmEntry.Text = rpm.ToString();

        OnResetClicked(null, EventArgs.Empty);
    }

    public void OnStartStopClicked(object? sender, EventArgs e)
    {
        if (_isRunning)
        {
            StopTimer();
        }
        else
        {
            ParseInputs();
            StartTimer();
        }
    }

    private void ParseInputs()
    {
        _jumpSecs = int.TryParse(JumpSecsEntry.Text, out var j) && j > 0 ? j : 30;
        _restSecs = int.TryParse(RestSecsEntry.Text, out var r) && r >= 0 ? r : 15;
        _totalSets = int.TryParse(TotalSetsEntry.Text, out var s) && s > 0 ? s : 8;
        _rpm = int.TryParse(RpmEntry.Text, out var rpm) && rpm > 0 ? rpm : 120;
    }

    private void StartTimer()
    {
        _isRunning = true;
        StartStopButton.Text = "Pause";
        StartStopButton.BackgroundColor = Color.FromArgb("#EF4444");

        if (_timer == null)
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        _timer.Start();
        _motionDetector.StartTracking();
        SpeakText("Workout Started! Jump!");
    }

    private void StopTimer()
    {
        _isRunning = false;
        _timer?.Stop();
        _motionDetector.StopTracking();

        StartStopButton.Text = "Resume";
        StartStopButton.BackgroundColor = Color.FromArgb("#6366F1");
    }

    public void OnResetClicked(object? sender, EventArgs e)
    {
        StopTimer();
        ParseInputs();

        _isRunning = false;
        _isResting = false;
        _currentSet = 1;
        _secondsRemaining = _jumpSecs;
        _totalWorkoutSeconds = 0;
        _jumpCount = 0;

        StartStopButton.Text = "Start Workout";
        StartStopButton.BackgroundColor = Color.FromArgb("#6366F1");

        LiveJumpsLabel.Text = "0";
        UpdateTimerDisplay();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _secondsRemaining--;
        _totalWorkoutSeconds++;

        if (_secondsRemaining is > 0 and <= 3)
        {
            SpeakText(_secondsRemaining.ToString());
            TriggerHaptic();
        }

        if (_secondsRemaining <= 0)
        {
            if (!_isResting)
            {
                if (_currentSet >= _totalSets)
                {
                    FinishWorkout();
                    return;
                }

                _isResting = true;
                _secondsRemaining = _restSecs;
                SpeakText("Rest!");
                TriggerHaptic();
            }
            else
            {
                _isResting = false;
                _currentSet++;
                _secondsRemaining = _jumpSecs;
                SpeakText($"Set {_currentSet}! Jump!");
                TriggerHaptic();
            }
        }

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        int totalMaxSeconds = _isResting ? _restSecs : _jumpSecs;
        double progress = totalMaxSeconds > 0 ? (double)_secondsRemaining / totalMaxSeconds : 0;

        _ringDrawable.Progress = progress;
        _ringDrawable.IsRestMode = _isResting;
        TimerGraphicsView.Invalidate();

        int mins = _secondsRemaining / 60;
        int secs = _secondsRemaining % 60;
        TimerTextLabel.Text = $"{mins:D2}:{secs:D2}";
        CurrentSetTextLabel.Text = $"Set {_currentSet} of {_totalSets}";

        if (_isResting)
        {
            StatusBadge.BackgroundColor = Color.FromArgb("#F59E0B");
            StatusTextLabel.Text = "REST";
        }
        else
        {
            StatusBadge.BackgroundColor = Color.FromArgb("#6366F1");
            StatusTextLabel.Text = _isRunning ? "JUMP!" : "READY";
        }

        LiveJumpsLabel.Text = _jumpCount.ToString("N0");

        double estCalories = Math.Round((10.0 * 70.0 * (_totalWorkoutSeconds / 3600.0)), 1);
        LiveCaloriesLabel.Text = $"{estCalories:N0} kcal";
    }

    private async void SpeakText(string text)
    {
        try { await TextToSpeech.Default.SpeakAsync(text); } catch { }
    }

    private void TriggerHaptic()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private async void FinishWorkout()
    {
        StopTimer();
        SpeakText("Workout Complete! Great job!");

        int mins = _totalWorkoutSeconds / 60;
        int secs = _totalWorkoutSeconds % 60;

        SummaryJumpsLabel.Text = _jumpCount.ToString("N0");
        SummarySetsLabel.Text = _totalSets.ToString();
        SummaryDurationLabel.Text = $"{mins}m {secs}s";

        double estCalories = Math.Round((10.0 * 70.0 * (_totalWorkoutSeconds / 3600.0)), 1);
        SummaryCaloriesLabel.Text = $"{estCalories:N0} kcal";

        var record = new WorkoutRecord
        {
            Date = DateTime.Now,
            WorkoutType = "Interval Jump Rope",
            SetsCompleted = _totalSets,
            TotalJumps = _jumpCount,
            DurationSeconds = _totalWorkoutSeconds,
            CaloriesBurned = estCalories,
            TotalSteps = App.DailyStepCount
        };

        await _dbService.SaveWorkoutAsync(record);
        SummaryModalOverlay.IsVisible = true;
    }

    public void OnCloseSummaryClicked(object? sender, EventArgs e)
    {
        SummaryModalOverlay.IsVisible = false;
        OnResetClicked(null, EventArgs.Empty);
    }

}