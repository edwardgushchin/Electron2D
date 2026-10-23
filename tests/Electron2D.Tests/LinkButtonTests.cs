using Electron2D;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.Win32;

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
        var scheme = "e2d-link-" + Guid.NewGuid().ToString("N"); var uri = scheme + "://stored/path";
        var registryPath = @"Software\Classes\" + scheme; var app = System.IO.Path.Combine(folder, "Handler.app");
        const string register = "/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister";
        var registered = false;
        try
        {
            var scenePath = System.IO.Path.Combine(folder, "link.e2dscene"); var capture = System.IO.Path.Combine(folder, "opened.txt"); var opener = System.IO.Path.Combine(folder, "xdg-open");
            if (OperatingSystem.IsWindows())
            {
                using var existing = Registry.CurrentUser.OpenSubKey(registryPath); Check(existing is null, "The unique URI fixture must not replace an existing handler.");
                var commandFile = System.IO.Path.Combine(folder, "open.cmd"); var outputFile = capture.Replace("%", "%%");
                File.WriteAllText(commandFile, $"@echo off\r\n@chcp 65001 >nul\r\necho %~1>\"{outputFile}.pending\"\r\nmove /y \"{outputFile}.pending\" \"{outputFile}\" >nul\r\n");
                using var key = Registry.CurrentUser.CreateSubKey(registryPath); registered = true;
                key.SetValue("", "URL:" + scheme); key.SetValue("URL Protocol", "");
                using var command = key.CreateSubKey(@"shell\open\command");
                var shell = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
                command.SetValue("", $"\"{shell}\" /d /c \"\"{commandFile}\" \"%1\"\"");
            }
            else if (OperatingSystem.IsMacOS())
            {
                var script = System.IO.Path.Combine(folder, "handler.applescript");
                static string Literal(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                File.WriteAllText(script, "on open location receivedURI\n do shell script \"/usr/bin/printf %s \" & quoted form of receivedURI & \" > \" & quoted form of " + Literal(capture + ".pending") + " & \" && /bin/mv \" & quoted form of " + Literal(capture + ".pending") + " & \" \" & quoted form of " + Literal(capture) + "\n quit\nend open location\n");
                RunFixtureTool("/usr/bin/osacompile", "-o", app, script);
                var plist = System.IO.Path.Combine(app, "Contents", "Info.plist"); var document = XDocument.Load(plist);
                var dictionary = document.Root!.Element("dict")!;
                void Set(string name, XElement value)
                {
                    var key = dictionary.Elements("key").SingleOrDefault(key => key.Value == name);
                    if (key?.ElementsAfterSelf().FirstOrDefault() is { } previous) previous.ReplaceWith(value);
                    else dictionary.Add(new XElement("key", name), value);
                }
                var bundleID = "dev.electron2d.tests." + scheme;
                Set("CFBundleIdentifier", new XElement("string", bundleID));
                Set("CFBundleURLTypes", new XElement("array", new XElement("dict", new XElement("key", "CFBundleTypeRole"), new XElement("string", "Editor"), new XElement("key", "CFBundleURLName"), new XElement("string", bundleID), new XElement("key", "CFBundleURLSchemes"), new XElement("array", new XElement("string", scheme)))));
                Set("LSUIElement", new XElement("true")); document.DocumentType?.Remove(); document.Save(plist);
                RunFixtureTool("/usr/bin/plutil", "-lint", plist);
                RunFixtureTool("/usr/bin/lipo", "-info", System.IO.Path.Combine(app, "Contents", "MacOS", "applet"));
                RunFixtureTool("/usr/bin/codesign", "--force", "--sign", "-", app);
                registered = true; RunFixtureTool(register, "-f", "-v", app);
                BindMacURI(scheme, bundleID);
            }
            else
            {
                File.WriteAllText(opener, "#!/bin/sh\nprintf '%s\\n' \"$1\" > \"$E2D_LINK_CAPTURE.pending\"\nmv \"$E2D_LINK_CAPTURE.pending\" \"$E2D_LINK_CAPTURE\"\n");
                File.SetUnixFileMode(opener, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            using (var source = new LinkButton("Stored link") { URI = uri, FocusMode = FocusMode.All, Underline = LinkButton.UnderlineMode.OnHover })
            using (var scene = new PackedScene()) { scene.Pack(source); ResourceSaver.Save(scene, scenePath, SaverFlags.Compress | SaverFlags.SaveBigEndian); }
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(LinkButtonTests).Assembly.Location);
            start.Environment.Remove("ELECTRON2D_TEST_LINK"); start.Environment["ELECTRON2D_TEST_LINK_CHILD"] = scenePath;
            start.Environment["E2D_LINK_CAPTURE"] = capture; start.Environment["PATH"] = folder + System.IO.Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
            start.Environment["DBUS_SESSION_BUS_ADDRESS"] = "unix:path=" + System.IO.Path.Combine(folder, "absent-bus");
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh link consumer."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh LinkButton passed"), "Fresh link: " + error.GetAwaiter().GetResult());
            Check(File.ReadAllText(capture).Trim() == uri, "Real SDL URL launch reached isolated native process with exact URI.");
        }
        catch
        {
            var plist = System.IO.Path.Combine(app, "Contents", "Info.plist");
            if (OperatingSystem.IsMacOS() && File.Exists(plist)) Console.Error.WriteLine(File.ReadAllText(plist));
            throw;
        }
        finally
        {
            try
            {
                if (registered && OperatingSystem.IsWindows()) Registry.CurrentUser.DeleteSubKeyTree(registryPath, false);
                if (registered && OperatingSystem.IsMacOS()) RunFixtureTool(register, "-u", app);
            }
            finally { Directory.Delete(folder, true); }
        }
    }
    private static unsafe void BindMacURI(string scheme, string bundleID)
    {
        var core = NativeLibrary.Load("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation");
        nint services = 0, schemeRef = 0, bundleRef = 0, handlers = 0, current = 0;
        delegate* unmanaged[Cdecl]<nint, void> release = null;
        try
        {
            release = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(core, "CFRelease");
            var create = (delegate* unmanaged[Cdecl]<nint, nint, uint, nint>)NativeLibrary.GetExport(core, "CFStringCreateWithCString");
            var equal = (delegate* unmanaged[Cdecl]<nint, nint, byte>)NativeLibrary.GetExport(core, "CFEqual");
            var count = (delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(core, "CFArrayGetCount");
            var item = (delegate* unmanaged[Cdecl]<nint, nint, nint>)NativeLibrary.GetExport(core, "CFArrayGetValueAtIndex");
            var copyText = (delegate* unmanaged[Cdecl]<nint, nint, nint, uint, byte>)NativeLibrary.GetExport(core, "CFStringGetCString");
            services = NativeLibrary.Load("/System/Library/Frameworks/CoreServices.framework/CoreServices");
            var claims = (delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(services, "LSCopyAllHandlersForURLScheme");
            var bind = (delegate* unmanaged[Cdecl]<nint, nint, int>)NativeLibrary.GetExport(services, "LSSetDefaultHandlerForURLScheme");
            var read = (delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(services, "LSCopyDefaultHandlerForURLScheme");
            fixed (byte* a = System.Text.Encoding.UTF8.GetBytes(scheme + '\0')) schemeRef = create(0, (nint)a, 0x08000100);
            fixed (byte* b = System.Text.Encoding.UTF8.GetBytes(bundleID + '\0')) bundleRef = create(0, (nint)b, 0x08000100);
            Check(schemeRef != 0 && bundleRef != 0, "Native URI strings must be created.");
            handlers = claims(schemeRef);
            var registeredCount = handlers == 0 ? 0 : count(handlers);
            Console.WriteLine("Native URI registered app count: " + registeredCount);
            Check(registeredCount == 1, "The unique URI scheme must have exactly one registered app.");
            var handler = item(handlers, 0); var text = stackalloc byte[1024];
            Check(copyText(handler, (nint)text, 1024, 0x08000100) != 0, "Native URI claim must decode.");
            Console.WriteLine("Native URI claim: " + Marshal.PtrToStringUTF8((nint)text));
            Check(equal(handler, bundleRef) != 0, "The unique URI scheme must belong to the owned app.");
            var status = bind(schemeRef, bundleRef); Check(status == 0, "Native URI binding failed: " + status);
            current = read(schemeRef);
            Check(current != 0 && equal(current, bundleRef) != 0, "Native URI preferred-handler readback must match.");
        }
        finally
        {
            if (release != null)
            {
                if (current != 0) release(current); if (handlers != 0) release(handlers);
                if (bundleRef != 0) release(bundleRef); if (schemeRef != 0) release(schemeRef);
            }
            if (services != 0) NativeLibrary.Free(services); NativeLibrary.Free(core);
        }
    }
    private static void RunFixtureTool(string tool, params string[] arguments)
    {
        var start = new ProcessStartInfo(tool) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Native URI fixture tool: " + tool); }
        Check(process.ExitCode == 0, $"Native URI fixture tool {tool}: {output.GetAwaiter().GetResult()}{error.GetAwaiter().GetResult()}");
        if (output.Result.Length + error.Result.Length != 0) Console.WriteLine($"Native URI fixture {tool}: {output.Result}{error.Result}");
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
