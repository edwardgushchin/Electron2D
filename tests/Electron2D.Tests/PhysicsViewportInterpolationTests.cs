using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsViewportInterpolationTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var root = new Node { PhysicsInterpolationMode = PhysicsInterpolationMode.Off };
        var viewport = new SubViewport { World = world, Size = new(100, 80) };
        var body = new RigidBody
        {
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(600, 0),
            LinearDamp = 0,
            LinearDampMode = RigidBody.DampMode.Replace
        };
        using var shape = new CircleShape { Radius = 1 };
        body.AddChild(new CollisionShape { Shape = shape }); viewport.AddChild(body); root.AddChild(viewport);
        var camera = new Camera { AnchorMode = AnchorMode.FixedTopLeft, LimitEnabled = false };
        viewport.AddChild(camera);
        using var tree = new SceneTree(root) { PhysicsInterpolation = true };
        tree.PhysicsFrameStarted += _ => camera.Position += new Vector2(10, 0);
        tree.PhysicsFrame(1d / 60); var before = body.Position; tree.PhysicsFrame(1d / 60);
        Verify(true, before);
        Check(world.PhysicsBackend == backend && world.RequestedPhysicsBackend == backend && world.PhysicsFallbackReason is null,
            "Viewport interpolation exercises the explicit requested numerical backend without fallback");
        Check(viewport.PhysicsInterpolationMode == PhysicsInterpolationMode.On && !root.IsPhysicsInterpolated() && body.IsPhysicsInterpolatedAndEnabled(),
            "A default nested viewport creates an enabled interpolation boundary under an Off parent");
        Check(MathF.Abs(viewport.GetInterpolatedCanvasTransform(.5f).Origin.X + 15) < .001f && MathF.Abs(viewport.CanvasTransform.Origin.X + 20) < .001f,
            "The nested viewport interpolates camera presentation while logical canvas coordinates stay current");
        body.ResetPhysicsInterpolation();
        Check(body.GetInterpolatedVisualTransform(.5f).Origin == body.GlobalPosition, "Explicit body reset clears historical presentation at the boundary");
        camera.ResetPhysicsInterpolation();
        Check(viewport.GetInterpolatedCanvasTransform(.5f) == viewport.CanvasTransform, "Explicit camera reset clears viewport history");
        var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        long own = 0, all = 0; var measuring = false;
        for (var i = 0; i < 128; i++) Cycle();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false); GC.WaitForPendingFinalizers();
        measuring = true;
        for (var i = 0; i < 128; i++) Cycle();
        measuring = false;
        Check(own == 0 && all == 0, $"Viewport policy/full-step presentation allocated {own}/{all} B");
        tree.PhysicsInterpolation = false; Step(false); tree.PhysicsInterpolation = true; Step(true);
        Check(body.GlobalPosition.DistanceTo(state.Transform.Origin) < .001f && body.LinearVelocity.DistanceTo(new(600, 0)) < .001f,
            "Presentation policy preserves selected physical pose and velocity");
        DefaultsAndStorage();
        Console.WriteLine($"Viewport interpolation on {backend}: nested boundary, explicit policy, camera/reset, packing/revert and 128 cycles/384 frames {own}/{all} B passed.");

        void Cycle()
        {
            viewport.PhysicsInterpolationMode = PhysicsInterpolationMode.Inherit; Step(false);
            viewport.PhysicsInterpolationMode = PhysicsInterpolationMode.Off; Step(false);
            viewport.PhysicsInterpolationMode = PhysicsInterpolationMode.On; Step(true);
        }
        void Step(bool interpolated)
        {
            var previous = body.Position;
            var startOwn = measuring ? GC.GetAllocatedBytesForCurrentThread() : 0;
            var startAll = measuring ? GC.GetTotalAllocatedBytes(true) : 0;
            tree.PhysicsFrame(1d / 60); Verify(interpolated, previous);
            if (measuring) { own += GC.GetAllocatedBytesForCurrentThread() - startOwn; all += GC.GetTotalAllocatedBytes(true) - startAll; }
        }
        void Verify(bool interpolated, Vector2 previous)
        {
            var current = body.Position; var expected = interpolated ? previous.Lerp(current, .5f) : current;
            Check(current.X - previous.X > 9.9f && body.GetInterpolatedVisualTransform(.5f).Origin.DistanceTo(expected) < .001f,
                "Selected physical motion has the viewport policy's interpolated presentation under an Off parent");
        }
    }

    private static void DefaultsAndStorage()
    {
        using var ordinary = new Node(); using var control = new Control();
        Check(ordinary.PhysicsInterpolationMode == PhysicsInterpolationMode.Inherit && control.PhysicsInterpolationMode == PhysicsInterpolationMode.Off,
            "Ordinary Node and Control keep their distinct defaults");
        using var window = new Window(); using var sub = new SubViewport();
        foreach (var viewport in new Viewport[] { window, sub })
        {
            var property = viewport.GetPropertyList().Single(p => p.Name == nameof(Node.PhysicsInterpolationMode));
            Check(viewport.PhysicsInterpolationMode == PhysicsInterpolationMode.On && !viewport.PropertyCanRevert(property),
                "Both viewport constructors and descriptor defaults agree on On");
            using var packed = new PackedScene(); packed.Pack(viewport);
            Check(ReadMode(packed.GetState()) == PhysicsInterpolationMode.On, "Stored viewport policy agrees with its executable default");
            using (var copy = packed.Instantiate()) Check(copy.GetType() == viewport.GetType() && copy.PhysicsInterpolationMode == PhysicsInterpolationMode.On,
                "A packed viewport reconstructs On in its original type");
            foreach (var mode in new[] { PhysicsInterpolationMode.Inherit, PhysicsInterpolationMode.Off })
            {
                viewport.PhysicsInterpolationMode = mode; packed.Pack(viewport);
                Check(ReadMode(packed.GetState()) == mode, "Explicit viewport modes remain stored overrides");
                using var copy = packed.Instantiate(); Check(copy.PhysicsInterpolationMode == mode, "Packed explicit viewport modes survive construction defaults");
                using var duplicated = (PackedScene)packed.Duplicate(); using var duplicateInstance = duplicated.Instantiate();
                Check(duplicateInstance.PhysicsInterpolationMode == mode, "Duplicated packed state preserves the explicit viewport policy");
                using var copied = new PackedScene(); copied.CopyFromResource(packed); using var copiedInstance = copied.Instantiate();
                Check(copiedInstance.PhysicsInterpolationMode == mode, "Copied packed state preserves the explicit viewport policy");
                Check(viewport.PropertyCanRevert(property), "An explicit viewport override can revert");
                viewport.RevertProperty(property); Check(viewport.PhysicsInterpolationMode == PhysicsInterpolationMode.On,
                    "Viewport metadata reverts to its executable On default");
            }
        }
        static PhysicsInterpolationMode ReadMode(SceneState state)
        {
            for (var i = 0; i < state.GetNodePropertyCount(0); i++)
                if (state.GetNodePropertyName(0, i) == nameof(Node.PhysicsInterpolationMode)) return state.GetNodePropertyValue<PhysicsInterpolationMode>(0, i);
            throw new InvalidOperationException("The stored viewport policy is absent.");
        }
    }
}
