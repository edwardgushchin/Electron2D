using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkWorld
{
    private const int GeometryFamilies = 5;
    internal const uint GeometryFamilyMask = (1u << GeometryFamilies) - 1;
    private static readonly Vector2[] PolygonPoints = [new(-8, -4), new(6, -6), new(9, 4), new(-6, 7)];
    private readonly CapsuleShape _geometryCapsule = new() { Radius = 4, Height = 18 };
    private readonly ConvexPolygonShape _geometryPolygon = new() { Points = PolygonPoints };
    private readonly ConcavePolygonShape _geometryLedge = new() { Segments = [new(-60, 0), new(0, 0), new(0, 0), new(60, 0)] };
    private readonly WorldBoundaryShape _geometryBoundary = new() { Normal = Vector2.Up };
    private readonly SeparationRayShape _geometryRay = new() { Length = 10, SlideOnSlope = true };
    private readonly CircleShape _geometryCircle = new() { Radius = 4 };
    private readonly RectangleShape _geometryBox = new() { Size = new(8, 6) };
    private readonly PhysicsDirectBodyState?[] _geometryStates = new PhysicsDirectBodyState?[GeometryFamilies];
    private uint _geometryWakeExpected;
    private float _geometryRayRotationBefore;
    internal int GeometrySteps, GeometryCorrections, GeometryKicks;
    internal uint GeometryContacts, GeometryImpulses, GeometrySleeping, GeometryWakes, GeometryContactCorrections, GeometrySleepingCorrections;
    internal uint GeometryWakeCorrections;
    private uint _geometryCorrectionSleeping;
    internal float MaxGeometryPenetration, MaxGeometryRayAngularSpeed;
    internal int GeometryRayAngularSteps, GeometryRayAngularCorrections;
    internal uint GeometryCompoundContacts, GeometryCompoundImpulses, GeometryCompoundContactCorrections;

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
            Inertia = spec.Kind == ObjectKind.GeometryRotatingRay ? 2 : 0,
            AngularDamp = spec.Kind == ObjectKind.GeometryRotatingRay ? 4 : .8f
        };
        switch (spec.Kind)
        {
            case ObjectKind.GeometryCapsule: body.AddChild(new CollisionShape { Shape = _geometryCapsule }); break;
            case ObjectKind.GeometryPolygon: body.AddChild(new CollisionShape { Shape = _geometryPolygon }); break;
            case ObjectKind.GeometryRay:
            case ObjectKind.GeometryRotatingRay: body.AddChild(new CollisionShape { Shape = _geometryRay }); break;
            case ObjectKind.GeometryCompound:
                body.AddChild(new CollisionShape { Name = "Circle", Shape = _geometryCircle, Position = new(-6, 0) });
                body.AddChild(new CollisionShape { Name = "Box", Shape = _geometryBox, Position = new(6, 0), Rotation = .35f }); break;
            default: throw new InvalidDataException("Unknown network geometry recipe.");
        }
        body.BodyEntered += other => Contact(spec.ID, other, 1);
        return body;
    }
    private void PrepareGeometry(ulong tick, bool final)
    {
        _geometryRayRotationBefore = Find(214)!.Body!.GlobalRotation;
        _geometryWakeExpected = 0;
        if (!final && (tick < 240 || tick % 120 != 0)) return;
        for (var i = 0; i < GeometryFamilies; i++)
        {
            var body = (RigidBody)Find((ulong)(210 + i))!.Body!;
            if (body.Sleeping) _geometryWakeExpected |= 1u << i;
            body.ApplyCentralImpulse(new(0, -40));
            if (i == 4) body.ApplyTorqueImpulse(tick % 240 == 0 ? 2 : -2);
            GeometryKicks++;
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
            var localShape = state.GetContactLocalShape(i);
            if (state.GetContactCollider(i) != floor.GetRID() || state.GetContactColliderID(i) != floor.InstanceID ||
                !ReferenceEquals(state.GetContactColliderObject(i), floor) || state.GetContactColliderShape(i) != 0 ||
                (uint)localShape >= (index == 3 ? 2u : 1u) || !normal.IsFinite() ||
                MathF.Abs(normal.Length() - 1) > .002f || !impulse.IsFinite())
                throw new InvalidOperationException("Network geometry contact lost its local shape, mapped floor or finite impulse.");
            if (recordImpulse && impulse.Y < -.01f) GeometryImpulses |= 1u << index;
            if (index == 3)
            {
                var point = actor.Body!.GlobalTransform.AffineInverse() * state.GetContactLocalPosition(i);
                if (!point.IsFinite() || MathF.Abs(point.X) <= .25f || localShape != (point.X < 0 ? 0 : 1))
                    throw new InvalidOperationException("Network compound contact shape does not match its authored left/right geometry.");
                if (recordImpulse)
                {
                    GeometryCompoundContacts |= 1u << localShape;
                    if (impulse.Y < -.01f) GeometryCompoundImpulses |= 1u << localShape;
                }
                else GeometryCompoundContactCorrections |= 1u << localShape;
            }
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
            case 4: return MathF.Max(pose.Origin.Y, (pose * new Vector2(0, 10)).Y);
            default:
                var box = (CollisionShape)body.GetChild(1);
                var angle = box.GlobalRotation;
                return MathF.Max((pose * new Vector2(-6, 0)).Y + 4,
                    box.GlobalPosition.Y + 4 * MathF.Abs(MathF.Sin(angle)) + 3 * MathF.Abs(MathF.Cos(angle)));
        }
    }
    private void CheckGeometry()
    {
        for (var i = 0; i < GeometryFamilies; i++)
        {
            var actor = Find((ulong)(210 + i))!; var body = (RigidBody)actor.Body!; var bit = 1u << i;
            var excess = GeometryBottom(i, body) - (i == 1 ? 80 : 100);
            if (!body.Position.IsFinite() || !body.LinearVelocity.IsFinite() || !float.IsFinite(body.AngularVelocity) ||
                !float.IsFinite(excess) || excess > .65f || MathF.Abs(body.Position.X - actor.Spec.Spawn.X) > 50)
                throw new InvalidOperationException($"Network geometry continuation failed at {Tick}/{Phase}, family={i}, pose={body.Position}, excess={excess}.");
            MaxGeometryPenetration = MathF.Max(MaxGeometryPenetration, excess);
            if (i == 4)
            {
                MaxGeometryRayAngularSpeed = MathF.Max(MaxGeometryRayAngularSpeed, MathF.Abs(body.AngularVelocity));
                if (MathF.Abs(Mathf.AngleDifference(_geometryRayRotationBefore, body.GlobalRotation)) > .0001f && MathF.Abs(body.AngularVelocity) > .01f)
                    GeometryRayAngularSteps++;
            }
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
        var rotatingRay = (RigidBody)Find(214)!.Body!;
        if (MathF.Abs(rotatingRay.AngularVelocity) > .01f && MathF.Abs(Mathf.AngleDifference(.3f, rotatingRay.GlobalRotation)) > .01f)
            GeometryRayAngularCorrections++;
        uint sleeping = 0;
        for (var i = 0; i < GeometryFamilies; i++)
        {
            if (GeometryContact(i, recordImpulse: false)) GeometryContactCorrections |= 1u << i;
            if (((RigidBody)Find((ulong)(210 + i))!.Body!).Sleeping) sleeping |= 1u << i;
        }
        GeometrySleepingCorrections |= sleeping;
        GeometryWakeCorrections |= _geometryCorrectionSleeping & ~sleeping;
        _geometryCorrectionSleeping = sleeping;
    }
    private void DisposeGeometry()
    {
        _geometryCapsule.Dispose(); _geometryPolygon.Dispose(); _geometryLedge.Dispose(); _geometryBoundary.Dispose();
        _geometryRay.Dispose(); _geometryCircle.Dispose(); _geometryBox.Dispose();
    }
}
