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

    private const string KeySavedDate = "SavedStepDate";

    public override IBinder? OnBind(Intent? intent) => null;
    string todayKey = $"Steps_{DateTime.Today:yyyy-MM-dd}";
 
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        StartForeground(NotificationId, BuildNotification("Tracking steps..."));

        string today = DateTime.Today.ToString("yyyy-MM-dd");
        string lastSavedDate = Microsoft.Maui.Storage.Preferences.Default.Get(KeySavedDate, string.Empty);

        if (lastSavedDate != today)
        {
            Microsoft.Maui.Storage.Preferences.Default.Set(KeySavedDate, today);
            Microsoft.Maui.Storage.Preferences.Default.Set("SavedDailySteps", 0);

            // Invalidate baseline on a new day so it resets correctly
            Microsoft.Maui.Storage.Preferences.Default.Set("StepBaseline", -1);
        }

        InitializeSensor();
        return StartCommandResult.Sticky;
    }

    private void InitializeSensor()
    {
        _sensorManager = GetSystemService(Context.SensorService) as SensorManager;
        _stepCounterSensor = _sensorManager?.GetDefaultSensor(SensorType.StepCounter);

        if (_stepCounterSensor != null)
        {
            _sensorManager?.RegisterListener(this, _stepCounterSensor, SensorDelay.Normal);
        }
    }

    // Correct Android Sensor Event Listener signature
    public void OnSensorChanged(SensorEvent? e)
    {
        if (e == null || e.Values == null || e.Values.Count == 0) return;

        int totalHardwareSteps = (int)e.Values[0];
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        string lastSavedDate = Microsoft.Maui.Storage.Preferences.Default.Get("SavedStepDate", string.Empty);

        int dailySteps = Microsoft.Maui.Storage.Preferences.Default.Get("SavedDailySteps", 0);
        int lastHardware = Microsoft.Maui.Storage.Preferences.Default.Get("LastHardwareSteps", -1);

        // 1. Handle day rollover (New Day)
        if (lastSavedDate != today)
        {
            Microsoft.Maui.Storage.Preferences.Default.Set("SavedStepDate", today);
            dailySteps = 0;
            lastHardware = totalHardwareSteps;
        }
        // 2. Handle first run initialization
        else if (lastHardware == -1)
        {
            lastHardware = totalHardwareSteps;
        }
        // 3. Handle Phone Reboot / Sensor Reset (Hardware counter dropped to 0 or lower)
        else if (totalHardwareSteps < lastHardware)
        {
            // The phone restarted! The hardware counter reset, but our previous dailySteps 
            // are already safely saved. We add the new steps walked since the reboot.
            dailySteps += totalHardwareSteps;
        }
        // 4. Normal incremental movement
        else
        {
            int diff = totalHardwareSteps - lastHardware;
            dailySteps += diff;
        }

        // Persist everything securely
        Microsoft.Maui.Storage.Preferences.Default.Set("SavedDailySteps", dailySteps);
        Microsoft.Maui.Storage.Preferences.Default.Set($"Steps_{DateTime.Today:yyyy-MM-dd}", dailySteps);
        Microsoft.Maui.Storage.Preferences.Default.Set("LastHardwareSteps", totalHardwareSteps);
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
        var intent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? "");
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