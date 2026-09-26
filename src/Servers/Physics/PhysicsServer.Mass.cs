namespace Electron2D;

public sealed partial class PhysicsServer
{
    private PhysicsBodyRuntime MassRuntime(RID body)
    {
        ThrowIfDisposed();
        var runtime = BodyRuntime(body);
        var owners = runtime.Owners;
        owners.Scene?.Tree?.EnsureOwnerThread();
        owners.Scene?.EnsurePhysicsParticipationChange();
        runtime.EnsureMutable();
        if (runtime.Space is not null)
        {
            if (owners.Scene is { } scene) scene.PrepareBackend(); else owners.Server!.PrepareBackend();
        }
        return runtime;
    }

    private static float ConfiguredMass(PhysicsBodyRuntime runtime) => runtime.Owners.Scene is RigidBody rigid ? rigid.Mass : runtime.Mass;
    private static float ConfiguredInertia(PhysicsBodyRuntime runtime) => runtime.Owners.Scene is RigidBody rigid ? rigid.Inertia : runtime.Inertia;
    private static Vector2? ConfiguredCenter(PhysicsBodyRuntime runtime) => runtime.Owners.Scene is RigidBody rigid ? rigid.CustomMassCenter : runtime.CustomCenter;

    /// <summary>Sets a body's positive finite mass in kilograms.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="mass">Positive kilograms within the finite solver range; default is one.</param>
    /// <remarks>Retains center and inertia policy. Detached configuration is applied on attachment.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mass or resulting geometry is outside the solver range.</exception>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetMass(RID body, float mass)
    {
        var runtime = MassRuntime(body);
        runtime.SetMassProfile(mass, ConfiguredInertia(runtime), ConfiguredCenter(runtime));
    }

    /// <summary>Gets the body's configured mass in kilograms.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>Configured positive mass, including while static, kinematic or detached.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public float BodyGetMass(RID body) => ConfiguredMass(MassRuntime(body));

    /// <summary>Sets rotational inertia in kilograms times squared scene units.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="inertia">Zero selects geometry-derived inertia; positive values override it.</param>
    /// <exception cref="ArgumentOutOfRangeException">The inertia is negative, nonfinite or outside the solver range.</exception>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetInertia(RID body, float inertia)
    {
        var runtime = MassRuntime(body);
        runtime.SetMassProfile(ConfiguredMass(runtime), inertia, ConfiguredCenter(runtime));
    }

    /// <summary>Gets configured or most recently resolved rotational inertia in scene units.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>An explicit override or geometry-derived kilograms times squared scene units; zero before an automatic profile is first attached.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public float BodyGetInertia(RID body)
    {
        var runtime = MassRuntime(body);
        var value = ConfiguredInertia(runtime);
        return value > 0 ? value : runtime.MassData.rotationalInertia / PhysicsMass.InertiaScale;
    }

    /// <summary>Sets a custom center of mass relative to body origin in local scene units.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="center">Finite local offset within the solver range.</param>
    /// <remarks>Scene RigidBody switches to Custom mode. The profile is committed before property-list callbacks.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The center or resulting inertia exceeds the finite solver range.</exception>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetCenterOfMass(RID body, Vector2 center)
    {
        var runtime = MassRuntime(body);
        runtime.SetMassProfile(ConfiguredMass(runtime), ConfiguredInertia(runtime), center);
    }

    /// <summary>Gets the configured or most recently resolved local center of mass.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>Local scene-unit center, or zero before automatic geometry is first attached.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public Vector2 BodyGetCenterOfMass(RID body)
    {
        var runtime = MassRuntime(body);
        return ConfiguredCenter(runtime) ?? new Vector2(runtime.MassData.center.X * PhysicsSpace.UnitsPerMeter,
            runtime.MassData.center.Y * PhysicsSpace.UnitsPerMeter);
    }

    /// <summary>Restores automatic center and inertia while retaining configured body mass.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <remarks>Scene RigidBody stored Inertia becomes zero and CenterOfMassMode becomes Auto with a zero stored center.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodyResetMassProperties(RID body)
    {
        var runtime = MassRuntime(body);
        runtime.SetMassProfile(ConfiguredMass(runtime), 0, null);
    }
}
