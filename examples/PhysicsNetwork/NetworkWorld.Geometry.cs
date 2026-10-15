using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkWorld
{
    private static readonly Vector2[] PolygonPoints = [new(-8, -4), new(6, -6), new(9, 4), new(-6, 7)];
    private readonly CapsuleShape _geometryCapsule = new() { Radius = 4, Height = 18 };
    private readonly ConvexPolygonShape _geometryPolygon = new() { Points = PolygonPoints };
    private readonly ConcavePolygonShape _geometryLedge = new() { Segments = [new(-60, 0), new(0, 0), new(0, 0), new(60, 0)] };
    private readonly WorldBoundaryShape _geometryBoundary = new() { Normal = Vector2.Up };
    private readonly SeparationRayShape _geometryRay = new() { Length = 10, SlideOnSlope = true };
    private readonly CircleShape _geometryCircle = new() { Radius = 4 };
    private readonly RectangleShape _geometryBox = new() { Size = new(8, 6) };
    private readonly PhysicsDirectBodyState?[] _geometryStates = new PhysicsDirectBodyState?[4];
    private uint _geometryWakeExpected;
    internal int GeometrySteps, GeometryCorrections, GeometryKicks;
    internal uint GeometryContacts, GeometryImpulses, GeometrySleeping, GeometryWakes, GeometryContactCorrections, GeometrySleepingCorrections;
    internal float MaxGeometryPenetration;

    private CollisionObject CreateGeometry(ObjectSpec spec, string name)
    {
        if (spec.Kind is ObjectKind.GeometryBoundary or ObjectKind.GeometryLedge)
        {
            var floor = new StaticBody { Name = name, Position = spec.Spawn, CollisionLayer = 4, CollisionMask = 4 };
            floor.AddChild(new CollisionShape { Shape = spec.Kind == ObjectKind.GeometryBoundary ? _geometryBoundary : _geometryLedge });
            return floor;
        }
        var body = new RigidBody
        {
            Name = name,
            Position = spec.Spawn,
            Mass = 1,
            CanSleep = true,
            CollisionLayer = 4,
            CollisionMask = 4,
            MaxContactsReported = 8,
            ContactMonitor = true,
            LockRotation = spec.Kind == ObjectKind.GeometryRay,
            Rotation = spec.Kind == ObjectKind.GeometryRay ? 0 : .3f,
            ContinuousCD = spec.Kind == ObjectKind.GeometryPolygon ? CCDMode.CastRay : CCDMode.CastShape,
            LinearDamp = .2f,
            AngularDamp = .8f
        };
        switch (spec.Kind)
        {
            case ObjectKind.GeometryCapsule: body.AddChild(new CollisionShape { Shape = _geometryCapsule }); break;
            case ObjectKind.GeometryPolygon: body.AddChild(new CollisionShape { Shape = _geometryPolygon }); break;
            case ObjectKind.GeometryRay: body.AddChild(new CollisionShape { Shape = _geometryRay }); break;
            case ObjectKind.GeometryCompound:
                body.AddChild(new CollisionShape { Name = "Circle", Shape = _geometryCircle, Position = new(-6, 0) });
                body.AddChild(new CollisionShape { Name = "Box", Shape = _geometryBox, Position = new(6, 0), Rotation = .35f }); break;
            default: throw new InvalidDataException("Unknown network geometry recipe.");
        }
        body.BodyEntered += other => Contact(spec.ID, other, 1);
        return body;
    }
    private void PrepareGeometry(ulong tick)
    {
        _geometryWakeExpected = 0;
        if (tick < 240 || tick % 120 != 0) return;
        for (var i = 0; i < 4; i++)
        {
            var body = (RigidBody)Find((ulong)(210 + i))!.Body!;
            if (body.Sleeping) _geometryWakeExpected |= 1u << i;
            body.ApplyCentralImpulse(new(0, -40)); GeometryKicks++;
        }
    }
    private bool GeometryContact(int index, bool recordImpulse = true)
    {
        var actor = Find((ulong)(210 + index))!;
        var floor = Find(index == 1 ? 201u : 200u)!.Body!;
        var state = _geometryStates[index] ??= PhysicsServer.BodyGetDirectState(actor.RID)!;
        var count = state.GetContactCount();
        for (var i = 0; i < count; i++)
        {
            var impulse = state.GetContactImpulse(i); var normal = state.GetContactLocalNormal(i);
            if (state.GetContactCollider(i) != floor.GetRID() || state.GetContactColliderID(i) != floor.InstanceID ||
                !ReferenceEquals(state.GetContactColliderObject(i), floor) || state.GetContactColliderShape(i) != 0 ||
                (uint)state.GetContactLocalShape(i) >= (index == 3 ? 2u : 1u) || !normal.IsFinite() ||
                MathF.Abs(normal.Length() - 1) > .002f || !impulse.IsFinite())
                throw new InvalidOperationException("Network geometry contact lost its local shape, mapped floor or finite impulse.");
            if (recordImpulse && impulse.Y < -.01f) GeometryImpulses |= 1u << index;
        }
        return count > 0;
    }
    private float GeometryBottom(int index, RigidBody body)
    {
        var pose = body.GlobalTransform;
        switch (index)
        {
            case 0: return pose.Origin.Y + 4 + 5 * MathF.Abs(MathF.Cos(pose.Rotation));
            case 1:
                var bottom = float.NegativeInfinity;
                foreach (var point in PolygonPoints) bottom = MathF.Max(bottom, (pose * point).Y);
                return bottom;
            case 2: return (pose * new Vector2(0, 10)).Y;
            default:
                var box = (CollisionShape)body.GetChild(1);
                var angle = box.GlobalRotation;
                return MathF.Max((pose * new Vector2(-6, 0)).Y + 4,
                    box.GlobalPosition.Y + 4 * MathF.Abs(MathF.Sin(angle)) + 3 * MathF.Abs(MathF.Cos(angle)));
        }
    }
    private void CheckGeometry()
    {
        for (var i = 0; i < 4; i++)
        {
            var actor = Find((ulong)(210 + i))!; var body = (RigidBody)actor.Body!; var bit = 1u << i;
            var excess = GeometryBottom(i, body) - (i == 1 ? 80 : 100);
            if (!body.Position.IsFinite() || !body.LinearVelocity.IsFinite() || !float.IsFinite(body.AngularVelocity) ||
                !float.IsFinite(excess) || excess > .65f || MathF.Abs(body.Position.X - actor.Spec.Spawn.X) > 50)
                throw new InvalidOperationException($"Network geometry continuation failed at {Tick}/{Phase}, family={i}, pose={body.Position}, excess={excess}.");
            MaxGeometryPenetration = MathF.Max(MaxGeometryPenetration, excess);
            if (GeometryContact(i)) GeometryContacts |= bit;
            if (body.Sleeping) GeometrySleeping |= bit;
            if ((_geometryWakeExpected & bit) != 0)
            {
                if (body.Sleeping) throw new InvalidOperationException("Network geometry impulse did not wake the sleeping body.");
                GeometryWakes |= bit;
            }
        }
        GeometrySteps++;
    }
    internal void CheckGeometryCorrection()
    {
        GeometryCorrections++;
        for (var i = 0; i < 4; i++)
        {
            if (GeometryContact(i, recordImpulse: false)) GeometryContactCorrections |= 1u << i;
            if (((RigidBody)Find((ulong)(210 + i))!.Body!).Sleeping) GeometrySleepingCorrections |= 1u << i;
        }
    }
    private void DisposeGeometry()
    {
        _geometryCapsule.Dispose(); _geometryPolygon.Dispose(); _geometryLedge.Dispose(); _geometryBoundary.Dispose();
        _geometryRay.Dispose(); _geometryCircle.Dispose(); _geometryBox.Dispose();
    }
}
