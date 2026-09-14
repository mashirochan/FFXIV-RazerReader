using System;

namespace BatteryBuddy.Corsair;

using System.Runtime.InteropServices;

internal enum CorsairError
{
    Success = 0,
    NotConnected = 1,
    NoControl = 2,
    IncompatibleProtocol = 3,
    InvalidArguments = 4,
    InvalidOperation = 5,
    DeviceNotFound = 6,
    NotAllowed = 7
}

[Flags]
internal enum CorsairDeviceType : uint
{
    Unknown = 0,
    Keyboard = 1 << 0,
    Mouse = 1 << 1,
    MouseMat = 1 << 2,
    Headset = 1 << 3,
    HeadsetStand = 1 << 4,
    Commander = 1 << 5,
    LightingNode = 1 << 6,
    MemoryModule = 1 << 7,
    Cooler = 1 << 8,
    LedController = 1 << 9,
    GraphicsCard = 1 << 10,
    Motherboard = 1 << 11,
    PowerSupply = 1 << 12,
    UnknownDevice = 1 << 13,

    All = 0xFFFFFFFF
}

internal enum CorsairSessionState
{
    Invalid = 0,
    Closed = 1,
    Connecting = 2,
    Timeout = 3,
    ConnectionRefused = 4,
    ConnectionLost = 5,
    Connected = 6
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct CorsairDeviceInfo
{
    public CorsairDeviceType Type;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Serial;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Model;

    public int LedCount;
    public int ChannelCount;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CorsairDeviceFilter
{
    public CorsairDeviceType DeviceTypeMask;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CorsairSessionStateChanged
{
    public CorsairSessionState State;
}
