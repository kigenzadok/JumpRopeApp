using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class StatsPage : ContentPage
{
    private readonly DatabaseService _dbService = new();

    public StatsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        var (jumps, sets, workouts) = await _dbService.GetLifetimeStatsAsync();

        AllTimeJumpsLabel.Text = jumps.ToString("N0");
        TotalSetsLabel.Text = sets.ToString("N0");
        TotalWorkoutsLabel.Text = workouts.ToString("N0");

        int avg = workouts > 0 ? jumps / workouts : 0;
        AvgJumpsLabel.Text = avg.ToString("N0");
    }

    private async void OnResetStatsClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Reset Stats", "Are you sure you want to reset all lifetime stats?", "Yes", "No");
        if (confirm)
        {
            // Clear stored preference values
            Preferences.Default.Remove("Lifetime_Jumps");
            Preferences.Default.Remove("Lifetime_Sets");
            Preferences.Default.Remove("Lifetime_Workouts");

            // Optionally clear database records if stats are derived from SQLite
            // await _dbService.ClearAllWorkoutsAsync();

            // Refresh UI stats correctly with await
            await LoadStatsAsync();
        }
    }
}