using JumpRopeApp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class StatsPage : ContentPage
{
    private readonly DatabaseService _dbService = new();
    private readonly WeeklyChartDrawable _chartDrawable = new();
    private UserProfile _profile = new();
    private bool _isEditingProfile = false;

    public StatsPage()
    {
        InitializeComponent();
        ChartGraphicsView.Drawable = _chartDrawable;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _profile = await _dbService.GetProfileAsync();
        UpdateProfileUI();

        // 1. Lifetime Stats
        var (totalJumps, totalSets, totalWorkouts, totalCalories) = await _dbService.GetLifetimeStatsAsync();
        AllTimeJumpsLabel.Text = totalJumps.ToString("N0");
        TotalWorkoutsLabel.Text = totalWorkouts.ToString("N0");
        TotalCaloriesLabel.Text = $"{totalCalories:N0} kcal";

        int avgJumpsPerSet = totalSets > 0 ? totalJumps / totalSets : 0;
        AvgJumpsLabel.Text = avgJumpsPerSet.ToString("N0");

        // 2. Daily Goal Progress
        var (todaySets, todayJumps) = await _dbService.GetDayStatsAsync(DateTime.Today);
        int targetGoal = _profile.DailyTargetJumps > 0 ? _profile.DailyTargetJumps : 5000;
        double goalProgress = Math.Min(1.0, (double)todayJumps / targetGoal);

        GoalProgressTextLabel.Text = $"Today: {todayJumps:N0} / {targetGoal:N0} jumps";
        GoalPercentLabel.Text = $"{(int)(goalProgress * 100)}%";
        GoalProgressBar.Progress = goalProgress;

        // 3. Personal Records & Streak
        var (maxJumps, maxSets, streak) = await _dbService.GetPersonalRecordsAsync();
        PrMostJumpsLabel.Text = maxJumps.ToString("N0");
        PrMostSetsLabel.Text = maxSets.ToString();
        PrStreakLabel.Text = $"{streak} days";

        // 4. 7-Day Visualizer Chart
        var weeklyData = await _dbService.GetWeeklyJumpTotalsAsync();
        _chartDrawable.DayLabels = weeklyData.Select(d => d.DayName).ToList();
        _chartDrawable.DailyTotals = weeklyData.Select(d => d.Jumps).ToList();
        ChartGraphicsView.Invalidate();
    }

    private void UpdateProfileUI()
    {
        DisplayPageNameLabel.Text = string.IsNullOrWhiteSpace(_profile.Name) ? "👤 User Profile" : $"👤 {_profile.Name}";
        DisplayWeightLabel.Text = $"Weight: {_profile.WeightKg} kg";
        DisplayHeightLabel.Text = $"Height: {_profile.HeightCm} cm";
        DisplayGoalLabel.Text = $"Daily Goal: {_profile.DailyTargetJumps:N0} jumps";

        NameEntry.Text = _profile.Name;
        WeightEntry.Text = _profile.WeightKg.ToString();
        HeightEntry.Text = _profile.HeightCm.ToString();
        GoalEntry.Text = _profile.DailyTargetJumps.ToString();
        HapticsSwitch.IsToggled = _profile.EnableHaptics;
    }

    private async void OnEditProfileClicked(object sender, EventArgs e)
    {
        if (_isEditingProfile)
        {
            // Save Profile Action
            _profile.Name = NameEntry.Text;
            _profile.WeightKg = double.TryParse(WeightEntry.Text, out var w) ? w : 70;
            _profile.HeightCm = double.TryParse(HeightEntry.Text, out var h) ? h : 175;
            _profile.DailyTargetJumps = int.TryParse(GoalEntry.Text, out var g) ? g : 5000;
            _profile.EnableHaptics = HapticsSwitch.IsToggled;

            await _dbService.SaveOrUpdateProfileAsync(_profile);
            _isEditingProfile = false;
            ToggleEditMode(false);
            await LoadDataAsync();
        }
        else
        {
            // Toggle to Edit Mode
            _isEditingProfile = true;
            ToggleEditMode(true);
        }
    }

    private void OnCancelProfileClicked(object sender, EventArgs e)
    {
        _isEditingProfile = false;
        ToggleEditMode(false);
        UpdateProfileUI();
    }

    private void ToggleEditMode(bool isEditing)
    {
        ProfileViewMode.IsVisible = !isEditing;
        ProfileEditMode.IsVisible = isEditing;
        EditProfileButton.Text = isEditing ? "Save" : "Edit";
        EditProfileButton.BackgroundColor = isEditing ? Color.FromArgb("#10B981") : Color.FromArgb("#6366F1");
    }

    private async void OnExportCsvClicked(object sender, EventArgs e)
    {
        try
        {
            string csvPath = await _dbService.ExportWorkoutsToCsvAsync();
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Workout History",
                File = new ShareFile(csvPath)
            });
        }
        catch (Exception ex)
        {
            // Line 131 fix
            await DisplayAlertAsync("Export Error", ex.Message, "OK");
        }
    }
}