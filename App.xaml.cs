using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;

namespace JumpRopeApp;

public partial class App : Application
{
    public static int DailyStepCount { get; private set; } = 0;
    public static event Action<int>? OnDailyStepsUpdated;

    private const string StepCountKey = "SavedDailySteps";
    private const string StepDateKey = "SavedStepDate";

    public static void NotifyStepUpdate(int newSteps)
    {
        DailyStepCount = newSteps;
        OnDailyStepsUpdated?.Invoke(newSteps);
    }

    public App()
    {
        InitializeComponent();
        LoadSavedStepCount();
       // MainPage = new AppShell();
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
            DailyStepCount = Preferences.Default.Get(StepCountKey, 0);
        }
        else
        {
            DailyStepCount = 0;
            Preferences.Default.Set(StepCountKey, 0);
            Preferences.Default.Set(StepDateKey, todayDate);
        }
    }
}