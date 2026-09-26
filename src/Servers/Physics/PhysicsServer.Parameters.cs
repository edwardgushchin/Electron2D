namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Sets signed friction; negative values project rough-surface precedence.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="friction">Finite signed coefficient; default one.</param>
    /// <remarks>A scene override belongs to this body and does not mutate a borrowed PhysicsMaterial.
    /// Material assignment/revision reload replaces this raw override.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetFriction(RID body, float friction) => ParameterRuntime(body).SetMaterialParameter(friction, true);
    /// <summary>Gets effective signed friction, including a scene material projection.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed coefficient; default one.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public float BodyGetFriction(RID body) => ParameterRuntime(body).GetFriction();
    /// <summary>Sets signed restitution; negative values project absorbent-surface subtraction.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="bounce">Finite signed coefficient; default zero.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetBounce(RID body, float bounce) => ParameterRuntime(body).SetMaterialParameter(bounce, false);
    /// <summary>Gets effective signed restitution, including the scene material projection.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed coefficient; default zero.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public float BodyGetBounce(RID body) => ParameterRuntime(body).GetBounce();
    /// <summary>Sets the signed multiplier of resolved Area/world gravity.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="scale">Finite signed scale; default one.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetGravityScale(RID body, float scale)
    {
        PhysicsBodyRuntime.Finite(scale); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.GravityScale = scale;
        else
        {
            if (Mathf.IsZeroApprox(runtime.BodyGravityScale)) runtime.Wake();
            runtime.BodyGravityScale = scale;
        }
    }
    /// <summary>Gets the body's configured gravity multiplier.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed multiplier; default one.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public float BodyGetGravityScale(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.GravityScale : runtime.BodyGravityScale;
    }
    /// <summary>Sets signed linear damping in inverse seconds.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="damp">Finite signed damping; default zero.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetLinearDamp(RID body, float damp)
    {
        PhysicsBodyRuntime.Finite(damp); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.LinearDamp = damp; else runtime.BodyLinearDamp = damp;
    }
    /// <summary>Gets configured linear damping in inverse seconds.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed value; default zero.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public float BodyGetLinearDamp(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.LinearDamp : runtime.BodyLinearDamp;
    }
    /// <summary>Sets signed angular damping in inverse seconds.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="damp">Finite signed damping; default zero.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetAngularDamp(RID body, float damp)
    {
        PhysicsBodyRuntime.Finite(damp); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.AngularDamp = damp; else runtime.BodyAngularDamp = damp;
    }
    /// <summary>Gets configured angular damping in inverse seconds.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed value; default zero.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public float BodyGetAngularDamp(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.AngularDamp : runtime.BodyAngularDamp;
    }
    /// <summary>Chooses whether body linear damping combines with or replaces selected fields.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="mode">Combine/default or Replace.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetLinearDampMode(RID body, RigidBody.DampMode mode)
    {
        ValidateBodyDampMode(mode); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.LinearDampMode = mode; else runtime.BodyLinearDampMode = mode;
    }
    /// <summary>Gets the linear damping combination policy.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Combine/default or Replace.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public RigidBody.DampMode BodyGetLinearDampMode(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.LinearDampMode : runtime.BodyLinearDampMode;
    }
    /// <summary>Chooses whether body angular damping combines with or replaces selected fields.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="mode">Combine/default or Replace.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void BodySetAngularDampMode(RID body, RigidBody.DampMode mode)
    {
        ValidateBodyDampMode(mode); var runtime = ParameterRuntime(body);
        if (runtime.Owners.Scene is RigidBody rigid) rigid.AngularDampMode = mode; else runtime.BodyAngularDampMode = mode;
    }
    /// <summary>Gets the angular damping combination policy.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Combine/default or Replace.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public RigidBody.DampMode BodyGetAngularDampMode(RID body)
    {
        var runtime = ParameterRuntime(body); return runtime.Owners.Scene is RigidBody rigid ? rigid.AngularDampMode : runtime.BodyAngularDampMode;
    }

    private static void ValidateBodyDampMode(RigidBody.DampMode mode)
    {
        if (mode is not RigidBody.DampMode.Combine and not RigidBody.DampMode.Replace)
            throw new ArgumentOutOfRangeException(nameof(mode));
    }
}
