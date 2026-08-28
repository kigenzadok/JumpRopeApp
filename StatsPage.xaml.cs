using JumpRopeApp.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class StatsPage : ContentPage
{
    private readonly DatabaseService _dbService = new();
    private readonly WeeklyChartDrawable _chartDrawable = new();
    private bool _hasExistingProfile = false;

    public StatsPage()
    {
        InitializeComponent();
        ChartGraphicsView.Drawable = _chartDrawable;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProfileAsync();
        await LoadStatsAsync();
        await LoadPersonalRecordsAsync();
        await LoadChartDataAsync();
    }

    // ==========================================
    // 1. PROFILE & SETTINGS
    // ==========================================

    private async Task LoadProfileAsync()
    {
        var profile = await _dbService.GetProfileAsync();

        if (profile != null && (!string.IsNullOrWhiteSpace(profile.Name) || profile.WeightKg > 0 || profile.HeightCm > 0))
        {
            _hasExistingProfile = true;

            DisplayPageNameLabel.Text = string.IsNullOrWhiteSpace(profile.Name) ? "Not set" : profile.Name;
            DisplayWeightLabel.Text = profile.WeightKg > 0 ? $"{profile.WeightKg} kg" : "-- kg";
            DisplayHeightLabel.Text = profile.HeightCm > 0 ? $"{profile.HeightCm} cm" : "-- cm";
            DisplayGoalLabel.Text = profile.DailyJumpGoal > 0 ? profile.DailyJumpGoal.ToString("N0") : "1,000";

            NameEntry.Text = profile.Name;
            WeightEntry.Text = profile.WeightKg > 0 ? profile.WeightKg.ToString() : string.Empty;
            HeightEntry.Text = profile.HeightCm > 0 ? profile.HeightCm.ToString() : string.Empty;
            GoalEntry.Text = profile.DailyJumpGoal > 0 ? profile.DailyJumpGoal.ToString() : "1000";

            HapticsSwitch.IsToggled = profile.EnableHaptics;

            SwitchToViewMode();
        }
        else
        {
            _hasExistingProfile = false;
            SwitchToEditMode();
        }
    }

    private void SwitchToViewMode()
    {
        ProfileViewMode.IsVisible = true;
        ProfileEditMode.IsVisible = false;
        EditProfileButton.IsVisible = true;
    }

    private void SwitchToEditMode()
    {
        ProfileViewMode.IsVisible = false;
        ProfileEditMode.IsVisible = true;
        EditProfileButton.IsVisible = false;
        CancelOrDeleteButton.Text = _hasExistingProfile ? "Cancel" : "Clear";
    }

    public void OnEditProfileClicked(object? sender, EventArgs e) => SwitchToEditMode();

    public async void OnSaveProfileClicked(object? sender, EventArgs e)
    {
        var profile = await _dbService.GetProfileAsync() ?? new UserProfile();

        profile.Name = NameEntry.Text ?? string.Empty;
        profile.WeightKg = double.TryParse(WeightEntry.Text, out var w) ? w : 0;
        profile.HeightCm = double.TryParse(HeightEntry.Text, out var h) ? h : 0;
        profile.DailyJumpGoal = int.TryParse(GoalEntry.Text, out var g) && g > 0 ? g : 1000;
        profile.EnableHaptics = HapticsSwitch.IsToggled;

        await _dbService.SaveOrUpdateProfileAsync(profile);
        await LoadProfileAsync();
        await LoadStatsAsync();
    }

    public void OnCancelOrDeleteClicked(object? sender, EventArgs e)
    {
        if (_hasExistingProfile)
        {
            SwitchToViewMode();
        }
        else
        {
            NameEntry.Text = string.Empty;
            WeightEntry.Text = string.Empty;
            HeightEntry.Text = string.Empty;
            GoalEntry.Text = "1000";
        }
    }

    public async void OnSettingsToggled(object? sender, ToggledEventArgs e)
    {
        var profile = await _dbService.GetProfileAsync();
        if (profile != null)
        {
            profile.EnableHaptics = HapticsSwitch.IsToggled;
            await _dbService.SaveOrUpdateProfileAsync(profile);
        }
    }

    // ==========================================
    // 2. ANALYTICS, PRs, PROGRESS & CHART
    // ==========================================

    private async Task LoadStatsAsync()
    {
        var (jumps, sets, workouts, calories) = await _dbService.GetLifetimeStatsAsync();

        AllTimeJumpsLabel.Text = jumps.ToString("N0");
        TotalWorkoutsLabel.Text = workouts.ToString("N0");
        TotalCaloriesLabel.Text = $"{calories:N0} kcal";

        int avg = workouts > 0 ? jumps / workouts : 0;
        AvgJumpsLabel.Text = avg.ToString("N0");

        // Today's Goal Progress
        var (todaySets, todayJumps) = await _dbService.GetDayStatsAsync(DateTime.Now);
        var profile = await _dbService.GetProfileAsync();
        int targetGoal = profile?.DailyJumpGoal > 0 ? profile.DailyJumpGoal : 1000;

        double progress = Math.Clamp((double)todayJumps / targetGoal, 0.0, 1.0);
        GoalProgressBar.Progress = progress;
        GoalPercentLabel.Text = $"{Math.Round(progress * 100)}%";
        GoalProgressTextLabel.Text = $"{todayJumps:N0} / {targetGoal:N0} Jumps Completed";
    }

    private async Task LoadPersonalRecordsAsync()
    {
        var (maxJumps, maxSets, streak) = await _dbService.GetPersonalRecordsAsync();
        PrMostJumpsLabel.Text = maxJumps.ToString("N0");
        PrMostSetsLabel.Text = maxSets.ToString("N0");
        PrStreakLabel.Text = $"{streak} Days";
    }

    private async Task LoadChartDataAsync()
    {
        var weeklyData = await _dbService.GetWeeklyJumpTotalsAsync();
        _chartDrawable.Data = weeklyData;
        ChartGraphicsView.Invalidate();
    }

    // ==========================================
    // 3. CSV EXPORT & RESET
    // ==========================================

    public async void OnExportCsvClicked(object? sender, EventArgs e)
    {
        string filePath = await _dbService.ExportWorkoutsToCsvAsync();
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Jump Rope Workouts",
                File = new ShareFile(filePath)
            });
        }
        else
        {
            await DisplayAlert("Error", "No workout data available to export.", "OK");
        }
    }

    public async void OnResetStatsClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Reset Stats", "Are you sure you want to reset all lifetime stats?", "Yes", "No");
        if (confirm)
        {
            // ? Correct: reload the individual async tasks directly
            await LoadProfileAsync();
            await LoadStatsAsync();
            await LoadPersonalRecordsAsync();
            await LoadChartDataAsync();
        }
    }
}