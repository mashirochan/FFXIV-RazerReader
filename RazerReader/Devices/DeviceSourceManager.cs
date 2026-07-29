using BatteryBuddy.Devices.DeviceManagers;
using BatteryBuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BatteryBuddy.Devices;

public sealed class DeviceSourceManager
{
    private readonly Dictionary<DeviceSource, IDeviceManager> managers;

    public event Action<IReadOnlyList<Device>>? OnDevicesUpdated;

    public DeviceSourceManager(IEnumerable<IDeviceManager> deviceManagers)
    {
        managers = deviceManagers.ToDictionary(m => m.Source, m => m);
        foreach (var manager in managers.Values)
        {
            manager.OnDevicesUpdated += OnDevicesUpdatedInternal;
        }
    }

    private void OnDevicesUpdatedInternal(IReadOnlyList<Device> devices)
    {
        OnDevicesUpdated?.Invoke(devices);
    }

    public IDeviceManager? GetManager(DeviceSource source) =>
        managers.TryGetValue(source, out var manager) ? manager : null;

    public List<IDeviceManager> GetAllManagers() => managers.Values.ToList();

    public void StartAll()
    {
        foreach (var manager in managers.Values)
        {
            if (manager.Status.IsEnabled)
                manager.Start();
        }
    }

    public void StopAll()
    {
        foreach (var manager in managers.Values)
        {
            manager.Stop();
        }
    }
}
