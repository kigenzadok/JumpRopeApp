using Microsoft.Maui.Devices.Sensors;
using System;

namespace JumpRopeApp.Services;

public class MotionDetectorService
{
    public event Action? OnJumpDetected;
    private bool _isTracking = false;
    private DateTime _lastJumpTime = DateTime.MinValue;

    public void StartTracking()
    {
        if (_isTracking) return;

        try
        {
            if (Accelerometer.Default.IsSupported)
            {
                Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                Accelerometer.Default.Start(SensorSpeed.UI);
                _isTracking = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start accelerometer: {ex.Message}");
        }
    }

    public void StopTracking()
    {
        if (!_isTracking) return;

        try
        {
            if (Accelerometer.Default.IsSupported)
            {
                Accelerometer.Default.Stop();
                Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
                _isTracking = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to stop accelerometer: {ex.Message}");
        }
    }

    private void Accelerometer_ReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var data = e.Reading;

        // Calculate the total G-force magnitude from the 3 axes (X, Y, Z)
        double magnitude = Math.Sqrt(
            (data.Acceleration.X * data.Acceleration.X) +
            (data.Acceleration.Y * data.Acceleration.Y) +
            (data.Acceleration.Z * data.Acceleration.Z));

        // When jumping rope, acceleration spikes past standard gravity (~1.0)
        // Threshold set to 1.8g with a 250ms debounce window to prevent double-counting per jump
        if (magnitude > 1.8)
        {
            var now = DateTime.Now;
            if ((now - _lastJumpTime).TotalMilliseconds > 250)
            {
                _lastJumpTime = now;
                OnJumpDetected?.Invoke();
            }
        }
    }
}