using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Electron2D;
using Path = System.IO.Path;

internal static class ControlScriptTests
{
    private static bool _registered;
    private static readonly ConditionalWeakTable<Control, State> States = new();
    private static Control Create() => new();
    private static void Register()
    {
        if (_registered) return;
        Script.RegisterBinding<Control, ControlScriptFirst>("Control.Script.First", ControlScriptFirst.SourcePath, Create, ControlScriptFirst.Bind, ControlScriptFirst.Clear, ControlScriptFirst.Value);
        Script.RegisterBinding<Control, ControlScriptSecond>("Control.Script.Second", ControlScriptSecond.SourcePath, Create, ControlScriptSecond.Bind, ControlScriptSecond.Clear, ControlScriptFirst.Value);
        _registered = true;
    }
    internal sealed class State(bool second)
    {
        internal readonly Color Color = second ? Colors.Blue : Colors.Red;
        internal Vector2 Minimum = second ? new(20, 22) : new(12, 14), Maximum = new(80, 90);
        internal CursorShape Cursor = CursorShape.PointingHand;
        internal readonly string Tooltip = second ? "second" : "first";
        internal readonly DragPayload<int> Payload = new(19);
        internal readonly TextBIDIRange[] Ranges = [new(0, 2, TextDirection.RTL)];
        internal readonly List<string> Trace = [];
        internal readonly List<Vector2> DragPositions = [];
        internal readonly IOException Error = new("control script callback");
        internal bool TraceEnabled, Accept, FailInput, InvalidRanges, NullContext, FailText, HasPoint = true;
        internal int Inputs, Texts, Compositions, Drops, Releases, Value, Draws;
        internal Action<Control>? DuringInput;
        internal void Record(string text) { if (TraceEnabled) Trace.Add(text); }
    }
    internal static State Get(Control owner) => States.GetValue(owner, _ => throw new InvalidOperationException("No live control state."));
    internal static void Install(Control owner, bool second)
    {
        var state = new State(second); States.Add(owner, state);
        owner.SetScriptCallbacks(new ControlScriptCallbacks<Control>
        {
            GetMinimumSize = _ => state.Minimum,
            GetMaximumSize = _ => state.Maximum,
            HasPoint = (_, _) => state.HasPoint,
            GetCursorShape = (_, _) => state.Cursor,
            GetTooltip = (_, _) => state.Tooltip,
            GetTooltipAutoTranslateModeAt = (_, _) => NodeAutoTranslateMode.Disabled,
            MakeCustomTooltip = (_, text) => new Control { Name = text, Size = new(20, 10) },
            StructuredTextParser = (_, _, _) => state.InvalidRanges ? [new(-1, 20, TextDirection.RTL)] : state.Ranges,
            GUIInput = (control, input) => { state.Inputs++; state.Record("script"); state.DuringInput?.Invoke(control); if (state.Accept) control.AcceptEvent(); if (state.FailInput) throw state.Error; },
            GetDragData = (_, position) => { if (state.TraceEnabled) state.DragPositions.Add(position); return state.Payload; },
            CanDropData = (_, position, payload) => { if (state.TraceEnabled) state.DragPositions.Add(position); return ReferenceEquals(payload, state.Payload); },
            DropData = (_, position, _) => { state.Drops++; if (state.TraceEnabled) state.DragPositions.Add(position); },
            AccessibilityGetContextualInfo = _ => state.NullContext ? null! : "context",
            GetAccessibilityContainerName = (_, child) => child.Name,
            TextInput = (_, text) => { state.Texts++; state.Record("text"); if (state.FailText) throw state.Error; },
            IMECompositionChanged = (_, text, selection) => { state.Compositions++; state.Record("ime"); },
            Draw = control => { state.Draws++; control.DrawRect(new(0, 0, 20, 20), state.Color); },
            Retired = _ => { state.Releases++; States.Remove(owner); }
        });
    }
    private sealed class NativeProbe : Control
    {
        internal readonly List<string> Trace = [];
        protected override Vector2 OnGetMinimumSize() => new(91, 92);
        protected override Vector2 OnGetMaximumSize() => new(191, 192);
        protected override string OnGetTooltip(Vector2 position) => "native";
        protected override void OnGUIInput(InputEvent input) => Trace.Add("native");
    }
    internal static void Run(PhysicsServer.Backend backend, bool separate, string? renderer)
    {
        Register();
        var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        if (renderer is not null) { ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); }
        try
        {
            using var first = ResourceLoader.Load<Script>(ControlScriptFirst.SourcePath); using var second = ResourceLoader.Load<Script>(ControlScriptSecond.SourcePath);
            using var world = new World(backend);
            using Viewport root = renderer is null ? new SubViewport { Size = new(144, 96), World = world } : new Window { Size = new(144, 96), World = world, Title = "Live Control Script" };
            if (root is Window native) native.OpenNative();
            try
            {
                using var owner = first.New<Control>(); owner.Name = "Live"; owner.Position = new(8, 8); owner.Size = new(24, 24); owner.FocusMode = FocusMode.All;
                root.AddChild(owner);
                using var body = new RigidBody { Position = new(100, 70), GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, LinearVelocity = new(1, 0) };
                using var circle = new CircleShape { Radius = 1 }; body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
                using var tree = new SceneTree(root); tree.ProcessFrame(0); owner.GrabFocus();
                var state = Get(owner); VerifyQueries(owner, state); VerifyInput(root, owner, first, second); VerifyNativeQueries(root, first); owner.GrabFocus(); VerifyFailures(first);
                owner.SetScript(first); state = Get(owner); state.Value = 37;
                using (var duplicate = (Control)owner.Duplicate()) Check(Get(duplicate).Value == 37 && !ReferenceEquals(Get(duplicate), state) && ReferenceEquals(duplicate.GetScript(), first), "Copies bind independent state before typed storage restoration.");
                VerifyArchive(owner);
                using var key = new InputEventKey { Pressed = true };
                var ranges = new List<TextBIDIRange>(4);
                void Frame()
                {
                    _ = owner.GetMinimumSize(); _ = owner.GetMaximumSize(); _ = owner.GetTooltip(); _ = owner.GetCursorShape();
                    _ = owner.HitTest(new(9, 9)); _ = owner.AccessibleContextualInfo(); _ = owner.AccessibleContainerName(body);
                    owner.ParseStructuredText(StructuredTextParser.Custom, Array.Empty<string>(), "ab", ranges);
                    _ = owner.GetDragData(default); _ = owner.CanDropData(default, state.Payload); owner.DropData(default, state.Payload);
                    owner.DispatchTextInput("ab"); owner.DispatchIMEComposition("ab", new(0, 1)); root.PushInput(key, inLocalCoordinates: true);
                    tree.ProcessFrame(1d / 60); tree.PhysicsFrame(1d / 60);
                }
                for (var i = 0; i < 256; i++) Frame(); AllocationWarmupCheck.Collect(); for (var i = 0; i < 128; i++) Frame();
                var texts = state.Texts; var compositions = state.Compositions; var inputs = state.Inputs; var drops = state.Drops;
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - own; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
                Check(ownerBytes == 0 && allBytes == 0 && body.GlobalPosition.X > 101 && world.PhysicsBackend == backend && world.PhysicsFallbackReason is null,
                    $"Complete Control script/process/physics frames: {ownerBytes}/{allBytes} bytes, selected solver and moving body.");
                Check(state.Texts == texts + 128 && state.Compositions == compositions + 128 && state.Inputs == inputs + 128 && state.Drops == drops + 128, "Every measured frame executes text, IME, GUI input and drag delivery.");
                if (root is Window window) VerifyPixels(window, tree, owner, first, second, renderer!);
                var retiring = Get(owner); owner.SetScript(null); Check(retiring.Releases == 1 && owner.GetMinimumSize() == Vector2.Zero && owner.GetDragData(default) is null, "Detach clears every control slot.");
                Console.WriteLine($"Control Script callbacks passed: {backend}, separate={separate}, renderer={renderer ?? "headless"}, 128 complete frames {ownerBytes}/{allBytes} bytes.");
            }
            finally { if (root is Window nativeWindow) nativeWindow.CloseNative(); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread); ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback); }
    }
    private static void VerifyQueries(Control owner, State state)
    {
        Check(owner.GetMinimumSize() == state.Minimum && owner.GetMaximumSize() == state.Maximum && owner.GetCursorShape() == CursorShape.PointingHand && owner.HitTest(new(9, 9)), "Live native Control layout, cursor and hit queries execute.");
        Check(owner.GetTooltip() == "first" && owner.GetTooltipTranslationMode(default) == NodeAutoTranslateMode.Disabled, "Tooltip queries use live code.");
        using (var tip = owner.CreateTooltipControl("custom")!) Check(tip.Name == "custom" && tip.Parent is null && tip.Tree is null, "Custom tooltip returns a detached transferred control.");
        var ranges = new List<TextBIDIRange>(); owner.ParseStructuredText(StructuredTextParser.Custom, Array.Empty<string>(), "ab", ranges);
        Check(ranges.SequenceEqual(state.Ranges) && owner.AccessibleContextualInfo() == "context" && owner.AccessibleContainerName(owner) == "Live", "Structured text and semantic queries execute.");
        state.HasPoint = false; Check(!owner.HitTest(new(9, 9)), "A false live hit result does not fall back to the native rectangle."); state.HasPoint = true;
        state.TraceEnabled = true;
        Check(ReferenceEquals(owner.GetDragData(default), state.Payload) && owner.CanDropData(default, state.Payload), "Pointer drag uses the typed script producer and target."); owner.DropData(default, state.Payload);
        Check(ReferenceEquals(owner.GetAccessibilityDragData(), state.Payload) && owner.CanAccessibilityDrop(state.Payload), "Accessible drag shares the script hooks."); owner.DropAccessibilityData(state.Payload);
        Check(state.DragPositions.SequenceEqual([default, default, default, Control.AccessibilityDragPosition, Control.AccessibilityDragPosition, Control.AccessibilityDragPosition]), "Pointer and accessible hooks preserve their distinct positions.");
        owner.SetDragForwarding(_ => null, (_, _) => false, (_, _) => state.Record("forward"));
        Check(owner.GetDragData(default) is null && !owner.CanDropData(default, state.Payload) && owner.GetAccessibilityDragData() is null, "Supplied forwarding rejects without falling back to scripts.");
        owner.DropData(default, state.Payload); Check(state.Trace.SequenceEqual(["forward"]), "Forwarding delivery overrides the script slot."); owner.SetDragForwarding(null, null, null); state.TraceEnabled = false;
        state.Minimum = new(float.NaN, 1); Reject<ArgumentException>(() => owner.GetMinimumSize()); state.Minimum = new(12, 14);
        state.Maximum = new(1, float.PositiveInfinity); Reject<ArgumentException>(() => owner.GetMaximumSize()); state.Maximum = new(80, 90);
        state.Cursor = (CursorShape)999; Reject<ArgumentOutOfRangeException>(() => owner.GetCursorShape()); state.Cursor = CursorShape.PointingHand;
        state.InvalidRanges = true; Reject<InvalidOperationException>(() => owner.ParseStructuredText(StructuredTextParser.Custom, Array.Empty<string>(), "ab", ranges)); state.InvalidRanges = false;
        state.NullContext = true; Reject<InvalidOperationException>(() => owner.AccessibleContextualInfo()); state.NullContext = false;
    }
    private static void VerifyInput(Viewport root, Control owner, Script first, Script second)
    {
        using var key = new InputEventKey { Pressed = true }; var state = Get(owner); state.TraceEnabled = true; state.Trace.Clear();
        var signalAccept = false; Action<InputEvent> signal = _ => { state.Record("signal"); if (signalAccept) owner.AcceptEvent(); };
        owner.GUIInput += signal;
        root.PushInput(key, inLocalCoordinates: true); Check(state.Trace.SequenceEqual(["signal", "script"]), "Signal precedes the live input hook.");
        state.Trace.Clear(); signalAccept = true; root.PushInput(key, inLocalCoordinates: true); Check(state.Trace.SequenceEqual(["signal"]), "Signal acceptance prevents script input.");
        state.Trace.Clear(); signalAccept = false; owner.SetBlockSignals(true); root.PushInput(key, inLocalCoordinates: true); Check(state.Trace.SequenceEqual(["script"]), "Signal blocking retains direct script input."); owner.SetBlockSignals(false);
        state.FailInput = true; Expect(state.Error, () => root.PushInput(key, inLocalCoordinates: true)); state.FailInput = false;
        state.FailText = true; var textSignals = 0; Action<string> textSignal = _ => textSignals++; owner.TextInput += textSignal;
        Expect(state.Error, () => owner.DispatchTextInput("ab")); Check(textSignals == 1, "Text signal remains attempted after a script text error."); owner.TextInput -= textSignal; state.FailText = false;
        state.DuringInput = control => control.SetScript(second); root.PushInput(key, inLocalCoordinates: true);
        Check(state.Releases == 1 && owner.GetTooltip() == "second", "Replacement inside an active input hook retires old state.");
        owner.GUIInput -= signal; owner.SetScript(first); state = Get(owner); state.DuringInput = control => control.SetScript(null); root.PushInput(key, inLocalCoordinates: true);
        Check(state.Releases == 1 && owner.GetScript() is null, "Detachment inside an input hook is committed.");
        owner.SetScript(first); var latest = Get(owner); Action<InputEvent> change = _ => owner.SetScript(second); owner.GUIInput += change; root.PushInput(key, inLocalCoordinates: true); owner.GUIInput -= change;
        Check(latest.Inputs == 0 && latest.Releases == 1 && Get(owner).Inputs == 1, "A signal's replacement selects current script code for the same event.");
        using var native = new NativeProbe { Size = new(24, 24), FocusMode = FocusMode.All }; root.AddChild(native); native.SetScript(first); native.GrabFocus();
        var nativeState = Get(native); nativeState.Accept = true; root.PushInput(key, inLocalCoordinates: true);
        Check(nativeState.Inputs == 1 && native.Trace.Count == 0, "Live acceptance prevents the native derived hook.");
        nativeState.Accept = false; root.PushInput(key, inLocalCoordinates: true);
        Check(nativeState.Inputs == 2 && native.Trace.Count == 1, "Without acceptance the native derived hook runs once.");
        using var doomed = new NativeProbe { Size = new(24, 24), FocusMode = FocusMode.All }; root.AddChild(doomed); doomed.SetScript(first); doomed.GrabFocus(); var doomedState = Get(doomed);
        doomed.GUIInput += _ => doomed.Dispose(); root.PushInput(key, inLocalCoordinates: true); Check(doomedState.Inputs == 0 && doomedState.Releases == 1 && doomed.Trace.Count == 0, "Disposal in a signal prevents script/native delivery.");
        owner.GrabFocus();
    }
    private static void VerifyNativeQueries(Viewport root, Script first)
    {
        using var native = new NativeProbe(); native.SetScript(first); var state = Get(native);
        Check(native.GetMinimumSize() == state.Minimum && native.GetMaximumSize() == state.Maximum && native.GetTooltip() == "first", "Live queries precede native/project sibling overrides.");
        native.SetScript(null); Check(native.GetMinimumSize() == new Vector2(91, 92) && native.GetMaximumSize() == new Vector2(191, 192) && native.GetTooltip() == "native", "Retirement restores native query hooks.");
        using var button = new Button { Size = new(24, 24) }; button.SetScript(first); Check(button.GetMinimumSize() == Get(button).Minimum, "Native button layout honors the installed Control role.");
        button.Position = new(40, 8); root.AddChild(button); var pressed = 0; button.Pressed += () => pressed++;
        using var click = new InputEventMouseButton { Position = new(45, 12), ButtonIndex = MouseButton.Left, Pressed = true };
        root.PushInput(click, inLocalCoordinates: true); Get(button).HasPoint = false;
        using (var motion = new InputEventMouseMotion { Position = click.Position }) root.PushInput(motion, inLocalCoordinates: true);
        click.Pressed = false; root.PushInput(click, inLocalCoordinates: true);
        Check(pressed == 0, "Captured native button motion uses the live hit query.");
        Get(button).HasPoint = true; click.Pressed = true; root.PushInput(click, inLocalCoordinates: true); click.Pressed = false; root.PushInput(click, inLocalCoordinates: true);
        Check(pressed == 1, "Native button activation survives the non-accepting live input hook."); button.SetScript(null);
    }
    private static void VerifyFailures(Script first)
    {
        using var control = new Control(); ControlScriptFirst.FailBind = true;
        try { Expect(ControlScriptFirst.Error, () => control.SetScript(first)); } finally { ControlScriptFirst.FailBind = false; }
        Check(control.GetScript() is null && control.GetMinimumSize() == Vector2.Zero && !States.TryGetValue(control, out _), "Partial installation retires every control slot and state.");
        control.SetScript(first); var state = Get(control); ControlScriptFirst.FailClear = true;
        try { Expect(ControlScriptFirst.Error, () => control.SetScript(null)); } finally { ControlScriptFirst.FailClear = false; }
        Check(control.GetScript() is null && state.Releases == 1 && control.GetTooltip() == "", "A clear failure cannot retain retired control hooks.");
    }
    private static void VerifyArchive(Control owner)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-script-control-scene-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            using var packed = new PackedScene(); packed.Pack(owner); var path = Path.Combine(folder, "control.e2dscene"); ResourceSaver.Save(packed, path, SaverFlags.BundleResources);
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location); start.ArgumentList.Add("--control-script-child"); start.ArgumentList.Add(path);
            foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("ELECTRON2D_TEST_", StringComparison.Ordinal)).ToArray()) start.Environment.Remove(key);
            start.Environment["SDL_VIDEODRIVER"] = "dummy"; start.Environment["VK_DRIVER_FILES"] = start.Environment["VK_ICD_FILENAMES"] = "/nonexistent/control-script.json";
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            try { if (!process.WaitForExit(30000)) throw new TimeoutException("Control Script archive worker."); Check(process.ExitCode == 0 && output.Result.Contains("Control Script fresh archive passed.", StringComparison.Ordinal), output.Result + error.Result); }
            finally { if (!process.HasExited) { process.Kill(true); process.WaitForExit(5000); } }
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void Child(string path)
    {
        Register(); using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var owner = (Control)packed.Instantiate();
        Check(owner.GetType() == typeof(Control) && owner.GetMinimumSize() == new Vector2(12, 14) && Get(owner).Value == 37, "Fresh archive restores native owner, binding and typed script value.");
        using var root = new SubViewport(); root.AddChild(owner); using var tree = new SceneTree(root); tree.PhysicsFrame(1d / 60);
        Console.WriteLine("Control Script fresh archive passed.");
    }
    private static void VerifyPixels(Window window, SceneTree tree, Control owner, Script first, Script second, string renderer)
    {
        Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "Native window uses the selected renderer without fallback."); RenderingServer.SetDefaultClearColor(Colors.Black);
        NativeText();
        for (var i = 0; i < 64; i++) { owner.QueueRedraw(); window.Render(tree, 1d / 60); }
        AllocationWarmupCheck.Collect(); for (var i = 0; i < 64; i++) { owner.QueueRedraw(); window.Render(tree, 1d / 60); }
        var state = Get(owner); var draws = state.Draws; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) { owner.QueueRedraw(); window.Render(tree, 1d / 60); }
        Check(GC.GetAllocatedBytesForCurrentThread() == own && GC.GetTotalAllocatedBytes(true) == all && state.Draws == draws + 64, "Prepared live canvas redraw allocates zero on owner/all threads.");
        draws = state.Draws; own = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) window.Render(tree, 1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() == own && GC.GetTotalAllocatedBytes(true) == all && state.Draws == draws, "Prepared retained rendering allocates zero and does not invoke live drawing again.");
        Draw(Colors.Red); owner.SetScript(second); Draw(Colors.Blue); owner.SetScript(null); Draw(Colors.Black); owner.SetScript(first);
        void NativeText()
        {
            var windows = SDL3.SDL.GetWindows(out var count); Check(count == 1 && windows is { Length: 1 }, "The control scenario owns one native window.");
            owner.GrabFocus(); window.SetIMEActive(true); var id = SDL3.SDL.GetWindowID(windows![0]);
            var preedit = Marshal.StringToCoTaskMemUTF8("a🙂"); var commit = Marshal.StringToCoTaskMemUTF8("é👩‍🚀");
            try
            {
                var editing = new SDL3.SDL.Event { Edit = new SDL3.SDL.TextEditingEvent { Type = SDL3.SDL.EventType.TextEditing, WindowID = id, Text = preedit, Start = 1, Length = 1 } };
                var text = new SDL3.SDL.Event { Text = new SDL3.SDL.TextInputEvent { Type = SDL3.SDL.EventType.TextInput, WindowID = id, Text = commit } };
                var before = Get(owner); var texts = before.Texts; var compositions = before.Compositions;
                Check(SDL3.SDL.PushEvent(ref editing) && SDL3.SDL.PushEvent(ref text), "The native queue accepts live Control preedit and commit."); window.PumpEvents();
                Check(before.Texts == texts + 1 && before.Compositions >= compositions + 1, "Native text and IME reach the installed Control callbacks.");
            }
            finally { Marshal.FreeCoTaskMem(preedit); Marshal.FreeCoTaskMem(commit); window.SetIMEActive(false); }
        }
        void Draw(Color expected)
        {
            window.Render(tree, 1d / 60); using var image = window.GetTexture().GetImage()!;
            var pixel = image.GetPixel(12, 12); Check(Math.Abs(pixel.R - expected.R) < .05f && Math.Abs(pixel.G - expected.G) < .05f && Math.Abs(pixel.B - expected.B) < .05f, "Real native pixels follow live binding and retirement.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_CONTROL_SCRIPT_OUTPUT") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(Path.Combine(folder, renderer + "-" + expected.R + "-" + expected.B + ".png")); }
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Expect(Exception expected, Action action)
    {
        try { action(); } catch (Exception actual) when (ReferenceEquals(actual, expected) || actual is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, expected))) { return; }
        throw new InvalidOperationException("Expected the original callback error.");
    }
}
