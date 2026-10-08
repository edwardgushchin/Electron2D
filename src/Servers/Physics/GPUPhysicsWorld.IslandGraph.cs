using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2Islands;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct GraphIsland
    {
        internal int Parent, ID, Removed, Padding;
        internal int BodyHead, BodyTail, BodyCount, Padding1;
        internal int ContactHead, ContactTail, ContactCount, Padding2;
        internal int JointHead, JointTail, JointCount, Padding3;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct GraphChange
    {
        internal int Contact;
        internal uint Generation;
        internal int A, B, Link, Root, Freed, Padding;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct GraphStep { internal int Bodies, Contacts, Joints, Islands, Operation, Changes, Updates, OutputCapacity; }

    [StructLayout(LayoutKind.Sequential)]
    private struct GraphStatus { internal int Error, Merges, OutputWords, Padding; }

    private readonly Storage<GraphIsland> _graphIslands;
    private readonly Storage<SplitMember> _graphBodies, _graphContacts, _graphJoints;
    private readonly Storage<GraphChange> _graphChanges;
    private readonly Storage<GraphStatus> _graphStatus;
    private readonly Action<B2World> _beginIslandChanges, _finishIslandChanges;
    private readonly Func<B2World, B2Contact, bool, bool> _changeContactIsland;
    private B2World? _graphWorld;
    private int _graphCount, _graphCursor, _graphRemovals;
    private bool _graphReady;
    private bool[] _graphExpectedContacts = [], _graphFreed = [];
    internal long IslandChangeCount { get; private set; }
    internal long MergedIslandCount { get; private set; }
    internal long IslandGraphTransferBytes { get; private set; }
    internal readonly double[] IslandGraphProfileMS = new double[6];

    private void RecordGraphProfile(int phase, ref long start)
    {
        if (!PhysicsSpace.ProfilingEnabled) return;
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        IslandGraphProfileMS[phase] += System.Diagnostics.Stopwatch.GetElapsedTime(start, now).TotalMilliseconds;
        start = now;
    }

    internal void EnableIslandChanges(B2World world)
    {
        EnsureOwner();
        DetachIslandChanges(); _graphWorld = world;
        world.islandGraphChanged = _graphChanged;
        world.beginIslandChanges = _beginIslandChanges;
        world.changeContactIsland = _changeContactIsland;
        world.finishIslandChanges = _finishIslandChanges;
    }

    private void DetachIslandChanges()
    {
        if (_graphWorld is not null)
        {
            if (_graphWorld.islandGraphChanged == _graphChanged) _graphWorld.islandGraphChanged = null!;
            if (_graphWorld.beginIslandChanges == _beginIslandChanges) _graphWorld.beginIslandChanges = null!;
            if (_graphWorld.changeContactIsland == _changeContactIsland) _graphWorld.changeContactIsland = null!;
            if (_graphWorld.finishIslandChanges == _finishIslandChanges) _graphWorld.finishIslandChanges = null!;
        }
        _graphWorld = null; _graphReady = false; _residentGraph = null; ClearGraphJournal();
    }

    private void BeginIslandChanges(B2World world)
    {
        EnsureOwner();
        if (_graphReady || !ReferenceEquals(world, _graphWorld)) throw new InvalidOperationException("The GPU island graph batch is not available.");
        var profile = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        _graphCount = _graphCursor = _graphRemovals = 0;
        _graphChanges.Reserve(Math.Max(1, world.contacts.count));
        ref var bits = ref world.taskContexts.data[0].contactStateBitSet;
        for (var block = 0; block < bits.blockCount; block++)
        {
            var value = bits.bits[block];
            while (value != 0)
            {
                var id = 64 * block + System.Numerics.BitOperations.TrailingZeroCount(value); value &= value - 1;
                var c = world.contacts.data[id]; var flags = b2GetContactSim(world, c).simFlags;
                var link = (flags & (uint)B2ContactSimFlags.b2_simDisjoint) == 0 && (flags & (uint)B2ContactSimFlags.b2_simStartedTouching) != 0;
                if (!link && (c.islandId < 0 || (flags & (uint)(B2ContactSimFlags.b2_simDisjoint | B2ContactSimFlags.b2_simStoppedTouching)) == 0)) continue;
                if (!link) _graphRemovals++;
                _graphChanges.Data[_graphCount++] = new()
                {
                    Contact = id,
                    Generation = c.generation,
                    A = world.bodies.data[c.edges[0].bodyId].islandId,
                    B = world.bodies.data[c.edges[1].bodyId].islandId,
                    Link = link ? 1 : 0,
                    Root = -1,
                    Freed = -1
                };
            }
        }
        if (_graphCount == 0) { RecordGraphProfile(0, ref profile); return; }
        PrepareGraphResidency(world);
        RecordGraphProfile(0, ref profile);
        DispatchIslandChanges(world);
        profile = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        ValidateIslandChanges(world);
        b2Array_Reserve(ref world.islandIdPool.freeArray, checked(world.islandIdPool.freeArray.count + _graphStatus.Data[0].Merges));
        ClearGraphJournal();
        _graphReady = true;
        RecordGraphProfile(4, ref profile);
    }

    private void DispatchIslandChanges(B2World world)
    {
        var first = true;
        while (true)
        {
            var profile = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            var command = SDL.AcquireGPUCommandBuffer(Device); if (command == 0) throw Failure("acquire island graph commands");
            nint fence = 0;
            try
            {
                if (first)
                {
                    var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw Failure("begin island graph upload");
                    long bytes;
                    if (_graphSnapshot)
                    {
                        _graphIslands.Upload(copy, world.islands.count); _graphBodies.Upload(copy, world.bodies.count);
                        _graphContacts.Upload(copy, world.contacts.count); _graphJoints.Upload(copy, world.joints.count);
                        bytes = (long)world.islands.count * sizeof(GraphIsland) + (long)(world.bodies.count + world.contacts.count + world.joints.count) * sizeof(SplitMember);
                    }
                    else { _graphUpdates.Upload(copy, _graphUpdateCount); bytes = (long)_graphUpdateCount * sizeof(GraphUpdate); }
                    _graphChanges.Upload(copy, _graphCount); bytes += (long)_graphCount * sizeof(GraphChange); SDL.EndGPUCopyPass(copy);
                    IslandGraphUploadBytes += bytes; IslandGraphTransferBytes += bytes;
                    if (_graphUpdateCount != 0) GraphPass(command, world, 4, _graphUpdateCount);
                    GraphPass(command, world, 3, _graphKeys);
                    GraphPass(command, world, 0, 1);
                    GraphPass(command, world, 7, _graphCount);
                    var follow = 0;
                    for (var width = 1; width <= _graphCount - _graphRemovals; width *= 2)
                    { GraphPass(command, world, 8, world.islands.count + _graphCount, follow); follow = 1 - follow; }
                    GraphPass(command, world, 9, world.islands.count + _graphCount, follow);
                    if (_graphRemovals != 0) GraphPass(command, world, 10, _graphCount);
                    GraphPass(command, world, 1, Math.Max(world.bodies.count, Math.Max(world.contacts.count, world.joints.count)));
                    GraphPass(command, world, 2, world.islands.count);
                    GraphPass(command, world, 11, world.contacts.count);
                    var skip = 0;
                    for (var width = 1; width <= _graphRemovals; width *= 2)
                    { GraphPass(command, world, 12, world.contacts.count, skip); skip = 1 - skip; }
                    GraphPass(command, world, 13, world.contacts.count, skip);
                    GraphPass(command, world, 14, world.islands.count, skip);
                    GraphPass(command, world, 15, world.contacts.count);
                    if (_graphRemovals != 0) GraphPass(command, world, 16, _graphCount);
                }
                else GraphPass(command, world, 6, 1); // Repack completed output; never execute a merge twice.
                GraphPass(command, world, 5, _graphKeys);
                var readback = SDL.BeginGPUCopyPass(command); if (readback == 0) throw Failure("begin island graph readback");
                _graphOutput.Download(readback, _graphOutput.Data.Length);
                _graphChanges.Download(readback, _graphCount); _graphStatus.Download(readback, 1); SDL.EndGPUCopyPass(readback);
                var bytesRead = (long)_graphOutput.Data.Length * sizeof(int) + (long)_graphCount * sizeof(GraphChange) + sizeof(GraphStatus);
                IslandGraphReadbackBytes += bytesRead; IslandGraphTransferBytes += bytesRead;
                RecordGraphProfile(1, ref profile);
                var submitted = command; command = 0; fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
                if (fence == 0) throw Failure("submit island graph changes");
                Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for island graph changes");
                RecordGraphProfile(2, ref profile);
                _graphStatus.Read(1);
                if (_graphStatus.Data[0].Error != 0) throw new InvalidOperationException($"GPU island graph error {_graphStatus.Data[0].Error}.");
                var words = _graphStatus.Data[0].OutputWords;
                var maximum = checked(world.islands.count * 17 + (world.bodies.count + world.contacts.count + world.joints.count) * 5);
                if (words < 0 || words > maximum) throw new InvalidOperationException("GPU graph output count is invalid.");
                if (words > _graphOutput.Data.Length)
                {
                    _graphOutput.Reserve(words); IslandGraphReadbackRetries++; first = false; RecordGraphProfile(3, ref profile); continue;
                }
                _graphChanges.Read(_graphCount); ReadGraphOutput(world);
                RecordGraphProfile(3, ref profile);
                return;
            }
            finally
            {
                if (command != 0) SDL.CancelGPUCommandBuffer(command);
                if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
            }
        }
    }

    private void GraphPass(nint command, B2World world, int operation, int count, int parity = -1)
    {
        Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[8];
        bindings[0] = new() { Buffer = _graphIslands.Handle }; bindings[1] = new() { Buffer = _graphBodies.Handle };
        bindings[2] = new() { Buffer = _graphContacts.Handle }; bindings[3] = new() { Buffer = _graphJoints.Handle };
        bindings[4] = new() { Buffer = _graphChanges.Handle }; bindings[5] = new() { Buffer = _graphStatus.Handle };
        bindings[6] = new() { Buffer = _graphDirtyStorage.Handle }; bindings[7] = new() { Buffer = _graphOutput.Handle };
        var pass = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 8);
        if (pass == 0) throw Failure("begin island graph pass");
        SDL.BindGPUComputePipeline(pass, _islandGraphPipeline.DangerousGetHandle());
        var update = _graphUpdates.Handle; SDL.BindGPUComputeStorageBuffers(pass, 0, (nint)(&update), 1);
        var step = new GraphStep { Bodies = world.bodies.count, Contacts = world.contacts.count, Joints = world.joints.count, Islands = world.islands.count, Operation = operation, Changes = _graphCount, Updates = parity >= 0 ? parity : _graphUpdateCount, OutputCapacity = _graphOutput.Data.Length };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&step), (uint)sizeof(GraphStep));
        SDL.DispatchGPUCompute(pass, checked((uint)(count + 63) / 64), 1, 1); SDL.EndGPUComputePass(pass); DispatchCount++;
    }

    private void ValidateIslandChanges(B2World world)
    {
        var merges = 0; var previous = -1;
        for (var i = 0; i < _graphCount; i++)
        {
            var op = _graphChanges.Data[i];
            if (op.Contact <= previous || (uint)op.Contact >= (uint)world.contacts.count ||
                world.contacts.data[op.Contact].generation != op.Generation || (uint)op.Root >= (uint)world.islands.count ||
                world.islands.data[op.Root].islandId != op.Root || (op.Link != 0 && op.Link != 1))
                throw new InvalidOperationException("GPU island changes returned invalid contact identities.");
            if (op.Freed >= 0)
            {
                if (op.Link == 0 || (uint)op.Freed >= (uint)world.islands.count || op.Freed == op.Root || _graphFreed[op.Freed] || world.islands.data[op.Freed].islandId != op.Freed)
                    throw new InvalidOperationException("GPU island changes returned invalid freed islands.");
                _graphFreed[op.Freed] = true; merges++;
            }
            previous = op.Contact;
        }
        if (merges != _graphStatus.Data[0].Merges) throw new InvalidOperationException("GPU island merge count differs.");
        var bodies = 0; var contacts = 0; var joints = 0;
        for (var i = 0; i < world.islands.count; i++)
        {
            var g = _graphIslands.Data[i];
            if (world.islands.data[i].islandId < 0) continue;
            if (g.ID != (_graphFreed[i] ? -1 : i) || g.Removed < 0 || (_graphFreed[i] ? g.Parent != -1 || g.BodyCount != 0 || g.ContactCount != 0 || g.JointCount != 0 : g.Parent != i || g.BodyCount <= 0))
                throw new InvalidOperationException("GPU island descriptor differs.");
            if (_graphFreed[i]) continue;
            ValidateGraphList(g.BodyHead, g.BodyTail, g.BodyCount, i, _graphBodies.Data, world.bodies.count);
            ValidateGraphList(g.ContactHead, g.ContactTail, g.ContactCount, i, _graphContacts.Data, world.contacts.count);
            ValidateGraphList(g.JointHead, g.JointTail, g.JointCount, i, _graphJoints.Data, world.joints.count);
            bodies += g.BodyCount; contacts += g.ContactCount; joints += g.JointCount;
        }
        for (var i = 0; i < world.bodies.count; i++)
            if (world.bodies.data[i].id >= 0) { ValidateGraphMember(_graphBodies.Data[i], world.bodies.data[i].islandId >= 0, world.islands.count); if (_graphBodies.Data[i].Root >= 0) bodies--; }
        for (var i = 0; i < world.contacts.count; i++)
            if (world.contacts.data[i].contactId >= 0) { ValidateGraphMember(_graphContacts.Data[i], _graphExpectedContacts[i], world.islands.count); if (_graphContacts.Data[i].Root >= 0) contacts--; }
        for (var i = 0; i < world.joints.count; i++)
            if (world.joints.data[i].jointId >= 0) { ValidateGraphMember(_graphJoints.Data[i], world.joints.data[i].islandId >= 0, world.islands.count); if (_graphJoints.Data[i].Root >= 0) joints--; }
        if (bodies != 0 || contacts != 0 || joints != 0) throw new InvalidOperationException("GPU island lists omitted or duplicated members.");
    }

    private void ValidateGraphMember(SplitMember member, bool linked, int islands)
    {
        if (member.Visited != 1 || (linked ? (uint)member.Root >= (uint)islands || _graphIslands.Data[member.Root].ID != member.Root || _graphFreed[member.Root] : member.Root != -1))
            throw new InvalidOperationException("GPU island member belongs to an invalid component.");
    }

    private static void ValidateGraphList(int head, int tail, int count, int root, SplitMember[] members, int limit)
    {
        if (count < 0 || count > limit) throw new InvalidOperationException("GPU island list has an invalid count.");
        var id = head; var previous = -1;
        for (var i = 0; i < count; i++)
        {
            if ((uint)id >= (uint)limit) throw new InvalidOperationException("GPU island list has an invalid member.");
            var member = members[id];
            if (member.Root != root || member.Prev != previous || member.Visited != 1) throw new InvalidOperationException("GPU island list has invalid links.");
            previous = id; id = member.Next;
        }
        if (id != -1 || previous != tail) throw new InvalidOperationException("GPU island list is cyclic or truncated.");
    }

    private bool ChangeContactIsland(B2World world, B2Contact contact, bool link)
    {
        EnsureOwner();
        if (!_graphReady) return false; // Authoring outside collision keeps its immediate CPU path.
        if (!ReferenceEquals(world, _graphWorld) || _graphCursor >= _graphCount) throw new InvalidOperationException("GPU island publication is out of order.");
        var op = _graphChanges.Data[_graphCursor++];
        if (op.Contact != contact.contactId || op.Generation != contact.generation || op.Link != (link ? 1 : 0) || world.islands.data[op.Root].islandId != op.Root)
            throw new InvalidOperationException("GPU island publication contact differs.");
        if (op.Freed >= 0) b2DestroyIsland(world, op.Freed);
        if (!link) contact.islandId = contact.islandPrev = contact.islandNext = -1;
        return true;
    }

    private void FinishIslandChanges(B2World world)
    {
        EnsureOwner(); if (!_graphReady) return;
        var profile = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        if (!ReferenceEquals(world, _graphWorld) || _graphCursor != _graphCount) throw new InvalidOperationException("GPU island changes were not fully published.");
        foreach (var key in _graphResults)
        {
            var i = key >> 2;
            switch (key & 3)
            {
                case 0:
                    var m = _graphIslands.Data[i]; var g = world.islands.data[i];
                    g.headBody = m.BodyHead; g.tailBody = m.BodyTail; g.bodyCount = m.BodyCount;
                    g.headContact = m.ContactHead; g.tailContact = m.ContactTail; g.contactCount = m.ContactCount;
                    g.headJoint = m.JointHead; g.tailJoint = m.JointTail; g.jointCount = m.JointCount; g.constraintRemoveCount = m.Removed;
                    break;
                case 1:
                    var b = world.bodies.data[i]; var bm = _graphBodies.Data[i]; b.islandId = bm.Root; b.islandPrev = bm.Prev; b.islandNext = bm.Next;
                    break;
                case 2:
                    var c = world.contacts.data[i]; var cm = _graphContacts.Data[i]; c.islandId = cm.Root; c.islandPrev = cm.Prev; c.islandNext = cm.Next;
                    break;
                case 3:
                    var j = world.joints.data[i]; var jm = _graphJoints.Data[i]; j.islandId = jm.Root; j.islandPrev = jm.Prev; j.islandNext = jm.Next;
                    break;
            }
        }
        _residentGraph = world.islandIdPool;
        if (_graphSnapshot) IslandGraphSnapshotCount++;
        IslandChangeCount += _graphCount; MergedIslandCount += _graphStatus.Data[0].Merges;
        _graphReady = false;
        RecordGraphProfile(5, ref profile);
    }
}
