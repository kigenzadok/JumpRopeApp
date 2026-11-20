using JumpRopeApp.Models;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class StatsPage : ContentPage
{
    private readonly DatabaseService _dbService = new();
    private readonly JumpsChartDrawable _jumpsChartDrawable = new();
    private readonly StepsChartDrawable _stepsChartDrawable = new();

    private UserProfile _profile = new();
    private bool _hasExistingProfile = false;

    public StatsPage()
    {
        InitializeComponent();
        JumpsChartGraphicsView.Drawable = _jumpsChartDrawable;
        StepsChartGraphicsView.Drawable = _stepsChartDrawable;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Logger.Log("Navigated to StatsPage - Starting OnAppearing");

        try
        {
            await LoadProfileAsync();
            await LoadStatsAsync();
            await LoadPersonalRecordsAsync();
            await LoadChartDataAsync();
            Logger.Log("StatsPage loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.Log("CRASH DETECTED IN STATSPAGE OnAppearing", ex);
            await DisplayAlert("Crash Diagnostic Captured", $"{ex.Message}\n\nCheck logs via Lifetime Overview.", "OK");
        }
    }

    // ==========================================
    // 1. PROFILE & SETTINGS
    // ==========================================

    private async Task LoadProfileAsync()
    {
        var savedProfile = await _dbService.GetProfileAsync();
        if (savedProfile != null)
        {
            _profile = savedProfile;
            _hasExistingProfile = true;
            UpdateProfileUI();
            SwitchToViewMode();
        }
        else
        {
            _profile = new UserProfile { DailyJumpGoal = 1000 };
            _hasExistingProfile = false;
            UpdateProfileUI();
            SwitchToEditMode();
        }
    }

    private void UpdateProfileUI()
    {
        DisplayPageNameLabel.Text = string.IsNullOrWhiteSpace(_profile.Name) ? "Not set" : _profile.Name;
        DisplayWeightLabel.Text = _profile.WeightKg > 0 ? $"{_profile.WeightKg} kg" : "-- kg";
        DisplayHeightLabel.Text = _profile.HeightCm > 0 ? $"{_profile.HeightCm} cm" : "-- cm";
        DisplayGoalLabel.Text = (_profile.DailyJumpGoal > 0 ? _profile.DailyJumpGoal : 1000).ToString("N0");

        NameEntry.Text = _profile.Name;
        WeightEntry.Text = _profile.WeightKg > 0 ? _profile.WeightKg.ToString() : string.Empty;
        HeightEntry.Text = _profile.HeightCm > 0 ? _profile.HeightCm.ToString() : string.Empty;
        GoalEntry.Text = (_profile.DailyJumpGoal > 0 ? _profile.DailyJumpGoal : 1000).ToString();
        HapticsSwitch.IsToggled = _profile.EnableHaptics;
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

        profile.Name = NameEntry.Text?.Trim() ?? string.Empty;
        profile.WeightKg = double.TryParse(WeightEntry.Text, out var w) ? w : 0;
        profile.HeightCm = double.TryParse(HeightEntry.Text, out var h) ? h : 0;
        profile.DailyJumpGoal = int.TryParse(GoalEntry.Text, out var g) && g > 0 ? g : 1000;
        profile.EnableHaptics = HapticsSwitch.IsToggled;

        await _dbService.SaveOrUpdateProfileAsync(profile);
        await LoadProfileAsync();
        await LoadStatsAsync();
        SwitchToViewMode();
    }

    public void OnCancelOrDeleteClicked(object? sender, EventArgs e)
    {
        if (_hasExistingProfile)
        {
            UpdateProfileUI();
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
    // 2. ANALYTICS, PRs, PROGRESS & CHARTS
    // ==========================================

    private async Task LoadStatsAsync()
    {
        var (jumps, sets, workouts, calories) = await _dbService.GetLifetimeStatsAsync();

        AllTimeJumpsLabel.Text = jumps.ToString("N0");
        TotalWorkoutsLabel.Text = workouts.ToString("N0");
        TotalCaloriesLabel.Text = $"{calories:N0} kcal";

        int avg = workouts > 0 ? jumps / workouts : 0;
        AvgJumpsLabel.Text = avg.ToString("N0");

        var workoutsList = await _dbService.GetWorkoutsAsync();
        int totalStepsFromWorkouts = workoutsList != null ? workoutsList.Sum(w => w.DailySteps) : 0;
        int currentAccumulativeSteps = Preferences.Default.Get("SavedAccumulativeSteps", 0);
        int totalLifetimeSteps = Math.Max(totalStepsFromWorkouts, currentAccumulativeSteps);

        LifetimeStepsLabel.Text = totalLifetimeSteps.ToString("N0");

        var (_, todayJumps) = await _dbService.GetDayStatsAsync(DateTime.Today);
        var profile = await _dbService.GetProfileAsync();
        int targetGoal = profile?.DailyJumpGoal > 0 ? profile.DailyJumpGoal : 1000;

        double progress = Math.Clamp((double)todayJumps / targetGoal, 0.0, 1.0);
        GoalProgressBar.Progress = progress;
        GoalPercentLabel.Text = $"{Math.Round(progress * 100)}%";
        GoalProgressTextLabel.Text = $"{todayJumps:N0} / {targetGoal:N0} Jumps Completed";

        MostStepsLabel.Text = (workoutsList != null && workoutsList.Any())
            ? workoutsList.Max(w => w.DailySteps).ToString("N0")
            : "0";
    }

    private async Task LoadPersonalRecordsAsync()
    {
        var (maxJumps, maxSets, streak) = await _dbService.GetPersonalRecordsAsync();
        PrMostJumpsLabel.Text = maxJumps.ToString("N0");
        PrMostSetsLabel.Text = maxSets.ToString();
        PrStreakLabel.Text = $"{streak} d";
    }

    private async Task LoadChartDataAsync()
    {
        try
        {
            var weeklyJumps = await _dbService.GetWeeklyJumpTotalsAsync() ?? new();

            _jumpsChartDrawable.Data = weeklyJumps.Any()
                ? weeklyJumps.Select(d => (d.DayName, d.Jumps)).ToList()
                : new();

            DateTime today = DateTime.Today;
            var stepDataList = new List<(string DayName, int Steps)>();

            foreach (var item in weeklyJumps)
            {
                int steps = 0;
                try
                {
                    if (item.DayName.Equals(today.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                    {
                        steps = Preferences.Default.Get("SavedDailySteps", 0);
                    }
                    else
                    {
                        for (int i = 1; i <= 7; i++)
                        {
                            DateTime pastDate = today.AddDays(-i);
                            if (pastDate.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture)
                                .Equals(item.DayName, StringComparison.OrdinalIgnoreCase))
                            {
                                string dateKey = $"Steps_{pastDate:yyyy-MM-dd}";
                                steps = Preferences.Default.Get(dateKey, 0);
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    steps = 0;
                }

                stepDataList.Add((item.DayName, steps));
            }

            _stepsChartDrawable.Data = stepDataList;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                JumpsChartGraphicsView?.Invalidate();
                StepsChartGraphicsView?.Invalidate();
            });
        }
        catch (Exception ex)
        {
            Logger.Log("Error loading chart data on StatsPage", ex);
        }
    }

    // ==========================================
    // 3. CHART DRAWABLES
    // ==========================================

    public class JumpsChartDrawable : IDrawable
    {
        public List<(string DayName, int Jumps)> Data { get; set; } = new();

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.Transparent;
            canvas.FillRectangle(dirtyRect);
            if (Data == null || !Data.Any()) return;

            float width = dirtyRect.Width;
            float height = dirtyRect.Height;
            int count = Data.Count;
            float spacing = 8;
            float barWidth = Math.Max(6, (width - (spacing * (count + 1))) / count);
            float usableHeight = height - 25;

            canvas.StrokeColor = Color.FromRgba(150, 150, 150, 25);
            canvas.StrokeSize = 1;
            canvas.DrawLine(0, 5, width, 5);
            canvas.DrawLine(0, usableHeight / 2, width, usableHeight / 2);
            canvas.DrawLine(0, usableHeight, width, usableHeight);

            for (int i = 0; i < count; i++)
            {
                var item = Data[i];
                float x = spacing + i * (barWidth + spacing);

                float barHeight = usableHeight * Math.Clamp((float)item.Jumps / 2000f, 0.04f, 1.0f);
                float y = usableHeight - barHeight;

                canvas.FillColor = Color.FromArgb("#6366F1");
                canvas.FillRoundedRectangle(x, y, barWidth, barHeight, barWidth / 2);

                canvas.FontColor = Color.FromArgb("#94A3B8");
                canvas.FontSize = 9;
                canvas.DrawString(item.DayName, x - 4, height - 18, barWidth + 8, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }

    }
        public class StepsChartDrawable : IDrawable
        {
            public List<(string DayName, int Steps)> Data { get; set; } = new();

            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                canvas.FillColor = Colors.Transparent;
                canvas.FillRectangle(dirtyRect);
                if (Data == null || !Data.Any()) return;

                float width = dirtyRect.Width;
                float height = dirtyRect.Height;
                int count = Data.Count;
                float spacing = 8;
                float barWidth = Math.Max(6, (width - (spacing * (count + 1))) / count);
                float usableHeight = height - 25;

                canvas.StrokeColor = Color.FromRgba(150, 150, 150, 25);
                canvas.StrokeSize = 1;
                canvas.DrawLine(0, 5, width, 5);
                canvas.DrawLine(0, usableHeight / 2, width, usableHeight / 2);
                canvas.DrawLine(0, usableHeight, width, usableHeight);

                for (int i = 0; i < count; i++)
                {
                    var item = Data[i];
                    float x = spacing + i * (barWidth + spacing);
                    float barHeight = usableHeight * Math.Clamp((float)item.Steps / 10000f, 0.04f, 1.0f);
                    float y = usableHeight - barHeight;

                    canvas.FillColor = Color.FromArgb("#10B981");
                    canvas.FillRoundedRectangle(x, y, barWidth, barHeight, barWidth / 2);

                    canvas.FontColor = Color.FromArgb("#94A3B8");
                    canvas.FontSize = 9;
                    canvas.DrawString(item.DayName, x - 4, height - 18, barWidth + 8, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
        }
    
}