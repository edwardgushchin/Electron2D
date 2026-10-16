using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private readonly Dictionary<Resource, RID> _rendererTextures = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<GPUTexture> _rendererUploads = [];
    internal RID BorrowRendererTexture(Resource source, GPUTexture? pixels, CanvasRenderTarget? target)
    {
        IdleList();
        if (_rendererTextures.TryGetValue(source, out var cached) && _entries.TryGetValue(cached, out var existing) && existing.Borrowed?.Alive == true) { PrepareRendererTexture(existing); return cached; }
        var width = (uint)(target?.Size.X ?? pixels!.Pixels.Source.Width); var height = (uint)(target?.Size.Y ?? pixels!.Pixels.Source.Height);
        var mips = (uint)(target is not null ? 1 : pixels!.Pixels.Levels); var layers = (uint)(pixels?.ArrayPixels?.Layers.Length ?? 1);
        var format = pixels?.NativeTextureFormat switch
        {
            SDL.GPUTextureFormat.BC1RGBAUnorm => DataFormat.BC1RGBAUNORMBLOCK,
            SDL.GPUTextureFormat.BC2RGBAUnorm => DataFormat.BC2UNORMBLOCK,
            SDL.GPUTextureFormat.BC3RGBAUnorm => DataFormat.BC3UNORMBLOCK,
            SDL.GPUTextureFormat.BC4RUnorm => DataFormat.BC4UNORMBLOCK,
            SDL.GPUTextureFormat.BC5RGUnorm => DataFormat.BC5UNORMBLOCK,
            SDL.GPUTextureFormat.BC7RGBAUnorm => DataFormat.BC7UNORMBLOCK,
            SDL.GPUTextureFormat.BC6HRGBFloat => DataFormat.BC6HSFLOATBLOCK,
            SDL.GPUTextureFormat.BC6HRGBUFloat => DataFormat.BC6HUFLOATBLOCK,
            SDL.GPUTextureFormat.R32G32B32A32Float => DataFormat.R32G32B32A32SFLOAT,
            SDL.GPUTextureFormat.R16G16B16A16Uint => DataFormat.R16G16B16A16UINT,
            _ => DataFormat.R8G8B8A8UNORM
        };
        var usage = pixels?.DeviceUsage ?? TextureUsageBits.Sampling | TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | TextureUsageBits.CanUpdate;
        if (target is not null && TextureIsFormatSupportedForUsage(format, usage | TextureUsageBits.Storage)) usage |= TextureUsageBits.Storage;
        var native = NativeFormat(format); var offsets = new uint[mips]; var packed = new uint[mips]; var sizes = new uint[mips]; uint total = 0, transferSize = 0; var alignment = Math.Max(4u, SDL.GPUTextureFormatTexelBlockSize(native));
        for (var mip = 0; mip < mips; mip++) { packed[mip] = total; offsets[mip] = checked((transferSize + alignment - 1) / alignment * alignment); sizes[mip] = TextureBytes(native, Math.Max(1, width >> mip), Math.Max(1, height >> mip)); total = checked(total + sizes[mip]); transferSize = checked(offsets[mip] + sizes[mip]); }
        var borrow = target is not null ? new RendererTextureBorrow((ViewportTexture)source, target) : new RendererTextureBorrow(source, pixels!);
        var entry = new Entry { Kind = Kind.Texture, Borrowed = borrow, Size = total, Texture = new(format, layers > 1 || source is TextureLayered ? TextureType.TextureArray : TextureType.Texture, width, height, layers, mips, usage, offsets, packed, sizes, total, transferSize, [format]) };
        try { PrepareRendererTexture(entry); var rid = Add(entry); _rendererTextures[source] = rid; return rid; }
        catch { borrow.Dispose(); throw; }
    }
    private void PrepareRendererTexture(Entry entry)
    {
        var borrow = entry.Borrowed; if (borrow is null) return;
        if (!borrow.Alive) throw new ArgumentException("The borrowed renderer texture allocation expired.");
        borrow.RefreshPixels(); var pixels = borrow.Pixels;
        if (pixels?.NeedsRecording != true) return;
        if (_drawPass != 0) throw new InvalidOperationException("Prepare authored texture updates before beginning a draw list.");
        pixels.Upload(Commands()); pixels.RecordUpload(); _rendererUploads.Add(pixels);
    }
    private void RetireRendererTextures()
    {
        if (_command != 0 || _fence != 0 || _recordedReadbacks.Count != 0) return;
        foreach (var pair in _entries)
        {
            var entry = pair.Value; if (entry.Borrowed is not { } borrow || borrow.Alive) continue;
            if (_rendererTextures.GetValueOrDefault(borrow.Source) == pair.Key) _rendererTextures.Remove(borrow.Source);
            entry.Expired = true; entry.Borrowed = null; borrow.Dispose();
        }
    }
    private void CommitRendererUploads() { foreach (var pixels in _rendererUploads) pixels.CommitUpload(); _rendererUploads.Clear(); }
    private void CancelRendererUploads() { foreach (var pixels in _rendererUploads) pixels.CancelUpload(); _rendererUploads.Clear(); }
}
