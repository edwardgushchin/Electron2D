# Input

Last updated: 2026-09-24

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/Input.cs`](../../src/Core/Input/Input.cs), [`src/Core/Input/Input.Pointer.cs`](../../src/Core/Input/Input.Pointer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed partial class Input : ElectronObject`

> Owns process-wide input state and translates typed events into named actions.

## Description

Owns process-wide input state and translates typed events into named actions.

`Input` is the non-disposable process-wide owner of raw keyboard/mouse/controller state, mapped action contributions, and independent process/physics transition windows. It never owns submitted events or native devices.
Controller button and axis queries retain signed raw IDs from submitted events and isolate values by device; a missing ID returns false or zero. Querying a negative physical device ID raises a typed argument error.

A platform host submits events through [`Input.ParseInputEvent(InputEvent)`](Input.md#m-electron2d-input-parseinputevent-electron2d-inputevent). State is committed before scene delivery,
so callbacks observe the new state. Optional touch-to-mouse and mouse-to-touch emulation sends a generated event
before its source event; generated mouse events also update raw and mapped state. Queries and synthetic action changes
are lock-serialized; event delivery is synchronous on the caller thread and an attached [`SceneTree`](SceneTree.md)
requires its owner thread.

Native pointer controls use the active `DisplayServer` for the current mouse mode and cursor shape. Input retains only the default cursor policy used when no Control overrides it. Native operations require a live display and its owner thread; the singleton itself remains available without a display for managed input state.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
Input input = Input.Instance;
if (input.IsActionPressed("jump"))
    Jump();
```

## Properties

| Member | Description |
| --- | --- |
| [`public static Input Instance { get; }`](#p-electron2d-input-instance) | Gets the process-wide input service. |
| [`public bool UseAccumulatedInput { get; set; }`](#p-electron2d-input-useaccumulatedinput) | Controls native pointer-motion accumulation; defaults to `true`. |
| [`public bool EmulateMouseFromTouch { get; set; }`](#p-electron2d-input-emulatemousefromtouch) | Makes the first active touch contact generate left-button mouse input; defaults to `true`. |
| [`public bool EmulateTouchFromMouse { get; set; }`](#p-electron2d-input-emulatetouchfrommouse) | Makes left-button mouse input generate touch input; defaults to `false`. |
| [`public MouseButtonMask MouseButtonMask { get; }`](#p-electron2d-input-mousebuttonmask) | Gets the non-wheel mouse buttons currently held. |
| [`public Vector2 LastMouseVelocity { get; }`](#p-electron2d-input-lastmousevelocity) | Gets the most recently submitted local mouse velocity. |
| [`public Vector2 LastMouseScreenVelocity { get; }`](#p-electron2d-input-lastmousescreenvelocity) | Gets the most recently submitted screen-space mouse velocity. |
| [`public MouseModeEnum MouseMode { get; set; }`](#p-electron2d-input-mousemode) | Gets or applies native pointer visibility, capture and confinement. |

## Methods

| Member | Description |
| --- | --- |
| [`public bool IsKeyPressed(Key keycode)`](#m-electron2d-input-iskeypressed-electron2d-key) | Gets whether a logical key is currently held. |
| [`public bool IsPhysicalKeyPressed(Key keycode)`](#m-electron2d-input-isphysicalkeypressed-electron2d-key) | Gets whether a physical key position is currently held. |
| [`public bool IsKeyLabelPressed(Key keycode)`](#m-electron2d-input-iskeylabelpressed-electron2d-key) | Gets whether a localized key label is currently held. |
| [`public bool IsMouseButtonPressed(MouseButton button)`](#m-electron2d-input-ismousebuttonpressed-electron2d-mousebutton) | Gets whether a non-wheel mouse button is currently held. |
| [`public bool IsJoyButtonPressed(JoyButton button, int device = 0)`](#m-electron2d-input-isjoybuttonpressed-electron2d-joybutton-system-int32) | Gets whether a controller button is currently held. |
| [`public float GetJoyAxis(JoyAxis axis, int device = 0)`](#m-electron2d-input-getjoyaxis-electron2d-joyaxis-system-int32) | Gets the latest controller-axis value. |
| [`public bool IsAnythingPressed()`](#m-electron2d-input-isanythingpressed) | Gets whether any key, mouse button, controller button, or action is currently pressed. |
| [`public bool IsActionPressed(string action, bool exactMatch = false)`](#m-electron2d-input-isactionpressed-system-string-system-boolean) | Gets whether an action is currently pressed. |
| [`public bool IsActionJustPressed(string action, bool exactMatch = false)`](#m-electron2d-input-isactionjustpressed-system-string-system-boolean) | Gets whether an action transitioned from released to pressed since the current callback lane last completed. |
| [`public bool IsActionJustReleased(string action, bool exactMatch = false)`](#m-electron2d-input-isactionjustreleased-system-string-system-boolean) | Gets whether an action transitioned from pressed to released since the current callback lane last completed. |
| [`public bool IsActionJustPressedByEvent(string action, InputEvent event, bool exactMatch = false)`](#m-electron2d-input-isactionjustpressedbyevent-system-string-electron2d-inputevent-system-boolean) | Gets whether a specific event caused the action's current just-pressed transition. |
| [`public bool IsActionJustReleasedByEvent(string action, InputEvent event, bool exactMatch = false)`](#m-electron2d-input-isactionjustreleasedbyevent-system-string-electron2d-inputevent-system-boolean) | Gets whether a specific event caused the action's current just-released transition. |
| [`public float GetActionStrength(string action, bool exactMatch = false)`](#m-electron2d-input-getactionstrength-system-string-system-boolean) | Gets an action's deadzone-adjusted strength. |
| [`public float GetActionRawStrength(string action, bool exactMatch = false)`](#m-electron2d-input-getactionrawstrength-system-string-system-boolean) | Gets an action's strength before deadzone remapping. |
| [`public float GetAxis(string negativeAction, string positiveAction)`](#m-electron2d-input-getaxis-system-string-system-string) | Combines a negative and positive action into one signed axis. |
| [`public Vector2 GetVector(string negativeX, string positiveX, string negativeY, string positiveY, float deadzone = -1f)`](#m-electron2d-input-getvector-system-string-system-string-system-string-system-string-system-single) | Combines four actions into a circularly deadzoned two-dimensional input vector. |
| [`public void ActionPress(string action, float strength = 1f)`](#m-electron2d-input-actionpress-system-string-system-single) | Presses a registered action without producing an input event. |
| [`public void ActionRelease(string action)`](#m-electron2d-input-actionrelease-system-string) | Releases the synthetic source of a registered action without producing an input event. |
| [`public void FlushBufferedEvents()`](#m-electron2d-input-flushbufferedevents) | Delivers native pointer motion buffered by the display adapter. |
| [`public void ParseInputEvent(InputEvent event)`](#m-electron2d-input-parseinputevent-electron2d-inputevent) | Submits one typed input event, updates state, and synchronously routes it to the active main loop. |
| [`public void ReleasePressedEvents()`](#m-electron2d-input-releasepressedevents) | Releases every tracked key, mouse button, controller button, axis, and action source. |
| [`public CursorShape GetCurrentCursorShape()`](#m-electron2d-input-getcurrentcursorshape) | Gets the shape selected on the active display. |
| [`public void SetDefaultCursorShape(CursorShape shape = CursorShape.Arrow)`](#m-electron2d-input-setdefaultcursorshape) | Stores the viewport default and refreshes the current native cursor. |
| [`public void SetCustomMouseCursor(Resource? image, CursorShape shape = CursorShape.Arrow, Vector2 hotspot = default)`](#m-electron2d-input-setcustommousecursor) | Installs copied image pixels in a cursor slot or restores the system shape. |
| [`public void WarpMouse(Vector2 position)`](#m-electron2d-input-warpmouse) | Requests a native pointer warp on supporting backends. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-input-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override void ValidateDisposal()`](#m-electron2d-input-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Constants

| Member | Description |
| --- | --- |

## Nested enums

| Enum | Values |
| --- | --- |
| [MouseModeEnum](Input.MouseModeEnum.md) | `Visible = 0`, `Hidden = 1`, `Captured = 2`, `Confined = 3`, `ConfinedHidden = 4`, `Max = 5` (sentinel). The `Enum` suffix avoids collision with the `MouseMode` property. |
| [CursorShape](Input.CursorShape.md) | `Arrow = 0`, `IBeam = 1`, `PointingHand = 2`, `Cross = 3`, `Wait = 4`, `Busy = 5`, `Drag = 6`, `CanDrop = 7`, `Forbidden = 8`, `VSize = 9`, `HSize = 10`, `BDiagSize = 11`, `FDiagSize = 12`, `Move = 13`, `VSplit = 14`, `HSplit = 15`, `Help = 16`. |

## Property Descriptions

<a id="p-electron2d-input-mousemode"></a>
### `public MouseModeEnum MouseMode { get; set; }`

Reads or applies the active display's pointer mode. The five selectable modes control native cursor visibility, relative capture and window confinement. `Max` is invalid. Native failures preserve the previous mode on a best-effort basis. Reading or writing without a display throws `InvalidOperationException`; the display owner thread is required.

<a id="p-electron2d-input-instance"></a>
### `public static Input Instance { get; }`

Gets the process-wide input service.

**Value:** The same non-disposable instance for the lifetime of the process.

<a id="p-electron2d-input-useaccumulatedinput"></a>
### `public bool UseAccumulatedInput { get; set; }`

Controls whether the native display adapter combines consecutive pointer-motion events in its pending batch.

**Value:** `true` by default. A change applies to the next native event; without a native host this setting has no effect. Keyboard, button, and touch event order is preserved.

<a id="p-electron2d-input-emulatemousefromtouch"></a>
### `public bool EmulateMouseFromTouch { get; set; }`

Controls whether the first active touch contact generates left-button mouse press, motion, and release events.

**Value:** `true` by default. Generated mouse events use device ID `-1` and update mouse-button and action state. Other contacts remain touch-only. Turning this off during an emulated press suppresses further motion, but that contact still sends the release needed to clear state.

<a id="p-electron2d-input-emulatetouchfrommouse"></a>
### `public bool EmulateTouchFromMouse { get; set; }`

Controls whether left-button mouse input also generates touch press, drag, and release events at index zero.

**Value:** `false` by default. Generated touch events use device ID `-1` and are delivered to the active scene without changing raw input or mapped action state. One mouse device owns an active emulated contact; another device cannot move or end it. Turning this off during an emulated press suppresses further drags, but the release still ends the generated contact. Without an active scene there is no generated touch delivery.

<a id="p-electron2d-input-mousebuttonmask"></a>
### `public MouseButtonMask MouseButtonMask { get; }`

Gets the non-wheel mouse buttons currently held.

**Value:** A thread-safe snapshot of the current button mask.

<a id="p-electron2d-input-lastmousevelocity"></a>
### `public Vector2 LastMouseVelocity { get; }`

Gets the most recently submitted local mouse velocity.

**Value:** A thread-safe snapshot in content-scaled pixels per second.

<a id="p-electron2d-input-lastmousescreenvelocity"></a>
### `public Vector2 LastMouseScreenVelocity { get; }`

Gets the most recently submitted screen-space mouse velocity.

**Value:** A thread-safe snapshot in unscaled screen pixels per second.

## Method Descriptions

<a id="m-electron2d-input-iskeypressed-electron2d-key"></a>
### `public bool IsKeyPressed(Key keycode)`

Gets whether a logical key is currently held.

**Parameters**

- `keycode`: A non-modifier logical key code.

**Returns:** `true` when held.

<a id="m-electron2d-input-isphysicalkeypressed-electron2d-key"></a>
### `public bool IsPhysicalKeyPressed(Key keycode)`

Gets whether a physical key position is currently held.

**Parameters**

- `keycode`: A non-modifier physical key code.

**Returns:** `true` when held.

<a id="m-electron2d-input-iskeylabelpressed-electron2d-key"></a>
### `public bool IsKeyLabelPressed(Key keycode)`

Gets whether a localized key label is currently held.

**Parameters**

- `keycode`: A non-modifier key label.

**Returns:** `true` when held.

<a id="m-electron2d-input-ismousebuttonpressed-electron2d-mousebutton"></a>
### `public bool IsMouseButtonPressed(MouseButton button)`

Gets whether a non-wheel mouse button is currently held.

**Parameters**

- `button`: The button to query.

**Returns:** `true` when the button is held.

**Exceptions**

- `ArgumentOutOfRangeException`: `button` is not defined.

**Remarks:** Wheel directions are transient events and always return `false`.

<a id="m-electron2d-input-isjoybuttonpressed-electron2d-joybutton-system-int32"></a>
### `public bool IsJoyButtonPressed(JoyButton button, int device = 0)`

Gets whether a controller button is currently held.

**Parameters**

- `button`: Any signed standardized or raw button index.
- `device`: The non-negative controller identifier.

**Returns:** `true` when held.

**Exceptions**

- `ArgumentOutOfRangeException`: `device` is negative.

<a id="m-electron2d-input-getjoyaxis-electron2d-joyaxis-system-int32"></a>
### `public float GetJoyAxis(JoyAxis axis, int device = 0)`

Gets the latest controller-axis value.

**Parameters**

- `axis`: Any signed standardized or raw axis index.
- `device`: The non-negative controller identifier.

**Returns:** The last stored source value, or zero before the first event.

**Exceptions**

- `ArgumentOutOfRangeException`: `device` is negative.

<a id="m-electron2d-input-isanythingpressed"></a>
### `public bool IsAnythingPressed()`

Gets whether any key, mouse button, controller button, or action is currently pressed.

**Returns:** `true` when at least one tracked input is pressed.

<a id="m-electron2d-input-isactionpressed-system-string-system-boolean"></a>
### `public bool IsActionPressed(string action, bool exactMatch = false)`

Gets whether an action is currently pressed.

**Parameters**

- `action`: The registered, case-sensitive action name.
- `exactMatch`: Whether only contributions with exact modifiers or analog direction are considered.

**Returns:** `true` when at least one matching source is pressed.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-input-isactionjustpressed-system-string-system-boolean"></a>
### `public bool IsActionJustPressed(string action, bool exactMatch = false)`

Gets whether an action transitioned from released to pressed since the current callback lane last completed.

**Parameters**

- `action`: The registered, case-sensitive action name.
- `exactMatch`: Whether the transition must have been caused by an exact match.

**Returns:** `true` during the current process and physics transition windows.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-input-isactionjustreleased-system-string-system-boolean"></a>
### `public bool IsActionJustReleased(string action, bool exactMatch = false)`

Gets whether an action transitioned from pressed to released since the current callback lane last completed.

**Parameters**

- `action`: The registered, case-sensitive action name.
- `exactMatch`: Whether only exact-match state is considered.

**Returns:** `true` during the current process and physics transition windows.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-input-isactionjustpressedbyevent-system-string-electron2d-inputevent-system-boolean"></a>
### `public bool IsActionJustPressedByEvent(string action, InputEvent event, bool exactMatch = false)`

Gets whether a specific event caused the action's current just-pressed transition.

**Parameters**

- `action`: The registered, case-sensitive action name.
- `event`: The event reference previously submitted to [`Input.ParseInputEvent(InputEvent)`](Input.md#m-electron2d-input-parseinputevent-electron2d-inputevent).
- `exactMatch`: Whether the event must have matched exactly.

**Returns:** `true` when the current transition was caused by `event`.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` or `event` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: `event` is disposing or disposed.

<a id="m-electron2d-input-isactionjustreleasedbyevent-system-string-electron2d-inputevent-system-boolean"></a>
### `public bool IsActionJustReleasedByEvent(string action, InputEvent event, bool exactMatch = false)`

Gets whether a specific event caused the action's current just-released transition.

**Parameters**

- `action`: The registered, case-sensitive action name.
- `event`: The event reference previously submitted to [`Input.ParseInputEvent(InputEvent)`](Input.md#m-electron2d-input-parseinputevent-electron2d-inputevent).
- `exactMatch`: Whether the event must have matched exactly.

**Returns:** `true` when the current transition was caused by `event`.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` or `event` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: `event` is disposing or disposed.

<a id="m-electron2d-input-getactionstrength-system-string-system-boolean"></a>
### `public float GetActionStrength(string action, bool exactMatch = false)`

Gets an action's deadzone-adjusted strength.

**Parameters**

- `action`: The registered, case-sensitive action name.
- `exactMatch`: Whether only exact contributions are considered.

**Returns:** The greatest matching strength from zero through one.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-input-getactionrawstrength-system-string-system-boolean"></a>
### `public float GetActionRawStrength(string action, bool exactMatch = false)`

Gets an action's strength before deadzone remapping.

**Parameters**

- `action`: The registered, case-sensitive action name.
- `exactMatch`: Whether only exact contributions are considered.

**Returns:** The greatest matching raw strength from zero through one.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-input-getaxis-system-string-system-string"></a>
### `public float GetAxis(string negativeAction, string positiveAction)`

Combines a negative and positive action into one signed axis.

**Parameters**

- `negativeAction`: The action contributing toward minus one.
- `positiveAction`: The action contributing toward plus one.

**Returns:** Positive strength minus negative strength, from minus one through one.

**Exceptions**

- `ArgumentException`: An action name is empty or whitespace.
- `ArgumentNullException`: An action name is `null`.
- `Collections.Generic.KeyNotFoundException`: An action is not registered.

<a id="m-electron2d-input-getvector-system-string-system-string-system-string-system-string-system-single"></a>
### `public Vector2 GetVector(string negativeX, string positiveX, string negativeY, string positiveY, float deadzone = -1f)`

Combines four actions into a circularly deadzoned two-dimensional input vector.

**Parameters**

- `negativeX`: The action contributing toward negative X.
- `positiveX`: The action contributing toward positive X.
- `negativeY`: The action contributing toward negative Y.
- `positiveY`: The action contributing toward positive Y.
- `deadzone`: A finite override from zero through one, or minus one to average the four action deadzones.

**Returns:** A vector whose length does not exceed one.

**Exceptions**

- `ArgumentException`: An action name is empty or whitespace.
- `ArgumentNullException`: An action name is `null`.
- `ArgumentOutOfRangeException`: `deadzone` is not minus one or a finite value from zero through one.
- `Collections.Generic.KeyNotFoundException`: An action is not registered.

<a id="m-electron2d-input-actionpress-system-string-system-single"></a>
### `public void ActionPress(string action, float strength = 1f)`

Presses a registered action without producing an input event.

**Parameters**

- `action`: The registered, case-sensitive action name.
- `strength`: A finite strength clamped to zero through one.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `ArgumentOutOfRangeException`: `strength` is NaN or infinite.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

**Remarks:** A zero-strength source is still pressed but contributes zero analog strength. Call [`Input.ActionRelease(String)`](Input.md#m-electron2d-input-actionrelease-system-string) to remove it.

<a id="m-electron2d-input-actionrelease-system-string"></a>
### `public void ActionRelease(string action)`

Releases the synthetic source of a registered action without producing an input event.

**Parameters**

- `action`: The registered, case-sensitive action name.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-input-flushbufferedevents"></a>
### `public void FlushBufferedEvents()`

Delivers pointer motion currently accumulated by the native display adapter, in event order, on the caller thread.
This is a no-op if no native host is active or its pending batch contains no motion. A native host can reject calls
outside its owner thread or while pumping events.

<a id="m-electron2d-input-parseinputevent-electron2d-inputevent"></a>
### `public void ParseInputEvent(InputEvent event)`

Submits one typed input event, updates state, and synchronously routes it to the active main loop.

**Parameters**

- `event`: A live event. Ownership remains with the caller.

**Exceptions**

- `ArgumentNullException`: `event` is `null`.
- `InvalidOperationException`: Parsing is re-entered, an active loop is called off its owner thread or during another callback, or a direct action event cannot obtain a valid source index.
- `Collections.Generic.KeyNotFoundException`: An [`InputEventAction`](InputEventAction.md) names an unregistered action.
- `ObjectDisposedException`: `event` or a matched binding is disposing or disposed.
- `AggregateException`: One or more scene input callbacks throw after state is committed.

**Remarks:** Mapping and state changes are committed before callbacks. With pointer emulation enabled, a generated
event with device ID `-1` is sent before the source event. The first active touch contact generates left-button mouse
input; left-button mouse input can generate a scene-only touch at index zero. Generated events do not recursively
emulate. If a callback for the generated event throws, the source event is still delivered; failures from both are
combined in an `AggregateException`. Committed state is not rolled back. Re-entry is rejected. An active loop validates
owner-thread and lifecycle eligibility before any state changes. Events may be submitted without an active loop;
mouse emulation still updates state, while touch emulation requires an active scene.

<a id="m-electron2d-input-releasepressedevents"></a>
### `public void ReleasePressedEvents()`

Releases every tracked key, mouse button, controller button, axis, and action source.

**Remarks:** Actions that were pressed receive a just-released transition in both callback lanes. Emulated contact
tracking and per-device mouse button masks are cleared. This method does not emit events or route callbacks and does
not alter [`InputMap`](InputMap.md) or the emulation settings.

<a id="m-electron2d-input-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

Appends read-only current-state descriptors.

<a id="m-electron2d-input-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Exceptions**

- `InvalidOperationException`: Always thrown because the singleton has process lifetime.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

The process-wide input service cannot be disposed.

<a id="m-electron2d-input-getcurrentcursorshape"></a>
### `public CursorShape GetCurrentCursorShape()`

Returns the current display cursor shape, including a change made directly through `DisplayServer`. Requires an active display on its owner thread.

<a id="m-electron2d-input-setdefaultcursorshape"></a>
### `public void SetDefaultCursorShape(CursorShape shape = CursorShape.Arrow)`

Stores one of the 17 native cursor shapes separately from the current display shape. It selects the native shape immediately, then reapplies an active root-viewport Control override if present. It does not synthesize mouse motion; the pinned event side effect and nested viewport routing remain gaps.

<a id="m-electron2d-input-setcustommousecursor"></a>
### `public void SetCustomMouseCursor(Resource? image, CursorShape shape = CursorShape.Arrow, Vector2 hotspot = default)`

Copies pixels from a readable `Image` or `Texture` into the selected cursor slot; `null` restores its system cursor. The source stays caller-owned and may be disposed after the call. Size is limited to 256 × 256 pixels, and the hotspot must fall inside the image. Invalid resources, shapes, image sizes and hotspots fail through `DisplayServer`. Animated-texture first-frame semantics and native behavior beyond the tested Linux backend are not fully audited.

<a id="m-electron2d-input-warpmouse"></a>
### `public void WarpMouse(Vector2 position)`

Requests movement to finite client-area coordinates, truncating fractions to native integer pixels. Out-of-range coordinates fail before reaching the display. Wayland rejects the warp with `NotSupportedException`; movement and clipping on supported backends have not been natively verified.

## Constant Descriptions

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Lifecycle, ordering, and errors

The singleton exists for the process lifetime. Parsing first validates any active MainLoop's owner thread and idle-running lifecycle, then fully resolves mappings before mutation, commits state, and dispatches. Wrong-thread, nested-frame/lifecycle, source-capacity, and parse re-entry failures occur before state mutation. Pointer emulation delivers a generated event before its source and keeps each contact's release paired even if an emulation setting changes mid-contact. Scene callback failure propagates without rolling committed state back; the source event is delivered after a generated-event callback failure. Invalid names/devices/enums/non-finite strengths/deadzones throw typed C# exceptions; unregistered actions throw `KeyNotFoundException`.

Each process/physics callback sees its own transition latch. That lane clears in MainLoop `finally`; a failed frame cannot leak a just transition into its next frame. Outside a callback, transition queries use the process lane.

## Threading and invariants

State/configuration queries are lock-serialized. Event parsing is serialized and callback delivery runs on the caller thread; an active MainLoop therefore requires its owner thread and rejects calls made inside another loop callback. Returned value snapshots need no lifetime management. The warmed non-emulated mapped parse/traversal path is allocation-free; generating a pointer event allocates a short-lived resource.

## Dependencies, verification, and limitations

Depends on InputMap, typed event classes, Engine/MainLoop, SceneTree, core math and, for native pointer controls, the active DisplayServer. Managed tests cover input state, emulation order, first-contact ownership, release pairing, failure/re-entry, non-emulated allocation and root viewport Control GUI delivery and hover. The optional SDL dummy-driver suite checks pointer modifier translation. Targeted Wayland native runs verify pointer modes, cursor shapes, a copied image, Control cursor precedence and cleanup. Controller discovery/effects, sensors, MIDI, shortcuts, action persistence, complete GUI routing and nested viewports have exact implementation triggers in [ADR 0038](../decisions/input.md#deferred-coverage-and-exact-implementation-triggers); physical pointer hardware and the full native-host matrix have not been exercised.

A SceneTree with a Viewport root localizes window coordinates after Input commits raw state. Positional callback events can therefore be distinct short-lived copies: polling and by-event transition identity still refer to the original parsed input. Canvas/viewport conversion never rewrites raw polling coordinates. See [Viewport.PushInput](Viewport.md#pushinput).
