using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Electron2D;

internal sealed partial class LiveRPCTests
{
    private static bool _registered;
    private static readonly string SourcePath = Source();
    internal static readonly RPCOptions Any = new(RPCMode.AnyPeer, true);
    internal static bool FailBind, FailClear, CheckBindGuard;
    internal static readonly Exception BindError = new IOException("live RPC bind"), ClearError = new IOException("live RPC clear");
    internal static Action? SizeHook, DecodeHook;
    internal static Script? SwitchAfterDrive;
    private static int _manualCalls, _plainCalls;
    private static readonly ConditionalWeakTable<RigidBody, State> States = new();
    private static readonly RPCMethod<Node, int> Manual = Method<Node>(9919, static (_, v) => _manualCalls += v);
    private static readonly RPCMethod<Node, int> Plain = Method<Node>(9920, static (_, v) => _plainCalls += v);
    internal sealed class State(int mode)
    {
        internal readonly int Mode = mode;
        internal int Value, Calls, Extra, Ack, Sender, Releases;
        internal float Speed;
        internal bool Veto;
    }
    internal static State StateFor(RigidBody node) => States.GetValue(node, static _ => throw new InvalidOperationException("No live RPC owner state."));
    private static RigidBody Create() => new();
    private static Node CreateNode() => new();
    private static ScriptRPCActor CreateOwnerActor() => new();
    private static void BindNode(Node owner) { }
    private static void ClearNode(Node owner) { }
    internal static RPCMethod<T, int> Method<T>(uint id, Action<T, int> action, bool hooks = false) where T : Node => new(id, action,
        _ => { if (hooks) SizeHook?.Invoke(); return 4; },
        static (v, b) => { BinaryPrimitives.WriteInt32LittleEndian(b, v); return 4; },
        b => { if (b.Length != 4) throw new InvalidDataException("Expected complete integer RPC payload."); var value = BinaryPrimitives.ReadInt32LittleEndian(b); if (hooks) DecodeHook?.Invoke(); return value; });
    internal static void Install(RigidBody owner, int mode)
    {
        var state = new State(mode); States.Add(owner, state);
        owner.SetScriptCallbacks(new RigidBodyScriptCallbacks<RigidBody>
        {
            PhysicsProcess = static (n, _) => n.LinearVelocity = new(StateFor(n).Speed, 0),
            Notification = static (n, what, _) => { if (what == ElectronObject.NotificationPreDelete && StateFor(n).Veto) { StateFor(n).Veto = false; n.CancelDisposal(); } },
            Retired = n => { States.Remove(n); state.Releases++; }
        });
        if (CheckBindGuard) Reject<InvalidOperationException>(() => owner.RPC(LiveRPCForward.Drive, 1));
        if (FailBind) throw BindError;
    }
    internal static void Drive(RigidBody owner, int value)
    {
        var state = StateFor(owner); state.Calls++; state.Value += state.Mode * value; state.Speed = state.Mode * value;
        state.Sender = owner.Multiplayer.GetRemoteSenderID();
        if (SwitchAfterDrive is { } next) { SwitchAfterDrive = null; owner.SetScript(next); }
    }
    private static void Register()
    {
        if (_registered) return;
        Reject<ArgumentException>(() => Script.RegisterBindingWithRPC<RigidBody, LiveRPCForward>("LiveRPC.Invalid", LiveRPCForward.SourcePath, Create, LiveRPCForward.Bind, LiveRPCForward.Clear, [default]));
        var wrong = Method<Control>(9911, static (_, _) => { });
        Reject<ArgumentException>(() => Script.RegisterBindingWithRPC<RigidBody, LiveRPCForward>("LiveRPC.Invalid", LiveRPCForward.SourcePath, Create, LiveRPCForward.Bind, LiveRPCForward.Clear, [new(wrong, Any)]));
        Reject<ArgumentException>(() => Script.RegisterBindingWithRPC<RigidBody, LiveRPCForward>("LiveRPC.Invalid", LiveRPCForward.SourcePath, Create, LiveRPCForward.Bind, LiveRPCForward.Clear, [new(LiveRPCForward.Drive, Any), new(LiveRPCForward.Drive, Any)]));
        Reject<ArgumentException>(() => Script.RegisterBindingWithRPC<RigidBody, LiveRPCForward>("LiveRPC.Invalid", LiveRPCForward.SourcePath, Create, LiveRPCForward.Bind, LiveRPCForward.Clear, new RPCRegistration[65537]));
        var declarations = new[] { new RPCRegistration(LiveRPCForward.Drive, Any), new RPCRegistration(LiveRPCForward.Ack, new(RPCMode.Authority)) };
        Script.RegisterBindingWithRPC<RigidBody, LiveRPCForward>("LiveRPC.Forward", LiveRPCForward.SourcePath, Create, LiveRPCForward.Bind, LiveRPCForward.Clear, declarations); declarations[0] = default;
        Script.RegisterBindingWithRPC<RigidBody, LiveRPCReverse>("LiveRPC.Reverse", LiveRPCReverse.SourcePath, Create, LiveRPCReverse.Bind, LiveRPCReverse.Clear, [new(LiveRPCReverse.Drive, Any), new(LiveRPCReverse.Ack, new(RPCMode.Authority))]);
        using var source = ResourceLoader.Load<Script>(LiveRPCForward.SourcePath); using var early = source.New<RigidBody>();
        Check(source.GetRPCConfig().Length == 2 && early.GetNodeRPCConfig().Length == 2, "Copied code declarations precede late base registration.");
        Script.RegisterBindingWithRPC<RigidBody, LiveRPCBase>("LiveRPC.Base", LiveRPCBase.SourcePath, Create, LiveRPCBase.Bind, LiveRPCBase.Clear, [new(LiveRPCBase.Shadow, Any), new(LiveRPCBase.Extra, Any)]);
        Check(source.GetRPCConfig().Length == 3 && early.GetNodeRPCConfig().Length == 3 && source.GetRPCConfig()[0].Method == LiveRPCForward.Drive, "Late inherited compatible declarations remain visible with derived ID priority.");
        Script.RegisterBindingWithRPC<Node, LiveRPCTests>("LiveRPC.Plain", SourcePath, CreateNode, BindNode, ClearNode, [new(Plain, Any)]);
        Script.RegisterAbstractWithRPC<ScriptRPCBaseActor>("LiveRPC.OwnerBase", ScriptRPCBaseActor.SourcePath, [new(ScriptRPCBaseActor.BaseScore, Any)]);
        using var earlierClass = new ScriptRPCActor(); earlierClass.RPCConfig(ScriptRPCBaseActor.BaseScore, Any);
        Script.RegisterNodeWithRPC("LiveRPC.OwnerClass", ScriptRPCActor.SourcePath, CreateOwnerActor, [new(ScriptRPCActor.Add, Any)]);
        Check(earlierClass.GetNodeRPCConfig().Single().Method == ScriptRPCBaseActor.BaseScore, "Existing CLR-class overrides retain their token identity after late derived registration.");
        _registered = true;
    }
    internal static void Run(PhysicsServer.Backend backend, bool separate)
    {
        Register(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        using var forward = ResourceLoader.Load<Script>(LiveRPCForward.SourcePath); using var reverse = ResourceLoader.Load<Script>(LiveRPCReverse.SourcePath);
        using var plain = ResourceLoader.Load<Script>(SourcePath);
        using (var actor = new ScriptRPCActor())
        { actor.SetScript(plain); using var actorTree = new SceneTree(actor); Reject<InvalidOperationException>(() => Task.Run(() => actor.SetScript(null)).GetAwaiter().GetResult()); Check(actor.GetScript() == plain, "Foreign-thread binding mutation retains attached owner state."); actor.RPC(Plain, 1); actor.RPC(ScriptRPCActor.Add, 2); Check(actor.Score == 2 && actor.GetNodeRPCConfig().Length == 2, "Stable owner-class declarations remain as unshadowed defaults."); actor.SetScript(null); Check(actor.GetNodeRPCConfig().Length == 1, "Retirement restores owner-class defaults."); }
        using var world = new World(backend); var root = new SubViewport { Name = "Root", World = world, Size = new(100, 80) };
        var body = forward.New<RigidBody>(); body.Name = "Body"; body.GravityScale = 0; body.CanSleep = false; body.LinearDampMode = RigidBody.DampMode.Replace; body.LinearDamp = 0;
        using var shape = new CircleShape { Radius = 2 }; var collider = new CollisionShape { Shape = shape }; body.AddChild(collider); collider.Owner = body; root.AddChild(body);
        using var tree = new SceneTree(root); var identity = body.InstanceID; var rid = body.GetRID();
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Requested full solver without fallback.");
        var state = StateFor(body); body.RPC(LiveRPCForward.Drive, 2); tree.PhysicsFrame(1d / 60); Check(body.LinearVelocity.X == 2 && state.Sender == 1, "Offline live code RPC drives real physical state with restored sender scope.");
        var policy = new RPCOptions(RPCMode.AnyPeer, true, TransferMode.UnreliableOrdered, 1); body.RPCConfig(LiveRPCForward.Drive, policy); body.RPCConfig(Manual, Any);
        body.SetScript(reverse); var backwards = StateFor(body); Check(state.Releases == 1 && body.GetNodeRPCConfig().Single(r => r.Method.ID == 9911).Options == policy, "ID-based policies survive replacement without retaining the old method.");
        Reject<ArgumentException>(() => body.RPC(LiveRPCForward.Drive, 1)); body.RPC(LiveRPCReverse.Drive, 3); tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity.X == -3 && backwards.Value == -3 && body.InstanceID == identity && body.GetRID() == rid, "Replacement changes handlers while retaining physics identity.");
        body.RPC(Manual, 2); Check(_manualCalls == 2, "Explicit standalone methods retain their caller-owned lifetime.");
        body.RPCConfig(LiveRPCReverse.Drive); body.SetScript(forward); Reject<InvalidOperationException>(() => body.RPC(LiveRPCForward.Drive, 1));
        body.RPCConfig(LiveRPCForward.Drive, Any); body.RPC(LiveRPCForward.Drive, 1); Measure();
        using (var copy = (RigidBody)body.Duplicate()) Check(copy.GetNodeRPCConfig().Length == 3 && StateFor(copy) != StateFor(body), "Copies bind independent default methods; runtime-only overrides are not copied.");
        using (var edited = (Script)forward.Duplicate())
        using (var owner = edited.New<RigidBody>())
        { edited.CopyFromResource(reverse); Check(owner.GetNodeRPCConfig()[0].Method == LiveRPCForward.Drive, "Mutable/disposed source assets do not replace installed immutable RPC declarations."); edited.Dispose(); Check(owner.GetNodeRPCConfig().Length == 3, "Disposed borrowed source keeps active RPC defaults."); }
        body.SetScript(null); Check(body.GetNodeRPCConfig().Length == 1 && body.GetNodeRPCConfig()[0].Method == Manual, "Detachment removes script defaults without removing standalone methods.");
        FailBind = true; Expect(BindError, () => body.SetScript(forward)); FailBind = false; Check(body.GetNodeRPCConfig().Length == 1 && !States.TryGetValue(body, out _), "Failed binding retires partial defaults and state.");
        CheckBindGuard = true; body.SetScript(forward); CheckBindGuard = false; FailClear = true; Expect(ClearError, () => body.SetScript(reverse)); FailClear = false; Check(body.GetNodeRPCConfig().Length == 1, "Clear failure removes old source RPC defaults.");
        body.SetScript(forward); var alive = StateFor(body); alive.Veto = true; body.Dispose(); Check(!body.IsDisposed && StateFor(body) == alive, "Veto retains installed RPC code.");
        NetworkGuards(forward, reverse);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-live-rpc-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            using var scene = new PackedScene(); scene.Pack(body); var path = System.IO.Path.Combine(folder, "live.e2dscene"); ResourceSaver.Save(scene, path, SaverFlags.BundleResources);
            Cohort(backend, separate, false, path); Cohort(backend, separate, true, path);
        }
        finally { Directory.Delete(folder, true); }
        body.Dispose(); Check(alive.Releases == 1 && !States.TryGetValue(body, out _), "Committed disposal releases owner-local code state.");
        Console.WriteLine($"Live Script RPC {backend}, separate={separate}: metadata, code/policy lifetime, codec guards, separate CPU server/client transports and 0/0 physical frames passed.");

        void Measure()
        {
            void Frame() { body.RPC(LiveRPCForward.Drive, 1); tree.ProcessFrame(1d / 60); tree.PhysicsFrame(1d / 60); }
            for (var i = 0; i < 256; i++) Frame(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 128; i++) Frame(); var count = StateFor(body).Calls; var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            Check(StateFor(body).Calls == count + 128 && GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "128 live RPC/process/physics pairs execute with strict zero owner/all-thread managed bytes.");
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Expect(Exception expected, Action action) { try { action(); } catch (Exception error) when (ReferenceEquals(expected, error)) { return; } throw new InvalidOperationException("Missing original error: " + expected.Message); }
    private static string Source([CallerFilePath] string path = "") => path;
}
