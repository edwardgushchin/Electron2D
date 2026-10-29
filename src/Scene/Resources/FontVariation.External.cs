namespace Electron2D;

public partial class FontVariation
{
    private Font? _externalFont;
    private FontCallbackSource? _externalBaseSource;
    private long _externalConfiguration = -1, _externalBaseGeneration = -1, _externalRevision;
    [ThreadStatic] private static List<FontVariation>? _preparingExternal;
    internal override FontCallbackSource? ExternalSource => _externalFont?.ExternalSource;
    internal override void PrepareExternalSource()
    {
        var source = ResolveBase(); source?.PrepareExternalSource();
        if (source?.ExternalSource is not { } sourceBinding) return;
        var sourceGeneration = source.GetContentGeneration();
        lock (FontGate)
        {
            ThrowIfDisposed();
            if (_externalFont is not null && _externalConfiguration == _externalRevision && _externalBaseGeneration == sourceGeneration && _externalBaseSource == sourceBinding) return;
        }
        var preparing = _preparingExternal ??= [];
        if (preparing.Contains(this)) throw new InvalidOperationException("An unpublished external font variation cannot resolve itself recursively.");
        preparing.Add(this);
        try
        {
            for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
            {
                source = ResolveBase(); source?.PrepareExternalSource();
                if (source?.ExternalSource is not { } binding) return;
                using var read = source.BeginRead(); sourceGeneration = read.Generation;
                FontInstance instance; int[] spacing; long generation;
                lock (FontGate)
                {
                    ThrowIfDisposed(); generation = _externalRevision;
                    if (_externalFont is not null && _externalConfiguration == generation && _externalBaseGeneration == sourceGeneration && _externalBaseSource == binding) return;
                    instance = new(_face, new(_coordinates), _embolden, _transform, _baseline, _palette, (Color[])_colors.Clone());
                    spacing = (int[])_spacing.Clone();
                }
                var copy = binding.Owner.CopyFontForVariation(binding.RID, instance, spacing);
                Font? old = null; var published = false; var alreadyPublished = false;
                try
                {
                    lock (FontGate)
                    {
                        ThrowIfDisposed();
                        if (generation == _externalRevision && sourceGeneration == source.SourceGeneration)
                        {
                            alreadyPublished = _externalFont is not null && _externalConfiguration == generation && _externalBaseGeneration == sourceGeneration && _externalBaseSource == binding;
                            if (!alreadyPublished)
                            {
                                binding.Owner.BindResourceFont(copy.GetRID(), this);
                                old = _externalFont; if (old is not null) old.Changed -= _backendChanged; _externalFont = copy; copy.Changed += _backendChanged;
                                _externalConfiguration = generation; _externalBaseGeneration = sourceGeneration; _externalBaseSource = binding;
                                _sourceBaseGeneration = sourceGeneration; _dirty = false; published = true;
                            }
                        }
                    }
                }
                catch (Exception failure)
                { try { copy.Dispose(); } catch (Exception cleanup) { throw new AggregateException(failure, cleanup); } throw; }
                if (!published) copy.Dispose();
                if (old is not null) QueueSourceRetirement(old);
                DrainSourceRetirements();
                if (published || alreadyPublished) return;
            }
            throw UnsettledRead();
        }
        finally { preparing.RemoveAt(preparing.Count - 1); }
    }
    private void RetireExternalSourceLocked()
    {
        _externalRevision++;
        var old = _externalFont; if (old is not null) old.Changed -= _backendChanged; _externalFont = null; _externalBaseSource = null;
        _externalConfiguration = _externalBaseGeneration = -1;
        if (old is not null) QueueSourceRetirement(old);
    }
}
