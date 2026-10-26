using System.Diagnostics;
using Electron2D;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
using Unit = Electron2D.AccessibilityServer.AccessibilityScrollUnit;
using Hint = Electron2D.AccessibilityServer.AccessibilityScrollHint;
using Path = System.IO.Path;

internal static class AccessibilityScrollTests
{
    private sealed class ScrollLine : LineEdit { internal void RepeatIME() => OnIMECompositionChanged("", default); }
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Native Scroll Probe");
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(1024, 768) };
        var outer = new ScrollContainer { AccessibilityName = "Outer", Position = new(8, 8), Size = new(300, 200), Scale = new(1.2f, 1), ScrollHorizontalCustomStep = 7, ScrollVerticalCustomStep = 9 };
        var content = new Panel { AccessibilityName = "Content", CustomMinimumSize = new(900, 700) };
        var inner = new ScrollContainer { AccessibilityName = "Inner", Position = new(450, 260), Size = new(280, 150) };
        var innerContent = new Panel { AccessibilityName = "Inner content", CustomMinimumSize = new(650, 500) };
        var target = new Button("Reveal target") { AccessibilityName = "Deep target", Position = new(450, 320), Size = new(130, 32) };
        content.AddChild(new Button("Fixed canvas") { AccessibilityName = "Top level", TopLevel = true, Position = new(220, 650), Size = new(130, 30) });
        innerContent.AddChild(target); inner.AddChild(innerContent); content.AddChild(inner); outer.AddChild(content); window.AddChild(outer);
        var list = new ItemList { AccessibilityName = "List", Position = new(400, 8), Size = new(292, 130), WraparoundItems = false, FixedColumnWidth = 800 };
        for (var i = 0; i < 30; i++) list.AddItem("Item " + i + " with a long scrollable caption"); list.Select(2); window.AddChild(list);
        var tree = new Tree { AccessibilityName = "Tree", Position = new(704, 8), Size = new(300, 130), HideRoot = true, Columns = 2 };
        tree.SetColumnCustomMinimumWidth(0, 600); tree.SetColumnCustomMinimumWidth(1, 200); var root = tree.CreateItem(); TreeItem? first = null, last = null;
        for (var i = 0; i < 30; i++) { var row = root.CreateChild(); row.SetText(0, "Tree row " + i); row.SetText(1, "Value " + i); first ??= row; last = row; }
        first!.Select(0); window.AddChild(tree);
        var text = new TextEdit { AccessibilityName = "Text", Position = new(400, 150), Size = new(292, 140), Editable = false, Text = string.Join('\n', Enumerable.Repeat("A long text line with words and columns that extend well beyond the narrow viewport.", 35)), CaretBlink = false };
        var code = new CodeEdit { AccessibilityName = "Code", Position = new(704, 150), Size = new(300, 140), Text = string.Join('\n', Enumerable.Repeat("var a_long_name_that_extends_far_outside_the_viewport = 1234567890123456789;", 30)), CaretBlink = false };
        var line = new ScrollLine { AccessibilityName = "Line", Position = new(400, 302), Size = new(604, 38), Text = string.Concat(Enumerable.Repeat("0123456789 ", 15)), CaretBlink = false };
        window.AddChild(text); window.AddChild(code); window.AddChild(line);
        var rich = new RichTextLabel { AccessibilityName = "Rich", Position = new(8, 224), Size = new(380, 210), Text = string.Join('\n', Enumerable.Range(0, 70).Select(i => "Rich text line " + i)) }; window.AddChild(rich);
        var tabs = new TabBar { AccessibilityName = "Tabs", Position = new(8, 600), Size = new(996, 40) };
        for (var i = 0; i < 18; i++) tabs.AddTab("Wide tab caption " + i); tabs.CurrentTab = 2; tabs.SetTabHidden(3, true); window.AddChild(tabs);
        var graph = new GraphEdit { AccessibilityName = "Graph", Position = new(400, 352), Size = new(604, 180), Rotation = 0.1f };
        graph.AddChild(new GraphNode { AccessibilityName = "Graph node", Title = "Pan this graph", PositionOffset = new(180, 80), Size = new(180, 80) }); window.AddChild(graph);
        var next = new Button("Next scroll phase") { AccessibilityName = "Next scroll phase", Position = new(8, 708), Size = new(180, 36) }; window.AddChild(next);
        var extremeRequests = 0; var singularCalls = 0;
        var phase = 0; var units = 0; var sequential = 0; var hints = 0; var graphEvents = 0; var pressed = 0; var pointCalls = 0; var requestedPoint = Vector2.Zero;
        graph.ScrollOffsetChanged += _ => graphEvents++; target.Pressed += () => pressed++;
        RID manual = default, child = default, hintTarget = default, pointTarget = default;
        next.Pressed += () =>
        {
            phase++;
            if (phase == 1)
            {
                Check(extremeRequests == 2 && singularCalls == 0, "Finite extreme offsets execute; singular scroll transforms reject without dispatch.");
                Check(units == 255 && sequential == 2 && hints == 63, "Every typed unit/hint and sequential callback receives its exact native payload.");
                Check(requestedPoint == new Vector2(367, 528) && pointCalls == 2, "Window and parent native coordinates reach typed point callbacks.");
                Check(list.IsSelected(2) && tree.GetSelected() == first && tabs.CurrentTab == 2 && text.GetCaretLine() == 0 && line.CaretColumn == 0, "Scrolling preserves selections and carets.");
                line.RepeatIME();
                Check(line.GetScrollOffset() < 0, "Reading the horizontal offset preserves the independently panned view.");
                Check(window.GetGUIFocusOwner() == line, "Scrolling preserves the managed keyboard focus owner.");
                Check(graphEvents > 0 && pressed == 1, "Graph user scroll events and the revealed ordinary button execute.");
                AccessibilityServer.UpdateSetFlag(hintTarget, Flag.Hidden, true); AccessibilityServer.UpdateSetFlag(pointTarget, Flag.Disabled, true);
                tabs.LayoutDirection = LayoutDirection.RTL; list.LayoutDirection = LayoutDirection.RTL; tree.LayoutDirection = LayoutDirection.RTL;
                tabs.Position = new(8, 600); list.Position = new(400, 8); tree.Position = new(704, 8); target.Size = new(600, 400);
            }
            if (phase == 2)
            {
                Check(pointCalls == 2, "Disabled point requests do not enter the callback.");
                Check(target.Size == new Vector2(600, 400), "Revealing an oversized element does not resize it.");
                AccessibilityServer.UpdateAddAction(manual, AAction.SetScrollOffset, (Vector2 _) => AccessibilityServer.FreeElement(manual));
            }
            if (phase == 3)
            {
                Check(!AccessibilityServer.HasElement(manual) && !AccessibilityServer.HasElement(child), "A reentrant scroll callback can retire its owned target subtree.");
                target.Size = new(130, 32); list.GetHScrollBar().Value = 0; list.GetVScrollBar().Value = 0; tree.EnsureCursorIsVisible();
                text.ScrollVertical = code.ScrollVertical = 2; rich.GetVScrollBar().Value = 0;
            }
        };
        Process? client = null; Task<string>? output = null, error = null; using var texture = window.GetTexture();
        var complete = false; var post = 0; var frames = 0; var measuring = false; long before = 0, idle = 0, active = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Actual renderer and native bridge are available.");
            manual = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.ScrollView); AccessibilityServer.UpdateSetName(manual, "Manual"); AccessibilityServer.UpdateSetBounds(manual, new(8, 500, 320, 110)); AccessibilityServer.UpdateSetFlag(manual, Flag.ClipsChildren, true);
            child = AccessibilityServer.CreateSubElement(manual, Role.Button); AccessibilityServer.UpdateSetName(child, "Manual child"); AccessibilityServer.UpdateSetBounds(child, new(480, 780, 40, 20));
            AccessibilityServer.UpdateSetScrollXRange(manual, 0, 600); AccessibilityServer.UpdateSetScrollYRange(manual, 0, 700); AccessibilityServer.UpdateSetScrollX(manual, 0); AccessibilityServer.UpdateSetScrollY(manual, 0);
            AccessibilityServer.UpdateAddAction(manual, AAction.SetScrollOffset, (Vector2 offset) => { offset = offset.Clamp(Vector2.Zero, new(600, 700)); AccessibilityServer.UpdateSetScrollX(manual, offset.X); AccessibilityServer.UpdateSetScrollY(manual, offset.Y); AccessibilityServer.UpdateSetBounds(child, new(new Vector2(480, 780) - offset, new(40, 20))); });
            AccessibilityServer.UpdateSetScrollX(manual, -7); AccessibilityServer.UpdateSetBounds(child, new(487, 780, 40, 20));
            var extreme = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.ScrollView); AccessibilityServer.UpdateSetName(extreme, "Extreme"); AccessibilityServer.UpdateSetBounds(extreme, new(8, 650, 80, 20));
            AccessibilityServer.UpdateSetScrollXRange(extreme, -float.MaxValue, float.MaxValue); AccessibilityServer.UpdateSetScrollX(extreme, 0);
            AccessibilityServer.UpdateAddAction(extreme, AAction.SetScrollOffset, (Vector2 offset) => { Check(offset.IsFinite(), "Native extreme offsets stay finite."); extremeRequests++; AccessibilityServer.UpdateSetScrollX(extreme, offset.X); });
            var singular = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.ScrollView); AccessibilityServer.UpdateSetName(singular, "Singular"); AccessibilityServer.UpdateSetBounds(singular, new(8, 650, 80, 20)); AccessibilityServer.UpdateSetTransform(singular, default);
            AccessibilityServer.UpdateAddAction(singular, AAction.SetScrollOffset, (Vector2 _) => singularCalls++);
            var singularChild = AccessibilityServer.CreateSubElement(singular, Role.Button); AccessibilityServer.UpdateSetName(singularChild, "Singular child"); AccessibilityServer.UpdateSetBounds(singularChild, new(90, 650, 40, 20));
            var group = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Container); AccessibilityServer.UpdateSetName(group, "Grouping");
            var groupChild = AccessibilityServer.CreateSubElement(group, Role.Button); AccessibilityServer.UpdateSetName(groupChild, "Group child"); AccessibilityServer.UpdateSetBounds(groupChild, new(150, 650, 40, 20));
            AAction[] directions = [AAction.ScrollUp, AAction.ScrollDown, AAction.ScrollLeft, AAction.ScrollRight];
            for (var i = 0; i < directions.Length; i++) { var bit = i * 2; AccessibilityServer.UpdateAddAction(manual, directions[i], (Unit unit) => units |= 1 << (bit + (int)unit)); }
            AccessibilityServer.UpdateAddAction(manual, AAction.ScrollBackward, () => sequential++); AccessibilityServer.UpdateAddAction(manual, AAction.ScrollForward, () => sequential++);
            hintTarget = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Button); AccessibilityServer.UpdateSetName(hintTarget, "Hint"); AccessibilityServer.UpdateSetBounds(hintTarget, new(350, 600, 30, 20)); AccessibilityServer.UpdateAddAction(hintTarget, AAction.ScrollIntoView, (Hint hint) => hints |= 1 << (int)hint);
            var pointGroup = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Container); AccessibilityServer.UpdateSetName(pointGroup, "Point group"); AccessibilityServer.UpdateSetBounds(pointGroup, new(350, 510, 100, 60));
            pointTarget = AccessibilityServer.CreateSubElement(pointGroup, Role.Button); AccessibilityServer.UpdateSetName(pointTarget, "Point"); AccessibilityServer.UpdateSetBounds(pointTarget, new(350, 510, 40, 20)); AccessibilityServer.UpdateAddAction(pointTarget, AAction.ScrollToPoint, (Vector2 point) => { requestedPoint = point; pointCalls++; });
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetScrollX(manual, float.NaN)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetScrollY(manual, float.PositiveInfinity));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetScrollXRange(manual, 2, 1)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetScrollYRange(manual, 0, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateAddAction(manual, AAction.Click, (Unit _) => { })); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateAddAction(manual, AAction.ScrollDown, () => { }));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateAddAction(manual, AAction.SetValue, (Hint _) => { })); Reject<ArgumentNullException>(() => AccessibilityServer.UpdateAddAction(manual, AAction.ScrollToPoint, (Action<Vector2>)null!));
            Task.Run(() => Reject<InvalidOperationException>(() => AccessibilityServer.UpdateSetScrollX(manual, 1))).GetAwaiter().GetResult();
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/scroll.py")); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96); if (measuring) before = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 80)
                {
                    var alternate = (post & 1) == 0; outer.ScrollVertical = alternate ? 10 : 40; outer.ScrollHorizontal = alternate ? 10 : 40;
                    list.GetVScrollBar().Value = alternate ? 10 : 40; tree.ScrollToItem(alternate ? first! : last!);
                    text.ScrollVertical = alternate ? 1 : 10; code.ScrollVertical = alternate ? 1 : 10; line.CaretColumn = alternate ? 0 : 80;
                    rich.GetVScrollBar().Value = alternate ? 10 : 40; tabs.EnsureTabVisible(alternate ? 0 : 17); graph.ScrollOffset = alternate ? new(20, 30) : new(40, 60);
                }
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (!complete) return; post++;
            if (post == 8 && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } dir) { Directory.CreateDirectory(dir); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(dir, "scroll-" + backend + ".png")); }
            if (post == 160) { Check(idle == 0 && active == 0, $"Scroll semantic idle={idle}, active={active} managed bytes."); window.Tree!.Quit(); }
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) => { frames++; if (!complete && client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && phase == 3, "Independent native scroll client completed all phases."); complete = true; } if (Stopwatch.GetElapsedTime(started).TotalSeconds > 90) throw new TimeoutException("Native scroll client timed out."); }, window) == 0, "Scroll host completes.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        RunCallbackFailure(); Console.WriteLine($"PASS {_checks} native scroll contracts on {backend}; {frames} frames, 64 idle/64 active {idle}/{active} managed bytes.");
    }
    private static void RunCallbackFailure()
    {
        var window = new Window { Size = new(120, 80) }; Process? client = null; Task<string>? output = null, error = null; RID producer = default;
        window.Ready += _ =>
        {
            producer = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.ScrollView); AccessibilityServer.UpdateSetName(producer, "Throwing scroll"); AccessibilityServer.UpdateSetBounds(producer, new(0, 0, 100, 60)); AccessibilityServer.UpdateSetScrollYRange(producer, 0, 100); AccessibilityServer.UpdateAddAction(producer, AAction.SetScrollOffset, (Vector2 _) => throw new InvalidOperationException("Injected native scroll failure"));
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/scroll.py")); start.ArgumentList.Add("--failure"); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
        };
        var started = Stopwatch.GetTimestamp(); var failed = false;
        try { Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Scroll failure client timed out."); }, window); }
        catch (AggregateException errors) { var failures = errors.Flatten().InnerExceptions; failed = failures.Count == 1 && failures[0] is InvalidOperationException { Message: "Injected native scroll failure" }; }
        finally
        {
            if (client is { HasExited: false } && !client.WaitForExit(5000)) client.Kill(true); if (output is not null) Console.Write(output.GetAwaiter().GetResult()); if (error is not null) Console.Write(error.GetAwaiter().GetResult()); Check(failed && client?.ExitCode == 0 && !AccessibilityServer.HasElement(producer), "Scroll callback failure replies to the client, reaches its owner and retires the producer."); client?.Dispose();
        }
        var fresh = new Window { Size = new(100, 60) }; fresh.Ready += _ => { Check(AccessibilityServer.IsSupported(), "Display reopens after native scroll failure."); fresh.Tree!.Quit(); }; Check(Engine.Run(fresh) == 0, "Fresh display closes cleanly.");
    }
}
