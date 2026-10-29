namespace Electron2D;

public abstract partial class Font
{
    [ThreadStatic] private static ReadContext? _callbackReadHead;
    private readonly List<ReadContext> _callbackReads = [];

    // Retain source snapshots across user callbacks without holding the resource monitor.
    private ReadScope BeginCallbackRead(TextServerManager.ConsumerRead provider)
    {
        ActiveFontReads++;
        ReadContext? context = null; var acquired = 0;
        try
        {
            lock (FontGate)
            {
                ThrowIfDisposed();
                var nesting = 0;
                for (var read = _callbackReadHead; read is not null; read = read.CallbackParent)
                    if (ReferenceEquals(read.CallbackOwner, this) && ++nesting >= MaximumReadAttempts) throw new InvalidOperationException("Font callback read reentry exceeds sixty-four levels.");
                foreach (var candidate in _callbackReads) if (!candidate.CallbackActive) { context = candidate; break; }
                if (context is null)
                {
                    context = new(); _callbackReads.Add(context);
                }
                context.CallbackActive = true;
                for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
                {
                    EnsureSourcesCurrent(); context.Generation = _generation; context.Sources.AddRange(_sources);
                    foreach (var source in context.Sources) context.SystemGenerations.Add(source.SystemGeneration);
                    context.Dependencies.AddRange(_dependencies); context.Generations.AddRange(_dependencyGenerations);
                    while (acquired < context.Sources.Count && context.Sources[acquired].TryAcquireRead()) acquired++;
                    if (acquired == context.Sources.Count)
                    {
                        TextServer.RetainNativeFontSources(context.Dependencies, context.NativeSourceReads);
                        context.ProviderRead = provider; CollectCallbackSources(context, this, 0); context.CallbackOwner = this;
                        context.CallbackParent = _callbackReadHead; _callbackReadHead = context;
                        return new(this, context, true);
                    }
                    var count = acquired; acquired = 0; ReleaseContext(context, count); ClearLayoutCaches();
                }
                throw UnsettledRead();
            }
        }
        catch
        {
            try { if (context is not null) ReleaseContext(context, acquired); }
            finally
            {
                if (context is not null) lock (FontGate) context.CallbackActive = false;
                ActiveFontReads--; provider.Dispose(); DrainPendingSourceRetirements();
            }
            throw;
        }
    }

    private void EndCallbackRead(ReadContext context)
    {
        _callbackReadHead = context.CallbackParent; context.CallbackParent = null; context.CallbackOwner = null;
        ActiveFontReads--;
        var provider = context.ProviderRead; context.ProviderRead = default;
        lock (FontGate) context.CallbackActive = false;
        provider.Dispose();
    }
}
