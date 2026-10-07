using Box2D.NET;
using static Box2D.NET.B2Bodies;

namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    private Vector2 _surfaceLinear;
    private float _surfaceAngular;
    private bool _canSleep = true;

    internal Transform GetTransform()
    {
        var owner = Owners;
        return owner.Scene?.GlobalTransform ?? owner.Server!.GetTransform();
    }

    internal void SetTransform(Transform transform)
    {
        PhysicsServerCollider.ValidateTransform(transform);
        var owner = Owners;
        if (owner.Scene is { } scene) { scene.GlobalTransform = transform; scene.PrepareBackend(); }
        else owner.Server!.SetTransform(transform);
        if (Space is not null && b2Body_GetType(BodyID) == B2BodyType.b2_staticBody) b2Body_WakeTouching(BodyID);
        Wake();
    }

    internal void SetLinearVelocity(Vector2 velocity)
    {
        Finite(velocity);
        var owner = Owners;
        if (owner.Scene is RigidBody rigid) rigid.LinearVelocity = velocity;
        else if (owner.Scene is StaticBody surface)
        {
            if (Space is not null) b2Body_SetLinearVelocity(BodyID, default);
            surface.ConstantLinearVelocity = velocity;
        }
        else if (owner.Scene is not null)
        {
            if (Space is not null)
            {
                b2Body_SetLinearVelocity(BodyID, default);
                SetSurfaceVelocity(BodyID, velocity, _surfaceAngular);
            }
            _surfaceLinear = velocity;
        }
        else owner.Server!.SetLinearVelocity(velocity);
        Wake();
    }

    internal void SetAngularVelocity(float velocity)
    {
        Finite(velocity);
        var owner = Owners;
        if (owner.Scene is RigidBody rigid) rigid.AngularVelocity = velocity;
        else if (owner.Scene is StaticBody surface)
        {
            if (Space is not null) b2Body_SetAngularVelocity(BodyID, 0);
            surface.ConstantAngularVelocity = velocity;
        }
        else if (owner.Scene is not null)
        {
            if (Space is not null)
            {
                b2Body_SetAngularVelocity(BodyID, 0);
                SetSurfaceVelocity(BodyID, _surfaceLinear, velocity);
            }
            _surfaceAngular = velocity;
        }
        else owner.Server!.SetAngularVelocity(velocity);
        Wake();
    }

    internal void SetAxisVelocity(Vector2 axisVelocity) => SetLinearVelocity(ProjectAxisVelocity(GetLinearVelocity(), axisVelocity));

    internal static Vector2 ProjectAxisVelocity(Vector2 velocity, Vector2 axisVelocity)
    {
        Finite(axisVelocity);
        var length = Math.Sqrt((double)axisVelocity.X * axisVelocity.X + (double)axisVelocity.Y * axisVelocity.Y);
        if (length == 0) return velocity;
        var x = axisVelocity.X / length; var y = axisVelocity.Y / length;
        var projection = x * velocity.X + y * velocity.Y;
        var result = new Vector2((float)(velocity.X - x * projection + axisVelocity.X),
            (float)(velocity.Y - y * projection + axisVelocity.Y));
        Finite(result); return result;
    }

    internal bool GetSleeping()
    {
        if (Space is not null) return !b2Body_IsAwake(BodyID);
        var owner = Owners;
        return owner.Scene is RigidBody rigid ? rigid.Sleeping : owner.Server?.GetSleeping() ?? owner.Scene is StaticBody and not AnimatableBody;
    }

    internal void SetSleeping(bool sleeping)
    {
        if (!Dynamic) return;
        var owner = Owners;
        if (owner.Scene is RigidBody rigid) rigid.Sleeping = sleeping;
        else owner.Server!.SetSleeping(sleeping);
    }

    internal bool GetCanSleep() => Owners.Scene is RigidBody rigid ? rigid.CanSleep : Owners.Server?.GetCanSleep() ?? _canSleep;

    internal void SetCanSleep(bool canSleep)
    {
        var owner = Owners;
        if (owner.Scene is RigidBody rigid) rigid.CanSleep = canSleep;
        else if (owner.Server is { } server) server.SetCanSleep(canSleep);
        else
        {
            if (Space is not null) b2Body_EnableSleep(BodyID, canSleep);
            _canSleep = canSleep;
        }
    }

    internal void RestoreSceneState()
    {
        if (Owners.Scene is RigidBody) return;
        b2Body_EnableSleep(BodyID, _canSleep);
        if (Owners.Scene is not StaticBody) SetSurfaceVelocity(BodyID, _surfaceLinear, _surfaceAngular);
    }
}
