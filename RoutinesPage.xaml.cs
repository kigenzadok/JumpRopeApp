using JumpRopeApp.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
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
        var routines = await _dbService.GetRoutinesAsync();
        RoutinesCollectionView.ItemsSource = routines;
    }

    public async void OnSaveRoutineClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RoutineNameEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a routine name.", "OK");
            return;
        }

        var routine = new Routine
        {
            Name = RoutineNameEntry.Text,
            Description = "Custom multi-stage interval routine"
        };

        var steps = new List<RoutineStep>
        {
            new RoutineStep { StepOrder = 1, JumpDurationSecs = 30, RestDurationSecs = 15 },
            new RoutineStep { StepOrder = 2, JumpDurationSecs = 45, RestDurationSecs = 15 },
            new RoutineStep { StepOrder = 3, JumpDurationSecs = 60, RestDurationSecs = 20 }
        };

        await _dbService.SaveRoutineAsync(routine, steps);
        RoutineNameEntry.Text = string.Empty;
        await LoadRoutinesAsync();
        await DisplayAlert("Success", "Routine saved successfully!", "OK");
    }
}