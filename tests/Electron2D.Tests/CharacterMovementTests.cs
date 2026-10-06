using System.Collections.Concurrent;
using Electron2D;
using Electron2D.Examples;
using SDL = SDL3.SDL;
using IOPath = System.IO.Path;

internal static class CharacterMovementTests
{
    internal static void Run()
    {
        foreach (var invalid in new[] { new Vector2i(0, 600), new(800, 0), new(-1, 600) })
        {
            try { CharacterMovementScene.GetSurfaceLayout(invalid); throw new Exception("An empty surface was accepted."); }
            catch (ArgumentOutOfRangeException) { }
        }
        foreach (var size in new[] { new Vector2i(800, 600), new(1080, 2340), new(1920, 1080), new(3840, 2160), new(390, 844) })
        {
            var (scale, canvas) = CharacterMovementScene.GetSurfaceLayout(size);
            Check(Math.Abs(Math.Min(canvas.X, canvas.Y) - 600) < .001f, "The short canvas side retains the reference size.");
            Check((canvas * scale).IsEqualApprox(new(size.X, size.Y)), "The canvas fills every surface without bars or cropping.");
            Check(Math.Abs(canvas.Aspect() - (float)size.X / size.Y) < .001f, "The field preserves the surface aspect ratio.");
        }
        using var texture = ResourceLoader.Load<ImageTexture>(IOPath.Combine(AppContext.BaseDirectory, "Assets", "mark-dark.svg"));
        using var font = new FontFile();
        font.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
        Engine.MaxFPS = 60;
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        foreach (var (backend, asynchronous) in new[] { ("gpu", false), ("compatibility", false), ("compatibility", true) })
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            var window = CharacterMovementScene.CreateWindow(texture, font);
            var player = (Sprite)window.GetChild(3);
            var scale = player.Scale;
            var previous = player.Position;
            var frames = 0;
            var step = 0;
            window.Ready += _ => RenderingServer.FramePostDraw += () =>
            {
                Check(++frames < 120, "Input and close complete within the frame budget.");
                Check(window.Size == new Vector2i(800, 600) && window.Unresizable &&
                    window.MinSize == window.Size && window.MaxSize == window.Size, "The desktop window is fixed at 800 by 600.");
                Check(player.Scale == scale, "Input never resizes the character.");
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
                        Touch(SDL.EventType.FingerDown, player.Position + new Vector2(10, 0));
                        Touch(SDL.EventType.FingerMotion, new(200, 180));
                        break;
                    case 8:
                        Check(player.Position.IsEqualApprox(new Vector2(190, 180)), "Touch drag preserves the grab offset.");
                        Touch(SDL.EventType.FingerMotion, new(-100, -100)); break;
                    case 9:
                        Check(player.Position == new Vector2(80, 144), "Dragging retains the whole character inside the grid.");
                        Touch(SDL.EventType.FingerUp, new(-100, -100));
                        using (var press = new InputEventJoypadButton { ButtonIndex = JoyButton.DpadRight, Pressed = true })
                            Check(press.IsAction("ui_right"), "The built-in directional action accepts the D-pad.");
                        Input.ActionPress("ui_right");
                        previous = player.Position; break;
                    case 10:
                        Check(player.Position.X > previous.X, "Remote/controller directional action moves the character.");
                        Input.ActionRelease("ui_right");
                        previous = player.Position; break;
                    case 11:
                        Check(player.Position == previous, "Releasing a remote button stops movement.");
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
            var code = asynchronous ? RunOnContext(() => Engine.RunAsync(window)) : Engine.Run(window);
            Check(code == 0 && step >= 11, "Scene exits successfully after input checks.");
            Check(window.IsDisposed && player.IsDisposed && !texture.IsDisposed && !font.IsDisposed &&
                Engine.MainLoop is null && !RenderingServer.IsAvailable && !DisplayServer.IsAvailable, "Cleanup preserves borrowed assets.");
        }
        CheckAsyncErrors();
        Console.WriteLine("CharacterMovement passed: fixed desktop pixels, arrows, native touch, D-pad actions, synchronous/asynchronous exit and cleanup.");
    }

    private static void CheckAsyncErrors()
    {
        var pending = new Window();
        using var blocked = new Window();
        using var manual = new PendingLoop();
        Engine.Service.ReserveWindowRun();
        try { Engine.Run(blocked); throw new Exception("Pending startup accepted another window."); }
        catch (InvalidOperationException) { }
        try { Engine.Start(manual); throw new Exception("Pending startup accepted a manual loop."); }
        catch (InvalidOperationException) { }
        Check(!blocked.IsDisposed && Engine.MainLoop is null, "Pending startup rejection retains caller ownership.");
        Engine.Service.CancelReservedWindowCore(pending);
        Check(pending.IsDisposed && Engine.MainLoop is null, "Cancelling pending startup disposes its root and releases the engine.");
        using var callerOwned = new Window();
        var rejected = false;
        try { Engine.RunAsync(callerOwned); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected && !callerOwned.IsDisposed && Engine.MainLoop is null, "Context rejection retains caller ownership.");
        var failing = new Window { Size = new(100, 100) };
        var expected = new InvalidOperationException("async ready failure");
        failing.Ready += _ => throw expected;
        try { RunOnContext(() => Engine.RunAsync(failing)); throw new InvalidOperationException("A ready failure was swallowed."); }
        catch (Exception error) when (ReferenceEquals(error, expected)) { }
        catch (AggregateException error) when (error.Flatten().InnerExceptions.Any(e => ReferenceEquals(e, expected))) { }
        Check(failing.IsDisposed && Engine.MainLoop is null && !DisplayServer.IsAvailable, "Async startup failure cleans up.");
    }

    private static int RunOnContext(Func<Task<int>> start)
    {
        var previous = SynchronizationContext.Current;
        using var context = new MainThreadContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var task = start();
            while (!task.IsCompleted) context.Pump();
            return task.GetAwaiter().GetResult();
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private sealed class PendingLoop : MainLoop { }

    private sealed class MainThreadContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
        public override void Post(SendOrPostCallback callback, object? state) => _queue.Add((callback, state));
        internal void Pump()
        {
            if (!_queue.TryTake(out var work, TimeSpan.FromSeconds(10)))
                throw new TimeoutException("No main-thread frame continuation.");
            work.Callback(work.State);
        }
        public void Dispose() => _queue.Dispose();
    }

    private static void Touch(SDL.EventType type, Vector2 position)
    {
        var windows = SDL.GetWindows(out var count);
        Check(count == 1, "One native window for touch delivery.");
        var touch = new SDL.Event
        {
            TFinger = new SDL.TouchFingerEvent
            {
                Type = type,
                WindowID = SDL.GetWindowID(windows![0]),
                TouchID = 1,
                FingerID = 1,
                X = position.X / 800f,
                Y = position.Y / 600f
            }
        };
        Check(SDL.PushEvent(ref touch), "Native touch event accepted.");
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
