namespace Electron2D;

public sealed partial class PhysicsServer
{
    private PhysicsBodyRuntime ParameterRuntime(RID body, bool writing = true)
    {
        ThrowIfDisposed();
        var runtime = BodyRuntime(body);
        if (writing) runtime.EnsureAccess(writing: true); else runtime.EnsureAuthoringReadAccess();
        if (writing) PrepareParameters(runtime);
        return runtime;
    }

    private static void PrepareParameters(PhysicsBodyRuntime runtime)
    {
        var space = runtime.Space;
        if (space is null) return;
        space.EnsureImplementationWriteAccess();
        var owners = runtime.Owners;
        if (owners.Scene is { } scene) scene.PrepareBackend(); else owners.Server!.PrepareBackend();
    }

    private static float ConfiguredMass(PhysicsBodyRuntime runtime) => runtime.Owners.Scene is RigidBody rigid ? rigid.Mass : runtime.Mass;
    private static float ConfiguredInertia(PhysicsBodyRuntime runtime) => runtime.Owners.Scene is RigidBody rigid ? rigid.Inertia : runtime.Inertia;
    private static Vector2? ConfiguredCenter(PhysicsBodyRuntime runtime) => runtime.Owners.Scene is RigidBody rigid ? rigid.CustomMassCenter : runtime.CustomCenter;

    internal void BodySetMassCore(RID body, float mass)
    {
        var runtime = ParameterRuntime(body);
        runtime.SetMassProfile(mass, ConfiguredInertia(runtime), ConfiguredCenter(runtime));
    }

    internal float BodyGetMassCore(RID body) => ConfiguredMass(ParameterRuntime(body, writing: false));

    internal void BodySetInertiaCore(RID body, float inertia)
    {
        var runtime = ParameterRuntime(body);
        runtime.SetMassProfile(ConfiguredMass(runtime), inertia, ConfiguredCenter(runtime));
    }

    internal float BodyGetInertiaCore(RID body)
    {
        var runtime = ParameterRuntime(body, writing: false);
        var value = ConfiguredInertia(runtime);
        if (value > 0) return value;
        PrepareParameters(runtime);
        return runtime.MassProperties.Inertia;
    }

    internal void BodySetCenterOfMassCore(RID body, Vector2 center)
    {
        var runtime = ParameterRuntime(body);
        runtime.SetMassProfile(ConfiguredMass(runtime), ConfiguredInertia(runtime), center);
    }

    internal Vector2 BodyGetCenterOfMassCore(RID body)
    {
        var runtime = ParameterRuntime(body, writing: false);
        if (ConfiguredCenter(runtime) is { } center) return center;
        PrepareParameters(runtime);
        return runtime.MassProperties.Center;
    }

    internal void BodyResetMassPropertiesCore(RID body)
    {
        var runtime = ParameterRuntime(body);
        runtime.SetMassProfile(ConfiguredMass(runtime), 0, null);
    }
}
