using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Selects the comparison of an incoming depth or stencil reference against stored data.</summary>
    public enum CompareOperator
    {
        /// <summary>Never passes.</summary>
        Never = 0,
        /// <summary>Passes when incoming is less.</summary>
        Less = 1,
        /// <summary>Passes when equal.</summary>
        Equal = 2,
        /// <summary>Passes when incoming is less or equal.</summary>
        LessOrEqual = 3,
        /// <summary>Passes when incoming is greater.</summary>
        Greater = 4,
        /// <summary>Passes when different.</summary>
        NotEqual = 5,
        /// <summary>Passes when incoming is greater or equal.</summary>
        GreaterOrEqual = 6,
        /// <summary>Always passes.</summary>
        Always = 7,
        /// <summary>Exclusive nonselectable bound.</summary>
        Max = 8,
    }
    /// <summary>Selects the eight-bit stencil update after comparison.</summary>
    public enum StencilOperation
    {
        /// <summary>Preserves the value.</summary>
        Keep = 0,
        /// <summary>Writes zero.</summary>
        Zero = 1,
        /// <summary>Writes the reference.</summary>
        Replace = 2,
        /// <summary>Increments with saturation.</summary>
        IncrementAndClamp = 3,
        /// <summary>Decrements with saturation.</summary>
        DecrementAndClamp = 4,
        /// <summary>Inverts all bits.</summary>
        Invert = 5,
        /// <summary>Increments modulo 256.</summary>
        IncrementAndWrap = 6,
        /// <summary>Decrements modulo 256.</summary>
        DecrementAndWrap = 7,
        /// <summary>Exclusive nonselectable bound.</summary>
        Max = 8,
    }
    private static SDL.GPUCompareOp Compare(CompareOperator value) => value is >= CompareOperator.Never and < CompareOperator.Max ? (SDL.GPUCompareOp)((int)value + 1) : throw new ArgumentOutOfRangeException(nameof(value));
    private static SDL.GPUStencilOp Stencil(StencilOperation value) => value is >= StencilOperation.Keep and < StencilOperation.Max ? (SDL.GPUStencilOp)((int)value + 1) : throw new ArgumentOutOfRangeException(nameof(value));
    private static bool DepthFormat(DataFormat format) => format is DataFormat.D16UNORM or DataFormat.X8D24UNORMPACK32 or DataFormat.D32SFLOAT or DataFormat.D16UNORMS8UINT or DataFormat.D24UNORMS8UINT or DataFormat.D32SFLOATS8UINT;
    private static bool StencilFormat(DataFormat format) => format is DataFormat.D24UNORMS8UINT or DataFormat.D32SFLOATS8UINT;
    private static SDL.GPUDepthStencilState DepthState(RDPipelineDepthStencilState state, RDPipelineRasterizationState raster, FramebufferLayout target, out byte reference)
    {
        var enabled = state.EnableDepthTest || state.EnableDepthWrite || state.EnableStencil;
        if (enabled && target.DepthIndex < 0) throw new ArgumentException("Depth/stencil state requires a depth attachment.", nameof(state));
        if (state.EnableStencil && !StencilFormat(target.Formats[target.DepthIndex])) throw new ArgumentException("Stencil testing requires a stencil aspect.", nameof(state));
        var frontReference = state.FrontOpReference; var backReference = state.BackOpReference;
        if (state.EnableStencil && (raster.CullMode != PolygonCullMode.Front && frontReference is < 0 or > 255 || raster.CullMode != PolygonCullMode.Back && backReference is < 0 or > 255)) throw new ArgumentOutOfRangeException(nameof(state), "Stencil reference must fit eight bits.");
        var frontMask = unchecked((byte)state.FrontOpCompareMask); var backMask = unchecked((byte)state.BackOpCompareMask);
        var frontWrite = unchecked((byte)state.FrontOpWriteMask); var backWrite = unchecked((byte)state.BackOpWriteMask);
        // shortcut: distinct visible-face masks/references need a native per-face state adapter.
        if (state.EnableStencil && raster.CullMode == PolygonCullMode.Disabled && (frontReference != backReference || frontMask != backMask || frontWrite != backWrite)) throw new NotSupportedException("Both visible faces require equal effective stencil masks and references on this backend.");
        var back = raster.CullMode == PolygonCullMode.Front;
        reference = (byte)(back ? backReference : frontReference);
        return new()
        {
            CompareOp = Compare(state.DepthCompareOperator),
            EnableDepthTest = state.EnableDepthTest,
            EnableDepthWrite = state.EnableDepthWrite,
            EnableStencilTest = state.EnableStencil,
            CompareMask = back ? backMask : frontMask,
            WriteMask = back ? backWrite : frontWrite,
            FrontStencilState = new() { CompareOp = Compare(state.FrontOpCompare), FailOp = Stencil(state.FrontOpFail), PassOp = Stencil(state.FrontOpPass), DepthFailOp = Stencil(state.FrontOpDepthFail) },
            BackStencilState = new() { CompareOp = Compare(state.BackOpCompare), FailOp = Stencil(state.BackOpFail), PassOp = Stencil(state.BackOpPass), DepthFailOp = Stencil(state.BackOpDepthFail) }
        };
    }
    private void InitializeDepth(Entry entry)
    {
        var info = entry.Texture!;
        for (uint layer = 0; layer < info.Layers; layer++)
            for (uint mip = 0; mip < info.Mips; mip++)
            {
                var depth = new SDL.GPUDepthStencilTargetInfo { Texture = entry.NativeHandle, Layer = (byte)layer, MipLevel = (byte)mip, ClearDepth = 0, ClearStencil = 0, LoadOp = SDL.GPULoadOp.Clear, StencilLoadOp = SDL.GPULoadOp.Clear, StoreOp = SDL.GPUStoreOp.Store, StencilStoreOp = SDL.GPUStoreOp.Store };
                var pass = SDL.BeginGPURenderPass(Commands(), 0, 0, (nint)(&depth));
                if (pass == 0) throw GPUPhysicsDevice.Failure("initialize depth attachment");
                SDL.EndGPURenderPass(pass);
            }
    }
}
