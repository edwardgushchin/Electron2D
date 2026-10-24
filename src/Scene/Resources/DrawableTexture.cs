namespace Electron2D;

/// <summary>A sampled texture whose pixels are produced by ordered GPU blit passes.</summary>
/// <remarks>Setup may run before renderer startup. Native storage is created on first GPU use and reset
/// in each renderer session. Duplication and archives retain the authored setup and clear color, not GPU results.
/// Source textures and materials remain borrowed. Compatibility rendering does not support this resource.</remarks>
public sealed class DrawableTexture : Texture
{
    /// <summary>The writable color storage format.</summary>
    public enum DrawableFormat
    {
        /// <summary>Four normalized eight-bit channels.</summary>
        RGBA8 = 0,
        /// <summary>Eight-bit sRGB color with linear shader sampling and blending.</summary>
        RGBA8SRGB = 1,
        /// <summary>Four sixteen-bit floating-point channels.</summary>
        RGBAH = 2,
        /// <summary>Four thirty-two-bit floating-point channels.</summary>
        RGBAF = 3,
    }

    private int _width, _height;
    private DrawableFormat _format;
    private bool _mipmaps, _blitting;
    private Color _clear = Colors.White;
    private RenderingDevice? _device;
    private RID _image;
    internal RenderingServer? ServerOwner;
    internal bool ServerReleased;
    internal string ServerPath = string.Empty;
    internal Vector2i? DisplaySize;

    /// <summary>Creates an uninitialized texture with zero dimensions.</summary>
    public DrawableTexture() { }

    /// <summary>Sets dimensions, format, clear color and mip allocation, replacing prior GPU contents.</summary>
    /// <param name="width">Width from one through 16384.</param>
    /// <param name="height">Height from one through 16384.</param>
    /// <param name="format">The writable color format.</param>
    /// <param name="color">Finite clear color; null selects opaque white.</param>
    /// <param name="useMIPMaps">Whether to allocate the complete mip chain.</param>
    /// <remarks>The logical RID remains stable. Active GPU creation validates before committing new configuration.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Dimensions or format are invalid.</exception>
    /// <exception cref="ArgumentException">The clear color is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during blit preparation or on another owner thread.</exception>
    /// <exception cref="NotSupportedException">The active GPU lacks the selected format.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public void Setup(int width, int height, DrawableFormat format, Color? color = null, bool useMIPMaps = false)
    {
        ValidateMutation();
        if (width is < 1 or > 16384 || height is < 1 or > 16384) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format));
        var clear = color ?? Colors.White; ValidateColor(clear);
        var device = RenderingServer.GetRenderingDevice();
        var image = device is null ? default : CreateImage(device, width, height, format, clear, useMIPMaps);
        var oldDevice = _device; var oldImage = _image;
        _width = width; _height = height; _format = format; _clear = clear; _mipmaps = useMIPMaps; _device = device; _image = image;
        ReleaseNativeImage(oldDevice, oldImage);
        EmitChanged(); NotifyPropertyListChanged();
    }

    /// <summary>Changes the format and resets initialized storage to its authored clear color.</summary>
    /// <param name="format">The writable color format.</param>
    /// <exception cref="ArgumentOutOfRangeException">The format is undefined.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during blit preparation or violates device access.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public void SetFormat(DrawableFormat format)
    {
        ValidateMutation(); if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format)); if (_format == format) return;
        if (_width != 0) Setup(_width, _height, format, _clear, _mipmaps); else { _format = format; EmitChanged(); }
    }

    /// <summary>Changes mip allocation and resets initialized storage to its authored clear color.</summary>
    /// <param name="mipmaps">Whether to allocate all mip levels.</param>
    /// <exception cref="InvalidOperationException">Mutation occurs during blit preparation or violates device access.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public void SetUseMIPMaps(bool mipmaps)
    {
        ValidateMutation(); if (_mipmaps == mipmaps) return;
        if (_width != 0) Setup(_width, _height, _format, _clear, mipmaps); else { _mipmaps = mipmaps; EmitChanged(); }
    }
    /// <summary>Reports whether the texture is configured with a complete mip chain.</summary>
    /// <returns>The authored mip allocation setting.</returns>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public bool GetUseMIPMaps() { ThrowIfDisposed(); return _mipmaps; }

    /// <summary>Scales a complete source into a destination rectangle on one mip level.</summary>
    /// <param name="rect">Destination pixels at the selected mip; clipping retains the original UV mapping. Negative size flips that axis.</param>
    /// <param name="source">Borrowed source; null or an atlas selects opaque white.</param>
    /// <param name="modulate">Finite modulation; null selects opaque white.</param>
    /// <param name="mipmap">Allocated destination mip level.</param>
    /// <param name="material">A BlitMaterial, a TextureBlit ShaderMaterial, or null for built-in mixing.</param>
    /// <exception cref="ArgumentException">A source aliases a destination, or targets disagree.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mip level is not allocated.</exception>
    /// <exception cref="InvalidOperationException">Setup, the GPU renderer or valid owner-thread access is absent.</exception>
    /// <exception cref="NotSupportedException">The material belongs to another shader domain.</exception>
    /// <exception cref="ObjectDisposedException">An input resource is disposed.</exception>
    public void BlitRect(Rect2i rect, Texture? source, Color? modulate = null, int mipmap = 0, Material? material = null)
    {
        var target = this;
        RenderingServer.BlitDrawableTextures(System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan(ref target, 1), rect,
            System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan(ref source, 1), modulate ?? Colors.White, mipmap, material);
    }

    /// <summary>Executes one fragment pass writing this texture and up to three extra targets.</summary>
    /// <remarks>Missing imported graph material images preserve their declared ordinary/array shape and
    /// render-only image fallback policy. This does not change the white policy for absent source slots.</remarks>
    /// <param name="rect">Destination rectangle in the selected mip level.</param>
    /// <param name="sources">Up to four borrowed sources; missing sources use opaque white. Atlas sources use opaque white.</param>
    /// <param name="extraTargets">Up to three distinct targets with equal actual dimensions, format and mip allocation.</param>
    /// <param name="modulate">Finite modulation; null selects opaque white.</param>
    /// <param name="mipmap">Destination mip level.</param>
    /// <param name="material">A built-in or programmable TextureBlit material.</param>
    /// <remarks>All inputs and aliases validate before drawing; no source may sample any output allocation.</remarks>
    /// <exception cref="ArgumentException">The sources, target family or aliases are invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A count or mip level exceeds its supported range.</exception>
    /// <exception cref="InvalidOperationException">No active GPU renderer exists or an input is uninitialized/reentrant.</exception>
    /// <exception cref="NotSupportedException">The material has another shader domain.</exception>
    /// <exception cref="ObjectDisposedException">An input resource is disposed.</exception>
    public void BlitRectMulti(Rect2i rect, ReadOnlySpan<Texture?> sources, ReadOnlySpan<DrawableTexture> extraTargets, Color? modulate = null, int mipmap = 0, Material? material = null)
    {
        if (extraTargets.Length > 3) throw new ArgumentOutOfRangeException(nameof(extraTargets));
        RenderingServer.BlitDrawableTextures(this, extraTargets, rect, sources, modulate ?? Colors.White, mipmap, material);
    }

    /// <summary>Regenerates all lower mip levels from current level-zero GPU pixels.</summary>
    /// <remarks>Does nothing when no lower levels are allocated. Requires initialized storage and the active GPU renderer.</remarks>
    /// <exception cref="InvalidOperationException">Setup or valid GPU owner access is absent.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public void GenerateMIPMaps() { ValidateMutation(); var image = EnsureImage(); _device!.GenerateDrawableMIPMaps(image); RenderingServer.NotifyRenderMutation(); }

    /// <inheritdoc />
    public override int GetWidth() { ThrowIfDisposed(); return DisplaySize?.X ?? _width; }
    /// <inheritdoc />
    public override int GetHeight() { ThrowIfDisposed(); return DisplaySize?.Y ?? _height; }
    /// <inheritdoc />
    public override Image.Format PixelFormat { get { ThrowIfDisposed(); return _format switch { DrawableFormat.RGBAH => Image.Format.Rgbah, DrawableFormat.RGBAF => Image.Format.Rgbaf, _ => Image.Format.Rgba8 }; } }
    /// <inheritdoc />
    public override bool HasAlpha { get { ThrowIfDisposed(); return false; } }
    /// <inheritdoc />
    public override bool HasMIPMaps { get { ThrowIfDisposed(); return Math.Max(_width, _height) > 1 && _mipmaps; } }
    /// <inheritdoc />
    public override int MIPMapCount { get { ThrowIfDisposed(); return HasMIPMaps ? System.Numerics.BitOperations.Log2((uint)Math.Max(_width, _height)) : 0; } }
    /// <summary>Synchronizes GPU work and returns independent pixels for every allocated mip level.</summary>
    /// <returns>A caller-owned image, or null before setup or without an active GPU renderer.</returns>
    /// <remarks>This is a cold explicit readback; normal canvas drawing samples native storage directly.</remarks>
    /// <exception cref="InvalidOperationException">Device access occurs on another thread or during submission.</exception>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    public override Image? GetImage()
    {
        ThrowIfDisposed(); if (_width == 0 || RenderingServer.GetRenderingDevice() is null) return null;
        var image = EnsureImage(); return _device!.CanvasImage(image, 0);
    }
    internal override TexturePixels? CapturePixels() { ThrowIfDisposed(); return null; }
    internal RenderingDevice.DataFormat NativeFormat => _format switch
    {
        DrawableFormat.RGBA8 => RenderingDevice.DataFormat.R8G8B8A8UNORM,
        DrawableFormat.RGBA8SRGB => RenderingDevice.DataFormat.R8G8B8A8SRGB,
        DrawableFormat.RGBAH => RenderingDevice.DataFormat.R16G16B16A16SFLOAT,
        _ => RenderingDevice.DataFormat.R32G32B32A32SFLOAT
    };
    internal Vector2i NativeSize => new(_width, _height);
    internal RID AllocatedImage => _image;
    internal RID EnsureImage()
    {
        ThrowIfDisposed(); var device = RenderingServer.GetRenderingDevice() ?? throw new InvalidOperationException("Writable textures require the active GPU renderer.");
        if (_width == 0 || _height == 0) throw new InvalidOperationException("Call Setup before using a writable texture.");
        if (!ReferenceEquals(_device, device) || device.CanvasInfo(_image) is null)
        { _image = CreateImage(device, _width, _height, _format, _clear, _mipmaps); _device = device; }
        return _image;
    }
    internal nint NativeTexture(nint device) { var image = EnsureImage(); return _device!.CanvasHandle(image, device); }
    private static RID CreateImage(RenderingDevice device, int width, int height, DrawableFormat format, Color clear, bool mipmaps)
    {
        using var description = new RDTextureFormat
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = format switch { DrawableFormat.RGBA8 => RenderingDevice.DataFormat.R8G8B8A8UNORM, DrawableFormat.RGBA8SRGB => RenderingDevice.DataFormat.R8G8B8A8SRGB, DrawableFormat.RGBAH => RenderingDevice.DataFormat.R16G16B16A16SFLOAT, _ => RenderingDevice.DataFormat.R32G32B32A32SFLOAT },
            MIPMaps = mipmaps ? (uint)(1 + System.Numerics.BitOperations.Log2((uint)Math.Max(width, height))) : 1,
            UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.CanCopyTo
        };
        using var view = new RDTextureView(); var image = device.TextureCreate(description, view);
        try { device.TextureClear(image, clear, 0, description.MIPMaps, 0, 1); return image; }
        catch { device.FreeRID(image); throw; }
    }
    internal void BeginBlit() { ValidateMutation(); _blitting = true; }
    internal void EndBlit() => _blitting = false;
    /// <inheritdoc />
    protected override void ValidateMutation() { ThrowIfDisposed(); if (_blitting) throw new InvalidOperationException("A writable texture cannot change while its storage is being prepared."); ServerOwner?.EnsureDeviceAccess(); if (_device is { IsDisposed: false }) _device.ValidateCanvasMutation(); }
    internal static void ValidateColor(Color color) { if (!float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B) || !float.IsFinite(color.A)) throw new ArgumentException("Color components must be finite."); }
    private void ReleaseImage()
    { var device = _device; var image = _image; _device = null; _image = default; ReleaseNativeImage(device, image); }
    private void ReleaseNativeImage(RenderingDevice? device, RID image)
    {
        if (device is not { IsDisposed: false } || !image.IsValid()) return;
        var guarded = _blitting; _blitting = true;
        try { device.FreeRID(image); } finally { _blitting = guarded; }
    }
    internal void EnsureCanRelease() => ValidateMutation();
    internal void NotifyStorageChanged() => EmitChanged();
    internal void TakeStorage(DrawableTexture source)
    {
        BeginBlit();
        try
        {
            source.BeginBlit();
            try
            {
                ReleaseImage();
                _device = source._device; _image = source._image; _width = source._width; _height = source._height;
                _format = source._format; _mipmaps = source._mipmaps; _clear = source._clear;
                ServerPath = source.ServerPath; DisplaySize = source.DisplaySize;
                source._device = null; source._image = default;
            }
            finally { source.EndBlit(); }
        }
        finally { EndBlit(); }
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new DrawableTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (DrawableTexture)target; copy.ValidateMutation(); copy.ReleaseImage(); copy._width = _width; copy._height = _height; copy._format = _format; copy._mipmaps = _mipmaps; copy._clear = _clear; }
    internal override void ValidateStateMutation() => ValidateMutation();
    /// <inheritdoc />
    protected override void OnResetState() { ValidateMutation(); ReleaseImage(); EmitChanged(); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<DrawableTexture, Vector2i>("setup_size", p => new(p._width, p._height), (p, v) => { if (v != Vector2i.Zero && (v.X is < 1 or > 16384 || v.Y is < 1 or > 16384)) throw new ArgumentOutOfRangeException(nameof(v)); p.ValidateMutation(); p.ReleaseImage(); p._width = v.X; p._height = v.Y; }, _ => Vector2i.Zero, stored: true),
        new PropertyDescriptor<DrawableTexture, DrawableFormat>("setup_format", p => p._format, (p, v) => p.SetFormat(v), _ => DrawableFormat.RGBA8, stored: true),
        new PropertyDescriptor<DrawableTexture, bool>("setup_mipmaps", p => p._mipmaps, (p, v) => p.SetUseMIPMaps(v), _ => false, stored: true),
        new PropertyDescriptor<DrawableTexture, Color>("setup_color", p => p._clear, (p, v) => { ValidateColor(v); p.ValidateMutation(); p.ReleaseImage(); p._clear = v; }, _ => Colors.White, stored: true)
    ]);
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (ServerOwner is not null && !ServerReleased) throw new InvalidOperationException("The rendering server owns this texture."); ValidateMutation(); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) ReleaseImage(); base.Dispose(disposing); }
}
