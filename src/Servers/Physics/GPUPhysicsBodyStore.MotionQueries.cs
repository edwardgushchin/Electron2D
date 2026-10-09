using System.Runtime.InteropServices;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    internal readonly record struct MotionQuery(BodyHandle Body, Transform From, Vector2 Motion, float Margin = .08f,
        bool RecoveryAsCollision = false, bool CollideSeparationRay = false,
        int BodyExclusionStart = 0, int BodyExclusionCount = 0, int ObjectExclusionStart = 0, int ObjectExclusionCount = 0);
    [StructLayout(LayoutKind.Sequential)]
    internal struct MotionQueryResult
    {
        internal ulong Collider;
        internal int LocalShape, ColliderShape;
        internal uint LocalShapeIndex, LocalShapeGeneration, ColliderShapeIndex, ColliderShapeGeneration;
        internal uint ColliderBody, ColliderBodyGeneration, Flags, Padding;
        internal ulong Object, Padding2;
        internal Float4 PointNormal, VelocityDepth, TravelRemainder, Fractions;
        internal readonly bool Collided => (Flags & 1) != 0;
        internal readonly Vector2 Point => new(PointNormal.X, PointNormal.Y);
        internal readonly Vector2 Normal => new(PointNormal.Z, PointNormal.W);
        internal readonly Vector2 Velocity => new(VelocityDepth.X, VelocityDepth.Y);
        internal readonly float Depth => VelocityDepth.Z;
        internal readonly Vector2 Travel => new(TravelRemainder.X, TravelRemainder.Y);
        internal readonly Vector2 Remainder => new(TravelRemainder.Z, TravelRemainder.W);
        internal readonly float SafeFraction => Fractions.X;
        internal readonly float UnsafeFraction => Fractions.Y;
        internal readonly Vector2 Recovery => new(Fractions.Z, Fractions.W);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MotionQueryInput
    {
        internal Float4 Pose, MotionMargin;
        internal uint Body, Generation, ShapesStart, ShapesCount;
        internal uint BodiesStart, BodiesCount, ObjectsStart, ObjectsCount;
        internal uint ExceptionsStart, ExceptionsCount, Flags, Padding;
    }
    private RenderHandle? _motionQueryPipeline;
    private MotionQueryInput[] _motionQueryInputs = [];
    private ulong[] _motionPayload = [];
    private int[] _motionQueryCounts = [];

    /// <summary>Tests each body's shapes at a supplied pose, including recovery and first impact; the live bodies stay unchanged.</summary>
    /// <remarks>Exclusion keys and object IDs are authored query identities. Results include recovery travel even on a miss; collision fields then clear.</remarks>
    internal void TestMotion(ReadOnlySpan<MotionQuery> queries, ReadOnlySpan<ulong> excludedBodies, ReadOnlySpan<ulong> excludedObjects, Span<MotionQueryResult> results)
    {
        EnsureAccess();
        if (results.Length < queries.Length) throw new ArgumentException("The motion result destination is too small.", nameof(results));
        var payloadCount = checked(excludedBodies.Length + excludedObjects.Length);
        foreach (ref readonly var q in queries)
        {
            Validate(q.Body); ValidateShapePose(q.From);
            if (!q.Motion.IsFinite() || !float.IsFinite(q.Motion.Length()) || !(q.From.Origin + q.Motion).IsFinite() || !float.IsFinite(q.Margin) || q.Margin < 0 ||
                q.BodyExclusionStart < 0 || q.BodyExclusionStart > excludedBodies.Length || q.BodyExclusionCount < 0 || q.BodyExclusionCount > excludedBodies.Length - q.BodyExclusionStart ||
                q.ObjectExclusionStart < 0 || q.ObjectExclusionStart > excludedObjects.Length || q.ObjectExclusionCount < 0 || q.ObjectExclusionCount > excludedObjects.Length - q.ObjectExclusionStart)
                throw new ArgumentOutOfRangeException(nameof(queries));
            for (var shape = _slots[q.Body.Index].FirstShape; shape >= 0; shape = _shapeSlots[shape].NextOnBody) payloadCount = checked(payloadCount + 1);
            for (var edge = _slots[q.Body.Index].FirstException; edge >= 0; edge = ExceptionNext(edge, q.Body.Index)) payloadCount = checked(payloadCount + 1);
        }
        if (queries.IsEmpty) return;
        PrepareQueryRequests(queries.Length);
        if (_motionQueryInputs.Length < queries.Length) { var capacity = Capacity(queries.Length); Array.Resize(ref _motionQueryInputs, capacity); Array.Resize(ref _motionQueryCounts, capacity); }
        if (_motionPayload.Length < payloadCount) Array.Resize(ref _motionPayload, Capacity(payloadCount));
        excludedBodies.CopyTo(_motionPayload); excludedObjects.CopyTo(_motionPayload.AsSpan(excludedBodies.Length));
        var at = excludedBodies.Length + excludedObjects.Length;
        for (var i = 0; i < queries.Length; i++)
        {
            ref readonly var q = ref queries[i];
            var input = new MotionQueryInput
            {
                Pose = new(q.From.Origin.X, q.From.Origin.Y, MathF.Cos(q.From.Rotation), MathF.Sin(q.From.Rotation)),
                MotionMargin = new(q.Motion.X, q.Motion.Y, MathF.Max(q.Margin, .0001f), 0),
                Body = (uint)q.Body.Index,
                Generation = q.Body.Generation,
                BodiesStart = (uint)q.BodyExclusionStart,
                BodiesCount = (uint)q.BodyExclusionCount,
                ObjectsStart = (uint)(excludedBodies.Length + q.ObjectExclusionStart),
                ObjectsCount = (uint)q.ObjectExclusionCount,
                Flags = (q.RecoveryAsCollision ? 1u : 0) | (q.CollideSeparationRay ? 2u : 0),
                ShapesStart = (uint)at
            };
            for (var shape = _slots[q.Body.Index].FirstShape; shape >= 0; shape = _shapeSlots[shape].NextOnBody)
                _motionPayload[at++] = (ulong)_shapeSlots[shape].Generation << 32 | (uint)shape;
            input.ShapesCount = (uint)at - input.ShapesStart; input.ExceptionsStart = (uint)at;
            for (var edge = _slots[q.Body.Index].FirstException; edge >= 0; edge = ExceptionNext(edge, q.Body.Index))
            {
                var pair = _exceptionSlots[edge].Pair; var ownA = pair.A == q.Body.Index;
                _motionPayload[at++] = (ulong)(ownA ? pair.GenerationB : pair.GenerationA) << 32 | (ownA ? pair.B : pair.A);
            }
            input.ExceptionsCount = (uint)at - input.ExceptionsStart; _motionQueryInputs[i] = input; _queryLimits[i] = 1;
        }
        ExecuteQueries<MotionQueryInput, MotionQueryResult>(_motionQueryInputs.AsSpan(0, queries.Length), _motionPayload.AsSpan(0, at), _motionQueryCounts, results, queries.Length,
            ref _motionQueryPipeline, "PhysicsResidentMotionQueries.comp.spv", centers: true);
        for (var i = 0; i < queries.Length; i++)
            if (_motionQueryCounts[i] == 0) results[i] = new() { TravelRemainder = new(queries[i].Motion.X, queries[i].Motion.Y, 0, 0), Fractions = new(1, 1, 0, 0) };
    }
}
