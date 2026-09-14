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

public class CorsairDeviceManager : IDeviceManager
{
    private readonly IPluginLog log;
    private readonly Configuration config;
    private readonly Timer pollingTimer;
    private BatteryBuddy.Corsair.CorsairClient? client;

    private DateTime lastWriteTime;

    public DeviceSource Source => DeviceSource.Corsair;
    public DeviceManagerStatus Status { get; } = new();
    public IReadOnlyList<Device> CurrentDevices { get; private set; } = Array.Empty<Device>();
    public event Action<IReadOnlyList<Device>>? OnDevicesUpdated;

    public CorsairDeviceManager(Plugin plugin, IPluginLog log)
    {
        this.log = log;
        config = plugin.Configuration;
        pollingTimer = new Timer(config.PollingTime * 1000);
        pollingTimer.Elapsed += (s, e) => PollForChanges();
        Status.IsEnabled = config.RazerEnabled;
    }

    public void Start()
    {
        if (Status.IsRunning)
            return;

        Status.IsRunning = true;

        // Use the packaged SDK path (native/x64/iCUESDK.x64_2019.dll). Fail if it can't be loaded.
        try
        {
            var sdkPath = Corsair.CorsairClient.GetDefaultSdkFullPath();
            log.Debug($"Corsair SDK path: {sdkPath}");
            log.Debug($"Corsair SDK exists: {System.IO.File.Exists(sdkPath)}");

            client = new Corsair.CorsairClient();
            var connectResult = client.Connect();

            if (connectResult == Corsair.CorsairError.Success && client.IsConnected)
            {
                log.Debug("Connected to packaged Corsair SDK");

                try
                {
                    var devices = client.GetDevices();
                    log.Debug($"Corsair GetDevices returned {devices?.Count ?? 0} devices");

                    if (devices != null)
                    {
                        foreach (var d in devices)
                        {
                            log.Debug($"Corsair Device: Model={d.Model}, Id={d.Id}, Serial={d.Serial}, Type={d.Type}, Leds={d.LedCount}, Channels={d.ChannelCount}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error calling CorsairClient.GetDevices: {ex.Message}");
                }
            }
            else
            {
                log.Debug($"Corsair Connect failed: {connectResult}");
                client.Dispose();
                client = null;
            }
        }
        catch (Exception ex)
        {
            log.Debug($"Failed to initialize packaged Corsair client: {ex.Message}");
            client = null;
        }

        PollForChanges();
        pollingTimer.Start();
        log.Info($"Started Corsair device manager; polling every {config.PollingTime} second(s).");
    }

    public void Stop()
    {
        if (!Status.IsRunning)
            return;

        pollingTimer.Stop();
        Status.IsRunning = false;
        log.Info("Stopping polling for Corsair devices.");
    }

    public void LogError(string errorMessage)
    {
        Status.IsHealthy = false;
        Status.Message = errorMessage;
        log.Error(errorMessage);
    }

    private void PollForChanges()
    {

    }

    private void UpdateDeviceList(List<Device> devices)
    {
        foreach (var configDevice in config.DeviceList.All.Where(d => d.source == Source))
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
}
