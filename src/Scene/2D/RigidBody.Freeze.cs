using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

/// <summary>Chooses stationary or manually driven kinematic participation while a rigid body is frozen.</summary>
public enum RigidFreezeMode
{
    /// <summary>Manual transforms teleport a stationary body without contact velocity.</summary>
    Static = 0,
    /// <summary>Manual transforms drive a kinematic body with derived contact velocity.</summary>
    Kinematic = 1
}

public partial class RigidBody
{
    private RigidFreezeMode _freezeMode;
    private Transform _frozenSolverPose = Transform.Identity;
    private bool _frozenQueryPoseApplied;
    private B2Transform _frozenNativePose;

    /// <summary>Gets or sets how a frozen body participates in physics.</summary>
    /// <value><see cref="RigidFreezeMode.Static"/> by default. The value has no effect while Freeze is false.</value>
    /// <remarks>Kinematic derives velocity from manual global pose changes over the next nonzero fixed step.
    /// Gravity and force response remain disabled; inherited MakeStatic can temporarily override this role.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned enum value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner or during solver ownership.</exception>
    /// <exception cref="ObjectDisposedException">The body is disposed.</exception>
    public RigidFreezeMode FreezeMode
    {
        get { ThrowIfDisposed(); return _freezeMode; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_freezeMode == value) return;
            EnsurePhysicsParticipationChange();
            if (HasBackend) PrepareBackend();
            _freezeMode = value;
            UpdatePhysicsParticipation();
        }
    }

    internal bool FrozenKinematic => _freeze && _freezeMode == RigidFreezeMode.Kinematic;

    internal override void ApplySceneTransform(Vector2 position, float rotation)
    {
        if (FrozenKinematic && !PhysicsMadeStatic && GlobalTransform == _frozenSolverPose) return;
        base.ApplySceneTransform(position, rotation);
        _frozenQueryPoseApplied = FrozenKinematic && !PhysicsMadeStatic;
    }

    internal void ResetFrozenSolverPose()
    {
        _frozenSolverPose = GlobalTransform;
        if (HasBackend) _frozenNativePose = b2Body_GetTransform(BackendID);
        _frozenQueryPoseApplied = false;
    }

    internal void PrepareFrozenMotion(double delta)
    {
        if (!FrozenKinematic) return;
        var target = GlobalTransform;
        if (!target.IsFinite() || !target.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(target.Skew))
            throw new InvalidOperationException("Physics bodies require finite unit-scale global poses.");
        if (PhysicsMadeStatic)
        {
            b2Body_SetTransform(BackendID, Shape.ToBackend(target.Origin), b2MakeRot(target.Rotation));
            return;
        }
        if (target == _frozenSolverPose)
        {
            b2Body_SetLinearVelocity(BackendID, default); b2Body_SetAngularVelocity(BackendID, 0);
            _frozenQueryPoseApplied = false;
            return;
        }
        if (_frozenQueryPoseApplied)
            b2Body_SetTransform(BackendID, _frozenNativePose.p, _frozenNativePose.q);
        b2Body_SetTargetTransform(BackendID, new(Shape.ToBackend(target.Origin), b2MakeRot(target.Rotation)), (float)delta, true);
    }

}
