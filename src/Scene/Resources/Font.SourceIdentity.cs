namespace Electron2D;

public abstract partial class Font
{
    /// <summary>Returns the borrowed identity of this font's primary native source cache.</summary>
    /// <returns>A live source identity, or an empty identity when no primary source resolves.</returns>
    /// <remarks>Source configuration changes preserve the cache identity. Cache removal, full reset, file import and owner disposal retire it.
    /// TextServer can query and edit the cache; FreeRID cannot release a resource-owned source.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public override RID GetRID()
    {
        try
        {
            lock (FontGate)
            {
                ThrowIfDisposed();
                return PrimaryData is { } data ? GetSourceRID(data) : default;
            }
        }
        finally { DrainSourceRetirements(); }
    }

    /// <summary>Copies primary and explicit fallback source identities in their authored traversal order.</summary>
    /// <returns>An independent array; repeated fallback entries retain repeated identities.</returns>
    /// <remarks>Automatic fallback sources appear in shaped glyph/run metadata after realization. Source caches remain
    /// borrowed and may retire after this query. Null fallbacks are skipped; fallback depth is bounded to sixty-four.</remarks>
    /// <exception cref="ObjectDisposedException">A required font is disposed.</exception>
    /// <exception cref="InvalidOperationException">A dependency changes continuously or the fallback graph is invalid.</exception>
    public RID[] GetRIDs()
    {
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            using var read = BeginRead(); var ids = new List<RID>();
            Add(this, 0);
            if (read.IsCurrent) return ids.ToArray();
            void Add(Font font, int depth)
            {
                if (depth > 64) throw new InvalidOperationException("Font fallback depth exceeds sixty-four levels.");
                var rid = font.GetRID(); if (rid != default) ids.Add(rid);
                foreach (var fallback in font.EffectiveFallbacks) if (fallback is not null) Add(fallback, depth + 1);
            }
        }
        throw UnsettledRead();
    }

    internal virtual RID GetSourceRID(FontData source)
    {
        if (source.SourceOwner?.TryGetTarget(out var owner) == true) return owner.SourceRetiring ? default : owner.SourceRID;
        throw new InvalidOperationException("The shaped font source has no live cache owner.");
    }
}
