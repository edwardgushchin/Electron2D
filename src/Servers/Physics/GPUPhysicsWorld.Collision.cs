using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using Float4 = System.Numerics.Vector4;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [InlineArray(8)]
    private struct Vertices { private Float4 _first; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Geometry { internal int TypeFlags; internal float Radius; internal int Count, ID; internal Vertices Vertices; internal Float4 Material; }
    [StructLayout(LayoutKind.Sequential)]
    private struct CollisionPair { internal int A, B, Flags, History; internal Float4 PoseA, PoseB, Offset; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ManifoldResult { internal Float4 Normal, Anchor1, Point1, Anchor2, Point2; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ContactHistory { internal Float4 Impulses, Features; }
    [StructLayout(LayoutKind.Sequential)]
    private struct CollisionStep { internal uint Count, Operation, Options, ContactLimit; }

    private readonly Storage<Geometry> _geometryStorage, _geometryUpdateStorage;
    private readonly Action<int> _geometryChanged;
    private B2World? _geometryTrackingWorld;
    private bool[] _geometryResident = [];
    internal int ResidentGeometryCount { get; private set; }
    internal int UploadedGeometryCount { get; private set; }
    internal long GeometryUploadBytes => (long)UploadedGeometryCount * sizeof(Geometry);
    internal long GeometryCacheResetCount { get; private set; }
    private readonly Storage<CollisionPair> _pairStorage;
    private readonly Storage<ManifoldResult> _manifoldStorage;
    private readonly Storage<ContactHistory> _historyStorage, _matchedStorage;
    internal int ResidentHistoryCount { get; private set; }
    internal int UploadedHistoryCount { get; private set; }
    internal long HistoryUploadBytes => (long)UploadedHistoryCount * sizeof(ContactHistory);
    private B2Manifold[] _manifolds = [];
    private Float4[] _centerOffsets = [];
    private B2StepContext? _manifoldContext;
    private int[] _geometryStamps = [];
    private int _geometryStamp;
    internal long CollisionSubmissionCount { get; private set; }

    internal void GenerateManifolds(B2StepContext context, int count) => GenerateManifolds(context, count, 0);

    private void GenerateManifolds(B2StepContext context, int count, uint options)
    {
        EnsureOwner();
        var world = context.world;
        if ((uint)count > (uint)context.contacts.Count) throw new ArgumentOutOfRangeException(nameof(count));
        context.generatedManifolds = null!;
        context.generatedContactsUpdated = false;
        context.generatedManifoldOwner = null!;
        _manifoldContext = null;
        ResidentHistoryCount = UploadedHistoryCount = 0;
        ResidentGeometryCount = UploadedGeometryCount = 0;
        _preSolveContactCount = 0;
        if (count == 0) return;
        var useContactSlots = ReferenceEquals(_contactTrackingWorld, world);
        if (useContactSlots) { PrepareContactPool(world, 0); options |= 128; }
        else _contactSlotStorage.Reserve(1);
        PrepareGeometryCache(world);
        _pairStorage.Reserve(world.contacts.capacity);
        _manifoldStorage.Reserve(world.contacts.capacity);
        _contactMaterialStorage.Reserve(world.contacts.capacity);
        _historyStorage.Reserve(world.contacts.capacity); _matchedStorage.Reserve(world.contacts.capacity);
        _contactStorage.Reserve(1);
        if (_manifolds.Length < _pairStorage.Data.Length) _manifolds = new B2Manifold[_pairStorage.Data.Length];
        if (_centerOffsets.Length < _pairStorage.Data.Length) _centerOffsets = new Float4[_pairStorage.Data.Length];
        if (_geometryStamps.Length < _geometryStorage.Data.Length) _geometryStamps = new int[_geometryStorage.Data.Length];
        if (_geometryStamp == int.MaxValue) { Array.Clear(_geometryStamps); _geometryStamp = 0; }
        _geometryStamp++;
        for (var i = 0; i < count; i++)
        {
            var contact = context.contacts[i];
            if ((options & 4) != 0 && (contact.simFlags & (uint)B2ContactSimFlags.b2_simEnablePreSolveEvents) != 0) _preSolveContactCount++;
            var a = world.shapes.data[contact.shapeIdA]; var b = world.shapes.data[contact.shapeIdB];
            var pair = new CollisionPair
            {
                A = useContactSlots ? contact.contactId : contact.shapeIdA,
                B = useContactSlots ? unchecked((int)contact.generation) : contact.shapeIdB,
                Flags = (int)(contact.simFlags >> 16) << 1,
                History = -1
            };
            if (b2AABB_Overlaps(a.fatAABB, b.fatAABB))
            {
                PackGeometry(a, contact.shapeIdA); PackGeometry(b, contact.shapeIdB);
                var simA = b2GetBodySim(world, world.bodies.data[a.bodyId]);
                var simB = b2GetBodySim(world, world.bodies.data[b.bodyId]);
                var poseA = simA.transform; var poseB = simB.transform;
                var offsetA = b2RotateVector(poseA.q, simA.localCenter);
                var offsetB = b2RotateVector(poseB.q, simB.localCenter);
                _centerOffsets[i] = new(offsetA.X, offsetA.Y, offsetB.X, offsetB.Y);
                pair.PoseA = new(poseA.p.X, poseA.p.Y, poseA.q.c, poseA.q.s);
                pair.PoseB = new(poseB.p.X, poseB.p.Y, poseB.q.c, poseB.q.s);
                pair.Flags |= 1;
                if ((options & 1) != 0)
                {
                    pair.Offset = _centerOffsets[i];
                    _centerOffsets[i] = default;
                }
                if (ReferenceEquals(_solvedWorld, world) && SolverSubmissionCount > 0 && contact.generatedManifoldVersion == -SolverSubmissionCount)
                {
                    pair.History = contact.generatedManifoldIndex; ResidentHistoryCount++;
                }
                else if (contact.manifold.pointCount > 0 || contact.manifold.rollingImpulse != 0)
                {
                    ref readonly var old = ref contact.manifold;
                    _historyStorage.Data[UploadedHistoryCount] = new()
                    {
                        Impulses = new(old.points[0].normalImpulse, old.points[0].tangentImpulse, old.points[1].normalImpulse, old.points[1].tangentImpulse),
                        Features = new(old.points[0].id, old.points[1].id, old.pointCount, old.rollingImpulse)
                    };
                    pair.History = -UploadedHistoryCount++ - 2;
                }
            }
            _pairStorage.Data[i] = pair;
        }
        DispatchCollision(world, count, options);
        // Reject invalid results before any manifold reaches the live world.
        for (var i = 0; i < count; i++)
        {
            var result = _manifoldStorage.Data[i];
            if (result.Normal.Z is not (0 or 1 or 2) || (result.Normal.W < 0 || result.Normal.W > ((options & 1) != 0 ? 255 : 3) || result.Normal.W != (int)result.Normal.W) ||
                ((options & 1) != 0 && !Finite(_contactMaterialStorage.Data[i])) ||
                _matchedStorage.Data[i].Features.Z != result.Normal.Z || !Finite(result.Normal) ||
                !Finite(_matchedStorage.Data[i].Impulses) || !Finite(_matchedStorage.Data[i].Features) ||
                !Finite(result.Anchor1) || !Finite(result.Point1) || !Finite(result.Anchor2) || !Finite(result.Point2))
                throw new InvalidOperationException("GPU collision returned an invalid manifold.");
        }
        for (var i = 0; i < count; i++)
        {
            var result = _manifoldStorage.Data[i];
            var warm = _matchedStorage.Data[i];
            var manifold = new B2Manifold { normal = new(result.Normal.X, result.Normal.Y), pointCount = (int)result.Normal.Z, rollingImpulse = warm.Features.W };
            if (manifold.pointCount > 0) manifold.points[0] = UnpackPoint(result.Anchor1, result.Point1);
            if (manifold.pointCount > 1) manifold.points[1] = UnpackPoint(result.Anchor2, result.Point2);
            for (var point = 0; point < manifold.pointCount; point++)
            {
                manifold.points[point].normalImpulse = point == 0 ? warm.Impulses.X : warm.Impulses.Z;
                manifold.points[point].tangentImpulse = point == 0 ? warm.Impulses.Y : warm.Impulses.W;
                manifold.points[point].persisted = ((int)result.Normal.W & (1 << point)) != 0;
            }
            _manifolds[i] = manifold;
            context.contacts[i].generatedManifoldVersion = CollisionSubmissionCount;
            context.contacts[i].generatedManifoldIndex = i;
        }
        for (var i = 0; i < UploadedGeometryCount; i++)
            _geometryResident[_geometryUpdateStorage.Data[i].ID] = true;
        context.generatedManifolds = _manifolds;
        context.generatedManifoldOwner = this;
        _manifoldContext = context;
    }

    private static B2ManifoldPoint UnpackPoint(Float4 anchor, Float4 point) => new()
    {
        anchorA = new(anchor.X, anchor.Y),
        anchorB = new(point.X, point.Y),
        point = new(point.Z, point.W),
        separation = anchor.Z,
        id = checked((ushort)anchor.W)
    };

    private void MarkGeometryChanged(int index)
    {
        if ((uint)index < (uint)_geometryResident.Length) _geometryResident[index] = false;
    }

    private void DetachGeometryTracking()
    {
        if (_geometryTrackingWorld is not null && _geometryTrackingWorld.shapeGeometryChanged == _geometryChanged)
            _geometryTrackingWorld.shapeGeometryChanged = null!;
        _geometryTrackingWorld = null;
        Array.Clear(_geometryResident);
    }

    private void PrepareGeometryCache(B2World world)
    {
        if (!ReferenceEquals(_geometryTrackingWorld, world) || world.shapeGeometryChanged != _geometryChanged ||
            world.shapes.capacity > _geometryStorage.Data.Length)
        {
            DetachGeometryTracking();
            _geometryTrackingWorld = world;
            world.shapeGeometryChanged = _geometryChanged;
            GeometryCacheResetCount++;
        }
        _geometryStorage.Reserve(Math.Max(1, world.shapes.capacity));
        _geometryUpdateStorage.Reserve(_geometryStorage.Data.Length);
        if (_geometryResident.Length < _geometryStorage.Data.Length) _geometryResident = new bool[_geometryStorage.Data.Length];
    }

    private void PackGeometry(B2Shape shape, int index)
    {
        if (shape.type is < B2ShapeType.b2_circleShape or > B2ShapeType.b2_polygonShape)
            throw new NotSupportedException("GPU manifolds do not yet support chain segments.");
        if (_geometryStamps[index] == _geometryStamp) return;
        _geometryStamps[index] = _geometryStamp;
        if (_geometryResident[index]) { ResidentGeometryCount++; return; }
        var g = new Geometry
        {
            TypeFlags = (int)shape.type | ((int)(shape.material.userMaterialId & 3) << 8) | (shape.enableHitEvents ? 1024 : 0),
            Count = 2,
            ID = index,
            Material = new(shape.material.friction, shape.material.restitution, shape.material.rollingResistance, shape.material.tangentSpeed)
        };
        switch (shape.type)
        {
            case B2ShapeType.b2_circleShape:
                g.Radius = shape.us.circle.radius; g.Count = 1;
                g.Vertices[0] = new(shape.us.circle.center.X, shape.us.circle.center.Y, 0, 0); break;
            case B2ShapeType.b2_capsuleShape:
                g.Radius = shape.us.capsule.radius;
                g.Vertices[0] = new(shape.us.capsule.center1.X, shape.us.capsule.center1.Y, 0, 0);
                g.Vertices[1] = new(shape.us.capsule.center2.X, shape.us.capsule.center2.Y, 0, 0); break;
            case B2ShapeType.b2_segmentShape:
                g.Vertices[0] = new(shape.us.segment.point1.X, shape.us.segment.point1.Y, 0, 0);
                g.Vertices[1] = new(shape.us.segment.point2.X, shape.us.segment.point2.Y, 0, 0); break;
            case B2ShapeType.b2_polygonShape:
                g.Radius = shape.us.polygon.radius; g.Count = shape.us.polygon.count;
                for (var i = 0; i < shape.us.polygon.count; i++)
                {
                    var p = shape.us.polygon.vertices[i]; var n = shape.us.polygon.normals[i];
                    g.Vertices[i] = new(p.X, p.Y, n.X, n.Y);
                }
                break;
        }
        _geometryUpdateStorage.Data[UploadedGeometryCount++] = g;
    }

    private void DispatchCollision(B2World world, int pairCount, uint options)
    {
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire collision commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin the collision upload");
            if ((options & 128) != 0) UploadContactPool(copy);
            _geometryUpdateStorage.Upload(copy, UploadedGeometryCount); _pairStorage.Upload(copy, pairCount);
            _historyStorage.Upload(copy, UploadedHistoryCount);
            SDL.EndGPUCopyPass(copy);
            if ((options & 128) != 0) UpdateContactPool(command);
            var contactLimit = (options & 128) != 0 ? (uint)world.contactIdPool.nextIndex : 0;
            if (UploadedGeometryCount != 0) CollisionPass(command, UploadedGeometryCount, 1, options, contactLimit);
            CollisionPass(command, pairCount, 0, options, contactLimit);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin the manifold readback");
            _manifoldStorage.Download(copy, pairCount); _matchedStorage.Download(copy, pairCount);
            if ((options & 1) != 0) _contactMaterialStorage.Download(copy, pairCount);
            if ((options & 128) != 0) _contactPoolStorage.Download(copy, 1);
            SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0;
            fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit collision generation");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for collision generation");
            _manifoldStorage.Read(pairCount); _matchedStorage.Read(pairCount);
            if ((options & 1) != 0) _contactMaterialStorage.Read(pairCount);
            if ((options & 128) != 0)
            {
                _contactPoolStorage.Read(1);
                ValidateContactPool(world.contactIdPool.nextIndex, world.contactIdPool.freeArray.count);
                CommitContactPool(world);
            }
            CollisionSubmissionCount++;
        }
        finally
        {
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
            if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
        }
    }

    private void CollisionPass(nint command, int count, uint operation, uint options, uint contactLimit)
    {
        Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[5];
        bindings[0] = new() { Buffer = _geometryStorage.Handle }; bindings[1] = new() { Buffer = _pairStorage.Handle };
        bindings[2] = new() { Buffer = _manifoldStorage.Handle }; bindings[3] = new() { Buffer = _matchedStorage.Handle };
        bindings[4] = new() { Buffer = _contactMaterialStorage.Handle };
        var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 5);
        if (compute == 0) throw Failure("begin collision generation");
        SDL.BindGPUComputePipeline(compute, _collide.DangerousGetHandle());
        var inputs = stackalloc nint[4] { _contactStorage.Handle, _historyStorage.Handle, _geometryUpdateStorage.Handle, _contactSlotStorage.Handle };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 4);
        var uniform = new CollisionStep { Count = (uint)count, Operation = operation, Options = options, ContactLimit = contactLimit };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&uniform), (uint)sizeof(CollisionStep));
        SDL.DispatchGPUCompute(compute, checked((uint)(count + 63) / 64), 1, 1);
        SDL.EndGPUComputePass(compute);
    }

}
