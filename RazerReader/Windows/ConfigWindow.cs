using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RazerReader.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration config;
    private readonly Plugin plugin;
    private readonly ServerBar serverBar;

    public ConfigWindow(Plugin plugin, ServerBar serverBar) : base("Razer Reader Config##razerreader_config")
    {
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 90);
        SizeCondition = ImGuiCond.Once;

        this.plugin = plugin;
        config = plugin.Configuration;
        this.serverBar = serverBar;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var windowEnabled = config.IsWindowEnabled;
        if (ImGui.Checkbox("Window Enabled", ref windowEnabled))
        {
            config.IsWindowEnabled = windowEnabled;
            config.Save();
        }

        ImGui.Spacing();

        var movable = config.IsWindowMovable;
        if (ImGui.Checkbox("Movable Window", ref movable))
        {
            config.IsWindowMovable = movable;
            config.Save();
        }

        ImGui.Spacing();

        var useDalamudBackground = config.UseDalamudBackground;
        if (ImGui.Checkbox("Use Dalamud BG Style", ref useDalamudBackground))
        {
            config.UseDalamudBackground = useDalamudBackground;
            Plugin.Log.Debug($"UseDalamudBackground set to: {(useDalamudBackground ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        ImGui.PushItemWidth(100);
        ImGui.BeginDisabled(config.UseDalamudBackground);

        var backgroundOpacity = config.BackgroundOpacity;
        if (ImGui.SliderInt("BG Opacity", ref backgroundOpacity, 0, 100))
        {
            config.BackgroundOpacity = backgroundOpacity;
            Plugin.Log.Debug($"BackgroundOpacity set to: {backgroundOpacity}");
            config.Save();
        }

        ImGui.PopItemWidth();
        ImGui.EndDisabled();

        ImGui.Spacing();

        var drawBorder = config.DrawBorder;
        if (ImGui.Checkbox("Window Border", ref drawBorder))
        {
            config.DrawBorder = drawBorder;
            Plugin.Log.Debug($"DrawBorder set to: {(drawBorder ? "True" : "False")}");
            config.Save();
        }
        
        ImGui.Spacing();

        var colorPercent = config.ColorPercent;
        if (ImGui.Checkbox("Color Percent", ref colorPercent))
        {
            config.ColorPercent = colorPercent;
            Plugin.Log.Debug($"ColorPercent set to: {(colorPercent ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        var showBatteryIcon = config.ShowBatteryIcon;
        if (ImGui.Checkbox("Show Battery Icon", ref showBatteryIcon))
        {
            config.ShowBatteryIcon = showBatteryIcon;
            Plugin.Log.Debug($"ShowBatteryIcon set to: {(showBatteryIcon ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        var showDtrEntry = config.ShowDtrEntry;
        if (ImGui.Checkbox("Show DTR Entry", ref showDtrEntry))
        {
            config.ShowDtrEntry = showDtrEntry;
            Plugin.Log.Debug($"ShowDtrEntry set to: {(showDtrEntry ? "True" : "False")}");
            serverBar.UpdateDtrBar();
            config.Save();
        }

        ImGui.Spacing();

        string[] synapseOptions = ["v3", "v4"];
        var synapseVersion = config.SynapseVersion;
        var synapseIndex = synapseVersion == 3 ? 0 : 1;
        ImGui.SetNextItemWidth(50);
        if (ImGui.BeginCombo("##SynapseVersion", synapseOptions[synapseIndex]))
        {
            for (var i = 0; i < synapseOptions.Length; i++)
            {
                var isSelected = (synapseIndex == i);
                if (ImGui.Selectable(synapseOptions[i], isSelected))
                {
                    synapseVersion = (i == 0) ? 3 : 4;
                    config.SynapseVersion = synapseVersion;
                    Plugin.Log.Debug($"SynapseVersion set to: {synapseVersion}");
                    config.Save();
                    plugin.PollFileForChanges(true);
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();

        ImGui.TextUnformatted("Synapse Version");
    }
}
