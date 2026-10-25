using SDL3;

namespace Electron2D;

internal static unsafe class GPUDeviceFactory
{
    internal static (nint Device, bool Relaxed, bool SizedPoints) Create(bool allowAndroidRelaxation)
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
