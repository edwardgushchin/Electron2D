using Electron2D;
using SDL3;

internal static class SamplerConfigurationTests
{
    internal static void Run()
    {
        using var state = new RDSamplerState { UseAnisotropy = true };
        var configuration = SamplerConfiguration.Capture(state);
        Check(!configuration.Linear && !configuration.Native("vulkan", false).EnableAnisotropy, "A limit of one disables anisotropy, including integer and relaxed-device paths.");
        state.AnisotropyMax = 4; configuration = SamplerConfiguration.Capture(state);
        Reject<NotSupportedException>(() => configuration.Native("vulkan", false));
        state.MinFilter = RenderingDevice.SamplerFilter.Linear; state.MIPFilter = RenderingDevice.SamplerFilter.Linear;
        state.RepeatU = RenderingDevice.SamplerRepeatMode.MirroredRepeat; state.RepeatV = RenderingDevice.SamplerRepeatMode.Repeat;
        state.MinLOD = 1; state.MaxLOD = 3; state.LODBias = .5f; state.EnableCompare = true; state.CompareOp = RenderingDevice.CompareOperator.Greater;
        configuration = SamplerConfiguration.Capture(state); var native = configuration.Native("vulkan");
        Check(native.MinFilter == SDL.GPUFilter.Linear && native.MagFilter == SDL.GPUFilter.Nearest && native.MipmapMode == SDL.GPUSamplerMipmapMode.Linear &&
            native.AddressModeU == SDL.GPUSamplerAddressMode.MirroredRepeat && native.AddressModeV == SDL.GPUSamplerAddressMode.Repeat &&
            native.AddressModeW == SDL.GPUSamplerAddressMode.ClampToEdge && native.MinLod == 1 && native.MaxLod == 3 && native.MipLodBias == .5f &&
            native.EnableAnisotropy && native.MaxAnisotropy == 4 && native.EnableCompare && native.CompareOp == SDL.GPUCompareOp.Greater, "All copied fields reach the native creation contract.");
        Reject<NotSupportedException>(() => configuration.Native("metal"));
        state.LODBias = 0; _ = SamplerConfiguration.Capture(state).Native("metal");
        foreach (var mode in new[] { RenderingDevice.SamplerRepeatMode.ClampToBorder, RenderingDevice.SamplerRepeatMode.MirrorClampToEdge })
        { state.RepeatU = mode; Reject<NotSupportedException>(() => SamplerConfiguration.Capture(state)); }
        Console.WriteLine("Sampler configuration: complete native field projection, effective anisotropy, relaxed-device and Metal/address guards passed without creating a device.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
