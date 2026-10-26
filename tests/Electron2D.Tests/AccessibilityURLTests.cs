using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;

internal static class AccessibilityURLTests
{
    private const string First = "https://example.invalid/путь?value=😀&encoded=%20";
    private const string Second = "file:///tmp/uri%20target.txt#section";
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private sealed class ProbeLink : LinkButton
    {
        internal string? Activated;
        internal bool Fail;
        internal int Calls;
        protected override string OnAccessibilityGetContextualInfo() => URI;
        protected override void OnPressed() { Activated = URI; Calls++; if (Fail) throw new InvalidOperationException("Injected link activation failure"); }
    }
    private static (PackedScene Scene, LinkButton Link) StoredLink()
    {
        using var source = new LinkButton("Stored link") { URI = First }; var scene = new PackedScene(); scene.Pack(source);
        var copy = (LinkButton)scene.Instantiate(); Check(copy.URI == First && !copy.GetAccessibilityElement().IsValid(), "Authored URI survives packing without native identity."); return (scene, copy);
    }
    internal static void RunManaged()
    {
        Reject<ArgumentNullException>(() => AccessibilityServer.UpdateSetURL(default, null!));
        Reject<NotSupportedException>(() => AccessibilityServer.UpdateSetURL(default, "https://example.invalid"));
        var packed = StoredLink(); using var scene = packed.Scene; using var stored = packed.Link; using var duplicate = (LinkButton)stored.Duplicate(); Check(duplicate.URI == First && !duplicate.GetAccessibilityElement().IsValid(), "Duplication preserves URI but not native identity.");
        Console.WriteLine($"PASS {_checks} managed URL publication and authoring contracts.");
    }
    internal static void RunNative(string backend)
    {
        RunManaged(); ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D URL Probe"); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(720, 420), GUIEmbedSubwindows = true };
        var link = new ProbeLink { Text = "Live link", URI = First, Position = new(16, 16), Size = new(300, 45) };
        var packed = StoredLink(); using var scene = packed.Scene; var stored = packed.Link; stored.Position = new(16, 80); stored.Size = new(300, 40);
        var destination = new Label("Inside target") { Position = new(16, 140), Size = new(300, 35) };
        var holder = new Control { Position = new(370, 16), Size = new(320, 70) };
        var status = new Label { Text = "Ready", Position = new(16, 300), Size = new(660, 90) };
        foreach (var node in new Control[] { link, stored, destination, holder, status }) window.AddChild(node);
        Process? client = null; Task<string>? output = null, error = null; RID producer = default, controller = default;
        var complete = false; var clientDone = false; var captured = false; var post = 0; var measuring = false; long before = 0, idle = 0, active = 0;
        using var texture = window.GetTexture();
        void Command(int id)
        {
            switch (id)
            {
                case 1: AccessibilityServer.UpdateSetURL(producer, First); status.Text = "URL added"; break;
                case 2: AccessibilityServer.UpdateSetInPageLinkTarget(producer, destination.GetAccessibilityElement()); status.Text = "Hybrid"; break;
                case 3: AccessibilityServer.UpdateSetURL(producer, ""); status.Text = "Internal only"; break;
                case 4: AccessibilityServer.UpdateSetURL(producer, Second); destination.Dispose(); status.Text = "Target retired"; break;
                case 5: AccessibilityServer.UpdateSetURL(producer, ""); status.Text = "URL cleared"; break;
                case 6: AccessibilityServer.UpdateSetURL(producer, "relative/\0😀"); status.Text = "Unicode"; break;
                case 7: AccessibilityServer.UpdateSetURL(producer, "https://example.invalid/" + new string('λ', 3000)); status.Text = "Long URL"; break;
                case 8: AccessibilityServer.UpdateSetURL(link.GetAccessibilityElement(), "metadata:override"); Check(link.URI == First && link.Calls == 0, "Publishing metadata does not change the configured URI or activate it."); status.Text = "Override"; break;
                case 9: Check(link.Calls == 1 && link.Activated == First, "Native Click uses the actual configured URI through the public activation hook."); link.URI = Second; status.Text = "Source changed"; break;
                case 10: AccessibilityServer.UpdateSetURL(link.GetAccessibilityElement(), ""); status.Text = "Source override cleared"; break;
                case 11: link.URI = First; status.Text = "Source restored"; break;
                case 12: window.RemoveChild(link); holder.AddChild(link); status.Text = "Reparented"; break;
                case 13: AccessibilityServer.FreeElement(producer); status.Text = "Producer retired"; break;
                case 99: complete = true; status.Text = "Measuring"; break;
                default: throw new InvalidOperationException("Unexpected URI command");
            }
        }
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Requested backend and real native bridge execute.");
            var root = window.GetAccessibilityElement(); producer = AccessibilityServer.CreateSubElement(root, Role.Button); AccessibilityServer.UpdateSetName(producer, "Generic link");
            controller = AccessibilityServer.CreateSubElement(root, Role.Button); AccessibilityServer.UpdateSetName(controller, "URL controller");
            AccessibilityServer.UpdateAddAction(controller, AAction.Custom, (int id) => Command(id)); foreach (var id in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 99 }) AccessibilityServer.UpdateAddCustomAction(controller, id, "command-" + id);
            Reject<KeyNotFoundException>(() => AccessibilityServer.UpdateSetURL(default, ""));
            Task.Run(() => Reject<InvalidOperationException>(() => AccessibilityServer.UpdateSetURL(producer, First))).GetAwaiter().GetResult();
            client = Process.Start(Client())!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96 && post < 160); if (measuring) before = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 80) { var even = (post & 1) == 0; link.URI = even ? First : Second; stored.URI = even ? First : ""; }
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (!captured && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture) { captured = true; Directory.CreateDirectory(capture); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(capture, "native-url-" + backend + ".png")); }
            if (complete && ++post == 160) Check(idle == 0 && active == 0, $"Prepared URL idle/active owner-frame managed bytes {idle}/{active}.");
        }
        var started = Stopwatch.GetTimestamp();
        try { Check(Engine.Run((_, _) => { if (!clientDone && client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && complete, "Independent native URI client completes."); clientDone = true; } if (clientDone && post >= 160) window.Tree!.Quit(); if (Stopwatch.GetElapsedTime(started).TotalSeconds > 70) throw new TimeoutException("Native URI client timeout."); }, window) == 0, "URL host closes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        Check(!AccessibilityServer.HasElement(producer) && !AccessibilityServer.HasElement(controller), "Retirement releases native identities.");
        RunFailure(); Console.WriteLine($"PASS {_checks} URL source/native/lifetime contracts on {backend}; 64 idle/64 active {idle}/{active} managed bytes.");
    }
    private static ProcessStartInfo Client(string? mode = null)
    { var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/url.py")); if (mode is not null) start.ArgumentList.Add(mode); return start; }
    private static void RunFailure()
    {
        var window = new Window { Size = new(280, 120) }; var link = new ProbeLink { Text = "Failing URL", URI = First, Fail = true, Size = new(240, 80) }; window.AddChild(link);
        Process? client = null; Task<string>? output = null, error = null; var failed = false; var started = Stopwatch.GetTimestamp();
        window.Ready += _ => { client = Process.Start(Client("--failure"))!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync(); };
        try { Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Native URL failure client timeout."); }, window); }
        catch (AggregateException errors) { failed = errors.Flatten().InnerExceptions.Any(e => e is InvalidOperationException { Message: "Injected link activation failure" }); }
        finally { if (client is { HasExited: false } && !client.WaitForExit(5000)) client.Kill(true); if (output is not null) Console.Write(output.GetAwaiter().GetResult()); if (error is not null) Console.Write(error.GetAwaiter().GetResult()); Check(failed && client?.ExitCode == 0 && link.Activated == First, "Native URL activation retains failure/owner/wire semantics."); client?.Dispose(); }
        var fresh = new Window { Size = new(120, 80) }; fresh.Ready += _ => { Check(AccessibilityServer.IsSupported(), "Native bridge reopens after link failure."); fresh.Tree!.Quit(); }; Check(Engine.Run(fresh) == 0, "Fresh display closes.");
    }
}
