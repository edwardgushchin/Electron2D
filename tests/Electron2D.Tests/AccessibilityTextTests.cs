using System.Diagnostics;
using System.Text;
using Electron2D;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
using Path = System.IO.Path;

internal static class AccessibilityTextTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunNative(string backend)
    {
        var clipboard = backend == "clipboard";
        if (clipboard)
        {
            Check(Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy" && Environment.GetEnvironmentVariable("SDL_RENDER_DRIVER") == "software", "Clipboard profile requires an isolated dummy/software process.");
            backend = "compatibility";
        }
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Native Text Probe");
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(620, 520) };
        if (clipboard) window.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
        var single = new LineEdit { AccessibilityName = "Single", Text = "A😀é שלום world.", Position = new(12, 12), Size = new(580, 40), CaretBlink = false };
        var multi = new TextEdit { AccessibilityName = "Multi", Text = "First 😀 line.\nSecond line!\n", Position = new(12, 64), Size = new(580, 128), CaretBlink = false };
        var code = new CodeEdit { AccessibilityName = "Code", Text = "var value = 1;\nreturn value;", Position = new(12, 204), Size = new(580, 88), CaretBlink = false };
        var secret = new LineEdit { AccessibilityName = "Password", Text = "private😀", Secret = true, Position = new(12, 304), Size = new(280, 36), CaretBlink = false };
        var next = new Button("Next") { AccessibilityName = "Next phase", Position = new(12, 476), Size = new(120, 32) };
        var wrapped = new TextEdit { AccessibilityName = "Wrapped", Text = string.Join(' ', Enumerable.Repeat("wrap words", 25)), WrapMode = TextEdit.LineWrappingMode.Boundary, Position = new(310, 352), Size = new(282, 48), CaretBlink = false };
        window.AddChild(wrapped);
        window.AddChild(single); window.AddChild(multi); window.AddChild(code); window.AddChild(secret); window.AddChild(next);
        RID shapedLine = default, fragmentLeft = default, fragmentRight = default;
        var phase = 0; var changed = 0; var frames = 0; var post = 0; var complete = false; var measuring = false; long before = 0, idle = 0, active = 0;
        single.TextChanged += _ => changed++;
        next.Pressed += () =>
        {
            phase++;
            if (phase == 1) { Check(single.Text == "A😀Z שלום world." && single.HasUndo() && changed > 0, "Native selection replacement preserves user edits and history."); single.Editable = false; multi.Editable = false; }
            if (phase == 2) { single.Editable = true; single.MaxLength = 4; single.Text = "ab"; }
            if (phase == 3) { Check(single.Text == "ab😀X", "Native insertion applies scalar MaxLength."); single.MaxLength = 0; single.Text = "Short"; single.SelectingEnabled = false; }
            if (phase == 4) { multi.Editable = true; single.SelectingEnabled = true; single.Text = "Changed after publication"; multi.Text = "new\nlast"; }
            if (phase == 5) { window.RemoveChild(code); code.Dispose(); AccessibilityServer.FreeElement(shapedLine); }
            if (phase == 6) { AccessibilityServer.UpdateSetNextOnLine(fragmentLeft, default); AccessibilityServer.UpdateSetPreviousOnLine(fragmentRight, default); }
            if (phase == 7) { AccessibilityServer.UpdateSetNextOnLine(fragmentLeft, fragmentLeft); AccessibilityServer.UpdateSetPreviousOnLine(fragmentRight, fragmentLeft); AccessibilityServer.FreeElement(fragmentLeft); }
        };
        Process? client = null; Task<string>? output = null, error = null;
        using var texture = window.GetTexture(); RID manual = default;
        window.Ready += _ =>
        {
            if (clipboard) { Check(DisplayServer.GetName() == "headless", "Clipboard writes stay inside the actual headless SDL driver."); DisplayServer.ClipboardSet("fixture clipboard"); }
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Actual renderer and native accessibility bridge.");
            manual = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.TextField); AccessibilityServer.UpdateSetName(manual, "Shaped");
            var server = TextServerManager.GetPrimaryInterface(); var buffer = server.CreateShapedText();
            try { server.ShapedTextAddString(buffer, "Shaped ﬁ שלום", [single.GetThemeFont("font")!], 24); shapedLine = AccessibilityServer.CreateSubTextEditElements(manual, buffer, 30, isLastLine: true); }
            finally { server.FreeRID(buffer); }
            AccessibilityServer.UpdateSetBounds(shapedLine, new(310, 304, 280, 40));
            AccessibilityServer.UpdateSetTextAlign(manual, HorizontalAlignment.Center); AccessibilityServer.UpdateSetForegroundColor(manual, new(1, 0, 0));
            AccessibilityServer.UpdateSetTextDecorations(manual, true, true, false); AccessibilityServer.UpdateSetLanguage(manual, "he"); AccessibilityServer.UpdateSetPlaceholder(manual, "unused placeholder");
            AccessibilityServer.UpdateSetTextSelection(manual, shapedLine, 0, shapedLine, 0); AccessibilityServer.UpdateSetValue(manual, "semantic value");
            var fragments = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.TextField); AccessibilityServer.UpdateSetName(fragments, "Fragments");
            RID Fragment(string text, Vector2 position)
            {
                var shape = server.CreateShapedText(); try { server.ShapedTextAddString(shape, text, [single.GetThemeFont("font")!], 24); var result = AccessibilityServer.CreateSubTextEditElements(fragments, shape, 30, isLastLine: true); AccessibilityServer.UpdateSetBounds(result, new(position, new(120, 30))); return result; } finally { server.FreeRID(shape); }
            }
            fragmentLeft = Fragment("left ", new(12, 352)); fragmentRight = Fragment("right", new(130, 360));
            AccessibilityServer.UpdateSetNextOnLine(fragmentLeft, fragmentRight); AccessibilityServer.UpdateSetPreviousOnLine(fragmentRight, fragmentLeft);
            var emptyOwner = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.TextField); AccessibilityServer.UpdateSetName(emptyOwner, "Empty line");
            AccessibilityServer.CreateSubTextEditElements(emptyOwner, default, 24);
            var raw = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.TextField); AccessibilityServer.UpdateSetName(raw, "Raw text"); AccessibilityServer.UpdateSetValue(raw, "a\0b\ud800");
            var producer = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.TextField); AccessibilityServer.UpdateSetName(producer, "Producer");
            var stored = "abc😀"; var anchor = 0; var focus = 0; AccessibilityServer.UpdateSetValue(producer, stored);
            AccessibilityServer.UpdateAddAction(producer, AAction.SetTextSelection, (RID start, int from, RID end, int to) => { Check(start == producer && end == producer, "Typed selection callback receives producer identities."); anchor = from; focus = to; AccessibilityServer.UpdateSetTextSelection(producer, producer, from, producer, to); });
            AccessibilityServer.UpdateAddAction(producer, AAction.ReplaceSelectedText, (string value) =>
            {
                if (value == "retire") { AccessibilityServer.FreeElement(producer); return; }
                var scalars = stored.EnumerateRunes().ToArray(); stored = string.Concat(scalars.Take(anchor).Select(rune => rune.ToString())) + value + string.Concat(scalars.Skip(focus).Select(rune => rune.ToString()));
                anchor += value.EnumerateRunes().Count(); focus = anchor; AccessibilityServer.UpdateSetValue(producer, stored); AccessibilityServer.UpdateSetTextSelection(producer, producer, anchor, producer, focus);
            });

            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.CreateSubTextEditElements(manual, default, float.NaN));
            Reject<InvalidOperationException>(() => AccessibilityServer.CreateSubTextEditElements(manual, manual, 0));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetTextSelection(manual, shapedLine, -1, shapedLine, 0));
            Reject<ArgumentException>(() => AccessibilityServer.UpdateSetTextSelection(manual, single.GetAccessibilityElement(), 0, shapedLine, 0));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateAddAction(manual, AAction.Click, (string _) => { }));
            Task.Run(() => Reject<InvalidOperationException>(() => AccessibilityServer.UpdateSetValue(manual, "foreign thread"))).GetAwaiter().GetResult();
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/text.py")); if (clipboard) start.ArgumentList.Add("--clipboard"); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96);
                if (measuring) before = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 80) { single.CaretColumn = (post & 1) == 0 ? 0 : 3; multi.SetCaretColumn((post & 1) == 0 ? 0 : 1); }
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (!complete) return; post++;
            if (post == 8 && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } dir) { Directory.CreateDirectory(dir); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(dir, "text-" + (clipboard ? "clipboard" : backend) + ".png")); }
            if (post == 160) { Check(idle == 0 && active == 0, $"Text semantic idle={idle}, active={active} managed bytes."); window.Tree!.Quit(); }
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) =>
            {
                frames++;
                if (!complete && client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && phase == 7, "External Text/EditableText client completed all phases."); complete = true; }
                if (Stopwatch.GetElapsedTime(started).TotalSeconds > 60) throw new TimeoutException("Native text client timed out.");
            }, window) == 0, "Text host completes.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        Check(!AccessibilityServer.HasElement(manual) && !AccessibilityServer.HasElement(shapedLine), "Display teardown retires copied shaped text and source lines.");
        RunCallbackFailure();
        Console.WriteLine($"PASS {_checks} native text contracts on {backend}; {frames} frames, 64 idle/64 active {idle}/{active} managed bytes.");
    }
    private static void RunCallbackFailure()
    {
        var window = new Window { Size = new(120, 80) }; Process? client = null; Task<string>? output = null, error = null; RID producer = default;
        window.Ready += _ =>
        {
            producer = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.TextField); AccessibilityServer.UpdateSetName(producer, "Throwing producer"); AccessibilityServer.UpdateSetValue(producer, "safe");
            AccessibilityServer.UpdateAddAction(producer, AAction.SetTextSelection, (RID anchor, int from, RID focus, int to) => AccessibilityServer.UpdateSetTextSelection(producer, anchor, from, focus, to));
            AccessibilityServer.UpdateAddAction(producer, AAction.ReplaceSelectedText, (string _) => throw new InvalidOperationException("Injected native text failure"));
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/text.py")); start.ArgumentList.Add("--failure");
            client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
        };
        var started = Stopwatch.GetTimestamp(); var failed = false;
        try
        {
            Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Text failure client timed out."); }, window);
        }
        catch (AggregateException errors)
        {
            var failures = errors.Flatten().InnerExceptions; failed = failures.Count == 1 && failures[0] is InvalidOperationException { Message: "Injected native text failure" };
        }
        finally
        {
            if (client is { HasExited: false } && !client.WaitForExit(5000)) client.Kill(true);
            if (output is not null) Console.Write(output.GetAwaiter().GetResult()); if (error is not null) Console.Write(error.GetAwaiter().GetResult());
            Check(failed && client?.ExitCode == 0 && !AccessibilityServer.HasElement(producer), "Native text callback failure reaches the owner and retires all identities."); client?.Dispose();
        }
        var reopened = new Window { Size = new(100, 60) }; reopened.Ready += _ => { Check(AccessibilityServer.IsSupported(), "Fresh native display reopens after text callback failure."); reopened.Tree!.Quit(); }; Check(Engine.Run(reopened) == 0, "Fresh display closes cleanly.");
    }

}
