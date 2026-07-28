using BatteryBuddy.Models;
using System;
using System.Collections.Generic;

namespace BatteryBuddy.Devices.DeviceManagers;

public interface IDeviceManager
{
    DeviceSource Source { get; }
    DeviceManagerStatus Status { get; }
    IReadOnlyList<Device> CurrentDevices { get; }
    event Action<IReadOnlyList<Device>>? OnDevicesUpdated;
    void Start();
    void Stop();
    void LogError(string errorMessage);
}
