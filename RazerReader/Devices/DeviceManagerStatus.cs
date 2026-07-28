using System;

namespace BatteryBuddy.Devices;

public sealed class DeviceManagerStatus
{
    public bool IsEnabled { get; set; }
    public bool IsRunning { get; set; }
    public bool IsHealthy { get; set; } = true;
    public string? Message { get; set; }
    public DateTime? LastUpdate { get; set; }
}
