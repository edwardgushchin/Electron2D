using System.Runtime.InteropServices;
using System.Text;
using SDL3;

namespace Electron2D;

internal sealed partial record GraphicsAdapterIdentity
{
    private const uint D3D12FeatureLevels = 2;
    private const uint DXGISoftware = 2;
    private const uint DXCoreDriverVersion = 1, DXCoreDriverDescription = 2;

    private static readonly Guid DXGIDeviceID = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
    private static readonly Guid DXGIAdapterID = new("29038f61-3839-4626-91fd-086879011a05");
    private static readonly Guid DXGIFactoryID = new("1bc6ea02-ef36-464f-bf0c-21ca39e5168a");
    private static readonly Guid DXCoreFactoryID = new("78ee5945-c36e-4b13-a669-005dd11c0f06");
    private static readonly Guid DXCoreAdapterID = new("f0db4c7f-fe5a-42a2-bd62-f2a6cf6fc83e");

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        internal uint LowPart;
        internal int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct DXGIAdapterDescription
    {
        internal fixed char Name[128];
        internal uint Vendor, Device, Subsystem, Revision;
        internal nuint DedicatedVideoMemory, DedicatedSystemMemory, SharedSystemMemory;
        internal LUID Luid;
        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct FeatureLevels
    {
        internal uint Count;
        internal uint* Requested;
        internal uint Maximum;
    }

    internal static unsafe GraphicsAdapterIdentity ReadDirect3DRenderer(nint renderer, string driver)
    {
        var props = SDL.GetRendererProperties(renderer);
        if (driver == "direct3d11")
        {
            var device = SDL.GetPointerProperty(props, SDL.Props.RendererD3D11DevicePointer, 0);
            if (device == 0 || Marshal.QueryInterface(device, in DXGIDeviceID, out var dxgiDevice) < 0) return Empty;
            try
            {
                // IDXGIDevice::GetAdapter returns an owned reference.
                nint deviceAdapter;
                if (((delegate* unmanaged[Stdcall]<nint, nint*, int>)Methods(dxgiDevice)[7])(dxgiDevice, &deviceAdapter) < 0) return Empty;
                try
                {
                    // ID3D11Device::GetFeatureLevel describes the existing device.
                    var level = ((delegate* unmanaged[Stdcall]<nint, uint>)Methods(device)[37])(device);
                    return ReadDXGIAdapter(deviceAdapter, level, cacheIdentity: false, Empty);
                }
                finally { Marshal.Release(deviceAdapter); }
            }
            finally { Marshal.Release(dxgiDevice); }
        }

        var device12 = SDL.GetPointerProperty(props, SDL.Props.RendererD3D12DevicePointer, 0);
        if (device12 == 0 || CreateDXGIFactory1(in DXGIFactoryID, out var factory) < 0) return Empty;
        try
        {
            // ID3D12Device::GetAdapterLuid uses the Windows COM structure return slot.
            LUID luid;
            ((delegate* unmanaged[Stdcall]<nint, LUID*, LUID*>)Methods(device12)[43])(device12, &luid);
            // IDXGIFactory4::EnumAdapterByLuid returns the adapter for this device, even on hybrid systems.
            nint adapter;
            var id = DXGIAdapterID;
            if (((delegate* unmanaged[Stdcall]<nint, LUID, Guid*, nint*, int>)Methods(factory)[26])(factory, luid, &id, &adapter) < 0)
                return Empty;
            try { return ReadDXGIAdapter(adapter, ReadD3D12FeatureLevel(device12), cacheIdentity: false, Empty); }
            finally { Marshal.Release(adapter); }
        }
        finally { Marshal.Release(factory); }
    }

    private static GraphicsAdapterIdentity ReadD3D12GPU(nint device, GraphicsAdapterIdentity fallback)
    {
        try
        {
            if (RenderingDevice.QueryDriverResource(device, 1, (uint)RenderingDevice.DriverResource.LogicalDevice, 0, out var logical) != 1 ||
                RenderingDevice.QueryDriverResource(device, 1, (uint)RenderingDevice.DriverResource.PhysicalDevice, 0, out var adapter) != 1)
                return fallback;

            return ReadDXGIAdapter((nint)adapter, ReadD3D12FeatureLevel((nint)logical), cacheIdentity: true, fallback);
        }
        catch (EntryPointNotFoundException) { return fallback; }
    }

    private static unsafe uint ReadD3D12FeatureLevel(nint device)
    {
        uint* levels = stackalloc uint[] { 0xC200, 0xC100, 0xC000, 0xB100, 0xB000 };
        var query = new FeatureLevels { Count = 5, Requested = levels };
        // ID3D12Device::CheckFeatureSupport.
        var check = (delegate* unmanaged[Stdcall]<nint, uint, FeatureLevels*, uint, int>)Methods(device)[13];
        if (check(device, D3D12FeatureLevels, &query, (uint)sizeof(FeatureLevels)) >= 0) return query.Maximum;
        // Older Direct3D runtimes can reject a list that includes feature level 12_2.
        query.Requested++;
        query.Count--;
        return check(device, D3D12FeatureLevels, &query, (uint)sizeof(FeatureLevels)) >= 0 ? query.Maximum : 0;
    }

    private static unsafe GraphicsAdapterIdentity ReadDXGIAdapter(nint adapter, uint featureLevel, bool cacheIdentity, GraphicsAdapterIdentity fallback)
    {
        if (Marshal.QueryInterface(adapter, in DXGIAdapterID, out var adapter1) < 0) return fallback;
        try
        {
            DXGIAdapterDescription description;
            // IDXGIAdapter1::GetDesc1 supplies the PCI vendor and the Windows adapter identity.
            if (((delegate* unmanaged[Stdcall]<nint, DXGIAdapterDescription*, int>)Methods(adapter1)[10])(adapter1, &description) < 0)
                return fallback;

            var text = new ReadOnlySpan<char>(description.Name, 128);
            var end = text.IndexOf('\0');
            var name = new string(end < 0 ? text : text[..end]);
            var vendor = VendorName(description.Vendor);
            var type = (description.Flags & DXGISoftware) != 0 ? RenderingDevice.DeviceType.CPU :
                description.DedicatedVideoMemory == 0 ? RenderingDevice.DeviceType.IntegratedGPU : RenderingDevice.DeviceType.DiscreteGPU;
            var api = featureLevel == 0 ? "" : FormattableString.Invariant($"{featureLevel >> 12}_{(featureLevel >> 8) & 15}");
            var (driverName, driverVersion) = ReadDXCoreDriver(description.Luid);
            if (driverName.Length == 0) driverName = fallback.DriverName;
            if (driverVersion.Length == 0) driverVersion = fallback.DriverVersion;

            var key = "";
            if (cacheIdentity && driverVersion.Length > 0)
            {
                var luid = Convert.ToHexStringLower(new ReadOnlySpan<byte>(&description.Luid, sizeof(LUID)));
                key = FormattableString.Invariant($"{luid}-driver-{description.Revision}-{driverVersion}");
            }

            return new(name.Length == 0 ? fallback.Name : name, vendor, api, type, driverName, driverVersion, key);
        }
        finally { Marshal.Release(adapter1); }
    }

    private static unsafe (string Name, string Version) ReadDXCoreDriver(LUID luid)
    {
        nint factory;
        try
        {
            if (DXCoreCreateAdapterFactory(in DXCoreFactoryID, out factory) < 0) return ("", "");
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException)
        {
            return ("", "");
        }

        try
        {
            nint adapter;
            // IDXCoreAdapterFactory::GetAdapterByLuid, using the LUID from IDXGIAdapter1.
            var id = DXCoreAdapterID;
            if (((delegate* unmanaged[Stdcall]<nint, LUID*, Guid*, nint*, int>)Methods(factory)[4])(factory, &luid, &id, &adapter) < 0)
                return ("", "");

            try
            {
                // IDXCoreAdapter::GetProperty and GetPropertySize.
                var get = (delegate* unmanaged[Stdcall]<nint, uint, nuint, void*, int>)Methods(adapter)[6];
                var size = (delegate* unmanaged[Stdcall]<nint, uint, nuint*, int>)Methods(adapter)[7];
                ulong raw;
                var version = get(adapter, DXCoreDriverVersion, sizeof(ulong), &raw) >= 0 && raw != 0
                    ? FormattableString.Invariant($"{raw >> 48}.{(raw >> 32) & 65535}.{(raw >> 16) & 65535}.{raw & 65535}") : "";
                nuint length;
                if (size(adapter, DXCoreDriverDescription, &length) < 0 || length is 0 or > 4096) return ("", version);

                Span<byte> text = stackalloc byte[(int)length];
                fixed (byte* bytes = text)
                    if (get(adapter, DXCoreDriverDescription, length, bytes) < 0) return ("", version);
                var end = text.IndexOf((byte)0);
                return (end < 0 ? "" : Encoding.UTF8.GetString(text[..end]), version);
            }
            finally { Marshal.Release(adapter); }
        }
        finally { Marshal.Release(factory); }
    }

    private static unsafe nint* Methods(nint instance) => *(nint**)instance;

    [LibraryImport("dxgi.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int CreateDXGIFactory1(in Guid id, out nint factory);

    [LibraryImport("dxcore.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DXCoreCreateAdapterFactory(in Guid id, out nint factory);
}
