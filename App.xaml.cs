using JumpRopeApp.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class App : Application
{
    public static StepTrackerService StepTracker { get; } = new();
    public static int DailyStepCount { get; private set; } = 0;
    public static event Action<int>? OnDailyStepsUpdated;

    public App()
    {
        InitializeComponent();
        _ = InitializeStepTrackerAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    private async Task InitializeStepTrackerAsync()
    {
        await StepTracker.StartContinuousTrackingAsync(steps =>
        {
            DailyStepCount = steps;
            OnDailyStepsUpdated?.Invoke(DailyStepCount);
        });
    }
}