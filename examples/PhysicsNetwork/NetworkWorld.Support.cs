using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkWorld
{
    internal static ReadOnlySpan<ObjectSpec> InitialObjects => Initial;
    private static readonly ObjectSpec[] Initial =
    [
        new(1, 1, ObjectKind.Floor, 0, 1, new(320, 320)),
        new(2, 1, ObjectKind.Sensor, 0, 1, new(240, 260)),
        new(10, 1, ObjectKind.Box, 0, 1, new(80, 285)),
        new(11, 1, ObjectKind.Ball, 0, 1, new(160, 285)),
        new(20, 1, ObjectKind.Ball, 0, 1, new(340, 160)),
        new(21, 1, ObjectKind.Ball, 0, 1, new(375, 180)),
        new(100, 1, ObjectKind.Pin, 0, 1, new(360, 170), 20, 21),
        new(40, 1, ObjectKind.Anchor, 0, 1, new(700, 140)),
        new(41, 1, ObjectKind.Ball, 0, 1, new(700, 180)),
        new(101, 1, ObjectKind.Groove, 0, 1, new(700, 140), 40, 41),
        new(50, 1, ObjectKind.Anchor, 0, 1, new(850, 140)),
        new(51, 1, ObjectKind.Ball, 0, 1, new(850, 190)),
        new(102, 1, ObjectKind.Spring, 0, 1, new(850, 140), 50, 51),
        new(70, 1, ObjectKind.Anchor, 0, 1, new(1040, 0)),
        new(71, 1, ObjectKind.SupportBody, 0, 1, new(1040, 10)),
        new(72, 1, ObjectKind.SupportBody, 0, 1, new(1040, 14)),
        new(73, 1, ObjectKind.SupportFloor, 0, 1, new(1040, 30)),
        new(110, 1, ObjectKind.SupportPin, 0, 1, new(1040, 12), 71, 72),
        new(111, 1, ObjectKind.SupportGroove, 0, 1, new(1040, 0), 70, 71),
        new(112, 1, ObjectKind.SupportSpring, 0, 1, new(1040, 0), 70, 71),
        new(200, 1, ObjectKind.GeometryBoundary, 0, 1, new(1300, 100)),
        new(201, 1, ObjectKind.GeometryLedge, 0, 1, new(1400, 80)),
        new(210, 1, ObjectKind.GeometryCapsule, 0, 1, new(1300, 40)),
        new(211, 1, ObjectKind.GeometryPolygon, 0, 1, new(1400, 40)),
        new(212, 1, ObjectKind.GeometryRay, 0, 1, new(1500, 40)),
        new(213, 1, ObjectKind.GeometryCompound, 0, 1, new(1600, 40)),
        new(214, 1, ObjectKind.GeometryRotatingRay, 0, 1, new(1700, 40)),
        new(220, 1, ObjectKind.CrossedCapsuleFirst, 0, 1, new(1800, 40)),
        new(221, 1, ObjectKind.CrossedCapsuleSecond, 0, 1, new(1800, 40))
    ];
    private readonly CircleShape _supportCircle = new() { Radius = 2 };
    private readonly RectangleShape _supportFloor = new() { Size = new(200, 2) };
    private readonly PhysicsMaterial _supportMaterial = new() { Friction = .6f };
    private PhysicsDirectBodyState? _supportState;
    private bool _supportWakeExpected, _supportKicked, _supportWasSleeping, _supportWasAwake;
    private float _supportMomentum, _supportLoadSpeed;
    internal int SupportSteps, SupportContacts, SupportSleeping, SupportWakes, SupportKicks, SupportImpulseSamples;
    internal int SupportCorrections, SupportContactCorrections, SupportSleepingCorrections;
    internal int SupportWakeCorrections;
    private bool _supportCorrectionSleeping;
    internal float MaxSupportPinError, MaxSupportGrooveError, MaxSupportImpulseError;

    private void PrepareSupport(ulong tick, bool final)
    {
        var load = (RigidBody)Find(71)!.Body!; var peer = (RigidBody)Find(72)!.Body!;
        _supportWakeExpected = false; _supportKicked = final || tick >= 240 && tick % 120 == 0;
        _supportWasSleeping = load.Sleeping && peer.Sleeping;
        _supportWasAwake = !load.Sleeping && !peer.Sleeping;
        if (_supportKicked)
        {
            _supportWakeExpected = _supportWasSleeping;
            peer.ApplyCentralImpulse(new(10, -20)); SupportKicks++;
        }
        _supportLoadSpeed = load.LinearVelocity.Y; _supportMomentum = _supportLoadSpeed + peer.LinearVelocity.Y;
    }
    private bool SupportContact()
    {
        var peer = Find(72)!; var floor = Find(73)!.Body!;
        _supportState ??= PhysicsServer.BodyGetDirectState(peer.RID)!;
        var count = _supportState.GetContactCount();
        for (var i = 0; i < count; i++)
            if (_supportState.GetContactCollider(i) != floor.GetRID() || _supportState.GetContactColliderID(i) != floor.InstanceID ||
                !ReferenceEquals(_supportState.GetContactColliderObject(i), floor) || _supportState.GetContactLocalShape(i) != 0 ||
                _supportState.GetContactColliderShape(i) != 0 || _supportState.GetContactLocalNormal(i).DistanceTo(Vector2.Up) >= .001f)
                throw new InvalidOperationException("Network coupled contact lost its real floor identity, shape or normal.");
        return count > 0;
    }
    private void CheckSupport()
    {
        var load = (RigidBody)Find(71)!.Body!; var peer = (RigidBody)Find(72)!.Body!;
        var origin = Find(70)!.Position; var a = load.Position - origin; var b = peer.Position - origin;
        var pin = (b - a - new Vector2(0, 4)).Length();
        var groove = MathF.Max(MathF.Abs(a.X), MathF.Max(-a.Y, a.Y - 100));
        if (!a.IsFinite() || !b.IsFinite() || !float.IsFinite(pin) || pin >= .5f || groove >= .5f ||
            MathF.Abs(load.Rotation) > .001f || MathF.Abs(peer.Rotation) > .001f)
            throw new InvalidOperationException($"Network coupled constraints failed at {Tick} in {Phase}: {a}/{b}, pin={pin}, groove={groove}.");
        MaxSupportPinError = MathF.Max(MaxSupportPinError, pin); MaxSupportGrooveError = MathF.Max(MaxSupportGrooveError, groove);
        var contact = SupportContact(); if (contact) SupportContacts++;
        if (load.Sleeping && peer.Sleeping)
        {
            if (MathF.Abs(a.Y - 23) >= .15f || MathF.Abs(b.Y - 27) >= .15f)
                throw new InvalidOperationException("Network coupled sleeping island lost its physical support.");
            SupportSleeping++;
        }
        if (_supportWakeExpected)
        {
            if (load.Sleeping || peer.Sleeping) throw new InvalidOperationException("Network coupled impulse failed to wake both connected bodies.");
            SupportWakes++;
        }
        // A transition to sleep can stop gravity partway through the frame.
        if (contact && _supportWasAwake && !load.Sleeping && !peer.Sleeping && !_supportKicked &&
            load.LinearVelocity.Length() < .1f && peer.LinearVelocity.Length() < .1f)
        {
            SupportImpulseSamples++;
            var elastic = 20 * (50 - a.Y); var impulse = Vector2.Zero;
            for (var i = 0; i < _supportState!.GetContactCount(); i++) impulse += _supportState.GetContactImpulse(i);
            var expected = (1960 + elastic) / 60 - (load.LinearVelocity.Y + peer.LinearVelocity.Y - _supportMomentum);
            var dampingBound = (elastic / 60 + MathF.Abs(_supportLoadSpeed) + 980f / 60) * (1 - MathF.Exp(-4f / 60));
            var deficit = expected + impulse.Y;
            if (!impulse.IsFinite() || impulse.Y >= -1 || deficit < -.15f || deficit > dampingBound + .15f)
                throw new InvalidOperationException($"Network coupled support impulse {impulse} at {Tick}/{Phase} does not balance weight and spring: expected={expected}, bound={dampingBound}, sleepingBefore={_supportWasSleeping}, sleepingAfter={load.Sleeping}/{peer.Sleeping}.");
            MaxSupportImpulseError = MathF.Max(MaxSupportImpulseError, MathF.Abs(deficit));
        }
        SupportSteps++;
    }
    internal void CheckSupportCorrection()
    {
        SupportCorrections++;
        if (SupportContact()) SupportContactCorrections++;
        var sleeping = ((RigidBody)Find(71)!.Body!).Sleeping && ((RigidBody)Find(72)!.Body!).Sleeping;
        if (sleeping) SupportSleepingCorrections++;
        if (_supportCorrectionSleeping && !((RigidBody)Find(71)!.Body!).Sleeping && !((RigidBody)Find(72)!.Body!).Sleeping) SupportWakeCorrections++;
        _supportCorrectionSleeping = sleeping;
    }
}
