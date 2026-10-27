using Electron2D;

internal static class ControlFocusContracts
{
    private static int _checks;
    private static readonly List<string> Failures = [];
    private static void Check(bool value, string message) { _checks++; if (!value) Failures.Add(message); }
    private static Control C(Node parent, string name, float x = 0, float y = 0, FocusMode mode = FocusMode.All)
    { var c = new Control { Name = name, Position = new(x, y), Size = new(10, 10), FocusMode = mode }; parent.AddChild(c); return c; }
    private static string Name(Control? c) => c?.Name ?? "null";
    private static void Query(string label, Control c, string next, string previous, string right, string down)
    {
        var actual = string.Join(",", Name(c.FindNextValidFocus()), Name(c.FindPrevValidFocus()), Name(c.FindValidFocusNeighbor(Side.Right)), Name(c.FindValidFocusNeighbor(Side.Bottom)));
        Check(actual == string.Join(",", next, previous, right, down), label + ": " + actual);
    }
    private static SceneTree Tree(out Window window)
    {
        var host = new SubViewport { Size = new(800, 600), GUIEmbedSubwindows = true };
        window = new Window { Name = "window", Size = new(800, 600), Borderless = true }; host.AddChild(window);
        return new SceneTree(host);
    }
    internal static void Run(string profile)
    {
        if (profile != "managed") { Native(profile); return; }
        using (var tree = Tree(out var window))
        {
            var root = C(window, "r", mode: FocusMode.None); root.Size = new(600, 400);
            var a = C(root, "a"); var b = C(root, "b", 40); C(root, "click", 60, mode: FocusMode.Click);
            Query("basic_a", a, "b", "b", "b", "null"); Query("basic_b", b, "a", "a", "null", "null");
            a.FocusNeighborRight = "../click"; Query("explicit_click", a, "b", "b", "click", "null");
            a.FocusNeighborRight = "../b"; b.FocusMode = FocusMode.None; b.FocusNeighborRight = "../a";
            Query("eligible_cycle", a, "a", "a", "a", "null");
            b.FocusNeighborRight = "."; Query("ineligible_cycle", a, "a", "a", "null", "null");
            b.FocusNeighborRight = ""; C(root, "beyond", 80); Query("chain_rebase", a, "beyond", "beyond", "beyond", "null");
            a.FocusNeighborRight = ""; b.FocusMode = FocusMode.All;
            var neutral = new Node { Name = "neutral" }; root.AddChild(neutral); var child = C(neutral, "neutral_child", 20);
            Query("neutral_source", child, "neutral_child", "neutral_child", "null", "null");
            Query("neutral_skipped", a, "b", "beyond", "b", "null");
            var top = C(root, "top", 20, 20); top.TopLevel = true; var topa = C(top, "topa"); var topb = C(top, "topb", 20);
            Query("top_source", top, "topa", "topb", "topb", "null"); Query("topa", topa, "topb", "top", "topb", "null");
            Query("topb", topb, "top", "topa", "null", "null"); Query("normal_skips_top", a, "b", "beyond", "b", "null");
            a.Hide(); Query("hidden_source", a, "b", "beyond", "b", "null"); a.Show();
        }
        using (var tree = Tree(out var window))
        {
            var left = C(window, "left", mode: FocusMode.None); left.Size = new(100, 100);
            var right = C(window, "right", 200, mode: FocusMode.None); right.Size = new(100, 100);
            var first = C(left, "first"); var second = C(right, "second");
            Query("window_forest_first", first, "second", "second", "null", "null"); Query("window_forest_second", second, "first", "first", "null", "null");
            var vp = new SubViewport { Name = "vp", Size = new(100, 100) }; window.AddChild(vp);
            var inner = C(vp, "inner1"); C(vp, "inner2", 20); Query("viewport_forest_inner1", inner, "inner1", "inner1", "null", "null");
            first.FocusNext = first.GetPathTo(inner); first.FocusNeighborRight = first.GetPathTo(inner);
            Query("explicit_cross_viewport", first, "inner1", "second", "inner1", "null");
            first.GrabFocus(); using var tab = new InputEventKey { Pressed = true, Keycode = Key.Tab }; window.PushInput(tab, true);
            Check(inner.HasFocus() && !first.HasFocus() && inner.HasFocus(true), "Input applies an explicit target in another viewport and reveals focus.");
            inner.ReleaseFocus(); first.GrabFocus(); first.FocusExited += () => throw new InvalidOperationException("focus exit probe");
            Reject<AggregateException>(() => window.PushInput(tab, true));
            Check(inner.HasFocus() && !first.HasFocus(), "A failing source exit still transfers focus across viewport contexts.");
        }
        using (var detached = new Control { Name = "detached", FocusMode = FocusMode.All })
        {
            Query("detached", detached, "null", "detached", "null", "null"); detached.TopLevel = true;
            Query("detached_top", detached, "detached", "detached", "null", "null");
        }
        Geometry(); PathsAndLifetime();
        Finish("managed");
    }
    private static void Geometry()
    {
        using (var tree = Tree(out var window))
        {
            var root = C(window, "outer", mode: FocusMode.None); root.Size = new(400, 400); var source = C(root, "source");
            var bridge = new Entity(); root.AddChild(bridge); var bridged = C(bridge, "bridged", 20);
            Query("canvas_bridge_source", source, "source", "source", "bridged", "null"); Query("canvas_bridge_child", bridged, "null", "bridged", "null", "null");
            var implementation = new Control { Name = "internal", Position = new(11, 0), Size = new(10, 10), FocusMode = FocusMode.All };
            root.AddChild(implementation, internalMode: Node.InternalMode.Front);
            Query("internal_included", source, "internal", "internal", "internal", "null");
            implementation.FocusMode = FocusMode.None; source.FocusMode = FocusMode.None;
            Check(source.FindNextValidFocus() is null && source.FindPrevValidFocus() is null, "A nonfocusable cycle terminates even when a spatial bridge has an eligible descendant.");
        }
        using (var tree = Tree(out var window))
        {
            var root = C(window, "outer", mode: FocusMode.None); root.Size = new(400, 400); var source = C(root, "source");
            C(root, "diagonal", 10, 10); var aligned = C(root, "aligned", 10);
            Query("spatial_tie", source, "diagonal", "aligned", "aligned", "diagonal");
            aligned.Position = new(0.000001f, 0); Query("epsilon_excluded", source, "diagonal", "aligned", "diagonal", "diagonal");
            root.ClipContents = true; root.Size = new(10, 10); Query("ordinary_clip_ignored", source, "diagonal", "aligned", "diagonal", "diagonal");
            root.ClipContents = false; aligned.Position = new(10, 0);
            for (var i = 0; i < 256; i++) { _ = source.FindNextValidFocus(); _ = source.FindPrevValidFocus(); _ = source.FindValidFocusNeighbor(Side.Right); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 256; i++) { _ = source.FindNextValidFocus(); _ = source.FindPrevValidFocus(); _ = source.FindValidFocusNeighbor(Side.Right); }
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(bytes == 0, $"768 prepared sequential/spatial queries allocate {bytes} owner bytes.");
        }
        using (var tree = Tree(out var window))
        {
            var root = C(window, "outer", mode: FocusMode.None); root.Size = new(400, 400);
            var scroll = new ScrollContainer { Name = "scroll", Size = new(100, 80) }; root.AddChild(scroll);
            var content = C(scroll, "content", mode: FocusMode.None); content.CustomMinimumSize = new(100, 300);
            var source = C(content, "source", 10, 10); var offscreen = C(content, "offscreen", 10, 180); C(root, "outside", 10, 120);
            Engine.Start(tree);
            try
            {
                Engine.AdvanceFrame(1d / 60);
                Query("scroll_clipped", source, "offscreen", "outside", "null", "outside"); scroll.FollowFocus = true;
                Query("scroll_follow", source, "offscreen", "outside", "null", "offscreen");
                source.GrabFocus(); offscreen.GrabFocus(); Engine.AdvanceFrame(1d / 60);
                Check(scroll.ScrollVertical > 0 && scroll.GetGlobalRect().Intersects(offscreen.GetGlobalRect()), "Focus following actually scrolls the selected target into view.");
                scroll.FollowFocus = false; scroll.ScrollVertical = 0;
                var innerScroll = new ScrollContainer { Name = "inner_scroll", Position = new(0, 120), Size = new(80, 60), FollowFocus = true }; content.AddChild(innerScroll);
                var innerContent = C(innerScroll, "inner_content", mode: FocusMode.None); innerContent.CustomMinimumSize = new(80, 220);
                var nestedSource = C(innerContent, "nested_source", 10, 10); C(innerContent, "nested_offscreen", 10, 160);
                Engine.AdvanceFrame(1d / 60);
                Query("nested_outer_clipped", nestedSource, "nested_offscreen", "offscreen", "null", "outside");
                scroll.FollowFocus = true; Query("nested_follow", nestedSource, "nested_offscreen", "offscreen", "null", "nested_offscreen");
            }
            finally { Engine.Stop(); }
        }
    }
    private sealed class Errors : Logger
    {
        internal readonly List<string> Messages = [];
        protected override void OnLogError(string function, string file, int line, string code, string rationale, bool editorNotify, LoggerErrorType errorType, ReadOnlySpan<ScriptBacktrace> scriptBacktraces)
        { if (errorType == LoggerErrorType.Error) Messages.Add(rationale); }
    }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } Check(false, "Expected " + typeof(T).Name); }
    private static void PathsAndLifetime()
    {
        using var tree = Tree(out var window); var root = C(window, "root", mode: FocusMode.None); var a = C(root, "a"); var b = C(root, "b", 20);
        root.AddChild(new Node { Name = "neutral" }); using var logger = new Errors(); OS.AddLogger(logger);
        a.FocusNext = "../missing"; a.FocusPrevious = "../neutral"; a.FocusNeighborLeft = "../missing"; a.FocusNeighborRight = "../neutral";
        Check(a.FindNextValidFocus() is null && a.FindPrevValidFocus() is null && a.FindValidFocusNeighbor(Side.Left) is null && a.FindValidFocusNeighbor(Side.Right) is null, "Invalid explicit paths do not fall back or throw.");
        Check(logger.Messages.SequenceEqual(new[] { "Next focus node path is invalid: '../missing'.", "Previous focus node is not a control: 'neutral'.", "Neighbor focus node path is invalid: '../missing'.", "Neighbor focus node is not a control: 'neutral'." }), "Missing and wrong-type paths emit exact structured diagnostics.");
        a.FocusNext = "../b"; a.FocusPrevious = "../b"; b.FocusMode = FocusMode.None;
        Check(ReferenceEquals(a.FindNextValidFocus(), a) && ReferenceEquals(a.FindPrevValidFocus(), a), "Ineligible sequential overrides fall back from the source.");
        a.FocusNext = ""; a.FocusPrevious = ""; a.FocusNeighborLeft = ""; a.FocusNeighborRight = "";
        var chain = new Control[514];
        for (var i = 0; i < chain.Length; i++) { chain[i] = C(root, "n" + i, mode: FocusMode.None); if (i > 0) chain[i - 1].FocusNeighborRight = "../n" + i; }
        chain[512].FocusMode = FocusMode.All; Check(ReferenceEquals(chain[0].FindValidFocusNeighbor(Side.Right), chain[512]), "A 512-edge chain may return its eligible target.");
        chain[512].FocusMode = FocusMode.None; chain[513].FocusMode = FocusMode.All; Check(chain[0].FindValidFocusNeighbor(Side.Right) is null, "Search stops before a 513th edge.");
        a.FocusNext = "../b"; a.FocusPrevious = "../b"; a.FocusNeighborLeft = "../b"; a.FocusNeighborTop = "../b"; a.FocusNeighborRight = "../b"; a.FocusNeighborBottom = "../b";
        using var packed = new PackedScene(); packed.Pack(a); using var copy = (Control)packed.Instantiate(); using var duplicate = (Control)a.Duplicate();
        foreach (var clone in new[] { copy, duplicate }) Check(clone.FocusNext == "../b" && clone.FocusPrevious == "../b" && Enum.GetValues<Side>().All(side => clone.GetFocusNeighbor(side) == "../b"), "All six focus paths survive independent scene copies.");
        Reject<ArgumentNullException>(() => a.FocusNext = null!); Reject<ArgumentNullException>(() => a.FocusPrevious = null!); Reject<ArgumentNullException>(() => a.SetFocusNeighbor(Side.Left, null!));
        Reject<ArgumentOutOfRangeException>(() => a.FindValidFocusNeighbor((Side)4)); Reject<ArgumentOutOfRangeException>(() => a.GetFocusNeighbor((Side)(-1))); Reject<ArgumentOutOfRangeException>(() => a.SetFocusNeighbor((Side)4, ""));
        Exception? foreign = null; var thread = new System.Threading.Thread(() => { try { a.FocusNext = ""; } catch (Exception error) { foreign = error; } }); thread.Start(); thread.Join();
        Check(foreign is InvalidOperationException && a.FocusNext == "../b", "Off-owner path writes reject without changing state.");
        a.Dispose(); Reject<ObjectDisposedException>(() => a.FindNextValidFocus()); Reject<ObjectDisposedException>(() => a.FindPrevValidFocus()); Reject<ObjectDisposedException>(() => a.FindValidFocusNeighbor(Side.Right)); Reject<ObjectDisposedException>(() => a.FocusNext = "");
    }
    private static void Finish(string profile)
    {
        foreach (var error in Failures) Console.Error.WriteLine("FAIL " + error);
        Console.WriteLine($"{(Failures.Count == 0 ? "PASS" : "FAIL")} {_checks} focus contracts on {profile}; {Failures.Count} failures.");
        if (Failures.Count != 0) throw new InvalidOperationException("Focus contract failures: " + Failures.Count);
    }
    private static void Native(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var accessible = Environment.GetEnvironmentVariable("ELECTRON2D_FOCUS_ACCESSIBLE") == "1";
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, accessible ? AccessibilitySupport.Always : AccessibilitySupport.Disabled);
        var window = new Window { Title = "Keyboard and gamepad focus", Size = new(380, 180) };
        var panel = new Control { Size = new(380, 180) }; window.AddChild(panel);
        var scroll = new ScrollContainer { Position = new(20, 20), Size = new(180, 110), FollowFocus = true }; panel.AddChild(scroll);
        var content = new Control { CustomMinimumSize = new(160, 340) }; scroll.AddChild(content);
        using var normal = new StyleBoxFlat { BGColor = new(.12f, .2f, .32f), AntiAliasing = false };
        using var focus = new StyleBoxFlat { DrawCenter = false, BorderColor = Colors.Yellow, AntiAliasing = false }; focus.SetBorderWidthAll(3);
        Button Tile(Node parent, string name, Vector2 position)
        {
            var tile = new Button { Name = name, Position = position, Size = new(120, 32), FocusMode = accessible ? FocusMode.Accessibility : FocusMode.All };
            tile.AddThemeStyleBoxOverride("normal", normal); tile.AddThemeStyleBoxOverride("hover", normal); tile.AddThemeStyleBoxOverride("focus", focus); parent.AddChild(tile); return tile;
        }
        var first = Tile(content, "first", new(10, 10)); var last = Tile(content, "last", new(10, 250)); var outside = Tile(panel, "outside", new(230, 30));
        last.FocusNeighborBottom = last.GetPathTo(first);
        using var right = new InputEventKey { Keycode = Key.Right, Pressed = true };
        using var tab = new InputEventKey { Keycode = Key.Tab, Pressed = true };
        using var reverse = new InputEventKey { Keycode = Key.Tab, ShiftPressed = true, Pressed = true };
        using var down = new InputEventJoypadButton { Device = 7, ButtonIndex = JoyButton.DpadDown, Pressed = true };
        using var downRelease = new InputEventJoypadButton { Device = 7, ButtonIndex = JoyButton.DpadDown, Pressed = false };
        using var stick = new InputEventJoypadMotion { Device = 9, Axis = JoyAxis.LeftY, AxisValue = 1 };
        using var heldStick = new InputEventJoypadMotion { Device = 9, Axis = JoyAxis.LeftY, AxisValue = 1 };
        using var neutral = new InputEventJoypadMotion { Device = 9, Axis = JoyAxis.LeftY };
        var consumes = 0; void Consume(InputEvent e) { if (e is InputEventKey { Keycode: Key.Tab }) { outside.AcceptEvent(); consumes++; } }
        var draws = 0; var phase = 0; long before = 0, bytes = 0;
        using var texture = window.GetTexture();
        window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested native renderer executes."); Check(AccessibilityServer.IsSupported() == accessible, "Native accessibility eligibility matches the actual active bridge."); first.GrabFocus(true); Check(first.HasFocus() && !first.HasFocus(true), "Initial pointer-style focus hides its ring."); RenderingServer.FramePostDraw += AfterDraw; };
        void AfterDraw()
        {
            draws++;
            if (phase is 3 or 9)
            {
                using var image = texture.GetImage()!;
                var target = phase == 3 ? last : outside; var rect = target.GetGlobalRect();
                var pixel = image.GetPixel((int)(rect.Position.X + rect.Size.X / 2), (int)rect.Position.Y + 1);
                Check(pixel.R > .8f && pixel.G > .8f && pixel.B < .2f, "The actual rendered button has a visible yellow focus border.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder)
                { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, $"focus-{backend}-{phase}.png")); }
            }
            if (phase >= 160) window.Tree!.Quit();
        }
        try
        {
            Check(Engine.Run((hostWindow, frame) =>
            {
                phase = (int)frame;
                if (phase == 2) { Input.ParseInputEvent(right); Check(outside.HasFocus(true) && !first.HasFocus(), "Keyboard right transfers and reveals focus."); }
                if (phase == 3) { Input.ParseInputEvent(reverse); Check(last.HasFocus(true) && scroll.ScrollVertical > 0, "Reverse Tab selects the offscreen descendant and scrolls it into view."); }
                if (phase == 4) { first.GrabFocus(true); Input.ParseInputEvent(down); Check(last.HasFocus(true), "Nonzero-device D-pad navigation follows focus through a scroll container."); Input.ParseInputEvent(downRelease); }
                if (phase == 5) { Input.ParseInputEvent(tab); outside.GUIInput += Consume; Check(outside.HasFocus(), "Tab leaves the scroll subtree in scene order."); }
                if (phase == 6) { Input.ParseInputEvent(tab); outside.GUIInput -= Consume; Check(outside.HasFocus() && consumes == 1, "Consumed Tab does not navigate."); }
                if (phase == 7)
                {
                    first.GrabFocus(true); Input.ParseInputEvent(stick); Check(last.HasFocus(), "Analog press navigates."); Input.ParseInputEvent(heldStick); Check(last.HasFocus(), "Held analog input does not repeat.");
                    Input.ParseInputEvent(neutral); Input.ParseInputEvent(stick); Check(first.HasFocus(), "A fresh analog transition honors the explicit neighbor."); Input.ParseInputEvent(neutral);
                }
                if (phase == 9) outside.GrabFocus();
                if (phase == 32) before = GC.GetAllocatedBytesForCurrentThread();
                if (phase is > 32 and <= 160)
                {
                    _ = first.FindNextValidFocus(); _ = outside.FindPrevValidFocus(); _ = first.FindValidFocusNeighbor(Side.Bottom);
                    if (phase == 160) bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                }
                if (phase > 180) throw new TimeoutException("Focus rendering did not complete.");
            }, window) == 0, "The native focus host completes cleanly.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; }
        Check(draws >= 160 && bytes == 0, $"128 complete prepared frames and 384 queries allocate {bytes} owner bytes.");
        Finish(backend);
    }
}
