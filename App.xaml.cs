using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

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
        _ = InitializeStepTrackerAsync();
        MainPage = new AppShell();
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

    private async Task InitializeStepTrackerAsync()
    {
#if ANDROID
        try
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
            {
                var sensorStatus = await Permissions.CheckStatusAsync<Permissions.Sensors>();
                if (sensorStatus != PermissionStatus.Granted)
                {
                    await Permissions.RequestAsync<Permissions.Sensors>();
                }
            }

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                var notifStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (notifStatus != PermissionStatus.Granted)
                {
                    await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
            }

            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(Platforms.Android.StepForegroundService));
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                Android.App.Application.Context.StartForegroundService(intent);
            }
            else
            {
                Android.App.Application.Context.StartService(intent);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start service: {ex.Message}");
        }
#endif
    }
}