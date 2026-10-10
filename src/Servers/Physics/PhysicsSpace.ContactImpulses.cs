using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private BodyMotion[] _bodyMotions = [];
    private readonly record struct BodyMotion(B2Vec2 Center, B2Vec2 Velocity, float Angular, bool Active);

    private void CaptureBodyMotions()
    {
        Array.Clear(_bodyMotions);
        var world = b2GetWorldFromId(_worldID);
        var awake = world.solverSets.data[(int)B2SolverSetType.b2_awakeSet];
        for (var i = 0; i < awake.bodySims.count; i++)
        {
            var sim = awake.bodySims.data[i];
            var state = awake.bodyStates.data[i];
            _bodyMotions[sim.bodyId] = new(sim.center, b2Add(state.linearVelocity, sim.surfaceLinearVelocity), state.angularVelocity + sim.surfaceAngularVelocity, true);
        }
        var stationary = world.solverSets.data[(int)B2SolverSetType.b2_staticSet];
        for (var i = 0; i < stationary.bodySims.count; i++)
        {
            var sim = stationary.bodySims.data[i];
            _bodyMotions[sim.bodyId] = new(sim.center, sim.surfaceLinearVelocity, sim.surfaceAngularVelocity, true);
        }
    }

    internal B2Vec2 SolvedPointVelocity(B2BodyId id, B2Vec2 point)
    {
        ref readonly var motion = ref _bodyMotions[id.index1 - 1];
        if (motion.Active) return b2Add(motion.Velocity, b2CrossSV(motion.Angular, b2Sub(point, motion.Center)));
        var world = b2GetWorldFromId(_worldID); var sim = b2GetBodySim(world, b2GetBodyFullId(world, id));
        return b2Add(sim.surfaceLinearVelocity, b2CrossSV(sim.surfaceAngularVelocity, b2Sub(point, sim.center)));
    }

    private bool _aggregateContactImpulses;
    private readonly Dictionary<(int A, int B, ushort Feature), int> _frameContactIndices = [];
    private readonly List<FrameContact> _frameContacts = [];
    private int[] _frameContactHeads = [], _frameContactTails = [];
    private struct FrameContact
    {
        internal int ShapeA, ShapeB, BodyA, BodyB, NextA, NextB;
        internal bool LinkedA, LinkedB;
        internal B2Vec2 Point, Normal, Impulse;
        internal float Separation, Depth;
        internal ulong Seen;
    }

    // Fixture identity cannot change inside the outer solver interval. Canonical ordering also handles reversed pairs.
    private static (int A, int B, ushort Feature) ImpulseKey(B2ContactSim contact, ushort feature) =>
        contact.shapeIdA < contact.shapeIdB ? (contact.shapeIdA, contact.shapeIdB, feature) :
            (contact.shapeIdB, contact.shapeIdA, BinaryPrimitives.ReverseEndianness(feature));

    private void BeginFrameContacts()
    {
        foreach (var body in _contactBodies)
            if (body.MaxContactsReported > 0) { _aggregateContactImpulses = true; break; }
        if (!_aggregateContactImpulses)
            foreach (var body in _callbackBodies)
                if (body.Scene is not RigidBody && body.Runtime.ContactLimit > 0) { _aggregateContactImpulses = true; break; }
        if (!_aggregateContactImpulses) return;
        var count = Math.Max(_preparedBodyCapacity, b2GetWorldFromId(_worldID).bodies.count);
        if (_frameContactHeads.Length < count)
        {
            Array.Resize(ref _frameContactHeads, count); Array.Resize(ref _frameContactTails, count);
        }
        Array.Fill(_frameContactHeads, -1); Array.Fill(_frameContactTails, -1);
    }

    private void CaptureIntervalImpulses()
    {
        if (!_aggregateContactImpulses) return;
        var world = b2GetWorldFromId(_worldID);
        foreach (var body in _contactBodies)
            if (body.MaxContactsReported > 0) Capture(body.BackendID);
        foreach (var body in _callbackBodies)
            if (body.Scene is not RigidBody && body.Runtime.ContactLimit > 0) Capture(body.Backend.BodyID);

        void Capture(B2BodyId id)
        {
            for (var key = b2GetBodyFullId(world, id).headContactKey; key != B2_NULL_INDEX;)
            {
                var contact = world.contacts.data[key >> 1]; var edge = key & 1;
                key = contact.edges[edge].nextKey;
                if ((contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag) == 0) continue;
                var sim = b2GetContactSim(world, contact);
                for (var i = 0; i < sim.manifold.pointCount; i++)
                {
                    ref readonly var point = ref sim.manifold.points[i];
                    CaptureFramePoint(world, id, sim, point);
                }
            }
        }
    }

    private bool CaptureFramePoint(B2World world, B2BodyId id, B2ContactSim sim, in B2ManifoldPoint point)
    {
        var pair = ImpulseKey(sim, point.id);
        ref var index = ref CollectionsMarshal.GetValueRefOrAddDefault(_frameContactIndices, pair, out var found);
        if (!found)
        {
            index = _frameContacts.Count;
            _frameContacts.Add(new()
            {
                ShapeA = pair.A,
                ShapeB = pair.B,
                BodyA = world.shapes.data[pair.A].bodyId,
                BodyB = world.shapes.data[pair.B].bodyId,
                NextA = -1,
                NextB = -1,
                Depth = -point.separation
            });
        }
        ref var total = ref CollectionsMarshal.AsSpan(_frameContacts)[index];
        var first = total.BodyA == id.index1 - 1;
        if (first ? !total.LinkedA : !total.LinkedB)
        {
            var body = id.index1 - 1; var tail = _frameContactTails[body];
            if (tail < 0) _frameContactHeads[body] = index;
            else
            {
                ref var previous = ref CollectionsMarshal.AsSpan(_frameContacts)[tail];
                if (previous.BodyA == body) previous.NextA = index; else previous.NextB = index;
            }
            _frameContactTails[body] = index;
            if (first) total.LinkedA = true; else total.LinkedB = true;
        }
        if (total.Seen == world.stepIndex) return false;
        var normal = sim.shapeIdA < sim.shapeIdB ? sim.manifold.normal : -sim.manifold.normal;
        if (sim.solvedStep == world.stepIndex)
        {
            var impulse = normal * point.totalNormalImpulse + b2RightPerp(normal) * point.totalTangentImpulse;
            var sum = total.Impulse + impulse;
            if (!float.IsFinite(sum.X) || !float.IsFinite(sum.Y)) throw new InvalidOperationException("Contact impulse total exceeds the finite physics range.");
            total.Impulse = sum;
        }
        total.Point = point.point; total.Normal = normal; total.Separation = point.separation;
        total.Depth = MathF.Max(total.Depth, -point.separation); total.Seen = world.stepIndex;
        return true;
    }

    internal bool CaptureFrameContacts(PhysicsColliderBackend backend, PhysicsDirectBodyState state, B2BodyId id, int limit)
    {
        if (!_aggregateContactImpulses) return false;
        var world = b2GetWorldFromId(_worldID);
        var contacts = CollectionsMarshal.AsSpan(_frameContacts);
        for (var index = _frameContactHeads[id.index1 - 1]; index >= 0;)
        {
            ref readonly var contact = ref contacts[index]; var first = contact.BodyA == id.index1 - 1;
            index = first ? contact.NextA : contact.NextB;
            var own = world.shapes.data[first ? contact.ShapeA : contact.ShapeB].userData.GetRef<PhysicsFixtureTag>();
            var other = world.shapes.data[first ? contact.ShapeB : contact.ShapeA].userData.GetRef<PhysicsFixtureTag>();
            if (own is null || other is null) continue;
            backend.CaptureViewContact(state, contact.Normal, contact.Point, contact.Separation, contact.Depth, contact.Impulse,
                b2MakeBodyId(world, first ? contact.BodyB : contact.BodyA), first, own, other, limit);
        }
        return true;
    }

    internal B2Vec2 SolvedContactImpulse(B2ContactSim contact, in B2ManifoldPoint point)
    {
        if (contact.solvedStep == 0 || contact.solvedStep != b2GetWorldFromId(_worldID).stepIndex) return default;
        return contact.manifold.normal * point.totalNormalImpulse + b2RightPerp(contact.manifold.normal) * point.totalTangentImpulse;
    }
}
