using Electron2D;
using System.Runtime.CompilerServices;

internal static class PhysicsAutomaticCallbackTests
{
    private class Actor : CharacterBody
    {
        internal int Physics, Idle, InputCalls, Shortcut, KeyCalls, Unhandled, ReadyCount;
        internal bool ReadyFlags, DisableInReady;
        protected override void OnReady()
        {
            ReadyCount++; ReadyFlags = All(this);
            if (DisableInReady) Disable(this);
            base.OnReady();
        }
        protected override void OnPhysicsProcess(double delta) { Physics++; MoveAndSlide(); }
        protected override void OnProcess(double delta) { Idle++; }
        protected override void OnInput(InputEvent e) { InputCalls++; }
        protected override void OnShortcutInput(InputEvent e) { Shortcut++; }
        protected override void OnUnhandledKeyInput(InputEventKey e) { KeyCalls++; }
        protected override void OnUnhandledInput(InputEvent e) { Unhandled++; }
    }
    private sealed class InheritedActor : Actor
    {
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new InheritedActor();
    }
    private sealed class PhysicsOnly : CharacterBody
    {
        internal int Calls;
        protected override void OnPhysicsProcess(double delta) { Calls++; }
    }
    private class HiddenNode : Node
    {
        protected new void OnProcess(double delta) { throw new InvalidOperationException("Hidden method is not an override"); }
    }
    internal static void Run(PhysicsServer.Backend backend, bool threaded)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            Verify(); var weak = RetiredActor(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            Check(!weak.TryGetTarget(out _), "Callback mask cache never retains a node instance");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine($"Automatic six callback lanes: {backend}, threaded={threaded}, ready/inheritance/input/physics/lifecycle and zero warm bytes passed.");
        void Verify()
        {
            using var world = new World(backend); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
            var actor = new InheritedActor { Name = "Actor", Velocity = new(100, 0) }; actor.AddChild(new CollisionShape { Shape = shape });
            var disabled = new Actor { Name = "Disabled", DisableInReady = true, Position = new(100, 100) }; disabled.AddChild(new CollisionShape { Shape = shape });
            var only = new PhysicsOnly { Name = "Only", Position = new(300, 300) }; root.AddChild(only);
            var plain = new Node { Name = "Plain" }; var hidden = new HiddenNode { Name = "Hidden" }; root.AddChild(actor); root.AddChild(disabled); root.AddChild(plain); root.AddChild(hidden);
            Check(!All(actor) && !Any(actor), "Detached override lanes start disabled");
            using var tree = new SceneTree(root);
            Check(actor.ReadyFlags && disabled.ReadyFlags && All(actor), "All six overrides enable before inherited OnReady");
            Check(only.PhysicsProcessEnabled && !only.ProcessEnabled && !only.InputEnabled && !only.ShortcutInputEnabled && !only.UnhandledInputEnabled && !only.UnhandledKeyInputEnabled, "Only the actually overridden lane enables");
            Check(!Any(disabled) && !Any(plain) && !hidden.ProcessEnabled, "OnReady can disable; plain/hidden methods stay off");
            tree.PhysicsFrame(.03); Check(actor.Physics == 1 && MathF.Abs(actor.Position.X - 3) < .002f && disabled.Physics == 0, "An ordinary override receives a real selected physical frame without explicit flags");
            tree.ProcessFrame(.02); Check(actor.Idle == 1 && disabled.Idle == 0, "Ordinary idle override enables independently");
            using var key = new InputEventKey { Keycode = Key.F9, Pressed = true }; root.PushInput(key, true);
            Check(actor.InputCalls == 1 && actor.Shortcut == 1 && actor.KeyCalls == 1 && actor.Unhandled == 1 && disabled.InputCalls == 0, "Every automatic input stage receives the same unhandled key");
            Disable(actor); var before = actor.Position; tree.PhysicsFrame(.03); tree.ProcessFrame(.02); root.PushInput(key, true);
            Check(actor.Position == before && actor.Physics == 1 && actor.Idle == 1 && actor.InputCalls == 1, "Explicit post-ready disabling stays effective");
            root.RemoveChild(actor); root.AddChild(actor); Check(actor.ReadyCount == 1 && !Any(actor), "Ordinary reentry does not reenable consumed ready lanes");
            actor.RequestReady(); Check(!Any(actor), "RequestReady alone does not activate lanes"); root.RemoveChild(actor); root.AddChild(actor);
            Check(actor.ReadyCount == 2 && All(actor), "Requested ready repeats automatic override detection");
            Disable(actor); using (var packed = new PackedScene())
            {
                packed.Pack(actor); var restored = (InheritedActor)packed.Instantiate();
                Check(!Any(restored), "Stored disabled flags stay detached until ready"); restored.Name = "Restored"; root.AddChild(restored);
                Check(restored.ReadyFlags && All(restored), "Fresh packed instance enables inherited overrides at ready"); root.RemoveChild(restored); restored.Dispose();
            }
            actor.Notify(Node.NotificationReady);
            actor.ProcessMode = ProcessMode.Disabled; tree.PhysicsFrame(.01); Check(actor.Physics == 1, "Pause policy still gates automatic callbacks"); actor.ProcessMode = ProcessMode.Always;
            Check(world.PhysicsBackend == backend, "Explicit selected backend remains independent");
            Warm(() => { actor.Position = Vector2.Zero; tree.PhysicsFrame(.01); tree.ProcessFrame(.02); root.PushInput(key, true); });
            Disable(actor); actor.Notify(Node.NotificationReady); Check(All(actor) && actor.ReadyCount == 4, "Manual ready notification also applies automatic lanes without changing managed ready state");
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Actor> RetiredActor()
    {
        var actor = new Actor(); actor.Notify(Node.NotificationReady); actor.Dispose(); return new(actor);
    }
    private static bool All(Node n) => n.ProcessEnabled && n.PhysicsProcessEnabled && n.InputEnabled && n.ShortcutInputEnabled && n.UnhandledKeyInputEnabled && n.UnhandledInputEnabled;
    private static bool Any(Node n) => n.ProcessEnabled || n.PhysicsProcessEnabled || n.InputEnabled || n.ShortcutInputEnabled || n.UnhandledKeyInputEnabled || n.UnhandledInputEnabled;
    private static void Disable(Node n) { n.ProcessEnabled = n.PhysicsProcessEnabled = n.InputEnabled = n.ShortcutInputEnabled = n.UnhandledKeyInputEnabled = n.UnhandledInputEnabled = false; }
    private static void Warm(Action frame)
    {
        for (var i = 0; i < 128; i++) frame(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); for (var i = 0; i < 128; i++) frame();
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        if (own != 0 || all != 0) throw new InvalidOperationException($"Automatic callbacks allocated {own}/{all} B"); Console.WriteLine("Automatic full physics/idle/input: 128 warm/128 measured, 0/0 B.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
