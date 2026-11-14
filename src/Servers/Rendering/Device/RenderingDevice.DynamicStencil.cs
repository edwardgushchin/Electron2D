using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private const PipelineDynamicStateFlags DynamicStencilFlags =
        PipelineDynamicStateFlags.StencilCompareMask | PipelineDynamicStateFlags.StencilWriteMask | PipelineDynamicStateFlags.StencilReference;
    private NativeDepthStencilState _drawStencil;
    private bool? _dynamicStencilSupported;
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeStencilCommands
    {
        private nint _compareMask, _writeMask;
    }
    private NativeStencilCommands _stencilCommands;

    private void RequireDynamicStencil(in NativeDepthStencilState state)
    {
        if (_dynamicStencilSupported is null)
        {
            try { _dynamicStencilSupported = NativeDrawStencil(Device, 1, 0, in state, ref _stencilCommands) == 1; }
            catch (EntryPointNotFoundException) { _dynamicStencilSupported = false; }
        }
        if (_dynamicStencilSupported != true) throw new NotSupportedException("Dynamic stencil requires the current Vulkan command adapter.");
    }

    private void ValidateDynamicStencil(long drawList, PipelineDynamicStateFlags flag)
    {
        DrawList(drawList);
        if (_graphicsPipeline is null || (_graphicsPipeline.Graphics!.Dynamic & flag) == 0)
            throw new InvalidOperationException("Bind a pipeline with the requested dynamic stencil state first.");
    }

    /// <summary>Sets the front and back stencil comparison masks for subsequent draws.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="frontMask">Front-face mask. Only the low eight bits are used.</param>
    /// <param name="backMask">Back-face mask. Only the low eight bits are used.</param>
    /// <exception cref="ArgumentException">The draw list is not active.</exception>
    /// <exception cref="InvalidOperationException">The bound pipeline does not enable dynamic stencil comparison masks, or the caller is not the owner.</exception>
    public void DrawListSetStencilCompareMask(long drawList, int frontMask, int backMask)
    {
        ValidateDynamicStencil(drawList, PipelineDynamicStateFlags.StencilCompareMask);
        _drawStencil = _drawStencil with { FrontMask = unchecked((byte)frontMask), BackMask = unchecked((byte)backMask) };
    }

    /// <summary>Sets the front and back stencil write masks for subsequent draws.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="frontMask">Front-face mask. Only the low eight bits are used.</param>
    /// <param name="backMask">Back-face mask. Only the low eight bits are used.</param>
    /// <exception cref="ArgumentException">The draw list is not active.</exception>
    /// <exception cref="InvalidOperationException">The bound pipeline does not enable dynamic stencil write masks, or the caller is not the owner.</exception>
    public void DrawListSetStencilWriteMask(long drawList, int frontMask, int backMask)
    {
        ValidateDynamicStencil(drawList, PipelineDynamicStateFlags.StencilWriteMask);
        _drawStencil = _drawStencil with { FrontWrite = unchecked((byte)frontMask), BackWrite = unchecked((byte)backMask) };
    }

    /// <summary>Sets the front and back stencil references for subsequent draws.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="frontReference">Front-face reference between zero and 255.</param>
    /// <param name="backReference">Back-face reference between zero and 255.</param>
    /// <exception cref="ArgumentException">The draw list is not active.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Either reference is outside zero through 255.</exception>
    /// <exception cref="InvalidOperationException">The bound pipeline does not enable dynamic stencil references, or the caller is not the owner.</exception>
    public void DrawListSetStencilReference(long drawList, int frontReference, int backReference)
    {
        ValidateDynamicStencil(drawList, PipelineDynamicStateFlags.StencilReference);
        if ((uint)frontReference > byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(frontReference));
        if ((uint)backReference > byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(backReference));
        _drawStencil = _drawStencil with { FrontReference = (uint)frontReference, BackReference = (uint)backReference };
    }

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_dynamic_stencil")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeDrawStencil(nint device, uint abi, nint commands, in NativeDepthStencilState state, ref NativeStencilCommands functions);
}
