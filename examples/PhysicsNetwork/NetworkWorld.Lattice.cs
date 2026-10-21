using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkWorld
{
    private readonly CircleShape _latticeCircle = new() { Radius = 1 };
    private PhysicsDirectBodyState? _latticeState;
    private Vector2 _latticeVelocityBefore;
    private bool _latticeImpactExpected;
    internal int LatticeSteps, LatticeKicks, LatticeContacts, LatticeCorrections, LatticeContactCorrections;
    internal double MaxLatticeEnergy;
    internal float MaxLatticeMomentumError, MaxLatticeAnchorError;
    private static readonly (ulong A, ulong B, Vector2 LocalA, Vector2 LocalB)[] LatticeEdges =
    [
        (300, 301, new(2, 0), new(-2, 0)), (300, 302, new(0, 2), new(0, -2)),
        (301, 303, new(0, 2), new(0, -2)), (302, 303, new(2, 0), new(-2, 0))
    ];

    private void PrepareLattice(ulong tick, bool final)
    {
        _latticeImpactExpected = final || tick == 1 || tick >= 240 && tick % 120 == 0;
        if (_latticeImpactExpected)
        {
            for (ulong id = 300; id <= 304; id++)
            {
                var actor = Find(id)!; var body = (RigidBody)actor.Body!;
                body.Position = actor.Spec.Spawn; body.Rotation = 0; body.LinearVelocity = Vector2.Zero; body.AngularVelocity = 0;
            }
            ((RigidBody)Find(304)!.Body!).ApplyCentralImpulse(new(8, 0)); LatticeKicks++;
        }
        // Scene velocity publishes after the frame; the impulse already changed live server state.
        _latticeVelocityBefore = PhysicsServer.BodyGetLinearVelocity(Find(304)!.RID);
    }

    private bool LatticeContact(bool stepping)
    {
        var witness = Find(304)!; _latticeState ??= PhysicsServer.BodyGetDirectState(witness.RID)!;
        var count = _latticeState.GetContactCount(); var impulse = Vector2.Zero;
        for (var i = 0; i < count; i++)
        {
            var collider = _latticeState.GetContactCollider(i); Actor? other = null;
            for (ulong id = 300; id <= 303; id++) if (Find(id)!.RID == collider) { other = Find(id); break; }
            var part = _latticeState.GetContactImpulse(i);
            if (other is null || !ReferenceEquals(_latticeState.GetContactColliderObject(i), other.Body) ||
                _latticeState.GetContactColliderID(i) != other.Body!.InstanceID ||
                _latticeState.GetContactLocalShape(i) != 0 || _latticeState.GetContactColliderShape(i) != 0 || !part.IsFinite())
                throw new InvalidOperationException("Network lattice contact lost its mapped body, logical slot or finite impulse.");
            impulse += part;
        }
        if (stepping && (((RigidBody)witness.Body!).LinearVelocity - _latticeVelocityBefore).DistanceTo(impulse) >= .002f)
            throw new InvalidOperationException($"Network lattice contact impulse does not explain the free unit-mass witness velocity change at {Tick}/{Phase}: before={_latticeVelocityBefore}, after={((RigidBody)witness.Body!).LinearVelocity}, impulse={impulse}, count={count}.");
        return count > 0 && impulse.LengthSquared() > 0;
    }

    private void CheckLattice()
    {
        double energy = 0; var momentum = Vector2.Zero;
        for (ulong id = 300; id <= 304; id++)
        {
            var body = (RigidBody)Find(id)!.Body!;
            if (!body.GlobalTransform.IsFinite() || !body.LinearVelocity.IsFinite() || !float.IsFinite(body.AngularVelocity) || body.Sleeping)
                throw new InvalidOperationException("Network lattice body must remain finite and awake.");
            energy += .5 * (body.LinearVelocity.LengthSquared() + body.AngularVelocity * body.AngularVelocity);
            momentum += body.LinearVelocity;
        }
        var momentumError = momentum.DistanceTo(new(8, 0)); var gap = 0f;
        foreach (var edge in LatticeEdges)
            gap = MathF.Max(gap, (Find(edge.A)!.Body!.GlobalTransform * edge.LocalA).DistanceTo(Find(edge.B)!.Body!.GlobalTransform * edge.LocalB));
        if (!double.IsFinite(energy) || energy > 32 * 1.01 + .0001 || momentumError >= .002f || gap >= .05f)
            throw new InvalidOperationException($"Network lattice continuation failed at {Tick}/{Phase}: energy={energy:R}, momentum={momentumError:R}, gap={gap:R}.");
        var contact = LatticeContact(true);
        if (_latticeImpactExpected && !contact) throw new InvalidOperationException("Network lattice impact must produce a real nonzero contact impulse.");
        if (contact) LatticeContacts++;
        MaxLatticeEnergy = Math.Max(MaxLatticeEnergy, energy); MaxLatticeMomentumError = MathF.Max(MaxLatticeMomentumError, momentumError); MaxLatticeAnchorError = MathF.Max(MaxLatticeAnchorError, gap);
        LatticeSteps++;
    }

    internal void CheckLatticeCorrection(bool final = false)
    {
        LatticeCorrections++; var contact = LatticeContact(false);
        if (final && !contact) throw new InvalidOperationException("Network lattice final correction must import the actual impact contact.");
        if (contact) LatticeContactCorrections++;
    }
}
