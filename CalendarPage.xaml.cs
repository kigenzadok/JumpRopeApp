using JumpRopeApp.Models;
using Microsoft.Maui.Controls;
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
        UpdateMonthlySummary(WorkoutDatePicker.Date);
        FilterDayWorkouts(WorkoutDatePicker.Date);
    }

    private void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        UpdateMonthlySummary(e.NewDate);
        FilterDayWorkouts(e.NewDate);
    }

    private void FilterDayWorkouts(DateTime targetDate)
    {
        SelectedDayHeaderLabel.Text = $"Logs for {targetDate:MMM dd, yyyy}";

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
        int totalSecs = monthRecords.Sum(w => w.DurationSeconds);
        double totalCalories = monthRecords.Sum(w => w.CaloriesBurned);

        MonthlyJumpsLabel.Text = totalJumps.ToString("N0");
        MonthlyTimeLabel.Text = $"{totalSecs / 60}m {totalSecs % 60}s";
        MonthlyCaloriesLabel.Text = $"{totalCalories:N0} kcal";
    }

    private async void OnDeleteWorkoutInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is WorkoutRecord record)
        {
            bool confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete this workout log?", "Delete", "Cancel");
            if (confirm)
            {
                await _dbService.DeleteWorkoutAsync(record);
                await RefreshDataAsync();
            }
        }
    }
}