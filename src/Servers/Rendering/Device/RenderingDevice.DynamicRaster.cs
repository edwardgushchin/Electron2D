using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private const PipelineDynamicStateFlags DynamicRasterFlags =
        PipelineDynamicStateFlags.LineWidth | PipelineDynamicStateFlags.DepthBias | PipelineDynamicStateFlags.DepthBounds;

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativeRasterValues(float Width, float Constant, float Clamp, float Slope, float Minimum, float Maximum);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRasterCommands
    {
        private nint _lineWidth, _depthBias, _depthBounds;
    }

    private NativeRasterCommands _rasterCommands;
    private NativeRasterValues _drawRaster;
    private GPUDeviceFactory.LineLimits _dynamicLineLimits;
    private bool? _dynamicRasterSupported;

    private void ValidateDynamicRasterPipeline(PipelineDynamicStateFlags flags, RDPipelineRasterizationState raster, NativeDepthStencilState depth, FramebufferPassLayout target)
    {
        if ((flags & PipelineDynamicStateFlags.DepthBias) != 0 &&
            (!raster.DepthBiasEnabled || target.DepthIndex < 0 || target.Formats[target.DepthIndex] == DataFormat.S8UINT))
            throw new ArgumentException("Dynamic depth bias requires enabled bias and a depth attachment.", nameof(raster));
        if ((flags & PipelineDynamicStateFlags.DepthBounds) != 0 && (depth.Flags & 1) == 0)
            throw new ArgumentException("Dynamic depth bounds require enabled depth bounds.", nameof(depth));
        if (_dynamicRasterSupported is null)
        {
            try { _dynamicRasterSupported = NativeDrawRaster(Device, 1, 0, 0, in _drawRaster, ref _rasterCommands) == 1; }
            catch (EntryPointNotFoundException) { _dynamicRasterSupported = false; }
            if (_dynamicRasterSupported == true && SDL.GetBooleanProperty(SDL.GetGPUDeviceProperties(Device), GPUDeviceFactory.WideLinesProperty, false) &&
                GPUDeviceFactory.ReadLineSupport(Device, 1, out var limits) == 1 &&
                float.IsFinite(limits.Minimum) && float.IsFinite(limits.Maximum) && limits.Minimum > 0 && limits.Maximum >= limits.Minimum)
                _dynamicLineLimits = limits;
        }
        if (_dynamicRasterSupported != true) throw new NotSupportedException("Dynamic raster state requires the current Vulkan command adapter.");
        if ((flags & PipelineDynamicStateFlags.LineWidth) != 0) _ = LimitLineWidth(raster.LineWidth);
    }

    private float LimitLineWidth(float width)
    {
        if (width == 1) return 1;
        if (_dynamicLineLimits.Minimum == 0) throw new NotSupportedException("Nonunit line width requires native wide-line rasterization.");
        return Math.Clamp(width, _dynamicLineLimits.Minimum, _dynamicLineLimits.Maximum);
    }

    private void ValidateDepthBiasClamp(float clamp)
    {
        if (clamp != 0 && _context.Driver == "vulkan" &&
            !SDL.GetBooleanProperty(SDL.GetGPUDeviceProperties(Device), GPUDeviceFactory.DepthBiasClampProperty, false))
            throw new NotSupportedException("Nonzero depth bias clamp requires the independently enabled native feature.");
    }

    /// <summary>Sets the raster line width in pixels for subsequent draws.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="width">Finite positive width. Nonunit widths clamp to the enabled native range.</param>
    /// <exception cref="ArgumentException">The draw list is not active.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Width is nonpositive or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The bound pipeline lacks dynamic line width or the caller is not the owner.</exception>
    /// <exception cref="NotSupportedException">The device lacks wide lines and the width is nonunit.</exception>
    public void DrawListSetLineWidth(long drawList, float width)
    {
        ValidateDynamicState(drawList, PipelineDynamicStateFlags.LineWidth);
        if (!float.IsFinite(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        _drawRaster = _drawRaster with { Width = LimitLineWidth(width) };
    }

    /// <summary>Sets depth bias factors and the signed clamp for subsequent draws.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="constantFactor">Finite bias multiplier for the attachment depth precision.</param>
    /// <param name="clamp">Finite maximum positive or minimum negative bias. Zero disables clamping.</param>
    /// <param name="slopeFactor">Finite bias multiplier for the maximum fragment depth slope.</param>
    /// <exception cref="ArgumentException">The draw list is not active.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The bound pipeline lacks dynamic depth bias or the caller is not the owner.</exception>
    /// <exception cref="NotSupportedException">The device lacks the feature required for nonzero clamp.</exception>
    public void DrawListSetDepthBias(long drawList, float constantFactor, float clamp, float slopeFactor)
    {
        ValidateDynamicState(drawList, PipelineDynamicStateFlags.DepthBias);
        if (!float.IsFinite(constantFactor)) throw new ArgumentOutOfRangeException(nameof(constantFactor));
        if (!float.IsFinite(clamp)) throw new ArgumentOutOfRangeException(nameof(clamp));
        if (!float.IsFinite(slopeFactor)) throw new ArgumentOutOfRangeException(nameof(slopeFactor));
        ValidateDepthBiasClamp(clamp);
        _drawRaster = _drawRaster with { Constant = constantFactor, Clamp = clamp, Slope = slopeFactor };
    }

    /// <summary>Sets the inclusive range for the stored depth test on subsequent draws.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="minimum">Finite lower bound from zero through one.</param>
    /// <param name="maximum">Finite upper bound from the minimum through one.</param>
    /// <exception cref="ArgumentException">The draw list is not active.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The bounds are nonfinite, outside zero through one, or reversed.</exception>
    /// <exception cref="InvalidOperationException">The bound pipeline lacks dynamic depth bounds or the caller is not the owner.</exception>
    public void DrawListSetDepthBounds(long drawList, float minimum, float maximum)
    {
        ValidateDynamicState(drawList, PipelineDynamicStateFlags.DepthBounds);
        if (!float.IsFinite(minimum) || minimum is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(minimum));
        if (!float.IsFinite(maximum) || maximum < minimum || maximum > 1) throw new ArgumentOutOfRangeException(nameof(maximum));
        _drawRaster = _drawRaster with { Minimum = minimum, Maximum = maximum };
    }

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_dynamic_raster")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeDrawRaster(nint device, uint abi, nint commands, PipelineDynamicStateFlags flags, in NativeRasterValues values, ref NativeRasterCommands functions);
}
