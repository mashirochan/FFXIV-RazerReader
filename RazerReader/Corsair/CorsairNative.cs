using System;
using System.Runtime.InteropServices;

namespace BatteryBuddy.Corsair;

internal sealed unsafe class CorsairNative : IDisposable
{
    private readonly nint library;

    public delegate* unmanaged[Cdecl]<
        CorsairSessionStateChangedHandler,
        nint,
        CorsairError> Connect;

    public delegate* unmanaged[Cdecl]<CorsairError> Disconnect;

    public delegate* unmanaged[Cdecl]<
        CorsairDeviceFilter,
        int,
        nint,
        out int,
        CorsairError> GetDevices;

    public CorsairNative(string libraryPath)
    {
        library = NativeLibrary.Load(libraryPath);

        // Resolve function addresses and cast to function-pointer types.
        nint addr;

        addr = GetFunctionAddress("CorsairConnect");
        Connect = (delegate* unmanaged[Cdecl]<CorsairSessionStateChangedHandler, nint, CorsairError>)addr;

        addr = GetFunctionAddress("CorsairDisconnect");
        Disconnect = (delegate* unmanaged[Cdecl]<CorsairError>)addr;

        addr = GetFunctionAddress("CorsairGetDevices");
        GetDevices = (delegate* unmanaged[Cdecl]<CorsairDeviceFilter, int, nint, out int, CorsairError>)addr;
    }

    private nint GetFunctionAddress(string name)
    {
        return NativeLibrary.GetExport(library, name);
    }

    public void Dispose()
    {
        NativeLibrary.Free(library);
    }
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void CorsairSessionStateChangedHandler(
    nint context,
    CorsairSessionStateChanged* eventData);
