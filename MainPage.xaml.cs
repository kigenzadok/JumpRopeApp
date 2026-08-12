using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices; // Provides HapticFeedback
using Microsoft.Maui.Graphics;
using Plugin.Maui.Audio;     // Provides AudioPlayer
using System;
using System.IO;

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

    public static readonly BindableProperty PageBackgroundColorProperty =
        BindableProperty.Create(nameof(PageBackgroundColor), typeof(Color), typeof(MainPage), Colors.White);

    public Color PageBackgroundColor
    {
        get => (Color)GetValue(PageBackgroundColorProperty);
        set => SetValue(PageBackgroundColorProperty, value);
    }

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        LoadAudio();
    }

    private async void LoadAudio()
    {
        try
        {
            // Load beep sound from Resources/Raw/beep.mp3
            var stream = await FileSystem.OpenAppPackageFileAsync("beep.mp3");
            _beepPlayer = AudioManager.Current.CreatePlayer(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading audio: {ex.Message}");
        }
    }

    private void TriggerTransitionAlert()
    {
        // 1. Play Audio Alert
        _beepPlayer?.Play();

        // 2. Trigger Haptic Vibration Feedback
        if (HapticFeedback.Default.IsSupported)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
    }

    private void OnStartWorkoutClicked(object sender, EventArgs e)
    {
        _jumpTimeSecs = int.TryParse(JumpSecsEntry.Text, out var j) ? j : 30;
        _restTimeSecs = int.TryParse(RestSecsEntry.Text, out var r) ? r : 15;
        _totalSets = int.TryParse(TargetSetsEntry.Text, out var s) ? s : 5;

        _currentSet = 1;
        _totalJumpsLogged = 0;

        ConfigView.IsVisible = false;
        SummaryView.IsVisible = false;
        ActiveView.IsVisible = true;

        StartJumpInterval();
    }

    private void StartJumpInterval()
    {
        TriggerTransitionAlert(); // <-- Alert on transition to Jump
        PageBackgroundColor = Color.FromArgb("#4CAF50"); // Green
        StatusLabel.Text = "JUMP!";
        SetInfoLabel.Text = $"Set {_currentSet} of {_totalSets}";
        StartCountDown(_jumpTimeSecs, OnJumpIntervalFinished);
    }

    private void StartRestInterval()
    {
        TriggerTransitionAlert(); // <-- Alert on transition to Rest
        PageBackgroundColor = Color.FromArgb("#FF9800"); // Orange
        StatusLabel.Text = "REST";
        StartCountDown(_restTimeSecs, OnRestIntervalFinished);
    }

    private void StartCountDown(int seconds, Action onFinish)
    {
        _timeRemaining = seconds;
        TimerLabel.Text = _timeRemaining.ToString();

        _timer?.Stop();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) =>
        {
            _timeRemaining--;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimerLabel.Text = _timeRemaining.ToString();

                // Optional: Short haptic click during the last 3 seconds of a countdown
                if (_timeRemaining is > 0 and <= 3 && HapticFeedback.Default.IsSupported)
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }

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
            FinishWorkout();
        }
    }

    private void OnEndWorkoutClicked(object sender, EventArgs e)
    {
        _timer?.Stop();
        FinishWorkout();
    }

    private void FinishWorkout()
    {
        TriggerTransitionAlert(); // Completion alert
        PageBackgroundColor = Colors.White;
        ActiveView.IsVisible = false;
        SummaryView.IsVisible = true;

        CompletedSetsLabel.Text = $"Sets Completed: {_currentSet}";
        TotalJumpsLabel.Text = $"Total Jumps Recorded: {_totalJumpsLogged}";
    }

    private void OnAddJumpsClicked(object sender, EventArgs e)
    {
        if (int.TryParse(JumpsLoggedEntry.Text, out var count))
        {
            _totalJumpsLogged += count;
            TotalJumpsLabel.Text = $"Total Jumps Recorded: {_totalJumpsLogged}";
            JumpsLoggedEntry.Text = string.Empty;
        }
    }

    private void OnNewWorkoutClicked(object sender, EventArgs e)
    {
        SummaryView.IsVisible = false;
        ConfigView.IsVisible = true;
    }
}