using SDL3;

namespace Electron2D;

internal static unsafe class GPUDeviceFactory
{
    private static readonly object Gate = new();
    private static SharedDevice? _shared;

    internal static RenderMemorySnapshot ReadMemory(RenderHandle lease) => Volatile.Read(ref _shared)?.ReadMemory(lease) ?? default;

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
        if (device == 0) device = SDL.CreateGPUDevice(formats, false, null);
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
            if (!sizedPoints) return SDL.CreateGPUDeviceWithProperties(props);
            // VkPhysicalDeviceFeatures has 55 VkBool32 fields; largePoints is field 16.
            Span<uint> features = stackalloc uint[55]; features.Clear(); features[16] = 1;
            fixed (uint* pointer = features)
            {
                var options = new SDL.GPUVulkanOptions { VulkanApiVersion = 1u << 22, Vulkan10PhysicalDeviceFeatures = (nint)pointer };
                if (!SDL.SetPointerProperty(props, SDL.Props.GPUDeviceCreateVulkanOptionsPointer, (nint)(&options)))
                    throw new InvalidOperationException("GPU point capabilities could not be requested: " + SDL.GetError());
                return SDL.CreateGPUDeviceWithProperties(props);
            }
        }
        finally { SDL.DestroyProperties(props); }
    }
}
