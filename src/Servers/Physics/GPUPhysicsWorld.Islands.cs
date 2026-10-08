using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Islands;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SplitBody { internal int ID, Contact, Joint, Padding; }
    [StructLayout(LayoutKind.Sequential)]
    private struct SplitEdge { internal int ID, A, B, Flags, NextA, NextB, Padding1, Padding2; }
    [StructLayout(LayoutKind.Sequential)]
    private struct SplitMember { internal int Root, Prev, Next, Visited; }
    [StructLayout(LayoutKind.Sequential)]
    private struct SplitGroup
    {
        internal int BodyHead, BodyTail, BodyCount, Offset;
        internal int ContactHead, ContactTail, ContactCount, Padding1;
        internal int JointHead, JointTail, JointCount, Padding2;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct SplitStatus { internal int Changed, Error, Groups, Padding; }
    [StructLayout(LayoutKind.Sequential)]
    private struct SplitStep { internal int Operation, Bodies, Contacts, Joints, Leaf, Offset, Width, Padding; }

    private readonly Storage<SplitBody> _splitBodyStorage;
    private readonly Storage<SplitEdge> _splitContactStorage, _splitJointStorage;
    private readonly Storage<SplitMember> _splitBodyResult, _splitContactResult, _splitJointResult;
    private readonly Storage<SplitGroup> _splitGroupStorage;
    private readonly Storage<int> _splitStackStorage, _splitScanStorage;
    private readonly Storage<SplitStatus> _splitStatusStorage;
    private readonly Action<B2World, int> _splitIsland;
    private B2World? _splitWorld;
    private int[] _splitBodyMap = [], _splitContactMap = [], _splitJointMap = [], _splitIDs = [];
    private int[] _splitBodyStamps = [], _splitContactStamps = [], _splitJointStamps = [];
    private int _splitStamp, _splitBodies, _splitContacts, _splitJoints, _splitLeaf;
    internal long SplitIslandCount { get; private set; }
    internal long SplitComponentCount { get; private set; }
    internal long SplitConvergenceBatches { get; private set; }

    internal void EnableIslandSplitting(B2World world)
    {
        EnsureOwner();
        if (_splitWorld is not null && _splitWorld.splitIsland == _splitIsland) _splitWorld.splitIsland = null!;
        _splitWorld = world; world.splitIsland = _splitIsland;
    }

    internal void SplitIsland(B2World world, int baseID)
    {
        EnsureOwner();
        var source = world.islands.data[baseID];
        if (source.islandId != baseID) throw new InvalidOperationException("The split island is no longer alive.");
        if (source.setIndex != (int)B2SolverSetType.b2_awakeSet || source.constraintRemoveCount == 0) return;
        PackSplit(world, source);
        DispatchSplit();
        ValidateSplit(world, source);
        PublishSplit(world, source);
        SplitIslandCount++; SplitComponentCount += _splitStatusStorage.Data[0].Groups;
    }

    private static void ReserveSplitMap(ref int[] map, ref int[] stamps, int size)
    {
        if (map.Length >= size) return;
        Array.Resize(ref map, size); Array.Resize(ref stamps, size);
    }

    private void PackSplit(B2World world, B2Island source)
    {
        _splitBodies = source.bodyCount; _splitContacts = _splitJoints = 0;
        if (_splitBodies <= 0) throw new InvalidOperationException("An awake island must contain bodies.");
        ReserveSplitMap(ref _splitBodyMap, ref _splitBodyStamps, world.bodies.capacity);
        ReserveSplitMap(ref _splitContactMap, ref _splitContactStamps, world.contacts.capacity);
        ReserveSplitMap(ref _splitJointMap, ref _splitJointStamps, world.joints.capacity);
        if (_splitStamp == int.MaxValue)
        {
            Array.Clear(_splitBodyStamps); Array.Clear(_splitContactStamps); Array.Clear(_splitJointStamps); _splitStamp = 0;
        }
        _splitStamp++;
        _splitBodyStorage.Reserve(_splitBodies); _splitBodyResult.Reserve(_splitBodies); _splitGroupStorage.Reserve(_splitBodies);
        _splitContactStorage.Reserve(Math.Max(1, world.contacts.capacity)); _splitContactResult.Reserve(_splitContactStorage.Data.Length);
        _splitJointStorage.Reserve(Math.Max(1, world.joints.capacity)); _splitJointResult.Reserve(_splitJointStorage.Data.Length);
        _splitStackStorage.Reserve(_splitBodies); _splitStatusStorage.Reserve(1);
        _splitLeaf = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)_splitBodies));
        _splitScanStorage.Reserve(checked(2 * _splitLeaf));
        if (_splitIDs.Length < _splitBodyStorage.Data.Length) Array.Resize(ref _splitIDs, _splitBodyStorage.Data.Length);
        var bodyID = source.headBody;
        for (var i = 0; i < _splitBodies; i++)
        {
            if ((uint)bodyID >= (uint)world.bodies.count || _splitBodyStamps[bodyID] == _splitStamp)
                throw new InvalidOperationException("The source island body list is invalid.");
            var body = world.bodies.data[bodyID];
            if (body.islandId != source.islandId || body.setIndex != source.setIndex)
                throw new InvalidOperationException("An island body belongs to a different solver set.");
            _splitBodyMap[bodyID] = i; _splitBodyStamps[bodyID] = _splitStamp;
            _splitBodyStorage.Data[i] = new() { ID = bodyID, Contact = body.headContactKey, Joint = body.headJointKey };
            var key = body.headContactKey; var visited = 0;
            while (key >= 0)
            {
                var id = key >> 1;
                if (++visited > body.contactCount || (uint)id >= (uint)world.contacts.count) throw new InvalidOperationException("Invalid contact adjacency while splitting.");
                if (_splitContactStamps[id] != _splitStamp)
                {
                    _splitContactStamps[id] = _splitStamp; _splitContactMap[id] = _splitContacts;
                    _splitContactStorage.Data[_splitContacts++].ID = id;
                }
                key = world.contacts.data[id].edges[key & 1].nextKey;
            }
            key = body.headJointKey; visited = 0;
            while (key >= 0)
            {
                var id = key >> 1;
                if (++visited > body.jointCount || (uint)id >= (uint)world.joints.count) throw new InvalidOperationException("Invalid joint adjacency while splitting.");
                if (_splitJointStamps[id] != _splitStamp)
                {
                    _splitJointStamps[id] = _splitStamp; _splitJointMap[id] = _splitJoints;
                    _splitJointStorage.Data[_splitJoints++].ID = id;
                }
                key = world.joints.data[id].edges[key & 1].nextKey;
            }
            bodyID = body.islandNext;
        }
        if (bodyID != -1) throw new InvalidOperationException("The source island body count differs.");
        for (var i = 0; i < _splitBodies; i++)
        {
            ref var body = ref _splitBodyStorage.Data[i];
            body.Contact = SplitKey(body.Contact, _splitContactMap, _splitContactStamps);
            body.Joint = SplitKey(body.Joint, _splitJointMap, _splitJointStamps);
        }
        for (var i = 0; i < _splitContacts; i++)
        {
            var c = world.contacts.data[_splitContactStorage.Data[i].ID];
            _splitContactStorage.Data[i] = new()
            {
                ID = c.contactId,
                A = SplitBodyIndex(world, c.edges[0].bodyId),
                B = SplitBodyIndex(world, c.edges[1].bodyId),
                Flags = (c.flags & (uint)B2ContactFlags.b2_contactTouchingFlag) != 0 ? 1 : 0,
                NextA = SplitKey(c.edges[0].nextKey, _splitContactMap, _splitContactStamps),
                NextB = SplitKey(c.edges[1].nextKey, _splitContactMap, _splitContactStamps)
            };
        }
        for (var i = 0; i < _splitJoints; i++)
        {
            var j = world.joints.data[_splitJointStorage.Data[i].ID];
            var a = world.bodies.data[j.edges[0].bodyId]; var b = world.bodies.data[j.edges[1].bodyId];
            _splitJointStorage.Data[i] = new()
            {
                ID = j.jointId,
                A = SplitBodyIndex(world, a.id),
                B = SplitBodyIndex(world, b.id),
                Flags = (j.setIndex != (int)B2SolverSetType.b2_disabledSet ? 1 : 0) |
                    (a.type == B2BodyType.b2_dynamicBody ? 2 : 0) | (b.type == B2BodyType.b2_dynamicBody ? 4 : 0) |
                    (a.setIndex == (int)B2SolverSetType.b2_disabledSet || b.setIndex == (int)B2SolverSetType.b2_disabledSet ? 8 : 0),
                NextA = SplitKey(j.edges[0].nextKey, _splitJointMap, _splitJointStamps),
                NextB = SplitKey(j.edges[1].nextKey, _splitJointMap, _splitJointStamps)
            };
        }
    }

    private int SplitBodyIndex(B2World world, int id) => _splitBodyStamps[id] == _splitStamp ? _splitBodyMap[id] :
        world.bodies.data[id].setIndex == (int)B2SolverSetType.b2_staticSet ? -1 : -2;
    private int SplitKey(int key, int[] map, int[] stamps) => key >= 0 && stamps[key >> 1] == _splitStamp ? 2 * map[key >> 1] + (key & 1) : -1;

    private void DispatchSplit()
    {
        var rounds = Math.Min(16, (int)System.Numerics.BitOperations.Log2((uint)_splitBodies) + 1);
        for (var completed = 0; completed < _splitBodies; completed += rounds)
        {
            var command = SDL.AcquireGPUCommandBuffer(Device);
            if (command == 0) throw Failure("acquire island split commands");
            nint fence = 0;
            try
            {
                if (completed == 0)
                {
                    var copy = SDL.BeginGPUCopyPass(command);
                    if (copy == 0) throw Failure("begin island split upload");
                    _splitBodyStorage.Upload(copy, _splitBodies); _splitContactStorage.Upload(copy, _splitContacts); _splitJointStorage.Upload(copy, _splitJoints);
                    SDL.EndGPUCopyPass(copy); SplitPass(command, 0, Math.Max(_splitBodies, Math.Max(_splitContacts, _splitJoints)));
                }
                for (var i = 0; i < rounds; i++)
                {
                    if (_splitContacts + _splitJoints > 0) SplitPass(command, 1, _splitContacts + _splitJoints);
                    SplitPass(command, 2, _splitBodies);
                }
                SplitPass(command, 3, 1);
                SplitPass(command, 4, Math.Max(_splitBodies, _splitContacts + _splitJoints));
                SplitPass(command, 5, _splitBodies);
                SplitPass(command, 6, _splitLeaf);
                for (var width = _splitLeaf / 2; width > 0; width /= 2) SplitPass(command, 7, width, width);
                SplitPass(command, 8, _splitBodies);
                var readback = SDL.BeginGPUCopyPass(command);
                if (readback == 0) throw Failure("begin island split readback");
                _splitBodyResult.Download(readback, _splitBodies); _splitContactResult.Download(readback, _splitContacts); _splitJointResult.Download(readback, _splitJoints);
                _splitGroupStorage.Download(readback, _splitBodies); _splitStatusStorage.Download(readback, 1); SDL.EndGPUCopyPass(readback);
                var submitted = command; command = 0;
                fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
                if (fence == 0) throw Failure("submit island split");
                Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for island split");
                SplitConvergenceBatches++; _splitStatusStorage.Read(1);
                if (_splitStatusStorage.Data[0].Error != 0) throw new InvalidOperationException($"GPU island split error {_splitStatusStorage.Data[0].Error}.");
                if (_splitStatusStorage.Data[0].Changed != 0) continue;
                _splitBodyResult.Read(_splitBodies); _splitContactResult.Read(_splitContacts); _splitJointResult.Read(_splitJoints); _splitGroupStorage.Read(_splitBodies);
                return;
            }
            finally
            {
                if (command != 0) SDL.CancelGPUCommandBuffer(command);
                if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
            }
        }
        throw new InvalidOperationException("GPU island connectivity did not converge.");
    }

    private void SplitPass(nint command, int operation, int count, int offset = 0)
    {
        Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[7];
        bindings[0] = new() { Buffer = _splitBodyResult.Handle }; bindings[1] = new() { Buffer = _splitContactResult.Handle };
        bindings[2] = new() { Buffer = _splitJointResult.Handle }; bindings[3] = new() { Buffer = _splitGroupStorage.Handle };
        bindings[4] = new() { Buffer = _splitStackStorage.Handle }; bindings[5] = new() { Buffer = _splitScanStorage.Handle }; bindings[6] = new() { Buffer = _splitStatusStorage.Handle };
        var pass = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 7);
        if (pass == 0) throw Failure("begin island split pass");
        SDL.BindGPUComputePipeline(pass, _islandSplitPipeline.DangerousGetHandle());
        var inputs = stackalloc nint[3] { _splitBodyStorage.Handle, _splitContactStorage.Handle, _splitJointStorage.Handle };
        SDL.BindGPUComputeStorageBuffers(pass, 0, (nint)inputs, 3);
        var step = new SplitStep { Operation = operation, Bodies = _splitBodies, Contacts = _splitContacts, Joints = _splitJoints, Leaf = _splitLeaf, Offset = offset };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&step), (uint)sizeof(SplitStep));
        SDL.DispatchGPUCompute(pass, checked((uint)(count + 63) / 64), 1, 1); SDL.EndGPUComputePass(pass); DispatchCount++;
    }

    private void ValidateSplit(B2World world, B2Island source)
    {
        var bodies = 0; var contacts = 0; var joints = 0; var groups = 0;
        for (var i = 0; i < _splitBodies; i++)
        {
            var group = _splitGroupStorage.Data[i];
            if (group.BodyCount == 0) continue;
            if (group.BodyCount < 0 || group.ContactCount < 0 || group.JointCount < 0 || group.BodyHead != _splitBodyStorage.Data[i].ID)
                throw new InvalidOperationException("GPU island split returned invalid component counts or seed.");
            ValidateSplitList(group.BodyHead, group.BodyTail, group.BodyCount, i, _splitBodyMap, _splitBodyStamps, _splitBodyResult.Data);
            ValidateSplitList(group.ContactHead, group.ContactTail, group.ContactCount, i, _splitContactMap, _splitContactStamps, _splitContactResult.Data);
            ValidateSplitList(group.JointHead, group.JointTail, group.JointCount, i, _splitJointMap, _splitJointStamps, _splitJointResult.Data);
            bodies = checked(bodies + group.BodyCount); contacts = checked(contacts + group.ContactCount); joints = checked(joints + group.JointCount); groups++;
        }
        if (bodies != source.bodyCount || contacts != source.contactCount || joints != source.jointCount || groups != _splitStatusStorage.Data[0].Groups)
            throw new InvalidOperationException("GPU island split omitted or duplicated members.");
        for (var i = 0; i < _splitBodies; i++)
            if (!SplitRootValid(_splitBodyResult.Data[i].Root)) throw new InvalidOperationException("GPU island split returned an invalid body component.");
        for (var i = 0; i < _splitContacts; i++)
        {
            var root = _splitContactResult.Data[i].Root;
            if (world.contacts.data[_splitContactStorage.Data[i].ID].islandId == source.islandId ? !SplitRootValid(root) : root != -1)
                throw new InvalidOperationException("GPU island split changed contact membership.");
        }
        for (var i = 0; i < _splitJoints; i++)
        {
            var root = _splitJointResult.Data[i].Root;
            if (world.joints.data[_splitJointStorage.Data[i].ID].islandId == source.islandId ? !SplitRootValid(root) : root != -1)
                throw new InvalidOperationException("GPU island split changed joint membership.");
        }
    }

    private bool SplitRootValid(int root) => (uint)root < (uint)_splitBodies && _splitGroupStorage.Data[root].BodyCount > 0;

    private void ValidateSplitList(int head, int tail, int count, int root, int[] map, int[] stamps, SplitMember[] members)
    {
        var id = head; var previous = -1;
        for (var i = 0; i < count; i++)
        {
            if ((uint)id >= (uint)map.Length || stamps[id] != _splitStamp) throw new InvalidOperationException("GPU island member is stale.");
            var member = members[map[id]];
            if (member.Root != root || member.Prev != previous || member.Visited != 1) throw new InvalidOperationException("GPU island member links differ.");
            previous = id; id = member.Next;
        }
        if (id != -1 || previous != tail) throw new InvalidOperationException("GPU island member list is cyclic or truncated.");
    }

    private void PublishSplit(B2World world, B2Island source)
    {
        var groups = _splitStatusStorage.Data[0].Groups; var pool = world.islandIdPool;
        // Reserve every possible allocation before changing live membership.
        b2Array_Reserve(ref world.islands, checked(pool.nextIndex + Math.Max(0, groups - pool.freeArray.count)));
        var set = world.solverSets.data[source.setIndex];
        b2Array_Reserve(ref set.islandSims, checked(set.islandSims.count + groups));
        b2Array_Reserve(ref pool.freeArray, checked(pool.freeArray.count + 1));
        for (var i = 0; i < _splitBodies; i++)
        {
            var group = _splitGroupStorage.Data[i];
            if (group.BodyCount == 0) continue;
            var island = b2CreateIsland(world, source.setIndex); _splitIDs[i] = island.islandId;
            island.headBody = group.BodyHead; island.tailBody = group.BodyTail; island.bodyCount = group.BodyCount;
            island.headContact = group.ContactHead; island.tailContact = group.ContactTail; island.contactCount = group.ContactCount;
            island.headJoint = group.JointHead; island.tailJoint = group.JointTail; island.jointCount = group.JointCount;
        }
        for (var i = 0; i < _splitBodies; i++)
        {
            var body = world.bodies.data[_splitBodyStorage.Data[i].ID]; var member = _splitBodyResult.Data[i];
            body.islandId = _splitIDs[member.Root]; body.islandPrev = member.Prev; body.islandNext = member.Next;
            world.islandGraphChanged?.Invoke(1, _splitBodyStorage.Data[i].ID);
        }
        for (var i = 0; i < _splitContacts; i++)
        {
            var member = _splitContactResult.Data[i]; if (member.Root < 0) continue;
            var contact = world.contacts.data[_splitContactStorage.Data[i].ID];
            contact.islandId = _splitIDs[member.Root]; contact.islandPrev = member.Prev; contact.islandNext = member.Next;
            world.islandGraphChanged?.Invoke(2, _splitContactStorage.Data[i].ID);
        }
        for (var i = 0; i < _splitJoints; i++)
        {
            var member = _splitJointResult.Data[i]; if (member.Root < 0) continue;
            var joint = world.joints.data[_splitJointStorage.Data[i].ID];
            joint.islandId = _splitIDs[member.Root]; joint.islandPrev = member.Prev; joint.islandNext = member.Next;
            world.islandGraphChanged?.Invoke(3, _splitJointStorage.Data[i].ID);
        }
        b2DestroyIsland(world, source.islandId);
#if DEBUG
        for (var i = 0; i < _splitBodies; i++) if (_splitGroupStorage.Data[i].BodyCount > 0) b2ValidateIsland(world, _splitIDs[i]);
#endif
    }
}
