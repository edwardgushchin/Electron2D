using System.Diagnostics;
using System.Reflection;
using Electron2D;
using Path = System.IO.Path;

internal sealed class LiveScriptBindingTests
{
    private static bool _registered;
    private static readonly string SourcePath = Source();
    private static LiveScriptBody Create() => new();
    private static void FaultClear(LiveScriptBody owner) => LiveScriptForward.Clear(owner);
    private static void FaultBind(LiveScriptBody owner)
    {
        owner.Binds++; owner.Step = static (b, _) => b.Counter += 100;
        Check(owner.GetScript()?.CompiledType == typeof(LiveScriptBindingTests), "New source is visible during installation.");
        Reject<InvalidOperationException>(() => owner.SetScript(null));
        Reject<InvalidOperationException>(owner.Dispose);
        if (owner.BindEntered is { } entered)
        {
            entered.Set();
            if (!owner.ContinueBind!.Wait(30000)) throw new TimeoutException("Concurrent binding gate.");
        }
        if (owner.FailBind) throw owner.BindError;
    }
    private static void Register()
    {
        if (_registered) return;
        Reject<ArgumentException>(() => Script.RegisterBinding<Script, LiveScriptForward>("Live.Invalid", LiveScriptForward.SourcePath, null, static _ => { }, static _ => { }));
        var captured = 0;
        Reject<ArgumentException>(() => Script.RegisterBinding<LiveScriptBody, LiveScriptForward>("Live.Capture", LiveScriptForward.SourcePath, null, _ => captured++, LiveScriptForward.Clear));
        Check(captured == 0, "Invalid registration does not execute a bind.");
        Reject<ArgumentException>(() => Script.RegisterBinding<LiveScriptBody, LiveScriptForward>("Live.ForeignCode", LiveScriptForward.SourcePath, Create, LiveScriptReverse.Bind, LiveScriptForward.Clear));
        Script.RegisterNode("Live.Body", LiveScriptBody.SourcePath, Create, LiveScriptBody.CounterProperty);
        Script.RegisterBinding<LiveScriptBody, LiveScriptForward>("Live.Forward", LiveScriptForward.SourcePath, Create, LiveScriptForward.Bind, LiveScriptForward.Clear, LiveScriptBody.CounterProperty);
        Script.RegisterBinding<LiveScriptBody, LiveScriptReverse>("Live.Reverse", LiveScriptReverse.SourcePath, Create, LiveScriptReverse.Bind, LiveScriptReverse.Clear, LiveScriptBody.CounterProperty);
        Script.RegisterBinding<LiveScriptBody, LiveScriptBindingTests>("Live.Fault", SourcePath, null, FaultBind, FaultClear, LiveScriptBody.CounterProperty);
        _registered = true;
    }
    internal static void Run(PhysicsServer.Backend backend)
    {
        Register(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, Environment.GetEnvironmentVariable("ELECTRON2D_TEST_THREADED_COLLIDER") == "1");
        using var forward = ResourceLoader.Load<Script>(LiveScriptForward.SourcePath);
        using var reverse = ResourceLoader.Load<Script>(LiveScriptReverse.SourcePath);
        using var fault = ResourceLoader.Load<Script>(SourcePath);
        using var fixedSource = ResourceLoader.Load<Script>(LiveScriptBody.SourcePath); using var fixedOwner = fixedSource.New<LiveScriptBody>();
        Reject<NotSupportedException>(() => fixedOwner.SetScript(forward)); Check(fixedOwner.GetScript() == fixedSource && fixedOwner.Binds == 0, "Fixed CLR source reassignment remains explicitly unsupported and preserves its owner.");
        using var resource = new Resource(); Reject<ArgumentException>(() => resource.SetScript(forward));
        Check(forward.CompiledType == typeof(LiveScriptForward) && forward.GetInstanceBaseType() == typeof(LiveScriptBody) && forward.CanInstantiate(), "Code and stable owner roles remain distinct metadata.");
        using (var fresh = forward.New<LiveScriptBody>())
        {
            Check(fresh.ConstructorSource == forward && fresh.GetScript() == forward && fresh.State is LiveScriptForward && fresh.Binds == 1 && fresh.Counter == 10, "Initial construction binds once after the owner constructor.");
            Check(forward.GetPropertyDefaultValue(LiveScriptBody.CounterProperty, fresh) == 0, "Binding defaults accept the actual typed owner context.");
            fresh.Counter = 37;
            using var copy = (LiveScriptBody)fresh.Duplicate();
            Check(copy.GetScript() == forward && copy.State is LiveScriptForward && !ReferenceEquals(copy.State, fresh.State) && copy.Counter == 37 && copy.Binds == 1, "Node copies install independent state before stored restoration.");
            using var stripped = (LiveScriptBody)fresh.Duplicate(Node.DuplicateFlags.Groups);
            Check(stripped.GetScript() is null && stripped.State is null && stripped.Step is null, "Omitting Scripts preserves the stable owner role without installing code.");
            Stored(fresh);
        }
        using var plain = new ElectronObject(); Reject<ArgumentException>(() => plain.SetScript(forward));
        using var unbound = new Script();
        using var actor = new LiveScriptBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        var order = new List<string>(); Script? expected = forward;
        actor.PropertyListChanged += _ => { Check(actor.GetScript() == expected, "Property observer sees committed source."); order.Add("properties"); };
        actor.ScriptChanged += _ => { Check(actor.GetScript() == expected, "Script observer sees committed source."); order.Add("script"); };
        actor.SetScript(forward); Check(order.SequenceEqual(["properties", "script"]), "Initial assignment notification order.");
        var before = actor.Binds; actor.SetScript(forward); Check(actor.Binds == before && order.Count == 2, "Same source retains state without notifications.");
        Reject<NotSupportedException>(() => actor.SetScript(unbound)); Check(actor.GetScript() == forward && actor.Binds == before, "Prevalidation preserves the prior live binding.");
        actor.Veto = true; actor.Dispose(); Check(!actor.IsDisposed && actor.GetScript() == forward && actor.Clears == 0, "Cancelled pre-delete retains installed state.");
        using var world = new World(backend); using var viewport = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 }; actor.AddChild(new CollisionShape { Shape = shape }); viewport.AddChild(actor);
        using var tree = new SceneTree(viewport); var id = actor.InstanceID; var rid = actor.GetRID();
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "The full selected backend executes without fallback.");
        var start = actor.Position.X; Measure(); Check(actor.Position.X > start + 1, "Installed forward functions change the existing body's actual motion.");
        expected = reverse; order.Clear(); actor.SetScript(reverse);
        Check(actor.State is LiveScriptReverse && actor.Clears == 1 && order.SequenceEqual(["properties", "script"]), "Replacement retires the old state and publishes the new one.");
        start = actor.Position.X; Measure(); Check(actor.Position.X < start - 1 && actor.GetRID() == rid && actor.InstanceID == id && ReferenceEquals(viewport.Children[0], actor), "Replacement changes numerical motion without replacing owner/RID/scene identity.");
        expected = null; order.Clear(); actor.SetScript(null); var count = actor.Counter; tree.PhysicsFrame(1d / 60);
        Check(actor.State is null && actor.Step is null && actor.Counter == count && order.SequenceEqual(["properties", "script"]), "Detachment stops script functions and retires owned state.");
        expected = forward; actor.SetScript(forward); actor.SetBlockSignals(true); expected = reverse; order.Clear(); actor.SetScript(reverse); actor.SetBlockSignals(false);
        Check(order.Count == 0 && actor.State is LiveScriptReverse, "Signal blocking suppresses notifications without suppressing replacement.");
        actor.FailBind = true; expected = null; order.Clear(); Same(actor.BindError, () => actor.SetScript(fault));
        Check(actor.GetScript() is null && actor.Step is null && actor.State is null && order.SequenceEqual(["properties", "script"]), "Failed installation clears partial functions and publishes the unbound result.");
        actor.FailBind = false; expected = forward; actor.SetScript(forward); actor.FailClear = true; expected = null; Same(actor.ClearError, () => actor.SetScript(reverse));
        Check(actor.GetScript() is null && actor.Step is null, "Failed retirement leaves no installed new script."); actor.FailClear = false;
        using (var borrowed = (Script)forward.Duplicate())
        {
            expected = borrowed; actor.SetScript(borrowed); borrowed.Dispose(); before = actor.Binds; actor.SetScript(borrowed); tree.PhysicsFrame(1d / 60);
            Check(actor.Binds == before && actor.State is LiveScriptForward, "Disposed borrowed assets retain installed functions and equal-source suppression.");
        }
        actor.Dispose(); Check(actor.IsDisposed && actor.Step is null && actor.State is null, "Committed owner disposal retires functions and state.");
        using var observed = new LiveScriptBody(); var marker = new IOException("observer"); observed.PropertyListChanged += _ => throw marker;
        Same(marker, () => observed.SetScript(forward)); Check(observed.GetScript() == forward && observed.State is LiveScriptForward, "Observer failure does not undo committed installation.");
        using var nested = new LiveScriptBody(); var replaced = false;
        nested.ScriptChanged += _ => { if (!replaced) { replaced = true; nested.SetScript(reverse); } }; nested.SetScript(forward);
        Check(nested.GetScript() == reverse && nested.Clears == 1, "Post-commit observers may start a new lifetime transition.");
        using var failed = new LiveScriptBody { FailBind = true, FailClear = true };
        try { failed.SetScript(fault); throw new InvalidOperationException("Missing combined failure."); }
        catch (AggregateException error) { Check(error.InnerExceptions.SequenceEqual([failed.BindError, failed.ClearError]) && failed.GetScript() is null && failed.Step is null, "Bind and cleanup errors retain their order and original identities."); }
        using var retired = new LiveScriptBody(); var retiredSignals = 0;
        retired.PropertyListChanged += _ => retired.Dispose(); retired.ScriptChanged += _ => retiredSignals++;
        retired.SetScript(forward); Check(retired.IsDisposed && retired.State is null && retiredSignals == 0, "Retirement during property publication suppresses the later script signal.");
        using var terminal = new LiveScriptBody(); terminal.SetScript(forward); terminal.FailClear = true;
        Same(terminal.ClearError, terminal.Dispose); Check(terminal.IsDisposed && terminal.State is null, "Script cleanup failure still commits native owner disposal.");
        using var entered = new ManualResetEventSlim(); using var resume = new ManualResetEventSlim();
        using var concurrent = new LiveScriptBody { BindEntered = entered, ContinueBind = resume };
        var installing = Task.Run(() => concurrent.SetScript(fault));
        try
        {
            Check(entered.Wait(30000), "The actual concurrent binder entered its guarded operation.");
            Reject<InvalidOperationException>(concurrent.Dispose); Reject<InvalidOperationException>(() => concurrent.SetScript(forward));
        }
        finally { resume.Set(); }
        installing.GetAwaiter().GetResult(); Check(concurrent.GetScript() == fault && !concurrent.IsDisposed, "Concurrent rejected lifetime changes leave the completed binding live.");
        Console.WriteLine($"Live typed Script {backend}, separate={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: actual replacement/detachment, stable physics identity, source/state lifetime, copies/child archives and failure ordering passed.");

        void Measure()
        {
            for (var i = 0; i < 256; i++) tree.PhysicsFrame(1d / 60);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "Full active script/physics frames allocate 0/0 owner/all-thread bytes.");
            Console.WriteLine($"Live binding {backend}: 128 measured active frames, 0/0 managed bytes.");
        }
    }
    private static void Stored(LiveScriptBody owner)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-live-script-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var scene = new PackedScene(); scene.Pack(owner); var path = Path.Combine(folder, "live.e2dscene"); ResourceSaver.Save(scene, path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            start.ArgumentList.Add("--live-script-child"); start.ArgumentList.Add(path);
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); process.WaitForExit(5000); throw new InvalidOperationException("Live script child timeout."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Live script child passed"), output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void Child(string path)
    {
        Register(); using var scene = ResourceLoader.Load<PackedScene>(path); using var owner = (LiveScriptBody)scene.Instantiate();
        Check(owner.GetScript()?.CompiledTypeID == "Live.Forward" && owner.State is LiveScriptForward && owner.Counter == 37 && owner.Binds == 1, "Fresh scene process installs code before restoring stored state.");
        Console.WriteLine("Live script child passed");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Same(Exception expected, Action action) { try { action(); } catch (Exception error) when (ReferenceEquals(error, expected)) { return; } throw new InvalidOperationException("Original exception identity was not preserved."); }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
