using Electron2D;

public static partial class BackendChecks
{
    private static void AxisVelocityInactive(SubViewport root)
    {
        using (var detached = new RigidBody())
        {
            detached.Sleeping = true; detached.SetAxisVelocity(Vector2.Zero);
            Check(!detached.Sleeping && detached.LinearVelocity == Vector2.Zero, "Detached scene zero-axis writes wake retained dynamic state");
            foreach (var scene in new[] { false, true })
            {
                detached.LinearVelocity = new(float.MaxValue / 4, float.MaxValue / 4);
                Write(new(float.MaxValue / 4, -float.MaxValue / 4));
                Check(detached.LinearVelocity == new Vector2(float.MaxValue / 2, 0), "Double axis normalization preserves representable large diagonal output");
                detached.LinearVelocity = new(float.MaxValue, float.MaxValue);
                Reject<ArgumentOutOfRangeException>(() => Write(new(float.MaxValue, -float.MaxValue)));
                Check(detached.LinearVelocity == new Vector2(float.MaxValue, float.MaxValue), "Overflow rejection retains both original velocity channels");
                detached.LinearVelocity = new(10, -2); Write(new(float.Epsilon, 0));
                Check(detached.LinearVelocity == new Vector2(float.Epsilon, -2), "The smallest positive axis remains a valid direction");
                void Write(Vector2 axis) { if (scene) detached.SetAxisVelocity(axis); else PhysicsServer.BodySetAxisVelocity(detached.GetRID(), axis); }
            }
        }
        for (var role = 0; role < 4; role++)
            foreach (var scene in new[] { false, true })
            {
                using RigidBody body = role == 3 ? new PhysicalBone() : new RigidBody();
                if (role < 2) { body.FreezeMode = role == 0 ? RigidFreezeMode.Static : RigidFreezeMode.Kinematic; body.Freeze = true; }
                if (role == 2) { body.DisableMode = CollisionDisableMode.MakeStatic; body.ProcessMode = ProcessMode.Disabled; }
                root.AddChild(body);
                body.LinearVelocity = new(3, 4);
                if (scene) body.SetAxisVelocity(new(5, 0)); else PhysicsServer.BodySetAxisVelocity(body.GetRID(), new(5, 0));
                Check(body.LinearVelocity == new Vector2(5, 4), "Inactive native scene/server axis edits preserve configured future perpendicular velocity");
                if (scene) body.SetAxisVelocity(Vector2.Zero); else PhysicsServer.BodySetAxisVelocity(body.GetRID(), Vector2.Zero);
                Check(body.LinearVelocity == new Vector2(5, 4), "Inactive zero-axis edits preserve the complete future velocity");
                if (role < 3)
                {
                    if (role < 2) body.Freeze = false; else body.ProcessMode = ProcessMode.Inherit;
                    Check(PhysicsServer.BodyGetLinearVelocity(body.GetRID()).DistanceTo(new(5, 4)) < .0001f, "Reactivation publishes the retained projected velocity");
                }
                root.RemoveChild(body);
            }
    }
}
