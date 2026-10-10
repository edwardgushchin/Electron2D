using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly B2ContactSim _reportOnlyContact = new();
    private readonly HashSet<(int A, int B)> _reportOnlyPairs = [];
    private int _reportOnlyActive;
    private struct ReportOnlyQuery { internal PhysicsSpace Space; internal B2BodyId Receiver; internal B2Shape Shape; }

    private void CaptureReportOnlyContacts()
    {
        _reportOnlyPairs.Clear(); _reportOnlyActive = 0;
        foreach (var body in _bodies) Capture(body.Backend, body.Runtime.ContactLimit);
        foreach (var body in _serverColliders) if (!body.IsArea) Capture(body.Backend, body.Runtime.ContactLimit);
        void Capture(PhysicsColliderBackend backend, int limit)
        {
            if (limit == 0 || backend.IsDynamic) return;
            if (backend.HasMotionMode(PhysicsServer.BodyMode.Kinematic) && !b2Body_IsAwake(backend.BodyID)) _reportOnlyActive++;
            if (!_aggregateContactImpulses) { BeginFrameContacts(); CaptureIntervalImpulses(); }
            var world = b2GetWorldFromId(WorldID);
            var shapes = backend.Shapes;
            for (var i = 0; i < shapes.Count; i++)
            {
                var id = shapes[i];
                var shape = b2GetShape(world, id); var context = new ReportOnlyQuery { Space = this, Receiver = backend.BodyID, Shape = shape };
                for (var type = 0; type < 2; type++)
                {
                    var tree = world.broadPhase.trees[type];
                    var bounds = shape.type == B2ShapeType.b2_boundaryShape && tree.root >= 0 ? tree.nodes[tree.root].aabb : shape.fatAABB;
                    b2DynamicTree_Query(tree, bounds, ulong.MaxValue, ReportOnlyCandidate, ref context);
                }
                foreach (var boundary in world.broadPhase.boundaries.data.AsSpan(0, world.broadPhase.boundaries.count))
                {
                    var type = boundary.Key & 3; if (type >= 2) continue;
                    var tree = world.broadPhase.trees[type]; var proxy = boundary.Key >> 2;
                    ReportOnlyCandidate(proxy, b2DynamicTree_GetUserData(tree, proxy), ref context);
                }
            }
        }
    }
    private static bool ReportOnlyCandidate(int proxy, ulong data, ref ReportOnlyQuery query)
    {
        var space = query.Space; var world = b2GetWorldFromId(space.WorldID);
        var a = query.Shape; var b = world.shapes.data[(int)data];
        if (a.bodyId == b.bodyId || b.sensorIndex >= 0 || !b2ShouldShapesCollide(a.filter, b.filter) ||
            !b2ShouldBodiesCollide(world, world.bodies.data[a.bodyId], world.bodies.data[b.bodyId], reportOnly: true)) return true;
        var xa = b2GetBodyTransformQuick(world, world.bodies.data[a.bodyId]); var xb = b2GetBodyTransformQuick(world, world.bodies.data[b.bodyId]);
        var manifold = b2ReportManifold(ref a, ref xa, ref b, ref xb);
        if (manifold.pointCount == 0 || !space.AllowBodyContact(new(a.id + 1, world.worldId, a.generation), new(b.id + 1, world.worldId, b.generation), manifold.normal)) return true;
        var sim = space._reportOnlyContact; sim.shapeIdA = a.id; sim.shapeIdB = b.id; sim.manifold = manifold; sim.solvedStep = 0;
        for (var i = 0; i < manifold.pointCount; i++)
        {
            ref readonly var point = ref manifold.points[i]; if (point.separation > 0) continue;
            if (space.CaptureFramePoint(world, query.Receiver, sim, point) && point.separation < 0 && space._debugContactLimit > 0)
            {
                var half = point.separation * .5f * manifold.normal;
                space.AddDebugContact(point.point - half); space.AddDebugContact(point.point + half);
            }
            space._reportOnlyPairs.Add(a.id < b.id ? (a.id, b.id) : (b.id, a.id));
        }
        return true;
    }
}
