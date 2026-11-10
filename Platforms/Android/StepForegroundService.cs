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

    private int _rawInitialSensorSteps = -1;
    private const string KeyInitialRawSteps = "InitialRawSteps";
    private const string KeySavedDate = "SavedStepDate";
    private const string KeyLifetimeOffset = "LifetimeStepOffset";

    public override IBinder? OnBind(Intent? intent) => null;

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
            _rawInitialSensorSteps = -1;
        }
        else
        {
            _rawInitialSensorSteps = Microsoft.Maui.Storage.Preferences.Default.Get(KeyInitialRawSteps, -1);
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

    public void OnSensorChanged(SensorEvent? e)
    {
        if (e?.Sensor?.Type == SensorType.StepCounter && e.Values != null && e.Values.Count > 0)
        {
            int currentRawHardwareSteps = (int)e.Values[0];
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            string savedDate = Microsoft.Maui.Storage.Preferences.Default.Get(KeySavedDate, string.Empty);

            if (savedDate != today || _rawInitialSensorSteps == -1)
            {
                _rawInitialSensorSteps = currentRawHardwareSteps;
                Microsoft.Maui.Storage.Preferences.Default.Set(KeySavedDate, today);
                Microsoft.Maui.Storage.Preferences.Default.Set(KeyInitialRawSteps, _rawInitialSensorSteps);
                Microsoft.Maui.Storage.Preferences.Default.Set("SavedDailySteps", 0);
            }

            int dailySteps = Math.Max(0, currentRawHardwareSteps - _rawInitialSensorSteps);

            // Compute accumulative lifetime steps delta
            int lastRaw = Microsoft.Maui.Storage.Preferences.Default.Get("LastRawSensorSteps", currentRawHardwareSteps);
            int stepDelta = Math.Max(0, currentRawHardwareSteps - lastRaw);
            if (currentRawHardwareSteps < lastRaw) stepDelta = currentRawHardwareSteps; // Device rebooted

            Microsoft.Maui.Storage.Preferences.Default.Set("LastRawSensorSteps", currentRawHardwareSteps);

            int savedAccumulative = Microsoft.Maui.Storage.Preferences.Default.Get("SavedAccumulativeSteps", 0);
            int accumulativeSteps = savedAccumulative + stepDelta;

            Microsoft.Maui.Storage.Preferences.Default.Set("SavedAccumulativeSteps", accumulativeSteps);
            Microsoft.Maui.Storage.Preferences.Default.Set("SavedDailySteps", dailySteps);

            App.NotifyStepUpdate(dailySteps, accumulativeSteps);

            UpdateNotification($"Today: {dailySteps:N0} | Total: {accumulativeSteps:N0}");
        }
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