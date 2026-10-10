namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    internal float? FrictionOverride;
    internal float? BounceOverride;
    private float _bodyGravityScale = 1;
    internal float BodyGravityScale { get => _bodyGravityScale; set { GPUParametersDirty |= _bodyGravityScale != value; _bodyGravityScale = value; } }
    private float _bodyLinearDamp;
    internal float BodyLinearDamp { get => _bodyLinearDamp; set { GPUParametersDirty |= _bodyLinearDamp != value; _bodyLinearDamp = value; } }
    private float _bodyAngularDamp;
    internal float BodyAngularDamp { get => _bodyAngularDamp; set { GPUParametersDirty |= _bodyAngularDamp != value; _bodyAngularDamp = value; } }
    private RigidBody.DampMode _bodyLinearDampMode;
    internal RigidBody.DampMode BodyLinearDampMode { get => _bodyLinearDampMode; set { GPUParametersDirty |= _bodyLinearDampMode != value; _bodyLinearDampMode = value; } }
    private RigidBody.DampMode _bodyAngularDampMode;
    internal RigidBody.DampMode BodyAngularDampMode { get => _bodyAngularDampMode; set { GPUParametersDirty |= _bodyAngularDampMode != value; _bodyAngularDampMode = value; } }

    internal float GetFriction() => FrictionOverride ?? Owners.Scene?.MaterialOverride?.ComputedFriction ?? 1;
    internal float GetBounce() => BounceOverride ?? Owners.Scene?.MaterialOverride?.ComputedBounce ?? 0;
    internal void ResetMaterialOverrides() { FrictionOverride = null; BounceOverride = null; }

    internal void SetMaterialParameter(float value, bool friction)
    {
        Finite(value);
        if (friction) FrictionOverride = value; else BounceOverride = value;
        var owners = Owners;
        if (owners.Scene is { } scene) scene.MarkShapesDirty(); else owners.Server!.MarkShapesDirty();
    }

}
