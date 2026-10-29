using System.Collections.Concurrent;

namespace Electron2D;

public abstract partial class TextServer
{
    private static readonly ConcurrentDictionary<RID, WeakReference<TextServer>> ExternalOwners = new();
    private readonly Dictionary<RID, ExternalEntry> _external = [];
    private sealed class ExternalEntry(bool font, int ownerThread, Func<RID, RID>? duplicate)
    {
        internal readonly bool Font = font;
        internal RID Identity;
        internal readonly int OwnerThread = ownerThread;
        internal readonly Func<RID, RID>? Duplicate = duplicate;
        internal int Readers;
        internal bool Retiring, RetireWhenIdle;
        internal WeakReference<Font>? ResourceOwner;
        internal long Generation;
        internal ProviderFont? Projection;
    }

    /// <summary>Allocates an implementation-owned identity for an opaque external font source.</summary>
    /// <param name="duplicate">Copies the source to a fresh font identity owned by this implementation.</param>
    /// <returns>A new identity consumed by the typed font callbacks and Font projection.</returns>
    /// <remarks>Release through FreeRID. The independent duplication callback must copy source state;
    /// returning this source or another implementation's identity rejects.</remarks>
    /// <exception cref="ArgumentNullException">The duplication callback is null.</exception>
    /// <exception cref="ObjectDisposedException">Implementation retirement has started.</exception>
    protected RID AllocateFontRID(Func<RID, RID> duplicate)
    { ArgumentNullException.ThrowIfNull(duplicate); return AllocateExternalRID(true, duplicate); }

    /// <summary>Allocates an implementation-owned identity for an opaque external shaped buffer.</summary>
    /// <returns>A new identity; release through FreeRID.</returns>
    /// <remarks>Public buffers follow their creating thread. Engine consumer buffers use coordinated
    /// implementation admission and are retired explicitly by the consumer.</remarks>
    /// <exception cref="ObjectDisposedException">Implementation retirement has started.</exception>
    protected RID AllocateShapedTextRID() => AllocateExternalRID(false, null);

    private RID AllocateExternalRID(bool font, Func<RID, RID>? duplicate)
    {
        CheckFactoryAllocation();
        lock (_implementationGate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this); var rid = RID.Allocate();
            _external.Add(rid, new(font, font || TextServerManager.IsCreatingConsumerBuffer(this) ? 0 : Environment.CurrentManagedThreadId, duplicate) { Identity = rid });
            ExternalOwners.TryAdd(rid, new(this)); RecordAllocation(rid, font); return rid;
        }
    }
    private void CheckExternalOwner(ExternalEntry entry)
    {
        if (!entry.Font && entry.OwnerThread == 0 && !TextServerManager.IsConsumerAdmitted(this))
            throw new InvalidOperationException("A private engine buffer requires its consumer admission.");
        if (entry.OwnerThread != 0 && entry.OwnerThread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("The shaped buffer belongs to another creating thread.");
    }
    private ExternalEntry? BeginExternalRetirement(RID rid)
    {
        lock (_implementationGate)
        {
            if (!_external.TryGetValue(rid, out var entry)) return null;
            CheckExternalOwner(entry);
            if (entry.ResourceOwner is not null) throw new InvalidOperationException("A font resource owns this borrowed source identity.");
            if (entry.Retiring || entry.Readers != 0) throw new InvalidOperationException("An active external source cannot retire.");
            entry.Retiring = true; entry.Generation++; return entry;
        }
    }
    private void CompleteExternalRetirement(RID rid)
    {
        ProviderFont? projection;
        lock (_implementationGate) { projection = _external.GetValueOrDefault(rid)?.Projection; _external.Remove(rid); ExternalOwners.TryRemove(rid, out _); }
        projection?.RetireFromProvider();
    }
    private bool HasExternalRID(RID rid)
    { lock (_implementationGate) return _external.TryGetValue(rid, out var entry) && !entry.Retiring && !entry.RetireWhenIdle; }
}
