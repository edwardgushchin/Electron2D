using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Electron2D;
using Path = System.IO.Path;

internal static class ButtonScriptTests
{
    private static bool _registered;
    private static readonly ConditionalWeakTable<BaseButton, State> States = new();
    private static void Register()
    {
        if (_registered) return;
        Script.RegisterBinding<BaseButton, ButtonScriptFirst>("Button.Script.First", ButtonScriptFirst.SourcePath, null, ButtonScriptFirst.Bind, ButtonScriptFirst.Clear, ButtonScriptFirst.Value);
        Script.RegisterBinding<BaseButton, ButtonScriptSecond>("Button.Script.Second", ButtonScriptSecond.SourcePath, null, ButtonScriptSecond.Bind, ButtonScriptSecond.Clear, ButtonScriptFirst.Value);
        _registered = true;
    }
    internal sealed class State(bool second)
    {
        internal readonly string Tag = second ? "second:" : "first:";
        internal readonly Color Color = second ? Colors.Blue : Colors.Red;
        internal readonly int Direction = second ? -1 : 1;
        internal readonly IOException Error = new("button script callback");
        internal int Presses, Toggles, Releases, Value, Draws;
        internal bool ThrowPressed, ThrowToggled, Veto;
        internal List<string>? Trace;
        internal Action<BaseButton>? DuringPressed, DuringToggled;
        internal RigidBody? Body;
        internal void Record(string phase) { if (Trace is not null) Trace.Add(Tag + phase); }
    }
    internal static State Get(BaseButton owner) => States.GetValue(owner, _ => throw new InvalidOperationException("No button script state."));
    internal static void Install(BaseButton owner, bool second)
    {
        var state = new State(second); States.Add(owner, state);
        owner.SetScriptCallbacks(new BaseButtonScriptCallbacks<BaseButton>
        {
            GetTooltip = (_, _) => state.Tag,
            Pressed = button =>
            {
                state.Presses++; state.Value++; state.Record("pressed");
                if (state.Body is { } body) body.LinearVelocity = new(12 * state.Direction, 0);
                var action = state.DuringPressed; state.DuringPressed = null; action?.Invoke(button);
                if (state.ThrowPressed) throw state.Error;
            },
            Toggled = (button, value) =>
            {
                state.Toggles++; state.Record(value ? "true" : "false");
                var action = state.DuringToggled; state.DuringToggled = null; action?.Invoke(button);
                if (state.ThrowToggled) throw state.Error;
            },
            Draw = button => { state.Draws++; button.DrawRect(new(0, 0, 20, 20), state.Color); },
            Notification = (button, what, reverse) => { if (what == ElectronObject.NotificationPreDelete && state.Veto) button.CancelDisposal(); },
            Retired = _ => { state.Releases++; States.Remove(owner); }
        });
    }
    private sealed class Probe : Button
    {
        internal readonly List<string> Trace = [];
        protected override void OnPressed() => Trace.Add("native:pressed");
        protected override void OnToggled(bool value) => Trace.Add(value ? "native:true" : "native:false");
    }
    internal static void Run(PhysicsServer.Backend backend, bool separate, string? renderer)
    {
        Register(); ScriptTests.VerifyAbstractBindingOwner(); InputMap.LoadFromProjectSettings();
        var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        if (renderer is not null) { ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); }
        try
        {
            using var first = ResourceLoader.Load<Script>(ButtonScriptFirst.SourcePath); using var second = ResourceLoader.Load<Script>(ButtonScriptSecond.SourcePath);
            Check(!first.CanInstantiate() && !second.CanInstantiate() && !first.IsAbstract(), "Abstract native roles remain attachment-only while compiled code is concrete.");
            using var world = new World(backend);
            using Viewport root = renderer is null ? new SubViewport { Size = new(144, 96), World = world } : new Window { Size = new(144, 96), World = world, Title = "Live Button Script" };
            if (root is Window native) native.OpenNative();
            try
            {
                using var button = new Button { Position = new(8, 8), Size = new(40, 24), ToggleMode = true }; root.AddChild(button); button.SetScript(first);
                Check(button.GetTooltip() == "first:", "Button sets retain inherited live Control query callbacks.");
                using var body = new RigidBody { Position = new(100, 70), GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
                using var shape = new CircleShape { Radius = 1 }; body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
                using var tree = new SceneTree(root); tree.ProcessFrame(0);
                VerifyOrder(root, first, second); VerifyFailures(first); VerifyLifecycle(first);
                Get(button).Value = 37; using (var copy = (Button)button.Duplicate()) Check(Get(copy).Value == 37 && !ReferenceEquals(Get(copy), Get(button)) && ReferenceEquals(copy.GetScript(), first), "Native derived copies bind independent state before storage restoration.");
                VerifyArchive(button);
                button.GrabFocus(); var state = Get(button); state.Body = body; var id = button.InstanceID; var rid = body.GetRID();
                using var action = new InputEventAction { Action = "ui_accept" };
                void Frame()
                {
                    action.Pressed = true; root.PushInput(action, inLocalCoordinates: true); action.Pressed = false; root.PushInput(action, inLocalCoordinates: true);
                    tree.ProcessFrame(1d / 120); tree.PhysicsFrame(1d / 120);
                }
                for (var i = 0; i < 256; i++) Frame(); AllocationWarmupCheck.Collect(); for (var i = 0; i < 128; i++) Frame();
                var presses = state.Presses; var toggles = state.Toggles; var start = body.GlobalPosition; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - own; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
                Check(ownerBytes == 0 && allBytes == 0 && state.Presses == presses + 128 && state.Toggles == toggles + 128 && body.GlobalPosition.X > start.X + 10,
                    $"Complete live button/process/physics frames: {ownerBytes}/{allBytes} bytes with exact activation counts and physical motion.");
                button.SetScript(second); var oldState = state; state = Get(button); state.Body = body; var previous = body.GlobalPosition;
                for (var i = 0; i < 4; i++) Frame();
                Check(oldState.Releases == 1 && state.Presses == 4 && body.GlobalPosition.X < previous.X && button.InstanceID == id && body.GetRID() == rid && world.PhysicsBackend == backend && world.PhysicsFallbackReason is null,
                    "Replacement changes actual physical input behavior on stable scene/server identities.");
                if (root is Window window) VerifyPixels(window, tree, button, first, second, renderer!);
                var retiring = Get(button); button.SetScript(null); Check(retiring.Releases == 1 && !States.TryGetValue(button, out _), "Detach retires the complete button set.");
                Console.WriteLine($"Button Script callbacks passed: {backend}, separate={separate}, renderer={renderer ?? "headless"}, 128 complete frames {ownerBytes}/{allBytes} bytes.");
            }
            finally { if (root is Window window) window.CloseNative(); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread); ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback); }
    }
    private static void VerifyOrder(Viewport root, Script first, Script second)
    {
        using var probe = new Probe { Size = new(40, 24), ToggleMode = true }; root.AddChild(probe); probe.SetScript(first); probe.GrabFocus(); var state = Get(probe); state.Trace = probe.Trace;
        probe.Pressed += () => probe.Trace.Add("signal:pressed"); probe.Toggled += value => probe.Trace.Add(value ? "signal:true" : "signal:false");
        using var input = new InputEventAction { Action = "ui_accept" };
        void Activate() { input.Pressed = true; root.PushInput(input, inLocalCoordinates: true); input.Pressed = false; root.PushInput(input, inLocalCoordinates: true); }
        Activate(); Check(probe.Trace.SequenceEqual(["first:true", "native:true", "signal:true", "first:pressed", "native:pressed", "signal:pressed"]), "Script, native hook and signal follow their activation order.");
        probe.Trace.Clear(); probe.SetBlockSignals(true); Activate(); Check(probe.Trace.SequenceEqual(["first:false", "native:false", "first:pressed", "native:pressed"]), "Signal blocking retains both direct button callbacks."); probe.SetBlockSignals(false);
        probe.Trace.Clear(); probe.SetPressedNoSignal(!probe.ButtonPressed); Check(probe.Trace.Count == 0, "Silent toggle assignment invokes no hook or signal.");
        probe.Trace.Clear(); state.ThrowToggled = state.ThrowPressed = true; Expect(state.Error, Activate); state.ThrowToggled = state.ThrowPressed = false;
        Check(probe.Trace.Contains("native:pressed") && probe.Trace.Contains("signal:pressed"), "Callback failures preserve remaining current native and signal phases.");
        probe.Trace.Clear(); state.DuringToggled = owner => { owner.SetScript(second); Get(owner).Trace = probe.Trace; }; Activate();
        Check(state.Releases == 1 && Get(probe).Presses == 1 && probe.Trace.Contains("second:pressed") && !probe.Trace.Contains("first:pressed"), "A toggle replacement selects new code for the later pressed phase.");
        probe.SetScript(first); state = Get(probe); state.Trace = probe.Trace; probe.SetPressedNoSignal(false); probe.Trace.Clear(); state.DuringToggled = owner => owner.ButtonPressed = false; probe.ButtonPressed = true;
        Check(probe.Trace.SequenceEqual(["first:true", "first:false", "native:false", "signal:false"]), "A nested newer toggle prevents stale native/signal phases.");
        probe.SetScript(first); // Equality preserves the installed state.
        state.DuringPressed = owner => owner.SetScript(null); Activate(); Check(state.Releases == 1 && probe.GetScript() is null, "Detachment inside the active pressed callback is committed.");
        using var doomed = new Probe { Size = new(40, 24), ToggleMode = true }; root.AddChild(doomed); doomed.SetScript(first); var dying = Get(doomed); dying.DuringToggled = owner => owner.Dispose();
        doomed.ButtonPressed = true; Check(dying.Releases == 1 && doomed.Trace.Count == 0, "Disposal from Script skips old native and signal phases.");
    }
    private static void VerifyFailures(Script first)
    {
        using var button = new Button(); ButtonScriptFirst.FailBind = true;
        try { Expect(ButtonScriptFirst.Error, () => button.SetScript(first)); } finally { ButtonScriptFirst.FailBind = false; }
        Check(button.GetScript() is null && !States.TryGetValue(button, out _), "Partial installation retires all button slots.");
        button.SetScript(first); var state = Get(button); ButtonScriptFirst.FailClear = true;
        try { Expect(ButtonScriptFirst.Error, () => button.SetScript(null)); } finally { ButtonScriptFirst.FailClear = false; }
        Check(button.GetScript() is null && state.Releases == 1, "Clear failure still retires button callbacks.");
    }
    private static void VerifyLifecycle(Script first)
    {
        using var button = new Button(); button.SetScript(first); var state = Get(button); state.Veto = true; button.Dispose();
        Check(!button.IsDisposed && ReferenceEquals(button.GetScript(), first) && state.Releases == 0, "Cancelled pre-delete retains the installed button set.");
        state.Veto = false; button.Dispose(); Check(state.Releases == 1, "Committed disposal retires button state once.");
    }
    private static void VerifyArchive(Button button)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-button-script-scene-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            using var packed = new PackedScene(); packed.Pack(button); var path = Path.Combine(folder, "button.e2dscene"); ResourceSaver.Save(packed, path, SaverFlags.BundleResources);
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location); start.ArgumentList.Add("--button-script-child"); start.ArgumentList.Add(path);
            foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("ELECTRON2D_TEST_", StringComparison.Ordinal)).ToArray()) start.Environment.Remove(key);
            start.Environment["SDL_VIDEODRIVER"] = "dummy"; start.Environment["VK_DRIVER_FILES"] = start.Environment["VK_ICD_FILENAMES"] = "/nonexistent/button-script.json";
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            try { if (!process.WaitForExit(30000)) throw new TimeoutException("Button Script archive worker."); Check(process.ExitCode == 0 && output.Result.Contains("Button Script fresh archive passed.", StringComparison.Ordinal), output.Result + error.Result); }
            finally { if (!process.HasExited) { process.Kill(true); process.WaitForExit(5000); } }
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void Child(string path)
    {
        Register(); using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var button = (Button)packed.Instantiate();
        Check(button.GetType() == typeof(Button) && Get(button).Value == 37 && button.GetScript() is { } source && !source.CanInstantiate(), "Fresh archive binds an abstract role to its concrete native owner before stored values.");
        var before = Get(button).Toggles; button.ButtonPressed = !button.ButtonPressed; Check(Get(button).Toggles == before + 1, "Restored native button executes real code.");
        Console.WriteLine("Button Script fresh archive passed.");
    }
    private static void VerifyPixels(Window window, SceneTree tree, Button button, Script first, Script second, string renderer)
    {
        Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "Selected native renderer without fallback."); RenderingServer.SetDefaultClearColor(Colors.Black);
        button.SetScript(first); Pixel(Colors.Red); button.SetScript(second); Pixel(Colors.Blue);
        var state = Get(button); for (var i = 0; i < 64; i++) { button.QueueRedraw(); window.Render(tree, 1d / 120); }
        AllocationWarmupCheck.Collect(); for (var i = 0; i < 64; i++) { button.QueueRedraw(); window.Render(tree, 1d / 120); }
        var draws = state.Draws; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) { button.QueueRedraw(); window.Render(tree, 1d / 120); }
        Check(GC.GetAllocatedBytesForCurrentThread() == own && GC.GetTotalAllocatedBytes(true) == all && state.Draws == draws + 64, "Live native button redraw remains zero owner/all-thread managed bytes.");
        void Pixel(Color expected)
        {
            window.Render(tree, 1d / 120); using var image = window.GetTexture().GetImage()!; var pixel = image.GetPixel(12, 12);
            Check(Math.Abs(pixel.R - expected.R) < .05f && Math.Abs(pixel.G - expected.G) < .05f && Math.Abs(pixel.B - expected.B) < .05f, "Native button pixels follow the selected live code.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_BUTTON_SCRIPT_OUTPUT") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(Path.Combine(folder, renderer + "-" + expected.R + "-" + expected.B + ".png")); }
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Expect(Exception expected, Action action)
    {
        try { action(); } catch (Exception actual) when (ReferenceEquals(actual, expected) || actual is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, expected))) { return; }
        throw new InvalidOperationException("Expected original callback error.");
    }
}
