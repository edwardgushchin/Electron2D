namespace Electron2D;

public sealed partial class PhysicsServer
{
    private readonly Dictionary<RID, PhysicsBodyRuntime> _bodyRuntimes = [];

    internal (PhysicsBody? Scene, PhysicsServerCollider? Server) ResolveBodyOwners(RID rid)
    {
        if (TryResolveBodyOwners(rid, out var owners)) return owners;
        throw new ArgumentException("The RID does not identify a live physics body.", nameof(rid));
    }
    internal bool TryResolveBodyOwners(RID rid, out (PhysicsBody? Scene, PhysicsServerCollider? Server) owners)
    {
        if (ResolveSceneObject(rid) is PhysicsBody scene) { owners = (scene, null); return true; }
        lock (_registryGate)
            if (_serverColliders.TryGetValue(rid, out var collider) && !collider.IsArea) { owners = (null, collider); return true; }
        owners = default; return false;
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
        var runtime = PrepareBodyStateAccess(body);
        return runtime.Space is { } space ? runtime.GetView(space) : null;
    }

    internal PhysicsBodyRuntime PrepareBodyStateAccess(RID body)
    {
        ThrowIfDisposed();
        var runtime = BodyRuntime(body);
        var space = runtime.Space;
        if (space is null) return runtime;
        space.EnsureQueryAccess();
        var owner = runtime.Owners;
        if (owner.Scene is { } scene) scene.PrepareBackend(); else owner.Server!.PrepareBackend();
        runtime.PublishFields();
        return runtime;
    }

    internal void BodySetForceIntegrationCallbackCore(RID body, Action<PhysicsDirectBodyState>? callback)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureAccess(writing: true); runtime.ForceCallback = callback;
    }

    internal void BodySetForceIntegrationCallbackCore<T>(RID body, Action<PhysicsDirectBodyState, T>? callback, T userData) =>
    BodySetForceIntegrationCallbackCore(body, callback is null ? null : state => callback(state, userData));

    internal void BodySetStateSyncCallbackCore(RID body, Action<PhysicsDirectBodyState>? callback)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureAccess(writing: true); runtime.SyncCallback = callback;
    }

    internal void BodySetOmitForceIntegrationCore(RID body, bool enable)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureAccess(writing: true);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.CustomIntegrator = enable;
        else runtime.OmitForces = enable;
    }

    internal bool BodyIsOmittingForceIntegrationCore(RID body)
    {
        return ParameterRuntime(body, writing: false).Omitted;
    }

    internal void BodySetMaxContactsReportedCore(RID body, int amount)
    {
        ThrowIfDisposed(); if ((uint)amount > PhysicsBodyRuntime.MaxContactLimit) throw new ArgumentOutOfRangeException(nameof(amount));
        var runtime = BodyRuntime(body); runtime.EnsureAccess(writing: true);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.MaxContactsReported = amount;
        else
        {
            runtime.View?.PrepareContacts(amount); runtime.MaxContacts = amount;
            if (runtime.Space is not null) runtime.Backend.SetContactReporting(amount > 0);
        }
    }

    internal void BodySetContactsReportedDepthThresholdCore(RID body, float threshold)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(threshold) || threshold < 0) throw new ArgumentOutOfRangeException(nameof(threshold));
        var runtime = BodyRuntime(body); runtime.EnsureAccess(writing: true);
        runtime.ContactsReportedDepthThreshold = threshold;
    }
    internal float BodyGetContactsReportedDepthThresholdCore(RID body)
    {
        return ParameterRuntime(body, writing: false).ContactsReportedDepthThreshold;
    }

    internal int BodyGetMaxContactsReportedCore(RID body)
    {
        return ParameterRuntime(body, writing: false).ContactLimit;
    }
    internal void BodySetTransformCore(RID body, Transform transform) => ForceRuntime(body).SetTransform(transform);
    internal Transform BodyGetTransformCore(RID body) => ForceRuntime(body, writing: false).GetTransform();
    internal void BodySetLinearVelocityCore(RID body, Vector2 velocity) => ForceRuntime(body).SetLinearVelocity(velocity);
    internal Vector2 BodyGetLinearVelocityCore(RID body) => ForceRuntime(body, writing: false).GetLinearVelocity();
    internal void BodySetAngularVelocityCore(RID body, float velocity) => ForceRuntime(body).SetAngularVelocity(velocity);
    internal float BodyGetAngularVelocityCore(RID body) => ForceRuntime(body, writing: false).GetAngularVelocity();
    internal void BodySetSleepingCore(RID body, bool sleeping) => ForceRuntime(body).SetSleeping(sleeping);
    internal bool BodyGetSleepingCore(RID body) => ForceRuntime(body, writing: false).GetSleeping();
    internal void BodySetCanSleepCore(RID body, bool canSleep) => ForceRuntime(body).SetCanSleep(canSleep);
    internal bool BodyGetCanSleepCore(RID body) => ParameterRuntime(body, writing: false).GetCanSleep();
    internal void BodySetAxisVelocityCore(RID body, Vector2 axisVelocity) => ForceRuntime(body).SetAxisVelocity(axisVelocity);

}
