using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using JumpRopeApp;
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
            // New day detected! Reset daily count baseline
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
            int currentRawSteps = (int)e.Values[0];

            if (_rawInitialSensorSteps == -1)
            {
                _rawInitialSensorSteps = currentRawSteps;
                Microsoft.Maui.Storage.Preferences.Default.Set(KeyInitialRawSteps, currentRawSteps);
            }

            // Calculate steps taken today since midnight baseline
            int dailySteps = Math.Max(0, currentRawSteps - _rawInitialSensorSteps);

            // Calculate total lifetime/accumulative steps across days
            int lifetimeBase = Microsoft.Maui.Storage.Preferences.Default.Get(KeyLifetimeOffset, 0);
            int accumulativeSteps = lifetimeBase + dailySteps;

            // Save values
            Microsoft.Maui.Storage.Preferences.Default.Set("SavedDailySteps", dailySteps);
            Microsoft.Maui.Storage.Preferences.Default.Set("SavedAccumulativeSteps", accumulativeSteps);

            // Notify app UI
            App.NotifyStepUpdate(dailySteps);

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