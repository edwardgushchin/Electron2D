namespace Electron2D;

internal static class RenderingTextureRegistry
{
    // ponytail: one registry gate serializes RID lookup; partition by owner if measured contention warrants it.
    private static readonly object Gate = new();
    private static readonly Dictionary<RID, Entry> Entries = [];
    private static readonly List<RID> Stale = [];
    private static int _registrationsSinceSweep;
    private static readonly Lazy<TexturePixels> Placeholder = new(CreatePlaceholderPixels);
    private static readonly Lazy<ServerTexture> RenderPlaceholder = new(() => new(PlaceholderPixels));
    internal static TexturePixels PlaceholderPixels => Placeholder.Value;
    internal static Texture PlaceholderTexture => RenderPlaceholder.Value;

    private static TexturePixels CreatePlaceholderPixels()
    {
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++) image.SetPixel(x, y, (x + y) % 2 == 0 ? Colors.Magenta : Colors.Black);
        return TexturePixels.FromImage(image);
    }
    private sealed class Entry(Texture texture, RenderingServer? owner)
    {
        internal readonly WeakReference<Texture> Borrowed = new(texture);
        internal readonly Texture? Owned = owner is null ? null : texture;
        internal readonly RenderingServer? Owner = owner;
    }

    internal static RID Register(Texture texture, RenderingServer? owner = null)
    {
        var rid = RID.Allocate();
        lock (Gate)
        {
            if (++_registrationsSinceSweep >= 256)
            {
                _registrationsSinceSweep = 0; Stale.Clear();
                foreach (var pair in Entries)
                    if (!pair.Value.Borrowed.TryGetTarget(out var target) || target.IsDisposed) Stale.Add(pair.Key);
                foreach (var stale in Stale) Entries.Remove(stale);
                Stale.Clear();
            }
            Entries.Add(rid, new(texture, owner));
        }
        return rid;
    }

    internal static Texture Resolve(RID rid)
    {
        lock (Gate)
        {
            if (Entries.TryGetValue(rid, out var entry) && entry.Borrowed.TryGetTarget(out var texture) && !texture.IsDisposed)
                return texture;
            Entries.Remove(rid);
            throw new ArgumentException("The RID does not identify a live rendering texture.", nameof(rid));
        }
    }

    internal static Texture? ResolveProxySource(RID rid)
    {
        lock (Gate)
        {
            while (Entries.TryGetValue(rid, out var entry) && entry.Borrowed.TryGetTarget(out var texture) && !texture.IsDisposed)
            {
                if (texture is not ServerTexture { IsProxy: true } proxy) return texture;
                rid = proxy.ProxyTarget;
            }
            return null;
        }
    }

    internal static ServerTexture Owned(RID rid, RenderingServer owner)
    {
        lock (Gate)
        {
            if (!Entries.TryGetValue(rid, out var entry) || !entry.Borrowed.TryGetTarget(out var live) || live.IsDisposed)
                throw new ArgumentException("The texture RID is not live.", nameof(rid));
            if (entry.Owner != owner || entry.Owned is not ServerTexture texture)
                throw new InvalidOperationException("The texture RID belongs to another rendering or resource owner.");
            return texture;
        }
    }

    internal static void Remove(RID rid) { lock (Gate) Entries.Remove(rid); }
}

internal sealed class ServerTexture : Texture
{
    internal ServerTexture(TexturePixels pixels)
    {
        _pixels = pixels; Size = new(pixels.Source.Width, pixels.Source.Height); RetainRendererCache = true;
    }
    internal ServerTexture(RID target, Texture source)
    {
        IsProxy = true; SetProxyTarget(target, source);
    }
    private TexturePixels? _pixels;
    internal TexturePixels Pixels { get { ThrowIfDisposed(); return _pixels!; } set => _pixels = value; }
    internal Vector2i Size;
    internal string Path = string.Empty;
    internal bool IsProxy { get; }
    internal RID ProxyTarget { get; private set; }
    private Image.Format _proxyFormat;
    private bool _proxySizeOverride;
    internal bool Released;
    private RID _rid;

    internal void Bind(RID rid) => _rid = rid;
    public override RID GetRID() { ThrowIfDisposed(); return _rid; }
    public override int GetWidth()
    {
        ThrowIfDisposed(); var source = IsProxy ? RenderingTextureRegistry.ResolveProxySource(ProxyTarget) : null;
        return source is null || _proxySizeOverride ? Size.X : source.GetSize() == Vector2.Zero ? 4 : source.GetWidth();
    }
    public override int GetHeight()
    {
        ThrowIfDisposed(); var source = IsProxy ? RenderingTextureRegistry.ResolveProxySource(ProxyTarget) : null;
        return source is null || _proxySizeOverride ? Size.Y : source.GetSize() == Vector2.Zero ? 4 : source.GetHeight();
    }
    public override Vector2 GetSize()
    {
        ThrowIfDisposed();
        return IsProxy && !_proxySizeOverride && RenderingTextureRegistry.ResolveProxySource(ProxyTarget) is { } source
            ? ProxySize(source) : new(Size.X, Size.Y);
    }
    public override Image? GetImage() => IsProxy && RenderingTextureRegistry.ResolveProxySource(ProxyTarget) is ViewportTexture view ? view.GetImage() : CapturePixels()?.CopyImage();
    public override Image.Format PixelFormat => IsProxy ? CapturePixels()?.Source.Format ?? _proxyFormat : base.PixelFormat;
    internal override TexturePixels? CapturePixels()
    {
        ThrowIfDisposed();
        return IsProxy ? RenderingTextureRegistry.ResolveProxySource(ProxyTarget)?.CapturePixels() : Pixels;
    }
    internal void SetProxyTarget(RID target, Texture source)
    {
        ProxySize(source);
        var size = new Vector2i(source.GetWidth(), source.GetHeight());
        if (size == Vector2i.Zero) size = new(4, 4);
        var format = source.CapturePixels()?.Source.Format ?? Image.Format.Rgba8;
        var path = source is ServerTexture owned ? owned.Path : source.ResourcePath;
        ThrowIfDisposed();
        RenderingTextureRegistry.Resolve(target);
        ProxyTarget = target; Size = size; _proxyFormat = format; Path = path; _proxySizeOverride = false;
    }
    internal void RedirectProxy(RID target) => ProxyTarget = target;
    internal void SetDisplaySize(Vector2i size) { Size = size; _proxySizeOverride = IsProxy; }
    private static Vector2 ProxySize(Texture source)
    {
        var size = source.GetSize();
        if (!size.IsFinite() || size.X < 0 || size.Y < 0)
            throw new InvalidOperationException("Proxy source dimensions must be finite and nonnegative.");
        return size == Vector2.Zero ? new(4, 4) : size;
    }

    protected override void ValidateDisposal()
    {
        if (!Released) throw new InvalidOperationException("The rendering server owns this texture.");
        base.ValidateDisposal();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _pixels = null; Size = default; Path = string.Empty; ProxyTarget = default; }
        base.Dispose(disposing);
    }
}
