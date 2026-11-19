using JumpRopeApp.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Storage;
using System;
using System.Timers;
using JumpRopeApp.Services;


#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace JumpRopeApp;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _dbService = new();
    private readonly TimerRingDrawable _ringDrawable = new();
    private readonly MotionDetectorService _motionDetector = new();

    private IDispatcherTimer? _timer;
    private bool _isRunning = false;
    private bool _isResting = false;
    private readonly CloudService _cloudService = new();

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool canStartHealthService = true;

#if ANDROID
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // 1. Request Android runtime permissions safely while UI is active
            try
            {
                var sensorStatus = await Permissions.CheckStatusAsync<Permissions.Sensors>();
                if (sensorStatus != PermissionStatus.Granted)
                {
                    await Permissions.RequestAsync<Permissions.Sensors>();
                }

                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
                {
                    var activityStatus = await Permissions.CheckStatusAsync<ActivityRecognitionPermission>();
                    if (activityStatus != PermissionStatus.Granted)
                    {
                        activityStatus = await Permissions.RequestAsync<ActivityRecognitionPermission>();
                    }

                    canStartHealthService = activityStatus == PermissionStatus.Granted;
                    if (!canStartHealthService)
                    {
                        Logger.Log("ActivityRecognition permission not granted; skipping StepForegroundService start.");
                    }
                }

                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
                {
                    var notifStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                    if (notifStatus != PermissionStatus.Granted)
                    {
                        await Permissions.RequestAsync<Permissions.PostNotifications>();
                    }
                }
            }
            catch (Exception ex)
            {
                canStartHealthService = false;
                Logger.Log("Permission error in OnAppearing", ex);
            }

            // 2. Safely start the Foreground Service using platform context
            if (canStartHealthService)
            {
                try
                {
                    var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? Android.App.Application.Context;
                    var intent = new Android.Content.Intent(context, typeof(Platforms.Android.StepForegroundService));

                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                    {
                        context.StartForegroundService(intent);
                    }
                    else
                    {
                        context.StartService(intent);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Failed to start StepForegroundService", ex);
                }
            }
        }
#endif

        UpdateTimerDisplay();

        // 1. Display baseline daily steps immediately (Updated label reference)
        DailyStepsTextLabel.Text = App.DailyStepCount.ToString("N0");

        // 2. Subscribe to continuous background step updates
        App.OnDailyStepsUpdated += HandleStepsUpdated;

        // 3. Subscribe to real-time motion jump events
        _motionDetector.OnJumpDetected += HandleJumpDetected;

        // 1. Load initial daily & accumulative steps
        int displayDailySteps = Preferences.Get("SavedDailySteps", 0);
        DailyStepsTextLabel.Text = displayDailySteps.ToString("N0");
        int accumSteps = Preferences.Default.Get("SavedAccumulativeSteps", 0);
        UpdateStepDisplay(displayDailySteps, accumSteps);

        // 2. Subscribe to background step service updates
        App.OnDailyStepsUpdated += OnStepsUpdatedFromService;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        App.OnDailyStepsUpdated -= HandleStepsUpdated;
        _motionDetector.OnJumpDetected -= HandleJumpDetected;
        _motionDetector.StopTracking();
    }

    private void OnStepsUpdatedFromService(int daily, int accumulative)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateStepDisplay(daily, accumulative);
        });
    }

    private void UpdateStepDisplay(int dailySteps, int accumulativeSteps)
    {
        DailyStepsTextLabel.Text = dailySteps.ToString("N0");
        AccumulativeStepsLabel.Text = accumulativeSteps.ToString("N0");

        // Calculate estimated distance (~0.00076 km per step)
        double distanceKm = Math.Round(dailySteps * 0.00076, 2);
        DailyDistanceLabel.Text = $"{distanceKm:N2} km";

        // Progress toward 10,000 daily step goal
        double progress = Math.Min(1.0, (double)dailySteps / 10000.0);
        StepProgressBar.Progress = progress;
        DailyGoalProgressLabel.Text = $"{Math.Round(progress * 100)}%";
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
            StopWorkout();
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
                if (!_isResting && _restDuration > 0)
                {
                    _isResting = true;
                    _remainingSeconds = _restDuration;
                    StatusBadge.BackgroundColor = Color.FromArgb("#F59E0B");
                    StatusTextLabel.Text = "RESTING";
                }
                else
                {
                    if (_currentSet < _totalSets)
                    {
                        _currentSet++;
                        _isResting = false;
                        _remainingSeconds = _jumpDuration;
                        StatusBadge.BackgroundColor = Color.FromArgb("#10B981");
                        StatusTextLabel.Text = "JUMPING";
                    }
                    else
                    {
                        CompleteWorkout();
                    }
                }
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
            TotalJumps = _totalJumpsInWorkout,
            DurationSeconds = _totalDurationSeconds,
            CaloriesBurned = _totalCaloriesBurned,
            DailySteps = Preferences.Default.Get("SavedDailySteps", 0),
            SetsCompleted = _totalSets
        };

        await _dbService.SaveWorkoutAsync(record);

        // Show Summary Modal
        SummaryJumpsLabel.Text = _totalJumpsInWorkout.ToString("N0");
        SummarySetsLabel.Text = _totalSets.ToString();
        SummaryDurationLabel.Text = $"{_totalDurationSeconds / 60}m {_totalDurationSeconds % 60}s";
        SummaryCaloriesLabel.Text = $"{_totalCaloriesBurned} kcal";
        SummaryModalOverlay.IsVisible = true;
    }

    public void OnCloseSummaryClicked(object? sender, EventArgs e)
    {
        SummaryModalOverlay.IsVisible = false;
        OnResetClicked(null, EventArgs.Empty);
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        StopWorkout();
        _remainingSeconds = 30;
        _currentSet = 1;
        _totalJumpsInWorkout = 0;
        LiveJumpsLabel.Text = "0";
        LiveCaloriesLabel.Text = "0 kcal";
        UpdateTimerDisplay();
    }

    private void OnPresetBeginnerClicked(object sender, EventArgs e)
    {
        JumpSecsEntry.Text = "30"; RestSecsEntry.Text = "15"; TotalSetsEntry.Text = "6"; RpmEntry.Text = "100";
    }

    private void OnPresetHiitClicked(object sender, EventArgs e)
    {
        JumpSecsEntry.Text = "45"; RestSecsEntry.Text = "15"; TotalSetsEntry.Text = "8"; RpmEntry.Text = "130";
    }

    private void OnPresetEnduranceClicked(object sender, EventArgs e)
    {
        JumpSecsEntry.Text = "60"; RestSecsEntry.Text = "30"; TotalSetsEntry.Text = "10"; RpmEntry.Text = "110";
    }
    public async Task SaveCompletedWorkoutAsync(WorkoutRecord record)
    {
        // 1. Save locally first (Offline-first architecture)
        await _dbService.SaveWorkoutAsync(record);

        // 2. Push to MongoDB Atlas Node API in the background
        _ = Task.Run(async () =>
        {
            bool synced = await _cloudService.SyncWorkoutAsync(record);
            if (synced)
            {
                System.Diagnostics.Debug.WriteLine("Synced workout to MongoDB Atlas successfully!");
            }
        });
    }

    public class ActivityRecognitionPermission : Permissions.BasePlatformPermission
    {
#if ANDROID
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            new[] { (global::Android.Manifest.Permission.ActivityRecognition, true) };
#endif
    }
}