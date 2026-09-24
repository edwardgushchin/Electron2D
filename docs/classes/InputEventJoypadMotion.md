# InputEventJoypadMotion

Last updated: 2026-09-21

**Inherits:** [InputEvent](InputEvent.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventJoypad.cs`](../../src/Core/Input/InputEventJoypad.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventJoypadMotion : InputEvent`

> Represents motion on one game-controller axis.

## Description

Represents motion on one game-controller axis.

- Responsibility: signed motion on one standardized/raw controller axis.
- Complete declared API: `Axis`, `AxisValue`; overrides `IsMatch`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. Both values are stored typed descriptors.
- Matching/state: axis identity plus direction for exact matching. Raw `IsPressed` uses a fixed absolute threshold of `0.5`; action matching uses that action's separate deadzone. Action raw strength is absolute magnitude in the binding direction, and adjusted strength remaps the remaining range to `[0,1]`.
- Invariants/errors: axis accepts the `Invalid` (`-1`) and `Max` (`10`) sentinels and rejects values outside them; `AxisValue` retains arbitrary floats, including NaN and infinity. Disposed access fails.
- Text/verification: `AsText` reports the axis index, one of ten known controller descriptions or a safe unknown fallback, and a signed value with two decimals or non-finite source text. The source template and description use this event's translation domain; malformed translated templates fall back to source wording. Managed checks cover labels, sentinel text, the fixed raw-press threshold, action deadzones, non-finite values, observer failure after commit and copies/revert. Native gamepad delivery remains on its own backend trigger.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var inputEventJoypadMotion = new InputEventJoypadMotion();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventJoypadMotion()`](#m-electron2d-inputeventjoypadmotion-ctor) | Initializes a new InputEventJoypadMotion instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public JoyAxis Axis { get; set; }`](#p-electron2d-inputeventjoypadmotion-axis) | Gets or sets the controller axis. |
| [`public float AxisValue { get; set; }`](#p-electron2d-inputeventjoypadmotion-axisvalue) | Gets or sets the current signed axis position. |

## Methods

| Member | Description |
| --- | --- |
| [`public override bool IsMatch(InputEvent event, bool exactMatch = true)`](#m-electron2d-inputeventjoypadmotion-ismatch-electron2d-inputevent-system-boolean) | Tests whether this event has the same binding configuration as another event. |
| [`public override string AsText()`](#m-electron2d-inputeventjoypadmotion-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventjoypadmotion-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventjoypadmotion-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventjoypadmotion-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventjoypadmotion-ctor"></a>
### `public InputEventJoypadMotion()`

Initializes a new InputEventJoypadMotion instance.

## Property Descriptions

<a id="p-electron2d-inputeventjoypadmotion-axis"></a>
### `public JoyAxis Axis { get; set; }`

Gets or sets the controller axis.

**Value:** An axis index from `JoyAxis.Invalid` (`-1`) through `JoyAxis.Max` (`10`).

**Exceptions**

- `ArgumentOutOfRangeException`: The numeric value is below `-1` or above `10`.
- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventjoypadmotion-axisvalue"></a>
### `public float AxisValue { get; set; }`

Gets or sets the current signed axis position.

**Value:** The source value without range or finiteness validation; zero is the resting position. Magnitude at least `0.5` marks the raw event pressed.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventjoypadmotion-ismatch-electron2d-inputevent-system-boolean"></a>
### `public override bool IsMatch(InputEvent event, bool exactMatch = true)`

Tests whether this event has the same binding configuration as another event.

**Parameters**

- `event`: The event to compare.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the binding configurations match.

**Exceptions**

- `ArgumentNullException`: `event` is `null`.
- `ObjectDisposedException`: Either event is disposing or disposed.

<a id="m-electron2d-inputeventjoypadmotion-astext"></a>
### `public override string AsText()`

Returns the localized axis number, a known or unknown control description and signed value with two decimals or non-finite source text.

**Returns:** The axis text with a two-decimal or non-finite value.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventjoypadmotion-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventjoypadmotion-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventjoypadmotion-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEvent](InputEvent.md). Their lifecycle and error contracts remain applicable unless this page states an override.
