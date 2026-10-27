using SDL3;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal readonly partial record struct SamplerConfiguration(RenderingDevice.SamplerFilter MinFilter,
    RenderingDevice.SamplerFilter MagFilter, RenderingDevice.SamplerFilter MIPFilter,
    RenderingDevice.SamplerRepeatMode RepeatU, RenderingDevice.SamplerRepeatMode RepeatV,
    float LODBias, float MinLOD, float MaxLOD, float AnisotropyMax, bool UseAnisotropy,
    bool EnableCompare, RenderingDevice.CompareOperator CompareOp,
    RenderingDevice.SamplerBorderColor BorderColor, bool UnnormalizedUVW)
{
    internal bool HasBorder => RepeatU == RenderingDevice.SamplerRepeatMode.ClampToBorder || RepeatV == RenderingDevice.SamplerRepeatMode.ClampToBorder;
    internal bool? IntegerBorder => HasBorder ? ((int)BorderColor & 1) != 0 : null;
    internal bool Anisotropic => UseAnisotropy && AnisotropyMax > 1;
    internal bool LinearFilter => MinFilter == RenderingDevice.SamplerFilter.Linear ||
        MagFilter == RenderingDevice.SamplerFilter.Linear || MIPFilter == RenderingDevice.SamplerFilter.Linear;
    internal bool Linear => LinearFilter || Anisotropic;

    internal static SamplerConfiguration Capture(RDSamplerState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var result = new SamplerConfiguration(state.MinFilter, state.MagFilter, state.MIPFilter,
            state.RepeatU, state.RepeatV, state.LODBias, state.MinLOD, state.MaxLOD,
            state.AnisotropyMax, state.UseAnisotropy, state.EnableCompare, state.CompareOp, state.BorderColor, state.UnnormalizedUVW);
        result.Validate();
        return result;
    }

    internal void Validate()
    {
        RDTextureConfiguration.Filter(MinFilter); RDTextureConfiguration.Filter(MagFilter); RDTextureConfiguration.Filter(MIPFilter);
        RDTextureConfiguration.Repeat(RepeatU); RDTextureConfiguration.Repeat(RepeatV);
        RDTextureConfiguration.Finite(LODBias); RDTextureConfiguration.Finite(MinLOD); RDTextureConfiguration.Finite(MaxLOD);
        RDTextureConfiguration.Anisotropy(AnisotropyMax);
        if (MinLOD < 0 || MaxLOD < MinLOD) throw new ArgumentException("Invalid sampler LOD interval.");
        if (CompareOp is < RenderingDevice.CompareOperator.Never or >= RenderingDevice.CompareOperator.Max)
            throw new ArgumentOutOfRangeException(nameof(CompareOp));
        if (UnnormalizedUVW && (MinFilter != MagFilter || MIPFilter != RenderingDevice.SamplerFilter.Nearest || MinLOD != 0 || MaxLOD != 0 || Anisotropic || EnableCompare ||
            RepeatU is not (RenderingDevice.SamplerRepeatMode.ClampToEdge or RenderingDevice.SamplerRepeatMode.ClampToBorder) || RepeatV is not (RenderingDevice.SamplerRepeatMode.ClampToEdge or RenderingDevice.SamplerRepeatMode.ClampToBorder)))
            throw new ArgumentException("Unnormalized sampling requires matching filters, nearest mip selection, zero LOD bounds, clamp addressing and no anisotropy or comparison.");
        _ = Address(RepeatU); _ = Address(RepeatV); _ = Border(BorderColor);
    }

    internal RDSamplerState CreateState() => new()
    {
        MinFilter = MinFilter,
        MagFilter = MagFilter,
        MIPFilter = MIPFilter,
        RepeatU = RepeatU,
        RepeatV = RepeatV,
        LODBias = LODBias,
        MinLOD = MinLOD,
        MaxLOD = MaxLOD,
        AnisotropyMax = AnisotropyMax,
        UseAnisotropy = UseAnisotropy,
        EnableCompare = EnableCompare,
        CompareOp = CompareOp,
        BorderColor = BorderColor,
        UnnormalizedUVW = UnnormalizedUVW
    };

    internal SDL.GPUSamplerCreateInfo Native(string driver, bool anisotropyAvailable = true)
    {
        if ((HasBorder || UnnormalizedUVW) && driver != "vulkan") throw new NotSupportedException("Border and unnormalized sampling require the native Vulkan sampler adapter.");
        if (LODBias != 0 && driver == "metal") throw new NotSupportedException("This backend requires shader-applied LOD bias.");
        if (Anisotropic && !anisotropyAvailable) throw new NotSupportedException("Anisotropic filtering is unavailable on this GPU device.");
        return new()
        {
            MinFilter = (SDL.GPUFilter)MinFilter,
            MagFilter = (SDL.GPUFilter)MagFilter,
            MipmapMode = (SDL.GPUSamplerMipmapMode)MIPFilter,
            AddressModeU = Address(RepeatU),
            AddressModeV = Address(RepeatV),
            AddressModeW = SDL.GPUSamplerAddressMode.ClampToEdge,
            MipLodBias = LODBias,
            MinLod = MinLOD,
            MaxLod = MaxLOD,
            EnableAnisotropy = Anisotropic,
            MaxAnisotropy = AnisotropyMax,
            EnableCompare = EnableCompare,
            CompareOp = (SDL.GPUCompareOp)((int)CompareOp + 1)
        };
    }

    internal nint Create(nint device, string driver, bool anisotropyAvailable = true)
    {
        var info = Native(driver, anisotropyAvailable);
        return driver == "vulkan" ? CreateNative(device, 1, in info, (uint)RepeatU, (uint)RepeatV, (uint)BorderColor, UnnormalizedUVW ? 1u : 0u) : SDL.CreateGPUSampler(device, in info);
    }

    internal static RenderingDevice.SamplerBorderColor Border(RenderingDevice.SamplerBorderColor value) =>
        value is >= RenderingDevice.SamplerBorderColor.FloatTransparentBlack and < RenderingDevice.SamplerBorderColor.Max ? value : throw new ArgumentOutOfRangeException(nameof(value));

    internal void ValidateImage(int pixelType)
    {
        if (IntegerBorder is { } integer && integer != (pixelType != 0))
            throw new ArgumentException("The sampler border color must match the image's integer or floating-point sampling type.");
    }

    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_sampler")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint CreateNative(nint device, uint abi, in SDL.GPUSamplerCreateInfo info, uint u, uint v, uint border, uint unnormalized);

    internal static void RequireUnnormalizedView(nint device, nint texture, bool unsafeOperations = false)
    {
        if (unsafeOperations) throw new ArgumentException("Unnormalized sampling cannot use implicit LOD, projection, comparison, bias or offsets.");
        var compatible = CheckView(device, 1, texture);
        if (compatible < 0) throw new NotSupportedException("Unnormalized image validation requires the pinned Vulkan adapter.");
        if (compatible == 0) throw new ArgumentException("Unnormalized sampling requires a single-mip, single-layer ordinary image view.");
    }

    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_sampler_view")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int CheckView(nint device, uint abi, nint texture);

    private static SDL.GPUSamplerAddressMode Address(RenderingDevice.SamplerRepeatMode value) => value switch
    {
        RenderingDevice.SamplerRepeatMode.Repeat => SDL.GPUSamplerAddressMode.Repeat,
        RenderingDevice.SamplerRepeatMode.MirroredRepeat => SDL.GPUSamplerAddressMode.MirroredRepeat,
        RenderingDevice.SamplerRepeatMode.ClampToEdge => SDL.GPUSamplerAddressMode.ClampToEdge,
        RenderingDevice.SamplerRepeatMode.ClampToBorder => SDL.GPUSamplerAddressMode.ClampToEdge,
        _ => throw new NotSupportedException("The native sampler has no mirror-clamp mode.")
    };
}
