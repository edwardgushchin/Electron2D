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
    internal void ServerAttachSubstring(TextLine source, RID parent)
    {
        source.Ensure(); source._busy = true;
        try
        {
            source.BeginPreparedReads();
            if (source._serverSliceSnapshot is null)
            {
                var snapshot = source.Duplicate();
                try
                {
                    if (source._serverFullFormatted) { snapshot.Ensure(); source._full.ServerCopyFormatting(snapshot._full, false); source._full.ServerCopyFormatting(snapshot._display, false); snapshot._serverFullFormatted = true; }
                    source._serverSliceSnapshot = new(snapshot);
                }
                catch { snapshot.Dispose(); throw; }
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
        var lease = _serverSubstring!; _serverSubstring = null; ServerParent = default; _fonts.Clear();
        foreach (var span in _spans) if (!_fonts.Contains(span.Font)) _fonts.Add(span.Font);
        _fonts.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID)); _dirty = true; _generations.Clear(); lease.Release();
    }
    private void ServerReleaseSnapshot() { _serverSliceSnapshot?.Release(); _serverSliceSnapshot = null; }
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
