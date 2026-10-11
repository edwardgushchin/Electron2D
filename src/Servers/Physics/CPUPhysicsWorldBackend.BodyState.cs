namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private bool PrepareBodyStates(double delta)
    {
        Space.LastStep = (float)delta;
        Space.ClearCallbackBodies();
        var hasKinematicBodies = false;
        var captureCallbacks = false;
        var uniform = _fieldAreas.Count == 0 && !Space.DefaultAreaFields.GravityPoint;
        var gravity = Space.DefaultAreaFields.GravityVector * Space.DefaultAreaFields.Gravity;
        var linearDamp = Space.DefaultAreaFields.LinearDamp;
        var angularDamp = Space.DefaultAreaFields.AngularDamp;
        foreach (var body in Space.Bodies)
        {
            var runtime = body.Runtime;
            if (body is RigidBody or CharacterBody || body.HasServerMode)
            {
                if (!uniform) ResolveAreaFields(body.CollisionLayer, NativeCollider(body.Backend).Shapes, body.GlobalPosition, out gravity, out linearDamp, out angularDamp);
                ApplyBodyFields(runtime, gravity, linearDamp, angularDamp, delta);
                if (body is CharacterBody character) character.SetResolvedGravity(runtime.Gravity);
            }
            runtime.ApplyBeforeStep(body);
            captureCallbacks |= PhysicsSpace.RequiresBodySnapshot(runtime, body);
            if (body.HasServerMode) body.PrepareServerMotion(delta);
            else if (body is AnimatableBody animatable) animatable.PrepareMotion(delta);
            else if (body is CharacterBody character) character.PrepareMotion(delta);
            else if (body is RigidBody rigid) rigid.PrepareFrozenMotion(delta);
            hasKinematicBodies |= body.EffectiveBodyMode == PhysicsServer.BodyMode.Kinematic;
        }
        foreach (var body in Space.ServerColliders)
        {
            if (body.IsArea) continue;
            var runtime = body.Runtime;
            if (body.Mode != PhysicsServer.BodyMode.Static)
            {
                if (!uniform) ResolveAreaFields(body.CollisionLayer, NativeCollider(body.Backend).Shapes, body.GetTransform().Origin, out gravity, out linearDamp, out angularDamp);
                ApplyBodyFields(runtime, gravity, linearDamp, angularDamp, delta);
            }
            runtime.ApplyBeforeStep(null);
            body.PrepareMotion(delta);
            captureCallbacks |= PhysicsSpace.RequiresBodySnapshot(runtime, null);
            hasKinematicBodies |= body.Mode == PhysicsServer.BodyMode.Kinematic;
        }
        if (captureCallbacks) Space.PrepareCallbackBodies();
        return hasKinematicBodies;
    }

    private void ApplyBodyFields(PhysicsBodyRuntime runtime, Vector2 gravity, float linearDamp, float angularDamp, double delta)
    {
        var fields = NativeCollider(runtime.Backend).ApplyResolvedFields(runtime.CaptureIntegrationConfiguration(),
            new(runtime.Gravity, runtime.LinearDamp, runtime.AngularDamp, runtime.FieldsInitialized),
            gravity, linearDamp, angularDamp, Space.DefaultGravity, delta);
        runtime.Gravity = fields.Gravity; runtime.LinearDamp = fields.LinearDamp; runtime.AngularDamp = fields.AngularDamp;
        runtime.FieldsInitialized = fields.Initialized;
    }

}
