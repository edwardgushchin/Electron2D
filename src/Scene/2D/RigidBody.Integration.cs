using static Box2D.NET.B2Bodies;

namespace Electron2D;

public partial class RigidBody
{
    private bool _customIntegrator;
    internal float ResolvedLinearDamp => _effectiveLinearDamp;
    internal float ResolvedAngularDamp => _effectiveAngularDamp;

    /// <summary>Gets or sets omission of ordinary gravity, damping and accumulated force integration.</summary>
    /// <value>False by default. Impulses, motion and solver contacts remain active when true.</value>
    /// <remarks>Use the post-solver integration hook to implement custom velocity changes. The flag is stored in packed scenes.</remarks>
    public bool CustomIntegrator
    {
        get { ThrowIfDisposed(); return _customIntegrator; }
        set
        {
            EnsureMutable();
            _customIntegrator = value;
            if (HasBackend) b2Body_SetGravityScale(BackendID, value ? 0 : _gravityScale);
        }
    }

    /// <summary>Overrides velocity or pose using the body's live state after each active solver step.</summary>
    /// <param name="state">An owner-thread view of this attachment and its solved contacts.</param>
    /// <remarks>The scene pose is synchronized before and after this hook. It runs for ordinary and custom
    /// integration alike. An exception is collected without replaying committed edits or skipping other body callbacks.</remarks>
    protected virtual void IntegrateForces(PhysicsDirectBodyState state) { }

    internal void InvokeIntegration(PhysicsDirectBodyState state) => IntegrateForces(state);
}
