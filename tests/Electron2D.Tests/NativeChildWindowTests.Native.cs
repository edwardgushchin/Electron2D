using Electron2D;
using System.Runtime.InteropServices;
using SDL = SDL3.SDL;

internal static partial class NativeChildWindowTests
{
    private sealed class Paint : Control
    {
        internal int Draws, Keys;
        internal Color Ink;
        internal Paint(Color ink) { Ink = ink; Size = new(256, 256); InputEnabled = true; }
        protected override void OnDraw() { Draws++; DrawRect(new(Vector2.Zero, Size), Ink); }
        protected override void OnInput(InputEvent input) { if (input is InputEventKey { Pressed: true }) Keys++; }
    }

    private sealed class TextSink : Control
    {
        internal string? Committed;
        internal int Compositions;
        internal TextSink() { Position = new(0, 48); Size = new(32, 16); FocusMode = FocusMode.All; }
        protected override void OnTextInput(string text)
        {
            Check(DisplayServer.IMEGetText() == string.Empty, "Commit clears this window's composition before delivery.");
            Committed = text;
        }
        protected override void OnIMECompositionChanged(string text, Vector2i selection)
        {
            Check(DisplayServer.IMEGetText() == text && DisplayServer.IMEGetSelection() == selection, "Global IME observation follows the originating window.");
            if (text.Length != 0) Compositions++;
        }
    }

    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Title = "Native Root", Size = new(96, 80), Borderless = true };
        var first = new Window { Name = "First", Title = "Native First", Size = new(96, 80), Borderless = true };
        var second = new Window { Name = "Second", Title = "Native Second", Size = new(96, 80), Borderless = true };
        var red = new Paint(Colors.Red); var blue = new Paint(Colors.Blue);
        first.AddChild(red); second.AddChild(blue); root.AddChild(first); root.AddChild(second);
        var firstText = new TextSink(); var secondText = new TextSink(); first.AddChild(firstText); second.AddChild(secondText);
        var preedit = Marshal.StringToCoTaskMemUTF8("a🙂");
        var firstCommit = Marshal.StringToCoTaskMemUTF8("first é"); var secondCommit = Marshal.StringToCoTaskMemUTF8("second 漢");
        root.AddChild(new TextureRect { Texture = first.GetTexture(), Size = new(64, 64) });
        var once = new SubViewport { Size = new(8, 8), RenderTargetUpdateMode = ViewportUpdateMode.Once };
        once.AddChild(new ColorRect { Size = new(8, 8), Color = Colors.Green }); root.AddChild(once);
        first.AddChild(new TextureRect { Texture = once.GetTexture(), Position = new(24, 0), Size = new(8, 8) });
        var feedbackA = new SubViewport { Size = new(8, 8), RenderTargetUpdateMode = ViewportUpdateMode.Always };
        var feedbackB = new SubViewport { Size = new(8, 8), RenderTargetUpdateMode = ViewportUpdateMode.Always };
        feedbackA.AddChild(new ColorRect { Size = new(8, 8), Color = Colors.Yellow });
        feedbackA.AddChild(new TextureRect { Texture = feedbackB.GetTexture(), Position = new(0, 0), Size = new(4, 4) });
        feedbackB.AddChild(new ColorRect { Size = new(8, 8), Color = Colors.Magenta });
        feedbackB.AddChild(new TextureRect { Texture = feedbackA.GetTexture(), Position = new(4, 4), Size = new(4, 4) });
        root.AddChild(feedbackA); root.AddChild(feedbackB);
        first.AddChild(new TextureRect { Texture = feedbackA.GetTexture(), Position = new(40, 0), Size = new(8, 8) });
        var history = new SubViewport { Size = new(16, 8), RenderTargetUpdateMode = ViewportUpdateMode.Always, RenderTargetClearMode = ViewportClearMode.Never };
        var historyMark = new ColorRect { Size = new(8, 8), Color = Colors.Red }; history.AddChild(historyMark); root.AddChild(history);
        first.AddChild(new TextureRect { Texture = history.GetTexture(), Position = new(56, 0), Size = new(16, 8) });
        var phase = 0; var frames = 0; var retired = -1; var closeRequests = 0;
        long allocationStart = 0, allocated = 0;
        int stableRedDraws = 0, stableBlueDraws = 0;
        var renderer = default(RenderingServer)!;
        SubViewport? policyHost = null;
        Window? policyWindow = null, grandchild = null;
        void Pixel(Image image, Color expected, string message)
        {
            var actual = image.GetPixel(16, 16);
            Check(MathF.Abs(actual.R - expected.R) < .025f && MathF.Abs(actual.G - expected.G) < .025f && MathF.Abs(actual.B - expected.B) < .025f,
                backend + " phase " + phase + " " + message + ": " + actual);
        }
        void Captured()
        {
            if (phase >= 4)
            {
                if (frames == 0)
                {
                    using var pixels = renderer.ReadbackNativeWindow(first.GetWindowID()); Pixel(pixels, Colors.Red, "child presentation continues under a hidden root");
                }
                if (frames >= 32) allocated += GC.GetAllocatedBytesForCurrentThread() - allocationStart;
                if (++frames == 32) { stableRedDraws = red.Draws; stableBlueDraws = blue.Draws; }
                if (frames < 96) return;
                Check(allocated == 0, backend + " prepared multiwindow frames allocate " + allocated + " managed owner bytes.");
                Check(red.Draws == stableRedDraws + (phase == 5 ? 64 : 0) && blue.Draws == stableBlueDraws, "Prepared presentation records each active scene draw once and never replays callbacks per window.");
                Console.WriteLine(backend + " 64 prepared " + (phase == 5 ? "changing" : "unchanged") + " multiwindow frames: 0 managed owner bytes.");
                if (phase == 4) { phase++; frames = 0; allocated = 0; return; }
                root.Tree!.Quit(); return;
            }
            if (++frames < 3) return;
            using (var image = first.GetTexture().GetImage()!) Pixel(image, Colors.Red, "first authoritative viewport");
            using (var image = renderer.ReadbackNativeWindow(first.GetWindowID())) Pixel(image, Colors.Red, "first actual presentation");
            using (var image = renderer.ReadbackNativeWindow(second.GetWindowID())) Pixel(image, Colors.Blue, "second actual presentation");
            using (var actual = renderer.ReadbackNativeWindow(first.GetWindowID()))
            using (var expected = first.GetTexture().GetImage()!)
            {
                Check(actual.GetPixel(28, 4).G > .45f, "A once/disabled source survives native surface recreation.");
                Check(actual.GetPixel(60, 4).R > .95f && (phase == 0 || actual.GetPixel(68, 4).G > .45f), "ClearNever preserves old pixels and the later changed region across cold recreation.");
                for (var y = 0; y < 8; y++) for (var x = 0; x < 8; x++)
                        Check(actual.GetPixel(40 + x, y).IsEqualApprox(expected.GetPixel(40 + x, y)), "Cyclic viewport history matches authoritative canvas output.");
            }
            if (phase == 0)
            {
                using var image = renderer.ReadbackNativeWindow(0); Pixel(image, Colors.Red, "cross-window texture sampling");
                historyMark.Position = new(8, 0); historyMark.Color = Colors.Green;
                Check(red.Draws == 1 && blue.Draws == 1, "Native presentation reuses one retained scene recording.");
                Reject<InvalidOperationException>(() => first.ForceNative = true);
                Reject<InvalidOperationException>(() => root.GUIEmbedSubwindows = true);
                var dialogCount = typeof(DisplayServer).GetField("_pendingNativeDialogs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                var ownedDialogs = typeof(DisplayServer).GetField("_pendingOwnedNativeDialogs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                dialogCount.SetValue(DisplayServer.Service, 1); ownedDialogs.SetValue(first.NativeDisplay, 1);
                try
                {
                    var id = first.GetWindowID();
                    Reject<InvalidOperationException>(first.Hide); Reject<InvalidOperationException>(first.Dispose);
                    Reject<InvalidOperationException>(() => root.RemoveChild(first));
                    Check(first.Visible && !first.IsDisposed && first.Parent == root && first.GetWindowID() == id, "A native dialog parent rejects hide, removal and disposal before ownership changes.");
                }
                finally { ownedDialogs.SetValue(first.NativeDisplay, 0); dialogCount.SetValue(DisplayServer.Service, 0); }
                var count = DisplayServer.GetWindowList().Length;
                var failing = new Window { Name = "FailedHost", Visible = false, Transparent = true, Borderless = true };
                root.AddChild(failing);
                Reject<NotSupportedException>(failing.Show);
                Check(!failing.Visible && failing.GetWindowID() == -1 && DisplayServer.GetWindowList().Length == count, "Failed native startup leaves visibility and the display census intact.");
                failing.Transparent = false; failing.ForceNative = true; failing.Show();
                Check(failing.GetWindowID() > 0, "A failed child can activate after its unsupported policy is corrected.");
                failing.SetUnparentWhenInvisible(true); failing.Hide();
                Check(failing.Parent is null && failing.GetWindowID() == -1 && !failing.IsDisposed, "Native hide can unparent without orphaning presentation resources.");
                failing.Dispose();
                policyHost = new SubViewport { Name = "PolicyHost", Size = new(96, 80), GUIEmbedSubwindows = true, RenderTargetUpdateMode = ViewportUpdateMode.Always };
                policyWindow = new Window { Name = "ForcedNative", Title = "Forced Native", Size = new(96, 80), Visible = false, ForceNative = true, Borderless = true };
                policyWindow.Transient = true; policyWindow.TransientToFocused = true;
                policyWindow.AddChild(new Paint(Colors.Yellow)); policyHost.AddChild(policyWindow); root.AddChild(policyHost);
                var expectedParent = SDL.GetKeyboardFocus(); if (expectedParent == 0) expectedParent = SDL.GetWindowFromID(NativeIDForScene(root));
                policyWindow.Show();
                Check(SDL.GetWindowParent(SDL.GetWindowFromID(NativeIDForScene(policyWindow))) == expectedParent, "TransientToFocused samples the focused native owner before activation.");
                Check(!policyWindow.IsEmbedded() && policyWindow.GetWindowID() > 0, "ForceNative overrides an actual embedding viewport.");
                grandchild = new Window { Name = "NativeGrandchild", Title = "Native Grandchild", Size = new(96, 80), Borderless = true, Transient = true };
                grandchild.AddChild(new Paint(Colors.Yellow)); first.AddChild(grandchild);
                RenderingServer.ViewportAttachToScreen(first.GetViewportRID(), new(Vector2.Zero, first.Size), first.GetWindowID());
                firstText.GrabFocus(); secondText.GrabFocus(); first.SetIMEActive(true); second.SetIMEActive(true);
                foreach (var window in new[] { first, second })
                {
                    var native = NativeIDForScene(window);
                    var key = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = native, Key = SDL.Keycode.A, Scancode = SDL.Scancode.A, Down = true } };
                    Check(SDL.PushEvent(ref key), "Queue per-window scene input.");
                    var editing = new SDL.Event { Edit = new SDL.TextEditingEvent { Type = SDL.EventType.TextEditing, WindowID = native, Text = preedit, Start = 1, Length = 1 } };
                    var commit = new SDL.Event { Text = new SDL.TextInputEvent { Type = SDL.EventType.TextInput, WindowID = native, Text = window == first ? firstCommit : secondCommit } };
                    Check(SDL.PushEvent(ref editing) && SDL.PushEvent(ref commit), "Queue per-window composition and text.");
                }
            }
            else if (phase == 1)
            {
                Check(red.Keys == 1 && blue.Keys == 1, "Scene input stays inside its native window viewport.");
                history.RenderTargetUpdateMode = ViewportUpdateMode.Disabled;
                Check(firstText.Committed == "first é" && secondText.Committed == "second 漢" && firstText.Compositions == 1 && secondText.Compositions == 1, "Native text and composition reach the focused control in their own window: first=" + firstText.Committed + "/" + firstText.Compositions + ", second=" + secondText.Committed + "/" + secondText.Compositions + ", focus=" + firstText.HasFocus() + "/" + secondText.HasFocus());
                using (var image = renderer.ReadbackNativeWindow(policyWindow!.GetWindowID())) Pixel(image, Colors.Yellow, "forced native child output");
                policyWindow.Hide(); policyWindow.ForceNative = false; policyWindow.Position = Vector2i.Zero; policyWindow.Show();
                Check(policyWindow.IsEmbedded() && policyWindow.GetWindowID() == -1, "A hidden child can switch to the existing embedded host.");
                first.Transient = true; first.Exclusive = true;
                Check(SDL.GetWindowParent(SDL.GetWindowFromID(NativeIDForScene(first))) == SDL.GetWindowFromID(NativeIDForScene(root)), "Native transient parent uses the scene owner.");
                first.Exclusive = false; first.Transient = false;
                retired = first.GetWindowID(); var texture = first.GetTexture(); var rid = first.GetViewportRID();
                var grandchildID = grandchild!.GetWindowID(); var nativeGrandchild = SDL.GetWindowFromID(NativeIDForScene(grandchild));
                first.Hide();
                Check(grandchild.GetWindowID() == grandchildID && SDL.GetWindowFromID(SDL.GetWindowID(nativeGrandchild)) == nativeGrandchild, "Retiring a transient native parent preserves the independent child host.");
                Check(first.GetWindowID() == -1 && !DisplayServer.GetWindowList().Contains(retired), "Hide retires the native identity.");
                first.Show(); Check(first.GetWindowID() > retired && ReferenceEquals(texture, first.GetTexture()) && rid == first.GetViewportRID(), "Show recreates only the native host.");
                Check(SDL.GetWindowParent(nativeGrandchild) == SDL.GetWindowFromID(NativeIDForScene(first)), "A recreated native transient parent relinks its surviving child.");
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowGetSize(retired));
                var secondID = second.GetWindowID(); root.RemoveChild(second);
                Check(second.GetWindowID() == -1 && !DisplayServer.GetWindowList().Contains(secondID), "Detachment retires its native host immediately.");
                root.AddChild(second); Check(second.GetWindowID() > secondID, "Reattachment creates a fresh native host.");
            }
            else if (phase == 2)
            {
                using (var image = policyHost!.GetTexture().GetImage()!) Pixel(image, Colors.Yellow, "the same window executes after switching to embedding");
                using (var image = renderer.ReadbackNativeWindow(grandchild!.GetWindowID())) Pixel(image, Colors.Yellow, "surviving native grandchild presentation");
                grandchild.Dispose(); grandchild = null;
                root.RemoveChild(policyHost); policyHost.Dispose(); policyHost = null; policyWindow = null;
                var close = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = NativeIDForScene(first) } };
                Check(SDL.PushEvent(ref close), "Queue native child close request.");
            }
            else
            {
                Check(closeRequests == 1, "Child close delivery leaves the application running.");
                root.Hide(); Check(!root.Visible && first.Visible && second.Visible, "Independent children remain visible when the root hides.");
                root.Tree!.ProcessFrameStarted += _ =>
                {
                    if (phase >= 4 && frames >= 32) allocationStart = GC.GetAllocatedBytesForCurrentThread();
                    if (phase == 5) { red.Ink = (frames & 1) == 0 ? Colors.Red : Colors.Cyan; red.QueueRedraw(); }
                };
            }
            phase++; frames = 0;
        }
        first.CloseRequested += () => closeRequests++;
        first.FocusEntered += () => Check(Window.GetFocusedWindow() == first, "Native first focus identity commits before its signal.");
        second.FocusEntered += () => Check(Window.GetFocusedWindow() == second, "Native second focus identity commits before its signal.");
        root.Ready += _ =>
        {
            renderer = RenderingServer.Service!;
            Check(root.GetWindowID() == 0 && first.GetWindowID() > 0 && second.GetWindowID() > first.GetWindowID(), "Scene children create distinct native hosts.");
            Check(DisplayServer.HasFeature(DisplayServer.Feature.Subwindows), "The native host advertises its executable child-window capability.");
            var rootPolicy = DisplayServer.WindowGetVSyncMode();
            var secondPolicy = DisplayServer.WindowGetVSyncMode(second.GetWindowID());
            DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled);
            var appliedRoot = DisplayServer.WindowGetVSyncMode();
            DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Enabled, first.GetWindowID());
            Check(DisplayServer.WindowGetVSyncMode() == appliedRoot && DisplayServer.WindowGetVSyncMode(first.GetWindowID()) == DisplayServer.VSyncMode.Enabled && DisplayServer.WindowGetVSyncMode(second.GetWindowID()) == secondPolicy, "Presentation policies address independent windows.");
            DisplayServer.WindowSetVSyncMode(rootPolicy);
            Check(DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, first.GetWindowID()) != DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle), "Borrowed native window handles retain their own identity.");
            if (backend == "compatibility" && RenderingServer.GetCurrentRenderingDriverName() is "opengl" or "opengles2")
                Check(DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.OpenGLContext, first.GetWindowID()) != DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.OpenGLContext), "Compatibility windows retain distinct borrowed contexts without switching on query.");
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Captured;
        };
        try { Engine.Run(root); }
        finally { Marshal.FreeCoTaskMem(preedit); Marshal.FreeCoTaskMem(firstCommit); Marshal.FreeCoTaskMem(secondCommit); }
        Check(root.IsDisposed && first.IsDisposed && second.IsDisposed && !DisplayServer.IsAvailable && RenderingServer.Service is null, "All native hosts retire with the engine session.");
        Console.WriteLine(backend + " native child scene, actual presentation, cross-window sampling, input, hide/show, close and cleanup passed.");
    }

    private static uint NativeIDForScene(Window window)
    {
        var windows = SDL.GetWindows(out _)!;
        return SDL.GetWindowID(windows.Single(native => SDL.GetWindowTitle(native) == window.Title));
    }
}
