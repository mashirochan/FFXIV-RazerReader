using Dalamud.Configuration;
using BatteryBuddy.Models;
using System;
using System.Numerics;

namespace BatteryBuddy;

[Serializable]
public class Configuration : IPluginConfiguration
{
    // General
    public int Version { get; set; } = 0;
    public bool IsWindowEnabled { get; set; } = true;
    public bool IsWindowMovable { get; set; } = true;
    public Vector2 WindowPosition { get; set; } = new Vector2(100, 800);
    public bool IsBottomAnchored { get; set; } = true;
    public int PollingTime { get; set; } = 60;
    public bool UseDalamudBackground { get; set; } = false;
    public int BackgroundOpacity { get; set; } = 100;
    public bool DrawBorder { get; set; } = true;
    public bool ColorPercent { get; set; } = true;
    public bool ShowBatteryIcon { get; set; } = true;
    public bool ShowDeviceIcon { get; set; } = true;
    public bool ShowDeviceName { get; set; } = true;
    public bool ShowRemainingTime { get; set; } = true;
    public bool ShowDtrEntry { get; set; } = false;
    public bool LowBatteryNotification { get; set; } = true;
    public int LowBatteryLevel { get; set; } = 15;
    public int LowBatteryNotificationTimer { get; set; } = 10;
    public DeviceList DeviceList { get; set; } = new DeviceList();

    // Razer
    public bool RazerEnabled { get; set; } = false;

    // Logitech
    public bool LogitechEnabled { get; set; } = false;

    // Corsair
    public string CorsairSdkPath { get; set; } = string.Empty;

    // Corsair
    public bool CorsairEnabled { get; set; } = false;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
