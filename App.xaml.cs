using JumpRopeApp.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class App : Application
{
    public static StepTrackerService StepTracker { get; } = new();
    public static int DailyStepCount { get; private set; } = 0;
    public static event Action<int>? OnDailyStepsUpdated;

    private const string StepCountKey = "SavedDailySteps";
    private const string StepDateKey = "SavedStepDate";

    public App()
    {
        InitializeComponent();

        // Load persisted steps before initializing sensor
        LoadSavedStepCount();
        _ = InitializeStepTrackerAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    private void LoadSavedStepCount()
    {
        string lastSavedDate = Preferences.Default.Get(StepDateKey, string.Empty);
        string todayDate = DateTime.Today.ToString("yyyy-MM-dd");

        if (lastSavedDate == todayDate)
        {
            // Same day -> restore step count
            DailyStepCount = Preferences.Default.Get(StepCountKey, 0);
        }
        else
        {
            // New day -> reset count and save today's date
            DailyStepCount = 0;
            Preferences.Default.Set(StepCountKey, 0);
            Preferences.Default.Set(StepDateKey, todayDate);
        }
    }

    private async Task InitializeStepTrackerAsync()
    {
        await StepTracker.StartContinuousTrackingAsync(stepsDelta =>
        {
            string todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            string lastSavedDate = Preferences.Default.Get(StepDateKey, string.Empty);

            // Auto-reset at midnight
            if (lastSavedDate != todayDate)
            {
                DailyStepCount = 0;
                Preferences.Default.Set(StepDateKey, todayDate);
            }

            // Increment or update live step count
            DailyStepCount = stepsDelta;

            // Persist immediately
            Preferences.Default.Set(StepCountKey, DailyStepCount);

            // Notify UI
            OnDailyStepsUpdated?.Invoke(DailyStepCount);
        });
    }
}