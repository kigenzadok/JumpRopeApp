using Plugin.Maui.Pedometer;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading.Tasks;

namespace JumpRopeApp.Services;

public class StepTrackerService
{
    private readonly IPedometer _pedometer = Pedometer.Default;

    public bool IsSupported => _pedometer.IsSupported;

    public async Task StartContinuousTrackingAsync(Action<int> onStepCountUpdated)
    {
        try
        {
            if (!_pedometer.IsSupported) return;

            // Safe Main Thread Permission Dispatch
            var status = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var s = await Permissions.CheckStatusAsync<Permissions.Sensors>();
                if (s != PermissionStatus.Granted)
                {
                    s = await Permissions.RequestAsync<Permissions.Sensors>();
                }
                return s;
            });

            if (status != PermissionStatus.Granted) return;

            _pedometer.ReadingChanged += (sender, e) =>
            {
                int count = e.NumberOfSteps;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    onStepCountUpdated?.Invoke(count);
                });
            };

            _pedometer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StepTracker Exception: {ex.Message}");
        }
    }

    public void StopTracking()
    {
        try
        {
            if (_pedometer.IsSupported)
            {
                _pedometer.Stop();
            }
        }
        catch { }
    }
}