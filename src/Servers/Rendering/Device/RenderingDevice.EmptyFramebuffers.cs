using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private int? _emptySamples;
    /// <summary>Creates a single-pass framebuffer format without image attachments.</summary>
    /// <param name="samples">The exact raster sample count.</param>
    /// <returns>A device-local format identity, reused for the same sample count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The sample value is invalid.</exception>
    /// <exception cref="NotSupportedException">The native backend cannot execute this attachmentless sample count.</exception>
    public int FramebufferFormatCreateEmpty(TextureSamples samples = TextureSamples.Samples1)
    {
        IdleList(); ValidateEmptySamples(samples);
        return RetainFramebufferFormat(new([new([], [], [], [], [], -1, [], samples, [], [])], 1));
    }
    /// <summary>Creates a framebuffer with an explicit raster extent and no image attachments.</summary>
    /// <param name="size">Positive framebuffer dimensions within device limits.</param>
    /// <param name="samples">The exact raster sample count.</param>
    /// <param name="validateWithFormat">A matching empty format identity, or -1 to select it automatically.</param>
    /// <returns>An owned framebuffer identity released with FreeRID after pending work completes.</returns>
    /// <remarks>Vertex and fragment shaders can produce storage-buffer output without allocating dummy images.
    /// Fragment color outputs and depth/stencil tests require their corresponding attachments.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Dimensions or sample values are invalid.</exception>
    /// <exception cref="ArgumentException">The supplied format has attachments or a different sample count.</exception>
    /// <exception cref="NotSupportedException">The native attachmentless backend or exact sample count is unavailable.</exception>
    public RID FramebufferCreateEmpty(Vector2i size, TextureSamples samples = TextureSamples.Samples1, int validateWithFormat = -1)
    {
        IdleList(); ValidateEmptySamples(samples);
        if (size.X <= 0 || size.Y <= 0 || ValidationLimits is { } limits && ((ulong)size.X > limits[9] || (ulong)size.Y > limits[8])) throw new ArgumentOutOfRangeException(nameof(size));
        var format = validateWithFormat == -1 ? FramebufferFormatCreateEmpty(samples) : validateWithFormat;
        var layout = FramebufferLayouts(format);
        if (layout.ViewCount != 1 || layout.Passes.Length != 1 || layout.Passes[0].Formats.Length != 0 || layout.Passes[0].Samples != samples) throw new ArgumentException("Select an empty framebuffer format with the same sample count.", nameof(validateWithFormat));
        var native = NativeEmptyCreate(Device, 1, (uint)size.X, (uint)size.Y, (uint)samples);
        if (native == 0) throw GPUPhysicsDevice.Failure("create an attachmentless framebuffer");
        var handle = new RenderHandle(native, h => NativeEmptyRelease(Device, 1, h), _context.Handle);
        try { return Add(new() { Kind = Kind.Framebuffer, Handle = handle, Target = new([], format, size, native) { Visited = new bool[1] } }); }
        catch { handle.Dispose(); throw; }
    }
    private void ValidateEmptySamples(TextureSamples samples)
    {
        if (!Enum.IsDefined(samples) || samples == TextureSamples.Max) throw new ArgumentOutOfRangeException(nameof(samples));
        _emptySamples ??= NativeEmptySupport(Device, 1);
        if (_emptySamples <= 0 || (_emptySamples.Value & (1 << (int)samples)) == 0) throw new NotSupportedException("Attachmentless framebuffers require the native extent adapter and an available exact sample count.");
    }
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_empty_support")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeEmptySupport(nint device, uint abi);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_empty_create")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeEmptyCreate(nint device, uint abi, uint width, uint height, uint samples);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_empty_release")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void NativeEmptyRelease(nint device, uint abi, nint target);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_empty_begin")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeEmptyBegin(nint device, uint abi, nint commands, nint target);
}
