namespace Electron2D;

public abstract partial class TextServer
{
    internal readonly struct FontSourceRead : IDisposable
    {
        private readonly ImplementationCall _call;
        private readonly TextServer _owner;
        private readonly ExternalEntry? _source;
        private readonly long _generation;
        internal FontSourceRead(TextServer owner, RID rid)
        {
            _call = owner.EnterImplementation(rid, true); _owner = owner;
            lock (owner._implementationGate) { owner._external.TryGetValue(rid, out _source); _generation = _source?.Generation ?? 0; }
        }
        internal bool IsCurrent
        { get { lock (_owner._implementationGate) return !_owner.IsDisposed && (_source is null || !_source.Retiring && _source.Generation == _generation); } }
        public void Dispose() => _call.Dispose();
    }
    internal FontSourceRead RetainFontSource(RID rid) => new(this, rid);
    /// <summary>Publishes a font-source change made outside ordinary service setters.</summary>
    /// <param name="fontRID">A live font source accepted by this implementation.</param>
    /// <remarks>Active readers retain their source; generation checks retry any result spanning this change.
    /// Publish the implementation's own state coherently before notification. Resource callbacks run outside implementation locks.</remarks>
    protected void NotifyFontChanged(RID fontRID)
    {
        using var call = EnterImplementation(fontRID, true); ProviderFont? projection; var external = false;
        lock (_implementationGate)
        {
            if (_external.TryGetValue(fontRID, out var source)) { source.Generation++; projection = source.Projection; external = true; }
            else projection = null;
        }
        if (external) projection?.InvalidateFont(); else SourceFont(fontRID).InvalidateAutomaticSources();
    }
    private Font? ExternalFontResource(RID rid)
    {
        lock (_implementationGate)
        {
            if (!_external.TryGetValue(rid, out var entry)) return null;
            if (!entry.Font || entry.Retiring) throw new InvalidOperationException("The identity is not a live font source.");
            if (entry.ResourceOwner?.TryGetTarget(out var resource) == true && !resource.IsDisposed) return resource;
            return entry.Projection ??= new ProviderFont(this, rid);
        }
    }
    private ProviderFont DuplicateExternalFont(RID rid)
    {
        using var sourceRead = RetainFontSource(rid);
        Func<RID, RID> duplicate;
        lock (_implementationGate) duplicate = _external[rid].Duplicate!;
        var copy = CreateOwnedRID(true, () => duplicate(rid));
        lock (_implementationGate)
        {
            if (copy == rid || !_external.TryGetValue(copy, out var entry) || !entry.Font || entry.Retiring)
                throw new InvalidOperationException("The font copy callback must return a fresh font source owned by this implementation.");
        }
        return (ProviderFont)ExternalFontResource(copy)!;
    }
    internal void BindResourceFont(RID rid, Font resource)
    { lock (_implementationGate) _external[rid].ResourceOwner = new(resource); }
    private void MarkResourceFontRetiring(RID rid)
    { lock (_implementationGate) if (_external.TryGetValue(rid, out var source)) source.RetireWhenIdle = true; }
    private void DisposeResourceFont(RID rid)
    {
        using var implementationCall = EnterImplementation(); ExternalEntry source;
        lock (_implementationGate)
        {
            if (!_external.TryGetValue(rid, out source!) || source.Retiring) return;
            source.RetireWhenIdle = true; if (source.Readers != 0) return;
            source.Retiring = true; source.Generation++;
        }
        FinishFreeRID(rid, source);
    }
    internal Font CopyFontForVariation(RID source, FontInstance instance, int[] spacing)
    {
        var copy = DuplicateExternalFont(source);
        try
        {
            var rid = copy.GetRID();
            FontSetVariationCoordinates(rid, instance.Coordinates); FontSetFaceIndex(rid, instance.FaceIndex);
            FontSetEmbolden(rid, instance.Embolden); FontSetTransform(rid, instance.Transform); FontSetBaselineOffset(rid, instance.BaselineOffset);
            FontSetUsedPalette(rid, instance.PaletteIndex); FontSetPaletteCustomColors(rid, instance.CustomColors);
            for (var i = 0; i < spacing.Length; i++) FontSetSpacing(rid, (TextSpacingType)i, spacing[i]);
            return copy;
        }
        catch (Exception failure)
        { try { copy.Dispose(); } catch (Exception cleanup) { throw new AggregateException(failure, cleanup); } throw; }
    }
    private sealed class ProviderFont(TextServer owner, RID rid) : Font
    {
        private bool _providerRetirement;
        internal override void MarkRetiredSource() => owner.MarkResourceFontRetiring(rid);
        internal override void DisposeRetiredSource() => owner.DisposeResourceFont(rid);
        internal override FontData? PrimaryData { get { ThrowIfDisposed(); return null; } }
        internal override FontCallbackSource? ExternalSource => new(owner, rid);
        public override int GetSpacing(TextSpacingType spacing)
        { ThrowIfDisposed(); if (spacing is < TextSpacingType.Glyph or >= TextSpacingType.Max) throw new ArgumentOutOfRangeException(nameof(spacing)); return owner.FontGetSpacing(rid, spacing); }
        public override Dictionary<string, int> GetOpenTypeFeatures() { ThrowIfDisposed(); return owner.FontGetOpenTypeFeatureOverrides(rid); }
        private protected override void ValidateDisposalTransition()
        {
            base.ValidateDisposalTransition(); if (_providerRetirement) return;
            lock (owner._implementationGate)
                if (owner._external.TryGetValue(rid, out var source) && (source.Readers != 0 || source.Retiring || source.ResourceOwner is not null))
                    throw new InvalidOperationException("An active font projection cannot retire.");
        }
        protected override Resource CreateDuplicateInstance() { ThrowIfDisposed(); return owner.DuplicateExternalFont(rid); }
        internal void RetireFromProvider() { if (IsDisposed || IsDisposalCommitted) return; _providerRetirement = true; Dispose(); }
        protected override void ValidateDisposal()
        {
            base.ValidateDisposal();
            if (_providerRetirement) return;
            lock (owner._implementationGate)
                if (owner._external.TryGetValue(rid, out var source) && (source.Readers != 0 || source.Retiring || source.ResourceOwner is not null))
                    throw new InvalidOperationException("An active font projection cannot retire.");
        }
        protected override void Dispose(bool disposing)
        {
            try { if (disposing && !_providerRetirement) owner.FreeRID(rid); }
            finally { base.Dispose(disposing); }
        }
    }
}
