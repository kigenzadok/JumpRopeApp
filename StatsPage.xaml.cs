using JumpRopeApp.Models;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class StatsPage : ContentPage
{
    private readonly DatabaseService _dbService = new();
    private bool _hasExistingProfile = false;

    public StatsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProfileAsync();
        await LoadStatsAsync();
    }

    // ==========================================
    // 1. PROFILE MANAGEMENT & STATE TOGGLES
    // ==========================================

    private async Task LoadProfileAsync()
    {
        var profile = await _dbService.GetProfileAsync();

        if (profile != null && (!string.IsNullOrWhiteSpace(profile.Name) || profile.WeightKg > 0 || profile.HeightCm > 0))
        {
            _hasExistingProfile = true;

            // Populate Read-Only Labels
            DisplayPageNameLabel.Text = string.IsNullOrWhiteSpace(profile.Name) ? "Not set" : profile.Name;
            DisplayWeightLabel.Text = profile.WeightKg > 0 ? $"{profile.WeightKg} kg" : "-- kg";
            DisplayHeightLabel.Text = profile.HeightCm > 0 ? $"{profile.HeightCm} cm" : "-- cm";

            // Populate Input Entries for editing
            NameEntry.Text = profile.Name;
            WeightEntry.Text = profile.WeightKg > 0 ? profile.WeightKg.ToString() : string.Empty;
            HeightEntry.Text = profile.HeightCm > 0 ? profile.HeightCm.ToString() : string.Empty;

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

        if (_hasExistingProfile)
        {
            EditProfileButton.IsVisible = false;
            CancelOrDeleteButton.Text = "Cancel";
        }
        else
        {
            EditProfileButton.IsVisible = false;
            CancelOrDeleteButton.Text = "Clear";
        }
    }

    // Explicitly declared handler for XAML binding
    public void OnEditProfileClicked(object? sender, EventArgs e)
    {
        SwitchToEditMode();
    }

    public async void OnSaveProfileClicked(object? sender, EventArgs e)
    {
        var profile = await _dbService.GetProfileAsync() ?? new UserProfile();

        profile.Name = NameEntry.Text ?? string.Empty;
        profile.WeightKg = double.TryParse(WeightEntry.Text, out var w) ? w : 0;
        profile.HeightCm = double.TryParse(HeightEntry.Text, out var h) ? h : 0;

        await _dbService.SaveOrUpdateProfileAsync(profile);
        await LoadProfileAsync();
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
        }
    }

    // ==========================================
    // 2. ANALYTICS STATS METHODS
    // ==========================================

    private async Task LoadStatsAsync()
    {
        var (jumps, sets, workouts) = await _dbService.GetLifetimeStatsAsync();

        AllTimeJumpsLabel.Text = jumps.ToString("N0");
        TotalSetsLabel.Text = sets.ToString("N0");
        TotalWorkoutsLabel.Text = workouts.ToString("N0");

        int avg = workouts > 0 ? jumps / workouts : 0;
        AvgJumpsLabel.Text = avg.ToString("N0");
    }

    public async void OnResetStatsClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Reset Stats", "Are you sure you want to reset all lifetime stats?", "Yes", "No");
        if (confirm)
        {
            await _dbService.ClearAllWorkoutsAsync();
            await LoadStatsAsync();
        }
    }
}