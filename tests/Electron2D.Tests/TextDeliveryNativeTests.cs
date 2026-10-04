using System.Runtime.InteropServices;
using Electron2D;
using SDL = SDL3.SDL;

internal static partial class RenderingRuntimeTests
{
    private sealed class NativeTextControl : Control
    {
        internal readonly List<string> Order = [];
        internal string? Commit;
        internal (string Text, Vector2i Selection)? Preedit;
        internal int KeyInputs;

        internal NativeTextControl()
        {
            Name = "TextTarget"; Size = new(100, 30); FocusMode = FocusMode.All; InputEnabled = true;
            TextInput += text => Order.Add("event:" + text);
            IMECompositionChanged += (text, selection) => Order.Add("composition-event:" + text);
        }

        protected override void OnTextInput(string text)
        {
            Check(DisplayServer.IMEGetText() == string.Empty && DisplayServer.IMEGetSelection() == Vector2i.Zero,
                "The native commit clears preedit before scene delivery.");
            Commit = text; Order.Add("commit:" + text);
        }

        protected override void OnIMECompositionChanged(string text, Vector2i selection)
        {
            Preedit = (text, selection); Order.Add("composition:" + text);
        }

        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationOsImeUpdate) return;
            Check(DisplayServer.IMEGetText() == "a🙂" && DisplayServer.IMEGetSelection() == new Vector2i(1, 1),
                "Native composition state is committed before the scene-wide notification.");
            Order.Add("notify");
        }

        protected override void OnInput(InputEvent inputEvent)
        {
            if (inputEvent is InputEventKey) KeyInputs++;
        }
    }

    private static void VerifyTextDeliveryNative(string backend)
    {
        var window = new Window { Size = new(160, 90) };
        var control = new NativeTextControl(); window.AddChild(control);
        var frames = 0;
        nint preeditPointer = 0, commitPointer = 0, orphanPointer = 0;
        try
        {
            window.Ready += _ =>
            {
                var windows = SDL.GetWindows(out var count);
                Check(count == 1 && windows is { Length: 1 }, "The native text test owns one active window.");
                var windowID = SDL.GetWindowID(windows![0]);
                control.GrabFocus(); window.SetIMEActive(true);
                preeditPointer = Marshal.StringToCoTaskMemUTF8("a🙂");
                commitPointer = Marshal.StringToCoTaskMemUTF8("é👩‍🚀");
                var editing = new SDL.Event
                {
                    Edit = new SDL.TextEditingEvent
                    {
                        Type = SDL.EventType.TextEditing,
                        WindowID = windowID,
                        Text = preeditPointer,
                        Start = 1,
                        Length = 1
                    }
                };
                var commit = new SDL.Event
                {
                    Text = new SDL.TextInputEvent
                    {
                        Type = SDL.EventType.TextInput,
                        WindowID = windowID,
                        Text = commitPointer
                    }
                };
                Check(SDL.PushEvent(ref editing) && SDL.PushEvent(ref commit),
                    "The native queue accepts composition and a multi-scalar commit in order.");
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    if (frames == 1)
                    {
                        Check(control.Preedit is { Text: "a🙂", Selection: { X: 1, Y: 1 } } &&
                              control.Commit == "é👩‍🚀" && control.KeyInputs == 0 &&
                              control.Order.SequenceEqual(new[]
                              { "notify", "composition:a🙂", "composition-event:a🙂", "commit:é👩‍🚀", "event:é👩‍🚀" }),
                            $"The {backend} root scene receives exact IME and committed-text phases without fabricated key events.");
                        control.ReleaseFocus();
                        orphanPointer = Marshal.StringToCoTaskMemUTF8("ignored");
                        var orphan = new SDL.Event
                        {
                            Text = new SDL.TextInputEvent
                            { Type = SDL.EventType.TextInput, WindowID = windowID, Text = orphanPointer }
                        };
                        Check(SDL.PushEvent(ref orphan), "The native queue accepts a commit without a GUI focus owner.");
                    }
                    else
                    {
                        Check(control.Order.Count == 5 && control.Commit == "é👩‍🚀",
                            $"The {backend} unfocused control does not receive a later native commit.");
                        window.SetIMEActive(false);
                        window.Tree!.Quit();
                    }
                };
            };
            Engine.Run(window);
        }
        finally
        {
            if (preeditPointer != 0) Marshal.FreeCoTaskMem(preeditPointer);
            if (commitPointer != 0) Marshal.FreeCoTaskMem(commitPointer);
            if (orphanPointer != 0) Marshal.FreeCoTaskMem(orphanPointer);
        }
        Released(window);
        Check(frames == 2, $"The {backend} text bridge completed two frames.");
        Console.WriteLine($"Native focused text and IME scene delivery passed on {backend}.");
    }
}
