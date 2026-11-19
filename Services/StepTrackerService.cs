using Microsoft.Maui.Devices.Sensors;
using System;

namespace JumpRopeApp.Services;

public class StepTrackerService
{
    private bool _isTracking;
    private DateTime _lastStepTime = DateTime.MinValue;

    // --- ACCELEROMETER FINE-TUNING CONSTANTS ---
    private const double MinStepG = 1.25;       // Minimum G-force peak for a walk step
    private const double MaxStepG = 1.85;       // Maximum G-force (ignores strong phone shakes)
    private const int MinStepIntervalMs = 320;  // Minimum time between valid steps (~180 steps/min max)
    private const int MaxStepIntervalMs = 1200; // Maximum time allowed between steps in a walk cycle
    private const int ConsecutiveStepBufferRequirement = 4; // Ignore single shakes/fidgets until 4 real steps happen

    private int _bufferedSteps = 0;
    private DateTime _lastBufferedStepTime = DateTime.MinValue;

    public bool IsSupported => Accelerometer.Default.IsSupported;

    public Task StartContinuousTrackingAsync(Action<int> onStepCountUpdated)
    {
        if (_isTracking || !IsSupported) return Task.CompletedTask;

        Accelerometer.Default.ReadingChanged += (s, e) =>
        {
            var data = e.Reading.Acceleration;

            // Calculate total G-force magnitude: sqrt(X^2 + Y^2 + Z^2)
            double totalG = Math.Sqrt(data.X * data.X + data.Y * data.Y + data.Z * data.Z);
            DateTime now = DateTime.Now;
            double msSinceLastStep = (now - _lastStepTime).TotalMilliseconds;

            // 1. FILTER OUT NOISE: G-force must fall in typical human walking bounds
            if (totalG >= MinStepG && totalG <= MaxStepG && msSinceLastStep >= MinStepIntervalMs)
            {
                double msSinceBufferedStep = (now - _lastBufferedStepTime).TotalMilliseconds;

                // 2. CHECK RHYTHM: If time between movements is too long, reset buffer (it was just a single shake)
                if (msSinceBufferedStep > MaxStepIntervalMs)
                {
                    _bufferedSteps = 0;
                }

                _bufferedSteps++;
                _lastBufferedStepTime = now;
                _lastStepTime = now;

                // 3. BUFFER THRESHOLD: Only count as real walking if 4+ rhythmic steps happen continuously
                if (_bufferedSteps >= ConsecutiveStepBufferRequirement)
                {
                    int stepsToAdd = (_bufferedSteps == ConsecutiveStepBufferRequirement) ? ConsecutiveStepBufferRequirement : 1;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        onStepCountUpdated?.Invoke(App.DailyStepCount + stepsToAdd);
                    });
                }
            }
        };

        Accelerometer.Default.Start(SensorSpeed.UI);
        _isTracking = true;
        return Task.CompletedTask;
    }

    public void StopTracking()
    {
        if (_isTracking && IsSupported)
        {
            Accelerometer.Default.Stop();
            _isTracking = false;
        }
    }
}