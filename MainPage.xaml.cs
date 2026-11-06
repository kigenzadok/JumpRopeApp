using JumpRopeApp.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Storage;
using System;
using System.Timers;

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace JumpRopeApp;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _dbService = new();
    private readonly Services.MotionDetectorService _motionDetector = new();

    private System.Timers.Timer? _workoutTimer;
    private int _remainingSeconds = 30;
    private int _totalDurationSeconds = 0;
    private int _totalJumpsInWorkout = 0;
    private double _totalCaloriesBurned = 0;
    private int _currentSet = 1;
    private int _totalSets = 8;
    private int _jumpDuration = 30;
    private int _restDuration = 15;
    private bool _isWorkingOut = false;
    private bool _isResting = false;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Request Android runtime permissions safely while UI is active
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            try
            {
                var sensorStatus = await Permissions.CheckStatusAsync<Permissions.Sensors>();
                if (sensorStatus != PermissionStatus.Granted)
                {
                    await Permissions.RequestAsync<Permissions.Sensors>();
                }

#if ANDROID
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
#endif
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
                System.Diagnostics.Debug.WriteLine($"Permission error: {ex.Message}");
            }
        }

        // 2. Safely start the Foreground Service using platform context
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            try
            {
#if ANDROID
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
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start service: {ex.Message}");
            }
        }

        UpdateTimerDisplay();
        _motionDetector.OnJumpDetected += HandleJumpDetected;

        // 3. Load initial daily steps
        int dailySteps = Preferences.Default.Get("SavedDailySteps", 0);
        UpdateStepDisplay(dailySteps);

        // 4. Subscribe to background step service updates
        App.OnDailyStepsUpdated += OnStepsUpdatedFromService;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _motionDetector.OnJumpDetected -= HandleJumpDetected;
        _motionDetector.StopTracking();
        App.OnDailyStepsUpdated -= OnStepsUpdatedFromService;
    }

    private void OnStepsUpdatedFromService(int newSteps)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateStepDisplay(newSteps);
        });
    }

    private void UpdateStepDisplay(int dailySteps)
    {
        DailyStepsTextLabel.Text = dailySteps.ToString("N0");

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
        if (_isWorkingOut && !_isResting)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _totalJumpsInWorkout++;
                LiveJumpsLabel.Text = _totalJumpsInWorkout.ToString("N0");

                // Rough calorie estimate (~0.1 kcal per jump)
                _totalCaloriesBurned = Math.Round(_totalJumpsInWorkout * 0.1, 1);
                LiveCaloriesLabel.Text = $"{_totalCaloriesBurned} kcal";
            });
        }
    }

    private void OnStartStopClicked(object sender, EventArgs e)
    {
        if (!_isWorkingOut)
        {
            // Start Workout
            if (int.TryParse(JumpSecsEntry.Text, out int j) && j > 0) _jumpDuration = j;
            if (int.TryParse(RestSecsEntry.Text, out int r)) _restDuration = r;
            if (int.TryParse(TotalSetsEntry.Text, out int s) && s > 0) _totalSets = s;

            _remainingSeconds = _jumpDuration;
            _currentSet = 1;
            _totalJumpsInWorkout = 0;
            _totalDurationSeconds = 0;
            _totalCaloriesBurned = 0;

            _isWorkingOut = true;
            _isResting = false;

            StatusBadge.BackgroundColor = Color.FromArgb("#10B981");
            StatusTextLabel.Text = "JUMPING";
            StartStopButton.Text = "Pause Workout";

            _motionDetector.StartTracking();
            StartTimer();
        }
        else
        {
            StopWorkout();
        }
    }

    private void StartTimer()
    {
        _workoutTimer?.Stop();
        _workoutTimer = new System.Timers.Timer(1000);
        _workoutTimer.Elapsed += OnTimerElapsed;
        _workoutTimer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                if (_isWorkingOut && !_isResting) _totalDurationSeconds++;
                UpdateTimerDisplay();
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
        });
    }

    private void UpdateTimerDisplay()
    {
        int mins = _remainingSeconds / 60;
        int secs = _remainingSeconds % 60;
        TimerTextLabel.Text = $"{mins:D2}:{secs:D2}";
        CurrentSetTextLabel.Text = $"Set {_currentSet} of {_totalSets}";
    }

    private void StopWorkout()
    {
        _workoutTimer?.Stop();
        _isWorkingOut = false;
        _motionDetector.StopTracking();

        StatusBadge.BackgroundColor = Color.FromArgb("#6366F1");
        StatusTextLabel.Text = "READY";
        StartStopButton.Text = "Start Workout";
    }

    private async void CompleteWorkout()
    {
        StopWorkout();

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

    private void OnCloseSummaryClicked(object sender, EventArgs e)
    {
        SummaryModalOverlay.IsVisible = false;
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
}