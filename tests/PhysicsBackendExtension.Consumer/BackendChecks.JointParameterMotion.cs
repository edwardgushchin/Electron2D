using Electron2D;

public static partial class BackendChecks
{
    private static void JointParameterMotion(SceneTree tree, RigidBody scene, RID raw, RID[] all, RID spring)
    {
        foreach (var id in all) PhysicsServer.JointClear(id);
        foreach (var body in new[] { scene.GetRID(), raw })
        {
            PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetGravityScale(body, 0);
            PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
            PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(body, 0);
            PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0);
        }
        PhysicsServer.BodySetTransform(scene.GetRID(), new(0, Vector2.Zero)); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(100, 0)));
        PhysicsServer.JointMakeDampedSpring(spring, Vector2.Zero, new(100, 0), scene.GetRID(), raw);
        PhysicsServer.DampedSpringJointSetRestLength(spring, 0); PhysicsServer.DampedSpringJointSetStiffness(spring, .12f);
        PhysicsServer.DampedSpringJointSetDamping(spring, 0); PhysicsServer.JointSetMaxForce(spring, 10000);
        tree.PhysicsFrame(1d / 60); Motion(.1f, "Selected spring stiffness, literal rest and authored mass produce real response");
        PhysicsServer.DampedSpringJointSetStiffness(spring, .24f); PhysicsServer.JointSetMaxForce(spring, 6);
        tree.PhysicsFrame(1d / 60); Motion(.15f, "Live general force cap bounds the spring's complete impulse");
        PhysicsServer.DampedSpringJointSetDamping(spring, 120);
        tree.PhysicsFrame(1d / 60); Motion(.1f, "Live damping opposes closing velocity and remains force capped");
        PhysicsServer.DampedSpringJointSetDamping(spring, 0); PhysicsServer.DampedSpringJointSetRestLength(spring, 200);
        tree.PhysicsFrame(1d / 60); Motion(.05f, "Live rest length changes the force direction");
        var callbacks = 0;
        PhysicsServer.BodySetStateSyncCallback(raw, _ => { callbacks++; PhysicsServer.JointSetBias(spring, .3f); PhysicsServer.DampedSpringJointSetStiffness(spring, 0); });
        try { tree.PhysicsFrame(1d / 60); }
        finally { PhysicsServer.BodySetStateSyncCallback(raw, null); }
        Check(callbacks == 1 && PhysicsServer.JointGetBias(spring) == .3f && PhysicsServer.DampedSpringJointGetStiffness(spring) == 0,
            "Ordinary game-state callbacks may author the next interval's joint profile");
        PhysicsServer.JointClear(spring);
        foreach (var body in new[] { scene.GetRID(), raw }) { PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0); }
        PhysicsServer.BodySetTransform(scene.GetRID(), new(0, Vector2.Zero)); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(100, 0)));

        void Motion(float expected, string reason) => Check(PhysicsServer.BodyGetLinearVelocity(scene.GetRID()).DistanceTo(new(expected, 0)) < .001f &&
            PhysicsServer.BodyGetLinearVelocity(raw).DistanceTo(new(-expected, 0)) < .001f, reason);
    }
}
