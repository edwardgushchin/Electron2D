using Electron2D;
using SDL = SDL3.SDL;

internal static class PhysicsPickingNativeTests
{
    internal static void Run()
    {
        var rendering = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        try
        {
            foreach (var renderer in new[] { "gpu", "compatibility" })
                foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU })
                {
                    ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
                    using var world = new World(backend); using var shape = new CircleShape { Radius = 8 };
                    var window = new Window
                    {
                        Title = "Physics pointer conformance",
                        Size = new(160, 120),
                        World = world,
                        PhysicsObjectPicking = true,
                        CanvasTransform = new(0, new Vector2(2, 2), 0, new(10, 6)),
                        GlobalCanvasTransform = new(0, new Vector2(5, 4))
                    };
                    var body = new DrawnBody { Name = "Body", Position = new(20, 20), InputPickable = true };
                    body.AddChild(new CollisionShape { Shape = shape }); window.AddChild(body);
                    var probe = new NativeProbe(window, body); window.AddChild(probe);
                    Check(Engine.Run(window) == 0 && probe.Finished && probe.Drawn > 0 && body.Draws > 0, "Native pointer scene rendered and finished");
                    Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native picking teardown releases services");
                    Console.WriteLine($"Native physics picking passed: renderer={renderer}, physics={backend}; SDL click localization, capture suppression, pointer exit and rendered lifecycle.");
                }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, rendering); }
    }
    private sealed class DrawnBody : StaticBody
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawCircle(Vector2.Zero, 8, Colors.Pink); }
    }
    private sealed class NativeProbe(Window window, DrawnBody body) : Node
    {
        internal int Drawn;
        internal bool Finished;
        private uint _windowID;
        private int _stage, _frames, _clicks, _exits, _previousExits;
        private bool _expectExit, _observedExit;
        protected override void OnReady()
        {
            ProcessEnabled = true;
            var windows = SDL.GetWindows(out var count); Check(count == 1, "One native window"); _windowID = SDL.GetWindowID(windows![0]);
            RenderingServer.FramePostDraw += () => Drawn++;
            body.InputEvent += (view, input, index) =>
            {
                if (input is not InputEventMouseButton { Pressed: true } button) return;
                Check(ReferenceEquals(view, window) && index == 0 && button.Position.IsEqualApprox(new(50, 46)), "Native final transform removed before physics canvas query");
                _clicks++;
            };
            body.MouseExited += () => _exits++;
            window.MouseExited += () => { if (_expectExit) { Check(_exits > _previousExits, "Native exit clears physics hover before Window signal"); _observedExit = true; } };
            PushWindow(SDL.EventType.WindowMouseEnter); Click();
        }
        protected override void OnProcess(double delta)
        {
            Check(++_frames < 600, "Native picking scenario exceeded its frame budget");
            if (_stage == 0 && _clicks == 1)
            {
                Input.MouseMode = MouseMode.Captured; Click(); _stage = 1;
            }
            else if (_stage == 1)
            {
                Check(_clicks == 1, "Captured pointer does not generate physics input");
                Input.MouseMode = MouseMode.Visible; Click(); _stage = 2;
            }
            else if (_stage == 2 && _clicks >= 2)
            {
                _previousExits = _exits; _expectExit = true; PushWindow(SDL.EventType.WindowMouseEnter); PushWindow(SDL.EventType.WindowMouseLeave); _stage = 3;
            }
            else if (_stage == 3 && _observedExit) { Finished = true; Tree!.Quit(); }
        }
        private void Click()
        {
            var motion = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = _windowID, X = 55, Y = 50 } };
            Check(SDL.PushEvent(ref motion), "Queue native motion");
            var down = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = SDL.EventType.MouseButtonDown, WindowID = _windowID, Button = (byte)MouseButton.Left, Down = true, X = 55, Y = 50 } };
            Check(SDL.PushEvent(ref down), "Queue native button");
            down.Button.Type = SDL.EventType.MouseButtonUp; down.Button.Down = false; Check(SDL.PushEvent(ref down), "Queue native release");
        }
        private void PushWindow(SDL.EventType type)
        {
            var input = new SDL.Event { Window = new SDL.WindowEvent { Type = type, WindowID = _windowID } }; Check(SDL.PushEvent(ref input), "Queue native window event");
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
