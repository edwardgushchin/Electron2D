using SDL3;

namespace Electron2D;

internal readonly record struct SamplerConfiguration(RenderingDevice.SamplerFilter MinFilter,
    RenderingDevice.SamplerFilter MagFilter, RenderingDevice.SamplerFilter MIPFilter,
    RenderingDevice.SamplerRepeatMode RepeatU, RenderingDevice.SamplerRepeatMode RepeatV,
    float LODBias, float MinLOD, float MaxLOD, float AnisotropyMax, bool UseAnisotropy,
    bool EnableCompare, RenderingDevice.CompareOperator CompareOp)
{
    internal bool Anisotropic => UseAnisotropy && AnisotropyMax > 1;
    internal bool Linear => MinFilter == RenderingDevice.SamplerFilter.Linear ||
        MagFilter == RenderingDevice.SamplerFilter.Linear || MIPFilter == RenderingDevice.SamplerFilter.Linear || Anisotropic;

    internal static SamplerConfiguration Capture(RDSamplerState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var result = new SamplerConfiguration(state.MinFilter, state.MagFilter, state.MIPFilter,
            state.RepeatU, state.RepeatV, state.LODBias, state.MinLOD, state.MaxLOD,
            state.AnisotropyMax, state.UseAnisotropy, state.EnableCompare, state.CompareOp);
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
        _ = Address(RepeatU); _ = Address(RepeatV);
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
        CompareOp = CompareOp
    };

    internal SDL.GPUSamplerCreateInfo Native(string driver, bool anisotropyAvailable = true)
    {
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

    private static SDL.GPUSamplerAddressMode Address(RenderingDevice.SamplerRepeatMode value) => value switch
    {
        RenderingDevice.SamplerRepeatMode.Repeat => SDL.GPUSamplerAddressMode.Repeat,
        RenderingDevice.SamplerRepeatMode.MirroredRepeat => SDL.GPUSamplerAddressMode.MirroredRepeat,
        RenderingDevice.SamplerRepeatMode.ClampToEdge => SDL.GPUSamplerAddressMode.ClampToEdge,
        _ => throw new NotSupportedException("The native sampler has no border/mirror-clamp mode.")
    };
}
