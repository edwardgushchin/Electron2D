# InputEventJoypadButton

Last updated: 2026-09-21

**Inherits:** [InputEvent](InputEvent.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventJoypad.cs`](../../src/Core/Input/InputEventJoypad.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventJoypadButton : InputEvent`

> Represents a game-controller button press or release.

## Description

Represents a game-controller button press or release.

- Responsibility: one standardized or raw signed controller button press/release and optional pressure.
- Complete declared API: `ButtonIndex`, `Pressed`, `Pressure`; overrides `IsMatch`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. All three values are stored typed descriptors.
- Invariants/errors: the button index and pressure retain arbitrary caller values, including negative indexes and non-finite pressure; binding identity ignores pressure. `ButtonIndex` emits `Changed`, while `Pressed` and `Pressure` store without emitting it. Disposed access fails.
- Text: `AsText` reports `Joypad Button n`, adds one of 21 known controller descriptions for indexes 0–20, and adds `Pressure: value` only when pressure is nonzero. All other signed IDs use a safe numeric fallback. The source template, known descriptions and pressure label use this event's translation domain; malformed translated templates fall back to source wording.
- Threading/verification: caller-owned mutable state; per-device raw tracking, action matching, signed index extremes, non-finite pressure, copy/revert, all 21 defined text labels and change delivery are covered in managed tests. Virtual SDL gamepad/raw-joystick discovery, button delivery, rumble and LED callbacks pass on dummy and Linux Wayland; physical hardware remains unverified, and native event construction has a measured managed allocation gap.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var inputEventJoypadButton = new InputEventJoypadButton();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventJoypadButton()`](#m-electron2d-inputeventjoypadbutton-ctor) | Initializes a new InputEventJoypadButton instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public JoyButton ButtonIndex { get; set; }`](#p-electron2d-inputeventjoypadbutton-buttonindex) | Gets or sets the controller button. |
| [`public bool Pressed { get; set; }`](#p-electron2d-inputeventjoypadbutton-pressed) | Gets or sets whether the controller button is pressed. |
| [`public float Pressure { get; set; }`](#p-electron2d-inputeventjoypadbutton-pressure) | Gets or sets analog pressure reported for the button. |

## Methods

| Member | Description |
| --- | --- |
| [`public override bool IsMatch(InputEvent event, bool exactMatch = true)`](#m-electron2d-inputeventjoypadbutton-ismatch-electron2d-inputevent-system-boolean) | Tests whether this event has the same binding configuration as another event. |
| [`public override string AsText()`](#m-electron2d-inputeventjoypadbutton-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventjoypadbutton-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventjoypadbutton-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventjoypadbutton-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventjoypadbutton-ctor"></a>
### `public InputEventJoypadButton()`

Initializes a new InputEventJoypadButton instance.

## Property Descriptions

<a id="p-electron2d-inputeventjoypadbutton-buttonindex"></a>
### `public JoyButton ButtonIndex { get; set; }`

Gets or sets the controller button.

**Value:** A standardized or arbitrary signed raw button index.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventjoypadbutton-pressed"></a>
### `public bool Pressed { get; set; }`

Gets or sets whether the controller button is pressed.

**Value:** `false` for a release; `true` for a press.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="p-electron2d-inputeventjoypadbutton-pressure"></a>
### `public float Pressure { get; set; }`

Gets or sets analog pressure reported for the button.

**Value:** The source value without range or finiteness validation. Most hosts report zero and use [`InputEventJoypadButton.Pressed`](InputEventJoypadButton.md#p-electron2d-inputeventjoypadbutton-pressed).

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

## Method Descriptions

<a id="m-electron2d-inputeventjoypadbutton-ismatch-electron2d-inputevent-system-boolean"></a>
### `public override bool IsMatch(InputEvent event, bool exactMatch = true)`

Tests whether this event has the same binding configuration as another event.

**Parameters**

- `event`: The event to compare.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the binding configurations match.

**Exceptions**

- `ArgumentNullException`: `event` is `null`.
- `ObjectDisposedException`: Either event is disposing or disposed.

<a id="m-electron2d-inputeventjoypadbutton-astext"></a>
### `public override string AsText()`

Returns the localized button number, optional known description and nonzero pressure.

**Returns:** `Joypad Button n` with the applicable known label and pressure suffix.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventjoypadbutton-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventjoypadbutton-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventjoypadbutton-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEvent](InputEvent.md). Their lifecycle and error contracts remain applicable unless this page states an override.
