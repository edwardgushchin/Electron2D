using Electron2D;
using SDL3;
using static Electron2D.RenderingDevice;

internal static class SamplerConfigurationTests
{
    internal static void Run()
    {
        FilterProperties();
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
        Check(!configuration.Native("direct3d12", anisotropicPointMIP: true).EnableAnisotropy, "Direct3D 12 preserves an authored nearest min or mag filter.");
        var allLinear = configuration with { MagFilter = SamplerFilter.Linear };
        var pointMIP = allLinear with { MIPFilter = SamplerFilter.Nearest };
        Check(allLinear.Native("direct3d12").EnableAnisotropy && allLinear.Native("direct3d12").MaxAnisotropy == 4 &&
            !pointMIP.Native("direct3d12").EnableAnisotropy && pointMIP.Native("direct3d12", anisotropicPointMIP: true).EnableAnisotropy,
            "Direct3D 12 enables point mip anisotropy only with device support.");
        Reject<NotSupportedException>(() => (allLinear with { LODBias = 16 }).Native("direct3d12"));
        Reject<NotSupportedException>(() => (allLinear with { LODBias = -17 }).Native("direct3d12"));
        Check((allLinear with { LODBias = -16 }).Native("direct3d12").MipLodBias == -16 &&
            (allLinear with { LODBias = 15.99f }).Native("direct3d12").MipLodBias == 15.99f, "Direct3D 12 accepts both bias limits.");
        Reject<NotSupportedException>(() => configuration.Native("metal"));
        state.LODBias = 0; _ = SamplerConfiguration.Capture(state).Native("metal");
        var borderDescriptor = state.GetPropertyList().OfType<PropertyDescriptor<RDSamplerState, RenderingDevice.SamplerBorderColor>>().Single(p => p.Name == nameof(RDSamplerState.BorderColor));
        Check(borderDescriptor.GetValue(state) == RenderingDevice.SamplerBorderColor.FloatOpaqueBlack, "Typed border descriptor preserves the default.");
        borderDescriptor.SetValue(state, RenderingDevice.SamplerBorderColor.FloatOpaqueWhite);
        state.RepeatU = RenderingDevice.SamplerRepeatMode.ClampToBorder;
        state.BorderColor = RenderingDevice.SamplerBorderColor.FloatOpaqueWhite;
        configuration = SamplerConfiguration.Capture(state);
        using (var copy = configuration.CreateState()) Check(copy.BorderColor == state.BorderColor && configuration.HasBorder && configuration.IntegerBorder == false, "Border configuration copies and preserves the scalar role.");
        _ = configuration.Native("vulkan"); Reject<NotSupportedException>(() => configuration.Native("metal"));
        configuration.ValidateImage(0); Reject<ArgumentException>(() => configuration.ValidateImage(1));
        state.BorderColor = RenderingDevice.SamplerBorderColor.IntOpaqueWhite; configuration = SamplerConfiguration.Capture(state);
        configuration.ValidateImage(1); configuration.ValidateImage(2); Reject<ArgumentException>(() => configuration.ValidateImage(0));
        Reject<ArgumentOutOfRangeException>(() => state.BorderColor = RenderingDevice.SamplerBorderColor.Max);
        Reject<ArgumentOutOfRangeException>(() => state.BorderColor = (RenderingDevice.SamplerBorderColor)(-1));
        state.Dispose(); Reject<ObjectDisposedException>(() => _ = state.BorderColor); Reject<ObjectDisposedException>(() => state.BorderColor = RenderingDevice.SamplerBorderColor.FloatOpaqueBlack);
        using var mirrored = new RDSamplerState(); mirrored.RepeatU = RenderingDevice.SamplerRepeatMode.MirrorClampToEdge;
        var mirror = SamplerConfiguration.Capture(mirrored); Check(mirror.HasMirrorClamp && mirror.Native("vulkan").AddressModeU == SDL.GPUSamplerAddressMode.ClampToEdge, "Mirror-clamp authoring uses an unpublished native placeholder.");
        Reject<NotSupportedException>(() => mirror.Native("metal"));
        mirrored.UnnormalizedUVW = true; mirrored.MaxLOD = 0; Reject<ArgumentException>(() => SamplerConfiguration.Capture(mirrored));
        using var coordinates = new RDSamplerState { UnnormalizedUVW = true, MaxLOD = 0 };
        var coordinateDescriptor = coordinates.GetPropertyList().OfType<PropertyDescriptor<RDSamplerState, bool>>().Single(p => p.Name == nameof(RDSamplerState.UnnormalizedUVW));
        Check(coordinateDescriptor.GetValue(coordinates), "The typed coordinate descriptor preserves the authored mode.");
        var captured = SamplerConfiguration.Capture(coordinates); using (var copied = captured.CreateState()) Check(copied.UnnormalizedUVW && copied.MaxLOD == 0, "Coordinate state copies without a borrowed description.");
        _ = captured.Native("vulkan"); Reject<NotSupportedException>(() => captured.Native("metal"));
        foreach (var change in new Action<RDSamplerState>[] { p => p.MinFilter = RenderingDevice.SamplerFilter.Linear, p => p.MIPFilter = RenderingDevice.SamplerFilter.Linear, p => p.MinLOD = -1, p => p.MaxLOD = 1, p => p.RepeatU = RenderingDevice.SamplerRepeatMode.Repeat, p => p.RepeatV = RenderingDevice.SamplerRepeatMode.MirroredRepeat, p => p.EnableCompare = true, p => { p.UseAnisotropy = true; p.AnisotropyMax = 2; } })
        {
            using var invalidCoordinate = captured.CreateState(); change(invalidCoordinate); Reject<ArgumentException>(() => SamplerConfiguration.Capture(invalidCoordinate));
        }
        coordinates.UseAnisotropy = true; Check(!SamplerConfiguration.Capture(coordinates).Anisotropic, "A limit of one remains disabled in texel-coordinate mode.");
        coordinates.Dispose(); Reject<ObjectDisposedException>(() => coordinateDescriptor.GetValue(coordinates)); Reject<ObjectDisposedException>(() => coordinateDescriptor.SetValue(coordinates, false));
        Console.WriteLine("Sampler configuration: eight typed descriptors, copied defaults and authored values, validation, disposal, Vulkan/Direct3D 12 field projection and capability guards passed.");
    }

    private static void FilterProperties()
    {
        using var state = new RDSamplerState();
        Verify(nameof(state.MinFilter), SamplerFilter.Nearest, SamplerFilter.Linear, s => s.MinFilter);
        Verify(nameof(state.MagFilter), SamplerFilter.Nearest, SamplerFilter.Linear, s => s.MagFilter);
        Verify(nameof(state.MIPFilter), SamplerFilter.Nearest, SamplerFilter.Linear, s => s.MIPFilter);
        Verify(nameof(state.MinLOD), 0f, 1.25f, s => s.MinLOD);
        Verify(nameof(state.MaxLOD), 1e20f, 4f, s => s.MaxLOD);
        Verify(nameof(state.LODBias), 0f, -.75f, s => s.LODBias);
        Verify(nameof(state.UseAnisotropy), false, true, s => s.UseAnisotropy);
        Verify(nameof(state.AnisotropyMax), 1f, 8.5f, s => s.AnisotropyMax);

        Reject<ArgumentOutOfRangeException>(() => state.MinFilter = (SamplerFilter)2);
        Reject<ArgumentOutOfRangeException>(() => state.MagFilter = (SamplerFilter)(-1));
        Reject<ArgumentOutOfRangeException>(() => state.MIPFilter = (SamplerFilter)2);
        Reject<ArgumentOutOfRangeException>(() => state.MinLOD = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => state.MaxLOD = float.NegativeInfinity);
        Reject<ArgumentOutOfRangeException>(() => state.LODBias = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => state.AnisotropyMax = .99f);
        Reject<ArgumentOutOfRangeException>(() => state.AnisotropyMax = 16.01f);
        Reject<ArgumentNullException>(() => SamplerConfiguration.Capture(null!));
        state.MinLOD = -1;
        Reject<ArgumentException>(() => SamplerConfiguration.Capture(state));
        state.MinLOD = 5;
        Reject<ArgumentException>(() => SamplerConfiguration.Capture(state));
        state.Dispose();
        Reject<ObjectDisposedException>(() => SamplerConfiguration.Capture(state));

        void Verify<T>(string name, T initial, T authored, Func<RDSamplerState, T> read) where T : notnull
        {
            var property = state.GetPropertyList().OfType<PropertyDescriptor<RDSamplerState, T>>().Single(p => p.Name == name);
            Check(property.GetValue(state).Equals(initial) && read(state).Equals(initial), name + " default.");
            property.SetValue(state, authored);
            Check(property.TryGetRevertValue(state, out var revert) && revert.Equals(initial), name + " revert value.");
            using var copy = SamplerConfiguration.Capture(state).CreateState();
            property.SetValue(state, initial);
            Check(read(copy).Equals(authored) && property.GetValue(state).Equals(initial), name + " copied descriptor write.");
            property.SetValue(state, authored);
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
