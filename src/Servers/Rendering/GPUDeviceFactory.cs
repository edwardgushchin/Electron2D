using SDL3;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe class GPUDeviceFactory
{
    internal const string MirrorClampProperty = "Electron2D.gpu.sampler_mirror_clamp";
    internal const string Float64Property = "Electron2D.gpu.shader_float64";
    internal const string Int64Property = "Electron2D.gpu.shader_int64";
    internal const string BufferAtomic64Property = "Electron2D.gpu.buffer_int64_atomics";
    internal const string SharedAtomic64Property = "Electron2D.gpu.shared_int64_atomics";
    [StructLayout(LayoutKind.Sequential)]
    private struct Vulkan12Features
    {
        internal uint Type;
        internal nint Next;
        internal fixed uint Flags[47];
    }
    private static readonly object Gate = new();
    private static SharedDevice? _shared;

    internal static RenderMemorySnapshot ReadMemory(RenderHandle lease) => Volatile.Read(ref _shared)?.ReadMemory(lease) ?? default;

    internal static RenderFormatSupport ReadFormatSupport(RenderHandle lease)
    {
        lock (Gate)
        {
            var shared = _shared;
            if (lease.IsClosed || shared is null || shared.Handle.DangerousGetHandle() != lease.DangerousGetHandle()) throw new ObjectDisposedException(nameof(lease));
            return shared.FormatSupport ??= new RenderFormatSupport(shared.Handle.DangerousGetHandle());
        }
    }

    /// <summary>Retains the active native device without retaining another consumer or its window.</summary>
    internal static (RenderHandle Handle, bool Relaxed, bool SizedPoints) Acquire(bool allowAndroidRelaxation)
    {
        lock (Gate)
        {
            var shared = _shared ??= new SharedDevice(allowAndroidRelaxation);
            try
            {
                var lease = new RenderHandle(shared.Handle.DangerousGetHandle(), shared.Release);
                shared.Leases++;
                return (lease, shared.Relaxed, shared.SizedPoints);
            }
            catch
            {
                if (shared.Leases == 0) { shared.Handle.Dispose(); _shared = null; }
                throw;
            }
        }
    }

    private sealed class SharedDevice
    {
        internal readonly RenderHandle Handle;
        internal readonly bool Relaxed, SizedPoints;
        internal int Leases;
        internal RenderFormatSupport? FormatSupport;
        private int _memoryReaders, _retiring;

        internal RenderMemorySnapshot ReadMemory(RenderHandle lease)
        {
            if (lease.IsClosed || Volatile.Read(ref _retiring) != 0) return default;
            Interlocked.Increment(ref _memoryReaders);
            try
            {
                if (lease.IsClosed || Volatile.Read(ref _retiring) != 0) return default;
                return RenderMemory.Read(Handle.DangerousGetHandle());
            }
            finally { Interlocked.Decrement(ref _memoryReaders); }
        }

        internal SharedDevice(bool allowAndroidRelaxation)
        {
            var created = Create(allowAndroidRelaxation);
            Handle = new RenderHandle(created.Device, SDL.DestroyGPUDevice);
            Relaxed = created.Relaxed; SizedPoints = created.SizedPoints;
        }

        internal void Release(nint unused)
        {
            lock (Gate)
            {
                if (--Leases != 0) return;
                Volatile.Write(ref _retiring, 1);
                var wait = new SpinWait();
                while (Volatile.Read(ref _memoryReaders) != 0) wait.SpinOnce();
                // Complete instance retirement before another acquisition can create a Wayland device.
                Handle.Dispose(); _shared = null;
            }
        }
    }

    private static (nint Device, bool Relaxed, bool SizedPoints) Create(bool allowAndroidRelaxation)
    {
        var formats = ShaderCompiler.GetFormats();
        var device = Attempt(formats, true, false); var sized = device != 0;
        if (device == 0) device = Attempt(formats, false, false);
        var relaxed = false;
        if (device == 0 && allowAndroidRelaxation && OperatingSystem.IsAndroid() && (formats & SDL.GPUShaderFormat.SPIRV) != 0)
        {
            relaxed = true; device = Attempt(formats, true, true); sized = device != 0;
            if (device == 0) device = Attempt(formats, false, true);
        }
        var driver = device == 0 ? "" : SDL.GetGPUDeviceDriver(device);
        return (device, relaxed, driver == "metal" || driver == "vulkan" && sized);
    }

    private static nint Attempt(SDL.GPUShaderFormat formats, bool sizedPoints, bool relaxed)
    {
        ReadOnlySpan<(bool Float64, bool Int64)> profiles = [(true, true), (true, false), (false, true), (false, false)];
        foreach (var profile in profiles)
        {
            ReadOnlySpan<int> atomicProfiles = profile.Int64 ? [3, 1, 2, 0] : [0];
            foreach (var atomics in atomicProfiles)
            {
                var device = Attempt(formats, sizedPoints, relaxed, profile.Float64, profile.Int64, atomics);
                if (device != 0) return device;
            }
        }
        return 0;
    }

    private static nint Attempt(SDL.GPUShaderFormat formats, bool sizedPoints, bool relaxed, bool float64, bool int64, int atomics)
    {
        var props = SDL.CreateProperties();
        if (props == 0) throw new InvalidOperationException("GPU device properties could not be created: " + SDL.GetError());
        try
        {
            var valid = SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateDebugModeBoolean, false) &&
                SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersSPIRVBoolean, (formats & SDL.GPUShaderFormat.SPIRV) != 0) &&
                SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersDXBCBoolean, (formats & SDL.GPUShaderFormat.DXBC) != 0) &&
                SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersDXILBoolean, (formats & SDL.GPUShaderFormat.DXIL) != 0) &&
                SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersMSLBoolean, (formats & SDL.GPUShaderFormat.MSL) != 0) &&
                SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersMetalLibBoolean, (formats & SDL.GPUShaderFormat.MetalLib) != 0);
            if (relaxed) valid = valid &&
                SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateFeatureClipDistanceBoolean, false) &&
                SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateFeatureDepthClampingBoolean, false) &&
                SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateFeatureIndirectDrawFirstInstanceBoolean, false) &&
                SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateFeatureAnisotropyBoolean, false);
            if (!valid) throw new InvalidOperationException("GPU device properties could not be configured: " + SDL.GetError());
            // VkPhysicalDeviceFeatures has 55 VkBool32 fields; largePoints/float64/int64 are fields 16/39/40.
            Span<uint> features = stackalloc uint[55]; features.Clear(); features[16] = sizedPoints ? 1u : 0u;
            features[39] = float64 ? 1u : 0u; features[40] = int64 ? 1u : 0u;
            // VkPhysicalDeviceVulkan12Features has 47 flags: mirror clamp/buffer atomics/shared atomics are 0/5/6.
            var advanced = new Vulkan12Features { Type = 51 };
            advanced.Flags[0] = 1; advanced.Flags[5] = (uint)atomics & 1; advanced.Flags[6] = ((uint)atomics >> 1) & 1;
            fixed (uint* pointer = features)
            fixed (byte* extensionName = "VK_KHR_sampler_mirror_clamp_to_edge\0"u8)
            {
                var extension = (nint)extensionName;
                var options = new SDL.GPUVulkanOptions { VulkanApiVersion = (1u << 22) | (atomics == 0 ? 0u : 2u << 12), FeatureList = atomics == 0 ? 0 : (nint)(&advanced), Vulkan10PhysicalDeviceFeatures = (nint)pointer, DeviceExtensionCount = 1, DeviceExtensionNames = (nint)(&extension) };
                if (!SDL.SetPointerProperty(props, SDL.Props.GPUDeviceCreateVulkanOptionsPointer, (nint)(&options)))
                    throw new InvalidOperationException("GPU capabilities could not be requested: " + SDL.GetError());
                var device = SDL.CreateGPUDeviceWithProperties(props); var mirror = device != 0;
                if (device == 0)
                {
                    // Optional features must not prevent ordinary device creation.
                    options.DeviceExtensionCount = 0; options.DeviceExtensionNames = 0; advanced.Flags[0] = 0;
                    device = SDL.CreateGPUDeviceWithProperties(props);
                }
                if (device == 0) return 0;
                if (SDL.GetGPUDeviceDriver(device) == "vulkan" &&
                    (mirror && !SDL.SetBooleanProperty(SDL.GetGPUDeviceProperties(device), MirrorClampProperty, true) ||
                     float64 && !SDL.SetBooleanProperty(SDL.GetGPUDeviceProperties(device), Float64Property, true) ||
                     int64 && !SDL.SetBooleanProperty(SDL.GetGPUDeviceProperties(device), Int64Property, true) ||
                     (atomics & 1) != 0 && !SDL.SetBooleanProperty(SDL.GetGPUDeviceProperties(device), BufferAtomic64Property, true) ||
                     (atomics & 2) != 0 && !SDL.SetBooleanProperty(SDL.GetGPUDeviceProperties(device), SharedAtomic64Property, true)))
                {
                    var error = SDL.GetError(); SDL.DestroyGPUDevice(device);
                    throw new InvalidOperationException("GPU capabilities could not be retained: " + error);
                }
                return device;
            }
        }
        finally { SDL.DestroyProperties(props); }
    }
}
