using BatteryBuddy.Models;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Drawing;
using System.Numerics;

namespace BatteryBuddy.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration config;
    private readonly ConfigWindow configWindow;
    private readonly ImRaii.StyleDisposable preDrawStyles = new();

    public MainWindow(Plugin plugin, ConfigWindow configWindow)
        : base("Battery Buddy##batterybuddy_main",
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.AlwaysAutoResize)
    {
        RespectCloseHotkey = false;
        DisableWindowSounds = true;

        this.plugin = plugin;
        this.configWindow = configWindow;
        config = this.plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        if (!config.UseDalamudBackground)
        {
            var newOpacity = config.BackgroundOpacity / 100f;
            ImGui.SetNextWindowBgAlpha(newOpacity);
        }

        preDrawStyles.Push(ImGuiStyleVar.WindowBorderSize, config.DrawBorder ? 1 : 0);

        if (!config.IsWindowMovable)
        {
            Flags |= ImGuiWindowFlags.NoMove;
            Flags |= ImGuiWindowFlags.NoMouseInputs;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
            Flags &= ~ImGuiWindowFlags.NoMouseInputs;
        }
    }

    public override void Draw()
    {
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();

        preDrawStyles.Dispose();

        if (config.IsWindowMovable && configWindow.IsOpen)
        {
            var drawList = ImGui.GetForegroundDrawList();

            var outlineColor = ImGui.GetColorU32(new Vector4(255f / 255f, 98f / 255f, 98f / 255f, 0.75f));
            var thickness = 1.0f;

            if (config.IsBottomAnchored)
            {
                config.WindowPosition = new Vector2(windowPos.X, windowPos.Y + windowSize.Y);

                var bottomLeft = new Vector2(windowPos.X, windowPos.Y + windowSize.Y);
                var bottomRight = new Vector2(windowPos.X + windowSize.X, windowPos.Y + windowSize.Y);
                drawList.AddLine(bottomLeft, bottomRight, outlineColor, thickness);
            }
            else
            {
                config.WindowPosition = windowPos;

                var topLeft = new Vector2(windowPos.X, windowPos.Y);
                var topRight = new Vector2(windowPos.X + windowSize.X, windowPos.Y);
                drawList.AddLine(topLeft, topRight, outlineColor, thickness);
            }
        }

        foreach (var mouse in config.DeviceList.Mice)
        {
            DrawDevice(mouse);
        }
        foreach (var keyboard in config.DeviceList.Keyboards)
        {
            DrawDevice(keyboard);
        }
        foreach (var headset in config.DeviceList.Headsets)
        {
            DrawDevice(headset);
        }

        if (config.IsBottomAnchored)
        {
            ImGui.SetWindowPos(new Vector2(config.WindowPosition.X, config.WindowPosition.Y - windowSize.Y));
        }
        else
        {
            ImGui.SetWindowPos(config.WindowPosition);
        }
    }

    private void DrawDevice(Device device)
    {
        if (!device.enabled || !device.connected || plugin?.DeviceSourceManager?.GetManager(device.source)?.Status.IsEnabled == false)
            return;

        ImGui.BeginGroup();

        var levelColor = config.ColorPercent == false ? KnownColor.White.Vector() : Plugin.GetLevelColor(device.level);

        if (config.ShowBatteryIcon)
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.TextColoredWrapped(levelColor, GetBatteryIcon(device));
            }
            ImGui.SameLine();
        }

        // TO-DO: Actually align battery levels...
        ImGui.TextColored(levelColor, $"{(device.level.ToString().Length == 1 ? "   " : "")}{device.level}%");

        if (config.ShowDeviceName || config.ShowDeviceIcon)
        {
            ImGui.SameLine();
            using (ImRaii.PushIndent(1))
            {
                ImGui.SameLine();

                if (config.ShowDeviceIcon)
                {
                    using (ImRaii.PushFont(UiBuilder.IconFont))
                    {
                        ImGui.Text(Plugin.GetDeviceIcon(device.category));
                    }
                    ImGui.SameLine();
                }

                if (config.ShowDeviceName)
                {
                    ImGui.TextUnformatted(device.name);
                }
            }
        }

        ImGui.EndGroup();

        if (config.ShowRemainingTime && ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax()))
        {
            using var tooltip = ImRaii.Tooltip();

            if (config.ShowDeviceIcon)
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGui.Text(Plugin.GetDeviceIcon(device.category));
                }
                ImGui.SameLine();
            }
            ImGui.Text(device.name);

            if (device.mileage > 0)
            {
                ImGui.Text($"{device.mileage:F1} hours remaining" + (device.isCharging ? " (Charging)" : ""));
            }
        }

        ImGui.Spacing();
    }

    private static string GetBatteryIcon(Device device)
    {
        if (device.isCharging)
        {
            return FontAwesomeIcon.Bolt.ToIconString();
        }
        else if (device.level > 80)
        {
            return FontAwesomeIcon.BatteryFull.ToIconString();
        }
        else if (device.level > 50)
        {
            return FontAwesomeIcon.BatteryThreeQuarters.ToIconString();
        }
        else if (device.level > 30)
        {
            return FontAwesomeIcon.BatteryHalf.ToIconString();
        }
        else if (device.level > 15)
        {
            return FontAwesomeIcon.BatteryQuarter.ToIconString();
        }
        else
        {
            return FontAwesomeIcon.BatteryEmpty.ToIconString();
        }
    }
}
