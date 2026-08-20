using JumpRopeApp.Models;
using System;
using Microsoft.Maui.Controls;

namespace JumpRopeApp;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _dbService;

    // Timer & Interval State Variables
    private int _jumpSecs = 30;
    private int _restSecs = 15;
    private int _targetSets = 5;

    private int _currentSet = 1;
    private int _secondsRemaining = 0;
    private bool _isJumpInterval = true;
    private bool _isTimerRunning = false;

    private int _totalJumpsInWorkout = 0;

    public MainPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        UpdateConfigUI();
    }

    // ==========================================
    // 1. INTERVAL CONFIGURATION BUTTON HANDLERS
    // ==========================================

    private void OnIncreaseJumpClicked(object sender, EventArgs e)
    {
        _jumpSecs += 5;
        UpdateConfigUI();
    }

    private void OnDecreaseJumpClicked(object sender, EventArgs e)
    {
        if (_jumpSecs > 5) _jumpSecs -= 5;
        UpdateConfigUI();
    }

    private void OnIncreaseRestClicked(object sender, EventArgs e)
    {
        _restSecs += 5;
        UpdateConfigUI();
    }

    private void OnDecreaseRestClicked(object sender, EventArgs e)
    {
        if (_restSecs > 5) _restSecs -= 5;
        UpdateConfigUI();
    }

    private void OnIncreaseSetsClicked(object sender, EventArgs e)
    {
        _targetSets++;
        UpdateConfigUI();
    }

    private void OnDecreaseSetsClicked(object sender, EventArgs e)
    {
        if (_targetSets > 1) _targetSets--;
        UpdateConfigUI();
    }

    private void UpdateConfigUI()
    {
        JumpSecsLabel.Text = _jumpSecs.ToString();
        RestSecsLabel.Text = _restSecs.ToString();
        TargetSetsLabel.Text = _targetSets.ToString();
    }

    // ==========================================
    // 2. FITNESS SETS & PRESETS SELECTION
    // ==========================================

    private void OnWorkoutTypeChanged(object sender, EventArgs e)
    {
        if (WorkoutTypePicker.SelectedIndex == -1) return;

        string selectedType = WorkoutTypePicker.SelectedItem?.ToString() ?? string.Empty;

        switch (selectedType)
        {
            case "HIIT Speed Sets (20s / 10s)":
                _jumpSecs = 20;
                _restSecs = 10;
                _targetSets = 8;
                break;
            case "Skill Sets (Double Unders)":
                _jumpSecs = 45;
                _restSecs = 15;
                _targetSets = 5;
                break;
            case "Full-Body Mixed Circuit":
                _jumpSecs = 60;
                _restSecs = 30;
                _targetSets = 4;
                break;
            default: // Basic Jumps
                _jumpSecs = 30;
                _restSecs = 15;
                _targetSets = 5;
                break;
        }

        UpdateConfigUI();
    }

    private void OnPresetBeginnerClicked(object sender, EventArgs e)
    {
        _jumpSecs = 20;
        _restSecs = 20;
        _targetSets = 5;
        WorkoutTypePicker.SelectedIndex = 0; // Basic Jumps
        UpdateConfigUI();
    }

    private void OnPresetHIITClicked(object sender, EventArgs e)
    {
        _jumpSecs = 30;
        _restSecs = 15;
        _targetSets = 8;
        WorkoutTypePicker.SelectedIndex = 1; // HIIT
        UpdateConfigUI();
    }

    private void OnPresetTabataClicked(object sender, EventArgs e)
    {
        _jumpSecs = 20;
        _restSecs = 10;
        _targetSets = 8;
        WorkoutTypePicker.SelectedIndex = 1; // HIIT
        UpdateConfigUI();
    }

    private void OnPresetEnduranceClicked(object sender, EventArgs e)
    {
        _jumpSecs = 60;
        _restSecs = 20;
        _targetSets = 10;
        WorkoutTypePicker.SelectedIndex = 0; // Basic Jumps
        UpdateConfigUI();
    }

    // ==========================================
    // 3. WORKOUT EXECUTION & TIMER LOOP
    // ==========================================

    private void OnStartWorkoutClicked(object sender, EventArgs e)
    {
        ConfigView.IsVisible = false;
        SummaryView.IsVisible = false;
        ActiveView.IsVisible = true;

        _currentSet = 1;
        _isJumpInterval = true;
        _secondsRemaining = _jumpSecs;
        _isTimerRunning = true;
        _totalJumpsInWorkout = 0;

        UpdateTimerUI();
        StartTimerLoop();
    }

    private void StartTimerLoop()
    {
        Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            if (!_isTimerRunning) return false;

            _secondsRemaining--;

            if (_secondsRemaining <= 0)
            {
                if (_isJumpInterval)
                {
                    // Switch to Rest
                    _isJumpInterval = false;
                    _secondsRemaining = _restSecs;
                }
                else
                {
                    // Next Set
                    _currentSet++;
                    if (_currentSet > _targetSets)
                    {
                        _isTimerRunning = false;
                        ShowSummary();
                        return false;
                    }

                    _isJumpInterval = true;
                    _secondsRemaining = _jumpSecs;
                }
            }

            UpdateTimerUI();
            return true;
        });
    }

    private void UpdateTimerUI()
    {
        TimerLabel.Text = _secondsRemaining.ToString();
        SetInfoLabel.Text = $"Set {_currentSet} of {_targetSets}";

        if (_isJumpInterval)
        {
            StatusLabel.Text = "JUMP!";
            StatusLabel.TextColor = Color.FromArgb("#10B981"); // Green
        }
        else
        {
            StatusLabel.Text = "REST";
            StatusLabel.TextColor = Color.FromArgb("#F59E0B"); // Orange
        }
    }

    private void OnEndWorkoutClicked(object sender, EventArgs e)
    {
        _isTimerRunning = false;
        ShowSummary();
    }

    // ==========================================
    // 4. SUMMARY & DATABASE PERSISTENCE
    // ==========================================

    private void ShowSummary()
    {
        ActiveView.IsVisible = false;
        ConfigView.IsVisible = false;
        SummaryView.IsVisible = true;

        int completedSets = Math.Min(_currentSet, _targetSets);
        CompletedSetsLabel.Text = $"Sets Completed: {completedSets}";
        TotalJumpsLabel.Text = $"Total Jumps Recorded: {_totalJumpsInWorkout}";
    }

    private void OnAddJumpsClicked(object sender, EventArgs e)
    {
        if (int.TryParse(JumpsLoggedEntry.Text, out int extraJumps) && extraJumps > 0)
        {
            _totalJumpsInWorkout += extraJumps;
            TotalJumpsLabel.Text = $"Total Jumps Recorded: {_totalJumpsInWorkout}";
            JumpsLoggedEntry.Text = string.Empty;
        }
    }

    private async void OnSaveWorkoutClicked(object sender, EventArgs e)
    {
        int completedSets = Math.Min(_currentSet, _targetSets);
        string selectedType = WorkoutTypePicker.SelectedItem?.ToString() ?? "Basic Jumps";

        var record = new WorkoutRecord
        {
            Date = DateTime.Now,
            SetsCompleted = completedSets,
            TotalJumps = _totalJumpsInWorkout,
            JumpSecs = _jumpSecs,
            RestSecs = _restSecs,
            WorkoutType = selectedType
        };

        await _dbService.SaveWorkoutAsync(record);

        ResetToConfigView();
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        ResetToConfigView();
    }

    private void ResetToConfigView()
    {
        ActiveView.IsVisible = false;
        SummaryView.IsVisible = false;
        ConfigView.IsVisible = true;
    }
}