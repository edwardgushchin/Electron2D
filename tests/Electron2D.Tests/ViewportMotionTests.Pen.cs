using Electron2D;
using SDL = SDL3.SDL;

internal static partial class ViewportMotionTests
{
    internal static void RunPen()
    {
        using var display = DisplayServer.Open("Pen input checks", new Vector2i(256, 192), hidden: true);
        DisplayServer.ProcessEvents();
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "A single SDL window is required.");
        var window = windows![0];
        var scale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(window) : 1f;
        var queue = new PenQueue(SDL.GetWindowID(window), scale);
        var fixture = new Fixture(new SubViewport
        {
            Size = new(256, 192),
            Size2DOverride = new(128, 96),
            Size2DOverrideStretch = true,
        });
        fixture.Populate();
        var probe = new PenProbe();
        fixture.Root.AddChild(probe);
        using var tree = new SceneTree(fixture.Root);
        var prior = (Input.UseAccumulatedInput, Input.EmulateMouseFromTouch, Input.EmulateTouchFromMouse);
        Engine.Start(tree);
        try
        {
            tree.FlushDeferred();
            foreach (var emulate in new[] { false, true })
            {
                Input.UseAccumulatedInput = emulate;
                Input.EmulateMouseFromTouch = emulate;
                Input.EmulateTouchFromMouse = emulate;
                CheckPenSequence(queue, probe, fixture.Target, emulate);
            }
            CheckPenButtons(queue, probe);
            CheckRegularPointers(queue, probe);
            CheckUnfocusedChildRetirement(display, queue, probe);
            var driver = SDL.GetCurrentVideoDriver();
            var version = SDL.GetVersion();
            CheckDisplayReopen(display, queue, probe);
            Console.WriteLine($"SDL pen queue passed on {driver} (SDL {version}).");
        }
        finally
        {
            (Input.UseAccumulatedInput, Input.EmulateMouseFromTouch, Input.EmulateTouchFromMouse) = prior;
            Input.ReleasePressedEvents();
            Engine.Stop();
        }
    }

    private static void CheckPenSequence(PenQueue queue, PenProbe probe, Receiver target, bool emulate)
    {
        const uint a = 901;
        const uint b = 902;
        queue.Motion(a, Start, false);
        queue.Axis(a, SDL.PenAxis.Pressure, 0.25f);
        queue.Axis(a, SDL.PenAxis.XTilt, 30f);
        queue.Axis(a, SDL.PenAxis.YTilt, -30f);
        queue.Axis(a, SDL.PenAxis.Pressure, float.NaN);
        queue.Axis(b, SDL.PenAxis.Pressure, 0.75f, eraser: true);
        queue.Axis(b, SDL.PenAxis.XTilt, -30f, eraser: true);
        queue.Axis(b, SDL.PenAxis.YTilt, 30f, eraser: true);
        queue.Motion(b, Start, true);
        queue.Duplicates();
        DisplayServer.ProcessEvents();

        Check(probe.Events.Count == 2, "Each hovering pen delivers exactly one sample.");
        CheckPenSample(probe.Events[0], a, 0.25f, new(0.5f, -0.5f), false);
        CheckPenSample(probe.Events[1], b, 0.75f, new(-0.5f, 0.5f), true);
        probe.Events.Clear();

        queue.Touch(a, Start, true);
        queue.Touch(b, Start, true, eraser: true);
        queue.Duplicates();
        queue.AdvanceTime();
        queue.Motion(a, End, false, down: true);
        queue.Duplicates();
        queue.Motion(b, End, true, down: true);
        queue.Axis(b, SDL.PenAxis.Pressure, 0.875f, eraser: true);
        DisplayServer.ProcessEvents();

        var primary = probe.Events.Where(input => input.Device >= 0).ToArray();
        Check(primary.Length == 4 &&
              primary[0] is { Device: 901, Pressed: true } && primary[1] is { Device: 902, Pressed: true } &&
              primary[0].Index != primary[1].Index &&
              primary[2].Index == primary[0].Index && primary[3].Index == primary[1].Index,
            "Each pen gets one press and one drag with its own contact index.");
        CheckPenSample(primary[2], a, 0.25f, new(0.5f, -0.5f), false);
        CheckPenSample(primary[3], b, 0.875f, new(-0.5f, 0.5f), true);
        Check(target.SceneSample.Position.IsEqualApprox(new(7, 5)) &&
              target.SceneSample.Relative.IsEqualApprox(new(2, 1)) &&
              target.GUISample.Position.IsEqualApprox(new(2, 2)) &&
              target.GUISample.Relative.IsEqualApprox(Vector2.One) &&
              target.GUISample.ScreenRelative == Delta &&
              target.GUIPenSample is { Device: 902, Pressure: 0.875f, Eraser: true } &&
              target.GUIPenSample.Tilt.IsEqualApprox(new(-0.5f, 0.5f)),
            $"Nested viewport preserves pen data and local/screen coordinates: {target.SceneSample}, {target.GUISample}, {target.GUIPenSample}.");
        var copies = probe.Events.Where(input => input.Device == InputEvent.DeviceIdEmulation).ToArray();
        Check(emulate ? copies is [{ Pressed: true }, { Pressure: 0.25f, Eraser: false }] : copies.Length == 0,
            "The configured engine emulation produces one mouse press and motion for the first contact.");
        probe.Events.Clear();

        queue.AdvanceTime();
        queue.Axis(b, SDL.PenAxis.Pressure, 0.5f, eraser: true);
        queue.Duplicates();
        if (emulate)
        {
            queue.Reset(SDL.EventType.PenProximityOut, a, global: true);
        }
        else
        {
            queue.Touch(a, End, false);
        }
        queue.Touch(a, End, false);
        queue.Touch(b, End, false, eraser: true);
        DisplayServer.ProcessEvents();
        primary = probe.Events.Where(input => input.Device >= 0).ToArray();
        Check(primary is [{ Device: 902, Pressure: 0.5f }, { Device: 901, Pressed: false }, { Device: 902, Pressed: false }] &&
              primary[0].Relative == Vector2.Zero && primary[1].Canceled == emulate && !primary[2].Canceled &&
              probe.Events.Count == (emulate ? 4 : 3) && !Input.IsMouseButtonPressed(MouseButton.Left),
            "Stationary pressure, normal release, proximity cancellation and emulated release occur once.");
        probe.Events.Clear();

        CheckPenReset(queue, probe, SDL.EventType.PenProximityOut);
        CheckPenReset(queue, probe, SDL.EventType.WindowFocusLost);
        CheckPenReset(queue, probe, SDL.EventType.WindowCloseRequested);
    }

    private static void CheckPenReset(PenQueue queue, PenProbe probe, SDL.EventType reset)
    {
        const uint id = 902;
        queue.Axis(id, SDL.PenAxis.Pressure, 0.9f, eraser: true);
        queue.Axis(id, SDL.PenAxis.XTilt, 40f, eraser: true);
        queue.Touch(id, Start, true, eraser: true);
        queue.Reset(reset, id);
        queue.Touch(id, End, false, eraser: true);
        queue.Motion(id, End, false);
        DisplayServer.ProcessEvents();

        var events = probe.Events.Where(input => input.Device == id).ToArray();
        Check(events is [{ Pressed: true }, { Pressed: false, Canceled: true }, { Pressure: 0f, Eraser: false }] &&
              events[2].Tilt == Vector2.Zero && events[2].Relative == Vector2.Zero &&
              !Input.IsMouseButtonPressed(MouseButton.Left), $"{reset} cancels the contact and clears pen state.");
        probe.Events.Clear();
    }

    private static void CheckRegularPointers(PenQueue queue, PenProbe probe)
    {
        Input.EmulateTouchFromMouse = false;
        Input.EmulateMouseFromTouch = false;
        queue.MouseMotion(0);
        queue.MouseMotion(0);
        queue.MouseButton(0, true);
        queue.MouseButton(0, false);
        queue.Finger(35, SDL.EventType.FingerDown);
        queue.Finger(35, SDL.EventType.FingerMotion);
        queue.Finger(35, SDL.EventType.FingerUp);
        DisplayServer.ProcessEvents();
        var events = probe.Events;
        Check(events.Count == 6, "A pair of ordinary accumulated motions produces one event.");
        Check(events[0] is { Index: null, Pressed: null, Pressure: 0f, Eraser: false, Relative.X: 4f } &&
              events[1] is { Index: null, Pressed: true } && events[2] is { Index: null, Pressed: false },
            "Ordinary mouse motion and buttons are unaffected.");
        Check(events[3] is { Index: not null, Pressed: true } &&
              events[4] is { Index: not null, Pressed: null, Pressure: 0.375f, Eraser: false } &&
              events[5] is { Index: not null, Pressed: false } &&
              events[3].Index == events[4].Index && events[4].Index == events[5].Index,
            "Ordinary touch keeps one index for the complete contact.");
        probe.Events.Clear();
    }

    private static void CheckPenButtons(PenQueue queue, PenProbe probe)
    {
        const uint a = 901;
        const uint b = 902;
        var copy = unchecked((uint)SDL.PenMouseID);

        queue.BarrelButton(a, 1, true);
        queue.MouseButton(copy, true, button: 3);
        queue.BarrelButton(b, 2, true);
        queue.MouseButton(copy, true, button: 2);
        queue.Motion(a, Start, false);
        queue.MouseMotion(copy);
        queue.Motion(b, Start, false);
        queue.MouseMotion(0);
        DisplayServer.ProcessEvents();

        var events = probe.Events;
        Check(events.Count == 5, "Two native side presses and three motions pass without SDL copies.");
        Check(events[0] is { Device: 901, Pressed: true, Button: MouseButton.Right, Mask: MouseButtonMask.Right } &&
              events[1] is { Device: 902, Pressed: true, Button: MouseButton.Middle, Mask: MouseButtonMask.Middle } &&
              events[2] is { Device: 901, Pressed: null, Mask: MouseButtonMask.Right } &&
              events[3] is { Device: 902, Pressed: null, Mask: MouseButtonMask.Middle } &&
              events[4] is { Pressed: null, Mask: MouseButtonMask.None },
            "Hover carries the side buttons held by that pen. Ordinary mouse motion keeps its own mask.");
        Check(Input.IsMouseButtonPressed(MouseButton.Right) && Input.IsMouseButtonPressed(MouseButton.Middle),
            "Both pens' side buttons enter the public mouse polling state.");
        events.Clear();

        queue.BarrelButton(a, 1, false);
        queue.MouseButton(copy, false, button: 3);
        queue.Motion(a, End, false);
        queue.Motion(b, End, false);
        DisplayServer.ProcessEvents();
        Check(events.Count == 3 &&
              events[0] is { Device: 901, Pressed: false, Button: MouseButton.Right, Mask: MouseButtonMask.None } &&
              events[1] is { Device: 901, Pressed: null, Mask: MouseButtonMask.None } &&
              events[2] is { Device: 902, Pressed: null, Mask: MouseButtonMask.Middle } &&
              !Input.IsMouseButtonPressed(MouseButton.Right) && Input.IsMouseButtonPressed(MouseButton.Middle),
            "Releasing the first pen's side button keeps the other pen's hold.");
        events.Clear();

        queue.Reset(SDL.EventType.PenProximityOut, b);
        queue.BarrelButton(b, 2, false);
        queue.MouseButton(copy, false, button: 2);
        queue.Motion(b, Start, false);
        DisplayServer.ProcessEvents();
        Check(events.Count == 2 &&
              events[0] is { Device: 902, Pressed: false, Canceled: true, Button: MouseButton.Middle, Mask: MouseButtonMask.None } &&
              events[1] is { Device: 902, Pressed: null, Mask: MouseButtonMask.None } &&
              !Input.IsMouseButtonPressed(MouseButton.Middle),
            "Proximity exit cancels the remaining side button once and leaves the next hover clear.");
        events.Clear();
    }

    private static void CheckCrossWindowPenButtons(PenQueue main, PenQueue child, int childID)
    {
        const int a = 904;
        const int b = 905;
        var copy = unchecked((uint)SDL.PenMouseID);
        var childPoint = new Vector2(25, 30);
        var mainEvents = DisplayServer.GetWindowEvents();
        var childEvents = DisplayServer.GetWindowEvents(childID);
        var events = new List<(long Window, PenSample Sample, Vector2 Position)>();
        var focusChanges = 0;

        void Observe(InputEvent input)
        {
            if (input is InputEventMouse mouse &&
                (input is InputEventMouseButton || input.Device is a or b))
            {
                events.Add((mouse.WindowID, PenSample.From(mouse), mouse.Position));
            }
        }

        void FocusChanged(bool focused) => focusChanges++;

        DisplayServer.ProcessEvents();
        mainEvents.Input += Observe;
        childEvents.Input += Observe;
        mainEvents.FocusChanged += FocusChanged;
        childEvents.FocusChanged += FocusChanged;
        try
        {
            main.BarrelButton(a, 1, true);
            child.BarrelButton(a, 1, false, childPoint);
            child.MouseButton(copy, false, button: 3);
            DisplayServer.ProcessEvents();
            Check(events.Count == 2 && events[0].Sample is { Device: a, Pressed: true } &&
                  events[1] is { Window: var window, Sample: { Device: a, Pressed: false, Canceled: false, Mask: MouseButtonMask.None } } &&
                  window == childID && events[1].Position.IsEqualApprox(childPoint) &&
                  !Input.IsMouseButtonPressed(MouseButton.Right) && focusChanges == 0,
                "A cross-window release reaches the SDL destination once and clears the hold without a focus change.");
            events.Clear();

            main.BarrelButton(a, 1, true);
            child.BarrelButton(a, 2, true, childPoint);
            main.BarrelButton(b, 1, true);
            child.BarrelButton(a, 1, true, childPoint);
            child.Motion(a, childPoint, false);
            DisplayServer.ProcessEvents();

            var both = MouseButtonMask.Right | MouseButtonMask.Middle;
            Check(events.Count == 4 && events[1].Sample is { Device: a, Pressed: true } &&
                  events[1].Sample.Mask == both &&
                  events[3].Sample is { Device: a, Pressed: null } && events[3].Sample.Mask == both &&
                  Input.MouseButtonMask == both,
                "One pen combines holds from two windows, and a second down for the same hold is ignored.");
            events.Clear();

            child.BarrelButton(a, 1, false, childPoint);
            child.MouseButton(copy, false, button: 3);
            main.Motion(a, End, false);
            DisplayServer.ProcessEvents();

            Check(events.Count == 2 &&
                  events[0].Sample is { Device: a, Pressed: false, Canceled: false, Mask: MouseButtonMask.Middle } &&
                  events[1].Sample is { Device: a, Pressed: null, Mask: MouseButtonMask.Middle } &&
                  Input.MouseButtonMask == both,
                "Releasing one pen across windows preserves its other button and the other pen's right hold.");
            events.Clear();

            main.BarrelButton(a, 2, false);
            child.BarrelButton(b, 1, false, childPoint);
            child.BarrelButton(b, 1, false, childPoint);
            child.Motion(b, childPoint, false);
            DisplayServer.ProcessEvents();

            Check(events.Count == 3 &&
                  events[0].Sample is { Device: a, Pressed: false, Button: MouseButton.Middle, Mask: MouseButtonMask.None } &&
                  events[1].Sample is { Device: b, Pressed: false, Button: MouseButton.Right, Mask: MouseButtonMask.None } &&
                  events[2].Sample is { Device: b, Pressed: null, Mask: MouseButtonMask.None } &&
                  Input.MouseButtonMask == MouseButtonMask.None && focusChanges == 0,
                "Both release directions work without focus changes or duplicate up events.");
        }
        finally
        {
            childEvents.FocusChanged -= FocusChanged;
            mainEvents.FocusChanged -= FocusChanged;
            childEvents.Input -= Observe;
            mainEvents.Input -= Observe;
        }
    }

    private static void CheckUnfocusedChildRetirement(DisplayServer display, PenQueue queue, PenProbe probe)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsBrowser())
            return;

        const string action = "pen_retirement_left";
        const uint id = 903;
        using var child = display.CreateChildWindow("Unfocused pen contact", new(96, 80));
        var handle = child.AcquireRenderingWindow();
        PenQueue childQueue;
        try
        {
            var window = handle.DangerousGetHandle();
            var scale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(window) : 1f;
            childQueue = new PenQueue(SDL.GetWindowID(window), scale);
        }
        finally
        {
            child.ReleaseRenderingWindow();
        }

        CheckCrossWindowPenButtons(queue, childQueue, child.WindowID);

        using var binding = new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Device = InputEvent.DeviceIdEmulation,
        };
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, binding);
        Input.EmulateMouseFromTouch = true;
        try
        {
            DisplayServer.ProcessEvents();
            queue.MouseButton(0, true, button: 3);
            childQueue.Touch(id, new(30, 30), true);
            childQueue.BarrelButton(id, 2, true);
            DisplayServer.ProcessEvents();
            Check(!DisplayServer.WindowIsFocused(child.WindowID), "The native child is unfocused.");
            Check(Input.IsMouseButtonPressed(MouseButton.Left) && Input.IsActionPressed(action) &&
                  Input.IsMouseButtonPressed(MouseButton.Right) && Input.IsMouseButtonPressed(MouseButton.Middle),
                $"Expected child contact, action and side hold with ordinary right held. Mask: {Input.MouseButtonMask}, action: {Input.IsActionPressed(action)}.");

            child.Dispose();
            Check(!Input.IsMouseButtonPressed(MouseButton.Left) && !Input.IsActionPressed(action) &&
                  !Input.IsMouseButtonPressed(MouseButton.Middle) && Input.IsMouseButtonPressed(MouseButton.Right),
                "Programmatic child disposal releases its contact, action and side button only.");

            childQueue.Touch(id, new(30, 30), false);
            queue.Touch(id, Start, true);
            DisplayServer.ProcessEvents();
            Check(Input.IsMouseButtonPressed(MouseButton.Left) && Input.IsActionPressed(action),
                "The same pen can claim mouse emulation in the surviving window.");
            queue.Touch(id, Start, false);
            queue.MouseButton(0, false, button: 3);
            DisplayServer.ProcessEvents();
            Check(!Input.IsMouseButtonPressed(MouseButton.Left) && !Input.IsMouseButtonPressed(MouseButton.Right) &&
                  !Input.IsActionPressed(action), "Both remaining holds release normally.");
        }
        finally
        {
            InputMap.EraseAction(action);
            probe.Events.Clear();
        }
    }

    private static void CheckDisplayReopen(DisplayServer display, PenQueue queue, PenProbe probe)
    {
        const uint id = 901;
        Input.EmulateMouseFromTouch = true;
        queue.Axis(id, SDL.PenAxis.Pressure, 0.8f, eraser: true);
        queue.Touch(id, Start, true, eraser: true);
        DisplayServer.ProcessEvents();
        Check(Input.IsMouseButtonPressed(MouseButton.Left), "The active pen owns the emulated mouse hold.");

        display.Dispose();
        Check(!Input.IsMouseButtonPressed(MouseButton.Left), "Display disposal releases the emulated hold.");
        using var reopened = DisplayServer.Open("Reopened pen input checks", new Vector2i(256, 192), hidden: true);
        DisplayServer.ProcessEvents();
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "The replacement display owns one SDL window.");
        var window = windows![0];
        var scale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(window) : 1f;
        var reopenedQueue = new PenQueue(SDL.GetWindowID(window), scale);
        probe.Events.Clear();
        reopenedQueue.Touch(id, Start, false);
        reopenedQueue.Motion(id, End, false);
        DisplayServer.ProcessEvents();
        Check(probe.Events is [{ Device: 901, Pressure: 0f, Eraser: false, Index: null }] &&
              probe.Events[0].Tilt == Vector2.Zero && probe.Events[0].Relative == Vector2.Zero,
            "Reopening a display starts the same pen ID with fresh metadata and no contact.");
    }

    private static void CheckPenSample(PenSample sample, uint id, float pressure, Vector2 tilt, bool eraser)
    {
        Check(sample.Device == id && sample.Pressure == pressure && sample.Tilt.IsEqualApprox(tilt) &&
              sample.Eraser == eraser, $"Unexpected metadata for pen {id}: {sample}.");
    }

    private readonly record struct PenSample(int Device, int? Index, bool? Pressed, bool Canceled,
        float Pressure, Vector2 Tilt, bool Eraser, Vector2 Relative,
        MouseButtonMask Mask = MouseButtonMask.None, MouseButton Button = MouseButton.None)
    {
        internal static PenSample From(InputEvent input) => input switch
        {
            InputEventMouseMotion motion => new(motion.Device, null, null, false, motion.Pressure,
                motion.Tilt, motion.PenInverted, motion.ScreenRelative, motion.ButtonMask),
            InputEventScreenDrag drag => new(drag.Device, drag.Index, null, false, drag.Pressure,
                drag.Tilt, drag.PenInverted, drag.ScreenRelative),
            InputEventScreenTouch touch => new(touch.Device, touch.Index, touch.Pressed, touch.Canceled, 0f, default, false, default),
            InputEventMouseButton button => new(button.Device, null, button.Pressed, button.Canceled, 0f, default, false, default,
                button.ButtonMask, button.ButtonIndex),
            _ => throw new ArgumentOutOfRangeException(nameof(input)),
        };
    }

    private sealed class PenProbe : Entity
    {
        internal List<PenSample> Events { get; } = [];

        internal PenProbe()
        {
            InputEnabled = true;
        }

        protected override void OnInput(InputEvent input)
        {
            if (input is InputEventMouseMotion or InputEventMouseButton or InputEventScreenTouch or InputEventScreenDrag)
                Events.Add(PenSample.From(input));
        }
    }

    private sealed class PenQueue(uint window, float scale)
    {
        private ulong _timestamp = SDL.GetTicksNS() + 10_000_000_000UL;

        internal void AdvanceTime() => _timestamp += 500_000_000UL;

        internal void Motion(uint id, Vector2 position, bool eraser, bool down = false)
        {
            Push(new SDL.Event
            {
                PMotion = new SDL.PenMotionEvent
                {
                    Type = SDL.EventType.PenMotion,
                    WindowID = window,
                    Which = id,
                    Timestamp = _timestamp,
                    X = position.X / scale,
                    Y = position.Y / scale,
                    PenState = (eraser ? SDL.PenInputFlags.EraserTip : 0) | (down ? SDL.PenInputFlags.Down : 0),
                },
            });
        }

        internal void Axis(uint id, SDL.PenAxis axis, float value, bool eraser = false)
        {
            Push(new SDL.Event
            {
                PAxis = new SDL.PenAxisEvent
                {
                    Type = SDL.EventType.PenAxis,
                    WindowID = window,
                    Which = id,
                    Timestamp = _timestamp,
                    Axis = axis,
                    Value = value,
                    PenState = eraser ? SDL.PenInputFlags.EraserTip : 0,
                },
            });
        }

        internal void Touch(uint id, Vector2 position, bool pressed, bool eraser = false)
        {
            Push(new SDL.Event
            {
                PTouch = new SDL.PenTouchEvent
                {
                    Type = pressed ? SDL.EventType.PenDown : SDL.EventType.PenUp,
                    WindowID = window,
                    Timestamp = _timestamp,
                    Which = id,
                    X = position.X / scale,
                    Y = position.Y / scale,
                    Down = pressed,
                    Eraser = eraser,
                    PenState = (eraser ? SDL.PenInputFlags.EraserTip : 0) | (pressed ? SDL.PenInputFlags.Down : 0),
                },
            });
        }

        internal void Reset(SDL.EventType type, uint id, bool global = false)
        {
            if (type == SDL.EventType.PenProximityOut)
            {
                Push(new SDL.Event
                {
                    PProximity = new SDL.PenProximityEvent { Type = type, WindowID = global ? 0u : window, Which = id },
                });
            }
            else
            {
                Push(new SDL.Event
                {
                    Window = new SDL.WindowEvent { Type = type, WindowID = window },
                });
            }
        }

        internal void Duplicates()
        {
            var mouse = unchecked((uint)SDL.PenMouseID);
            var touch = unchecked((ulong)SDL.PenTouchID);
            MouseMotion(mouse);
            MouseButton(mouse, true);
            MouseButton(mouse, false);
            Finger(touch, SDL.EventType.FingerDown);
            Finger(touch, SDL.EventType.FingerMotion);
            Finger(touch, SDL.EventType.FingerUp);
        }

        internal void MouseMotion(uint device)
        {
            Push(new SDL.Event
            {
                Motion = new SDL.MouseMotionEvent
                {
                    Type = SDL.EventType.MouseMotion,
                    WindowID = window,
                    Which = device,
                    Timestamp = _timestamp,
                    X = End.X / scale,
                    Y = End.Y / scale,
                    XRel = 2f / scale,
                },
            });
        }

        internal void BarrelButton(uint id, byte button, bool pressed, Vector2? position = null)
        {
            var point = position ?? Start;
            Push(new SDL.Event
            {
                PButton = new SDL.PenButtonEvent
                {
                    Type = pressed ? SDL.EventType.PenButtonDown : SDL.EventType.PenButtonUp,
                    WindowID = window,
                    Which = id,
                    Timestamp = _timestamp,
                    Button = button,
                    Down = pressed,
                    X = point.X / scale,
                    Y = point.Y / scale,
                },
            });
        }

        internal void MouseButton(uint device, bool pressed, byte button = 1)
        {
            Push(new SDL.Event
            {
                Button = new SDL.MouseButtonEvent
                {
                    Type = pressed ? SDL.EventType.MouseButtonDown : SDL.EventType.MouseButtonUp,
                    WindowID = window,
                    Which = device,
                    Button = button,
                    Down = pressed,
                    X = End.X / scale,
                    Y = End.Y / scale,
                },
            });
        }

        internal void Finger(ulong touch, SDL.EventType type)
        {
            Push(new SDL.Event
            {
                TFinger = new SDL.TouchFingerEvent
                {
                    Type = type,
                    WindowID = window,
                    TouchID = touch,
                    FingerID = 1,
                    X = 0.5f,
                    Y = 0.5f,
                    Pressure = 0.375f,
                },
            });
        }

        private static void Push(SDL.Event input) => Check(SDL.PushEvent(ref input), "SDL accepts the queued input.");
    }
}
