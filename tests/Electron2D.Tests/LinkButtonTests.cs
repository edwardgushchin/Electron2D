using Electron2D;
using System.Diagnostics;

internal static class LinkButtonTests
{
    internal static void Run()
    {
        DefaultsAndStorage(); LayoutAndResources(); Routing(); CaptureGuard(); FreshProcess(); WarmFrames();
        Console.WriteLine("LinkButton: defaults/storage, shaping/RTL/ellipsis/BIDI, input, borrowed font lifecycle, fresh file/isolated URI launch and 64 warm active/idle frames passed.");
    }
    private static void DefaultsAndStorage()
    {
        using var link = new LinkButton();
        Check(link is BaseButton && (BaseButton)link is not Button && link.Text == "" && link.URI == "" && link.EllipsisChar == "…" && link.Underline == LinkButton.UnderlineMode.Always && link.FocusMode == FocusMode.Accessibility && link.GetCursorShape() == CursorShape.PointingHand, "Exact independent link defaults.");
        link.Disabled = true; Check(link.GetCursorShape() == CursorShape.Arrow, "Disabled links use arrow."); link.Disabled = false;
        Reject<ArgumentNullException>(() => link.Text = null!); Reject<ArgumentNullException>(() => link.URI = null!);
        Reject<ArgumentException>(() => link.Language = "en\0"); Reject<ArgumentOutOfRangeException>(() => link.TextDirection = (TextDirection)4);
        link.EllipsisChar = "😀abc"; Check(link.EllipsisChar == "😀", "First Unicode scalar, not first surrogate.");
        Reject<ArgumentException>(() => link.EllipsisChar = "\ud800"); Check(link.EllipsisChar == "😀", "Invalid ellipsis is atomic.");
        link.Text = "stored سلام"; link.URI = "e2d-test://stored/path"; link.Language = "ar"; link.TextDirection = TextDirection.RTL;
        link.TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce; link.Underline = LinkButton.UnderlineMode.OnHover;
        link.StructuredTextBIDIOverride = StructuredTextParser.List; var options = new[] { "/" }; link.StructuredTextBIDIOverrideOptions = options;
        options[0] = "wrong"; var got = link.StructuredTextBIDIOverrideOptions; got[0] = "wrong"; Check(link.StructuredTextBIDIOverrideOptions[0] == "/", "Parser options copy in both directions.");
        using var scene = new PackedScene(); scene.Pack(link); using var copy = (LinkButton)scene.Instantiate();
        Check(copy.Text == link.Text && copy.URI == link.URI && copy.EllipsisChar == "😀" && copy.Underline == link.Underline && copy.Language == "ar" && copy.TextDirection == TextDirection.RTL && copy.TextOverrunBehavior == link.TextOverrunBehavior && copy.StructuredTextBIDIOverride == StructuredTextParser.List && copy.StructuredTextBIDIOverrideOptions[0] == "/" && copy.FocusMode == FocusMode.Accessibility, "Every authored field survives packing with concrete defaults.");
        link.Dispose(); Reject<ObjectDisposedException>(() => _ = link.URI);
    }
    private static void LayoutAndResources()
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans }; using var link = new LinkButton("MMMMMMMM") { Size = new(180, 35) };
        link.AddThemeFontOverride("font", font); link.AddThemeFontSizeOverride("font_size", 18);
        var natural = link.GetMinimumSize(); Check(natural.X > 80 && natural.Y > 10, "Actual font natural geometry.");
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        Record(link, vertices, batches); var underlined = vertices.Count;
        link.Underline = LinkButton.UnderlineMode.Never; Record(link, vertices, batches); Check(vertices.Count < underlined, "Underline contributes real canvas geometry.");
        link.TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce; link.Size = new(35, 35); Record(link, vertices, batches); Check(link.GetMinimumSize().X == 0, "Trimming releases width.");
        Check(vertices.Count > 0 && vertices.All(v => v.Position.X >= -.1f && v.Position.X <= 35.1f), "Ellipsis fits available width.");
        link.TextOverrunBehavior = TextOverrunBehavior.NoTrimming; link.Size = new(200, 35); link.LayoutDirection = LayoutDirection.RTL; Record(link, vertices, batches); Check(vertices.Min(v => v.Position.X) > 40, "Layout RTL right-aligns the natural line.");
        using var custom = new ParserLink("ab/אב"); custom.AddThemeFontOverride("font", font); custom.StructuredTextBIDIOverride = StructuredTextParser.Custom; custom.GetMinimumSize(); Check(custom.Calls != 0, "Custom typed parser executes.");
        custom.Invalid = true; custom.Text = "different"; Reject<InvalidOperationException>(() => custom.GetMinimumSize()); custom.Invalid = false;
        custom.Reenter = true; custom.Text = "old"; custom.GetMinimumSize(); custom.GetMinimumSize(); Check(custom.Text == "new" && custom.GetMinimumSize().X > 0, "Parser mutation does not commit stale layout.");
        var root = new Node(); root.AddChild(link); using var tree = new SceneTree(root); tree.ProcessFrame(0);
        Reject<InvalidOperationException>(() => Task.Run(() => link.URI = "off-owner").GetAwaiter().GetResult());
        Task.Run(() => font.FixedSize = 22).GetAwaiter().GetResult(); tree.ProcessFrame(0); Check(link.GetMinimumSize().X > 0, "Worker font edits defer to owner.");
        root.RemoveChild(link); font.FixedSize = 26; root.AddChild(link); tree.ProcessFrame(0); link.GetMinimumSize();
        link.Dispose(); Check(!font.IsDisposed, "Link never owns its font.");
    }
    private sealed class ParserLink(string text) : LinkButton(text)
    {
        internal int Calls; internal bool Invalid, Reenter;
        protected override TextBIDIRange[] OnStructuredTextParser(IReadOnlyList<string> options, string text)
        { Calls++; if (Reenter) { Reenter = false; Text = "new"; } return Invalid ? [new(0, 999, TextDirection.LTR)] : [new(0, 2, TextDirection.LTR)]; }
    }
    private static void Routing()
    {
        var root = new SubViewport { Size = new(300, 100) }; var link = new LinkButton("Action") { Position = new(10, 10), Size = new(100, 35), FocusMode = FocusMode.All };
        root.AddChild(link); using var tree = new SceneTree(root); tree.ProcessFrame(0); var pressed = 0; link.Pressed += () => pressed++;
        Click(root); Check(pressed == 1, "Pointer release activates."); link.GrabFocus(); ActionInput(root); Check(pressed == 2, "Focused action activates.");
        link.Disabled = true; Click(root); ActionInput(root); Check(pressed == 2, "Disabled link blocks all activation."); link.Disabled = false;
        link.URI = "bad\0uri"; Reject<AggregateException>(() => Click(root)); Check(pressed == 3, "Platform failure still attempts inherited Pressed observers.");
        link.URI = ""; link.ToggleMode = true; Click(root); Check(link.ButtonPressed && pressed == 4, "Inherited toggling retains activation.");
    }
    private static void CaptureGuard()
    {
        using var link = new CaptureLink(); using var scene = new PackedScene(); scene.Pack(link);
        Check(link.Captured && link.Text == "guard", "Scene capture blocks authored link mutation atomically.");
        link.Dispose(); Reject<ObjectDisposedException>(link.Activate);
    }
    private sealed class CaptureLink : LinkButton
    {
        internal bool Captured;
        internal void Activate() => base.OnPressed();
        internal CaptureLink() : base("guard") { }
        protected override Func<Node> CreateSceneInstanceFactory() => CreateCaptureLink;
        private static Node CreateCaptureLink() => new CaptureLink();
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
        {
            foreach (var property in base.GetPropertyDescriptors()) yield return property;
            yield return new PropertyDescriptor<CaptureLink, int>("Guard", l => l.ReadGuard(), (l, v) => { }, _ => 0, stored: true);
        }
        private int ReadGuard() { Reject<InvalidOperationException>(() => Text = "during capture"); Reject<InvalidOperationException>(Activate); Captured = true; return 0; }
    }
    private static void FreshProcess()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-link-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var scenePath = System.IO.Path.Combine(folder, "link.e2dscene"); var capture = System.IO.Path.Combine(folder, "opened.txt"); var opener = System.IO.Path.Combine(folder, "xdg-open");
            File.WriteAllText(opener, "#!/bin/sh\nprintf '%s\\n' \"$1\" >> \"$E2D_LINK_CAPTURE\"\n");
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(opener, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            using (var source = new LinkButton("Stored link") { URI = "e2d-test://stored/path", FocusMode = FocusMode.All, Underline = LinkButton.UnderlineMode.OnHover })
            using (var scene = new PackedScene()) { scene.Pack(source); ResourceSaver.Save(scene, scenePath, SaverFlags.Compress | SaverFlags.SaveBigEndian); }
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(LinkButtonTests).Assembly.Location);
            start.Environment.Remove("ELECTRON2D_TEST_LINK"); start.Environment["ELECTRON2D_TEST_LINK_CHILD"] = scenePath;
            start.Environment["E2D_LINK_CAPTURE"] = capture; start.Environment["PATH"] = folder + System.IO.Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
            start.Environment["DBUS_SESSION_BUS_ADDRESS"] = "unix:path=" + System.IO.Path.Combine(folder, "absent-bus");
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh link consumer."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh LinkButton passed"), "Fresh link: " + error.GetAwaiter().GetResult());
            Check(File.ReadAllText(capture).Trim() == "e2d-test://stored/path", "Real SDL URL launch reached isolated native process with exact URI.");
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void RunChild(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var link = (LinkButton)scene.Instantiate(); scene.Dispose();
        var root = new SubViewport { Size = new(300, 100) }; link.Position = new(10, 10); link.Size = new(150, 35); root.AddChild(link); using var tree = new SceneTree(root); tree.ProcessFrame(0);
        var activated = false; link.Pressed += () => activated = true; Click(root); Check(activated && link.Text == "Stored link" && link.Underline == LinkButton.UnderlineMode.OnHover, "Fresh persisted link activates.");
        var capture = Environment.GetEnvironmentVariable("E2D_LINK_CAPTURE")!; var timeout = Stopwatch.StartNew(); while (!File.Exists(capture)) { if (timeout.ElapsedMilliseconds > 5000) throw new TimeoutException("Isolated SDL resource opener."); System.Threading.Thread.Sleep(10); }
        Console.WriteLine("Fresh LinkButton passed");
    }
    private static void WarmFrames()
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans }; var link = new LinkButton("warm A") { Size = new(120, 35), ToggleMode = true }; link.AddThemeFontOverride("font", font);
        using var tree = new SceneTree(link); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        for (var i = 0; i < 64; i++) { link.Text = (i & 1) == 0 ? "warm A" : "warm V"; link.ButtonPressed = (i & 1) == 0; tree.ProcessFrame(0); Record(link, vertices, batches); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { link.Text = (i & 1) == 0 ? "warm A" : "warm V"; link.ButtonPressed = (i & 1) == 0; tree.ProcessFrame(0); Record(link, vertices, batches); }
        var active = GC.GetAllocatedBytesForCurrentThread() - before; Check(active == 0, "Warm link layout/recording bytes=" + active);
        before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) tree.ProcessFrame(0); Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Idle font polling allocates zero.");
    }
    private static void Click(Viewport root) { using var down = new InputEventMouseButton { Position = new(20, 20), ButtonIndex = MouseButton.Left, Pressed = true }; using var up = new InputEventMouseButton { Position = new(20, 20), ButtonIndex = MouseButton.Left }; root.PushInput(down, true); root.PushInput(up, true); }
    private static void ActionInput(Viewport root) { using var down = new InputEventAction { Action = "ui_accept", Pressed = true }; using var up = new InputEventAction { Action = "ui_accept" }; root.PushInput(down, true); root.PushInput(up, true); }
    private static void Record(CanvasItem item, List<CanvasVertex> vertices, List<CanvasBatch> batches) { item.InvalidateCanvas(); item.PrepareCanvas(); vertices.Clear(); batches.Clear(); item.AppendCanvas(vertices, batches, Transform.Identity); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
