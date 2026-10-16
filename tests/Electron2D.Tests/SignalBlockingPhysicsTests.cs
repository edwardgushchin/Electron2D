using Electron2D;

internal static class SignalBlockingPhysicsTests
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            using var world = new World(backend);
            using var root = new SubViewport { World = world, PhysicsObjectPicking = true };
            using var circle = new CircleShape { Radius = 1 };
            using var sensor = new CircleShape { Radius = 5 };
            using var material = new PhysicsMaterial();
            var body = new ProbeBody { Position = new(100, 0), InputPickable = true, CanSleep = false, GravityScale = 0, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, PhysicsMaterialOverride = material };
            body.AddChild(new CollisionShape { Shape = circle });
            var area = new Area { Monitoring = true, InputPickable = false };
            area.AddChild(new CollisionShape { Shape = sensor }); root.AddChild(body); root.AddChild(area);
            using var tree = new SceneTree(root);
            Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "The full frame uses the requested backend without fallback");
            var entered = 0; var exited = 0; var shapes = 0; var picked = 0; var changes = 0; var onceCalls = 0;
            area.BodyEntered += other => { Check(ReferenceEquals(other, body), "Area source identity"); entered++; };
            area.BodyExited += _ => exited++;
            area.BodyShapeEntered += (rid, other, otherShape, localShape) => { Check(rid == body.GetRID() && ReferenceEquals(other, body) && otherShape == 0 && localShape == 0, "Actual shape event payload"); shapes++; };
            body.InputEvent += (_, input, index) => { Check(!input.IsDisposed && index == 0, "Borrowed input and logical shape"); picked++; };
            circle.Changed += _ => changes++; material.Changed += _ => changes++;
            using var once = EventConnection.Subscribe<Entity>(h => area.BodyEntered += h, h => area.BodyEntered -= h, _ => onceCalls++, oneShot: true, defer: tree.Defer);
            body.SetBlockSignals(true); area.SetBlockSignals(true); root.SetBlockSignals(true);
            circle.SetBlockSignals(true); material.SetBlockSignals(true);
            Frame(0);
            Check(entered == 0 && shapes == 0 && once.IsConnected && area.GetOverlappingBodies().Length == 1, "A suppressed real overlap still commits membership without consuming a one-shot token");
            using (var input = new InputEventMouseMotion { Position = new(.25f, .25f), GlobalPosition = new(.25f, .25f) })
            {
                root.PushInput(input, true); tree.PhysicsFrame(.01);
                Check(body.InputCalls == 1 && picked == 0, $"The real picking virtual callback runs while its ordinary event is blocked: {body.InputCalls}/{picked}");
            }
            circle.Radius = 2; material.Friction = .25f; material.Bounce = .5f; tree.PhysicsFrame(.01);
            Check(changes == 0 && PhysicsServer.BodyGetFriction(body.GetRID()) == .25f && PhysicsServer.BodyGetBounce(body.GetRID()) == .5f,
                "Blocked shape/material notifications preserve backend material revision polling");
            using (var geometry = (CircleShape)PhysicsServer.ShapeGetData(circle.GetRID()))
                Check(geometry.Radius == 2, "Blocked shape edits update public server geometry");
            Frame(1); Check(exited == 0 && area.GetOverlappingBodies().Length == 0, "Suppressed departures still commit membership");
            area.SetBlockSignals(false); Frame(0);
            Check(entered == 1 && shapes == 1 && onceCalls == 1 && !once.IsConnected, "The next actual transition resumes existing ordinary and deferred one-shot listeners");
            body.SetBlockSignals(false); var beforePick = body.InputCalls;
            using (var input = new InputEventMouseMotion { Position = new(1.5f, .25f), GlobalPosition = new(1.5f, .25f) })
            { root.PushInput(input, true); tree.PhysicsFrame(.01); Check(body.InputCalls > beforePick && picked == 1, $"Picking resumes its existing listener with enlarged geometry: {beforePick}/{body.InputCalls}/{picked}"); }
            root.PhysicsObjectPicking = false;
            for (var i = 0; i < 128; i++) Frame(i);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var before = (entered, exited, body.Ticks, body.Integrations); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) { area.SetBlockSignals(i % 4 < 2); body.SetBlockSignals(i % 4 < 2); Frame(i); }
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && allBytes == 0, "Warm full frames retain strict zero owner/all-thread managed bytes while changing policy");
            Check(entered - before.entered == 32 && exited - before.exited == 32 && body.Ticks - before.Ticks == 128 && body.Integrations - before.Integrations == 128,
                "Every measured step integrates and processes while exactly half the physical transitions publish");
            Console.WriteLine($"Signal blocking {backend}/{(threaded ? "separate" : "owner")}: real overlaps, picking, revisions, virtual callbacks and 128 measured full frames at {ownerBytes}/{allBytes} B passed.");
            void Frame(int i)
            {
                var x = i % 2 == 0 ? 0 : 100; body.Position = new(x, 0); body.LinearVelocity = new(1, 0);
                tree.PhysicsFrame(.01); Check(body.Position.X > x, "Real body integration continues while blocked");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    private sealed class ProbeBody : RigidBody
    {
        internal int Ticks, Integrations, InputCalls;
        protected override void OnPhysicsProcess(double delta) { base.OnPhysicsProcess(delta); Check(delta == .01, "Physical callback delta"); Ticks++; }
        protected override void IntegrateForces(PhysicsDirectBodyState state) { base.IntegrateForces(state); Integrations++; }
        protected override void OnInputEvent(Viewport viewport, InputEvent inputEvent, int shapeIndex) { base.OnInputEvent(viewport, inputEvent, shapeIndex); InputCalls++; }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
