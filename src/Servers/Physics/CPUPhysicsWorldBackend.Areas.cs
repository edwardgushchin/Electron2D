using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private readonly List<(PhysicsAreaFields Fields, uint Mask, IReadOnlyList<B2ShapeId> Shapes, Transform Transform)> _fieldAreas = [];

    internal override bool AreaContainsPoint(Area area, Vector2 position, uint mask)
    {
        var point = PhysicsShapeBackend.ToBackend(position);
        var shapes = area.BackendShapes;
        for (var index = 0; index < shapes.Count; index++)
            if (b2Shape_TestPoint(shapes[index], point)) return true;
        return false;
    }

    internal override void ScanAreas() { ScanSceneAreas(); ScanAreaMonitors(); }

    private void ScanSceneAreas()
    {
        // ponytail: Pairwise shape scans are quadratic; use a broad-phase candidate index if large worlds show a measured cost.
        foreach (var area in Space.Areas)
        {
            area.BeginOverlapScan();
            if (area.Monitoring)
            {
                foreach (var body in Space.Bodies)
                    if ((area.CollisionMask & body.CollisionLayer) != 0)
                        ScanOverlapPairs(area, body.GetRID(), body, false, body.BackendShapes);
                foreach (var other in Space.Areas)
                    if (!ReferenceEquals(area, other) && other.Monitorable &&
                        (area.CollisionMask & other.CollisionLayer) != 0)
                        ScanOverlapPairs(area, other.GetRID(), other, true, other.BackendShapes);
                foreach (var other in Space.ServerColliders)
                    if ((!other.IsArea || other.Monitorable) && (area.CollisionMask & other.CollisionLayer) != 0)
                        ScanOverlapPairs(area, other.RID, null, other.IsArea, other.BackendShapes);
            }
            Space.CommitAreaScan(area);
        }
    }

    private void ScanOverlapPairs(Area area, RID rid, CollisionObject? other, bool isArea, IReadOnlyList<B2ShapeId> otherShapes)
    {
        for (var localIndex = 0; localIndex < area.BackendShapes.Count; localIndex++)
        {
            var local = area.BackendShapes[localIndex];
            var localTag = b2Shape_GetUserData(local).GetRef<PhysicsFixtureTag>();
            if (localTag is null) continue;
            for (var remoteIndex = 0; remoteIndex < otherShapes.Count; remoteIndex++)
            {
                var remote = otherShapes[remoteIndex];
                var remoteTag = b2Shape_GetUserData(remote).GetRef<PhysicsFixtureTag>();
                if (remoteTag is not null && ShapePairOverlaps(local, remote))
                    area.Observe(new(rid, remoteTag.ObjectIdentity, isArea, remoteTag.ShapeIndex, localTag.ShapeIndex));
            }
        }
    }

    private void PrepareAreaFields()
    {
        var gravity = Space.DefaultAreaFields.GravityPoint ? Vector2.Zero : Space.DefaultAreaFields.GravityVector * Space.DefaultAreaFields.Gravity;
        if (!gravity.IsFinite()) throw new InvalidOperationException("Default physics gravity exceeds the finite simulation range.");
        if (gravity != Space.DefaultGravity)
        {
            b2World_SetGravity(WorldID, PhysicsShapeBackend.ToBackend(gravity));
            Space.SetDefaultGravity(gravity);
        }
        // ponytail: Stable insertion order is quadratic in field areas; use indexed sorting if large-world profiling needs it.
        _fieldAreas.Clear();
        foreach (var area in Space.Areas)
            AddFieldArea(area.Fields, area.CollisionMask, area.BackendShapes, area.GlobalTransform);
        foreach (var collider in Space.ServerColliders)
            if (collider.AreaFields is { } fields)
                AddFieldArea(fields, collider.CollisionMask, collider.BackendShapes, collider.GetTransform());

    }

    private void AddFieldArea(PhysicsAreaFields fields, uint mask, IReadOnlyList<B2ShapeId> shapes, Transform transform)
    {
        if (!fields.HasOverrides) return;
        var index = _fieldAreas.Count;
        var area = (fields, mask, shapes, transform);
        _fieldAreas.Add(area);
        while (index > 0 && _fieldAreas[index - 1].Fields.Priority < fields.Priority)
        {
            _fieldAreas[index] = _fieldAreas[index - 1];
            index--;
        }
        _fieldAreas[index] = area;
    }

    private void ResolveAreaFields(uint layer, IReadOnlyList<B2ShapeId> shapes, Vector2 position,
        out Vector2 gravity, out float linearDamp, out float angularDamp)
    {
        gravity = Vector2.Zero;
        linearDamp = 0f;
        angularDamp = 0f;
        var gravityDone = false;
        var linearDone = false;
        var angularDone = false;
        foreach (var area in _fieldAreas)
        {
            if ((area.Mask & layer) == 0 || !ShapesOverlap(area.Shapes, shapes)) continue;
            if (!gravityDone)
            {
                var mode = area.Fields.GravitySpaceOverride;
                if (mode is Area.SpaceOverride.Combine or Area.SpaceOverride.CombineReplace)
                    gravity += area.Fields.ComputeGravity(area.Transform, position);
                else if (mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine)
                    gravity = area.Fields.ComputeGravity(area.Transform, position);
                gravityDone = mode is Area.SpaceOverride.CombineReplace or Area.SpaceOverride.Replace;
            }
            if (!linearDone)
            {
                var mode = area.Fields.LinearDampSpaceOverride;
                if (mode is Area.SpaceOverride.Combine or Area.SpaceOverride.CombineReplace)
                    linearDamp += area.Fields.LinearDamp;
                else if (mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine)
                    linearDamp = area.Fields.LinearDamp;
                linearDone = mode is Area.SpaceOverride.CombineReplace or Area.SpaceOverride.Replace;
            }
            if (!angularDone)
            {
                var mode = area.Fields.AngularDampSpaceOverride;
                if (mode is Area.SpaceOverride.Combine or Area.SpaceOverride.CombineReplace)
                    angularDamp += area.Fields.AngularDamp;
                else if (mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine)
                    angularDamp = area.Fields.AngularDamp;
                angularDone = mode is Area.SpaceOverride.CombineReplace or Area.SpaceOverride.Replace;
            }
            if (gravityDone && linearDone && angularDone) break;
        }
        if (!gravityDone) gravity += Space.DefaultAreaFields.ComputeGravity(Transform.Identity, position);
        if (!linearDone) linearDamp += Space.DefaultAreaFields.LinearDamp;
        if (!angularDone) angularDamp += Space.DefaultAreaFields.AngularDamp;
    }

    private bool ShapesOverlap(IReadOnlyList<B2ShapeId> areaShapes, IReadOnlyList<B2ShapeId> otherShapes)
    {
        for (var first = 0; first < areaShapes.Count; first++)
            for (var second = 0; second < otherShapes.Count; second++)
                if (ShapePairOverlaps(areaShapes[first], otherShapes[second])) return true;
        return false;
    }

    private bool ShapePairOverlaps(B2ShapeId first, B2ShapeId second)
    {
        var world = b2GetWorldFromId(WorldID);
        var shapeA = b2GetShape(world, first); var shapeB = b2GetShape(world, second);
        if ((shapeA.type == B2ShapeType.b2_boundaryShape || shapeB.type == B2ShapeType.b2_boundaryShape) &&
            (shapeA.userData.GetRef<PhysicsFixtureTag>()?.SeparationRay is not null || shapeB.userData.GetRef<PhysicsFixtureTag>()?.SeparationRay is not null))
        {
            var directedCache = default(B2SimplexCache);
            return PhysicsSeparationRay.SolverContact(shapeA, b2Body_GetTransform(b2Shape_GetBody(first)), shapeB,
                b2Body_GetTransform(b2Shape_GetBody(second)), ref directedCache).pointCount != 0;
        }
        if (shapeA.type == B2ShapeType.b2_boundaryShape || shapeB.type == B2ShapeType.b2_boundaryShape)
            return B2Boundaries.Contact(b2MakeShapeDistanceProxy(shapeA), b2Body_GetTransform(b2Shape_GetBody(first)),
                b2MakeShapeDistanceProxy(shapeB), b2Body_GetTransform(b2Shape_GetBody(second)), 0).pointCount != 0;
        var aabbA = b2Shape_GetAABB(first);
        var rayA = b2Shape_GetUserData(first).GetRef<PhysicsFixtureTag>()?.SeparationRay;
        var rayB = b2Shape_GetUserData(second).GetRef<PhysicsFixtureTag>()?.SeparationRay;
        var aabbB = b2Shape_GetAABB(second);
        if (rayA is null && rayB is null &&
            (aabbA.upperBound.X < aabbB.lowerBound.X || aabbA.lowerBound.X > aabbB.upperBound.X ||
            aabbA.upperBound.Y < aabbB.lowerBound.Y || aabbA.lowerBound.Y > aabbB.upperBound.Y))
            return false;
        var input = new B2DistanceInput
        {
            proxyA = b2MakeShapeDistanceProxy(shapeA),
            proxyB = b2MakeShapeDistanceProxy(shapeB),
            transformA = b2Body_GetTransform(b2Shape_GetBody(first)),
            transformB = b2Body_GetTransform(b2Shape_GetBody(second)),
            useRadii = true
        };
        if (rayA is not null || rayB is not null)
        {
            var query = rayA is { } ray ? PhysicsSeparationRay.WorldProxy(ray, input.transformA, default) :
                CPUPhysicsWorldBackend.WorldProxy(input.proxyA, input.transformA, default);
            if (PhysicsSeparationRay.PairContact(query, rayA?.SlideOnSlope, input.proxyB,
                input.transformB, rayB, default, 0).pointCount != 0) return true;
            return false;
        }
        var cache = new B2SimplexCache();
        return b2ShapeDistance(ref input, ref cache, null, 0).distance <= 0.1f * B2_LINEAR_SLOP;
    }

    private void ScanAreaMonitors()
    {
        foreach (var area in Space.Areas)
        {
            var runtime = PhysicsServer.Service.FindAreaRuntime(area.PhysicsRID);
            if (runtime is { Monitoring: true }) ScanAreaMonitor(runtime, area.BackendShapes);
        }
        foreach (var area in Space.ServerColliders)
        {
            if (!area.IsArea) continue;
            var runtime = PhysicsServer.Service.FindAreaRuntime(area.RID);
            if (runtime is { Monitoring: true }) ScanAreaMonitor(runtime, area.BackendShapes);
        }
    }

    private void ScanAreaMonitor(PhysicsAreaRuntime receiver, IReadOnlyList<B2ShapeId> localShapes)
    {
        receiver.Pairs.Begin();
        if (receiver.BodyCallback is not null)
            foreach (var body in Space.Bodies)
                if ((receiver.Mask & body.CollisionLayer) != 0)
                    ScanMonitorPairs(receiver, localShapes, body.PhysicsRID, body, false, body.BackendShapes);
        if (receiver.AreaCallback is not null)
            foreach (var area in Space.Areas)
                if (area.PhysicsRID != receiver.RID && area.Monitorable && (receiver.Mask & area.CollisionLayer) != 0)
                    ScanMonitorPairs(receiver, localShapes, area.PhysicsRID, area, true, area.BackendShapes);
        foreach (var collider in Space.ServerColliders)
        {
            if (collider.RID == receiver.RID || (receiver.Mask & collider.CollisionLayer) == 0) continue;
            if (collider.IsArea ? receiver.AreaCallback is null || !collider.Monitorable : receiver.BodyCallback is null) continue;
            ScanMonitorPairs(receiver, localShapes, collider.RID, null, collider.IsArea, collider.BackendShapes);
        }
        receiver.Changes.Clear(); receiver.Pairs.Commit(receiver.Changes); Space.QueueMonitorChanges(receiver);
    }

    private void ScanMonitorPairs(PhysicsAreaRuntime receiver, IReadOnlyList<B2ShapeId> localShapes,
        RID otherRID, CollisionObject? other, bool isArea, IReadOnlyList<B2ShapeId> otherShapes)
    {
        for (var localIndex = 0; localIndex < localShapes.Count; localIndex++)
        {
            var local = localShapes[localIndex];
            var localTag = b2Shape_GetUserData(local).GetRef<PhysicsFixtureTag>();
            if (localTag is null) continue;
            for (var remoteIndex = 0; remoteIndex < otherShapes.Count; remoteIndex++)
            {
                var remote = otherShapes[remoteIndex];
                var tag = b2Shape_GetUserData(remote).GetRef<PhysicsFixtureTag>();
                if (tag is not null && ShapePairOverlaps(local, remote))
                    receiver.Pairs.Observe(new(otherRID, tag.ObjectIdentity, isArea, tag.ShapeIndex, localTag.ShapeIndex));
            }
        }
    }

}
