namespace Electron2D;

/// <summary>Adds borrowed post-solver direct-state integration to a live physical Script callback set.</summary>
/// <typeparam name="TBody">Compatible rigid-body receiver.</typeparam>
public sealed class RigidBodyScriptCallbacks<TBody> : CollisionObjectScriptCallbacks<TBody> where TBody : RigidBody
{
    /// <summary>Creates an unused node/picking/integration callback set.</summary>
    public RigidBodyScriptCallbacks() { }
    /// <summary>Runs on the ordinary owner integration lane with its borrowed solved body/contact state.</summary>
    /// <remarks>Uses the same lifetime, mutation and error policy as the native protected integration hook.</remarks>
    public Action<TBody, PhysicsDirectBodyState>? IntegrateForces { get; init; }
    internal override void Populate(NodeScriptBinding target, TBody owner)
    {
        base.Populate(target, owner);
        target.IntegrateForces = IntegrateForces is { } integrate ? state => integrate(owner, state) : null;
    }
}
