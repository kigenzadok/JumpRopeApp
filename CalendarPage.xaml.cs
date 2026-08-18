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

        if (WorkoutDatePicker != null)
        {
            WorkoutDatePicker.DateSelected += OnDateSelected;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataForDateAsync(WorkoutDatePicker.Date);
        await LoadStreakAsync();
    }

    private async void OnDateSelected(object? sender, DateChangedEventArgs e)
    {
        await LoadDataForDateAsync(e.NewDate);
    }

    private async Task LoadDataForDateAsync(DateTime date)
    {
        SelectedDateLabel.Text = date.ToString("MMM dd, yyyy");

        var (sets, jumps) = await _dbService.GetDayStatsAsync(date);

        SetsCompletedLabel.Text = $"Sets: {sets}";
        TotalJumpsLabel.Text = $"Jumps: {jumps:N0}";

        if (sets > 0 || jumps > 0)
        {
            BadgeLabel.Text = "✅ Completed";
            BadgeLabel.TextColor = Color.FromArgb("#10B981");
        }
        else
        {
            BadgeLabel.Text = "❌ No Workout";
            BadgeLabel.TextColor = Color.FromArgb("#A0A0B2");
        }
    }

    private async Task LoadStreakAsync()
    {
        int streak = await _dbService.GetCurrentStreakAsync();
        StreakLabel.Text = $"{streak} Day{(streak == 1 ? "" : "s")}";
    }
}