using System.Runtime.InteropServices;
using Electron2D;
using SDL = SDL3.SDL;

internal static partial class ViewportTextInputTests
{
    internal static void RunNative(string backend)
    {
        if (backend is not "gpu" and not "compatibility") throw new ArgumentException("Unknown renderer.", nameof(backend));
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var root = new Window { Title = "Focused text root", Size = new(460, 320), GUIEmbedSubwindows = true };
        var line = new LineEdit { Text = "old root", Size = new(200, 30) };
        var document = new TextEdit { Text = "old document", Position = new(0, 36), Size = new(200, 85) };
        var container = new SubViewportContainer { Position = new(0, 130), Size = new(200, 85) };
        var sub = new SubViewport { Size = new(200, 85) }; var subEditor = new TextEdit { Text = "old sub", Size = new(200, 85) };
        var embedded = new Window { Title = "Embedded text", Position = new(230, 60), Size = new(200, 90), Borderless = true, Visible = false };
        var embeddedLine = new LineEdit { Text = "old embedded", Size = new(180, 30) };
        var native = new Window { Title = "Focused text native", Size = new(240, 120), ForceNative = true };
        var code = new CodeEdit { Text = "old code", Size = new(230, 110) };
        root.AddChild(line); root.AddChild(document); root.AddChild(container); container.AddChild(sub); sub.AddChild(subEditor);
        root.AddChild(embedded); embedded.AddChild(embeddedLine); root.AddChild(native); native.AddChild(code);
        var changed = 0; var commits = 0; var sets = 0; var frames = 0;
        line.TextChanged += _ => changed++; embeddedLine.TextChanged += _ => changed++;
        foreach (var editor in new TextEdit[] { document, subEditor, code }) { editor.TextChanged += () => changed++; editor.TextSet += () => sets++; editor.TextInput += _ => commits++; }
        line.TextInput += _ => commits++; embeddedLine.TextInput += _ => commits++;
        nint pointer = 0; ulong beforeRoot = 0, beforeNative = 0;
        try
        {
            root.Ready += _ =>
            {
                code.SetLineAsBookmarked(0, true); code.SetLineAsBreakpoint(0, true);
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    if (frames == 1)
                    {
                        beforeRoot = Pixels(root); beforeNative = Pixels(native);
                        line.GrabFocus(); root.PushTextInput("ROOT 😀");
                        document.GrabFocus(); root.PushTextInput("literal (\r\nTEXT");
                        subEditor.GrabFocus(); sub.PushTextInput("SUB\nviewport");
                        embedded.Show(); embedded.GrabFocus(); embeddedLine.GrabFocus(); root.PushTextInput("EMBEDDED");
                        code.GrabFocus(); native.PushTextInput("NATIVE (\ncode");
                        Check(line.Text == "ROOT 😀" && document.Text == "literal (\nTEXT" && subEditor.Text == "SUB\nviewport" && embeddedLine.Text == "EMBEDDED" && code.Text == "NATIVE (\ncode", $"Public replacement reaches all roles: root={line.Text}, document={document.Text}, sub={subEditor.Text}, embedded={embeddedLine.Text}, native={code.Text}.");
                        Check(code.GetBookmarkedLines().Length == 0 && code.GetBreakpointedLines().Length == 0 && sets == 3 && changed == 0 && commits == 0, "Code TextSet cleanup is synchronous without user-input or deferred replacement notifications.");
                    }
                    else if (frames == 2)
                    {
                        Check(Pixels(root) != beforeRoot && Pixels(native) != beforeNative, "Replacement changes actual rendered pixels on root and independent child surfaces.");
                        Check(changed == 0, "Replacement remains silent after the deferred batch.");
                        pointer = Marshal.StringToCoTaskMemUTF8("+");
                        var window = SDL.GetWindows(out var nativeCount)!.Single(handle => SDL.GetWindowTitle(handle) == native.Title);
                        var input = new SDL.Event { Text = new SDL.TextInputEvent { Type = SDL.EventType.TextInput, WindowID = SDL.GetWindowID(window), Text = pointer } };
                        Check(SDL.PushEvent(ref input), "Native text producer accepts a committed insertion after replacement.");
                    }
                    else
                    {
                        Check(code.Text == "+NATIVE (\ncode" && commits == 1 && changed == 1, "Native commit remains a separate insertion with its ordinary signals.");
                        root.Tree!.Quit();
                    }
                };
            };
            Check(Engine.Run(root) == 0 && frames == 3, "Native replacement completes the ordinary engine lifecycle.");
        }
        finally { if (pointer != 0) Marshal.FreeCoTaskMem(pointer); ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
        Console.WriteLine($"Public focused text replacement, native insertion and actual root/child pixels passed on {backend}.");
    }

    private static ulong Pixels(Window window)
    {
        using var image = RenderingServer.Service!.ReadbackNativeWindow(window.GetWindowID());
        ulong hash = 14695981039346656037;
        foreach (var value in image.GetData()) hash = unchecked((hash ^ value) * 1099511628211);
        return hash;
    }
}
