using JumpRopeApp.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JumpRopeApp;

public class RoutineModel
{
    public string Name { get; set; } = string.Empty;
    public int TargetSteps { get; set; }
    public int TargetJumps { get; set; }
}

public partial class RoutinesPage : ContentPage
{
    private readonly List<RoutineModel> _routines = new();

    public RoutinesPage()
    {
        InitializeComponent();
        LoadRoutines();
    }

    private void LoadRoutines()
    {
        // For demonstration or persistence via Preferences / SQLite
        RoutinesCollectionView.ItemsSource = null;
        RoutinesCollectionView.ItemsSource = _routines;
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

        _routines.Add(new RoutineModel
        {
            Name = name,
            TargetSteps = steps,
            TargetJumps = jumps
        });

        // Clear entries
        RoutineNameEntry.Text = string.Empty;
        TargetStepsEntry.Text = string.Empty;
        TargetJumpsEntry.Text = string.Empty;

        LoadRoutines();
        await DisplayAlert("Success", "Custom routine saved successfully!", "OK");
    }

    private void OnDeleteRoutineClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is RoutineModel routine)
        {
            _routines.Remove(routine);
            LoadRoutines();
        }
    }
}