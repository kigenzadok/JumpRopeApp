using JumpRopeApp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class CalendarPage : ContentPage
{
    private readonly DatabaseService _dbService = new();
    private List<WorkoutRecord> _allWorkouts = new();

    public CalendarPage()
    {
        InitializeComponent();
        WorkoutDatePicker.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshDataAsync();
    }

    private async Task RefreshDataAsync()
    {
        _allWorkouts = await _dbService.GetWorkoutsAsync();
        DateTime activeDate = WorkoutDatePicker.Date ?? DateTime.Today;
        UpdateMonthlySummary(activeDate);
        FilterDayWorkouts(activeDate);
    }

    private void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        DateTime selectedDate = e.NewDate ?? DateTime.Today;

        UpdateMonthlySummary(selectedDate);
        FilterDayWorkouts(selectedDate);
    }

    private void FilterDayWorkouts(DateTime targetDate)
    {
        SelectedDayHeaderLabel.Text = $"Logs for {targetDate:MMM dd, yyyy}";

        int daySteps = GetStepsForDate(targetDate, _allWorkouts);
        SelectedDateStepsLabel.Text = $"Steps for {targetDate:MMM dd}: {daySteps:N0}";

        var dayRecords = _allWorkouts
            .Where(w => w.Date.Date == targetDate.Date)
            .OrderByDescending(w => w.Date)
            .ToList();

        DayWorkoutsCollectionView.ItemsSource = dayRecords;
    }

    private void UpdateMonthlySummary(DateTime selectedDate)
    {
        MonthSummaryTitleLabel.Text = $"📊 Summary for {selectedDate:MMMM yyyy}";

        var monthRecords = _allWorkouts
            .Where(w => w.Date.Year == selectedDate.Year && w.Date.Month == selectedDate.Month)
            .ToList();

        int totalJumps = monthRecords.Sum(w => w.TotalJumps);
        int totalSteps = GetMonthlySteps(selectedDate, _allWorkouts);
        int totalSecs = monthRecords.Sum(w => w.DurationSeconds);
        double totalCalories = monthRecords.Sum(w => w.CaloriesBurned);

        MonthJumpsLabel.Text = totalJumps.ToString("N0");
        MonthStepsLabel.Text = totalSteps.ToString("N0");
        MonthTimeLabel.Text = $"{totalSecs / 60}m {totalSecs % 60}s";
        MonthCaloriesLabel.Text = $"{totalCalories:N0} kcal";
    }

    private async void OnDeleteWorkoutInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is WorkoutRecord record)
        {
            bool confirm = await DisplayAlertAsync("Confirm Delete", "Are you sure you want to delete this workout log?", "Delete", "Cancel");
            if (confirm)
            {
                await _dbService.DeleteWorkoutAsync(record);
                await RefreshDataAsync();
            }
        }
    }

    private int GetStepsForDate(DateTime date, IEnumerable<WorkoutRecord> workouts)
    {
        int prefSteps = date.Date == DateTime.Today
            ? Math.Max(
                Preferences.Default.Get($"Steps_{date:yyyy-MM-dd}", 0),
                Preferences.Default.Get("SavedDailySteps", 0))
            : Preferences.Default.Get($"Steps_{date:yyyy-MM-dd}", 0);

        int workoutSteps = workouts
            .Where(w => w.Date.Date == date.Date)
            .Select(w => w.DailySteps)
            .DefaultIfEmpty(0)
            .Max();

        return Math.Max(prefSteps, workoutSteps);
    }

    private int GetMonthlySteps(DateTime monthAnchor, IEnumerable<WorkoutRecord> workouts)
    {
        int daysInMonth = DateTime.DaysInMonth(monthAnchor.Year, monthAnchor.Month);
        int totalMonthSteps = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDay = new DateTime(monthAnchor.Year, monthAnchor.Month, day);
            if (currentDay.Date > DateTime.Today)
                break;

            totalMonthSteps += GetStepsForDate(currentDay, workouts);
        }

        return totalMonthSteps;
    }
}