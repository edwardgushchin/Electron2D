using Electron2D;
using SDL = SDL3.SDL;

internal static partial class ViewportMotionTests
{
    private sealed class Receiver : Control
    {
        internal InputEvent? SceneEvent, GUIEvent;
        internal Sample SceneSample, GUISample;
        internal PenSample GUIPenSample;
        internal int SceneCount, GUICount;
        internal bool Handles;
        internal Vector2? Marker;

        protected override void OnInput(InputEvent input)
        {
            if (input is not (InputEventMouseMotion or InputEventScreenDrag)) return;
            SceneEvent = input;
            SceneSample = new(input);
            SceneCount++;
        }

        protected override void OnGUIInput(InputEvent input)
        {
            if (input is not (InputEventMouseMotion or InputEventScreenDrag)) return;
            GUIEvent = input;
            GUISample = new(input);
            GUIPenSample = PenSample.From(input);
            GUICount++;
            Marker = GUISample.Position;
            QueueRedraw();
            if (Handles)
            {
                AcceptEvent();
                Check(GetViewport()!.IsInputHandled() && Tree!.Root is Viewport root && root.IsInputHandled(), "Child handling reaches the root.");
            }
        }

        protected override void OnDraw()
        {
            if (Marker is { } point) DrawRect(new(point, Vector2.One), Colors.Lime);
        }
    }

    private readonly record struct Sample(Vector2 Position, Vector2 Global, Vector2 Relative, Vector2 Velocity,
        Vector2 ScreenRelative, Vector2 ScreenVelocity)
    {
        internal Sample(InputEvent input) : this(default, default, default, default, default, default)
        {
            if (input is InputEventMouseMotion mouse)
                (Position, Global, Relative, Velocity, ScreenRelative, ScreenVelocity) =
                    (mouse.Position, mouse.GlobalPosition, mouse.Relative, mouse.Velocity, mouse.ScreenRelative, mouse.ScreenVelocity);
            if (input is InputEventScreenDrag drag)
                (Position, Relative, Velocity, ScreenRelative, ScreenVelocity) =
                    (drag.Position, drag.Relative, drag.Velocity, drag.ScreenRelative, drag.ScreenVelocity);
        }
    }

    private sealed class Fixture(Viewport root)
    {
        internal readonly Viewport Root = root;
        internal readonly SubViewport Middle = new() { Name = "middle" }, Leaf = new() { Name = "leaf" };
        internal readonly Receiver Target = new()
        {
            Name = "target",
            Position = new(2, 1),
            Scale = new(2, 1),
            Size = new(3, 3),
            MouseFilter = MouseFilter.Pass
        };
        internal readonly Receiver Sibling = new() { Name = "sibling_probe" };
        internal readonly SubViewport Other = new() { Name = "other", TransparentBG = true };

        internal void Populate()
        {
            var outer = new SubViewportContainer
            {
                Name = "outer",
                Position = new(12, 10),
                Scale = new(2, 1),
                Size = new(56, 72),
                Stretch = true,
                StretchShrink = 2,
                TextureFilter = TextureFilter.Nearest
            };
            var inner = new SubViewportContainer
            {
                Name = "inner",
                Position = new(4, 6),
                Scale = new(1, 2),
                Size = new(24, 24),
                Stretch = true,
                StretchShrink = 2,
                TextureFilter = TextureFilter.Nearest,
                MouseFilter = MouseFilter.Pass
            };
            var layer = new CanvasLayer { Name = "layer", Offset = new(1, 2) };
            layer.AddChild(Target);
            Leaf.AddChild(layer);
            inner.AddChild(Leaf);
            Middle.AddChild(inner);
            outer.AddChild(Middle);
            Other.AddChild(Sibling);
            outer.AddChild(Other);
            Root.AddChild(outer);
        }
    }

    // Forward projection of a leaf GUI point (x,y): canvas (3+2x,3+y), middle
    // (4+2*(3+2x),6+4*(3+y)), client (104+32x,92+16y). At (2,2), the
    // leaf scene point is (7,5) and the leaf canvas point is (6,3).
    private static readonly Vector2 Start = new(136, 108), End = new(168, 124), Delta = new(32, 16);

    private static void CheckEnd(Receiver target, bool mouse, Vector2 screenRelative, Vector2 screenVelocity)
    {
        var viewport = new Vector2(7, 5);
        Equal(target.SceneSample, new(viewport, mouse ? viewport : default, new(2, 1), new(4, 2), screenRelative, screenVelocity), "leaf scene");
        Equal(target.GUISample, new(new(2, 2), mouse ? new(6, 3) : default, Vector2.One, new(2, 2), screenRelative, screenVelocity), "leaf GUI");
        Check(target.SceneEvent is { IsDisposed: true } && target.GUIEvent is { IsDisposed: true } &&
            !ReferenceEquals(target.SceneEvent, target.GUIEvent), "Scene and GUI copies expire after dispatch.");
    }

    internal static void RunManaged()
    {
        using var detached = new SubViewport();
        using var motion = new InputEventMouseMotion
        {
            Position = End,
            GlobalPosition = new(-1, -2),
            Relative = Delta,
            Velocity = 2 * Delta,
            ScreenRelative = new(float.NaN, 3),
            ScreenVelocity = new(4, float.PositiveInfinity)
        };
        Reject<InvalidOperationException>(() => detached.PushInput(motion));
        var fixture = new Fixture(new SubViewport { Size = new(256, 192), Size2DOverride = new(128, 96), Size2DOverrideStretch = true });
        fixture.Populate();
        using var tree = new SceneTree(fixture.Root);
        tree.FlushDeferred();
        var target = fixture.Target;
        fixture.Root.PushInput(motion);
        CheckEnd(target, true, motion.ScreenRelative, motion.ScreenVelocity);
        Equal(fixture.Sibling.SceneSample, new(new(18, 26), new(18, 26), new(4, 4), new(8, 8),
            motion.ScreenRelative, motion.ScreenVelocity), "sibling scene");
        Check(fixture.Sibling.SceneCount == 1 && fixture.Sibling.GUICount == 0, "Sibling receives its own scene coordinates.");
        Check(!motion.IsDisposed && motion.Position == End && motion.GlobalPosition == new Vector2(-1, -2) &&
            motion.Relative == Delta && motion.Velocity == 2 * Delta, "Root PushInput retains the original payload.");

        fixture.Leaf.GlobalCanvasTransform = new Transform(new(2, 0), new(0, 2), new(1, -1));
        using var leafClient = new InputEventMouseMotion
        {
            Position = new(15, 9),
            Relative = new(4, 2),
            Velocity = new(8, 4),
            ScreenRelative = Delta,
            ScreenVelocity = 2 * Delta
        };
        fixture.Leaf.PushInput(leafClient);
        CheckEnd(target, true, Delta, 2 * Delta);
        fixture.Leaf.GlobalCanvasTransform = Transform.Identity;

        using var local = new InputEventMouseMotion
        {
            Position = new(7, 5),
            GlobalPosition = new(-5, -6),
            Relative = new(2, 1),
            Velocity = new(4, 2)
        };
        fixture.Leaf.PushInput(local, true);
        Check(ReferenceEquals(target.SceneEvent, local) && !local.IsDisposed &&
            target.SceneSample.Global == new Vector2(-5, -6) && target.GUIEvent!.IsDisposed, "Local PushInput borrows the scene event.");
        Equal(target.GUISample, new(new(2, 2), new(6, 3), Vector2.One, new(2, 2), default, default), "direct leaf GUI");

        using var drag = new InputEventScreenDrag
        {
            Index = 7,
            Position = End,
            Relative = Delta,
            Velocity = 2 * Delta,
            ScreenRelative = new(80, 90),
            ScreenVelocity = new(65, 75)
        };
        fixture.Root.PushInput(drag);
        CheckEnd(target, false, drag.ScreenRelative, drag.ScreenVelocity);
        Check(!drag.IsDisposed && drag.Position == End && drag.Relative == Delta && drag.Velocity == 2 * Delta, "Drag source stays intact.");

        using var press = new InputEventScreenTouch { Index = 7, Position = Start, Pressed = true };
        fixture.Root.PushInput(press);
        drag.Position = new(200, 140);
        target.Handles = true;
        var before = fixture.Sibling.SceneCount;
        fixture.Root.PushInput(drag);
        Check(target.GUISample.Position.IsEqualApprox(new(3, 3)) && fixture.Sibling.SceneCount == before,
            "Captured drag reaches the leaf outside its bounds and handling stops sibling delivery.");
        press.Pressed = false;
        press.Position = drag.Position;
        fixture.Root.PushInput(press);
        before = target.GUICount;
        fixture.Root.PushInput(drag);
        Check(target.GUICount == before, "Touch release retires capture.");

        Reject<ArgumentNullException>(() => fixture.Root.PushInput(null!));
        using var disposed = new InputEventScreenDrag();
        disposed.Dispose();
        Reject<ObjectDisposedException>(() => fixture.Root.PushInput(disposed));
        Task.Run(() => Reject<InvalidOperationException>(() => fixture.Root.PushInput(motion))).GetAwaiter().GetResult();
        motion.Relative = new(float.NaN, 2);
        before = target.SceneCount;
        Reject<ArgumentOutOfRangeException>(() => fixture.Root.PushInput(motion));
        Check(!motion.IsDisposed && target.SceneCount == before, "Invalid client movement rejects before scene delivery.");
        motion.Relative = Delta;
        fixture.Leaf.GlobalCanvasTransform = new Transform(Vector2.Zero, new(0, 1), Vector2.Zero);
        Reject<AggregateException>(() => fixture.Root.PushInput(motion));
        fixture.Leaf.GlobalCanvasTransform = Transform.Identity;
        Action<InputEvent> fail = input =>
        {
            target.GUIEvent = input;
            target.GUISample = new(input);
            throw new ApplicationException("leaf GUI");
        };
        target.GUIInput += fail;
        Reject<AggregateException>(() => fixture.Root.PushInput(motion));
        CheckEnd(target, true, motion.ScreenRelative, motion.ScreenVelocity);
        target.GUIInput -= fail;
        fixture.Root.PushInput(motion);
        CheckEnd(target, true, motion.ScreenRelative, motion.ScreenVelocity);
        Console.WriteLine("Viewport motion PushInput passed: mouse/touch, nested and direct coordinates, capture, siblings, ownership and failure recovery.");
    }

    internal static void RunNative(string backend)
    {
        var window = new Window
        {
            Size = new(256, 192),
            ContentScaleSize = new(128, 96),
            ContentScaleMode = ContentScaleMode.CanvasItems
        };
        var fixture = new Fixture(window);
        fixture.Populate();
        var driver = new NativeDriver(fixture, backend) { Name = "native_driver" };
        fixture.Root.AddChild(driver);
        var textures = new[] { fixture.Root.GetTexture(), fixture.Middle.GetTexture(), fixture.Leaf.GetTexture(), fixture.Other.GetTexture() };
        var priorBackend = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var priorClear = ProjectSettings.Get(ProjectSettings.DefaultClearColor);
        var priorEmulation = Input.EmulateMouseFromTouch;
        var priorAccumulation = Input.UseAccumulatedInput;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            ProjectSettings.Set(ProjectSettings.DefaultClearColor, Colors.Black);
            Input.EmulateMouseFromTouch = false;
            Input.UseAccumulatedInput = false;
            Check(Engine.Run(window) == 0 && driver.Frames == 3, "Native engine loop.");
            Check(fixture.Root.IsDisposed && fixture.Middle.IsDisposed && fixture.Leaf.IsDisposed && fixture.Other.IsDisposed &&
                textures.All(texture => texture.GetImage() is null) && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable,
                "Scene, display and viewport targets retire.");
            Console.WriteLine("Native motion and pixel checks passed for both input families. Scene, window and four targets retired.");
        }
        finally
        {
            Input.UseAccumulatedInput = priorAccumulation;
            Input.EmulateMouseFromTouch = priorEmulation;
            ProjectSettings.Set(ProjectSettings.RenderingMethod, priorBackend);
            ProjectSettings.Set(ProjectSettings.DefaultClearColor, priorClear);
            foreach (var texture in textures) texture.Dispose();
            if (!fixture.Root.IsDisposed) fixture.Root.Dispose();
        }
    }

    private sealed class NativeDriver(Fixture fixture, string backend) : Node
    {
        internal int Frames;
        private uint _window;
        private float _density;
        private ulong _time;

        protected override void OnReady()
        {
            var windows = SDL.GetWindows(out var count);
            Check(count == 1, "Expected one native window.");
            _window = SDL.GetWindowID(windows![0]);
            _density = SDL.GetWindowPixelDensity(windows[0]);
            Check(DisplayServer.WindowGetSize() == new Vector2i(256, 192), "Expected a 256×192 client.");
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested renderer must actually be active.");
            Console.WriteLine($"Renderer {backend}/{RenderingServer.GetCurrentRenderingDriverName()}, adapter {RenderingServer.GetVideoAdapterName()}, SDL {SDL.GetCurrentVideoDriver()}, density {_density}.");
            _time = SDL.GetTicksNS() + 10_000_000_000;
            RenderingServer.FramePostDraw += Frame;
        }

        private void Frame()
        {
            if (++Frames == 1)
            {
                Mouse(Start, Vector2.Zero, _time);
                Mouse(End, Delta, _time + 500_000_000);
                return;
            }

            var mouse = Frames == 2;
            CheckEnd(fixture.Target, mouse, Delta, 2 * Delta);
            using var root = fixture.Root.GetTexture().GetImage()!;
            using var leaf = fixture.Leaf.GetTexture().GetImage()!;
            var projected = root.GetPixel(176, 128);
            var previous = root.GetPixel(148, 112);
            var leafProjected = leaf.GetPixel(7, 5);
            var leafPrevious = leaf.GetPixel(5, 4);
            Check(projected.G > 0.9f && previous.G < 0.1f && leafProjected.G > 0.9f && leafPrevious.G < 0.1f,
                $"Marker pixels (client new/old, leaf new/old): {projected}, {previous}, {leafProjected}, {leafPrevious}.");
            Console.WriteLine($"{(mouse ? "Mouse" : "Touch drag")}: viewport (7,5), GUI (2,2), pixel (176,128); old marker clear.");
            if (mouse)
            {
                fixture.Target.Marker = null;
                fixture.Target.QueueRedraw();
                Touch(SDL.EventType.FingerDown, Start, default, _time);
                Touch(SDL.EventType.FingerMotion, End, Delta, _time + 500_000_000);
                Touch(SDL.EventType.FingerUp, End, default, _time + 600_000_000);
            }
            else
            {
                RenderingServer.FramePostDraw -= Frame;
                Tree!.Quit();
            }
        }

        private void Mouse(Vector2 position, Vector2 relative, ulong time)
        {
            var input = new SDL.Event
            {
                Motion = new SDL.MouseMotionEvent
                {
                    Type = SDL.EventType.MouseMotion,
                    WindowID = _window,
                    Timestamp = time,
                    X = position.X / _density,
                    Y = position.Y / _density,
                    XRel = relative.X / _density,
                    YRel = relative.Y / _density
                }
            };
            Check(SDL.PushEvent(ref input), "Queue native mouse motion.");
        }

        private void Touch(SDL.EventType type, Vector2 position, Vector2 relative, ulong time)
        {
            var input = new SDL.Event
            {
                TFinger = new SDL.TouchFingerEvent
                {
                    Type = type,
                    WindowID = _window,
                    Timestamp = time,
                    TouchID = 117,
                    FingerID = 12,
                    X = position.X / 256,
                    Y = position.Y / 192,
                    DX = relative.X / 256,
                    DY = relative.Y / 192,
                    Pressure = 0.5f
                }
            };
            Check(SDL.PushEvent(ref input), "Queue native touch input.");
        }
    }

    private static void Equal(Sample actual, Sample expected, string stage)
    {
        Check(actual.Position.IsEqualApprox(expected.Position) && actual.Global.IsEqualApprox(expected.Global) &&
            actual.Relative.IsEqualApprox(expected.Relative) && actual.Velocity.IsEqualApprox(expected.Velocity) &&
            SameScreenVector(actual.ScreenRelative, expected.ScreenRelative) && SameScreenVector(actual.ScreenVelocity, expected.ScreenVelocity),
            $"{stage}: expected {expected}, received {actual}.");
    }

    private static bool SameScreenVector(Vector2 actual, Vector2 expected) =>
        actual.X.Equals(expected.X) && actual.Y.Equals(expected.Y);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
