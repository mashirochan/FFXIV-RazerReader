using BatteryBuddy.Models;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Websocket.Client;
using Timer = System.Timers.Timer;

namespace BatteryBuddy.Devices.DeviceManagers;

public class LogitechDeviceManager : IDeviceManager
{
    private readonly IPluginLog log;
    private readonly Configuration config;
    private readonly Timer pollingTimer;

    private const string WEBSOCKET_URI = "ws://localhost:9010";
    private readonly WebsocketClient? client;

    public DeviceSource Source => DeviceSource.Logitech;
    public DeviceManagerStatus Status { get; } = new();
    public IReadOnlyList<Device> CurrentDevices { get; private set; } = [];
    public event Action<IReadOnlyList<Device>>? OnDevicesUpdated;

    private List<JsonObject> logitechDevices = [];

    public LogitechDeviceManager(Plugin plugin, IPluginLog log)
    {
        this.log = log;
        config = plugin.Configuration;
        Status.IsEnabled = config.LogitechEnabled;

        pollingTimer = new Timer(config.PollingTime * 1000);
        pollingTimer.Elapsed += (s, e) => PollServerForChanges();

        client = CreateGHubWebSocketClient(WEBSOCKET_URI);

        client.MessageReceived.Subscribe(msg => ParseSocketMsg(msg, client));
        client.ErrorReconnectTimeout = TimeSpan.FromMilliseconds(500);
        client.ReconnectTimeout = null;
    }

    public void Start()
    {
        if (Status.IsRunning)
            return;

        Status.IsRunning = true;
        
        log.Info($"Starting Logitech websocket client for device changes.");
        try
        {
            if (client == null)
            {
                LogError("Failed to create Logitech websocket client!");
                return;
            }

            client.Start();

            // Subscribe to device state changes
            log.Info("Subscribing to Logitech device state changes...");
            client.Send(JsonSerializer.Serialize(new
            {
                msgId = "",
                verb = "SUBSCRIBE",
                path = "/devices/state/changed"
            }));
            log.Info("Subscribed to Logitech device state changes!");

            // Subscribe to battery state changes
            //log.Info("Subscribing to Logitech battery state changes...");
            //client.Send(JsonSerializer.Serialize(new
            //{
            //    msgId = "",
            //    verb = "SUBSCRIBE",
            //    path = "/battery/state/changed"
            //}));
            //log.Info("Subscribed to Logitech battery state changes!");

            PollServerForChanges();
            pollingTimer.Start();
            log.Info($"Polling for changes to Logitech devices every {config.PollingTime} second(s).");
        }
        catch (Exception ex)
        {
            LogError($"Error starting Logitech websocket client: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (!Status.IsRunning)
            return;

        pollingTimer.Stop();
        Status.IsRunning = false;
        log.Info("Stopping websocket client for Logitech device changes.");
    }

    public void LogError(string errorMessage)
    {
        Status.IsHealthy = false;
        Status.Message = errorMessage;
        log.Error(errorMessage);
    }

    private void PollServerForChanges()
    {
        log.Debug("Polling for changes to Logitech devices...");

        if (client == null)
        {
            LogError("Logitech websocket client is not initialized!");
            return;
        }

        try
        {
            log.Info("Requesting updated Logitech device list...");

            client.Send(JsonSerializer.Serialize(new
            {
                msgId = "",
                verb = "GET",
                path = "/devices/list"
            }));
        }
        catch (Exception ex)
        {
            LogError($"Error polling Logitech server for changes: {ex.Message}");
        }
    }

    private static WebsocketClient CreateGHubWebSocketClient(string uri)
    {
        var url = new Uri(uri);

        var factory = new Func<ClientWebSocket>(() =>
        {
            var client = new ClientWebSocket();
            client.Options.UseDefaultCredentials = false;
            client.Options.SetRequestHeader("Origin", "file://");
            client.Options.SetRequestHeader("Pragma", "no-cache");
            client.Options.SetRequestHeader("Cache-Control", "no-cache");
            client.Options.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
            client.Options.SetRequestHeader("Sec-WebSocket-Protocol", "json");
            client.Options.AddSubProtocol("json");
            return client;
        });

        return new WebsocketClient(url, factory);
    }

    private void ParseSocketMsg(ResponseMessage msg, WebsocketClient client)
    {
        log.Info("Socket message received, parsing...");

        var gHubMsg = JsonSerializer.Deserialize<GHubMessage>(msg.Text!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (gHubMsg == null)
        {
            log.Error("Failed to parse message:");
            log.Error(msg?.Text ?? "");
            return;
        }

        log.Debug(msg.ToString());

        switch (gHubMsg)
        {
            case { Path: "/devices/list" }:
                {
                    ParseDeviceList(gHubMsg.Payload, client);
                    break;
                }

            case { Path: "/battery/state/changed" }:
                {
                    ParseBatteryUpdate(gHubMsg.Payload);
                    break;
                }

            case { Path: string path } when Regex.Match(path, @"^/battery/(?<deviceId>[^/]+)/state$") is Match match && match.Success:
                {
                    var deviceId = match.Groups["deviceId"].Value;

                    log.Info(path);
                    ParseBatteryUpdate(gHubMsg.Payload, deviceId);
                    break;
                }

            case { Path: "/devices/state/changed" }:
                {
                    log.Info("Device state changed, requesting updated device list...");
                    client.Send(JsonSerializer.Serialize(new
                    {
                        msgId = "",
                        verb = "GET",
                        path = "/devices/list"
                    }));
                    break;
                }
        }
    }

    private void ParseDeviceList(JsonObject? payload, WebsocketClient client)
    {
        log.Info("Parsing device list payload...");

        if (payload == null)
        {
            log.Info("Received null payload for device list\n----------");
            return;
        }

        try
        {
            var deviceInfos = payload["deviceInfos"] as JsonArray;
            var deviceList = new List<JsonObject>();

            if (deviceInfos == null)
            {
                log.Info("deviceInfos is missing or not an array");
                return;
            }

            foreach (var deviceNode in deviceInfos)
            {
                if (deviceNode is not JsonObject device)
                    continue;

                var deviceId = deviceNode["id"]!.ToString();
                var deviceName = deviceNode["extendedDisplayName"]?.ToString() ?? "Unknown Logitech Device";

                log.Info($"Sending GET request for '/battery/{deviceId}/state' ({deviceName})...");
                client.Send(JsonSerializer.Serialize(new
                {
                    msgId = "",
                    verb = "GET",
                    path = $"/battery/{deviceId}/state"
                }));

                deviceList.Add(device);
            }

            logitechDevices = deviceList;
        }
        catch (Exception ex)
        {
            log.Info($"Error parsing device list: {ex.Message}");
            return;
        }
    }

    private void ParseBatteryUpdate(JsonObject? payload, string? pathDeviceId = null)
    {
        log.Info("Parsing battery update payload...");

        try
        {
            if (!string.IsNullOrWhiteSpace(pathDeviceId))
            {
                log.Info($"Received battery state update under path '/battery/{pathDeviceId}/state'");

                var deviceInfo = GetDeviceInfo(pathDeviceId);

                if (deviceInfo != null)
                {
                    var existingDevice = GetExistingDevice(deviceInfo["extendedDisplayName"]?.GetValue<string>() ?? "Unknown Logitech Device");

                    if (existingDevice == null)
                    {
                        log.Info($"Could not find existing device for device ID '{pathDeviceId}' in device list.");

                        if (payload == null)
                        {
                            log.Info($"Received null payload for battery update for device ID '{pathDeviceId}' and it is not in the device list - ignoring.");
                        }
                        else
                        {
                            log.Info($"Received battery update for device ID '{pathDeviceId}', but it is not in the device list. Adding new device info...");
                            var percentage = payload["percentage"]?.GetValue<int>();
                            var mileage = payload["mileage"]?.GetValue<float>();
                            var charging = payload["charging"]?.GetValue<bool>() ?? false;
                            var newDevice = new Device
                            {
                                name = deviceInfo["extendedDisplayName"]?.GetValue<string>() ?? "Unknown Logitech Device",
                                category = deviceInfo["deviceType"]?.GetValue<string>() ?? "Unknown",
                                isCharging = charging,
                                level = percentage ?? 0,
                                mileage = mileage ?? 0,
                                hasBattery = deviceInfo["capabilities"]?["hasBatteryStatus"]?.GetValue<bool>() ?? false,
                                enabled = true,
                                connected = true,
                                source = Source
                            };
                            UpdateDeviceList([newDevice]);
                        }
                    }
                    else
                    {
                        log.Info($"Found existing device for device ID '{pathDeviceId}' in device list. Attempting to update device info...");

                        if (payload == null)
                        {
                            log.Info($"Received null payload for battery update for device ID '{pathDeviceId}' and it is in the device list - setting as disconnected.");
                            existingDevice.connected = false;
                        }
                        else
                        {
                            log.Info($"Received battery update for device ID '{pathDeviceId}', updating existing device info...");
                            var percentage = payload["percentage"]?.GetValue<int>();
                            var mileage = payload["mileage"]?.GetValue<float>();
                            var charging = payload["charging"]?.GetValue<bool>() ?? false;
                            existingDevice.isCharging = charging;
                            existingDevice.level = percentage ?? 0;
                            existingDevice.mileage = mileage ?? 0;
                            existingDevice.connected = true;
                        }
                        UpdateDeviceList([existingDevice]);
                    }
                }
                else
                {
                    log.Info($"Could not find device ID '{pathDeviceId}' in device list.");
                }
                return;
            }
            else
            {
                log.Info($"Received battery state update under path '/battery/state/changed'");

                if (payload == null)
                {
                    log.Info("Received null payload for battery update");
                    return;
                }

                var deviceId = payload["deviceId"]?.GetValue<string>();
                var percentage = payload["percentage"]?.GetValue<int>();
                var mileage = payload["mileage"]?.GetValue<float>();
                var charging = payload["charging"]?.GetValue<bool>() ?? false;

                var deviceInfo = GetDeviceInfo(deviceId);

                if (deviceInfo == null)
                {
                    LogError($"Received battery update for unknown device ID: {deviceId}");
                    return;
                }

                var newDevice = new Device
                {
                    name = deviceInfo["extendedDisplayName"]?.GetValue<string>() ?? "Unknown Logitech Device",
                    category = deviceInfo["deviceType"]?.GetValue<string>() ?? "Unknown",
                    isCharging = charging,
                    level = percentage ?? 0,
                    mileage = mileage ?? 0,
                    hasBattery = deviceInfo["capabilities"]?["hasBatteryStatus"]?.GetValue<bool>() ?? false,
                    enabled = true,
                    connected = deviceInfo["state"]?.GetValue<string>() == "ACTIVE",
                    source = Source
                };

                UpdateDeviceList([newDevice]);
            }
        }
        catch (Exception ex)
        {
            log.Info($"Failed to parse battery update: {ex.Message}");
        }
    }

    private void UpdateDeviceList(List<Device> devices)
    {
        CurrentDevices = devices;
        OnDevicesUpdated?.Invoke(CurrentDevices);
        Status.Message = null;
        Status.IsHealthy = true;
        Status.LastUpdate = DateTime.Now;
    }

    private JsonObject? GetDeviceInfo(string? deviceId)
    {
        return logitechDevices.FirstOrDefault(d => d["id"]?.GetValue<string>() == deviceId);
    }

    private Device? GetExistingDevice(string name)
    {
        return config.DeviceList.All.SingleOrDefault(d => d.name == name);
    }
}
