using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class CalendarPage : ContentPage
{
    private readonly DatabaseService _dbService = new();

    public CalendarPage()
    {
        InitializeComponent();

        // Wire up the DatePicker selection event
        WorkoutDatePicker.DateSelected += OnDateSelected;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Default picker to today's date and load initial stats
        WorkoutDatePicker.Date = DateTime.Now.Date;
        await LoadCalendarDataAsync(WorkoutDatePicker.Date);
    }

    private async void OnDateSelected(object? sender, DateChangedEventArgs e)
    {
        await LoadCalendarDataAsync(e.NewDate);
    }

    private async Task LoadCalendarDataAsync(DateTime selectedDate)
    {
        // 1. Refresh Streak and Badges
        int streak = await _dbService.GetCurrentStreakAsync();
        StreakLabel.Text = $"{streak} Days";
        BadgeLabel.Text = GetBadgeForStreak(streak);

        // 2. Update Header Selected Date Label
        SelectedDateLabel.Text = selectedDate.ToString("MMM dd, yyyy");

        // 3. Load Day Aggregate Totals
        var (sets, jumps) = await _dbService.GetDayStatsAsync(selectedDate);
        DaySetsLabel.Text = $"Sets: {sets}";
        DayJumpsLabel.Text = $"Jumps: {jumps}";

        // 4. Load Detailed Workouts Collection for Selected Date
        var records = await _dbService.GetWorkoutsForDateAsync(selectedDate);

        if (records != null && records.Count > 0)
        {
            EmptyStateLabel.IsVisible = false;
            DayWorkoutsListView.ItemsSource = records;
            DayWorkoutsListView.IsVisible = true;
        }
        else
        {
            DayWorkoutsListView.ItemsSource = null;
            DayWorkoutsListView.IsVisible = false;
            EmptyStateLabel.IsVisible = true;
        }
    }

    private static string GetBadgeForStreak(int streakDays)
    {
        return streakDays switch
        {
            >= 30 => "Pro Jumper 🏆",
            >= 14 => "Consistent 🔥",
            >= 7 => "Rhythm Starter ⚡",
            _ => "Beginner 🌱"
        };
    }
}