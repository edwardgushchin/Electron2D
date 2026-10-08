using System.Runtime.InteropServices;
using Box2D.NET;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct GraphUpdate { internal int Key, Padding1, Padding2, Padding3; internal GraphIsland Value; }
    private readonly Storage<GraphUpdate> _graphUpdates;
    private readonly Storage<int> _graphDirtyStorage, _graphOutput;
    private readonly Action<int, int> _graphChanged;
    private readonly List<int> _graphJournal = [], _graphResults = [];
    private bool[] _graphDirty = [];
    // Independent CPU expectations: GPU readback must never define its own validity.
    private const byte GraphAlive = 1, GraphLinked = 2;
    private byte[] _graphExpected = [];
    private int[] _graphSeen = [];
    private int _graphEpoch, _graphKeys, _graphUpdateCount;
    private B2IdPool? _residentGraph;
    private bool _graphSnapshot;
    internal long IslandGraphSnapshotCount { get; private set; }
    internal long IslandGraphUploadBytes { get; private set; }
    internal long IslandGraphReadbackBytes { get; private set; }
    internal long IslandGraphReadbackRetries { get; private set; }

    private void ReserveGraphJournal(int count)
    {
        if (_graphDirty.Length >= count) return;
        var capacity = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(64, count)));
        Array.Resize(ref _graphDirty, capacity); Array.Resize(ref _graphSeen, capacity);
        Array.Resize(ref _graphExpected, capacity);
        _graphJournal.EnsureCapacity(capacity); _graphResults.EnsureCapacity(capacity);
    }

    private void MarkGraphChanged(int kind, int id)
    {
        // Prepared GPU merges already changed their island records. Lifecycle
        // contact removals still journal the final dead slot for the next batch.
        if (_graphReady && kind == 0) return;
        var key = checked(4 * id + kind);
        ReserveGraphJournal(checked(key + 1));
        if (_graphDirty[key]) return;
        _graphDirty[key] = true; _graphJournal.Add(key);
    }

    private void ClearGraphJournal()
    {
        foreach (var key in _graphJournal) _graphDirty[key] = false;
        _graphJournal.Clear();
    }

    private GraphIsland CaptureGraphRecord(B2World world, int key)
    {
        var id = key >> 2;
        if ((key & 3) == 0)
        {
            var g = world.islands.data[id];
            _graphExpected[key] = g.islandId >= 0 ? GraphAlive : (byte)0;
            var value = new GraphIsland
            {
                ID = g.islandId,
                Parent = g.islandId,
                Removed = g.constraintRemoveCount,
                BodyHead = g.headBody,
                BodyTail = g.tailBody,
                BodyCount = g.bodyCount,
                ContactHead = g.headContact,
                ContactTail = g.tailContact,
                ContactCount = g.contactCount,
                JointHead = g.headJoint,
                JointTail = g.tailJoint,
                JointCount = g.jointCount
            };
            _graphIslands.Data[id] = value; return value;
        }
        SplitMember member;
        if ((key & 3) == 1)
        {
            var b = world.bodies.data[id]; member = new() { Root = b.islandId, Prev = b.islandPrev, Next = b.islandNext, Visited = b.id >= 0 ? 1 : 0 };
            _graphBodies.Data[id] = member;
        }
        else if ((key & 3) == 2)
        {
            var c = world.contacts.data[id]; member = new() { Root = c.islandId, Prev = c.islandPrev, Next = c.islandNext, Visited = c.contactId >= 0 ? 1 : 0 };
            _graphContacts.Data[id] = member;
        }
        else
        {
            var j = world.joints.data[id]; member = new() { Root = j.islandId, Prev = j.islandPrev, Next = j.islandNext, Visited = j.jointId >= 0 ? 1 : 0 };
            _graphJoints.Data[id] = member;
        }
        _graphExpected[key] = member.Visited == 0 ? (byte)0 : (byte)(GraphAlive | (member.Root >= 0 ? GraphLinked : 0));
        return new() { Parent = member.Root, ID = member.Prev, Removed = member.Next, Padding = member.Visited };
    }

    private void PrepareGraphResidency(B2World world)
    {
        _graphSnapshot = !ReferenceEquals(_residentGraph, world.islandIdPool) || world.islandGraphChanged != _graphChanged ||
            world.islands.count > _graphIslands.Data.Length || world.bodies.count > _graphBodies.Data.Length ||
            world.contacts.count > _graphContacts.Data.Length || world.joints.count > _graphJoints.Data.Length;
        world.islandGraphChanged = _graphChanged;
        _residentGraph = null; // Only a successfully validated/published batch becomes reusable.
        _graphIslands.Reserve(Math.Max(1, world.islands.count)); _graphBodies.Reserve(Math.Max(1, world.bodies.count));
        _graphContacts.Reserve(Math.Max(1, world.contacts.count)); _graphJoints.Reserve(Math.Max(1, world.joints.count)); _graphStatus.Reserve(1);
        _graphKeys = checked(4 * Math.Max(world.islands.count, Math.Max(world.bodies.count, Math.Max(world.contacts.count, world.joints.count))));
        ReserveGraphJournal(_graphKeys); _graphDirtyStorage.Reserve(checked(_graphKeys + 6 * (world.islands.count + _graphCount) + 2 * world.contacts.count)); _graphOutput.Reserve(8192);
        _graphUpdates.Reserve(Math.Max(1, _graphJournal.Count)); _graphUpdateCount = 0;
        if (_graphSnapshot)
        {
            for (var i = 0; i < world.islands.count; i++) CaptureGraphRecord(world, 4 * i);
            for (var i = 0; i < world.bodies.count; i++) CaptureGraphRecord(world, 4 * i + 1);
            for (var i = 0; i < world.contacts.count; i++) CaptureGraphRecord(world, 4 * i + 2);
            for (var i = 0; i < world.joints.count; i++) CaptureGraphRecord(world, 4 * i + 3);
        }
        else
            foreach (var key in _graphJournal)
                _graphUpdates.Data[_graphUpdateCount++] = new() { Key = key, Value = CaptureGraphRecord(world, key) };
        if (_graphFreed.Length < _graphIslands.Data.Length) Array.Resize(ref _graphFreed, _graphIslands.Data.Length);
        Array.Clear(_graphFreed);
        for (var i = 0; i < _graphCount; i++)
        {
            var op = _graphChanges.Data[i]; var key = 4 * op.Contact + 2;
            _graphExpected[key] = (byte)((_graphExpected[key] & GraphAlive) | (op.Link != 0 ? GraphLinked : 0));
        }
    }

    private void ReadGraphOutput(B2World world)
    {
        var words = _graphStatus.Data[0].OutputWords;
        if (words < 0 || words > _graphOutput.Data.Length) throw new InvalidOperationException("GPU graph output size is invalid.");
        _graphOutput.Read(words); _graphResults.Clear();
        if (_graphEpoch == int.MaxValue) { Array.Clear(_graphSeen); _graphEpoch = 0; }
        _graphEpoch++;
        for (var at = 0; at < words;)
        {
            var key = _graphOutput.Data[at++]; var id = key >> 2; var kind = key & 3;
            var limit = kind switch { 0 => world.islands.count, 1 => world.bodies.count, 2 => world.contacts.count, _ => world.joints.count };
            var size = kind == 0 ? 16 : 4;
            if (key < 0 || id >= limit || words - at < size || _graphSeen[key] == _graphEpoch)
                throw new InvalidOperationException("GPU graph output contains an invalid or repeated record.");
            _graphSeen[key] = _graphEpoch; _graphResults.Add(key);
            var data = _graphOutput.Data.AsSpan(at, size);
            if (kind == 0) _graphIslands.Data[id] = MemoryMarshal.Cast<int, GraphIsland>(data)[0];
            else
            {
                var value = MemoryMarshal.Cast<int, SplitMember>(data)[0];
                if (kind == 1) _graphBodies.Data[id] = value; else if (kind == 2) _graphContacts.Data[id] = value; else _graphJoints.Data[id] = value;
            }
            at += size;
        }
    }
}
