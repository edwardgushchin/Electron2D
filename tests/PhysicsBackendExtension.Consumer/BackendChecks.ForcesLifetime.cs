using Electron2D;

public static partial class BackendChecks
{
    private static void ForceLifetimeChecks(SceneTree tree, World world, RID[] bodies)
    {
        const double delta = 1d / 60;
        foreach (var body in bodies)
        {
            PhysicsServer.BodySetConstantForce(body, Vector2.Zero); PhysicsServer.BodySetConstantTorque(body, 0);
            PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0);
            var view = PhysicsServer.BodyGetDirectState(body)!;
            PhysicsServer.BodySetSpace(body, default);
            Reject<InvalidOperationException>(() => view.ApplyCentralImpulse(new(2, 0)));
            PhysicsServer.BodyApplyCentralImpulse(body, new(2, 0)); PhysicsServer.BodyApplyTorqueImpulse(body, 4);
            PhysicsServer.BodyApplyCentralForce(body, new(12, 0)); PhysicsServer.BodyApplyTorque(body, 8);
            PhysicsServer.BodySetConstantForce(body, new(6, 0)); PhysicsServer.BodySetConstantTorque(body, 4);
        }
        tree.PhysicsFrame(delta);
        foreach (var body in bodies)
        {
            Motion(body, 1, .5f, "Detached inputs retain mass-based impulses without stepping");
            PhysicsServer.BodySetSpace(body, world.Space);
        }
        tree.PhysicsFrame(delta);
        foreach (var body in bodies) Motion(body, 1.15f, .525f, "Reentry applies retained pending and persistent totals once");
        tree.PhysicsFrame(delta);
        foreach (var body in bodies)
        {
            Motion(body, 1.2f, .525f + 1f / 120, "Reentry consumes transient totals but retains persistent ones");
            PhysicsServer.BodySetConstantForce(body, Vector2.Zero); PhysicsServer.BodySetConstantTorque(body, 0);
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyApplyCentralForce(body, new(12, 0)); PhysicsServer.BodyApplyTorque(body, 8);
            PhysicsServer.BodyApplyCentralImpulse(body, new(100, 0)); PhysicsServer.BodyApplyTorqueImpulse(body, 100);
        }
        tree.PhysicsFrame(delta);
        foreach (var body in bodies)
        {
            Motion(body, 0, 0, "Static roles ignore impulses and retain queued forces");
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
        }
        tree.PhysicsFrame(delta);
        foreach (var body in bodies)
        {
            Motion(body, .1f, 1f / 60, "Returning to a dynamic role consumes retained transient force/torque");
            var view = PhysicsServer.BodyGetDirectState(body)!;
            PhysicsServer.BodySetConstantForce(body, new(3, 4)); PhysicsServer.BodySetConstantTorque(body, 5);
            var linear = view.LinearVelocity; var angular = view.AngularVelocity;
            var invalid = new Vector2(float.NaN, 0);
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyApplyCentralForce(body, invalid));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyApplyForce(body, new(1, 0), invalid));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyApplyTorque(body, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyApplyCentralImpulse(body, invalid));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyApplyImpulse(body, new(float.MaxValue, 0), new(0, -float.MaxValue)));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyApplyTorqueImpulse(body, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyAddConstantCentralForce(body, invalid));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyAddConstantForce(body, new(float.MaxValue, 0), new(0, -float.MaxValue)));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyAddConstantTorque(body, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => view.SetConstantForce(invalid));
            Reject<ArgumentOutOfRangeException>(() => view.SetConstantTorque(float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetAxisVelocity(body, invalid));
            Check(view.LinearVelocity == linear && view.AngularVelocity == angular && view.GetConstantForce() == new Vector2(3, 4) && view.GetConstantTorque() == 5,
                "Nonfinite inputs and positioned moment overflow reject before observable partial writes");
            PhysicsServer.BodySetConstantForce(body, Vector2.Zero); PhysicsServer.BodySetConstantTorque(body, 0);
        }
        tree.PhysicsFrame(delta);
        foreach (var body in bodies) Motion(body, .1f, 1f / 60, "Rejected transient force inputs do not leak into the following tick");

        static void Motion(RID body, float linear, float angular, string reason) =>
            Check(PhysicsServer.BodyGetLinearVelocity(body).DistanceTo(new(linear, 0)) < .0001f && MathF.Abs(PhysicsServer.BodyGetAngularVelocity(body) - angular) < .0001f, reason);
    }
}
