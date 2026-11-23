using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using System;

namespace JumpRopeApp.Platforms.Android;

[Service(
    Enabled = true,
    Exported = false,
    ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeHealth)]
public class StepForegroundService : Service, ISensorEventListener
{
    public const int NotificationId = 1001;
    public const string ChannelId = "StepTrackerChannel";

    private SensorManager? _sensorManager;
    private Sensor? _stepCounterSensor;

    public override IBinder? OnBind(Intent? intent) => null;

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
    {
        try
        {
            int currentDailySteps = Microsoft.Maui.Storage.Preferences.Default.Get("SavedDailySteps", 0);

            CreateNotificationChannel();
            var notification = BuildNotification($"Steps today: {currentDailySteps:N0}");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake) // API 34+
            {
                StartForeground(NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeHealth);
            }
            else
            {
                StartForeground(NotificationId, notification);
            }

            InitializeSensor();
            return StartCommandResult.Sticky;
        }
        catch (Exception ex)
        {
            Logger.Log("StepForegroundService OnStartCommand failed; stopping service safely", ex);
            try { StopSelf(); } catch { }
            return StartCommandResult.NotSticky;
        }
    }

    private void InitializeSensor()
    {
        _sensorManager = GetSystemService(Context.SensorService) as SensorManager;
        _stepCounterSensor = _sensorManager?.GetDefaultSensor(SensorType.StepCounter);

        if (_stepCounterSensor != null)
        {
            _sensorManager?.UnregisterListener(this);
            _sensorManager?.RegisterListener(this, _stepCounterSensor, SensorDelay.Normal);
        }
    }

    public void OnSensorChanged(SensorEvent? e)
    {
        if (e == null || e.Values == null || e.Values.Count == 0) return;

        int totalHardwareSteps = (int)e.Values[0];
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        string lastSavedDate = Microsoft.Maui.Storage.Preferences.Default.Get("SavedStepDate", string.Empty);

        int dailySteps = Microsoft.Maui.Storage.Preferences.Default.Get("SavedDailySteps", 0);
        int lastHardware = Microsoft.Maui.Storage.Preferences.Default.Get("LastHardwareSteps", -1);

        if (lastSavedDate != today)
        {
            Microsoft.Maui.Storage.Preferences.Default.Set("SavedStepDate", today);
            dailySteps = 0;
            lastHardware = totalHardwareSteps;
        }
        else if (lastHardware == -1)
        {
            lastHardware = totalHardwareSteps;
        }
        else if (totalHardwareSteps < lastHardware)
        {
            dailySteps += totalHardwareSteps;
            lastHardware = totalHardwareSteps;
        }
        else
        {
            int diff = totalHardwareSteps - lastHardware;
            dailySteps += diff;
            lastHardware = totalHardwareSteps;
        }

        Microsoft.Maui.Storage.Preferences.Default.Set("SavedDailySteps", dailySteps);
        Microsoft.Maui.Storage.Preferences.Default.Set($"Steps_{today}", dailySteps);
        Microsoft.Maui.Storage.Preferences.Default.Set("LastHardwareSteps", lastHardware);
        Microsoft.Maui.Storage.Preferences.Default.Set("SavedAccumulativeSteps", totalHardwareSteps);

        UpdateNotification($"Steps today: {dailySteps:N0}");
    }

    public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy) { }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Step Tracking Service", NotificationImportance.Low);
            var manager = GetSystemService(Context.NotificationService) as NotificationManager;
            manager?.CreateNotificationChannel(channel);
        }
    }

    private Notification BuildNotification(string message)
    {
        var intent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
        var pendingIntent = PendingIntent.GetActivity(
            this, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("JumpRope Tracker Active")
            .SetContentText(message)
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuMyCalendar)
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            .Build();
    }

    private void UpdateNotification(string message)
    {
        var manager = GetSystemService(Context.NotificationService) as NotificationManager;
        manager?.Notify(NotificationId, BuildNotification(message));
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _sensorManager?.UnregisterListener(this);
    }
}