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
            window.Ready += _ => RenderingServer.FramePostDraw += () =>
            {
                switch (++frames)
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
                        if (backend == "gpu") KeyEvent(SDL.Scancode.Escape, true);
                        else
                        {
                            var windows = SDL.GetWindows(out var count);
                            Check(count == 1, "One native window.");
                            var close = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(windows![0]) } };
                            Check(SDL.PushEvent(ref close), "Native close event accepted.");
                        }
                        break;
                    default: Check(frames < 12, "Close completes within the frame budget."); break;
                }
            };
            Check(Engine.Run(window) == 0 && frames >= 7, "Scene exits successfully.");
            Check(window.IsDisposed && player.IsDisposed && !texture.IsDisposed && !font.IsDisposed &&
                Engine.MainLoop is null && !RenderingServer.IsAvailable && !DisplayServer.IsAvailable, "Cleanup preserves borrowed assets.");
        }
        Console.WriteLine("Character movement passed: real GPU/compatibility images, four movement directions, key release, bounds, Escape/native close and cleanup.");
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
