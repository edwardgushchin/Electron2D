namespace Electron2D;

public sealed partial class TileSetAtlasSource
{
    private bool _usePadding = true, _paddingDirty = true;
    private Texture? _padded;
    private Texture[] _paddedChannels = [];
    /// <summary>Gets or sets one-pixel edge/corner extrusion in the runtime atlas, enabled by default.</summary>
    /// <remarks>The source-owned runtime texture is rebuilt lazily after layout or original pixel changes. Original texture pixels are never mutated.</remarks>
    public bool UseTexturePadding { get { ThrowIfDisposed(); return _usePadding; } set { ThrowIfDisposed(); if (_usePadding == value) return; _usePadding = value; _paddingDirty = true; EmitChanged(); } }
    /// <summary>Returns the borrowed prepared runtime texture, or null without readable pixels.</summary>
    /// <remarks>With padding disabled this returns the original. A padded texture is source-owned, transient and invalidated after a later successful preparation or source disposal. Preparation allocates; unchanged calls reuse it.</remarks>
    public Texture? GetRuntimeTexture()
    {
        ThrowIfDisposed(); if (!_usePadding) { ReleasePadding(); return _texture; }
        if (!_paddingDirty && (_padded is null || !_padded.IsDisposed)) return _padded;
        var prepared = BuildPadding(); ReleasePadding(); _padded = prepared.Texture; _paddedChannels = prepared.Channels; _paddingDirty = false; return _padded;
    }
    /// <summary>Returns a frame's pixel region inside GetRuntimeTexture.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinate.</param><param name="frame">Zero-based animation frame.</param>
    public Rect2i GetRuntimeTileTextureRegion(Vector2i atlasCoords, int frame)
    {
        var rect = GetTileTextureRegion(atlasCoords, frame); if (!_usePadding) return rect;
        var coords = FrameCoords(atlasCoords, GetTile(atlasCoords), frame); var stride = PaddingStride();
        return new(new(checked(coords.X * stride.X + 1), checked(coords.Y * stride.Y + 1)), rect.Size);
    }
    private Vector2i PaddingStride() => new(checked(_regionSize.X + Math.Max(2, _separation.X)), checked(_regionSize.Y + Math.Max(2, _separation.Y)));
    private void ReleasePadding() { _padded?.Dispose(); _padded = null; foreach (var channel in _paddedChannels) channel.Dispose(); _paddedChannels = []; }
    private (Texture? Texture, Texture[] Channels) BuildPadding()
    {
        if (_texture is not CanvasTexture canvas) return (BuildPaddingChannel(_texture), []);
        var channels = new List<Texture>(3);
        CanvasTexture? prepared = null;
        try
        {
            prepared = new CanvasTexture();
            prepared.DiffuseTexture = BuildPaddingChannel(canvas.DiffuseTexture);
            if (prepared.DiffuseTexture is { } diffuse) channels.Add(diffuse);
            prepared.NormalTexture = BuildPaddingChannel(canvas.NormalTexture);
            if (prepared.NormalTexture is { } normal) channels.Add(normal);
            prepared.SpecularTexture = BuildPaddingChannel(canvas.SpecularTexture);
            if (prepared.SpecularTexture is { } specular) channels.Add(specular);
            prepared.SpecularColor = canvas.SpecularColor; prepared.SpecularShininess = canvas.SpecularShininess;
            prepared.TextureFilter = canvas.TextureFilter; prepared.TextureRepeat = canvas.TextureRepeat;
            return (prepared, channels.ToArray());
        }
        catch { prepared?.Dispose(); foreach (var channel in channels) channel.Dispose(); throw; }
    }
    private ImageTexture? BuildPaddingChannel(Texture? texture)
    {
        if (texture is null) return null;
        using var original = texture.GetImage(); if (original is null || original.IsEmpty) return null;
        if (original.IsCompressed) throw new NotSupportedException("Compressed texture decoding is not integrated.");
        var grid = GetAtlasGridSize(); if (grid.X == 0 || grid.Y == 0) return null;
        var stride = PaddingStride(); var width = checked(grid.X * stride.X); var height = checked(grid.Y * stride.Y);
        if (width > 16384 || height > 16384) throw new ArgumentException("The padded atlas exceeds the 16384-pixel texture axis bound.");
        var input = original.CopyPixels(); var bytesPerPixel = Image.TexturePixelByteSize(input.Format); var pixels = new byte[checked(width * height * bytesPerPixel)];
        foreach (var pair in _tiles) for (var frame = 0; frame < pair.Value.Durations.Length; frame++)
            {
                var source = GetTileTextureRegion(pair.Key, frame); var target = GetRuntimeTileTextureRegion(pair.Key, frame);
                // Authored frames may survive a texture shrink; they retain data but produce no padded pixels until repaired.
                if ((long)source.Position.X + source.Size.X > input.Width || (long)source.Position.Y + source.Size.Y > input.Height) continue;
                for (var y = -1; y <= source.Size.Y; y++)
                {
                    var sourceY = source.Position.Y + Math.Clamp(y, 0, source.Size.Y - 1);
                    for (var x = -1; x <= source.Size.X; x++)
                    {
                        var sourceX = source.Position.X + Math.Clamp(x, 0, source.Size.X - 1);
                        var from = checked((sourceY * input.Width + sourceX) * bytesPerPixel); var to = checked(((target.Position.Y + y) * width + target.Position.X + x) * bytesPerPixel);
                        input.Data.AsSpan(from, bytesPerPixel).CopyTo(pixels.AsSpan(to, bytesPerPixel));
                    }
                }
            }
        using var image = Image.CreateFromData(width, height, false, input.Format, pixels); return ImageTexture.CreateFromImage(image);
    }
}
