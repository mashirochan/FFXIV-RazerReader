using BatteryBuddy.Models;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Lua;
using Serilog;
using System;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace BatteryBuddy.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration config;
    private readonly Plugin plugin;
    private readonly IPluginLog log;
    private readonly ServerBar serverBar;

    public ConfigWindow(Plugin plugin, IPluginLog log, ServerBar serverBar) : base("Battery Buddy Config##batterybuddy_config")
    {
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 90);
        SizeCondition = ImGuiCond.Once;

        this.plugin = plugin;
        this.log = log;
        config = plugin.Configuration;
        this.serverBar = serverBar;
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("##batterybuddy_settingstabs");

        if (!tabBar.Success)
            return;

        DrawGeneralTab();
        DrawRazerTab();
        DrawLogitechTab();
    }

    private void DrawGeneralTab()
    {
        using var tabItem = ImRaii.TabItem("General");

        if (!tabItem.Success)
            return;

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

        var bottomAnchored = config.IsBottomAnchored;
        if (ImGui.Checkbox("Align to Bottom", ref bottomAnchored))
        {
            config.IsBottomAnchored = bottomAnchored;
            config.Save();
        }

        ImGui.Spacing();

        var useDalamudBackground = config.UseDalamudBackground;
        if (ImGui.Checkbox("Use Dalamud BG Style", ref useDalamudBackground))
        {
            config.UseDalamudBackground = useDalamudBackground;
            log.Debug($"UseDalamudBackground set to: {(useDalamudBackground ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        ImGui.PushItemWidth(100);
        ImGui.BeginDisabled(config.UseDalamudBackground);

        var backgroundOpacity = config.BackgroundOpacity;
        if (ImGui.SliderInt("BG Opacity", ref backgroundOpacity, 0, 100))
        {
            config.BackgroundOpacity = backgroundOpacity;
            log.Debug($"BackgroundOpacity set to: {backgroundOpacity}");
            config.Save();
        }

        ImGui.PopItemWidth();
        ImGui.EndDisabled();

        ImGui.Spacing();

        var drawBorder = config.DrawBorder;
        if (ImGui.Checkbox("Window Border", ref drawBorder))
        {
            config.DrawBorder = drawBorder;
            log.Debug($"DrawBorder set to: {(drawBorder ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        var colorPercent = config.ColorPercent;
        if (ImGui.Checkbox("Color Percent", ref colorPercent))
        {
            config.ColorPercent = colorPercent;
            log.Debug($"ColorPercent set to: {(colorPercent ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        var showBatteryIcon = config.ShowBatteryIcon;
        if (ImGui.Checkbox("Show Battery Icon", ref showBatteryIcon))
        {
            config.ShowBatteryIcon = showBatteryIcon;
            log.Debug($"ShowBatteryIcon set to: {(showBatteryIcon ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        var showDeviceIcon = config.ShowDeviceIcon;
        if (ImGui.Checkbox("Show Device Icon", ref showDeviceIcon))
        {
            config.ShowDeviceIcon = showDeviceIcon;
            log.Debug($"ShowDeviceIcon set to: {(showDeviceIcon ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        var showDeviceName = config.ShowDeviceName;
        if (ImGui.Checkbox("Show Device Name", ref showDeviceName))
        {
            config.ShowDeviceName = showDeviceName;
            log.Debug($"ShowDeviceName set to: {(showDeviceName ? "True" : "False")}");
            serverBar.UpdateDtrBar();
            config.Save();
        }

        ImGui.Spacing();

        var showRemainingTime = config.ShowRemainingTime;
        if (ImGui.Checkbox("Show Remaining Time Tooltip", ref showRemainingTime))
        {
            config.ShowRemainingTime = showRemainingTime;
            log.Debug($"ShowRemainingTime set to: {(showRemainingTime ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        var showDtrEntry = config.ShowDtrEntry;
        if (ImGui.Checkbox("Show DTR Entry", ref showDtrEntry))
        {
            config.ShowDtrEntry = showDtrEntry;
            log.Debug($"ShowDtrEntry set to: {(showDtrEntry ? "True" : "False")}");
            serverBar.UpdateDtrBar();
            config.Save();
        }

        ImGui.Spacing();

        var lowBatteryNotification = config.LowBatteryNotification;
        if (ImGui.Checkbox("Low Battery Notification", ref lowBatteryNotification))
        {
            config.LowBatteryNotification = lowBatteryNotification;
            log.Debug($"LowBatteryNotification set to: {(lowBatteryNotification ? "True" : "False")}");
            config.Save();
        }

        ImGui.Spacing();

        ImGui.PushItemWidth(100);
        ImGui.BeginDisabled(!config.LowBatteryNotification);

        var lowBatteryLevel = config.LowBatteryLevel;
        if (ImGui.SliderInt("Low Battery %", ref lowBatteryLevel, 0, 100))
        {
            config.LowBatteryLevel = lowBatteryLevel;
            log.Debug($"LowBatteryLevel set to: {lowBatteryLevel}");
            config.Save();
        }

        ImGui.PopItemWidth();

        ImGui.PushItemWidth(100);
        var lowBatteryNotificationTimer = config.LowBatteryNotificationTimer;
        if (ImGui.SliderInt("Low Battery Notification Timer (minutes)", ref lowBatteryNotificationTimer, 1, 60))
        {
            config.LowBatteryNotificationTimer = lowBatteryNotificationTimer;
            log.Debug($"LowBatteryNotificationTimer set to: {lowBatteryNotificationTimer}");

            plugin.lowBatteryTimer.Stop();
            plugin.lowBatteryTimer.Interval = lowBatteryNotificationTimer * 1000;
            plugin.lowBatteryTimer.Start();
            log.Info($"Low battery notification timer started with interval: {config.LowBatteryNotificationTimer} seconds.");

            config.Save();
        }
        ImGui.PopItemWidth();

        ImGui.EndDisabled();

        ImGui.Spacing();

        DrawDeviceTable(DeviceSource.Any);
    }

    private void DrawRazerTab()
    {
        using var tabItem = ImRaii.TabItem("Razer");

        if (!tabItem.Success)
            return;

        var manager = plugin.DeviceSourceManager.GetManager(DeviceSource.Razer);

        var razerEnabled = config.RazerEnabled;
        if (ImGui.Checkbox("Enable Razer Devices", ref razerEnabled))
        {
            config.RazerEnabled = razerEnabled;
            config.Save();

            if (manager != null)
            {
                var status = manager.Status;
                manager.Status.IsEnabled = razerEnabled;

                if (status.IsRunning && !razerEnabled)
                {
                    manager.Stop();
                }
                else if (!status.IsRunning && razerEnabled)
                {
                    manager.Start();
                }
            }
        }

        ImGui.BeginDisabled(!razerEnabled);

        if (manager != null)
        {
            var status = manager.Status;

            using (ImRaii.PushColor(ImGuiCol.Text, status.IsRunning ? KnownColor.Red.Vector() : KnownColor.Green.Vector()))
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PowerOff, $"{(status.IsRunning ? "Stop" : "Start")} Razer Manager"))
                {
                    if (status.IsRunning)
                        manager.Stop();
                    else
                        manager.Start();
                }
            }

            ImGui.TextUnformatted("Running:");
            ImGui.SameLine();
            ImGui.TextColored(status.IsRunning ? KnownColor.Green.Vector() : KnownColor.Red.Vector(), (status.IsRunning ? "Yes" : "No").PadRight(4));

            ImGui.SameLine();

            ImGui.TextUnformatted("Healthy:");
            ImGui.SameLine();
            ImGui.TextColored(status.IsHealthy ? KnownColor.Green.Vector() : KnownColor.Red.Vector(), (status.IsHealthy ? "Yes" : "No").PadRight(4));

            if (!string.IsNullOrEmpty(status.Message))
                ImGui.TextUnformatted(status.Message);

            if (status.LastUpdate.HasValue)
                ImGui.TextUnformatted($"Last update: {status.LastUpdate?.ToLocalTime():G}");
        }

        DrawDeviceTable(DeviceSource.Razer);

        ImGui.EndDisabled();
    }

    private void DrawLogitechTab()
    {
        using var tabItem = ImRaii.TabItem("Logitech");

        if (!tabItem.Success)
            return;

        var manager = plugin.DeviceSourceManager.GetManager(DeviceSource.Logitech);

        var logitechEnabled = config.LogitechEnabled;
        if (ImGui.Checkbox("Enable Logitech Devices", ref logitechEnabled))
        {
            config.LogitechEnabled = logitechEnabled;
            config.Save();

            if (manager != null)
            {
                var status = manager.Status;
                manager.Status.IsEnabled = logitechEnabled;

                if (status.IsRunning && !logitechEnabled)
                {
                    manager.Stop();
                }
                else if (!status.IsRunning && logitechEnabled)
                {
                    manager.Start();
                }
            }
        }

        ImGui.BeginDisabled(!logitechEnabled);

        if (manager != null)
        {
            var status = manager.Status;

            using (ImRaii.PushColor(ImGuiCol.Text, status.IsRunning ? KnownColor.Red.Vector() : KnownColor.Green.Vector()))
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PowerOff, $"{(status.IsRunning ? "Stop" : "Start")} Logitech Manager"))
                {
                    if (status.IsRunning)
                        manager.Stop();
                    else
                        manager.Start();
                }
            }

            ImGui.TextUnformatted("Running:");
            ImGui.SameLine();
            ImGui.TextColored(status.IsRunning ? KnownColor.Green.Vector() : KnownColor.Red.Vector(), (status.IsRunning ? "Yes" : "No").PadRight(4));

            ImGui.SameLine();

            ImGui.TextUnformatted("Healthy:");
            ImGui.SameLine();
            ImGui.TextColored(status.IsHealthy ? KnownColor.Green.Vector() : KnownColor.Red.Vector(), (status.IsHealthy ? "Yes" : "No").PadRight(4));

            if (!string.IsNullOrEmpty(status.Message))
                ImGui.TextUnformatted(status.Message);

            if (status.LastUpdate.HasValue)
                ImGui.TextUnformatted($"Last update: {status.LastUpdate?.ToLocalTime():G}");
        }

        DrawDeviceTable(DeviceSource.Logitech);

        ImGui.EndDisabled();
    }

    private void DrawDeviceTable(DeviceSource source)
    {
        using (ImRaii.Table("##deviceTable", 4))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Show", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Bat. %", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Device Name", ImGuiTableColumnFlags.WidthFixed, 300 * ImGuiHelpers.GlobalScale);
            ImGui.TableHeadersRow();

            foreach (var device in plugin.Configuration.DeviceList.All
                .Where(d => (source == DeviceSource.Any || d.source == source) && plugin?.DeviceSourceManager?.GetManager(d.source)?.Status.IsEnabled == true))
            {
                DrawDevice(device);
            }
        }
    }

    private void DrawDevice(Device device)
    {
        ImGui.TableNextColumn();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button(FontAwesomeIcon.TrashAlt.ToIconString() + "##delete_" + device.name))
            {
                ImGui.OpenPopup($"delete_confirmation_{device.name}");
            }
        }

        if (ImGui.BeginPopup($"delete_confirmation_{device.name}"))
        {
            ImGui.Text($"Remove {device.name}?");

            ImGui.AlignTextToFramePadding();
            if (ImGui.Button("Yes" + $"##yes_{device.name}"))
            {
                plugin.Configuration.DeviceList.Remove(device);
                config.Save();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            ImGui.AlignTextToFramePadding();
            if (ImGui.Button("No" + $"##no_{device.name}"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        ImGui.TableNextColumn();
        var isChecked = device.enabled;
        if (ImGui.Checkbox($"##show_{device.name}", ref isChecked))
        {
            device.enabled = isChecked;
            log.Debug($"{device.name} is set to: {(isChecked ? "Show" : "Hide")}");
            config.Save();
        }

        ImGui.TableNextColumn();
        var levelColor = config.ColorPercent == false ? KnownColor.White.Vector() : Plugin.GetLevelColor(device.level);
        ImGui.TextColored(levelColor, $"{device.level}%");

        ImGui.TableNextColumn();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Text(Plugin.GetDeviceIcon(device.category));
        }
        ImGui.SameLine();
        if (device.connected)
        {
            ImGui.TextUnformatted(device.name);
        }
        else
        {
            ImGui.TextColored(KnownColor.Red.Vector(), $"{device.name}");
        }
    }
}
