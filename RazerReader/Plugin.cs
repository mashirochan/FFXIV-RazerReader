using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using RazerReader.Models;
using RazerReader.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace RazerReader;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] internal static INotificationManager NotificationManager { get; private set; } = null!;

    private const string CommandName = "/prazer";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("RazerReader");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private ServerBar ServerBar { get; init; }

    private readonly Timer? pollingTimer;
    private DateTime lastWriteTime;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ServerBar = new ServerBar(this);
        ConfigWindow = new ConfigWindow(this, ServerBar);
        MainWindow = new MainWindow(this, ConfigWindow);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the configuration menu for Razer Reader."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        Framework.Update += OnUpdate;

        PollFileForChanges(true);

        pollingTimer = new Timer(Configuration.PollingTime * 1000);
        pollingTimer.Elapsed += (sender, e) => PollFileForChanges();
        pollingTimer.Start();

        Log.Info($"Polling for changes every {Configuration.PollingTime} second(s).");
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        ServerBar.Dispose();

        CommandManager.RemoveHandler(CommandName);

        pollingTimer?.Stop();
        pollingTimer?.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        ToggleConfigUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    private void OnUpdate(IFramework framework)
    {
        if (Configuration.IsWindowEnabled)
        {
            MainWindow.IsOpen = true;
        }
        else
        {
            MainWindow.IsOpen = false;
        }
    }

    private static string? GetMostRecentFile(string directoryPath, string fileFilter)
    {
        try
        {
            var logFiles = Directory.GetFiles(directoryPath, fileFilter);

            var mostRecentFile = logFiles
                .Select(file => new FileInfo(file))
                .OrderByDescending(fileInfo => fileInfo.LastWriteTime)
                .FirstOrDefault();

            if (mostRecentFile == null)
            {
                Log.Error("No matching log files found.");
                return null;
            }

            return mostRecentFile.FullName;
        }
        catch (Exception ex)
        {
            Log.Error($"Error polling files: {ex.Message}");
            return null;
        }
    }

    public void PollFileForChanges(bool forceUpdate = false)
    {
        try
        {
            Log.Debug("Polling...");

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directoryPath = Path.Combine(localAppData, "Razer", "RazerAppEngine", "User Data", "Logs");

            if (!Directory.Exists(directoryPath))
            {
                Log.Error($"Please make sure you have Synapse v4 installed and \"{directoryPath}\" exists!");
                var errorDevice = new Device()
                {
                    name = "Error, check logs!",
                    category = "ERROR",
                    chargingStatus = "ERROR",
                    level = 0,
                    hasBattery = false,
                    enabled = true
                };
                Configuration.DeviceList.Mice = [errorDevice];
                Configuration.Save();
                return;
            }

            var fileFilter = "systray_systrayv2*.log";
            var mostRecentFilePath = GetMostRecentFile(directoryPath, fileFilter);

            if (mostRecentFilePath == null)
            {
                Log.Error($"The Razer Synapse log could not be retrieved from \"{directoryPath}\"!");
                return;
            }

            var writeTime = File.GetLastWriteTime(mostRecentFilePath);

            if (writeTime > lastWriteTime || forceUpdate == true)
            {
                if (forceUpdate == true)
                {
                    Log.Debug($"Forcing initial update for: {mostRecentFilePath}");
                }
                else
                {
                    Log.Debug($"Detected change via polling: {mostRecentFilePath}");
                }

                ReadLatestContent(mostRecentFilePath);
                lastWriteTime = writeTime;
            }
            else
            {
                Log.Debug("No file changes detected!");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error polling file: {ex.Message}");
        }
    }

    private void ReadLatestContent(string filePath)
    {
        Log.Debug($"Reading latest content for \"{filePath}\"...");

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var fileLength = stream.Length;
        var position = fileLength;
        var bufferSize = 1024;

        var buffer = new byte[bufferSize];

        var content = new StringBuilder();

        while (position > 0)
        {
            var readPosition = Math.Max(position - bufferSize, 0);
            var bytesRead = (int)(position - readPosition);

            stream.Seek(readPosition, SeekOrigin.Begin);

            var bytesReadCount = stream.Read(buffer, 0, bytesRead);

            if (bytesReadCount > 0)
            {
                content.Insert(0, Encoding.UTF8.GetString(buffer, 0, bytesReadCount));
            }

            position = readPosition;

            if (content.ToString().Contains("connectingDeviceData: "))
            {
                break;
            }
        }

        var startIndex = content.ToString().IndexOf("connectingDeviceData: ") + "connectingDeviceData: ".Length;

        if (startIndex < 0)
        {
            Log.Error("'connectingDeviceData: ' not found in the log!");
            return;
        }

        var jsonContent = content.ToString()[startIndex..];
        var match = Regex.Match(jsonContent, @"\[\{.*\}\]");

        if (!match.Success)
        {
            Log.Error("No JSON data found after 'connectingDeviceData: ' in the log!");
            return;
        }

        var json = match.Value;
        var devices = JsonConvert.DeserializeObject<List<DeviceLog>>(json);

        if (devices == null)
        {
            Log.Error("The device list could not be deserialized!");
            return;
        }
            
        SetDeviceList(ConvertDeviceLogsToDevices(devices));
    }

    public List<Device> ConvertDeviceLogsToDevices(List<DeviceLog> deviceLogs)
    {
        List<Device> devices = [];
        foreach (var deviceLog in deviceLogs)
        {
            var existingDevice = GetExistingDevice(deviceLog.name.en);

            if (existingDevice != null)
            {
                Log.Debug($"Found existing device: {existingDevice.name}");
                existingDevice.level = deviceLog.powerStatus.level;
                existingDevice.chargingStatus = deviceLog.powerStatus.chargingStatus;
                devices.Add(existingDevice);
            }
            else
            {
                Log.Debug($"Device not found, adding: {deviceLog.name.en}");
                devices.Add(new Device()
                {
                    name = deviceLog.name.en,
                    category = deviceLog.category,
                    chargingStatus = deviceLog.powerStatus.chargingStatus,
                    level = deviceLog.powerStatus.level,
                    hasBattery = deviceLog.hasBattery,
                    enabled = true
                });
            }
        }
        return devices;
    }

    public Device? GetExistingDevice(string name)
    {
        foreach (var mouse in Configuration.DeviceList.Mice)
        {
            if (mouse.name == name)
                return mouse;
        }
        return null;
    }

    public void SetDeviceList(List<Device> devices)
    {
        var deviceList = new DeviceList();
        var lowBatteryDevices = new List<Device>();
        foreach (var device in devices)
        {
            if (device.hasBattery == false)
                continue;

            if (device.category == "MOUSE") deviceList.Mice.Add(device);
            else if (device.category == "KEYBOARD") deviceList.Keyboards.Add(device);
            else if (device.category == "HEADSET") deviceList.Headsets.Add(device);

            if (deviceList.LowestDevice == null || device.level < deviceList.LowestDevice.level)
            {
                deviceList.LowestDevice = device;
            }

            if (device.level < Configuration.LowBatteryLevel)
            {
                lowBatteryDevices.Add(device);
            }
        }
        Log.Debug(JsonConvert.SerializeObject(deviceList));
        Configuration.DeviceList = deviceList;
        Configuration.Save();
        Log.Info("Device information updated!");
        ServerBar.UpdateDtrBar();

        if (Configuration.LowBatteryNotification && lowBatteryDevices.Count > 0)
        {
            var deviceStr = "";

            foreach (var device in lowBatteryDevices)
            {
                deviceStr += $"- {device.name} ({device.level}%)\n";
            }

            var lowBattyNotification = new Notification()
            {
                Title = "Low Battery Warning!",
                Content = $"The following devices have low battery:\n{deviceStr}",
                MinimizedText = "Devices have low battery!",
                Type = NotificationType.Warning

            };

            NotificationManager.AddNotification(lowBattyNotification);
        }
    }
}
