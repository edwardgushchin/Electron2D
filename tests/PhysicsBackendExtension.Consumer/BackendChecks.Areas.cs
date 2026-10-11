using Electron2D;

public static partial class BackendChecks
{
    private static void AreaFieldChecks()
    {
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("AreaFields", () => solver = new());
        using var world = new World("AreaFields"); var space = world.Space;
        using var circle = new CircleShape { Radius = 8 };
        using var root = new SubViewport { World = world };
        var sceneArea = new Area
        {
            Gravity = 10,
            GravityDirection = Vector2.Right,
            GravitySpaceOverride = Area.SpaceOverride.Combine,
            LinearDamp = .3f,
            AngularDamp = .3f,
            LinearDampSpaceOverride = Area.SpaceOverride.Combine,
            AngularDampSpaceOverride = Area.SpaceOverride.Combine
        };
        var sceneBody = new RigidBody { CanSleep = false };
        sceneArea.AddChild(new CollisionShape { Shape = circle }); sceneBody.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(sceneArea); root.AddChild(sceneBody);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.AreaCreate(); var enters = 0;
        try
        {
            PhysicsServer.AreaAddShape(raw, circle.GetRID());
            PhysicsServer.AreaSetGravity(raw, 20); PhysicsServer.AreaSetGravityVector(raw, Vector2.Right); PhysicsServer.AreaSetPriority(raw, 10);
            PhysicsServer.AreaSetLinearDamp(raw, .2f); PhysicsServer.AreaSetAngularDamp(raw, .2f);
            PhysicsServer.AreaSetGravity(space, 2); PhysicsServer.AreaSetGravityVector(space, Vector2.Right);
            PhysicsServer.AreaSetLinearDamp(space, .1f); PhysicsServer.AreaSetAngularDamp(space, .1f);
            solver!.IdentityHook = id =>
            {
                if (id == raw || id == sceneArea.GetRID())
                {
                    RejectFieldWrites(id); RejectFieldWrites(space);
                    Check(PhysicsServer.AreaGetGravity(id) == (id == raw ? 20 : 10), "Borrowed Area field getters retain authored values");
                }
            };
            PhysicsServer.AreaSetSpace(raw, space); tree.PhysicsFrame(0); solver.IdentityHook = null;
            PhysicsServer.AreaSetMonitorCallback(raw, (status, _, _, _, _) =>
            {
                if (status != PhysicsServer.AreaBodyStatus.Added) return;
                enters++; PhysicsServer.AreaSetGravity(raw, 21); PhysicsServer.AreaSetGravity(space, 3);
            });
            tree.PhysicsFrame(1d / 60);
            Check(enters == 1 && PhysicsServer.AreaGetGravity(raw) == 21 && PhysicsServer.AreaGetGravity(space) == 3,
                $"Ordinary Area callback edits preserve history: enters={enters}, Area/default gravity={PhysicsServer.AreaGetGravity(raw)}/{PhysicsServer.AreaGetGravity(space)}");
            PhysicsServer.AreaSetGravity(raw, 20); PhysicsServer.AreaSetGravity(space, 2);
            solver.StatusHook = () =>
            {
                var worker = Task.Run(() => { var body = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(body); });
                Check(worker.Wait(TimeSpan.FromSeconds(2)), "Default-field status hooks execute outside the RID registry lock");
            };
            _ = PhysicsServer.AreaGetGravity(space); solver.StatusHook = null;
            var modes = new[] { Area.SpaceOverride.Disabled, Area.SpaceOverride.Combine, Area.SpaceOverride.CombineReplace,
                Area.SpaceOverride.Replace, Area.SpaceOverride.ReplaceCombine };
            var gravity = new[] { 12f, 32f, 20f, 20f, 32f }; var damp = new[] { .4f, .6f, .2f, .2f, .6f };
            for (var i = 0; i < modes.Length; i++)
            {
                PhysicsServer.AreaSetGravitySpaceOverride(raw, modes[i]); PhysicsServer.AreaSetLinearDampSpaceOverride(raw, modes[i]);
                PhysicsServer.AreaSetAngularDampSpaceOverride(raw, modes[i]); tree.PhysicsFrame(1d / 60);
                var view = PhysicsServer.BodyGetDirectState(sceneBody.GetRID())!;
                Check(view.TotalGravity.IsEqualApprox(new(gravity[i], 0)) && MathF.Abs(view.TotalLinearDamp - damp[i]) < .00001f &&
                    MathF.Abs(view.TotalAngularDamp - damp[i]) < .00001f && enters == 1,
                    "All Area override modes reach independent resolved gravity/damping and retain monitor episodes");
            }
            sceneArea.Priority = 20; tree.PhysicsFrame(1d / 60);
            Check(PhysicsServer.BodyGetDirectState(sceneBody.GetRID())!.TotalGravity.IsEqualApprox(new(22, 0)),
                "Live priority reorders replacement ahead of lower-priority accumulation");
            sceneArea.Priority = 0;
            PhysicsServer.AreaSetGravitySpaceOverride(raw, Area.SpaceOverride.Replace);
            PhysicsServer.AreaSetLinearDamp(raw, 0); PhysicsServer.AreaSetAngularDamp(raw, 0);
            PhysicsServer.AreaSetLinearDampSpaceOverride(raw, Area.SpaceOverride.Replace); PhysicsServer.AreaSetAngularDampSpaceOverride(raw, Area.SpaceOverride.Replace);
            PhysicsServer.AreaSetGravityPoint(raw, true); PhysicsServer.AreaSetGravityVector(raw, new(4, 0)); PhysicsServer.AreaSetGravityPointUnitDistance(raw, 2);
            sceneBody.Position = default; sceneBody.LinearVelocity = default; tree.PhysicsFrame(1d / 60);
            Check(solver.Bodies[raw].AreaFields == new PhysicsAreaFieldConfiguration(new(4, 0), 20, true, 2, 0, 0,
                Area.SpaceOverride.Replace, Area.SpaceOverride.Replace, Area.SpaceOverride.Replace, 10),
                "Every authored field component reaches the selected immutable configuration");
            Check(PhysicsServer.BodyGetDirectState(sceneBody.GetRID())!.TotalGravity.IsEqualApprox(new(5, 0)),
                "Point gravity uses transformed center and inverse-square unit distance");
            sceneBody.LinearVelocity = default; tree.PhysicsFrame(.1);
            Check(sceneBody.LinearVelocity.X > .49f && sceneBody.LinearVelocity.X < .51f && sceneBody.Position.X > 0,
                "Independent registered Area fields produce actual body acceleration");
            sceneBody.Position = default; sceneBody.LinearVelocity = default;
            PhysicsServer.AreaSetTransform(raw, new(MathF.PI / 2, Vector2.Zero)); tree.PhysicsFrame(1d / 60);
            Check(PhysicsServer.BodyGetDirectState(sceneBody.GetRID())!.TotalGravity.IsEqualApprox(new(0, 5)),
                "Point centers follow the Area pose while directional vectors remain unrotated");
            PhysicsServer.AreaSetGravityPoint(raw, false); PhysicsServer.AreaSetGravityVector(raw, Vector2.Right);
            PhysicsServer.AreaSetGravityVector(raw, new(2, 0)); tree.PhysicsFrame(1d / 60);
            Check(PhysicsServer.BodyGetDirectState(sceneBody.GetRID())!.TotalGravity.IsEqualApprox(new(40, 0)),
                "Directional gravity preserves vector magnitude");
            PhysicsServer.AreaSetGravityVector(raw, Vector2.Right); PhysicsServer.AreaSetCollisionMask(raw, 0); tree.PhysicsFrame(1d / 60);
            Check(PhysicsServer.BodyGetDirectState(sceneBody.GetRID())!.TotalGravity.IsEqualApprox(new(12, 0)),
                "Area masks filter independent field participation");
            PhysicsServer.AreaSetCollisionMask(raw, 1); PhysicsServer.AreaSetMonitorCallback(raw, null);
            PhysicsServer.AreaSetMonitorable(raw, false); tree.PhysicsFrame(1d / 60);
            Check(PhysicsServer.BodyGetDirectState(sceneBody.GetRID())!.TotalGravity.IsEqualApprox(new(20, 0)),
                "Area fields remain active without monitor callbacks or monitorability");
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) EditAndStep(i);
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) EditAndStep(i);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Registered Area edits/full frames allocated {own}/{all} owner/all-thread B");
            Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetGravity(raw, 9));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetGravity(space, 9));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaGetGravity(sceneArea.GetRID()));
            }).GetAwaiter().GetResult();
            Console.WriteLine($"Registered Area/default fields: all override modes, point gravity, callbacks and 64 warm full frames {own}/{all} owner/all-thread B");
        }
        finally { solver!.IdentityHook = null; solver.StatusHook = null; PhysicsServer.FreeRID(raw); }

        void EditAndStep(int i)
        {
            sceneBody.Position = default; sceneBody.LinearVelocity = default;
            var g = 20f + (i & 1); PhysicsServer.AreaSetGravity(raw, g); tree.PhysicsFrame(1d / 60);
            Check(MathF.Abs(sceneBody.LinearVelocity.X - g / 60) < .00001f, "Warm Area edits preserve real resolved acceleration");
        }
        void RejectFieldWrites(RID id)
        {
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetGravity(id, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetGravityVector(id, Vector2.Left));
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetGravitySpaceOverride(id, Area.SpaceOverride.Replace));
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetGravityPoint(id, true));
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetGravityPointUnitDistance(id, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetLinearDamp(id, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetAngularDamp(id, 9));
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetLinearDampSpaceOverride(id, Area.SpaceOverride.Replace));
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetAngularDampSpaceOverride(id, Area.SpaceOverride.Replace));
            Reject<InvalidOperationException>(() => PhysicsServer.AreaSetPriority(id, 9));
        }
    }
}
