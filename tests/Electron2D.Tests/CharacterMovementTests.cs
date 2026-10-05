using Electron2D;
using Electron2D.Examples;
using SDL = SDL3.SDL;
using IOPath = System.IO.Path;

internal static class CharacterMovementTests
{
    internal static void Run()
    {
        using var texture = ResourceLoader.Load<ImageTexture>(IOPath.Combine(AppContext.BaseDirectory, "Assets", "mark-dark.svg"));
        using var font = new FontFile();
        font.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
        Engine.MaxFPS = 60;
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        foreach (var backend in new[] { "gpu", "compatibility" })
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            var window = CharacterMovementScene.CreateWindow(texture, font);
            var player = (Sprite)window.GetChild(3);
            var previous = player.Position;
            var frames = 0;
            var step = 0;
            window.Ready += _ => RenderingServer.FramePostDraw += () =>
            {
                Check(++frames < 120, "Resize and close complete within the frame budget.");
                switch (++step)
                {
                    case 1:
                        using (var pixels = RenderingServer.Service!.Readback())
                        {
                            Check(pixels.Size == new Vector2i(800, 600), "Capture dimensions.");
                            Check(pixels.GetPixel(10, 10).IsEqualApprox(Color.FromHTML("#241B2C")), "Scene background is rendered.");
                            Check(pixels.GetPixel(40, 100).IsEqualApprox(Color.FromHTML("#2E2238")), "Grid is rendered.");
                            Check(pixels.GetPixel(368, 300).R > .8f, "Character is rendered.");
                            Directory.CreateDirectory("bin/character-movement");
                            pixels.SavePNG($"bin/character-movement/{backend}.png");
                        }
                        KeyEvent(SDL.Scancode.Right, true); break;
                    case 2:
                        Check(player.Position.X > previous.X, "Right moves the character.");
                        KeyEvent(SDL.Scancode.Right, false); previous = player.Position; break;
                    case 3:
                        Check(player.Position == previous, "Releasing a key stops movement.");
                        KeyEvent(SDL.Scancode.Left, true); break;
                    case 4:
                        Check(player.Position.X < previous.X, "Left moves the character.");
                        KeyEvent(SDL.Scancode.Left, false); previous = player.Position; KeyEvent(SDL.Scancode.Up, true); break;
                    case 5:
                        Check(player.Position.Y < previous.Y, "Up moves the character.");
                        KeyEvent(SDL.Scancode.Up, false); previous = player.Position; KeyEvent(SDL.Scancode.Down, true); break;
                    case 6:
                        Check(player.Position.Y > previous.Y, "Down moves the character.");
                        KeyEvent(SDL.Scancode.Down, false); player.Position = new(719.999f, 479.999f);
                        KeyEvent(SDL.Scancode.Right, true); KeyEvent(SDL.Scancode.Down, true); break;
                    case 7:
                        Check(player.Position == new Vector2(720, 480), "Diagonal movement stays inside the grid.");
                        KeyEvent(SDL.Scancode.Right, false); KeyEvent(SDL.Scancode.Down, false);
                        window.Size = new(1200, 800);
                        break;
                    case 8:
                        using (var pixels = RenderingServer.Service!.Readback())
                        {
                            // The compositor commits resize requests asynchronously.
                            if (pixels.Size != new Vector2i(1200, 800)) { step--; break; }
                            Check(pixels.GetPixel(1080, 700).IsEqualApprox(Color.FromHTML("#2E2238")), "The grid expands into the new window area.");
                            Check(((Label)window.GetChild(2)).Position == new Vector2(32, 752), "Instructions follow the bottom edge.");
                            Check(player.Position == new Vector2(720, 480), "Growing the window preserves the character position.");
                            pixels.SavePNG($"bin/character-movement/{backend}-wide.png");
                        }
                        player.Position = new(1119.999f, 679.999f);
                        KeyEvent(SDL.Scancode.Right, true); KeyEvent(SDL.Scancode.Down, true);
                        break;
                    case 9:
                        Check(player.Position == new Vector2(1120, 680), "Movement uses the enlarged field boundary.");
                        KeyEvent(SDL.Scancode.Right, false); KeyEvent(SDL.Scancode.Down, false);
                        window.Size = new(500, 400);
                        break;
                    case 10:
                        using (var pixels = RenderingServer.Service!.Readback())
                        {
                            if (pixels.Size != new Vector2i(500, 400)) { step--; break; }
                            Check(player.Position == new Vector2(420, 280), "Shrinking keeps the whole character inside the field.");
                            Check(((Label)window.GetChild(2)).Position == new Vector2(32, 352), "Instructions remain visible after shrinking.");
                        }
                        window.Size = window.MinSize;
                        break;
                    case 11:
                        using (var pixels = RenderingServer.Service!.Readback())
                        {
                            if (pixels.Size != new Vector2i(400, 300)) { step--; break; }
                            Check(player.Position == new Vector2(320, 180), "Minimum size keeps valid character bounds.");
                            Check(((Label)window.GetChild(2)).Position == new Vector2(32, 252), "Minimum size retains the instruction margin.");
                            Check(pixels.GetPixel(40, 100).IsEqualApprox(Color.FromHTML("#2E2238")), "Minimum-size grid is rendered.");
                            pixels.SavePNG($"bin/character-movement/{backend}-small.png");
                        }
                        window.Size = new(800, 600);
                        break;
                    case 12:
                        using (var pixels = RenderingServer.Service!.Readback())
                        {
                            if (pixels.Size != new Vector2i(800, 600)) { step--; break; }
                            Check(player.Position == new Vector2(320, 180), "Restoring the size preserves the clamped position.");
                        }
                        if (backend == "gpu") KeyEvent(SDL.Scancode.Escape, true);
                        else
                        {
                            var windows = SDL.GetWindows(out var count);
                            Check(count == 1, "One native window.");
                            var close = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(windows![0]) } };
                            Check(SDL.PushEvent(ref close), "Native close event accepted.");
                        }
                        break;
                }
            };
            Check(Engine.Run(window) == 0 && step >= 12, "Scene exits successfully after resize checks.");
            Check(window.IsDisposed && player.IsDisposed && !texture.IsDisposed && !font.IsDisposed &&
                Engine.MainLoop is null && !RenderingServer.IsAvailable && !DisplayServer.IsAvailable, "Cleanup preserves borrowed assets.");
        }
        Console.WriteLine("Character movement passed: real GPU/compatibility images, native grow/shrink/minimum/restore, movement bounds, Escape/native close and cleanup.");
    }

    private static void KeyEvent(SDL.Scancode scancode, bool pressed)
    {
        var windows = SDL.GetWindows(out var count);
        Check(count == 1, "One native window for input delivery.");
        var key = new SDL.Event
        {
            Key = new SDL.KeyboardEvent
            {
                Type = pressed ? SDL.EventType.KeyDown : SDL.EventType.KeyUp,
                WindowID = SDL.GetWindowID(windows![0]),
                Scancode = scancode,
                Key = SDL.GetKeyFromScancode(scancode, SDL.Keymod.None, true),
                Down = pressed
            }
        };
        Check(SDL.PushEvent(ref key), "Native key event accepted.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
