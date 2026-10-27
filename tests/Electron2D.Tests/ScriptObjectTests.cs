using System.Diagnostics;
using System.Reflection;
using Electron2D;
using Path = System.IO.Path;

internal static class ScriptObjectTests
{
    private static bool _registered;
    private static ScriptPlainObject Create() => new();
    private static void Register()
    {
        if (_registered) return;
        Reject<ArgumentException>(() => Script.RegisterObject<Node>("ScriptPlain.WrongNode", ScriptPlainObject.SourcePath, null));
        Reject<ArgumentException>(() => Script.RegisterObject<Resource>("ScriptPlain.WrongResource", ScriptPlainObject.SourcePath, null));
        Reject<ArgumentException>(() => Script.RegisterObject<ElectronObject>("ScriptPlain.Engine", ScriptPlainObject.SourcePath, null));
        var captured = 1;
        Reject<ArgumentException>(() => Script.RegisterObject("ScriptPlain.Capture", ScriptPlainObject.SourcePath, () => { captured++; return new ScriptPlainObject(); }));
        Check(captured == 1, "Registration rejects captured factories without constructing an owner.");
        Script.RegisterAbstract<ScriptPlainBase>("ScriptPlain.Base", ScriptPlainBase.SourcePath, ScriptPlainBase.PowerProperty);
        Script.RegisterObject("ScriptPlain.Object", ScriptPlainObject.SourcePath, Create, ScriptPlainObject.CountProperty);
        Script.RegisterObject<ScriptPlainRequired>("ScriptPlain.Required", ScriptPlainRequired.SourcePath, null);
        _registered = true;
    }

    internal static void Run(PhysicsServer.Backend backend)
    {
        Register();
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, Environment.GetEnvironmentVariable("ELECTRON2D_TEST_THREADED_COLLIDER") == "1");
        using var script = ResourceLoader.Load<Script>(ScriptPlainObject.SourcePath);
        Check(script.CompiledType == typeof(ScriptPlainObject) && script.GetInstanceBaseType() == typeof(ElectronObject) &&
            script.CanInstantiate() && script.GetGlobalName() == nameof(ScriptPlainObject), "Plain compiled classes retain source and native root metadata.");
        using var basis = script.GetBaseScript()!;
        Check(basis.IsAbstract() && !basis.CanInstantiate(), "Plain abstract project bases have metadata without construction.");
        Reject<InvalidOperationException>(() => basis.New<ElectronObject>());
        Check(script.HasScriptMethod(nameof(ScriptPlainBase.Twice)) && script.HasScriptSignal(nameof(ScriptPlainObject.Pulse)) &&
            script.GetScriptPropertyList().Length == 2 && script.GetRPCConfig().Length == 0 &&
            script.GetScriptConstantMap<int>()[nameof(ScriptPlainObject.Limit)] == 12 &&
            script.GetScriptConstantMap<string>()[nameof(ScriptPlainBase.Category)] == "Plain", "Typed inherited property/method/event/constants and absent Node RPC role.");
        using var actor = script.New<ScriptPlainObject>();
        Check(actor.GetScript() == script && actor.ConstructorSource == script && actor.BaseConstructorSource == script &&
            actor.Count == 3 && script.GetPropertyDefaultValue(ScriptPlainBase.PowerProperty, actor) == 7,
            "Plain owner and both constructor levels observe the exact initial source.");
        using var ordinary = new ScriptPlainObject(); Check(ordinary.GetScript() is null, "Ordinary construction does not infer a source.");
        Reject<InvalidOperationException>(() => script.New<Node>());
        Reject<InvalidOperationException>(() => script.New(() => actor));
        Check(!actor.IsDisposed, "A pre-existing returned object is not acquired or cleaned up.");
        using var worker = script.New(() => Task.Run(Create).GetAwaiter().GetResult());
        Check(worker.GetScript() == script && worker.ConstructorSource == script, "Plain typed construction keeps joined-worker provenance.");
        using var required = ResourceLoader.Load<Script>(ScriptPlainRequired.SourcePath);
        Check(required.CanInstantiate(), "A concrete argument-only plain class can instantiate with an explicit constructor.");
        var before = ScriptPlainRequired.Initializations;
        Reject<InvalidOperationException>(() => required.New<ElectronObject>());
        Check(ScriptPlainRequired.Initializations == before, "Missing default construction rejects before the project constructor.");
        using var initialized = required.New(() => new ScriptPlainRequired(42));
        Check(initialized.Value == 42 && initialized.ConstructorSource == required && ScriptPlainRequired.Initializations == before + 1, "Typed constructor arguments execute once with the source association.");
        var pulses = 0; actor.Pulse += _ => pulses++; actor.Notify(7001);
        Check(actor.Count == 4 && pulses == 1 && actor.Twice(9) == 18, "The actual class executes direct methods and notification callbacks.");
        actor.SetBlockSignals(true); actor.Notify(7001); actor.SetBlockSignals(false);
        Check(actor.Count == 5 && pulses == 1, "Object signal blocking does not suppress direct notification work.");
        actor.VetoDisposal = true; actor.Dispose();
        Check(!actor.IsDisposed && actor.Cleanups == 0 && actor.GetScript() == script, "Cancelled disposal retains the live script and project state.");
        using var copy = (Script)script.Duplicate(); using var copied = copy.New<ScriptPlainObject>();
        Check(copied.GetScript() == copy && copied.Count == 3, "Copied source assets create independent plain owners.");
        Stored(script);
        Physics(actor, script, backend);
        script.Dispose(); actor.Notify(7001);
        Check(actor.GetScript() == script && actor.Count > 5, "Source disposal leaves ordinary compiled code and its borrowed identity active.");
        Reject<ObjectDisposedException>(() => script.New<ElectronObject>());
        actor.Dispose(); Check(actor.Cleanups == 1, "Committed disposal cleans the plain owner once.");
        Reject<ObjectDisposedException>(() => actor.GetScript());
        Console.WriteLine($"Plain compiled object {backend}: metadata, constructors, source/copy/archive, callback/lifetime and selected physics passed.");
    }

    private static void Physics(ScriptPlainObject actor, Script script, PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var viewport = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 }; using var query = new PhysicsPointQueryParameters();
        var mover = new RigidBody { Position = new(20, 0), GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        mover.AddChild(new CollisionShape { Shape = shape }); viewport.AddChild(mover);
        using var tree = new SceneTree(viewport);
        var hits = new PhysicsPointResult[1]; var body = PhysicsServer.BodyCreate();
        try
        {
            Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Plain object fixture uses the requested full backend.");
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(body, shape.GetRID());
            PhysicsServer.BodyAttachObject(body, actor); PhysicsServer.BodySetSpace(body, world.Space);
            mover.LinearVelocity = new(1, 0);
            var direct = world.DirectSpaceState;
            for (var i = 0; i < 256; i++) Frame();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 128; i++) Frame();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "Prepared script callbacks/source/property/query/full-step intervals allocate zero owner/all-thread bytes.");
            Check(mover.Position.X > 21, "The measured fixture retains actual body travel.");
            Console.WriteLine($"Plain compiled owner {backend}: 128 measured moving frames and direct queries at 0/0 bytes.");
            void Frame()
            {
                actor.Notify(7001); tree.PhysicsFrame(1d / 60);
                Check(direct.IntersectPoint(query, hits) == 1 && hits[0].ColliderObject == actor &&
                    hits[0].ColliderID == actor.InstanceID && actor.GetScript() == script && ScriptPlainObject.CountProperty.GetValue(actor) == actor.Count,
                    "Queries and typed access keep the live source-associated object.");
            }
        }
        finally { PhysicsServer.FreeRID(body); }
    }

    private static void Stored(Script script)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-plain-script-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "source.e2dres"); ResourceSaver.Save(script, path);
            using var loaded = ResourceLoader.Load<Script>(path, ResourceLoader.CacheMode.Ignore); using var owner = loaded.New<ScriptPlainObject>();
            Check(loaded.CompiledTypeID == "ScriptPlain.Object" && owner.GetScript() == loaded && owner.Count == 3, "Archive source assets reconstruct actual plain owners.");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            start.ArgumentList.Add("--plain-script-child"); start.ArgumentList.Add(path);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(entireProcessTree: true); process.WaitForExit(5000); throw new InvalidOperationException("Fresh plain source child timed out."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh plain script passed"), "Fresh-process plain source: " + output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void Child(string path)
    {
        Register(); using var script = ResourceLoader.Load<Script>(path); using var owner = script.New<ScriptPlainObject>(); owner.Notify(7001);
        Check(owner.Count == 4 && owner.ConstructorSource == script && owner.BaseConstructorSource == script && script.GetInstanceBaseType() == typeof(ElectronObject), "Fresh file process recreates the registered plain class.");
        Console.WriteLine("Fresh plain script passed");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
