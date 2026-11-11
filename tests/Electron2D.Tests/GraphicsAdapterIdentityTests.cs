using System.Runtime.InteropServices;
using Electron2D;
using SDL3;

internal static class GraphicsAdapterIdentityTests
{
    internal static void Run(string profile, string[] expected)
    {
        CheckInactive();
        var gpu = profile is "vulkan" or "vulkan-no-identity" or "direct3d12";
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
            Check(driver == (profile == "vulkan-no-identity" ? "vulkan" : profile), $"Expected {profile}, got {driver}.");
            var name = RenderingServer.GetVideoAdapterName();
            var vendor = RenderingServer.GetVideoAdapterVendor();
            var api = RenderingServer.GetVideoAdapterAPIVersion();
            var type = RenderingServer.GetVideoAdapterType();
            var info = OS.GetVideoAdapterDriverInfo();
            if (gpu)
            {
                Check(name == expected[0], "Active device name agrees with the independent adapter inventory.");
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
                else
                    Check(api.Length == 0 && vendor.Length == 0 && uuid.Length == 0 &&
                        type == RenderingDevice.DeviceType.Other, "Unavailable native fields retain unknown values.");

                Check(info.Length == 2 && info[1].Length > 0, "The SDL GPU driver version is available.");
                if (driver == "vulkan")
                    Check(info[0] == expected[3], "Vulkan driver name agrees with the SDK inventory.");
                else
                    Check(info[0].Length == 0, "An unreported driver name stays empty.");
                Console.WriteLine($"GPU cache UUID: {uuid}");
                var version = info[1];
                info[1] = "caller edit";
                Check(OS.GetVideoAdapterDriverInfo()[1] == version, "The driver array is caller-owned.");
                Reject<InvalidOperationException>(() => Task.Run(local.GetDeviceVendorName).GetAwaiter().GetResult());
                local.Dispose();
                Reject<ObjectDisposedException>(() => local.GetDevicePipelineCacheUUID());
            }
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
