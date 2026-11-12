using System.Runtime.InteropServices;
using Electron2D;
using SDL3;

internal static class GraphicsAdapterIdentityTests
{
    internal static void Run(string profile, string[] expected)
    {
        CheckInactive();
        var gpu = profile is "vulkan" or "vulkan-no-identity" or "direct3d12";
        var expectedDriver = profile == "vulkan-no-identity" ? "vulkan" : profile == "direct3d12-compatibility" ? "direct3d12" : profile;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, gpu ? "gpu" : "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(96, 80), Title = "Adapter identity" };
        window.AddChild(new ColorRect { Size = new(96, 80), Color = Colors.Green });
        var checkedFrame = false;
        window.Ready += _ => RenderingServer.FramePostDraw += VerifyFrame;
        Check(Engine.Run(window) == 0 && checkedFrame, "The native renderer produced a checked frame.");
        CheckInactive();

        void VerifyFrame()
        {
            RenderingServer.FramePostDraw -= VerifyFrame;
            var driver = RenderingServer.GetCurrentRenderingDriverName();
            Check(driver == expectedDriver, $"Expected {profile}, got {driver}.");
            var name = RenderingServer.GetVideoAdapterName();
            var vendor = RenderingServer.GetVideoAdapterVendor();
            var api = RenderingServer.GetVideoAdapterAPIVersion();
            var type = RenderingServer.GetVideoAdapterType();
            var info = OS.GetVideoAdapterDriverInfo();
            if (gpu)
            {
                if (driver == "vulkan") Check(name == expected[0], "Active device name agrees with the independent adapter inventory.");
                var device = RenderingServer.GetRenderingDevice() ?? throw new InvalidOperationException("Missing active GPU device.");
                using var local = device.CreateLocalDevice();
                Check(local.GetDeviceName() == name && local.GetDeviceVendorName() == vendor, "The local GPU reports the same adapter.");
                var uuid = local.GetDevicePipelineCacheUUID();
                if (profile == "vulkan")
                {
                    Check(api == expected[1] && uuid == expected[2] && vendor == expected[4] &&
                        type == Enum.Parse<RenderingDevice.DeviceType>(expected[5]), "Vulkan identity must match the SDK inventory.");
                    Check(device.GetDevicePipelineCacheUUID() == uuid, "Device leases share the native cache identity.");
                }
                else if (driver == "direct3d12")
                {
                    var expectedKey = VerifyDirect3D(expected, driver, name, vendor, api, type, info, device);
                    Check(uuid == expectedKey && device.GetDevicePipelineCacheUUID() == uuid, "Both leases report the selected DXGI adapter and driver cache key.");
                }
                else
                    Check(api.Length == 0 && vendor.Length == 0 && uuid.Length == 0 &&
                        type == RenderingDevice.DeviceType.Other, "Unavailable native fields retain unknown values.");

                Check(info.Length == 2 && info[1].Length > 0, "The SDL GPU driver version is available.");
                if (driver == "vulkan")
                    Check(info[0] == expected[3], "Vulkan driver name agrees with the SDK inventory.");
                Console.WriteLine($"GPU cache UUID: {uuid}");
                var version = info[1];
                info[1] = "caller edit";
                Check(OS.GetVideoAdapterDriverInfo()[1] == version, "The driver array is caller-owned.");
                Reject<InvalidOperationException>(() => Task.Run(local.GetDeviceVendorName).GetAwaiter().GetResult());
                local.Dispose();
                Reject<ObjectDisposedException>(() => local.GetDevicePipelineCacheUUID());
            }
            else if (driver is "direct3d11" or "direct3d12")
                VerifyDirect3D(expected, driver, name, vendor, api, type, info, null);
            else if (profile == "opengl")
            {
                Check(name.Length > 0 && vendor.Length > 0 && api.Length > 0, "OpenGL reports its owned context strings.");
                VerifyGL(name, vendor, api);
                Check(type == RenderingDevice.DeviceType.Other && info.Length == 0, "OpenGL leaves its unclassified fields unknown.");
            }
            else
                CheckInactive();

            Check(Task.Run(RenderingServer.GetVideoAdapterName).GetAwaiter().GetResult() == name,
                "A worker sees the published adapter snapshot.");
            Console.WriteLine($"{driver}: {name}; {vendor}; {api}; {type}; {string.Join(", ", OS.GetVideoAdapterDriverInfo())}");
            checkedFrame = true;
            window.Tree!.Quit();
        }
    }

    private static unsafe string VerifyDirect3D(string[] expected, string driver, string name, string vendor, string api,
        RenderingDevice.DeviceType type, string[] info, RenderingDevice? device)
    {
        var nativeAPI = driver == "direct3d11" ? 11u : 12u;
        nint logical;
        if (device is not null)
            logical = (nint)device.GetDriverResource(RenderingDevice.DriverResource.LogicalDevice, default, 0);
        else
        {
            var windows = SDL.GetWindows(out var count);
            Check(count == 1, "The fixture has one native window.");
            var renderer = SDL.GetRenderer(windows![0]);
            Check(renderer != 0, "Inspect the window's actual SDL renderer.");
            var prop = nativeAPI == 11 ? SDL.Props.RendererD3D11DevicePointer : SDL.Props.RendererD3D12DevicePointer;
            logical = SDL.GetPointerProperty(SDL.GetRendererProperties(renderer), prop, 0);
        }
        Check(logical != 0 && expected.Length == 1, "Supply the SDK fixture path and use a live Direct3D device.");

        var module = NativeLibrary.Load(System.IO.Path.GetFullPath(expected[0]));
        try
        {
            var read = (delegate* unmanaged[Cdecl]<nint, uint, DXGIIdentity*, int>)NativeLibrary.GetExport(module, "e2d_test_direct3d_adapter");
            DXGIIdentity native;
            Check(read(logical, nativeAPI, &native) == 1, "The Windows SDK fixture can query the exact device adapter.");
            var model = new string(native.Name);
            var driverName = Marshal.PtrToStringUTF8((nint)native.DriverName);
            var rawVersion = native.DriverVersion;
            var version = FormattableString.Invariant($"{rawVersion >> 48}.{(rawVersion >> 32) & 65535}.{(rawVersion >> 16) & 65535}.{rawVersion & 65535}");
            var level = FormattableString.Invariant($"{native.FeatureLevel >> 12}_{(native.FeatureLevel >> 8) & 15}");
            Check(name == model && api == level && type == (RenderingDevice.DeviceType)native.Type,
                "Published adapter name, feature level and device kind match the SDK queries.");
            Check(vendor == (native.Vendor switch
            {
                0x1002 or 0x1022 => "Advanced Micro Devices, Inc.",
                0x10DE => "NVIDIA Corporation",
                0x1414 => "Microsoft",
                0x5143 => "Qualcomm",
                0x8086 => "Intel Corporation",
                _ => throw new InvalidOperationException($"Provide a fixture expectation for vendor {native.Vendor:X}."),
            }), "The vendor comes from the selected adapter's PCI identity.");
            Check(info.Length == 2 && driverName is { Length: > 0 } && info[0] == driverName && info[1] == version,
                "Driver description and the four-part driver version match DXCore.");
            var luid = Convert.ToHexStringLower(new ReadOnlySpan<byte>(&native.LUIDLow, 8));
            return FormattableString.Invariant($"{luid}-driver-{native.Revision}-{version}");
        }
        finally { NativeLibrary.Free(module); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct DXGIIdentity
    {
        internal fixed char Name[128];
        internal fixed byte DriverName[512];
        internal ulong DriverVersion;
        internal uint LUIDLow;
        internal int LUIDHigh;
        internal uint Vendor, Revision, Type, FeatureLevel;
    }

    private static unsafe void VerifyGL(string name, string vendor, string api)
    {
        var read = (delegate* unmanaged<uint, nint>)SDL.GLGetProcAddress("glGetString");
        Check(read != null && Marshal.PtrToStringUTF8(read(0x1f01)) == name &&
            Marshal.PtrToStringUTF8(read(0x1f00)) == vendor && Marshal.PtrToStringUTF8(read(0x1f02)) == api,
            "OpenGL snapshot agrees with the current native renderer context.");
    }

    private static void CheckInactive() => Check(RenderingServer.GetVideoAdapterName().Length == 0 &&
        RenderingServer.GetVideoAdapterVendor().Length == 0 && RenderingServer.GetVideoAdapterAPIVersion().Length == 0 &&
        RenderingServer.GetVideoAdapterType() == RenderingDevice.DeviceType.Other && OS.GetVideoAdapterDriverInfo().Length == 0,
        "The renderer supplies no adapter identity before startup, after shutdown or in software mode.");

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
