namespace Electron2D;

/// <summary>Selects automatic geometry center or the configured local center of mass.</summary>
public enum RigidCenterOfMassMode
{
    /// <summary>Derive the center from active collision geometry.</summary>
    Auto = 0,
    /// <summary>Use the body's configured local center.</summary>
    Custom = 1
}

public partial class RigidBody
{
    private float _inertia;
    private RigidCenterOfMassMode _centerOfMassMode;
    private Vector2 _centerOfMass;

    /// <summary>Gets or sets rotational inertia in kilograms times squared scene units.</summary>
    /// <value>Zero by default, selecting geometry-derived inertia without changing this stored value.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, nonfinite or outside the solver range.</exception>
    public float Inertia
    {
        get { ThrowIfDisposed(); return _inertia; }
        set => SetMassProfile(_mass, value, _centerOfMassMode, _centerOfMass);
    }

    /// <summary>Gets or sets how the body's local center of mass is chosen.</summary>
    /// <value><see cref="RigidCenterOfMassMode.Auto"/> by default.</value>
    /// <remarks>Returning to Auto clears the stored custom center and retains an explicit Inertia override.
    /// A changed mode reports PropertyListChanged after committing the new profile.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="Exception">A property-list subscriber throws after the profile is committed.</exception>
    public RigidCenterOfMassMode CenterOfMassMode
    {
        get { ThrowIfDisposed(); return _centerOfMassMode; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_centerOfMassMode == value) return;
            SetMassProfile(_mass, _inertia, value, value == RigidCenterOfMassMode.Auto ? Vector2.Zero : _centerOfMass);
        }
    }

    /// <summary>Gets or sets the configured center of mass in local scene units relative to body origin.</summary>
    /// <value>Zero by default. Automatic geometry calculations do not replace this stored value.</value>
    /// <exception cref="InvalidOperationException">A changed center is assigned outside Custom mode or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The vector or resulting mass geometry exceeds the finite solver range.</exception>
    public Vector2 CenterOfMass
    {
        get { ThrowIfDisposed(); return _centerOfMass; }
        set
        {
            EnsureMutable();
            PhysicsMass.Validate(_mass, _inertia, value);
            if (_centerOfMass == value) return;
            if (_centerOfMassMode != RigidCenterOfMassMode.Custom)
                throw new InvalidOperationException("Choose Custom center-of-mass mode before assigning a center.");
            SetMassProfile(_mass, _inertia, _centerOfMassMode, value);
        }
    }

    internal Vector2? CustomMassCenter => _centerOfMassMode == RigidCenterOfMassMode.Custom ? _centerOfMass : null;

    internal void SetMassProfile(float mass, float inertia, RigidCenterOfMassMode mode, Vector2 center)
    {
        EnsureMutable();
        PhysicsMass.Validate(mass, inertia, mode == RigidCenterOfMassMode.Custom ? center : null);
        EnsurePhysicsParticipationChange();
        if (HasBackend)
        {
            PrepareBackend();
            PhysicsServer.Instance.BodyRuntime(PhysicsRID).MassData =
                PhysicsMass.Apply(BackendID, BackendShapes, mass, inertia, mode == RigidCenterOfMassMode.Custom ? center : null);
        }
        var modeChanged = _centerOfMassMode != mode;
        _mass = mass; _inertia = inertia; _centerOfMassMode = mode; _centerOfMass = center;
        if (modeChanged) NotifyPropertyListChanged();
    }

    private void ApplyMass(float mass)
    {
        if (!HasBackend) return;
        PhysicsServer.Instance.BodyRuntime(PhysicsRID).MassData =
            PhysicsMass.Apply(BackendID, BackendShapes, mass, _inertia, CustomMassCenter);
    }
}
