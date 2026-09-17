using BatteryBuddy.Devices;
using BatteryBuddy.Devices.DeviceManagers;
using BatteryBuddy.Models;
using BatteryBuddy.Windows;
using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Timers;

namespace BatteryBuddy;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] internal static INotificationManager NotificationManager { get; private set; } = null!;

    private const string CommandName = "/pbattery";

    public Configuration Configuration { get; init; }
    public DeviceSourceManager DeviceSourceManager { get; init; }

    public readonly WindowSystem WindowSystem = new("BatteryBuddy");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private ServerBar ServerBar { get; init; }

    public readonly Timer lowBatteryTimer;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ServerBar = new ServerBar(this);
        ConfigWindow = new ConfigWindow(this, Log, ServerBar);
        MainWindow = new MainWindow(this, ConfigWindow);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the configuration menu for Battery Buddy."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        Framework.Update += OnUpdate;

        lowBatteryTimer = new Timer(Configuration.LowBatteryNotificationTimer * 1000 * 60);
        lowBatteryTimer.Elapsed += OnLowBatteryTimerElapsed;
        lowBatteryTimer.Start();
        Log.Info($"Low battery notification timer started with interval: {Configuration.LowBatteryNotificationTimer} minutes.");

        DeviceSourceManager = new DeviceSourceManager([
            new RazerDeviceManager(this, Log),
            new LogitechDeviceManager(this, Log)
        ]);

        DeviceSourceManager.OnDevicesUpdated += (devices) => UpdateDeviceList([.. devices]);

        DeviceSourceManager.StartAll();
    }

    public void Dispose()
    {
        Configuration.Save();

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        ServerBar.Dispose();

        CommandManager.RemoveHandler(CommandName);

        DeviceSourceManager.StopAll();

        lowBatteryTimer?.Stop();
        lowBatteryTimer?.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        ToggleConfigUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    private void OnUpdate(IFramework framework)
    {
        if (Configuration.IsWindowEnabled && Configuration.DeviceList.All.Any(d => d.enabled == true && d.connected == true))
        {
            MainWindow.IsOpen = true;
        }
        else
        {
            MainWindow.IsOpen = false;
        }
    }

    private void OnLowBatteryTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!Configuration.LowBatteryNotification)
        {
            return;
        }

        var lowBatteryDevices = Configuration.DeviceList.All
            .Where(d => d.enabled == true && d.level < Configuration.LowBatteryLevel && d.isCharging != true)
            .ToList();

        if (lowBatteryDevices.Count > 0)
        {
            var deviceStr = "";

            foreach (var device in lowBatteryDevices)
            {
                deviceStr += $"- {device.name} ({device.level}%)\n";
            }

            var lowBatteryNotification = new Notification()
            {
                Title = "Low Battery Warning!",
                Content = $"The following devices have low battery:\n{deviceStr}",
                MinimizedText = "Devices have low battery!",
                Type = NotificationType.Warning
            };

            NotificationManager.AddNotification(lowBatteryNotification);
        }
    }

    public void UpdateDeviceList(List<Device> devices)
    {
        Log.Info($"Received {devices.Count} devices. Updating device list...");
        foreach (var device in devices)
        {
            Log.Debug(JsonConvert.SerializeObject(device));
        }
        var newDeviceList = new DeviceList();
        var lowBatteryDevices = new List<Device>();

        foreach (var device in devices)
        {
            if (device.hasBattery == false)
                continue;

            if (device.category == "MOUSE") newDeviceList.Mice.Add(device);
            else if (device.category == "KEYBOARD") newDeviceList.Keyboards.Add(device);
            else if (device.category == "AUDIO" || device.category == "HEADSET") newDeviceList.Headsets.Add(device);

            if (device.level < Configuration.LowBatteryLevel)
            {
                lowBatteryDevices.Add(device);
            }
        }
        Log.Debug(JsonConvert.SerializeObject(newDeviceList));

        Configuration.DeviceList = MergeDeviceLists(Configuration.DeviceList, newDeviceList);

        Configuration.Save();
        Log.Info("Device information updated!");
        ServerBar.UpdateDtrBar();
    }

    private DeviceList MergeDeviceLists(DeviceList oldList, DeviceList newList)
    {
        var merged = oldList.All
            .Concat(newList.All)
            .GroupBy(d => d.name)
            .Select(g => g.Last())
            .ToList();

        return new DeviceList
        {
            Mice = [.. merged.Where(d => d.category == "MOUSE")],
            Keyboards = [.. merged.Where(d => d.category == "KEYBOARD")],
            Headsets = [.. merged.Where(d => d.category == "AUDIO" || d.category == "HEADSET")],
            LowestDevice = merged.Where(d => d.enabled == true && d.connected == true).OrderBy(d => d.level).FirstOrDefault()
        };
    }

    public static string GetDeviceIcon(string category)
    {
        return category switch
        {
            "MOUSE" => FontAwesomeIcon.Mouse.ToIconString(),
            "KEYBOARD" => FontAwesomeIcon.Keyboard.ToIconString(),
            "AUDIO" => FontAwesomeIcon.Headset.ToIconString(),
            "HEADSET" => FontAwesomeIcon.Headset.ToIconString(),
            _ => FontAwesomeIcon.Question.ToIconString(),
        };
    }

    public static Vector4 GetLevelColor(int level)
    {
        if (level > 80)
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
