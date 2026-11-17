using JumpRopeApp.Models;
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
            await DisplayAlertAsync("Crash Diagnostic Captured", $"{ex.Message}\n\nCheck logs via Lifetime Overview.", "OK");
        }
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

        // Calculate total lifetime steps from saved workouts + current accumulator
        var workoutsList = await _dbService.GetWorkoutsAsync();
        int totalStepsFromWorkouts = workoutsList != null ? workoutsList.Sum(w => w.DailySteps) : 0;
        int currentAccumulativeSteps = Preferences.Get("SavedAccumulativeSteps", 0);
        int totalLifetimeSteps = Math.Max(totalStepsFromWorkouts, currentAccumulativeSteps);

        LifetimeStepsLabel.Text = totalLifetimeSteps.ToString("N0");

        // Today's Goal Progress
        var (todaySets, todayJumps) = await _dbService.GetDayStatsAsync(DateTime.Now);
        var profile = await _dbService.GetProfileAsync();
        int targetGoal = profile?.DailyJumpGoal > 0 ? profile.DailyJumpGoal : 1000;

        double progress = Math.Clamp((double)todayJumps / targetGoal, 0.0, 1.0);
        GoalProgressBar.Progress = progress;
        GoalPercentLabel.Text = $"{Math.Round(progress * 100)}%";
        GoalProgressTextLabel.Text = $"{todayJumps:N0} / {targetGoal:N0} Jumps Completed";

        if (workoutsList != null && workoutsList.Any())
        {
            int mostSteps = workoutsList.Max(w => w.DailySteps);
            MostStepsLabel.Text = mostSteps.ToString("N0");
        }
        else
        {
            MostStepsLabel.Text = "0";
        }
    }

    private async Task LoadPersonalRecordsAsync()
    {
        var (maxJumps, maxSets, streak) = await _dbService.GetPersonalRecordsAsync();
        PrMostJumpsLabel.Text = maxJumps.ToString("N0");
        PrMostSetsLabel.Text = maxSets.ToString();
        PrStreakLabel.Text = $"{streak} days";

    private async Task LoadChartDataAsync()
    {
        try
        {
            // 1. Safely fetch weekly jumps with null protection
            var weeklyJumps = await _dbService.GetWeeklyJumpTotalsAsync() ?? new();

            if (weeklyJumps.Any())
            {
                _jumpsChartDrawable.Data = weeklyJumps.Select(d => (d.DayName, d.Jumps)).ToList();
            }
            else
            {
                _jumpsChartDrawable.Data = new();
            }

            // 2. Safely compute steps for the weekly chart without risking null/format crashes
            DateTime today = DateTime.Today;
            var stepDataList = new List<(string DayName, int Steps)>();

            foreach (var item in weeklyJumps)
            {
                int steps = 0;
                try
                {
                    // Check if it's today
                    if (item.DayName.Equals(today.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                    {
                        steps = Preferences.Get("SavedDailySteps", 0);
                    }
                    else
                    {
                        // Look back through recent days safely
                        for (int i = 1; i <= 7; i++)
                        {
                            DateTime pastDate = today.AddDays(-i);
                            if (pastDate.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture)
                                .Equals(item.DayName, StringComparison.OrdinalIgnoreCase))
                            {
                                string dateKey = $"Steps_{pastDate:yyyy-MM-dd}";
                                steps = Preferences.Get(dateKey, 0);
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    steps = 0; // Fallback safeguard per item
                }

                stepDataList.Add((item.DayName, steps));
            }

            _stepsChartDrawable.Data = stepDataList;

            // 3. Invalidate UI safely on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                JumpsChartGraphicsView?.Invalidate();
                StepsChartGraphicsView?.Invalidate();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading chart data: {ex.Message}");
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

                // Scaled for a 2k maximum limit
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

    // ==========================================
    // 4. CSV EXPORT & RESET
    // ==========================================

    public async void OnExportCsvClicked(object? sender, EventArgs e)
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
            await DisplayAlertAsync("Error", "No workout data available to export.", "OK");
        }
    }

    private void OnCancelProfileClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Reset Stats", "Are you sure you want to reset all lifetime stats?", "Yes", "No");
        if (confirm)
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
    public async void OnViewLogsClicked(object? sender, EventArgs e)
    {
        string logs = Logger.GetLogContent();

        // Display recent diagnostic output
        bool share = await DisplayAlertAsync("Diagnostic Logs", logs.Length > 1500 ? logs[..1500] + "..." : logs, "Share Log", "Close");

        if (share && File.Exists(Logger.LogFilePath))
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "App Diagnostics Log",
                File = new ShareFile(Logger.LogFilePath)
            });
        }
    }
}