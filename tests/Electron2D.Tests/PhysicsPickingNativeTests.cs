using Electron2D;
using SDL = SDL3.SDL;

internal static class PhysicsPickingNativeTests
{
    internal static void Run()
    {
        var rendering = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var maximumSteps = Engine.MaxPhysicsStepsPerFrame;
        try
        {
            // Keep the injected hover alive until the next native pump; later catch-up ticks sample the actual desktop pointer.
            Engine.MaxPhysicsStepsPerFrame = 1;
            BackendChecks.RunPicking();
            foreach (var renderer in new[] { "gpu", "compatibility" })
                foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU, PhysicsServer.Backend.Extension })
                {
                    ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
                    using var world = backend == PhysicsServer.Backend.Extension ? new World("Picking") : new World(backend); using var shape = new CircleShape { Radius = 8 };
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
                    var probe = new NativeProbe(window, body, $"{renderer}-{backend}"); window.AddChild(probe);
                    Check(Engine.Run(window) == 0 && probe.Finished && probe.Drawn > 0 && body.Draws > 0, "Native pointer scene rendered and finished");
                    Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native picking teardown releases services");
                    Console.WriteLine($"Native physics picking passed: renderer={renderer}, physics={backend}; SDL click localization, capture suppression, pointer exit and rendered lifecycle.");
                }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, rendering); Engine.MaxPhysicsStepsPerFrame = maximumSteps; }
    }
    private sealed class DrawnBody : StaticBody
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawCircle(Vector2.Zero, 8, Colors.Pink); }
    }
    private sealed class NativeProbe(Window window, DrawnBody body, string captureName) : Node
    {
        internal int Drawn;
        internal bool Finished;
        private uint _windowID;
        private float _inputScale;
        private Vector2 _expectedPosition, _injectedPosition;
        private int _stage, _frames, _clicks, _exits, _previousExits;
        private bool _expectExit, _observedExit;
        protected override void OnReady()
        {
            ProcessEnabled = InputEnabled = true;
            var windows = SDL.GetWindows(out var count); Check(count == 1, "One native window"); _windowID = SDL.GetWindowID(windows![0]);
            RenderingServer.FramePostDraw += () =>
            {
                if (++Drawn != 1) return;
                using var image = window.GetTexture().GetImage()!;
                var pixel = image.GetPixel(55, 50);
                Check(MathF.Abs(pixel.R - Colors.Pink.R) < .02f && MathF.Abs(pixel.G - Colors.Pink.G) < .02f && MathF.Abs(pixel.B - Colors.Pink.B) < .02f,
                    "Native pointer target is visibly drawn at the transformed click coordinate");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_PICKING_CAPTURE") is { } folder)
                { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, $"native-{captureName}.png")); }
            };
            body.InputEvent += (view, input, index) =>
            {
                if (input is not InputEventMouseButton { Pressed: true } button) return;
                Check(ReferenceEquals(view, window) && index == 0 && button.Position.IsEqualApprox(_expectedPosition), $"Native final transform removed before physics canvas query: position={button.Position}, inputScale={_inputScale}, final={window.GetFinalTransform()}, stage={_stage}, viewport={ReferenceEquals(view, window)}, shape={index}");
                _clicks++;
                // Queue exit while this hit is still hovered, before a later frame samples the desktop pointer.
                if (_stage == 2 && _clicks >= 2)
                {
                    _previousExits = _exits; _expectExit = true; PushWindow(SDL.EventType.WindowMouseEnter); PushWindow(SDL.EventType.WindowMouseLeave); _stage = 3;
                }
            };
            body.MouseExited += () => _exits++;
            window.MouseExited += () => { if (_expectExit) { Check(_exits > _previousExits, "Native exit clears physics hover before Window signal"); _observedExit = true; } };
            PushWindow(SDL.EventType.WindowMouseEnter); Click();
        }
        protected override void OnInput(InputEvent input)
        {
            if (Input.MouseMode == MouseMode.Captured || input is not InputEventMouseButton { Pressed: true } button) return;
            // SDL logical coordinates acquire the current pixel density when dispatched, which can change after queueing.
            var scale = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland" ? SDL.GetWindowPixelDensity(SDL.GetWindowFromID(_windowID)) : 1;
            _expectedPosition = window.GetFinalTransform().AffineInverse() * (_injectedPosition * scale);
            Check(button.Position.IsEqualApprox(_expectedPosition), $"Native click localization at dispatch: {button.Position} vs {_expectedPosition}, inputScale={_inputScale}, density={SDL.GetWindowPixelDensity(SDL.GetWindowFromID(_windowID))}, final={window.GetFinalTransform()}");
        }
        protected override void OnProcess(double delta)
        {
            Check(++_frames < 600, $"Native picking scenario exceeded its frame budget: stage={_stage}, clicks={_clicks}, inputScale={_inputScale}");
            if (_stage == 0 && _clicks == 1)
            {
                Input.MouseMode = MouseMode.Captured; Click(); _stage = 1;
            }
            else if (_stage == 1)
            {
                Check(_clicks == 1, "Captured pointer does not generate physics input");
                Input.MouseMode = MouseMode.Visible; Click(); _stage = 2;
            }
            else if (_stage == 3 && _observedExit) { Finished = true; Tree!.Quit(); }
        }
        private void Click()
        {
            _inputScale = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland" ? SDL.GetWindowPixelDensity(SDL.GetWindowFromID(_windowID)) : 1;
            _injectedPosition = new(55 / _inputScale, 50 / _inputScale);
            var motion = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = _windowID, X = _injectedPosition.X, Y = _injectedPosition.Y } };
            Check(SDL.PushEvent(ref motion), "Queue native motion");
            var down = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = SDL.EventType.MouseButtonDown, WindowID = _windowID, Button = (byte)MouseButton.Left, Down = true, X = _injectedPosition.X, Y = _injectedPosition.Y } };
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
