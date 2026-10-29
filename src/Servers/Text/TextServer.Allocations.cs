namespace Electron2D;

public abstract partial class TextServer
{
    [ThreadStatic] private static AllocationScope? _allocationScope;
    private readonly record struct Allocation(TextServer Owner, RID RID, bool Font);
    private sealed class AllocationScope(AllocationScope? parent)
    {
        internal readonly AllocationScope? Parent = parent;
        internal readonly List<Allocation> Created = [];
        internal bool RollingBack;
    }
    private static void CheckFactoryAllocation()
    {
        for (var scope = _allocationScope; scope is not null; scope = scope.Parent)
            if (scope.RollingBack) throw new InvalidOperationException("Text resource cleanup cannot allocate new identities during a failed factory rollback.");
    }
    private void RecordAllocation(RID rid, bool font)
    { for (var scope = _allocationScope; scope is not null; scope = scope.Parent) scope.Created.Add(new(this, rid, font)); }
    private bool HasOwnedRole(RID rid, bool font)
    {
        lock (_implementationGate) if (_external.TryGetValue(rid, out var entry)) return entry.Font == font && !entry.Retiring;
        if (font) { lock (_fontsGate) return _fonts.TryGetValue(rid, out var source) && !source.IsDisposed && !source.Retiring; }
        lock (_buffersGate) return _owned.TryGetValue(rid, out var buffer) && !buffer.IsDisposed;
    }
    private RID CreateOwnedRID(bool font, Func<RID> create)
    {
        CheckFactoryAllocation(); var scope = new AllocationScope(_allocationScope); _allocationScope = scope;
        try
        {
            var rid = create(); var fresh = false;
            if (this is TextServerDummy && rid == default && scope.Created.Count == 0) return rid;
            foreach (var entry in scope.Created) if (ReferenceEquals(entry.Owner, this) && entry.RID == rid && entry.Font == font) { fresh = true; break; }
            if (!fresh || !HasOwnedRole(rid, font)) throw new InvalidOperationException("A text resource factory must return a fresh live identity of its requested role owned by this implementation.");
            return rid;
        }
        catch (Exception failure)
        {
            scope.RollingBack = true; List<Exception>? errors = null;
            for (var i = scope.Created.Count - 1; i >= 0; i--)
            {
                var entry = scope.Created[i];
                if (entry.Owner.IsOwnedRID(entry.RID)) try { entry.Owner.FreeRID(entry.RID); } catch (Exception error) { Node.CollectException(ref errors, error); }
            }
            if (errors is not null) { errors.Insert(0, failure); throw new AggregateException("Text factory rollback failed.", errors); }
            throw;
        }
        finally { _allocationScope = scope.Parent; }
    }
    private RID AdoptBuffer(TextServer creator, RID rid)
    {
        if (ReferenceEquals(creator, this)) return rid;
        TextLine line;
        lock (creator._buffersGate)
        {
            if (!creator._owned.Remove(rid, out line!)) throw new InvalidOperationException("The native buffer copy has no transferable owner.");
        }
        return Own(line);
    }
}
