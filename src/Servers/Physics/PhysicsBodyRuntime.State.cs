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
        if (owner.Scene is { HasServerMode: true } direct) { direct.SetServerTransform(transform); direct.PrepareBackend(); }
        else if (owner.Scene is { } scene) { scene.GlobalTransform = transform; scene.PrepareBackend(); }
        else owner.Server!.SetTransform(transform);
        if (Space is not null && Backend.HasMotionMode(PhysicsServer.BodyMode.Static)) Backend.WakeTouching();
        Wake();
    }

    internal void SetLinearVelocity(Vector2 velocity)
    {
        Finite(velocity);
        var owner = Owners;
        if (owner.Scene is { HasServerMode: true } direct) direct.SetServerLinearVelocity(velocity);
        else if (owner.Scene is RigidBody rigid) rigid.LinearVelocity = velocity;
        else if (owner.Scene is StaticBody surface)
            surface.SetSurfaceLinearVelocity(velocity, true);
        else if (owner.Scene is not null)
        {
            _surfaceLinear = velocity;
            if (Space is not null) Backend.PublishMotion(linear: Vector2.Zero, surface: (velocity, _surfaceAngular));
        }
        else owner.Server!.SetLinearVelocity(velocity);
        if (Space is null) Wake();
    }

    internal void SetAngularVelocity(float velocity)
    {
        Finite(velocity);
        var owner = Owners;
        if (owner.Scene is { HasServerMode: true } direct) direct.SetServerAngularVelocity(velocity);
        else if (owner.Scene is RigidBody rigid) rigid.AngularVelocity = velocity;
        else if (owner.Scene is StaticBody surface)
            surface.SetSurfaceAngularVelocity(velocity, true);
        else if (owner.Scene is not null)
        {
            _surfaceAngular = velocity;
            if (Space is not null) Backend.PublishMotion(angular: 0, surface: (_surfaceLinear, velocity));
        }
        else owner.Server!.SetAngularVelocity(velocity);
        if (Space is null) Wake();
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
        if (Space is not null) return !Backend.IsAwake;
        var owner = Owners;
        if (owner.Scene?.EffectiveBodyMode == PhysicsServer.BodyMode.Static) return true;
        return owner.Scene is { HasServerMode: true } direct ? direct.ServerSleeping : owner.Scene is RigidBody rigid ? rigid.Sleeping : owner.Server?.GetSleeping() ?? owner.Scene is StaticBody and not AnimatableBody;
    }

    internal void SetSleeping(bool sleeping)
    {
        if (!Dynamic) return;
        var owner = Owners;
        if (owner.Scene is { HasServerMode: true } direct) direct.SetServerSleeping(sleeping);
        else if (owner.Scene is RigidBody rigid) rigid.Sleeping = sleeping;
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
            _canSleep = canSleep;
            if (owner.Scene is { HasServerMode: true } scene) scene.SetServerCanSleep(canSleep);
            else if (Space is not null) Backend.PublishMotion(canSleep: canSleep);
        }
    }

    internal void RestoreSceneState()
    {
        if (Owners.Scene is RigidBody) return;
        Backend.SetCanSleep(_canSleep);
        if (Owners.Scene is not StaticBody) Backend.SetSurfaceVelocity(_surfaceLinear, _surfaceAngular);
    }
}
