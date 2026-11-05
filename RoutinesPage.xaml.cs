using JumpRopeApp.Models;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class RoutinesPage : ContentPage
{
    private readonly DatabaseService _dbService = new();

    public RoutinesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRoutinesAsync();
    }

    private async Task LoadRoutinesAsync()
    {
        var routines = await _dbService.GetCustomRoutineRecordsAsync();
        RoutinesCollectionView.ItemsSource = routines;
    }

    private async void OnSaveRoutineClicked(object sender, EventArgs e)
    {
        string name = RoutineNameEntry.Text?.Trim() ?? string.Empty;
        int.TryParse(TargetStepsEntry.Text, out int steps);
        int.TryParse(TargetJumpsEntry.Text, out int jumps);

        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert("Error", "Please enter a routine name.", "OK");
            return;
        }

        var newRoutine = new RoutineRecord
        {
            Name = name,
            TargetSteps = steps,
            TargetJumps = jumps
        };

        await _dbService.SaveRoutineRecordAsync(newRoutine);

        // Clear entries
        RoutineNameEntry.Text = string.Empty;
        TargetStepsEntry.Text = string.Empty;
        TargetJumpsEntry.Text = string.Empty;

        await LoadRoutinesAsync();
        await DisplayAlert("Success", "Custom routine saved successfully!", "OK");
    }

    private async void OnDeleteRoutineClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is RoutineRecord routine)
        {
            await _dbService.DeleteRoutineRecordAsync(routine);
            await LoadRoutinesAsync();
        }
    }
}