namespace Electron2D;

public sealed partial class PhysicsServer
{
    private readonly Dictionary<RID, PhysicsBodyRuntime> _bodyRuntimes = [];

    internal (PhysicsBody? Scene, PhysicsServerCollider? Server) ResolveBodyOwners(RID rid)
    {
        if (ResolveSceneObject(rid) is PhysicsBody scene) return (scene, null);
        lock (_registryGate)
            if (_serverColliders.TryGetValue(rid, out var collider) && !collider.IsArea) return (null, collider);
        throw new ArgumentException("The RID does not identify a live physics body.", nameof(rid));
    }

    internal PhysicsBodyRuntime BodyRuntime(RID body)
    {
        var owners = ResolveBodyOwners(body);
        lock (_registryGate)
        {
            if (!_bodyRuntimes.TryGetValue(body, out var runtime)) _bodyRuntimes.Add(body, runtime = new(body, _sceneObjects.GetValueOrDefault(body), owners.Server));
            return runtime;
        }
    }

    internal void InvalidateBodyView(RID body)
    {
        lock (_registryGate)
            if (_bodyRuntimes.TryGetValue(body, out var runtime)) runtime.InvalidateView();
    }

    internal PhysicsDirectBodyState? BodyGetDirectStateCore(RID body)
    {
        ThrowIfDisposed();
        var runtime = BodyRuntime(body);
        var space = runtime.Space;
        if (space is null) return null;
        space.EnsureQueryAccess();
        var owner = runtime.Owners;
        if (owner.Scene is { } scene) scene.PrepareBackend(); else owner.Server!.PrepareBackend();
        return runtime.GetView(space);
    }

    internal void BodySetForceIntegrationCallbackCore(RID body, Action<PhysicsDirectBodyState>? callback)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable(); runtime.ForceCallback = callback;
    }

    internal void BodySetForceIntegrationCallbackCore<T>(RID body, Action<PhysicsDirectBodyState, T>? callback, T userData) =>
    BodySetForceIntegrationCallbackCore(body, callback is null ? null : state => callback(state, userData));

    internal void BodySetStateSyncCallbackCore(RID body, Action<PhysicsDirectBodyState>? callback)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable(); runtime.SyncCallback = callback;
    }

    internal void BodySetOmitForceIntegrationCore(RID body, bool enable)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable();
        if (runtime.Owners.Scene is RigidBody rigid) rigid.CustomIntegrator = enable;
        else runtime.OmitForces = enable;
    }

    internal bool BodyIsOmittingForceIntegrationCore(RID body)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable(); return runtime.Omitted;
    }

    internal void BodySetMaxContactsReportedCore(RID body, int amount)
    {
        ThrowIfDisposed(); if ((uint)amount > PhysicsBodyRuntime.MaxContactLimit) throw new ArgumentOutOfRangeException(nameof(amount));
        var runtime = BodyRuntime(body); runtime.EnsureMutable();
        if (runtime.Owners.Scene is RigidBody rigid) rigid.MaxContactsReported = amount;
        else { runtime.View?.PrepareContacts(amount); runtime.MaxContacts = amount; }
    }

    internal int BodyGetMaxContactsReportedCore(RID body)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable(); return runtime.ContactLimit;
    }
    internal void BodySetTransformCore(RID body, Transform transform) => ForceRuntime(body).SetTransform(transform);
    internal Transform BodyGetTransformCore(RID body) => ForceRuntime(body).GetTransform();
    internal void BodySetLinearVelocityCore(RID body, Vector2 velocity) => ForceRuntime(body).SetLinearVelocity(velocity);
    internal Vector2 BodyGetLinearVelocityCore(RID body) => ForceRuntime(body).GetLinearVelocity();
    internal void BodySetAngularVelocityCore(RID body, float velocity) => ForceRuntime(body).SetAngularVelocity(velocity);
    internal float BodyGetAngularVelocityCore(RID body) => ForceRuntime(body).GetAngularVelocity();
    internal void BodySetSleepingCore(RID body, bool sleeping) => ForceRuntime(body).SetSleeping(sleeping);
    internal bool BodyGetSleepingCore(RID body) => ForceRuntime(body).GetSleeping();
    internal void BodySetCanSleepCore(RID body, bool canSleep) => ForceRuntime(body).SetCanSleep(canSleep);
    internal bool BodyGetCanSleepCore(RID body) => ForceRuntime(body).GetCanSleep();
    internal void BodySetAxisVelocityCore(RID body, Vector2 axisVelocity) => ForceRuntime(body).SetAxisVelocity(axisVelocity);

}
