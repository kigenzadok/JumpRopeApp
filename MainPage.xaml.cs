using JumpRopeApp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using Plugin.Maui.Audio;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class MainPage : ContentPage
{
    private System.Timers.Timer? _timer;
    private int _timeRemaining;
    private int _currentSet;
    private int _totalSets;
    private int _jumpTimeSecs;
    private int _restTimeSecs;
    private int _totalJumpsLogged = 0;

    private IAudioPlayer? _beepPlayer;
    private readonly DatabaseService _dbService = new();

    public static readonly BindableProperty PageBackgroundColorProperty =
        BindableProperty.Create(nameof(PageBackgroundColor), typeof(Color), typeof(MainPage), Colors.White);

    public Color PageBackgroundColor
    {
        get => (Color)GetValue(PageBackgroundColorProperty);
        set => SetValue(PageBackgroundColorProperty, value);
    }

    private readonly JumpDetectorService _jumpDetector = new();
    private int _liveJumpCount = 0;
    private bool _isJumpInterval = true; // Set to true when user is in JUMP interval (not REST)
    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;

        // Force default text on load if empty
        if (string.IsNullOrWhiteSpace(JumpSecsLabel.Text)) JumpSecsLabel.Text = "30";
        if (string.IsNullOrWhiteSpace(RestSecsLabel.Text)) RestSecsLabel.Text = "15";
        if (string.IsNullOrWhiteSpace(TargetSetsLabel.Text)) TargetSetsLabel.Text = "5";
        _jumpDetector.JumpDetected += OnJumpDetected;
        LoadAudioSafely();
    }

    private async void LoadAudioSafely()
    {
        try
        {
            if (await FileSystem.AppPackageFileExistsAsync("beep.mp3"))
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("beep.mp3");
                _beepPlayer = AudioManager.Current.CreatePlayer(stream);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Audio loading bypassed: {ex.Message}");
        }
    }

    private void TriggerTransitionAlert()
    {
        try
        {
            if (_beepPlayer != null)
            {
                _beepPlayer.Play();
            }

            if (HapticFeedback.Default.IsSupported)
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Haptic/Audio error ignored: {ex.Message}");
        }
    }

    // --- INCREMENT / DECREMENT BUTTONS ---

    private void OnDecreaseJumpClicked(object sender, EventArgs e)
    {
        if (int.TryParse(JumpSecsLabel.Text, out var val) && val > 5)
            JumpSecsLabel.Text = (val - 5).ToString();
    }

    private void OnIncreaseJumpClicked(object sender, EventArgs e)
    {
        if (int.TryParse(JumpSecsLabel.Text, out var val))
            JumpSecsLabel.Text = (val + 5).ToString();
    }

    private void OnDecreaseRestClicked(object sender, EventArgs e)
    {
        if (int.TryParse(RestSecsLabel.Text, out var val) && val > 0)
            RestSecsLabel.Text = (val - 5).ToString();
    }

    private void OnIncreaseRestClicked(object sender, EventArgs e)
    {
        if (int.TryParse(RestSecsLabel.Text, out var val))
            RestSecsLabel.Text = (val + 5).ToString();
    }

    private void OnDecreaseSetsClicked(object sender, EventArgs e)
    {
        if (int.TryParse(TargetSetsLabel.Text, out var val) && val > 1)
            TargetSetsLabel.Text = (val - 1).ToString();
    }

    private void OnIncreaseSetsClicked(object sender, EventArgs e)
    {
        if (int.TryParse(TargetSetsLabel.Text, out var val))
            TargetSetsLabel.Text = (val + 1).ToString();
    }

    // --- PRESET ROUTINE BUTTONS ---

    private void ApplyPreset(int jumpSecs, int restSecs, int totalSets)
    {
        JumpSecsLabel.Text = jumpSecs.ToString();
        RestSecsLabel.Text = restSecs.ToString();
        TargetSetsLabel.Text = totalSets.ToString();

        TriggerTransitionAlert();
    }

    private void OnPresetBeginnerClicked(object sender, EventArgs e) => ApplyPreset(20, 20, 5);
    private void OnPresetHIITClicked(object sender, EventArgs e) => ApplyPreset(40, 20, 8);
    private void OnPresetTabataClicked(object sender, EventArgs e) => ApplyPreset(20, 10, 8);
    private void OnPresetEnduranceClicked(object sender, EventArgs e) => ApplyPreset(60, 15, 10);

    // --- WORKOUT CONTROL LOGIC ---

    private void OnStartWorkoutClicked(object sender, EventArgs e)
    {
        _jumpTimeSecs = int.TryParse(JumpSecsLabel.Text, out var j) ? j : 30;
        _restTimeSecs = int.TryParse(RestSecsLabel.Text, out var r) ? r : 15;
        _totalSets = int.TryParse(TargetSetsLabel.Text, out var s) ? s : 5;

        _currentSet = 1;
        _totalJumpsLogged = 0;

        ConfigView.IsVisible = false;
        SummaryView.IsVisible = false;
        ActiveView.IsVisible = true;

        StartJumpInterval();
    }

    private void OnJumpDetected(object? sender, EventArgs e)
    {
        // Only count jumps when active in a JUMP set
        if (_isJumpInterval)
        {
            _liveJumpCount++;

            // Update labels in real-time
            TotalJumpsLabel.Text = $"Total Jumps Recorded: {_liveJumpCount}";
        }
    }

    private void StartActiveWorkout()
    {
        ConfigView.IsVisible = false;
        ActiveView.IsVisible = true;
        SummaryView.IsVisible = false;

        _liveJumpCount = 0;
        _jumpDetector.StartTracking(); // Start pocket detection
    }

    // Call this when ending or completing a workout
    private void StopActiveWorkout()
    {
        _jumpDetector.StopTracking(); // Stop sensor listening to preserve battery

        ActiveView.IsVisible = false;
        SummaryView.IsVisible = true;

        CompletedSetsLabel.Text = $"Sets Completed: 5";
        TotalJumpsLabel.Text = $"Total Jumps Recorded: {_liveJumpCount}";
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _jumpDetector.StopTracking(); // Safety cleanup when navigating away
    }
    private void StartJumpInterval()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PageBackgroundColor = Color.FromArgb("#4CAF50"); // Green
            StatusLabel.Text = "JUMP!";
            SetInfoLabel.Text = $"Set {_currentSet} of {_totalSets}";
        });

        TriggerTransitionAlert();
        StartCountDown(_jumpTimeSecs, OnJumpIntervalFinished);
    }

    private void StartRestInterval()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PageBackgroundColor = Color.FromArgb("#FF9800"); // Orange
            StatusLabel.Text = "REST";
        });

        TriggerTransitionAlert();
        StartCountDown(_restTimeSecs, OnRestIntervalFinished);
    }

    private void StartCountDown(int seconds, Action onFinish)
    {
        _timeRemaining = seconds;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TimerLabel.Text = _timeRemaining.ToString();
        });

        _timer?.Stop();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) =>
        {
            _timeRemaining--;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimerLabel.Text = _timeRemaining.ToString();

                if (_timeRemaining <= 0)
                {
                    _timer.Stop();
                    onFinish();
                }
            });
        };
        _timer.Start();
    }

    private void OnJumpIntervalFinished()
    {
        if (_restTimeSecs > 0)
        {
            StartRestInterval();
        }
        else
        {
            OnRestIntervalFinished();
        }
    }

    private void OnRestIntervalFinished()
    {
        if (_currentSet < _totalSets)
        {
            _currentSet++;
            StartJumpInterval();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(FinishWorkout);
        }
    }

    private void OnEndWorkoutClicked(object sender, EventArgs e)
    {
        _timer?.Stop();
        FinishWorkout();
    }

    private void FinishWorkout()
    {
        TriggerTransitionAlert();
        PageBackgroundColor = Colors.White;
        ActiveView.IsVisible = false;
        SummaryView.IsVisible = true;

        CompletedSetsLabel.Text = $"Sets Completed: {_currentSet}";
        TotalJumpsLabel.Text = $"Total Jumps Recorded: {_totalJumpsLogged}";
    }

    // --- SUMMARY VIEW HANDLERS ---

    private void OnAddJumpsClicked(object sender, EventArgs e)
    {
        if (int.TryParse(JumpsLoggedEntry.Text, out var count))
        {
            _totalJumpsLogged += count;
            TotalJumpsLabel.Text = $"Total Jumps Recorded: {_totalJumpsLogged}";
            JumpsLoggedEntry.Text = string.Empty;
        }
    }

    private async void OnSaveWorkoutClicked(object sender, EventArgs e)
    {
        try
        {
            await _dbService.SaveWorkoutAsync(
                sets: _currentSet > 0 ? _currentSet : 1,
                jumps: _totalJumpsLogged,
                jumpSecs: _jumpTimeSecs,
                restSecs: _restTimeSecs
            );

            await DisplayAlert("Saved", "Workout recorded to Calendar & Stats!", "OK");
            ResetToConfigView();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving workout: {ex.Message}");
            await DisplayAlert("Error", "Could not save workout data.", "OK");
        }
    }

    private async void OnExitClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Exit Workout", "Discard this workout without saving?", "Yes", "No");
        if (confirm)
        {
            ResetToConfigView();
        }
    }

    private void ResetToConfigView()
    {
        _timer?.Stop();
        _currentSet = 0;
        _totalJumpsLogged = 0;

        if (JumpsLoggedEntry != null)
            JumpsLoggedEntry.Text = string.Empty;

        PageBackgroundColor = Colors.White;
        SummaryView.IsVisible = false;
        ActiveView.IsVisible = false;
        ConfigView.IsVisible = true;
    }
}