namespace Electron2D;

public partial class FontFile
{
    private static readonly object SourceRegistryGate = new();
    private static readonly Dictionary<RID, WeakReference<FontFile>> SourceRegistry = [];
    private WeakReference<FontFile>? _sourceWeak;
    private RID _sourceRID;
    private SourceProjection? _sourceProjection;
    private WeakReference<Font>? _notificationOwner;
    private volatile bool _sourceRetiring;

    internal override int GetShapingSpacing(FontData? source, TextSpacingType spacing)
    {
        var authored = GetSpacing(spacing);
        if (source is null || source.SourceOwner?.TryGetTarget(out var owner) == true && ReferenceEquals(owner, this)) return authored;
        return checked((int)((long)GetSourceSpacing(source, spacing) + authored - GetExtraSpacing(0, spacing)));
    }

    private void BindSource(FontData data) => data.SourceOwner = _sourceWeak ??= new(this);

    internal static FontFile AdoptSource(FontData data, ReadOnlySpan<int> spacing = default, Font? notificationOwner = null)
    {
        var file = new FontFile();
        var empty = file._fontData!;
        var cache = data.Authored ?? new FontCache();
        cache.Instance = data.SourceInstance;
        if (!spacing.IsEmpty) cache.Spacing = spacing.ToArray();
        data.Authored = cache;
        file._bytes = data.SourceBytes; file._type1Metrics = data.SourceMetrics;
        file._fixedSize = cache.FixedSize; file._fixedScaleMode = cache.ScaleMode;
        file._fontData = data; file._caches.Add(cache); file.BindSource(data);
        empty.Dispose();
        if (notificationOwner is not null) file._notificationOwner = new(notificationOwner);
        return file;
    }

    internal RID SourceRID
    {
        get
        {
            lock (FontGate)
            {
                ThrowIfDisposed();
                if (_sourceRetiring) throw new InvalidOperationException("The font source is retiring.");
                EnsureLoadedLocked(); EnsurePrimaryCacheLocked(); BindSource(_fontData!);
                if (_sourceRID == default)
                {
                    _sourceRID = RID.Allocate();
                    lock (SourceRegistryGate)
                    {
                        if ((SourceRegistry.Count & 255) == 255)
                            foreach (var pair in SourceRegistry.ToArray())
                                if (!pair.Value.TryGetTarget(out var owner) || owner.IsDisposalCommitted) SourceRegistry.Remove(pair.Key);
                        SourceRegistry.Add(_sourceRID, _sourceWeak!);
                    }
                }
                return _sourceRID;
            }
        }
    }

    internal static bool HasSourceIdentity(RID rid)
    {
        lock (SourceRegistryGate) return SourceRegistry.TryGetValue(rid, out var weak) && weak.TryGetTarget(out var file) && !file.IsDisposed && (!file._sourceRetiring || RetainsCallbackSource(rid)) && file._sourceRID == rid;
    }

    internal static bool TryResolveSource(RID rid, out FontFile file)
    {
        lock (SourceRegistryGate)
        {
            if (!SourceRegistry.TryGetValue(rid, out var weak) || !weak.TryGetTarget(out file!))
            { SourceRegistry.Remove(rid); file = null!; return false; }
        }
        lock (file.FontGate) return !file.IsDisposalCommitted && !file.IsDisposed && (!file._sourceRetiring || RetainsCallbackSource(rid)) && file._sourceRID == rid && file.TryGetNotificationOwner(out var owner) && !owner.IsDisposalCommitted && !owner.IsDisposed;
    }

    private void RetireSourceIdentityLocked()
    {
        lock (SourceRegistryGate) SourceRegistry.Remove(_sourceRID);
        _sourceRID = default; _sourceProjection?.Retire(); _sourceProjection = null;
    }

    internal Font SourceResource
    {
        get
        {
            lock (FontGate)
            {
                var rid = SourceRID;
                if (_sourceProjection is null || _sourceProjection.IsDisposalCommitted || _sourceProjection.IsDisposed)
                    _sourceProjection = new(this, rid);
                return _sourceProjection;
            }
        }
    }

    internal void SystemSourceChanged(FontData source)
    {
        lock (FontGate)
        {
            if (IsDisposalCommitted || IsDisposed || !ReferenceEquals(_fontData, source)) return;
            InvalidateFontStateLocked();
        }
        EmitFontChanged();
    }

    internal (FontData Data, long Generation) AcquireSourceRead()
    {
        lock (FontGate)
        {
            var data = PrimaryData!;
            if (!data.TryAcquireRead()) throw new InvalidOperationException("The font source has retired.");
            return (data, SourceGeneration);
        }
    }
    internal int SourceFaceCount => Query(0, static (_, data) => data.FaceCount);
    internal void MarkSourceRetiring() => _sourceRetiring = true;
    internal bool SourceRetiring => _sourceRetiring;
    internal bool TryGetNotificationOwner(out Font owner)
    {
        if (_notificationOwner is null) { owner = this; return true; }
        return _notificationOwner.TryGetTarget(out owner!);
    }

    internal void MirrorSpacing(ReadOnlySpan<int> spacing)
    {
        lock (FontGate)
        {
            var cache = CacheLocked(0); var data = DataLocked(0);
            lock (data.Gate) spacing.CopyTo(cache.Spacing);
            data.ClearSystemFallbackCache();
            InvalidateFontStateLocked();
        }
    }

    internal void InvalidateAutomaticSources()
    {
        lock (FontGate) { _fontData?.ClearSystemFallbackCache(); InvalidateFontStateLocked(); }
        if (TryGetNotificationOwner(out var owner)) owner.DrainSourceRetirements();
    }
    internal void NotifyMirroredSpacing() => _sourceProjection?.ChangedSource(this);

    private sealed class SourceProjection : Font
    {
        private FontFile _file;
        private readonly RID _identity;
        private readonly bool _ownsFile;
        private Font? _lifetimeOwner;
        private bool _retired;
        internal SourceProjection(FontFile file, RID identity, bool ownsFile = false)
        { _file = file; _identity = identity; _ownsFile = ownsFile; if (!file.TryGetNotificationOwner(out _lifetimeOwner) || _lifetimeOwner.IsDisposalCommitted || _lifetimeOwner.IsDisposed) throw new InvalidOperationException("The font source owner has retired."); file.Changed += ChangedSource; file.Disposed += DisposedSource; }
        internal void Retire() => _retired = true;
        private void EnsureLive()
        {
            ThrowIfDisposed();
            if (_retired || _lifetimeOwner is null || _lifetimeOwner.IsDisposalCommitted || _lifetimeOwner.IsDisposed || _file.SourceRetiring || _file.IsDisposalCommitted || _file.IsDisposed) throw new InvalidOperationException("The borrowed font source identity has retired.");
        }
        private void Unwatch() { _file.Changed -= ChangedSource; _file.Disposed -= DisposedSource; }
        internal void ChangedSource(Resource _) { if (_retired) Unwatch(); if (!IsDisposed && !IsDisposalCommitted) InvalidateFont(); }
        private void DisposedSource(ElectronObject _) { _retired = true; Unwatch(); if (!IsDisposed && !IsDisposalCommitted) InvalidateFont(); }
        internal override Font? BaseDependency => _file;
        internal override FontData? PrimaryData { get { EnsureLive(); return _file.PrimaryData; } }
        public override RID GetRID() { EnsureLive(); return _identity; }
        public override int GetSpacing(TextSpacingType spacing) { EnsureLive(); return _file.GetSpacing(spacing); }
        public override int GetFaceCount() { EnsureLive(); return _file.SourceFaceCount; }
        protected override Resource CreateDuplicateInstance()
        {
            EnsureLive(); var file = (FontFile)_file.Duplicate();
            var projection = new SourceProjection(file, file.SourceRID, true); file._sourceProjection = projection; return projection;
        }
        protected override void Dispose(bool disposing)
        {
            try { if (disposing) { Unwatch(); if (_ownsFile) _file.Dispose(); } }
            finally { try { base.Dispose(disposing); } finally { if (disposing) { _file = null!; _lifetimeOwner = null; } } }
        }
    }
}
