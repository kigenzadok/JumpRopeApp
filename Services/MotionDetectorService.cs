using Microsoft.Maui.Devices.Sensors;
using System;

namespace JumpRopeApp.Services;

public class MotionDetectorService
{
    private bool _isTracking;
    private DateTime _lastJumpTime = DateTime.MinValue;

    // Sensitivity threshold (Adjust between 1.8 and 2.5 depending on placement)
    private const double JumpThresholdG = 2.0;
    private const int DebounceMs = 250; // Minimum time between jumps

    public event Action? OnJumpDetected;

    public void StartTracking()
    {
        if (_isTracking || !Accelerometer.Default.IsSupported) return;

        Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
        Accelerometer.Default.Start(SensorSpeed.UI);
        _isTracking = true;
    }

    public void StopTracking()
    {
        if (!_isTracking || !Accelerometer.Default.IsSupported) return;

        Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
        Accelerometer.Default.Stop();
        _isTracking = false;
    }

    private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var data = e.Reading.Acceleration;

        // Calculate total G-force magnitude: sqrt(x^2 + y^2 + z^2)
        double totalG = Math.Sqrt(data.X * data.X + data.Y * data.Y + data.Z * data.Z);

        // Detect jump peak
        if (totalG > JumpThresholdG)
        {
            if ((DateTime.Now - _lastJumpTime).TotalMilliseconds > DebounceMs)
            {
                _lastJumpTime = DateTime.Now;

                // Notify subscribers (MainPage / Active Workout UI) on Main Thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnJumpDetected?.Invoke();
                });
            }
        }
    }
}