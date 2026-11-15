using Electron2D;

const string Left = "contract_left";
const string Right = "contract_right";
const string Up = "contract_up";
const string Down = "contract_down";
const string Button = "contract_button";
const string Either = "contract_either_direction";

foreach (var action in new[] { Left, Right, Up, Down, Button, Either })
    InputMap.AddAction(action, 0.5f);

using var left = new InputEventJoypadMotion { Device = -1, Axis = JoyAxis.LeftX, AxisValue = -1 };
using var right = new InputEventJoypadMotion { Device = -1, Axis = JoyAxis.LeftX, AxisValue = 1 };
using var key = new InputEventKey { Keycode = Key.F8, Device = -1 };
InputMap.ActionAddEvent(Left, left);
InputMap.ActionAddEvent(Right, right);
InputMap.ActionAddEvent(Either, left);
InputMap.ActionAddEvent(Either, right);
InputMap.ActionAddEvent(Button, key);

using var bitmap = new BitMap();
bitmap.Create(new Vector2i(4, 4));
bitmap.SetBitRect(new Rect2i(0, 0, 4, 4), true);
using var viewport = new SubViewport { Size = new Vector2i(64, 64) };
using var probe = new Probe();
using var touchButton = new TouchScreenButton { Action = Button, Bitmask = bitmap };
viewport.AddChild(probe);
viewport.AddChild(touchButton);
using var tree = new SceneTree(viewport);

Engine.Start(tree);
try
{
    Input.ReleasePressedEvents();
    using var sample = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 0.125f };
    Input.ParseInputEvent(sample);
    Check(Input.GetActionRawStrength(Right, exactMatch: true) == 0.125f &&
        Input.GetActionStrength(Right) == 0 && !Input.IsActionPressed(Right) && !Input.IsAnythingPressed(),
        "Raw-only motion contributes without marking the action pressed.");
    Check(Input.GetVector(Left, Right, Up, Down, 0) == new Vector2(0.125f, 0) &&
        Input.GetVector(Left, Right, Up, Down) == Vector2.Zero && Input.GetAxis(Left, Right) == 0,
        "Vector uses raw input and one circular deadzone. Axis uses effective strength.");
    sample.AxisValue = -0.75f;
    Input.ParseInputEvent(sample);
    Check(Input.GetAxis(Left, Right) == -0.5f && Input.IsAnythingPressed(), "Negative direction.");
    tree.ProcessFrame(0);
    sample.AxisValue = 0.75f;
    Input.ParseInputEvent(sample);
    Check(Input.GetAxis(Left, Right) == 0.5f && Input.IsActionPressed(Either, true) &&
        !Input.IsActionJustPressed(Either) && !Input.IsActionJustReleased(Either) &&
        !Input.IsActionJustPressed(Either, true) && !Input.IsActionJustReleased(Either, true),
        "One axis sample switches between opposite bindings without inventing an edge.");
    using var second = new InputEventJoypadMotion { Device = 2, Axis = JoyAxis.LeftX, AxisValue = -0.75f };
    Input.ParseInputEvent(second);
    Check(Input.GetAxis(Left, Right) == 0 && Input.IsActionPressed(Left) && Input.IsActionPressed(Right),
        "Opposite controller sources cancel the axis and retain both actions.");
    Input.ActionPress(Down);
    Check(Math.Abs(Input.GetVector(Up, Right, Up, Down, 0).Length() - 1) < 0.00001f,
        "Diagonal input is limited to unit length.");
    Input.ActionRelease(Down);
    Input.ReleasePressedEvents();
    Check(!Input.IsAnythingPressed() && Input.GetActionRawStrength(Right) == 0, "Release-all clears raw contributions.");
    tree.ProcessFrame(0);
    tree.PhysicsFrame(0);
    Console.WriteLine("strength, deadzones, conflicting directions and aggregate edges: passed");

    using var press = new InputEventKey { Keycode = Key.F8, Pressed = true };
    using var extraModifier = new InputEventKey { Keycode = Key.F8, ShiftPressed = true, Pressed = true };
    using var keyUp = new InputEventKey { Keycode = Key.F8 };
    using var equalCopy = (InputEvent)press.Duplicate();
    using var invalid = new InputEventAction { Action = Button, EventIndex = 32, Pressed = true };
    var called = 0;
    probe.DuringInput = value =>
    {
        Check(ReferenceEquals(value, extraModifier) && Input.IsKeyPressed(Key.F8) &&
            Input.IsActionJustPressedByEvent(Button, value) && !Input.IsActionPressed(Button, true),
            "Input state precedes callback, original identity is retained, and extra modifiers are permissive only.");
        Reject<InvalidOperationException>(() => Input.ParseInputEvent(keyUp));
        Reject<InvalidOperationException>(Input.FlushBufferedEvents);
        Task.Run(() =>
        {
            Reject<InvalidOperationException>(() => Input.ParseInputEvent(keyUp));
            Reject<InvalidOperationException>(Input.FlushBufferedEvents);
        }).GetAwaiter().GetResult();
        called++;
    };
    Check(Input.UseAccumulatedInput, "Native accumulation defaults to enabled.");
    Input.ParseInputEvent(extraModifier);
    Check(called == 1 && Input.IsActionPressed(Button), "Public parsing completes synchronously.");
    Reject<InvalidOperationException>(() => Input.ParseInputEvent(invalid));
    Check(called == 1 && Input.IsActionJustPressedByEvent(Button, extraModifier), "Invalid source leaves state and callbacks intact.");
    probe.DuringInput = null;
    Input.UseAccumulatedInput = false;
    Input.ParseInputEvent(press);
    Check(Input.IsActionPressed(Button, true) && Input.IsActionJustPressedByEvent(Button, press, true) &&
        !Input.IsActionJustPressedByEvent(Button, equalCopy, true) &&
        Input.IsActionJustPressedByEvent(Button, extraModifier) && !Input.IsActionJustPressedByEvent(Button, press),
        "Only the matching aggregate edge attributes the original event.");
    using var secondPress = new InputEventKey { Keycode = Key.F8, Device = 2, Pressed = true };
    using var secondRelease = new InputEventKey { Keycode = Key.F8, Device = 2 };
    Input.ParseInputEvent(secondPress);
    Input.ParseInputEvent(secondRelease);
    Check(Input.IsActionPressed(Button, true) && !Input.IsActionJustReleased(Button),
        "Releasing one of two equal physical inputs preserves the other.");

    tree.ProcessFrame(0);
    probe.DuringPhysics = () =>
    {
        Check(Input.IsActionJustPressedByEvent(Button, press, true) &&
            Input.IsActionJustPressedByEvent(Button, extraModifier), "Process completion leaves physics transitions intact.");
        Reject<InvalidOperationException>(Input.FlushBufferedEvents);
        throw new ProbeFailure();
    };
    Reject<AggregateException>(() => tree.PhysicsFrame(0));
    probe.DuringPhysics = () => Check(!Input.IsActionJustPressed(Button) && !Input.IsActionJustPressed(Button, true),
        "Failed physics frame still consumes its transition window.");
    tree.PhysicsFrame(0);
    probe.DuringPhysics = null;
    Input.ActionPress(Button, 0.6f);
    probe.DuringInput = _ => throw new ProbeFailure();
    Reject<AggregateException>(() => Input.ParseInputEvent(keyUp));
    Check(!Input.IsActionPressed(Button) && !Input.IsKeyPressed(Key.F8) &&
        Input.IsActionJustReleasedByEvent(Button, keyUp) && Input.IsActionJustReleasedByEvent(Button, keyUp, true),
        "Mapped release clears both the parsed hold and the manual override before a subscriber failure.");
    probe.DuringInput = null;
    Input.ParseInputEvent(press);
    Input.ActionPress(Button, 0.5f);
    Input.ActionRelease(Button);
    Check(!Input.IsActionPressed(Button) && Input.IsActionJustReleased(Button, true) &&
        !Input.IsActionJustReleasedByEvent(Button, keyUp) && Input.IsKeyPressed(Key.F8),
        "Public release clears non-virtual action state without changing raw key state or attributing the edge to an event.");
    Input.ParseInputEvent(keyUp);
    Input.FlushBufferedEvents();
    Console.WriteLine("callback ordering, by-event identity, lane lifetime and delivery rejection: passed");

    Input.EmulateMouseFromTouch = false;
    using var touch = new InputEventScreenTouch { Pressed = true, Position = new Vector2(1, 1), Index = 4 };
    Input.ActionPress(Button, 0.4f);
    Input.ParseInputEvent(press);
    Input.ParseInputEvent(touch);
    Check(touchButton.IsPressed() && Input.GetActionStrength(Button) == 1, "Virtual control contributes independently.");
    key.Changed += _ => throw new ProbeFailure();
    Reject<ProbeFailure>(() => key.Keycode = Key.F9);
    Reject<ArgumentOutOfRangeException>(() => Input.ActionPress(Button, float.NaN));
    Check(Input.GetActionStrength(Button) == 1, "Binding edit and rejected manual press preserve the virtual source.");
    touch.Pressed = false;
    Input.ParseInputEvent(touch);
    Check(!Input.IsActionPressed(Button), "Binding edit invalidates earlier physical and manual state.");
    touch.Pressed = true;
    Input.ParseInputEvent(touch);
    Input.ActionPress(Button, 0.4f);
    touch.Pressed = false;
    Input.ParseInputEvent(touch);
    Check(!touchButton.IsPressed() && Input.GetActionStrength(Button) == 0.4f && Input.IsActionPressed(Button, true),
        "Virtual release exposes the surviving manual source.");
    touch.Pressed = true;
    Input.ParseInputEvent(touch);
    Input.ActionRelease(Button);
    Check(Input.IsActionPressed(Button, true) && Input.GetActionStrength(Button) == 1,
        "Public release leaves the virtual control's contribution with its owner.");
    touch.Pressed = false;
    Input.ParseInputEvent(touch);
    Input.ParseInputEvent(keyUp);
    Check(!Input.IsActionPressed(Button) && !Input.IsAnythingPressed(), "The stale physical binding did not survive the edit.");
    Input.ActionPress(Button, -1);
    Check(Input.IsAnythingPressed() && Input.IsActionPressed(Button, true) && Input.GetActionStrength(Button) == 0,
        "A clamped zero-strength manual press remains pressed.");
    Input.ActionRelease(Button);
    Console.WriteLine("manual, physical and virtual source ownership and failed binding observation: passed");

    using var directPress = new InputEventAction { Action = Button, Strength = 0.8f, Pressed = true };
    using var directRelease = new InputEventAction { Action = Button };
    void MeasureStep()
    {
        Input.ParseInputEvent(directPress);
        Check(Input.IsActionPressed(Button, true) && Input.GetActionRawStrength(Button) == 0.8f &&
            Input.IsActionJustPressedByEvent(Button, directPress) && Input.IsAnythingPressed(), "Prepared direct press.");
        Input.ParseInputEvent(directRelease);
        Check(Input.IsActionJustReleasedByEvent(Button, directRelease, true), "Prepared direct release.");
        Input.ActionPress(Button, 0.5f);
        Check(Input.GetAxis(Left, Button) == 0.5f, "Prepared action axis.");
        _ = Input.GetVector(Left, Right, Up, Down);
        Input.ActionRelease(Button);
        Input.FlushBufferedEvents();
    }

    for (var i = 0; i < 20_000; i++) MeasureStep();
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < 4096; i++) MeasureStep();
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Console.WriteLine($"Prepared direct parsing, callbacks, action queries and idle flush: {allocated} bytes / 4096 iterations");
    Check(allocated == 0, "Warmed eligible paths must not allocate.");
}
finally
{
    Engine.Stop();
}

static void Check(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}

static void Reject<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

sealed class ProbeFailure : Exception;

sealed class Probe : Node
{
    public Action<InputEvent>? DuringInput { get; set; }
    public Action? DuringPhysics { get; set; }
    protected override void OnInput(InputEvent input) => DuringInput?.Invoke(input);
    protected override void OnPhysicsProcess(double delta) => DuringPhysics?.Invoke();
}
