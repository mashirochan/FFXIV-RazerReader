using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using RazerReader.Models;
using static System.Net.Mime.MediaTypeNames;

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

        var lowBatteryNotification = config.LowBatteryNotification;
        if (ImGui.Checkbox("Low Battery Notification", ref lowBatteryNotification))
        {
            config.LowBatteryNotification = lowBatteryNotification;
            Plugin.Log.Debug($"LowBatteryNotification set to: {(lowBatteryNotification ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        ImGui.PushItemWidth(100);
        ImGui.BeginDisabled(!config.LowBatteryNotification);

        var lowBatteryLevel = config.LowBatteryLevel;
        if (ImGui.SliderInt("Low Battery %", ref lowBatteryLevel, 0, 100))
        {
            config.LowBatteryLevel = lowBatteryLevel;
            Plugin.Log.Debug($"LowBatteryLevel set to: {lowBatteryLevel}");
            config.Save();
        }

        ImGui.PopItemWidth();
        ImGui.EndDisabled();

        ImGui.Spacing();

        using (ImRaii.Table("##deviceTable", 3))
        {
            ImGui.TableSetupColumn("Show", ImGuiTableColumnFlags.None, 50 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Bat. %", ImGuiTableColumnFlags.None, 50 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Device Name", ImGuiTableColumnFlags.None, 200 * ImGuiHelpers.GlobalScale);
            ImGui.TableHeadersRow();

            foreach (var mouse in plugin.Configuration.DeviceList.Mice)
            {
                DrawDevice(mouse);
            }
            foreach (var keyboard in plugin.Configuration.DeviceList.Keyboards)
            {
                DrawDevice(keyboard);
            }
            foreach (var headset in plugin.Configuration.DeviceList.Headsets)
            {
                DrawDevice(headset);
            }
        }
    }

    private void DrawDevice(Device device)
    {
        ImGui.TableNextColumn();
        var isChecked = device.enabled;
        if (ImGui.Checkbox($"##show_{device.name}", ref isChecked))
        {
            device.enabled = isChecked;
            Plugin.Log.Debug($"{device.name} is set to: {(isChecked ? "Show" : "Hide")}");
            config.Save();
        }

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{device.level}%");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(device.name);
    }
}
