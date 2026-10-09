using System.Runtime.InteropServices;
using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private struct ContinuousBody
    {
        internal PhysicsColliderBackend Backend;
        internal B2Sweep Sweep;
        internal CCDMode Mode;
        internal bool Dynamic;
        internal float Speed;
    }
    private readonly record struct ContinuousShape(B2ShapeId ID, int Body, B2AABB Bounds);
    private readonly List<ContinuousBody> _continuousBodies = [];
    private readonly List<ContinuousShape> _continuousShapes = [];
    private readonly List<int> _continuousProxies = [], _continuousBoundaries = [];
    private B2DynamicTree? _continuousTree;
    private Exception? _continuousFailure;
    private float _continuousFraction, _continuousNearStep, _continuousDuration;
    internal long ContinuousQueryCount { get; private set; }

    private bool HasContinuousBodies()
    {
        foreach (var body in _bodies) if (body.Runtime.ContinuousMode != CCDMode.Disabled && body.Backend.IsDynamic) return true;
        foreach (var body in _serverColliders) if (!body.IsArea && body.Runtime.ContinuousMode != CCDMode.Disabled && body.Backend.IsDynamic) return true;
        return false;
    }

    private void AddContinuousBody(PhysicsColliderBackend backend, CCDMode mode, B2StepContext context)
    {
        var world = context.world; var sim = PhysicsColliderBackend.Simulation(backend.BodyID);
        var body = b2GetBodyFullId(world, backend.BodyID);
        var state = body.setIndex == (int)B2SolverSetType.b2_awakeSet ? context.states[body.localIndex] : null;
        var displacement = state?.deltaPosition ?? default;
        var rotation = state is null ? sim.transform.q : b2NormalizeRot(b2MulRot(state.deltaRotation, sim.transform.q));
        var sweep = new B2Sweep(sim.localCenter, sim.center, sim.center + displacement, sim.transform.q, rotation);
        var angle = MathF.Abs(b2RelativeAngle(rotation, sim.transform.q));
        _continuousBodies.Add(new()
        {
            Backend = backend,
            Mode = mode,
            Dynamic = backend.IsDynamic,
            Speed = (b2Length(displacement) + 2 * MathF.Tan(.5f * angle) * sim.maxExtent) / context.dt,
            Sweep = sweep
        });
    }

    private float FindContinuousInterval(B2StepContext context)
    {
        var delta = context.dt; _continuousDuration = delta;
        _continuousTree ??= b2DynamicTree_Create();
        _continuousBodies.Clear(); _continuousShapes.Clear(); _continuousBoundaries.Clear(); _continuousFraction = 1; _continuousNearStep = delta;
        var world = b2GetWorldFromId(_worldID);
        foreach (var body in _bodies) AddContinuousBody(body.Backend, body.Runtime.ContinuousMode, context);
        foreach (var body in _serverColliders) if (!body.IsArea) AddContinuousBody(body.Backend, body.Runtime.ContinuousMode, context);
        var bodies = CollectionsMarshal.AsSpan(_continuousBodies);
        for (var index = 0; index < bodies.Length; index++)
        {
            ref var body = ref bodies[index]; var shapes = body.Backend.Shapes;
            for (var shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
            {
                var id = shapes[shapeIndex]; var shape = b2GetShape(world, id); if (shape.sensorIndex != B2Constants.B2_NULL_INDEX) continue;
                var start = b2GetSweepTransform(body.Sweep, 0); var end = b2GetSweepTransform(body.Sweep, 1);
                var a = b2ComputeShapeAABB(shape, start); var b = b2ComputeShapeAABB(shape, end);
                var radius = b2ComputeShapeExtent(shape, body.Sweep.localCenter).maxExtent;
                var rotate = body.Sweep.q1.c != body.Sweep.q2.c || body.Sweep.q1.s != body.Sweep.q2.s;
                var bounds = rotate ? new B2AABB(b2Min(body.Sweep.c1, body.Sweep.c2) - new B2Vec2(radius, radius),
                    b2Max(body.Sweep.c1, body.Sweep.c2) + new B2Vec2(radius, radius)) : new B2AABB(b2Min(a.lowerBound, b.lowerBound), b2Max(a.upperBound, b.upperBound));
                var pad = new B2Vec2(B2Constants.B2_LINEAR_SLOP, B2Constants.B2_LINEAR_SLOP);
                bounds.lowerBound -= pad; bounds.upperBound += pad;
                if (!b2IsValidVec2(bounds.lowerBound) || !b2IsValidVec2(bounds.upperBound)) throw new InvalidOperationException("Continuous bounds exceed the finite range.");
                var slot = _continuousShapes.Count; _continuousShapes.Add(new(id, index, bounds));
                if (shape.type == B2ShapeType.b2_boundaryShape) _continuousBoundaries.Add(slot);
                if (slot == _continuousProxies.Count) _continuousProxies.Add(b2DynamicTree_CreateProxy(_continuousTree, bounds, ulong.MaxValue, (ulong)slot));
                else b2DynamicTree_MoveProxy(_continuousTree, _continuousProxies[slot], bounds);
            }
        }
        while (_continuousProxies.Count > _continuousShapes.Count)
        { b2DynamicTree_DestroyProxy(_continuousTree, _continuousProxies[^1]); _continuousProxies.RemoveAt(_continuousProxies.Count - 1); }
        var query = new ContinuousQuery { Space = this };
        for (var i = 0; i < _continuousShapes.Count; i++)
        {
            var shape = _continuousShapes[i]; var body = bodies[shape.Body];
            if (!body.Dynamic || body.Mode == CCDMode.Disabled) continue;
            query.Shape = i;
            if (b2GetShape(world, shape.ID).type == B2ShapeType.b2_boundaryShape)
            {
                for (var j = 0; j < _continuousShapes.Count; j++) if (j != i) TestContinuousPair(i, j);
            }
            else
            {
                b2DynamicTree_Query(_continuousTree, shape.Bounds, ulong.MaxValue, QueryContinuousPair, ref query);
                foreach (var boundary in _continuousBoundaries) TestContinuousPair(i, boundary);
            }
        }
        return MathF.Min(delta * _continuousFraction, _continuousNearStep);
    }

    private struct ContinuousQuery { internal PhysicsSpace Space; internal int Shape; }
    private static bool QueryContinuousPair(int proxy, ulong data, ref ContinuousQuery query)
    {
        if ((int)data == query.Shape) return true;
        var target = query.Space._continuousShapes[(int)data];
        if (b2GetShape(b2GetWorldFromId(query.Space._worldID), target.ID).type != B2ShapeType.b2_boundaryShape)
            query.Space.TestContinuousPair(query.Shape, (int)data);
        return true;
    }

    private void TestContinuousPair(int first, int second)
    {
        var a = _continuousShapes[first]; var b = _continuousShapes[second]; if (a.Body == b.Body) return;
        var bodies = CollectionsMarshal.AsSpan(_continuousBodies);
        ref var ba = ref bodies[a.Body]; ref var bb = ref bodies[b.Body];
        if (bb.Dynamic && bb.Mode != CCDMode.Disabled && first > second) return;
        var world = b2GetWorldFromId(_worldID); var sa = b2GetShape(world, a.ID); var sb = b2GetShape(world, b.ID);
        if (!b2ShouldShapesCollide(sa.filter, sb.filter) || !b2ShouldBodiesCollide(world, world.bodies.data[sa.bodyId], world.bodies.data[sb.bodyId])) return;
        var ta = sa.userData.GetRef<PhysicsFixtureTag>(); var tb = sb.userData.GetRef<PhysicsFixtureTag>();
        if (ta is null || tb is null || PhysicsServer.Service.BodiesExcepted(ta.ColliderRID, tb.ColliderRID)) return;
        if (ta.SeparationRay is not null && tb.SeparationRay is not null ||
            ta.SeparationRay is { } rayA && b2LengthSquared(rayA.To - rayA.From) == 0 ||
            tb.SeparationRay is { } rayB && b2LengthSquared(rayB.To - rayB.From) == 0) return;
        if ((ta.OneWay is not null || tb.OneWay is not null) && CurrentContinuousSide(a.ID, b.ID) == false) return;
        var input = new B2TOIInput
        {
            proxyA = b2MakeShapeDistanceProxy(sa),
            proxyB = b2MakeShapeDistanceProxy(sb),
            sweepA = ba.Sweep,
            sweepB = bb.Sweep,
            maxFraction = 1
        };
        var full = ba.Mode == CCDMode.CastShape || bb.Dynamic && bb.Mode == CCDMode.CastShape;
        if (full) TestContinuousCast(input, a, b, ta, tb);
        else
        {
            if (ba.Mode == CCDMode.CastRay && MakeLeadingRay(ref input.proxyA, ref input.sweepA, input.sweepB, ta.Compound)) TestContinuousCast(input, a, b, ta, tb);
            if (bb.Dynamic && bb.Mode == CCDMode.CastRay)
            {
                input.proxyA = b2MakeShapeDistanceProxy(sa); input.sweepA = ba.Sweep;
                if (MakeLeadingRay(ref input.proxyB, ref input.sweepB, input.sweepA, tb.Compound)) TestContinuousCast(input, a, b, ta, tb);
            }
        }
    }

    private void TestContinuousCast(B2TOIInput input, ContinuousShape a, ContinuousShape b, PhysicsFixtureTag ta, PhysicsFixtureTag tb)
    {
        ContinuousQueryCount++;
        var hit = b2TimeOfImpact(input);
        if (hit.state == B2TOIState.b2_toiStateFailed) throw new InvalidOperationException("Continuous collision did not converge.");
        if (hit.state is B2TOIState.b2_toiStateSeparated or B2TOIState.b2_toiStateUnknown) return;
        var bodies = CollectionsMarshal.AsSpan(_continuousBodies);
        var xa = b2GetSweepTransform(input.sweepA, hit.fraction); var xb = b2GetSweepTransform(input.sweepB, hit.fraction);
        if (ta.SeparationRay is not null || tb.SeparationRay is not null)
        {
            // CastRay may have reduced either proxy to a support point. Directed validation needs the complete authored ray.
            var world = b2GetWorldFromId(_worldID);
            var directed = PhysicsSeparationRay.SolverContact(b2GetShape(world, a.ID), xa, b2GetShape(world, b.ID), xb, B2Constants.B2_LINEAR_SLOP);
            if (directed.pointCount == 0) return;
            hit.normal = directed.normal;
        }
        if (hit.state == B2TOIState.b2_toiStateOverlapped || hit.fraction <= 0)
        {
            var va = input.sweepA.c2 - input.sweepA.c1; var vb = input.sweepB.c2 - input.sweepB.c1;
            var cache = default(B2SimplexCache);
            var distanceInput = new B2DistanceInput
            {
                proxyA = input.proxyA,
                proxyB = input.proxyB,
                transformA = b2GetSweepTransform(input.sweepA, 0),
                transformB = b2GetSweepTransform(input.sweepB, 0),
                useRadii = false
            };
            var distance = b2ShapeDistance(ref distanceInput, ref cache, [], 0);
            var rotating = input.sweepA.q1.s != input.sweepA.q2.s || input.sweepA.q1.c != input.sweepA.q2.c ||
                input.sweepB.q1.s != input.sweepB.q2.s || input.sweepB.q1.c != input.sweepB.q2.c;
            if (!rotating && b2Dot(vb - va, distance.normal) >= 0) return;
            var rate = bodies[a.Body].Speed + bodies[b.Body].Speed;
            if (input.proxyA.isBoundary || input.proxyB.isBoundary) rate = MathF.Max(rate, B2Boundaries.SweepRate(input) / _continuousDuration);
            if (rate > 0)
            {
                var extent = MathF.Min(PhysicsColliderBackend.Simulation(bodies[a.Body].Backend.BodyID).minExtent,
                    PhysicsColliderBackend.Simulation(bodies[b.Body].Backend.BodyID).minExtent);
                _continuousNearStep = MathF.Min(_continuousNearStep, .25f * MathF.Max(extent, B2Constants.B2_LINEAR_SLOP) / rate);
            }
            return;
        }
        if (hit.fraction >= 1) return;
        if (CurrentContinuousSide(a.ID, b.ID) != true &&
            (!ContinuousSide(ta.OneWay, xa.q, hit.normal, true) || !ContinuousSide(tb.OneWay, xb.q, hit.normal, false))) return;
        _continuousFraction = MathF.Min(_continuousFraction, hit.fraction);
    }

    private bool? CurrentContinuousSide(B2ShapeId a, B2ShapeId b)
    {
        if (_oneWayPairs.Count == 0) return null;
        var first = PackShapeID(a); var second = PackShapeID(b); var key = first < second ? (first, second) : (second, first);
        return _oneWayPairs.TryGetValue(key, out var pair) && pair.SeenStep == _contactStep ? pair.Allowed : null;
    }

    private static bool ContinuousSide(OneWayContactData? data, B2Rot rotation, B2Vec2 normal, bool first)
    {
        if (data is null) return true;
        var direction = b2RotateVector(rotation, new(data.LocalDirection.X, data.LocalDirection.Y));
        var dot = b2Dot(direction, normal); return first ? dot < -1e-6f : dot > 1e-6f;
    }

    private static bool MakeLeadingRay(ref B2ShapeProxy shape, ref B2Sweep sweep, in B2Sweep other, PhysicsFixtureTag.CompoundContour? contour)
    {
        if (shape.isBoundary) { sweep.q2 = sweep.q1; return true; }

        var delta = sweep.c2 - sweep.c1 - (other.c2 - other.c1); var length = b2Length(delta);
        if (length == 0) return false;
        var direction = b2InvRotateVector(sweep.q1, delta * (1 / length));
        ReadOnlySpan<Vector2> full = contour is null ? [] : contour.Points;
        var count = full.IsEmpty ? shape.count : full.Length;
        var best = float.NegativeInfinity; var low = 0f; var high = 0f;
        var tangent = new B2Vec2(-direction.Y, direction.X);
        for (var i = 0; i < count; i++)
        {
            var point = full.IsEmpty ? shape.points[i] : PhysicsShapeBackend.ToBackend(contour!.LocalPose * full[i]);
            var along = b2Dot(point, direction); var side = b2Dot(point, tangent);
            if (along > best + 1e-6f) { best = along; low = high = side; }
            else if (MathF.Abs(along - best) <= 1e-6f) { low = MathF.Min(low, side); high = MathF.Max(high, side); }
        }
        var support = (best + shape.radius) * direction + .5f * (low + high) * tangent;
        shape = b2MakeProxy(support, 1, 0); sweep.q2 = sweep.q1; return true;
    }
}
