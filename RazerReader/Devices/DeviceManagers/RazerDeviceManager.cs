using BatteryBuddy.Models;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Timer = System.Timers.Timer;

namespace BatteryBuddy.Devices.DeviceManagers;

public class RazerDeviceManager : IDeviceManager
{
    private readonly IPluginLog log;
    private readonly Configuration config;
    private readonly Timer pollingTimer;

    private DateTime lastWriteTime;

    public DeviceSource Source => DeviceSource.Razer;
    public DeviceManagerStatus Status { get; } = new();
    public IReadOnlyList<Device> CurrentDevices { get; private set; } = [];
    public event Action<IReadOnlyList<Device>>? OnDevicesUpdated;

    public RazerDeviceManager(Plugin plugin, IPluginLog log)
    {
        this.log = log;
        config = plugin.Configuration;
        pollingTimer = new Timer(config.PollingTime * 1000);
        pollingTimer.Elapsed += (s, e) => PollFileForChanges();
        Status.IsEnabled = config.RazerEnabled;
    }

    public void Start()
    {
        if (Status.IsRunning)
            return;

        Status.IsRunning = true;
        PollFileForChanges(true);
        pollingTimer.Start();
        log.Info($"Polling for changes to Razer Synapse logs every {config.PollingTime} second(s).");
    }

    public void Stop()
    {
        if (!Status.IsRunning)
            return;

        pollingTimer.Stop();
        Status.IsRunning = false;
        log.Info("Stopping polling for Razer Synapse log changes.");
    }

    public void LogError(string errorMessage)
    {
        Status.IsHealthy = false;
        Status.Message = errorMessage;
        log.Error(errorMessage);
    }

    private void PollFileForChanges(bool forceUpdate = false)
    {
        try
        {
            log.Debug("Polling for changes to Razer Synapse logs...");

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directoryPath = Path.Combine(localAppData, "Razer", "RazerAppEngine", "User Data", "Logs");

            if (!Directory.Exists(directoryPath))
            {
                LogError($"Please make sure you have Synapse v4 installed and \"{directoryPath}\" exists!");
                var errorDevice = new Device()
                {
                    name = "Error, check logs!",
                    category = "ERROR",
                    isCharging = false,
                    level = 0,
                    hasBattery = false,
                    enabled = true
                };
                config.DeviceList.Mice = [errorDevice];
                config.Save();
                return;
            }

            var fileFilter = "systray_systrayv2*.log";
            var mostRecentFilePath = GetMostRecentFile(directoryPath, fileFilter);

            if (mostRecentFilePath == null)
            {
                LogError($"The Razer Synapse log could not be retrieved from \"{directoryPath}\"!");
                return;
            }

            var writeTime = File.GetLastWriteTime(mostRecentFilePath);

            if (writeTime > lastWriteTime || forceUpdate == true)
            {
                if (forceUpdate == true)
                {
                    log.Debug($"Forcing initial update for: {mostRecentFilePath}");
                }
                else
                {
                    log.Debug($"Detected change via polling: {mostRecentFilePath}");
                }

                ReadLatestContent(mostRecentFilePath);
                lastWriteTime = writeTime;
            }
            else
            {
                log.Debug("No file changes detected for Razer Synapse logs!");
            }
        }
        catch (Exception ex)
        {
            LogError($"RazerDeviceManager polling error: {ex.Message}");
        }
    }

    private string? GetMostRecentFile(string directoryPath, string fileFilter)
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
                LogError("No matching Razer Synapse log files found.");
                return null;
            }

            return mostRecentFile.FullName;
        }
        catch (Exception ex)
        {
            LogError($"Error polling Razer Synapse log files: {ex.Message}");
            return null;
        }
    }

    private void ReadLatestContent(string filePath)
    {
        log.Debug($"Reading latest content for \"{filePath}\"...");

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var fileLength = stream.Length;
        var position = fileLength;
        var bufferSize = 1024;

        var buffer = new byte[bufferSize];

        var content = new StringBuilder();

        log.Debug("1");

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

        log.Debug("2");

        var startIndex = content.ToString().IndexOf("connectingDeviceData: ") + "connectingDeviceData: ".Length;

        if (startIndex < 0)
        {
            log.Warning("'connectingDeviceData: ' not found in the Razer Synapse log!");
            return;
        }


        log.Debug("3");

        var jsonContent = content.ToString()[startIndex..];


        var noDevicesMatch = Regex.Match(jsonContent, @"^\[\]");

        if (noDevicesMatch.Success)
        {
            log.Debug("No devices found in the Razer Synapse log!");
            UpdateDeviceList([]);
            return;
        }

        var match = Regex.Match(jsonContent, @"\[\{.*\}\]");

        if (!match.Success)
        {
            log.Warning("No JSON data found after 'connectingDeviceData: ' in the Razer Synapse log!");
            log.Debug($"Content after 'connectingDeviceData: ': '{jsonContent}'");
            return;
        }

        log.Debug("4");

        var json = match.Value;
        var devicesArray = JArray.Parse(json);

        if (devicesArray == null)
        {
            LogError("The Razer Synapse device list could not be deserialized!");
            return;
        }

        log.Debug("5");

        UpdateDeviceList(ConvertDeviceLogsToDevices(devicesArray));
    }

    private void UpdateDeviceList(List<Device> devices)
    {
        foreach (var configDevice in config.DeviceList.All.Where(d => d.source == DeviceSource.Razer))
        {
            if (!devices.Any(d => d.name == configDevice.name))
            {
                configDevice.connected = false;
            }
        }

        CurrentDevices = devices;
        OnDevicesUpdated?.Invoke(CurrentDevices);
        Status.Message = null;
        Status.IsHealthy = true;
        Status.LastUpdate = DateTime.Now;
    }

    public List<Device> ConvertDeviceLogsToDevices(JArray deviceLogs)
    {
        List<Device> devices = [];
        foreach (var deviceLog in deviceLogs)
        {
            var deviceName = deviceLog["name"]?["en"];

            if (deviceName == null)
            {
                LogError("Device name not found! Skipping...");
                continue;
            }

            var deviceLevel = deviceLog["powerStatus"]?["level"];

            if (deviceLevel == null)
            {
                LogError("Device power level not found! Skipping...");
                continue;
            }

            var deviceStatus = deviceLog["powerStatus"]?["chargingStatus"];

            if (deviceStatus == null)
            {
                LogError("Device charging status not found! Skipping...");
                continue;
            }

            var connected = deviceStatus.ToString() != "off";
            var isCharging = deviceStatus.ToString() == "Charging";

            var existingDevice = GetExistingDevice(deviceName.ToString());

            if (existingDevice != null)
            {
                log.Debug($"Found existing device: {existingDevice.name}");
                existingDevice.level = deviceLevel.Value<int>();
                existingDevice.isCharging = isCharging;
                existingDevice.source = DeviceSource.Razer;
                existingDevice.connected = connected;
                devices.Add(existingDevice);
            }
            else
            {
                log.Debug($"Device not found, adding: {deviceName.ToString()}");

                var deviceCategory = deviceLog["category"];

                if (deviceCategory == null)
                {
                    LogError("Device category not found! Skipping...");
                    continue;
                }

                var deviceHasBattery = deviceLog["hasBattery"];

                if (deviceHasBattery == null)
                {
                    LogError("Device has battery not found! Skipping...");
                    continue;
                }

                devices.Add(new Device()
                {
                    name = deviceName.ToString(),
                    category = deviceCategory.ToString(),
                    isCharging = isCharging,
                    level = deviceLevel.Value<int>(),
                    hasBattery = deviceHasBattery.Value<bool>(),
                    enabled = true,
                    connected = connected,
                    source = DeviceSource.Razer
                });
            }
        }
        return devices;
    }

    public Device? GetExistingDevice(string name) => config.DeviceList.All.SingleOrDefault(d => d.name == name);
}
