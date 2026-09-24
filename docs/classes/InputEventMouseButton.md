# InputEventMouseButton

Last updated: 2026-09-24

**Inherits:** [InputEventMouse](InputEventMouse.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventMouse.cs`](../../src/Core/Input/InputEventMouse.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventMouseButton : InputEventMouse`

> Represents a mouse button or wheel press and release.

## Description

Represents a mouse button or wheel press and release.

- Responsibility: non-wheel button or wheel-direction press/release.
- Complete declared API: `ButtonIndex`, `Pressed`, `Canceled`, `DoubleClick`, and source `Factor`; overrides `IsMatch`, `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. All five declared values are stored typed descriptors.
- Matching/state: binding identity is button plus optional exact modifiers. Canceled is neither press nor release. Wheel directions never enter the held-button mask.
- Transform: returns an independent duplicate with transformed local position; global position is preserved.
- Errors/threading/verification: caller-supplied numeric button IDs and factor values are retained, including negative and non-finite values. Disposed access and disposed peers fail. Managed tests cover defaults, cancellation, duplication, transforms and localized button descriptions; Wayland synthetic events cover fractional wheel factor and invalid native input recovery. Wear OS rotary mapping and other platforms remain separate gaps.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var click = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true };
Input.Instance.ParseInputEvent(click);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventMouseButton()`](#m-electron2d-inputeventmousebutton-ctor) | Initializes a new InputEventMouseButton instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public MouseButton ButtonIndex { get; set; }`](#p-electron2d-inputeventmousebutton-buttonindex) | Gets or sets the button or wheel direction. |
| [`public bool Pressed { get; set; }`](#p-electron2d-inputeventmousebutton-pressed) | Gets or sets whether the button is pressed. |
| [`public bool Canceled { get; set; }`](#p-electron2d-inputeventmousebutton-canceled) | Gets or sets whether the event was canceled. |
| [`public bool DoubleClick { get; set; }`](#p-electron2d-inputeventmousebutton-doubleclick) | Gets or sets whether this press completed a double click. |
| [`public float Factor { get; set; }`](#p-electron2d-inputeventmousebutton-factor) | Gets or sets the platform-provided event amount. |

## Methods

| Member | Description |
| --- | --- |
| [`public override bool IsMatch(InputEvent event, bool exactMatch = true)`](#m-electron2d-inputeventmousebutton-ismatch-electron2d-inputevent-system-boolean) | Tests whether this event has the same binding configuration as another event. |
| [`public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`](#m-electron2d-inputeventmousebutton-xformedby-electron2d-transform-electron2d-vector2) | Returns this event transformed into another local coordinate space. |
| [`public override string AsText()`](#m-electron2d-inputeventmousebutton-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventmousebutton-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventmousebutton-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventmousebutton-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventmousebutton-ctor"></a>
### `public InputEventMouseButton()`

Initializes a new InputEventMouseButton instance.

## Property Descriptions

<a id="p-electron2d-inputeventmousebutton-buttonindex"></a>
### `public MouseButton ButtonIndex { get; set; }`

Gets or sets the button or wheel direction.

**Value:** [`MouseButton.None`](MouseButton.md#f-electron2d-mousebutton-none) by default; caller-supplied numeric IDs are retained without validation.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousebutton-pressed"></a>
### `public bool Pressed { get; set; }`

Gets or sets whether the button is pressed.

**Value:** `false` for a release; `true` for a press.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousebutton-canceled"></a>
### `public bool Canceled { get; set; }`

Gets or sets whether the event was canceled.

**Value:** A canceled event is neither pressed nor released.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousebutton-doubleclick"></a>
### `public bool DoubleClick { get; set; }`

Gets or sets whether this press completed a double click.

**Value:** `false` by default.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousebutton-factor"></a>
### `public float Factor { get; set; }`

Gets or sets the platform-provided event amount.

**Value:** The source amount, with one as the default. High-precision wheel events use it as their scroll amount; zero and arbitrary caller-supplied values are retained.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventmousebutton-ismatch-electron2d-inputevent-system-boolean"></a>
### `public override bool IsMatch(InputEvent event, bool exactMatch = true)`

Tests whether this event has the same binding configuration as another event.

**Parameters**

- `event`: The event to compare.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the binding configurations match.

**Exceptions**

- `ArgumentNullException`: `event` is `null`.
- `ObjectDisposedException`: Either event is disposing or disposed.

<a id="m-electron2d-inputeventmousebutton-xformedby-electron2d-transform-electron2d-vector2"></a>
### `public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`

Returns this event transformed into another local coordinate space.

**Parameters**

- `transform`: The affine transform applied to local positions and local motion vectors.
- `localOffset`: An offset added before local positions are transformed.

**Returns:** A transformed copy for positional events; this same event for non-positional events.

**Exceptions**

- `ArgumentOutOfRangeException`: `transform` or `localOffset` contains NaN or infinity.

**Remarks:** Global and screen-space coordinates are not transformed. All derived positional/motion values are checked for finiteness before duplicating the event; overflow throws ArgumentOutOfRangeException without allocating a partial copy. The successful copy is caller-owned and has a distinct InstanceID. [Canvas coordinate checks](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs) cover this failure boundary across positional event types.

<a id="m-electron2d-inputeventmousebutton-astext"></a>
### `public override string AsText()`

Returns the button or wheel name with active modifiers and an optional double-click suffix. The nine known names and fallback `Button #n` are resolved through this event's translation domain.

**Returns:** A known button description or numeric fallback with applicable modifiers and double-click state.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventmousebutton-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventmousebutton-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventmousebutton-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventMouse](InputEventMouse.md). Their lifecycle and error contracts remain applicable unless this page states an override.
