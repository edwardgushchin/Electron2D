using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkWorld
{
    private readonly CapsuleShape _crossedCapsule = new() { Radius = 1, Height = 6 };
    private readonly PhysicsMaterial _crossedMaterial = new() { Bounce = .5f, Friction = 0 };
    private readonly PhysicsDirectBodyState?[] _crossedStates = new PhysicsDirectBodyState?[2];
    private static Vector2 CrossedNormal => new Vector2(MathF.Sqrt(3.64f) / 2, .3f).Rotated(.41f);
    private bool _crossedPrepared;
    internal int CrossedContacts, CrossedContactCorrections;

    private RigidBody CreateCrossedCapsule(ObjectSpec spec, string name)
    {
        var first = spec.Kind == ObjectKind.CrossedCapsuleFirst;
        var body = new RigidBody
        {
            Name = name,
            Position = spec.Spawn,
            Mass = first ? 2 : 3,
            Inertia = first ? 20 : 30,
            GravityScale = 0,
            CanSleep = false,
            CollisionLayer = 8,
            CollisionMask = 8,
            MaxContactsReported = 8,
            ContactMonitor = true,
            ContinuousCD = CCDMode.CastShape,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0,
            AngularDamp = 0,
            PhysicsMaterialOverride = _crossedMaterial
        };
        body.AddChild(new CollisionShape { Name = "Disabled", Shape = _circle, Disabled = true });
        body.AddChild(new CollisionShape { Name = "Capsule", Shape = _crossedCapsule });
        body.BodyEntered += other => Contact(spec.ID, other, 1);
        return body;
    }
    private void PrepareCrossedCapsules(ulong tick, bool final)
    {
        _crossedPrepared = final || tick == 1 || tick >= 240 && tick % 120 == 0;
        if (!_crossedPrepared) return;
        for (var i = 0; i < 2; i++)
        {
            var actor = Find((ulong)(220 + i))!; var body = (RigidBody)actor.Body!;
            body.Position = actor.Spec.Spawn + (i == 0 ? Vector2.Zero :
                new Vector2(1.2f + MathF.Sqrt(3.64f), 1).Rotated(.41f) + CrossedNormal * .05f);
            body.Rotation = .41f + (i == 0 ? 0 : MathF.Atan2(.6f, .8f));
            body.LinearVelocity = CrossedNormal * (i == 0 ? 5 : -5); body.AngularVelocity = 0;
        }
    }
    private void CheckCrossedCapsules(bool correction = false)
    {
        if (!correction && !_crossedPrepared) return;
        var impulseA = Impulse(0); var impulseB = Impulse(1);
        var a = (RigidBody)Find(220)!.Body!; var b = (RigidBody)Find(221)!.Body!;
        var va = a.LinearVelocity; var vb = b.LinearVelocity;
        var energy = va.LengthSquared() + 1.5f * vb.LengthSquared() + 10 * a.AngularVelocity * a.AngularVelocity + 15 * b.AngularVelocity * b.AngularVelocity;
        if (!float.IsFinite(energy) || !impulseA.IsFinite() || !impulseB.IsFinite() || (impulseA + impulseB).Length() > .03f || (2 * va + 3 * vb + CrossedNormal * 5).Length() > .03f ||
            energy > 62.6f || (vb - va).Dot(CrossedNormal) <= 0 || a.AngularVelocity <= .01f || b.AngularVelocity >= -.01f)
            throw new InvalidOperationException($"Network crossed capsules lost coupled response at {Tick}/{Phase}, impulses={impulseA}/{impulseB}, energy={energy}, angular={a.AngularVelocity}/{b.AngularVelocity}.");
        if (correction) CrossedContactCorrections++; else CrossedContacts++;

        Vector2 Impulse(int index)
        {
            var actor = Find((ulong)(220 + index))!; var peer = Find((ulong)(221 - index))!.Body!;
            var body = (RigidBody)actor.Body!; var mass = index == 0 ? 2 : 3;
            var before = CrossedNormal * (index == 0 ? 5 : -5);
            var state = _crossedStates[index] ??= PhysicsServer.BodyGetDirectState(actor.RID)!;
            var impulse = Vector2.Zero; var moment = 0f;
            if (state.GetContactCount() == 0) throw new InvalidOperationException("Network crossed capsules lost their real contact.");
            for (var i = 0; i < state.GetContactCount(); i++)
            {
                if (state.GetContactCollider(i) != peer.GetRID() || state.GetContactColliderID(i) != peer.InstanceID ||
                    !ReferenceEquals(state.GetContactColliderObject(i), peer) || state.GetContactLocalShape(i) != 1 || state.GetContactColliderShape(i) != 1)
                    throw new InvalidOperationException("Network crossed capsules lost mapped object or logical slot identity.");
                var value = state.GetContactImpulse(i); var arm = state.GetContactLocalPosition(i) - body.GlobalTransform * state.CenterOfMassLocal;
                impulse += value; moment += arm.X * value.Y - arm.Y * value.X;
            }
            var limit = .01f + (float)FixedStep * MathF.Max(5, body.LinearVelocity.Length()) * impulse.Length();
            if (!impulse.IsFinite() || !float.IsFinite(moment) || (impulse - mass * (body.LinearVelocity - before)).Length() > .03f || MathF.Abs(moment - mass * 10 * body.AngularVelocity) > limit)
                throw new InvalidOperationException("Network crossed capsules lost full linear/angular impulse state.");
            return impulse;
        }
    }
    internal void CheckCrossedCorrection(bool final = false)
    {
        if (final || Tick == 1 || Tick >= 240 && Tick % 120 == 0) CheckCrossedCapsules(correction: true);
    }
    private void DisposeCrossedCapsules()
    {
        _crossedCapsule.Dispose(); _crossedMaterial.Dispose();
    }
}
