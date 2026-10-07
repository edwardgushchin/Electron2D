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
    private struct Geometry { internal Float4 Info; internal Vertices Vertices; }
    [StructLayout(LayoutKind.Sequential)]
    private struct CollisionPair { internal Float4 IDs, PoseA, PoseB; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ManifoldResult { internal Float4 Normal, Anchor1, Point1, Anchor2, Point2; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ContactHistory { internal Float4 Impulses, Features; }
    [StructLayout(LayoutKind.Sequential)]
    private struct CollisionStep { internal uint Count, A, B, C; }

    private readonly Storage<Geometry> _geometryStorage;
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

    internal void GenerateManifolds(B2StepContext context, int count)
    {
        EnsureOwner();
        var world = context.world;
        if ((uint)count > (uint)context.contacts.Count) throw new ArgumentOutOfRangeException(nameof(count));
        context.generatedManifolds = null!;
        context.generatedManifoldOwner = null!;
        _manifoldContext = null;
        ResidentHistoryCount = UploadedHistoryCount = 0;
        if (count == 0) return;
        _geometryStorage.Reserve(world.shapes.capacity);
        _pairStorage.Reserve(world.contacts.capacity);
        _manifoldStorage.Reserve(world.contacts.capacity);
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
            var a = world.shapes.data[contact.shapeIdA]; var b = world.shapes.data[contact.shapeIdB];
            var pair = new CollisionPair { IDs = new(contact.shapeIdA, contact.shapeIdB, 0, -1) };
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
                pair.IDs.Z = 1;
                if (ReferenceEquals(_solvedWorld, world) && SolverSubmissionCount > 0 && contact.generatedManifoldVersion == -SolverSubmissionCount)
                {
                    pair.IDs.W = contact.generatedManifoldIndex; ResidentHistoryCount++;
                }
                else if (contact.manifold.pointCount > 0 || contact.manifold.rollingImpulse != 0)
                {
                    ref readonly var old = ref contact.manifold;
                    _historyStorage.Data[UploadedHistoryCount] = new()
                    {
                        Impulses = new(old.points[0].normalImpulse, old.points[0].tangentImpulse, old.points[1].normalImpulse, old.points[1].tangentImpulse),
                        Features = new(old.points[0].id, old.points[1].id, old.pointCount, old.rollingImpulse)
                    };
                    pair.IDs.W = -UploadedHistoryCount++ - 2;
                }
            }
            _pairStorage.Data[i] = pair;
        }
        DispatchCollision(world.shapes.count, count);
        // Reject invalid results before any manifold reaches the live world.
        for (var i = 0; i < count; i++)
        {
            var result = _manifoldStorage.Data[i];
            if (result.Normal.Z is not (0 or 1 or 2) || result.Normal.W is not (0 or 1 or 2 or 3) ||
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

    private void PackGeometry(B2Shape shape, int index)
    {
        if (_geometryStamps[index] == _geometryStamp) return;
        var g = new Geometry { Info = new((int)shape.type, 0, 2, 0) };
        switch (shape.type)
        {
            case B2ShapeType.b2_circleShape:
                g.Info.Y = shape.us.circle.radius; g.Info.Z = 1;
                g.Vertices[0] = new(shape.us.circle.center.X, shape.us.circle.center.Y, 0, 0); break;
            case B2ShapeType.b2_capsuleShape:
                g.Info.Y = shape.us.capsule.radius;
                g.Vertices[0] = new(shape.us.capsule.center1.X, shape.us.capsule.center1.Y, 0, 0);
                g.Vertices[1] = new(shape.us.capsule.center2.X, shape.us.capsule.center2.Y, 0, 0); break;
            case B2ShapeType.b2_segmentShape:
                g.Vertices[0] = new(shape.us.segment.point1.X, shape.us.segment.point1.Y, 0, 0);
                g.Vertices[1] = new(shape.us.segment.point2.X, shape.us.segment.point2.Y, 0, 0); break;
            case B2ShapeType.b2_polygonShape:
                g.Info.Y = shape.us.polygon.radius; g.Info.Z = shape.us.polygon.count;
                for (var i = 0; i < shape.us.polygon.count; i++)
                {
                    var p = shape.us.polygon.vertices[i]; var n = shape.us.polygon.normals[i];
                    g.Vertices[i] = new(p.X, p.Y, n.X, n.Y);
                }
                break;
            default: throw new NotSupportedException("GPU manifolds do not yet support chain segments.");
        }
        _geometryStorage.Data[index] = g; _geometryStamps[index] = _geometryStamp;
    }

    private void DispatchCollision(int shapeCount, int pairCount)
    {
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire collision commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin the collision upload");
            _geometryStorage.Upload(copy, shapeCount); _pairStorage.Upload(copy, pairCount);
            _historyStorage.Upload(copy, UploadedHistoryCount);
            SDL.EndGPUCopyPass(copy);
            Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[4];
            bindings[0] = new() { Buffer = _geometryStorage.Handle }; bindings[1] = new() { Buffer = _pairStorage.Handle };
            bindings[2] = new() { Buffer = _manifoldStorage.Handle }; bindings[3] = new() { Buffer = _matchedStorage.Handle };
            var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 4);
            if (compute == 0) throw Failure("begin collision generation");
            SDL.BindGPUComputePipeline(compute, _collide.DangerousGetHandle());
            var history = stackalloc nint[2] { _contactStorage.Handle, _historyStorage.Handle };
            SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)history, 2);
            var uniform = new CollisionStep { Count = (uint)pairCount };
            SDL.PushGPUComputeUniformData(command, 0, (nint)(&uniform), (uint)sizeof(CollisionStep));
            SDL.DispatchGPUCompute(compute, checked((uint)(pairCount + 63) / 64), 1, 1);
            SDL.EndGPUComputePass(compute);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin the manifold readback");
            _manifoldStorage.Download(copy, pairCount); _matchedStorage.Download(copy, pairCount); SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0;
            fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit collision generation");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for collision generation");
            _manifoldStorage.Read(pairCount); _matchedStorage.Read(pairCount); CollisionSubmissionCount++;
        }
        finally
        {
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
            if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
        }
    }
}
