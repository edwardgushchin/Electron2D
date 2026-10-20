namespace Electron2D;

public abstract partial class TextServer
{
    private readonly object _fontsGate = new();
    private readonly Dictionary<RID, ServerFont> _fonts = [];

    /// <summary>Creates an owned empty font source and cache.</summary>
    /// <returns>A stable identity; release it with FreeRID.</returns>
    public RID CreateFont()
    {
        var file = new FontFile { MSDFPixelRange = 14 };
        try { return OwnFont(new ServerFont(this, file)); }
        catch { file.Dispose(); throw; }
    }
    /// <summary>Creates a linked font sharing the original source, configuration and cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A new owned identity with independent zero spacing and baseline offset.</returns>
    /// <exception cref="InvalidOperationException">The identity or its root has retired.</exception>
    public RID CreateFontLinkedVariation(RID fontRID)
    {
        lock (_fontsGate)
        {
            if (FontFile.TryResolveSource(fontRID, out var resource)) return OwnFont(new ServerFont(this, resource, fontRID));
            var root = FontSource(fontRID).Root;
            return OwnFont(new ServerFont(this, root));
        }
    }
    /// <summary>Returns a borrowed live font projection for typed shaping and canvas consumers.</summary>
    /// <param name="fontRID">Live font identity.</param><returns>The stable runtime font projection.</returns>
    /// <remarks>Disposing a service-owned projection releases its entry. Disposing a resource-owned projection preserves its borrowed source. Use FontFile for persistent source assets.
    /// Duplicate retains an independent source snapshot. Freeing a root invalidates source operations on its links.</remarks>
    /// <exception cref="InvalidOperationException">The identity or its root has retired.</exception>
    public Font FontGetResource(RID fontRID) => FontFile.TryResolveSource(fontRID, out var resource) ? resource.SourceResource : FontSource(fontRID);
    private RID OwnFont(ServerFont font)
    {
        try { lock (_fontsGate) { font.Identity = RID.Allocate(); _fonts.Add(font.Identity, font); return font.Identity; } }
        catch { font.Dispose(); throw; }
    }
    private ServerFont FontEntry(RID rid)
    {
        lock (_fontsGate)
            return _fonts.TryGetValue(rid, out var font) && !font.IsDisposalCommitted && !font.IsDisposed ? font : throw new InvalidOperationException("Font identity is absent or retired.");
    }
    private ServerFont FontSource(RID rid)
    {
        var font = FontEntry(rid);
        font.EnsureSourceLive();
        return font;
    }
    private FontFile SourceFont(RID rid) => FontFile.TryResolveSource(rid, out var resource) ? resource : FontSource(rid).Source;
    private int SourceSpacing(RID rid, TextSpacingType spacing) => FontFile.TryResolveSource(rid, out var resource) ? resource.GetSpacing(spacing) : FontEntry(rid).GetSpacing(spacing);
    private void SourceSpacing(RID rid, TextSpacingType spacing, int value)
    { if (FontFile.TryResolveSource(rid, out var resource)) resource.SetExtraSpacing(0, spacing, value); else FontEntry(rid).SetSpacing(spacing, value); }
    private float SourceBaseline(RID rid) => FontFile.TryResolveSource(rid, out var resource) ? resource.GetExtraBaselineOffset(0) : FontEntry(rid).Baseline;
    private void SourceBaseline(RID rid, float value)
    { if (FontFile.TryResolveSource(rid, out var resource)) resource.SetExtraBaselineOffset(0, value); else FontEntry(rid).Baseline = value; }
    private void ForgetFont(RID rid) { lock (_fontsGate) _fonts.Remove(rid); }
    private bool HasFont(RID rid) { lock (_fontsGate) return _fonts.TryGetValue(rid, out var font) && !font.IsDisposalCommitted && !font.IsDisposed; }
    private bool FreeFont(RID rid)
    {
        ServerFont? font; lock (_fontsGate) _fonts.TryGetValue(rid, out font);
        if (font is null) return false;
        font.Dispose(); return true;
    }

    private sealed class ServerFont : Font
    {
        private readonly TextServer? _server;
        internal readonly ServerFont Root;
        internal FontFile Source;
        private Font? _borrowedOwner;
        internal RID Identity;
        private readonly bool _ownsSource;
        private readonly RID _borrowedRoot;
        private readonly int[] _spacing = new int[4];
        private float _baseline;
        internal ServerFont(TextServer? server, FontFile file)
        { _server = server; Root = this; Source = file; _ownsSource = true; Watch(); }
        internal ServerFont(TextServer server, FontFile file, RID borrowedRoot)
        { _server = server; Root = this; Source = file; _borrowedRoot = borrowedRoot; if (!file.TryGetNotificationOwner(out _borrowedOwner)) throw new InvalidOperationException("The font source owner has retired."); Watch(); }
        internal ServerFont(TextServer server, ServerFont root)
        { _server = server; Root = root._borrowedRoot == default ? root : this; Source = root.Source; _borrowedRoot = root._borrowedRoot; _borrowedOwner = root._borrowedOwner; Watch(); }
        private void Watch() { Source.Changed += SourceChanged; Source.Disposed += SourceDisposed; }
        private void SourceChanged(Resource _) { if (!IsDisposed) InvalidateFont(); }
        private void SourceDisposed(ElectronObject _) { if (!IsDisposed) InvalidateFont(); }
        internal override Font? BaseDependency => Source;
        internal void EnsureSourceLive()
        {
            if (Root.IsDisposalCommitted || Root.IsDisposed || Source.IsDisposalCommitted || Source.IsDisposed || Source.SourceRetiring ||
                _borrowedRoot != default && !FontFile.TryResolveSource(_borrowedRoot, out _))
                throw new InvalidOperationException("The linked font source has retired.");
        }
        internal override FontData? PrimaryData { get { ThrowIfDisposed(); ObjectDisposedException.ThrowIf(Root.IsDisposalCommitted || Root.IsDisposed || Source.IsDisposalCommitted || Source.IsDisposed, Source); EnsureSourceLive(); return Source.PrimaryData; } }
        public override RID GetRID() { ThrowIfDisposed(); return Identity == default ? Source.SourceRID : Identity; }
        internal override RID GetSourceRID(FontData source)
        { return source.SourceOwner?.TryGetTarget(out var owner) == true && ReferenceEquals(owner, Source) ? GetRID() : base.GetSourceRID(source); }
        public override int GetSpacing(TextSpacingType spacing)
        { ValidateSpacing(spacing); lock (FontGate) { ThrowIfDisposed(); return _spacing[(int)spacing]; } }
        internal void SetSpacing(TextSpacingType spacing, int value)
        {
            ValidateSpacing(spacing);
            lock (FontGate) { ThrowIfDisposed(); if (_spacing[(int)spacing] == value) return; _spacing[(int)spacing] = value; InvalidateFontStateLocked(); }
            try { Source.InvalidateAutomaticSources(); } finally { EmitFontChanged(); }
        }
        private static void ValidateSpacing(TextSpacingType spacing)
        { if (spacing is < TextSpacingType.Glyph or >= TextSpacingType.Max) throw new ArgumentOutOfRangeException(nameof(spacing)); }
        internal float Baseline
        {
            get { lock (FontGate) { ThrowIfDisposed(); return _baseline; } }
            set
            {
                if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
                lock (FontGate) { ThrowIfDisposed(); if (_baseline == value) return; _baseline = value; InvalidateFontStateLocked(); }
                try { Source.InvalidateAutomaticSources(); } finally { EmitFontChanged(); }
            }
        }
        internal override int GetShapingSpacing(FontData? source, TextSpacingType spacing)
            => source?.SourceOwner?.TryGetTarget(out var owner) == true && ReferenceEquals(owner, Source) ? GetSpacing(spacing) : GetSourceSpacing(source, spacing);
        internal override float GetSystemBaseline(FontData source)
            => source.SourceOwner?.TryGetTarget(out var owner) == true && ReferenceEquals(owner, Source) ? Baseline : base.GetSystemBaseline(source);
        internal override float GetShapingBaselineOffset(FontData? source, int size)
        {
            var baseline = Baseline; if (source is null || source.SourceOwner?.TryGetTarget(out var owner) != true || !ReferenceEquals(owner, Source)) return 0;
            var metrics = source.GetMetrics(size); var shift = baseline * ((double)metrics.Ascent + metrics.Descent);
            if (!double.IsFinite(shift) || Math.Abs(shift) > float.MaxValue) throw new InvalidOperationException("Font baseline geometry exceeds its finite range.");
            return (float)shift - source.GetNativeBaselineShift(size);
        }
        protected override Resource CreateDuplicateInstance()
        {
            lock (FontGate)
            {
                ThrowIfDisposed(); var copy = new ServerFont(null, (FontFile)Source.Duplicate()) { _baseline = _baseline };
                _spacing.CopyTo(copy._spacing, 0); return copy;
            }
        }
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _server?.ForgetFont(Identity);
                    Source.Changed -= SourceChanged; Source.Disposed -= SourceDisposed;
                    if (_ownsSource) Source.Dispose();
                }
            }
            finally { try { base.Dispose(disposing); } finally { if (disposing) { Source = null!; _borrowedOwner = null; } } }
        }
    }
}
