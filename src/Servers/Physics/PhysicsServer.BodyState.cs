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
        ResolveBodyOwners(body);
        lock (_registryGate)
        {
            if (!_bodyRuntimes.TryGetValue(body, out var runtime)) _bodyRuntimes.Add(body, runtime = new(body));
            return runtime;
        }
    }

    internal void InvalidateBodyView(RID body)
    {
        lock (_registryGate)
            if (_bodyRuntimes.TryGetValue(body, out var runtime)) runtime.View = null;
    }

    /// <summary>Gets a live owner-thread view of an attached body, or null while detached.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>A cached view tied to this backend attachment; caller disposal invalidates only that view.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the solver is stepping.</exception>
    public PhysicsDirectBodyState? BodyGetDirectState(RID body)
    {
        ThrowIfDisposed();
        var runtime = BodyRuntime(body);
        var space = runtime.Space;
        if (space is null) return null;
        space.PrepareForQuery();
        return runtime.GetView(space, runtime.BodyID);
    }

    /// <summary>Sets or clears the body's post-solver force-integration callback.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="callback">Owner-thread callback invoked before state synchronization, or null to clear.</param>
    /// <remarks>Scene bodies retain their tree-owned pose synchronization. Registration does not enable custom integration.</remarks>
    public void BodySetForceIntegrationCallback(RID body, Action<PhysicsDirectBodyState>? callback)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable(); runtime.ForceCallback = callback;
    }

    /// <summary>Sets a post-solver force callback with strongly typed user data.</summary>
    /// <typeparam name="T">The user data type.</typeparam>
    /// <param name="body">A live body RID.</param>
    /// <param name="callback">Callback receiving the view and user data, or null to clear.</param>
    /// <param name="userData">Data retained with the registered callback.</param>
    /// <remarks>The adapter is allocated at registration; invocation does not box value-type data.</remarks>
    public void BodySetForceIntegrationCallback<T>(RID body, Action<PhysicsDirectBodyState, T>? callback, T userData) =>
        BodySetForceIntegrationCallback(body, callback is null ? null : state => callback(state, userData));

    /// <summary>Sets or clears an owner-thread observer of the body's solved state.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="callback">Post-integration state callback, or null to clear.</param>
    /// <remarks>A previous user callback is replaced. Scene-owned pose synchronization remains mandatory.</remarks>
    public void BodySetStateSyncCallback(RID body, Action<PhysicsDirectBodyState>? callback)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable(); runtime.SyncCallback = callback;
    }

    /// <summary>Enables or disables omission of automatic gravity, damping and accumulated forces.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="enable">True for manual force integration; impulses and solver contacts remain active.</param>
    public void BodySetOmitForceIntegration(RID body, bool enable)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable();
        if (runtime.Owners.Scene is RigidBody rigid) rigid.CustomIntegrator = enable;
        else runtime.OmitForces = enable;
    }

    /// <summary>Tests whether a body omits automatic force integration.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>The current custom-integration policy.</returns>
    public bool BodyIsOmittingForceIntegration(RID body)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable(); return runtime.Omitted;
    }

    /// <summary>Sets the maximum retained contact-point count for a body.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="amount">Nonnegative cap; zero disables contact snapshots.</param>
    /// <exception cref="ArgumentOutOfRangeException">The cap is negative.</exception>
    public void BodySetMaxContactsReported(RID body, int amount)
    {
        ThrowIfDisposed(); if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var runtime = BodyRuntime(body); runtime.EnsureMutable();
        if (runtime.Owners.Scene is RigidBody rigid) rigid.MaxContactsReported = amount;
        else runtime.MaxContacts = amount;
    }

    /// <summary>Gets the body's configured contact-point limit.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>The nonnegative limit; zero disables contact snapshots.</returns>
    public int BodyGetMaxContactsReported(RID body)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureMutable(); return runtime.ContactLimit;
    }
}
