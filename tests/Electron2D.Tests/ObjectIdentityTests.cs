using Electron2D;

internal static class ObjectIdentityTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread,
            Environment.GetEnvironmentVariable("ELECTRON2D_TEST_THREADED_COLLIDER") == "1");
        using var generic = new GenericProbe<int>();
        Check(generic.IsClass(generic.ClassName) && generic.IsClass(nameof(ElectronObject)), "Constructed class names retain their engine base.");
        Check(!generic.IsClass(nameof(IDisposable)) && !generic.IsClass(nameof(Object)), "Interfaces and the CLR root are not engine classes.");
        Reject<ArgumentNullException>(() => generic.IsClass(null!));
        Check(!generic.IsClass("") && !generic.IsClass(" ") && !generic.IsClass(generic.GetType().FullName!), "Empty and qualified names do not match.");
        using var custom = new CustomString();
        Check(((object)custom).ToString() == "custom object", "The diagnostic customization uses ordinary C# virtual dispatch.");
        var failed = new FailedCleanup(); var failedID = failed.InstanceID;
        Reject<InvalidOperationException>(failed.Dispose); failed.Dispose();
        Check(failed.IsDisposed && failed.InstanceID == failedID && failed.IsClass(nameof(FailedCleanup)) && failed.Cleanups == 1,
            "Cleanup failure keeps immutable identity and class inspection without retrying cleanup.");
        ScriptTests.Register(); using var scripted = new ScriptActor();
        Check(scripted.IsClass(nameof(ScriptActor)) && scripted.IsClass(nameof(Node)) && !scripted.IsClass("ScriptTest.Actor"),
            "Compiled subclasses use their CLR names rather than a registered file identity.");

        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 1 };
        CollisionObject[] objects = [new SpecialBody(), new StaticBody(), new AnimatableBody(), new CharacterBody(), new Area()];
        for (var i = 0; i < objects.Length; i++)
        {
            var body = objects[i]; body.Position = new(i * 100, 0); body.CollisionLayer = body.CollisionMask = 0;
            body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        }
        using var tree = new SceneTree(root); var moving = (RigidBody)objects[0];
        moving.CanSleep = false; moving.GravityScale = 0; moving.LinearDampMode = RigidBody.DampMode.Replace; moving.LinearDamp = 0;
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Identity is tested on the selected world without fallback.");
        foreach (var value in objects)
        {
            Check(value.IsClass(value.ClassName) && value.IsClass(nameof(CollisionObject)) && value.IsClass(nameof(Entity)) &&
                value.IsClass(nameof(CanvasItem)) && value.IsClass(nameof(Node)) && value.IsClass(nameof(ElectronObject)), "The complete collision-object ancestry remains separate.");
            Check(value.IsClass(nameof(PhysicsBody)) == (value is PhysicsBody), "Area and body roles retain their different branches.");
            Check(!value.IsClass("node") && !value.IsClass(nameof(CircleShape)), "Case and sibling classes do not match.");
        }
        Check(moving.IsClass(nameof(RigidBody)) && moving.IsClass(nameof(SpecialBody)), "Compiled body subclasses preserve their concrete body role.");
        Check(circle.IsClass(nameof(Shape)) && circle.IsClass(nameof(Resource)) && !circle.IsClass(nameof(Node)), "Resources keep their separate hierarchy.");
        var ids = objects.Select(value => value.InstanceID).Append(circle.InstanceID).ToArray();
        Check(ids.All(id => id != 0) && ids.Distinct().Count() == ids.Length, "Different engine roles retain distinct nonzero process identities.");
        Task.Run(() => Check(moving.IsClass(nameof(Node)) && moving.InstanceID == ids[0], "Readonly identity inspection needs no solver owner access.")).GetAwaiter().GetResult();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) Frame();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all,
            "Warmed full frames and hierarchy inspection allocate zero owner/all-thread bytes.");
        Check(moving.Position.X > 0, "The measured identity workload retains actual selected body simulation.");
        var name = moving.ClassName; var diagnostic = moving.ToString(); tree.Dispose();
        Check(moving.IsDisposed && moving.ClassName == name && moving.InstanceID == ids[0] && moving.IsClass(nameof(PhysicsBody)) && moving.ToString() == diagnostic,
            "Scene retirement preserves identity and diagnostics while solver resources are released.");
        using var next = new GenericProbe<int>(); Check(!ids.Contains(next.InstanceID), "Retired identities are not reused.");
        Console.WriteLine($"Object identity {backend}: hierarchy, script-name isolation, disposal/errors, diagnostics and 128 warm/128 measured full frames at 0/0 B passed.");

        void Frame()
        {
            foreach (var value in objects) Check(value.IsClass(nameof(Node)) && !value.IsClass(nameof(Resource)), "Warm ancestry.");
            moving.LinearVelocity = new(1, 0); tree.PhysicsFrame(.01);
        }
    }
    private sealed class GenericProbe<T> : ElectronObject { }
    private sealed class SpecialBody : RigidBody { }
    private sealed class CustomString : ElectronObject { public override string ToString() => "custom object"; }
    private sealed class FailedCleanup : ElectronObject
    {
        internal int Cleanups;
        protected override void Dispose(bool disposing) { Cleanups++; throw new InvalidOperationException("cleanup probe"); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
