namespace Electron2D;

public sealed partial class TextLine
{
    internal RID ServerParent;
    private sealed class SubstringSource(TextLine source)
    {
        internal TextLine Source { get; } = source;
        internal int References = 1;
        internal void Release() { if (--References == 0) Source.Dispose(); }
    }
    private SubstringSource? _serverSubstring, _serverSliceSnapshot;
    internal TextLine? ServerShapingSource => _serverSubstring?.Source;
    internal TextLine ServerDuplicate()
    {
        var copy = Duplicate();
        if (_consumerOwner is { } owner) copy.BindConsumerOwner(owner);
        try
        {
            if (_full.UsesProvider || _serverFullFormatted)
            {
                copy.Ensure(); _full.ServerCopyFormatting(copy._full, false); _display.ServerCopyFormatting(copy._display, false);
                copy._serverFullFormatted = _serverFullFormatted;
                copy._serverFitMinimum = _serverFitMinimum;
            }
            return copy;
        }
        catch (Exception failure) { try { copy.Dispose(); } catch (Exception cleanup) { throw new AggregateException(failure, cleanup); } throw; }
    }
    internal void ServerAttachSubstring(TextLine source, RID parent)
    {
        source.Ensure(); source._busy = true;
        try
        {
            source.BeginPreparedReads();
            if (source._serverSliceSnapshot is null)
            {
                var snapshot = source.ServerDuplicate();
                source._serverSliceSnapshot = new(snapshot);
            }
            _serverSubstring = source._serverSliceSnapshot; _serverSubstring.References++; ServerParent = parent;
            foreach (var font in _serverSubstring.Source._fonts) if (!_fonts.Contains(font)) _fonts.Add(font);
            _fonts.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID));
        }
        finally { try { source.EndReads(); } finally { source._busy = false; } }
    }
    internal void ServerDetachSubstring()
    {
        if (_serverSubstring is null) return;
        var fonts = new HashSet<Font>(ReferenceEqualityComparer.Instance); foreach (var span in _spans) fonts.Add(span.Font);
        _fonts.EnsureCapacity(fonts.Count);
        var lease = _serverSubstring!; _serverSubstring = null; ServerParent = default; _fonts.Clear(); _fonts.AddRange(fonts);
        _fonts.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID)); _dirty = true; _generations.Clear(); lease.Release();
    }
    private void ServerReleaseSnapshot() { var snapshot = _serverSliceSnapshot; _serverSliceSnapshot = null; snapshot?.Release(); }
    private void ServerCopyShaping(TextLine target)
    {
        Ensure(); _busy = true;
        try
        {
            BeginPreparedReads();
            _full.ServerCopySlice(target._full, target.ServerOffset - ServerOffset, target.ServerOffset - ServerOffset + target._count);
            _full.ServerCopySlice(target._display, target.ServerOffset - ServerOffset, target.ServerOffset - ServerOffset + target._count);
        }
        finally { try { EndReads(); } finally { _busy = false; } }
    }
}
