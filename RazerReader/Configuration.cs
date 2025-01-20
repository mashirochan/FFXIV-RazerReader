using Dalamud.Configuration;
using System;

namespace RazerReader;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool IsWindowEnabled { get; set; } = true;
    public bool IsWindowMovable { get; set; } = true;
    public int PollingTime { get; set; } = 60;
    public bool UseDalamudBackground { get; set; } = false;
    public int BackgroundOpacity { get; set; } = 100;
    public bool DrawBorder { get; set; } = true;
    public bool ColorPercent { get; set; } = true;
    public bool ShowBatteryIcon { get; set; } = true;
    public bool ShowDtrEntry { get; set; } = false;
    public int SynapseVersion { get; set; } = 4;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
