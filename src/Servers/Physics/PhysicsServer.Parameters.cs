namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal void BodySetFrictionCore(RID body, float friction) => ParameterRuntime(body).SetMaterialParameter(friction, true);
    internal float BodyGetFrictionCore(RID body) => ParameterRuntime(body).GetFriction();
    internal void BodySetBounceCore(RID body, float bounce) => ParameterRuntime(body).SetMaterialParameter(bounce, false);
    internal float BodyGetBounceCore(RID body) => ParameterRuntime(body).GetBounce();
    internal void BodySetGravityScaleCore(RID body, float scale)
    {
        PhysicsBodyRuntime.Finite(scale); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.GravityScale = scale;
        else
        {
            if (Mathf.IsZeroApprox(runtime.BodyGravityScale)) runtime.Wake();
            runtime.BodyGravityScale = scale;
        }
    }
    internal float BodyGetGravityScaleCore(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.GravityScale : runtime.BodyGravityScale;
    }
    internal void BodySetLinearDampCore(RID body, float damp)
    {
        PhysicsBodyRuntime.Finite(damp); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.LinearDamp = damp; else runtime.BodyLinearDamp = damp;
    }
    internal float BodyGetLinearDampCore(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.LinearDamp : runtime.BodyLinearDamp;
    }
    internal void BodySetAngularDampCore(RID body, float damp)
    {
        PhysicsBodyRuntime.Finite(damp); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.AngularDamp = damp; else runtime.BodyAngularDamp = damp;
    }
    internal float BodyGetAngularDampCore(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.AngularDamp : runtime.BodyAngularDamp;
    }
    internal void BodySetLinearDampModeCore(RID body, RigidBody.DampMode mode)
    {
        ValidateBodyDampMode(mode); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.LinearDampMode = mode; else runtime.BodyLinearDampMode = mode;
    }
    internal RigidBody.DampMode BodyGetLinearDampModeCore(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.LinearDampMode : runtime.BodyLinearDampMode;
    }
    internal void BodySetAngularDampModeCore(RID body, RigidBody.DampMode mode)
    {
        ValidateBodyDampMode(mode); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.AngularDampMode = mode; else runtime.BodyAngularDampMode = mode;
    }
    internal RigidBody.DampMode BodyGetAngularDampModeCore(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.AngularDampMode : runtime.BodyAngularDampMode;
    }

    private static void ValidateBodyDampMode(RigidBody.DampMode mode)
    {
        if (mode is not RigidBody.DampMode.Combine and not RigidBody.DampMode.Replace)
            throw new ArgumentOutOfRangeException(nameof(mode));
    }
}
