namespace Electron2D;

internal readonly record struct FontCallbackSource(TextServer Owner, RID RID);

public abstract partial class Font
{
    internal sealed class SourceChangedDuringRead : InvalidOperationException { }
    internal virtual FontCallbackSource? ExternalSource => null;
    internal virtual void PrepareExternalSource() { }
    private TextServer? PrepareCallbackGraph(int depth = 0)
    {
        if (depth > 64) throw new InvalidOperationException("Font source depth exceeds sixty-four levels.");
        PrepareExternalSource();
        Font? dependency; Font?[] fallbacks; TextServer? provider;
        lock (FontGate) { ThrowIfDisposed(); dependency = BaseDependency; fallbacks = EffectiveFallbacks; provider = ExternalSource?.Owner; }
        if (dependency is not null) Merge(dependency.PrepareCallbackGraph(depth + 1));
        foreach (var fallback in fallbacks) if (fallback is not null) Merge(fallback.PrepareCallbackGraph(depth + 1));
        return provider;
        void Merge(TextServer? other)
        { if (other is null) return; if (provider is not null && !ReferenceEquals(provider, other)) throw new InvalidOperationException("A font graph cannot combine opaque sources from different text implementations."); provider = other; }
    }
    private bool TryGetCallbackSource(out FontCallbackSource source)
    {
        PrepareExternalSource();
        if (ExternalSource is { } external) { source = external; return true; }
        if (TextServerManager.CallbackProvider is { } provider && GetRID() is var rid && rid != default)
        { source = new(provider, rid); return true; }
        source = default; return false;
    }
    private T QueryCallbackSource<T>(Func<TextServer, RID, T> query)
    {
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            var read = BeginRead(); Exception? readFailure = null;
            try
            {
                if (!TryGetCallbackSource(out var source)) throw new InvalidOperationException("The font source changed during a callback query.");
                var value = query(source.Owner, source.RID); if (read.IsCurrent) return value;

            }
            catch (Exception scopeError) { readFailure = scopeError; throw; }
            finally { read.Dispose(readFailure); }
        }
        throw UnsettledRead();
    }
    private T QueryCallbackSource<T, TArgument>(TArgument argument, Func<TextServer, RID, TArgument, T> query)
    {
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            var read = BeginRead(); Exception? readFailure = null;
            try
            {
                if (!TryGetCallbackSource(out var source)) throw new InvalidOperationException("The font source changed during a callback query.");
                var value = query(source.Owner, source.RID, argument); if (read.IsCurrent) return value;

            }
            catch (Exception scopeError) { readFailure = scopeError; throw; }
            finally { read.Dispose(readFailure); }
        }
        throw UnsettledRead();
    }
    private void CollectCallbackSources(ReadContext context, Font font, int depth)
    {
        if (depth > 64) throw new InvalidOperationException("Font fallback depth exceeds sixty-four levels.");
        Font?[] fallbacks;
        lock (font.FontGate)
        {
            font.ThrowIfDisposed();
            var source = font.ExternalSource;
            if (source is null && !TextServer.IsNativeBackendCall && context.ProviderRead.Provider is { UsesCallbackConsumers: true } provider && font.PrimaryData is { } nativeSource && font.GetSourceRID(nativeSource) is var rid && rid != default) source = new(provider, rid);
            if (source is { } value && !context.CallbackSources.Contains(value))
            {
                context.CallbackSources.Add(value);
                context.CallbackSourceReads.Add(value.Owner.RetainFontSource(value.RID));
            }
            fallbacks = font.EffectiveFallbacks;
        }
        foreach (var fallback in fallbacks) if (fallback is not null) CollectCallbackSources(context, fallback, depth + 1);
    }
    private List<FontCallbackSource>? CallbackSources()
    {
        for (var read = _callbackReadHead; read is not null; read = read.CallbackParent)
            if (ReferenceEquals(read.CallbackOwner, this)) return read.CallbackSources;
        return null;
    }
    private bool TryCallbackCharacter(int character, int size, out FontCallbackSource? selected, out int glyph, out float advance)
    {
        selected = null; glyph = 0; advance = 0;
        if (CallbackSources() is not { Count: > 0 } sources) return false;
        foreach (var source in sources)
        {
            if (!source.Owner.FontHasChar(source.RID, character)) continue;
            var index = source.Owner.FontGetGlyphIndex(source.RID, size, character, 0);
            if (index == 0) continue;
            selected = source; glyph = index; advance = source.Owner.FontGetGlyphAdvance(source.RID, size, index).X; break;
        }
        return true;
    }

    internal static bool RetainsCallbackSource(TextServer owner, RID rid)
    {
        for (var context = _callbackReadHead; context is not null; context = context.CallbackParent)
            foreach (var source in context.CallbackSources) if (ReferenceEquals(source.Owner, owner) && source.RID == rid) return true;
        return false;
    }
    internal static bool RetainsCallbackSource(RID rid)
    {
        for (var context = _callbackReadHead; context is not null; context = context.CallbackParent)
            foreach (var source in context.CallbackSources) if (source.RID == rid) return true;
        return false;
    }
    internal virtual void MarkRetiredSource() { }
    internal virtual void DisposeRetiredSource() => Dispose();

}
