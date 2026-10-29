namespace Electron2D;

public abstract partial class Font
{
    private readonly List<TextLayout> _retiredLayouts = [];
    internal readonly struct PreparedLayoutRead(Font owner, TextLayout layout, ReadScope fontRead) : IDisposable
    {
        internal TextLayout Layout => layout;
        internal void Dispose(Exception? failure)
        { try { Dispose(); } catch (Exception cleanup) { if (failure is not null) throw new AggregateException(failure, cleanup); throw; } }
        public void Dispose()
        {
            Exception? failure = null;
            try { lock (owner.FontGate) layout.ReleaseCachedUse(); }
            catch (Exception error) { failure = error; }
            try { fontRead.Dispose(); }
            catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
            try { owner.DrainRetiredLayouts(); }
            catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
    internal PreparedLayoutRead PrepareLayout(string text, HorizontalAlignment alignment, float width, int size, int maxLines, TextLineBreakFlags breaks,
        TextJustificationFlags justification, TextDirection direction, TextOrientation orientation, bool multiline)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(text); ValidateSize(size);
        if (!float.IsFinite(width)) throw new ArgumentOutOfRangeException(nameof(width));
        if (alignment is < HorizontalAlignment.Left or > HorizontalAlignment.Fill) throw new ArgumentOutOfRangeException(nameof(alignment));
        if (direction is < TextDirection.Auto or > TextDirection.Inherited) throw new ArgumentOutOfRangeException(nameof(direction));
        if (orientation is < TextOrientation.Horizontal or > TextOrientation.Vertical) throw new ArgumentOutOfRangeException(nameof(orientation));
        var key = new TextLayoutKey(text, size, width, alignment, maxLines, breaks, justification, direction, orientation, multiline);
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            var read = BeginRead(); TextLayout? result = null; var handedOff = false; var claimed = false; var cacheHit = false; Exception? failure = null;
            try
            {
                lock (FontGate)
                {
                    var cache = multiline ? _multi : _single; var capacity = multiline ? _multiCapacity : _singleCapacity;
                    foreach (var entry in cache) if (!entry.IsBusy && entry.Key == key) { result = entry; cacheHit = true; break; }
                    if (result is null)
                    {
                        if (capacity == 0) result = _scratch.IsBusy ? CurrentLayoutScratch() : _scratch;
                        else if (cache.Count < capacity) { result = new(); cache.Add(result); }
                        else
                        {
                            foreach (var entry in cache)
                                if (!entry.IsBusy && (result is null || entry.LastAccess < result.LastAccess)) result = entry;
                            result ??= CurrentLayoutScratch();
                        }
                    }
                    result.ClaimCachedUse(); claimed = true;
                }
                if (!cacheHit || result.Key != key || result.Generation != read.Generation)
                { result.Generation = -1; result.Build(this, key); }
                if (!read.IsCurrent) continue;
                lock (FontGate) { result.Generation = read.Generation; result.LastAccess = ++_access; }
                handedOff = true; return new(this, result, read);
            }
            catch (Exception error) { failure = error; throw; }
            finally
            {
                if (!handedOff)
                {
                    Exception? cleanup = null;
                    try { if (claimed) lock (FontGate) result!.ReleaseCachedUse(); } catch (Exception error) { cleanup = error; }
                    try { read.Dispose(); } catch (Exception error) { cleanup = cleanup is null ? error : new AggregateException(cleanup, error); }
                    try { DrainRetiredLayouts(); } catch (Exception error) { cleanup = cleanup is null ? error : new AggregateException(cleanup, error); }
                    if (cleanup is not null) throw failure is null ? cleanup : new AggregateException(failure, cleanup);
                }
            }
        }
        throw UnsettledRead();
    }
    private TextLayout CurrentLayoutScratch()
    {
        for (var context = _callbackReadHead; context is not null; context = context.CallbackParent)
            if (ReferenceEquals(context.CallbackOwner, this)) return context.Scratch ??= new();
        return ReadContextAt(_readDepth).Scratch ??= new();
    }
    private void RetireCachedLayouts(List<TextLayout> cache, int start)
    {
        for (var i = start; i < cache.Count; i++) _retiredLayouts.Add(cache[i]);
        cache.RemoveRange(start, cache.Count - start);
    }
    private void DrainRetiredLayouts()
    {
        List<Exception>? errors = null;
        while (true)
        {
            TextLayout? ready = null;
            lock (FontGate)
                for (var i = 0; i < _retiredLayouts.Count; i++)
                    if (!_retiredLayouts[i].IsBusy) { ready = _retiredLayouts[i]; _retiredLayouts.RemoveAt(i); break; }
            if (ready is null) break;
            try { ready.ReleaseContent(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null) throw new AggregateException("Retired font layout cleanup failed.", errors);
    }
}
