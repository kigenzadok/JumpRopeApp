using Microsoft.Maui.Devices.Sensors;

namespace JumpRopeApp.Services;

public class JumpDetectorService
{
    private bool _isTracking;
    private DateTime _lastJumpTime = DateTime.MinValue;

    // Thresholds tuned for pocket acceleration dynamics
    private const double JumpThreshold = 1.6;     // Acceleration force peak threshold (~1.6g)
    private const int MinJumpIntervalMs = 280;     // Cooldown period between jumps (~214 RPM max limit)

    public event EventHandler? JumpDetected;

    public void StartTracking()
    {
        if (Accelerometer.Default.IsSupported && !_isTracking)
        {
            Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
            Accelerometer.Default.Start(SensorSpeed.Game);
            _isTracking = true;
        }
    }

    public void StopTracking()
    {
        if (_isTracking)
        {
            Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
            Accelerometer.Default.Stop();
            _isTracking = false;
        }
    }

    private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var data = e.Reading;

        // Calculate vector magnitude from raw 3D accelerometer axis coordinates
        double magnitude = Math.Sqrt(data.Acceleration.X * data.Acceleration.X +
                                     data.Acceleration.Y * data.Acceleration.Y +
                                     data.Acceleration.Z * data.Acceleration.Z);

        // Detect peak force exceeding baseline gravity with cooldown debouncing
        if (magnitude > JumpThreshold)
        {
            if ((DateTime.Now - _lastJumpTime).TotalMilliseconds > MinJumpIntervalMs)
            {
                _lastJumpTime = DateTime.Now;

                // Dispatch event safely to UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    JumpDetected?.Invoke(this, EventArgs.Empty);
                });
            }
        }
    }
}