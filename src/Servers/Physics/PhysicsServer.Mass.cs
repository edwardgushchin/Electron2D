namespace Electron2D;

public sealed partial class PhysicsServer
{
    private PhysicsBodyRuntime ParameterRuntime(RID body)
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

    internal void BodySetMassCore(RID body, float mass)
    {
        var runtime = ParameterRuntime(body);
        runtime.SetMassProfile(mass, ConfiguredInertia(runtime), ConfiguredCenter(runtime));
    }

    internal float BodyGetMassCore(RID body) => ConfiguredMass(ParameterRuntime(body));

    internal void BodySetInertiaCore(RID body, float inertia)
    {
        var runtime = ParameterRuntime(body);
        runtime.SetMassProfile(ConfiguredMass(runtime), inertia, ConfiguredCenter(runtime));
    }

    internal float BodyGetInertiaCore(RID body)
    {
        var runtime = ParameterRuntime(body);
        var value = ConfiguredInertia(runtime);
        return value > 0 ? value : runtime.MassData.rotationalInertia / PhysicsMass.InertiaScale;
    }

    internal void BodySetCenterOfMassCore(RID body, Vector2 center)
    {
        var runtime = ParameterRuntime(body);
        runtime.SetMassProfile(ConfiguredMass(runtime), ConfiguredInertia(runtime), center);
    }

    internal Vector2 BodyGetCenterOfMassCore(RID body)
    {
        var runtime = ParameterRuntime(body);
        return ConfiguredCenter(runtime) ?? new Vector2(runtime.MassData.center.X * PhysicsSpace.UnitsPerMeter,
            runtime.MassData.center.Y * PhysicsSpace.UnitsPerMeter);
    }

    internal void BodyResetMassPropertiesCore(RID body)
    {
        var runtime = ParameterRuntime(body);
        runtime.SetMassProfile(ConfiguredMass(runtime), 0, null);
    }
}
