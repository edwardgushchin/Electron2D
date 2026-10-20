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
            var root = FontSource(fontRID).Root;
            return OwnFont(new ServerFont(this, root));
        }
    }
    /// <summary>Returns a borrowed live font projection for typed shaping and canvas consumers.</summary>
    /// <param name="fontRID">Live font identity.</param><returns>The stable runtime font projection.</returns>
    /// <remarks>Disposing this projection releases its service entry. Use FontFile for persistent source assets.
    /// Duplicate retains an independent source snapshot. Freeing a root invalidates source operations on its links.</remarks>
    /// <exception cref="InvalidOperationException">The identity or its root has retired.</exception>
    public Font FontGetResource(RID fontRID) => FontSource(fontRID);
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
        if (font.Root.IsDisposalCommitted || font.Root.IsDisposed) throw new InvalidOperationException("The linked font source has retired.");
        return font;
    }
    private FontFile SourceFont(RID rid) => FontSource(rid).Source;
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
        internal readonly FontFile Source;
        internal RID Identity;
        private readonly int[] _spacing = new int[4];
        private float _baseline;
        internal ServerFont(TextServer? server, FontFile file)
        { _server = server; Root = this; Source = file; Watch(); }
        internal ServerFont(TextServer server, ServerFont root)
        { _server = server; Root = root; Source = root.Source; Watch(); }
        private void Watch() { Source.Changed += SourceChanged; Source.Disposed += SourceDisposed; }
        private void SourceChanged(Resource _) { if (!IsDisposed) InvalidateFont(); }
        private void SourceDisposed(ElectronObject _) { if (!IsDisposed) InvalidateFont(); }
        internal override Font? BaseDependency => Source;
        internal override FontData? PrimaryData { get { ThrowIfDisposed(); return Source.PrimaryData; } }
        public override RID GetRID() { ThrowIfDisposed(); return Identity; }
        public override int GetSpacing(TextSpacingType spacing)
        { ValidateSpacing(spacing); lock (FontGate) { ThrowIfDisposed(); return _spacing[(int)spacing]; } }
        internal void SetSpacing(TextSpacingType spacing, int value)
        {
            ValidateSpacing(spacing);
            lock (FontGate) { ThrowIfDisposed(); if (_spacing[(int)spacing] == value) return; _spacing[(int)spacing] = value; InvalidateFontStateLocked(); }
            EmitChanged();
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
                EmitChanged();
            }
        }
        internal override float GetShapingBaselineOffset(FontData? source, int size)
        {
            var baseline = Baseline; if (baseline == 0 || source is null) return 0;
            var metrics = source.GetMetrics(size); var shift = baseline * ((double)metrics.Ascent + metrics.Descent);
            if (!double.IsFinite(shift) || Math.Abs(shift) > float.MaxValue) throw new InvalidOperationException("Font baseline geometry exceeds its finite range.");
            return (float)shift;
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
                    if (ReferenceEquals(Root, this)) Source.Dispose();
                }
            }
            finally { base.Dispose(disposing); }
        }
    }
}
