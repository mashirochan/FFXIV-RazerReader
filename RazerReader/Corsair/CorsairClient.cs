using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BatteryBuddy.Corsair;

internal unsafe sealed class CorsairClient : IDisposable
{
    private const int MaxDevices = 64;

    private CorsairNative? native;

    // The native SDK retains this callback, so this must remain rooted.
    private readonly CorsairSessionStateChangedHandler sessionCallback;

    private CorsairSessionState sessionState =
        CorsairSessionState.Invalid;

    public bool IsConnected =>
        sessionState == CorsairSessionState.Connected;

    public CorsairSessionState SessionState =>
        sessionState;

    private static readonly string DefaultSdkRelativePath = System.IO.Path.Combine("native", "x64", "iCUESDK.x64_2019.dll");

    public CorsairClient()
    {
        sessionCallback = OnSessionStateChanged;
        native = null;
    }

    public static string GetDefaultSdkFullPath()
    {
        var assemblyPath = System.IO.Path.GetDirectoryName(typeof(CorsairClient).Assembly.Location);
        var baseDir = string.IsNullOrEmpty(assemblyPath) ? AppContext.BaseDirectory : assemblyPath;
        return System.IO.Path.Combine(baseDir, DefaultSdkRelativePath);
    }

    public CorsairError Connect()
    {
        if (native == null)
        {
            var fullPath = GetDefaultSdkFullPath();
            if (!System.IO.File.Exists(fullPath))
            {
                return CorsairError.NotConnected;
            }

            try
            {
                native = new CorsairNative(fullPath);
            }
            catch
            {
                native = null;
                return CorsairError.NotConnected;
            }
        }

        return native.Connect(
            sessionCallback,
            nint.Zero);
    }

    public CorsairError Disconnect()
    {
        if (native == null)
            return CorsairError.NotConnected;

        return native.Disconnect();
    }

    public IReadOnlyList<CorsairDeviceInfo> GetDevices()
    {
        if (!IsConnected || native == null)
            return Array.Empty<CorsairDeviceInfo>();

        int structSize = Marshal.SizeOf<CorsairDeviceInfo>();

        IntPtr buffer = Marshal.AllocHGlobal(structSize * MaxDevices);

        try
        {
            var filter = new CorsairDeviceFilter
            {
                DeviceTypeMask = CorsairDeviceType.All
            };

            CorsairError result =
                native.GetDevices(
                    filter,
                    MaxDevices,
                    buffer,
                    out int count);

            if (result != CorsairError.Success)
                throw new CorsairException(
                    "CorsairGetDevices failed.",
                    result);

            var devices =
                new List<CorsairDeviceInfo>(count);

            for (int i = 0; i < count; i++)
            {
                IntPtr address = IntPtr.Add(buffer, i * structSize);

                var device = Marshal.PtrToStructure<CorsairDeviceInfo>(address);

                devices.Add(device);
            }

            return devices;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void OnSessionStateChanged(
        nint context,
        CorsairSessionStateChanged* eventData)
    {
        if (eventData == null)
        {
            sessionState =
                CorsairSessionState.Invalid;

            return;
        }

        sessionState = eventData->State;
        // no-op formatting change
    }

    public void Dispose()
    {
        if (native == null)
            return;

        try
        {
            if (sessionState != CorsairSessionState.Closed &&
                sessionState != CorsairSessionState.Invalid)
            {
                try
                {
                    native.Disconnect();
                }
                catch
                {
                    // swallow; we're disposing anyway
                }
            }
        }
        finally
        {
            native.Dispose();
            native = null;
        }
    }
}

internal sealed class CorsairException : Exception
{
    public CorsairError Error { get; }

    public CorsairException(
        string message,
        CorsairError error)
        : base($"{message} Error: {error}")
    {
        Error = error;
    }
}
