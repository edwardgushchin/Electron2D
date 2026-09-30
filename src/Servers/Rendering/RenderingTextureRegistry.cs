namespace Electron2D;

internal static class RenderingTextureRegistry
{
    // ponytail: one registry gate serializes RID lookup; partition by owner if measured contention warrants it.
    private static readonly object Gate = new();
    private static readonly Dictionary<RID, Entry> Entries = [];
    private static readonly List<RID> Stale = [];
    private static int _registrationsSinceSweep;
    private static readonly Lazy<TexturePixels> Placeholder = new(CreatePlaceholderPixels);
    internal static TexturePixels PlaceholderPixels => Placeholder.Value;

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

internal sealed class ServerTexture(TexturePixels pixels) : Texture
{
    private TexturePixels? _pixels = pixels;
    internal TexturePixels Pixels { get { ThrowIfDisposed(); return _pixels!; } set => _pixels = value; }
    internal Vector2i Size = new(pixels.Source.Width, pixels.Source.Height);
    internal string Path = string.Empty;
    internal bool Released;
    private RID _rid;

    internal void Bind(RID rid) => _rid = rid;
    public override RID GetRID() { ThrowIfDisposed(); return _rid; }
    public override int GetWidth() { ThrowIfDisposed(); return Size.X; }
    public override int GetHeight() { ThrowIfDisposed(); return Size.Y; }
    public override Vector2 GetSize() { ThrowIfDisposed(); return new(Size.X, Size.Y); }
    public override Image GetImage() { ThrowIfDisposed(); return Pixels.CopyImage(); }
    internal override TexturePixels CapturePixels() { ThrowIfDisposed(); return Pixels; }

    protected override void ValidateDisposal()
    {
        if (!Released) throw new InvalidOperationException("The rendering server owns this texture.");
        base.ValidateDisposal();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _pixels = null; Size = default; Path = string.Empty; }
        base.Dispose(disposing);
    }
}
