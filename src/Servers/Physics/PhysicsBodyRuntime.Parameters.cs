using static Box2D.NET.B2Bodies;

namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    internal float? FrictionOverride;
    internal float? BounceOverride;
    internal float BodyGravityScale = 1;
    internal float BodyLinearDamp;
    internal float BodyAngularDamp;
    internal RigidBody.DampMode BodyLinearDampMode;
    internal RigidBody.DampMode BodyAngularDampMode;

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

    internal void ApplyResolvedFields(Vector2 gravity, float linearDamp, float angularDamp, Vector2 defaultGravity, double delta)
    {
        var scaledGravity = gravity * BodyGravityScale;
        var resolvedLinear = BodyLinearDampMode == RigidBody.DampMode.Replace ? BodyLinearDamp : linearDamp + BodyLinearDamp;
        var resolvedAngular = BodyAngularDampMode == RigidBody.DampMode.Replace ? BodyAngularDamp : angularDamp + BodyAngularDamp;
        var linearFactor = MathF.Max(0, 1 - (float)delta * resolvedLinear);
        var angularFactor = MathF.Max(0, 1 - (float)delta * resolvedAngular);
        var acceleration = scaledGravity - defaultGravity * BodyGravityScale;
        if (!scaledGravity.IsFinite() || !acceleration.IsFinite() || !float.IsFinite(resolvedLinear) ||
            !float.IsFinite(resolvedAngular) || !float.IsFinite(linearFactor) || !float.IsFinite(angularFactor))
            throw new InvalidOperationException("The resolved body field exceeds the finite simulation range.");
        var id = BodyID;
        var changed = FieldsInitialized && (Gravity != scaledGravity || LinearDamp != resolvedLinear || AngularDamp != resolvedAngular);
        var active = b2Body_GetType(id) == Box2D.NET.B2BodyType.b2_dynamicBody && !Omitted && (changed || b2Body_IsAwake(id));
        var linear = default(Box2D.NET.B2Vec2); var angular = 0f; var force = default(Box2D.NET.B2Vec2);
        if (active)
        {
            linear = b2Body_GetLinearVelocity(id) * linearFactor;
            angular = b2Body_GetAngularVelocity(id) * angularFactor;
            force = Shape.ToBackend(acceleration) * b2Body_GetMass(id);
            if (!float.IsFinite(linear.X) || !float.IsFinite(linear.Y) || !float.IsFinite(angular) ||
                !float.IsFinite(force.X) || !float.IsFinite(force.Y))
                throw new InvalidOperationException("The resolved body field would produce nonfinite motion.");
        }
        Gravity = scaledGravity; LinearDamp = resolvedLinear; AngularDamp = resolvedAngular; FieldsInitialized = true;
        if (changed && b2Body_GetType(id) == Box2D.NET.B2BodyType.b2_dynamicBody) b2Body_SetAwake(id, true);
        if (!active) return;
        if (linearFactor != 1) b2Body_SetLinearVelocity(id, linear);
        if (angularFactor != 1) b2Body_SetAngularVelocity(id, angular);
        if (force.X != 0 || force.Y != 0) b2Body_ApplyForceToCenter(id, force, false);
    }
}
