using System.Collections.Generic;

namespace BatteryBuddy.Models;

public class DeviceList
{
    public List<Device> Mice { get; set; } = [];
    public List<Device> Keyboards { get; set; } = [];
    public List<Device> Headsets { get; set; } = [];
    public List<Device> All
    {
        get
        {
            var allDevices = new List<Device>();
            allDevices.AddRange(Mice);
            allDevices.AddRange(Keyboards);
            allDevices.AddRange(Headsets);
            return allDevices;
        }
    }

    public Device? LowestDevice { get; set; }

    public bool IsEmpty()
    {
        return Mice.Count == 0 && Keyboards.Count == 0 && Headsets.Count == 0;
    }

    public int Count()
    {
        return Mice.Count + Keyboards.Count + Headsets.Count;
    }

    public void Remove(Device device)
    {
        foreach (var d in Mice) {
            if (d == device) {
                Mice.Remove(device);
                break;
            }
        }
        foreach (var d in Keyboards) {
            if (d == device) {
                Keyboards.Remove(device);
                break;
            }
        }
        foreach (var d in Headsets) {
            if (d == device) {
                Headsets.Remove(device);
                break;
            }
        }
    }
}

public class Device
{
    public required string name { get; set; }
    public required string category { get; set; }
    public required int level { get; set; }
    public bool isCharging { get; set; }
    public float mileage { get; set; }
    public required bool hasBattery { get; set; }
    public bool enabled { get; set; }
    public bool connected { get; set; }
    public DeviceSource source { get; set; }
}

public enum DeviceSource
{   Any,
    Razer,
    Logitech,
    Corsair
}
