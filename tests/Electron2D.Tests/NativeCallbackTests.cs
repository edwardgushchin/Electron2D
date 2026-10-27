using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Electron2D;
using Path = System.IO.Path;

internal sealed class NativeCallbackTests
{
    private static bool _registered;
    private static readonly string SourcePath = Source();
    private static readonly ConditionalWeakTable<Node, NativeCallbackTests> States = new();
    private static string _mode = "normal";
    private static Node? _foreign;
    private static NodeScriptCallbacks<Node>? _pending;
    private static bool _failExit;
    private static readonly Exception ExitError = new IOException("native exit");
    private int _enter, _exit, _ready, _process, _physics, _input, _shortcut, _key, _unhandled, _retired;
    private readonly List<string> _notifications = [];
    private static Node CreateNode() => new();
    private static void Register()
    {
        if (_registered) return;
        NativeCallbackForward.Register(); NativeCallbackReverse.Register();
        Script.RegisterBinding<Node, NativeCallbackTests>("NativeCallbacks.Node", SourcePath, CreateNode, Bind, Clear);
        _registered = true;
    }
    private static void Clear(Node owner) { }
    private static void Bind(Node owner)
    {
        if (_mode == "foreign") { _foreign!.SetScriptCallbacks(new NodeScriptCallbacks<Node>()); return; }
        if (_mode == "worker") { Task.Run(() => owner.SetScriptCallbacks(new NodeScriptCallbacks<Node>())).GetAwaiter().GetResult(); return; }
        if (_mode == "wrong") { owner.SetScriptCallbacks(new NodeScriptCallbacks<Control>()); return; }
        if (_pending is { } pending) { owner.SetScriptCallbacks(pending); return; }
        var state = new NativeCallbackTests(); States.Add(owner, state);
        owner.SetScriptCallbacks(new NodeScriptCallbacks<Node>
        {
            EnterTree = state.Enter,
            ExitTree = state.Exit,
            Ready = state.Ready,
            Process = state.HandleProcess,
            PhysicsProcess = state.Physics,
            Input = state.Input,
            ShortcutInput = state.Shortcut,
            UnhandledKeyInput = state.HandleKey,
            UnhandledInput = state.Unhandled,
            Notification = state.Notify,
            Retired = state.Retire
        });
        if (_mode == "double") owner.SetScriptCallbacks(new NodeScriptCallbacks<Node>());
    }
    private void Enter(Node owner) => _enter++;
    private void Exit(Node owner) { Check(owner.GetScript() is not null, "Exit observes its installed source."); _exit++; if (_failExit) throw ExitError; }
    private void Ready(Node owner) => _ready++;
    private void HandleProcess(Node owner, double delta) => _process++;
    private void Physics(Node owner, double delta) => _physics++;
    private void Input(Node owner, InputEvent value) => _input++;
    private void Shortcut(Node owner, InputEvent value) => _shortcut++;
    private void HandleKey(Node owner, InputEventKey value) => _key++;
    private void Unhandled(Node owner, InputEvent value) => _unhandled++;
    private void Notify(Node owner, int what, bool reversed)
    {
        if (what < 7000) return;
        _notifications.Add(what + ":" + reversed);
        if (owner is TraceNode trace) trace.Trace.Add("script");
        if (what == 7100) owner.Notify(7101, !reversed);
    }
    private void Retire(Node owner) { States.Remove(owner); _retired++; }

    internal static void Run(PhysicsServer.Backend backend, bool separate)
    {
        Register(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        using var source = ResourceLoader.Load<Script>(NativeCallbackForward.SourcePath);
        using var reverse = ResourceLoader.Load<Script>(NativeCallbackReverse.SourcePath);
        using var nodeSource = ResourceLoader.Load<Script>(SourcePath);
        Guards(nodeSource);
        ExitLifetimes(nodeSource);
        using var world = new World(backend); using var view = new SubViewport { Size = new(200, 140), World = world, PhysicsObjectPicking = true };
        using var shape = new CircleShape { Radius = 5 };
        var body = source.New<RigidBody>(); body.Position = new(40, 40); body.GravityScale = 0; body.CanSleep = false;
        body.LinearDampMode = RigidBody.DampMode.Replace; body.LinearDamp = 0; body.InputPickable = true;
        body.AddChild(new CollisionShape { Shape = shape }); view.AddChild(body);
        var node = nodeSource.New<Node>(); view.AddChild(node);
        var state = NativeCallbackForward.States.GetValue(body, _ => throw new InvalidOperationException());
        var nodeState = States.GetValue(node, _ => throw new InvalidOperationException());
        Check(body.GetType() == typeof(RigidBody) && node.GetType() == typeof(Node), "Native owners use no project callback subclass.");
        using var tree = new SceneTree(view);
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Requested full solver executes without fallback.");
        Check(state.Enter == 1 && state.Ready == 1 && nodeState._enter == 1 && nodeState._ready == 1 && node.ProcessEnabled && body.PhysicsProcessEnabled, "Native readiness enables installed callbacks before Ready.");
        using (var input = new InputEventKey { Keycode = Key.A, Pressed = true }) view.PushInput(input, true);
        Check(nodeState._input == 1 && nodeState._shortcut == 1 && nodeState._key == 1 && nodeState._unhandled == 1, "All ordinary input stages dispatch installed code.");
        using (var mouse = new InputEventMouseMotion { Position = new(40, 40), GlobalPosition = new(40, 40) }) view.PushInput(mouse, true);
        var signals = 0; body.MouseEntered += () => signals++; body.SetBlockSignals(true); tree.PhysicsFrame(1d / 60); body.SetBlockSignals(false);
        Check(state.MouseIn == 1 && state.ShapeIn == 1 && state.Pointer > 0 && signals == 0, "Native picking hooks keep logical indices and execute while their ordinary signals are blocked.");
        using (var mouse = new InputEventMouseMotion { Position = new(180, 120), GlobalPosition = new(180, 120) }) view.PushInput(mouse, true);
        tree.PhysicsFrame(1d / 60); Check(state.MouseOut == 1 && state.ShapeOut == 1, "Native mouse/shape exit hooks execute.");
        Check(state.Integrations > 0 && body.LinearVelocity.Y == .25f, "Borrowed post-solver integration changes the actual native body's state.");
        Check(state.Borrowed!.LinearVelocity.Y == .25f, "A retained borrowed view follows the existing live attachment/query lifetime.");
        var id = body.InstanceID; var rid = body.GetRID(); var x = body.Position.X; Measure(); Check(body.Position.X > x + 1, "Forward callbacks produce real motion.");
        body.SetScript(reverse); var backwards = NativeCallbackReverse.States.GetValue(body, _ => throw new InvalidOperationException());
        Check(state.Releases == 1 && !NativeCallbackForward.States.TryGetValue(body, out _) && body.PhysicsProcessEnabled, "Replacement releases the old callback state once and keeps ordinary switches.");
        x = body.Position.X; Measure(); Check(body.Position.X < x - 1 && body.LinearVelocity.Y == -.5f && backwards.Integrations > 0 && body.InstanceID == id && body.GetRID() == rid, "Replacement changes native callbacks without replacing physical identity.");
        body.SetScript(null); var count = backwards.Physics; tree.PhysicsFrame(1d / 60); Check(backwards.Physics == count && backwards.Releases == 1, "Detachment removes all native frame/integration slots.");
        body.SetScript(source); var late = NativeCallbackForward.States.GetValue(body, _ => throw new InvalidOperationException());
        Check(late.Ready == 0 && body.PhysicsProcessEnabled, "Already-ready attachment enables callbacks without rerunning Ready.");
        late.Veto = true; body.Dispose(); Check(!body.IsDisposed && late.Releases == 0, "A script notification can cancel pre-delete and keep every slot.");
        tree.PhysicsFrame(1d / 60); Check(late.Physics == 1, "Vetoed owner remains operational.");
        view.RemoveChild(node); Check(nodeState._exit == 1, "Managed exit dispatch reaches native callback sets."); node.Dispose(); Check(nodeState._retired == 1, "Node state retires once after exit.");
        Failures(source, reverse);
        using (var copy = (RigidBody)body.Duplicate()) Check(copy.GetType() == typeof(RigidBody) && copy.GetScript() == source && NativeCallbackForward.States.TryGetValue(copy, out var copied) && !ReferenceEquals(copied, late), "Native copies install independent callbacks.");
        Stored(body);
        body.Dispose(); Check(late.Exit == 1 && late.Releases == 1 && body.IsDisposed, "Committed native owner disposal exits while script state is live, then retires it once.");
        Reject<ObjectDisposedException>(() => _ = late.Borrowed!.LinearVelocity);
        Console.WriteLine($"Native Script callbacks {backend}, separate={separate}: native Node/body/input/picking/integration, copies/child files, guards, failures and zero-allocation frames passed.");

        void Measure()
        {
            for (var i = 0; i < 256; i++) Frame(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 128; i++) Frame(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "Native callback full process/physics frames require 0/0 owner/all-thread bytes.");
            Console.WriteLine($"Native callback {backend}: 128 measured full process/physics pairs at 0/0 B.");
            void Frame() { tree.ProcessFrame(1d / 60); tree.PhysicsFrame(1d / 60); }
        }
    }

    private static void Guards(Script source)
    {
        using var owner = new Node(); using var foreign = new Node(); _foreign = foreign;
        Reject<InvalidOperationException>(() => owner.SetScriptCallbacks(new NodeScriptCallbacks<Node>()));
        foreach (var mode in new[] { "foreign", "worker", "wrong", "double" })
        {
            _mode = mode;
            if (mode == "wrong") Reject<ArgumentException>(() => owner.SetScript(source)); else Reject<InvalidOperationException>(() => owner.SetScript(source));
            Check(owner.GetScript() is null && foreign.GetScript() is null, "Rejected installation leaves no acquired binding.");
        }
        _mode = "normal"; _pending = new NodeScriptCallbacks<Node>(); owner.SetScript(source);
        Reject<InvalidOperationException>(() => foreign.SetScript(source)); Check(owner.GetScript() == source, "A reused packet does not retire another owner's state.");
        owner.SetScript(null); Reject<InvalidOperationException>(() => owner.SetScript(source)); _pending = null;
        using var trace = new TraceNode(); trace.SetScript(source); var state = States.GetValue(trace, _ => throw new InvalidOperationException());
        trace.Notify(7000, false); Check(trace.Trace.SequenceEqual(["native", "script"]), "Forward script notification follows native chain."); trace.Trace.Clear();
        trace.Notify(7000, true); Check(trace.Trace.SequenceEqual(["script", "native"]), "Reverse script notification precedes native chain.");
        state._notifications.Clear(); trace.Notify(7100, true); Check(state._notifications.SequenceEqual(["7100:True", "7101:False"]), "Nested numeric notification keeps its explicit direction.");
        _foreign = null;
    }
    private static void Failures(Script forward, Script reverse)
    {
        using var body = new RigidBody(); NativeCallbackForward.FailBind = true;
        try { Same(NativeCallbackForward.BindError, () => body.SetScript(forward)); Check(body.GetScript() is null && !NativeCallbackForward.States.TryGetValue(body, out _), "Partial bind cleans engine-owned callbacks and state."); }
        finally { NativeCallbackForward.FailBind = false; }
        body.SetScript(forward); var state = NativeCallbackForward.States.GetValue(body, _ => throw new InvalidOperationException());
        NativeCallbackForward.FailClear = NativeCallbackForward.FailRetired = true;
        try
        {
            try { body.SetScript(reverse); throw new InvalidOperationException("Missing cleanup failure."); }
            catch (AggregateException error) { Check(error.InnerExceptions.SequenceEqual([NativeCallbackForward.ClearError, NativeCallbackForward.RetiredError]) && state.Releases == 1 && body.GetScript() is null, "Both cleanup errors survive while native slots retire exactly once."); }
        }
        finally { NativeCallbackForward.FailClear = NativeCallbackForward.FailRetired = false; }
    }
    private static void ExitLifetimes(Script source)
    {
        using var view = new SubViewport(); var parent = source.New<Node>(); var child = source.New<Node>(); parent.AddChild(child); view.AddChild(parent);
        using var tree = new SceneTree(view); var p = States.GetValue(parent, _ => throw new InvalidOperationException()); var c = States.GetValue(child, _ => throw new InvalidOperationException());
        parent.Dispose(); Check(p._exit == 1 && c._exit == 1 && p._retired == 1 && c._retired == 1 && parent.IsDisposed && child.IsDisposed, "Native parent disposal exits the entire subtree before retiring each state once.");
        var failing = source.New<Node>(); view.AddChild(failing); var state = States.GetValue(failing, _ => throw new InvalidOperationException()); _failExit = true;
        try { Reject<Exception>(failing.Dispose); Check(failing.IsDisposed && state._exit == 1 && state._retired == 1, "Exit failure retains terminal disposal and state retirement."); }
        finally { _failExit = false; }
    }
    private static void Stored(RigidBody owner)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-native-callback-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var scene = new PackedScene(); scene.Pack(owner); var path = Path.Combine(folder, "native.e2dscene"); ResourceSaver.Save(scene, path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            start.ArgumentList.Add("--native-callback-child"); start.ArgumentList.Add(path);
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); process.WaitForExit(5000); throw new TimeoutException("Native callback child."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Native callback child passed"), output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void Child(string path)
    {
        Register(); using var scene = ResourceLoader.Load<PackedScene>(path); using var owner = (RigidBody)scene.Instantiate();
        Check(owner.GetType() == typeof(RigidBody) && owner.GetScript()?.CompiledTypeID == "NativeCallbacks.Forward" && NativeCallbackForward.States.TryGetValue(owner, out var state) && state.Ready == 0, "Fresh process reconstructs a real native owner and installs its code before activation.");
        Console.WriteLine("Native callback child passed");
    }
    private sealed class TraceNode : Node
    {
        internal readonly List<string> Trace = [];
        protected override void OnNotification(int what)
        { if (what >= 7000) Trace.Add("native"); base.OnNotification(what); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Same(Exception expected, Action action) { try { action(); } catch (Exception error) when (ReferenceEquals(error, expected)) { return; } throw new InvalidOperationException("Original exception identity was not retained."); }
    private static string Source([CallerFilePath] string path = "") => path;
}
