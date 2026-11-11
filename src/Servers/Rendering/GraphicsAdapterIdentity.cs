using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed partial record GraphicsAdapterIdentity(string Name, string Vendor, string APIVersion,
    RenderingDevice.DeviceType Type, string DriverName, string DriverVersion, string PipelineCacheUUID)
{
    internal static readonly GraphicsAdapterIdentity Empty = new("", "", "", RenderingDevice.DeviceType.Other, "", "", "");

    internal static unsafe GraphicsAdapterIdentity ReadGPU(nint device)
    {
        var properties = SDL.GetGPUDeviceProperties(device);
        var name = SDL.GetStringProperty(properties, SDL.Props.GPUDeviceNameString, "") ?? "";
        var driverName = SDL.GetStringProperty(properties, SDL.Props.GPUDeviceDriverNameString, "") ?? "";
        var driverVersion = SDL.GetStringProperty(properties, SDL.Props.GPUDeviceDriverVersionString, "") ?? "";
        if (SDL.GetGPUDeviceDriver(device) == "vulkan")
        {
            Span<byte> uuid = stackalloc byte[16];
            try
            {
                fixed (byte* bytes = uuid)
                {
                    if (ReadVulkan(device, 1, out var vendor, out var type, out var version, bytes) == 1)
                    {
                        var api = version == 0 ? "" : FormattableString.Invariant($"{(version >> 22) & 0x7f}.{(version >> 12) & 0x3ff}.{version & 0xfff}");
                        var kind = type < (uint)RenderingDevice.DeviceType.Max ? (RenderingDevice.DeviceType)type : RenderingDevice.DeviceType.Other;
                        return new(name, VendorName(vendor), api, kind, driverName, driverVersion, Convert.ToHexStringLower(uuid));
                    }
                }
            }
            catch (EntryPointNotFoundException) { } // A preceding native module still supplies SDL's public strings.
        }

        return new(name, "", "", RenderingDevice.DeviceType.Other, driverName, driverVersion, "");
    }

    internal static unsafe GraphicsAdapterIdentity ReadGL(nint window)
    {
        if (SDL.GLGetCurrentWindow() != window || SDL.GLGetCurrentContext() == 0)
            return Empty;
        var get = (delegate* unmanaged<uint, nint>)SDL.GLGetProcAddress("glGetString");
        if (get == null)
            return Empty;
        return new(Marshal.PtrToStringUTF8(get(0x1f01)) ?? "", Marshal.PtrToStringUTF8(get(0x1f00)) ?? "",
            Marshal.PtrToStringUTF8(get(0x1f02)) ?? "", RenderingDevice.DeviceType.Other, "", "", "");
    }

    private static string VendorName(uint id) => id switch
    {
        0x1002 or 0x1022 => "Advanced Micro Devices, Inc.",
        0x1010 => "Imagination Technologies",
        0x106B => "Apple",
        0x10DE => "NVIDIA Corporation",
        0x13B5 => "Arm",
        0x1414 => "Microsoft",
        0x5143 => "Qualcomm",
        0x8086 => "Intel Corporation",
        0x10005 => "Mesa",
        _ => "",
    };

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_adapter_identity")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int ReadVulkan(nint device, uint abi, out uint vendor, out uint type, out uint version, byte* uuid);
}
