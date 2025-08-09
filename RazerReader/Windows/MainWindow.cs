using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RazerReader.Models;
using System;
using System.Drawing;
using System.Numerics;

namespace RazerReader.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration config;
    private readonly ConfigWindow configWindow;
    private readonly ImRaii.Style preDrawStyles = new();

    public MainWindow(Plugin plugin, ConfigWindow configWindow)
        : base("Razer Reader##razerreader_main",
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

        if (config.IsWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
            Flags &= ~ImGuiWindowFlags.NoMouseInputs;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
            Flags |= ImGuiWindowFlags.NoMouseInputs;
        }
    }

    public override void Draw()
    {
        preDrawStyles.Dispose();

        if (!Flags.HasFlag(ImGuiWindowFlags.NoMove) && configWindow.IsOpen)
        {
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();

            var drawList = ImGui.GetForegroundDrawList();

            var outlineColor = ImGui.GetColorU32(new Vector4(255f / 255f, 98f / 255f, 98f / 255f, 0.75f));
            var thickness = 1.0f;

            var outlinePos = new Vector2(windowPos.X - (thickness * 0.5f), windowPos.Y - (thickness * 0.5f));
            var outlineSize = new Vector2(windowPos.X + windowSize.X + (thickness * 0.5f), windowPos.Y + windowSize.Y + (thickness * 0.5f));

            drawList.AddRect(outlinePos, outlineSize, outlineColor, 0.0f, ImDrawFlags.None, thickness);
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
    }

    private void DrawDevice(Device device)
    {
        if (!device.enabled)
            return;

        if (config.ShowBatteryIcon)
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.TextColoredWrapped(GetLevelColor(device.level), GetBatteryIcon(device.level));
            }
            ImGui.SameLine();
        }

        // TO-DO: Actually align battery levels...
        ImGui.TextColored(GetLevelColor(device.level), $"{(device.level.ToString().Length == 1 ? "   " : "")}{device.level}%%");

        if (config.ShowDeviceName)
        {
            ImGui.SameLine();
            using (ImRaii.PushIndent(1))
            {
                ImGui.SameLine();
                ImGui.TextUnformatted(device.name);
            }
        }

        ImGui.Spacing();
    }

    private static string GetBatteryIcon(int level)
    {
        if (level > 80)
        {
            return FontAwesomeIcon.BatteryFull.ToIconString();
        }
        else if (level > 50)
        {
            return FontAwesomeIcon.BatteryThreeQuarters.ToIconString();
        }
        else if (level > 30)
        {
            return FontAwesomeIcon.BatteryHalf.ToIconString();
        }
        else if (level > 15)
        {
            return FontAwesomeIcon.BatteryQuarter.ToIconString();
        }
        else
        {
            return FontAwesomeIcon.BatteryEmpty.ToIconString();
        }
    }

    private Vector4 GetLevelColor(int level)
    {
        if (config.ColorPercent == false)
        {
            return KnownColor.White.Vector();
        }
        else if (level > 80)
        {
            return KnownColor.LightGreen.Vector();
        }
        else if (level > 50)
        {
            return KnownColor.YellowGreen.Vector();
        }
        else if (level > 30)
        {
            return KnownColor.Orange.Vector();
        }
        else if (level > 15)
        {
            return KnownColor.OrangeRed.Vector();
        }
        else
        {
            return KnownColor.Red.Vector();
        }
    }
}
