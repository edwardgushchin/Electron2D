using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using static Box2D.NET.B2Contacts;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ContactSlot
    {
        internal int ShapeA, ShapeB, BodyA, BodyB;
        // Flags/set describe creation or import; the live graph stays in the CPU mirror.
        internal uint Generation, Flags, SimFlags;
        internal int InitialSet;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ContactRequest
    {
        internal int ShapeA, BodyA, TypeA, SetA, ShapeB, BodyB, TypeB, SetB;
        internal Float4 Material;
        internal uint FlagsA, FlagsB, MaterialA, MaterialB;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ContactCreationResult
    {
        internal ContactSlot Slot;
        internal Float4 Material;
        internal int ID, Padding1, Padding2, Padding3;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ContactPoolChange
    {
        internal ContactSlot Slot;
        internal int ID, Allocated, Padding1, Padding2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ContactPoolState
    {
        internal int Next, FreeCount, Error, Created;
        internal int BaseNext, BaseFree, Padding1, Padding2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ContactCreationStep
    {
        internal int Operation, Count, Capacity, LeafBase;
        internal int Offset, Width, Options, Padding;
    }

    private readonly Storage<ContactSlot> _contactSlotStorage;
    private readonly Storage<int> _contactFreeStorage, _contactScanStorage;
    private readonly Storage<ContactPoolState> _contactPoolStorage;
    private readonly Storage<ContactPoolChange> _contactChangeStorage;
    private readonly Storage<ContactRequest> _contactRequestStorage;
    private readonly Storage<ContactCreationResult> _contactCreationStorage;
    private readonly Action<int, bool> _contactIDChanged;
    private readonly Action<B2World> _createContacts;
    private readonly List<int> _contactIDChanges = [];
    private B2World? _contactTrackingWorld;
    private B2IdPool? _contactTrackingPool, _residentContactPool;
    private bool _resetContactPool, _onlyContactFrees = true;
    private int _claimedContactID = -1, _contactChangeCount, _contactLeafBase;
    internal long ContactPoolSnapshotCount { get; private set; }
    internal long ContactPoolUploadBytes { get; private set; }
    internal long CreatedContactCount { get; private set; }

    internal void EnableContactCreation(B2World world)
    {
        EnsureOwner();
        TrackContactPool(world);
        world.createBroadPhaseContacts = _createContacts;
    }

    internal void DisableContactCreation()
    {
        EnsureOwner();
        DetachContactPool();
    }

    private void MarkContactIDChanged(int id, bool allocated)
    {
        if (allocated && id == _claimedContactID) { _claimedContactID = -1; return; }
        _contactIDChanges.Add(allocated ? checked(id + 1) : checked(-id - 1));
        if (allocated) _onlyContactFrees = false;
    }

    private void ClearContactChanges()
    {
        _contactIDChanges.Clear();
        _onlyContactFrees = true;
    }

    private void DetachContactPool()
    {
        if (_contactTrackingPool is not null && _contactTrackingPool.changed == _contactIDChanged) _contactTrackingPool.changed = null!;
        if (_contactTrackingWorld is not null && _contactTrackingWorld.createBroadPhaseContacts == _createContacts) _contactTrackingWorld.createBroadPhaseContacts = null!;
        _contactTrackingWorld = null; _contactTrackingPool = _residentContactPool = null;
        ClearContactChanges();
    }

    private void TrackContactPool(B2World world)
    {
        if (ReferenceEquals(_contactTrackingWorld, world) && ReferenceEquals(_contactTrackingPool, world.contactIdPool) &&
            world.contactIdPool.changed == _contactIDChanged) return;
        var ownsCreator = world.createBroadPhaseContacts == _createContacts;
        DetachContactPool();
        if (ownsCreator) world.createBroadPhaseContacts = _createContacts;
        _contactTrackingWorld = world; _contactTrackingPool = world.contactIdPool;
        world.contactIdPool.changed = _contactIDChanged;
    }

    private static ContactSlot CaptureContactSlot(B2World world, int id)
    {
        var contact = world.contacts.data[id];
        if (contact.contactId != id) return new() { ShapeA = -1, ShapeB = -1, BodyA = -1, BodyB = -1, InitialSet = -1, Generation = contact.generation };
        return new()
        {
            ShapeA = contact.shapeIdA,
            ShapeB = contact.shapeIdB,
            BodyA = contact.edges[0].bodyId,
            BodyB = contact.edges[1].bodyId,
            Generation = contact.generation,
            Flags = contact.flags & (uint)B2ContactFlags.b2_contactEnableContactEvents,
            SimFlags = b2GetContactSim(world, contact).simFlags & (uint)B2ContactSimFlags.b2_simEnablePreSolveEvents,
            InitialSet = contact.setIndex
        };
    }

    private void PrepareContactPool(B2World world, int requests)
    {
        TrackContactPool(world);
        var pool = world.contactIdPool;
        var capacity = Math.Max(world.contacts.capacity, checked(pool.nextIndex + Math.Max(0, requests - pool.freeArray.count)));
        _resetContactPool = !ReferenceEquals(_residentContactPool, pool) || capacity > _contactSlotStorage.Data.Length;
        _residentContactPool = null;
        _contactSlotStorage.Reserve(Math.Max(1, capacity));
        _contactFreeStorage.Reserve(_contactSlotStorage.Data.Length);
        _contactPoolStorage.Reserve(1);
        _contactChangeStorage.Reserve(Math.Max(_contactSlotStorage.Data.Length, _contactIDChanges.Count));
        _contactIDChanges.EnsureCapacity(_contactChangeStorage.Data.Length);
        _contactLeafBase = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(1, requests)));
        _contactRequestStorage.Reserve(Math.Max(1, requests));
        _contactCreationStorage.Reserve(_contactRequestStorage.Data.Length);
        _contactScanStorage.Reserve(checked(2 * _contactLeafBase));
        _contactChangeCount = _resetContactPool ? 0 : _contactIDChanges.Count;
        if (_resetContactPool)
        {
            _contactSlotStorage.Data.AsSpan().Clear();
            for (var i = 0; i < world.contacts.count; i++) _contactSlotStorage.Data[i] = CaptureContactSlot(world, i);
            pool.freeArray.data.AsSpan(0, pool.freeArray.count).CopyTo(_contactFreeStorage.Data);
            _contactPoolStorage.Data[0] = new() { Next = pool.nextIndex, FreeCount = pool.freeArray.count };
        }
        else
        {
            for (var i = 0; i < _contactChangeCount; i++)
            {
                var change = _contactIDChanges[i]; var id = change > 0 ? change - 1 : -change - 1;
                _contactChangeStorage.Data[i] = new() { ID = id, Allocated = change > 0 ? 1 : 0, Slot = CaptureContactSlot(world, id) };
            }
        }
    }

    private void UploadContactPool(nint copy)
    {
        if (_resetContactPool)
        {
            _contactSlotStorage.Upload(copy, _contactSlotStorage.Data.Length);
            _contactFreeStorage.Upload(copy, _contactPoolStorage.Data[0].FreeCount);
            _contactPoolStorage.Upload(copy, 1);
            ContactPoolUploadBytes += (long)_contactSlotStorage.Data.Length * sizeof(ContactSlot) +
                (long)_contactPoolStorage.Data[0].FreeCount * sizeof(int) + sizeof(ContactPoolState);
        }
        else
        {
            _contactChangeStorage.Upload(copy, _contactChangeCount);
            ContactPoolUploadBytes += (long)_contactChangeCount * sizeof(ContactPoolChange);
        }
    }

    private void UpdateContactPool(nint command)
    {
        if (_contactChangeCount == 0) return;
        if (_onlyContactFrees)
        {
            ContactCreationPass(command, 0, _contactChangeCount);
            ContactCreationPass(command, 1, _contactChangeCount, dispatch: 1);
        }
        else ContactCreationPass(command, 2, _contactChangeCount, dispatch: 1);
    }

    private void ValidateContactPool(int next, int free)
    {
        ref readonly var state = ref _contactPoolStorage.Data[0];
        if (state.Error != 0 || state.Next != next || state.FreeCount != free)
            throw new InvalidOperationException($"GPU contact pool differs from its mirror: error {state.Error}, next {state.Next}/{next}, free {state.FreeCount}/{free}.");
    }

    private void CommitContactPool(B2World world)
    {
        if (_resetContactPool) ContactPoolSnapshotCount++;
        _residentContactPool = world.contactIdPool;
        ClearContactChanges();
    }

    internal void CreateContacts(B2World world)
    {
        EnsureOwner();
        var bp = world.broadPhase;
        var count = 0;
        for (var i = 0; i < bp.moveArray.count; i++)
            for (var pair = bp.moveResults[i].pairList; pair is not null; pair = pair.next) count++;
        if (count == 0) return;
        PrepareContactPool(world, count);
        var cursor = 0;
        for (var i = 0; i < bp.moveArray.count; i++)
            for (var pair = bp.moveResults[i].pairList; pair is not null; pair = pair.next)
            {
                var a = world.shapes.data[pair.shapeIndexA]; var b = world.shapes.data[pair.shapeIndexB];
                _contactRequestStorage.Data[cursor++] = new()
                {
                    ShapeA = a.id,
                    BodyA = a.bodyId,
                    TypeA = (int)a.type,
                    SetA = world.bodies.data[a.bodyId].setIndex,
                    ShapeB = b.id,
                    BodyB = b.bodyId,
                    TypeB = (int)b.type,
                    SetB = world.bodies.data[b.bodyId].setIndex,
                    Material = new(a.material.friction, a.material.restitution, b.material.friction, b.material.restitution),
                    FlagsA = (a.enableContactEvents ? 1u : 0u) | (a.enablePreSolveEvents ? 2u : 0u),
                    FlagsB = (b.enableContactEvents ? 1u : 0u) | (b.enablePreSolveEvents ? 2u : 0u),
                    MaterialA = (uint)(a.material.userMaterialId & 3),
                    MaterialB = (uint)(b.material.userMaterialId & 3)
                };
            }
        var (friction, bounce) = MaterialModes(world);
        DispatchContactCreation(count, (int)((friction << 3) | (bounce << 5)));

        // Validate the entire GPU allocation before callbacks or mirror mutation.
        var accepted = 0; var pool = world.contactIdPool;
        var next = pool.nextIndex; var free = pool.freeArray.count;
        for (var i = 0; i < count; i++)
        {
            var request = _contactRequestStorage.Data[i]; var result = _contactCreationStorage.Data[i];
            if (!b2GetContactOrder((B2ShapeType)request.TypeA, (B2ShapeType)request.TypeB, out var swap))
            {
                if (result.ID != -1) throw new InvalidOperationException("GPU created an unsupported contact pair.");
                continue;
            }
            var id = accepted < free ? pool.freeArray.data[free - accepted - 1] : next + accepted - free;
            var generation = id < world.contacts.count ? world.contacts.data[id].generation : 0;
            ref readonly var slot = ref result.Slot;
            if (result.ID != id || slot.Generation != unchecked(generation + 1) ||
                slot.ShapeA != (swap ? request.ShapeB : request.ShapeA) || slot.ShapeB != (swap ? request.ShapeA : request.ShapeB) ||
                slot.BodyA != (swap ? request.BodyB : request.BodyA) || slot.BodyB != (swap ? request.BodyA : request.BodyB) ||
                slot.InitialSet != (request.SetA == 2 || request.SetB == 2 ? 2 : 1) ||
                slot.Flags != (((request.FlagsA | request.FlagsB) & 1) != 0 ? 4u : 0u) ||
                slot.SimFlags != (((request.FlagsA | request.FlagsB) & 2) != 0 ? 0x200000u : 0u) || !Finite(result.Material))
                throw new InvalidOperationException("GPU contact creation returned an invalid identity or initialization record.");
            accepted++;
        }
        ValidateContactPool(checked(next + Math.Max(0, accepted - free)), Math.Max(0, free - accepted));
        if (_contactPoolStorage.Data[0].Created != accepted) throw new InvalidOperationException("GPU contact creation count differs.");
        try
        {
            for (var i = 0; i < count; i++)
            {
                var result = _contactCreationStorage.Data[i];
                if (result.ID < 0) continue;
                var slot = result.Slot;
                var creation = new B2ContactCreation
                {
                    ID = result.ID,
                    Generation = slot.Generation,
                    SetIndex = slot.InitialSet,
                    Flags = slot.Flags,
                    SimFlags = slot.SimFlags,
                    Friction = result.Material.X,
                    Restitution = result.Material.Y,
                    UseFriction = friction != 0,
                    UseRestitution = bounce != 0
                };
                _claimedContactID = result.ID;
                b2CreateContactPrepared(world, world.shapes.data[slot.ShapeA], world.shapes.data[slot.ShapeB], in creation);
            }
            ValidateContactPool(pool.nextIndex, pool.freeArray.count);
            CommitContactPool(world);
            CreatedContactCount += accepted;
        }
        finally { _claimedContactID = -1; }
    }

    private void DispatchContactCreation(int count, int options)
    {
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire contact creation commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin contact creation upload");
            UploadContactPool(copy); _contactRequestStorage.Upload(copy, count);
            SDL.EndGPUCopyPass(copy);
            UpdateContactPool(command);
            ContactCreationPass(command, 3, count, options: options, dispatch: _contactLeafBase);
            for (var width = _contactLeafBase / 2; width > 0; width /= 2)
                ContactCreationPass(command, 4, count, offset: width, width: width, dispatch: width);
            ContactCreationPass(command, 5, count, dispatch: 1);
            ContactCreationPass(command, 6, count);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin contact creation readback");
            _contactCreationStorage.Download(copy, count); _contactPoolStorage.Download(copy, 1);
            SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0;
            fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit contact creation");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for contact creation");
            _contactCreationStorage.Read(count); _contactPoolStorage.Read(1);
        }
        finally
        {
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
            if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
        }
    }

    private void ContactCreationPass(nint command, int operation, int count, int offset = 0, int width = 0, int options = 0, int dispatch = 0)
    {
        Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[5];
        bindings[0] = new() { Buffer = _contactSlotStorage.Handle }; bindings[1] = new() { Buffer = _contactFreeStorage.Handle };
        bindings[2] = new() { Buffer = _contactPoolStorage.Handle }; bindings[3] = new() { Buffer = _contactCreationStorage.Handle };
        bindings[4] = new() { Buffer = _contactScanStorage.Handle };
        var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 5);
        if (compute == 0) throw Failure("begin contact identity maintenance");
        SDL.BindGPUComputePipeline(compute, _contactCreationPipeline.DangerousGetHandle());
        var inputs = stackalloc nint[2] { _contactRequestStorage.Handle, _contactChangeStorage.Handle };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 2);
        var step = new ContactCreationStep { Operation = operation, Count = count, Capacity = _contactSlotStorage.Data.Length, LeafBase = _contactLeafBase, Offset = offset, Width = width, Options = options };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&step), (uint)sizeof(ContactCreationStep));
        SDL.DispatchGPUCompute(compute, checked((uint)((dispatch == 0 ? count : dispatch) + 63) / 64), 1, 1);
        SDL.EndGPUComputePass(compute); DispatchCount++;
    }
}
