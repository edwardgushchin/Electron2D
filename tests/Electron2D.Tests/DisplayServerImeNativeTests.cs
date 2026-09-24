using System.Runtime.InteropServices;
using Electron2D;
using SDL3;

internal static class DisplayServerImeNativeTests
{
    public static void RunMove()
    {
        using var display = DisplayServer.Open("Electron2D IME scale move test", new Vector2i(400, 300));
        Check(display.GetName() == "Wayland", "The IME scale move test requires Wayland.");
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is [var window] && window != 0,
            "The IME scale move test needs the native main window.");
        var nativeWindow = windows![0];
        var renderer = SDL.CreateRenderer(nativeWindow, "software");
        Check(renderer != 0, $"The IME scale move test cannot draw: {SDL.GetError()}");
        try
        {
            Check(SDL.SetRenderDrawColor(renderer, 35, 80, 180, 255), "The test sets a visible blue surface.");
            Console.WriteLine("IME scale move window ready: drag it to the 1.25-scale monitor and leave it there.");
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline)
            {
                display.ProcessEvents();
                Check(SDL.RenderClear(renderer) && SDL.RenderPresent(renderer), "The test presents its window.");
                var density = SDL.GetWindowPixelDensity(nativeWindow);
                if (Math.Abs(density - 1.25f) < 0.01f)
                {
                    display.WindowSetIMEActive(true);
                    try
                    {
                        display.WindowSetIMEPosition(new Vector2i(125, 75));
                        Check(SDL.GetTextInputArea(nativeWindow, out var area, out var cursor) &&
                              area.X == 100 && area.Y == 60 && area.W == 1 && area.H == 10 && cursor == 0,
                            "The 1.25-scale candidate area uses SDL logical coordinates (100, 60).");
                    }
                    finally
                    {
                        display.WindowSetIMEActive(false);
                    }
                    Console.WriteLine("Wayland IME candidate position passed at pixel density 1.25: (125,75) client -> (100,60) native.");
                    return;
                }
                Thread.Sleep(20);
            }
            throw new InvalidOperationException("The window did not reach pixel density 1.25 within 30 seconds.");
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
        }
    }

    public static void Run(DisplayServer display)
    {
        if (display.GetName() != "Wayland")
            return;

        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is [var window] && window != 0,
            "The IME check needs the native main window.");
        var nativeWindow = windows![0];
        var windowId = SDL.GetWindowID(nativeWindow);
        display.ProcessEvents();

        var editing = new List<(string Text, Vector2i Selection)>();
        var committed = new List<(string Text, string Composition, Vector2i Selection)>();
        void OnEditing(string text, Vector2i selection)
        {
            Check(display.IMEGetText() == text && display.IMEGetSelection() == selection,
                "Composition state commits before the editing callback.");
            editing.Add((text, selection));
        }
        void OnInput(string text) => committed.Add((text, display.IMEGetText(), display.IMEGetSelection()));

        display.TextEditing += OnEditing;
        display.TextInput += OnInput;
        display.WindowSetIMEActive(true);
        try
        {
            Check(SDL.TextInputActive(nativeWindow), "Text input starts for the native window.");
            var density = SDL.GetWindowPixelDensity(nativeWindow);
            Check(float.IsFinite(density) && density > 0f, "The native window reports a valid pixel density.");
            var caret = new Vector2i(125, 75);
            display.WindowSetIMEPosition(caret);
            var logicalCaret = new Vector2i(
                checked((int)Math.Round(caret.X / (double)density, MidpointRounding.AwayFromZero)),
                checked((int)Math.Round(caret.Y / (double)density, MidpointRounding.AwayFromZero)));
            Check(SDL.GetTextInputArea(nativeWindow, out var area, out var cursor) &&
                  area.X == logicalCaret.X && area.Y == logicalCaret.Y && area.W == 1 && area.H == 10 && cursor == 0,
                "The candidate area converts the client-pixel caret to native logical coordinates.");
            Console.WriteLine($"Wayland IME candidate density: {density}; client caret: {caret}; native caret: {logicalCaret}.");

            PushEditing(display, windowId, "a🙂", 1, 1);
            Check(editing is [("a🙂", { X: 1, Y: 1 })] &&
                  display.IMEGetText() == "a🙂" && display.IMEGetSelection() == new Vector2i(1, 1),
                "Composition text and codepoint selection are observable after event delivery.");

            PushEditing(display, windowId, "é", -1, -1);
            Check(editing is [_, ("é", { X: 0, Y: 0 })] &&
                  display.IMEGetSelection() == Vector2i.Zero,
                "Unknown native selection offsets become an empty selection.");

            PushInput(display, windowId, "é");
            Check(committed is [("é", "", { X: 0, Y: 0 })] &&
                  display.IMEGetText() == string.Empty && display.IMEGetSelection() == Vector2i.Zero,
                "Committed text clears the composition before callback delivery.");

            PushEditing(display, windowId, "pending", 0, 0);
        }
        finally
        {
            display.WindowSetIMEActive(false);
            display.TextEditing -= OnEditing;
            display.TextInput -= OnInput;
        }
        Check(!SDL.TextInputActive(nativeWindow) && display.IMEGetText() == string.Empty &&
              display.IMEGetSelection() == Vector2i.Zero,
            "Disabling text input clears any unfinished composition.");
    }

    private static void PushEditing(DisplayServer display, uint windowId, string text, int start, int length)
    {
        var pointer = Marshal.StringToCoTaskMemUTF8(text);
        try
        {
            var nativeEvent = new SDL.Event
            {
                Edit = new SDL.TextEditingEvent
                {
                    Type = SDL.EventType.TextEditing,
                    WindowID = windowId,
                    Text = pointer,
                    Start = start,
                    Length = length,
                },
            };
            Check(SDL.PushEvent(ref nativeEvent), "SDL accepts a synthetic composition update.");
            display.ProcessEvents();
        }
        finally
        {
            Marshal.FreeCoTaskMem(pointer);
        }
    }

    private static void PushInput(DisplayServer display, uint windowId, string text)
    {
        var pointer = Marshal.StringToCoTaskMemUTF8(text);
        try
        {
            var nativeEvent = new SDL.Event
            {
                Text = new SDL.TextInputEvent
                {
                    Type = SDL.EventType.TextInput,
                    WindowID = windowId,
                    Text = pointer,
                },
            };
            Check(SDL.PushEvent(ref nativeEvent), "SDL accepts a synthetic text commit.");
            display.ProcessEvents();
        }
        finally
        {
            Marshal.FreeCoTaskMem(pointer);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
