using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal sealed partial class PhysicsJointBackend
{
    private B2Vec2 _pointA;
    private B2Vec2 _pointB;
    private B2Vec2 _pendingImpulse;

    internal void PrepareSolverStep(float delta, PhysicsJointRuntime settings)
    {
        _pendingImpulse = default;
        if (settings.Type != PhysicsServer.JointType.DampedSpring || !IsAttached || settings.SpringStiffness == 0 && settings.SpringDamping == 0) return;
        _pointA = b2Body_GetWorldPoint(BodyAID, _localFrameA.p);
        _pointB = b2Body_GetWorldPoint(BodyBID, _localFrameB.p);
        var dx = (double)_pointB.X - _pointA.X;
        var dy = (double)_pointB.Y - _pointA.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < FLT_EPSILON) return;
        var nx = dx / distance;
        var ny = dy / distance;
        var first = PhysicsColliderBackend.Simulation(BodyAID);
        var second = PhysicsColliderBackend.Simulation(BodyBID);
        var crossA = ((double)_pointA.X - first.center.X) * ny - ((double)_pointA.Y - first.center.Y) * nx;
        var crossB = ((double)_pointB.X - second.center.X) * ny - ((double)_pointB.Y - second.center.Y) * nx;
        var inverse = first.invMass + (double)second.invMass + first.invInertia * crossA * crossA + second.invInertia * crossB * crossB;
        if (inverse == 0) return;
        var velocityA = b2Body_GetLinearVelocity(BodyAID);
        var velocityB = b2Body_GetLinearVelocity(BodyBID);
        var angularA = b2Body_GetAngularVelocity(BodyAID);
        var angularB = b2Body_GetAngularVelocity(BodyBID);
        var speed = ((double)velocityB.X - velocityA.X) * nx + ((double)velocityB.Y - velocityA.Y) * ny + angularB * crossB - angularA * crossA;
        var rest = (settings.SpringAutomaticRest ? Math.Abs(settings.SpringAutomaticLength) : settings.SpringRestLength) * (double)PhysicsSpace.MetersPerUnit;
        var elastic = (rest - distance) * settings.SpringStiffness * delta;
        var decay = Math.Exp(-(double)settings.SpringDamping * delta * inverse);
        var total = elastic * decay - speed * (1 - decay) / inverse;
        if (settings.MaxForce < float.MaxValue)
        {
            var limit = (double)settings.MaxForce * PhysicsSpace.MetersPerUnit * delta;
            total = Math.Clamp(total, -limit, limit);
        }
        var impulse = new B2Vec2((float)(nx * total), (float)(ny * total));
        if (!float.IsFinite(impulse.X) || !float.IsFinite(impulse.Y))
            throw new InvalidOperationException("Spring impulse exceeds the finite physics range.");
        _pendingImpulse = impulse;
    }

    internal void ValidateSolverStep(PhysicsSpace space)
    {
        if (_pendingImpulse.X == 0 && _pendingImpulse.Y == 0) return;
        space.ValidateJointImpulse(BodyAID, new(-_pendingImpulse.X, -_pendingImpulse.Y), _pointA);
        space.ValidateJointImpulse(BodyBID, _pendingImpulse, _pointB);
    }

    internal void ApplySolverStep()
    {
        if (_pendingImpulse.X == 0 && _pendingImpulse.Y == 0) return;
        b2Body_ApplyLinearImpulse(BodyAID, new(-_pendingImpulse.X, -_pendingImpulse.Y), _pointA, wake: true);
        b2Body_ApplyLinearImpulse(BodyBID, _pendingImpulse, _pointB, wake: true);
    }

}
