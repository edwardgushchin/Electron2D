# InputEventMouseMotion

Last updated: 2026-09-24

**Inherits:** [InputEventMouse](InputEventMouse.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventMouse.cs`](../../src/Core/Input/InputEventMouse.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventMouseMotion : InputEventMouse`

> Represents mouse or stylus motion.

## Description

Represents mouse or stylus motion.

- Responsibility: mouse/stylus movement in local and unscaled screen coordinate spaces.
- Complete declared API: `PenInverted`, `Pressure`, `Relative`, `ScreenRelative`, `Velocity`, `ScreenVelocity`, `Tilt`; overrides `Accumulate`, `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. All seven declared values are stored typed descriptors.
- Accumulation: requires equal window, press/cancel, buttons, and modifiers; retains arithmetic overflow, atomically adopts newest positions/velocities and both sums, then emits one change notification.
- Transform: transforms local position/relative/velocity only, preserving global/screen values.
- Text: `AsText` emits the source-format position/velocity sentence and translates its two-slot template through this event's domain. Integral components retain `.0`, fractional components use six-decimal real formatting, and non-finite values use `nan`/`inf`. The source template has two `%s` placeholders; a translated template without two supported placeholders falls back to the source sentence.
- Errors/threading/verification: source values are retained without range checks; positional transforms still require finite derived local coordinates. Disposed access fails. Managed tests cover defaults, non-finite copies, overflow accumulation, representative text formatting and throwing-observer post-commit state; Wayland synthetic input checks pixel deltas, timed velocity, captured-mode zero velocity and rejection of malformed native input. Exact all-float text rounding remains unaudited, and native pen pressure/eraser/tilt delivery remains absent.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var motion = new InputEventMouseMotion { Position = new Vector2(120f, 80f), Relative = new Vector2(2f, -1f) };
Input.Instance.ParseInputEvent(motion);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventMouseMotion()`](#m-electron2d-inputeventmousemotion-ctor) | Initializes a new InputEventMouseMotion instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public bool PenInverted { get; set; }`](#p-electron2d-inputeventmousemotion-peninverted) | Gets or sets whether the eraser end of a stylus generated the event. |
| [`public float Pressure { get; set; }`](#p-electron2d-inputeventmousemotion-pressure) | Gets or sets stylus pressure. |
| [`public Vector2 Relative { get; set; }`](#p-electron2d-inputeventmousemotion-relative) | Gets or sets local movement since the previous event. |
| [`public Vector2 ScreenRelative { get; set; }`](#p-electron2d-inputeventmousemotion-screenrelative) | Gets or sets unscaled screen-space movement since the previous event. |
| [`public Vector2 Velocity { get; set; }`](#p-electron2d-inputeventmousemotion-velocity) | Gets or sets local pointer velocity. |
| [`public Vector2 ScreenVelocity { get; set; }`](#p-electron2d-inputeventmousemotion-screenvelocity) | Gets or sets unscaled screen-space pointer velocity. |
| [`public Vector2 Tilt { get; set; }`](#p-electron2d-inputeventmousemotion-tilt) | Gets or sets stylus tilt. |

## Methods

| Member | Description |
| --- | --- |
| [`public override bool Accumulate(InputEvent withEvent)`](#m-electron2d-inputeventmousemotion-accumulate-electron2d-inputevent) | Attempts to merge a newer compatible motion event into this event. |
| [`public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`](#m-electron2d-inputeventmousemotion-xformedby-electron2d-transform-electron2d-vector2) | Returns this event transformed into another local coordinate space. |
| [`public override string AsText()`](#m-electron2d-inputeventmousemotion-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventmousemotion-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventmousemotion-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventmousemotion-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventmousemotion-ctor"></a>
### `public InputEventMouseMotion()`

Initializes a new InputEventMouseMotion instance.

## Property Descriptions

<a id="p-electron2d-inputeventmousemotion-peninverted"></a>
### `public bool PenInverted { get; set; }`

Gets or sets whether the eraser end of a stylus generated the event.

**Value:** `false` by default.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousemotion-pressure"></a>
### `public float Pressure { get; set; }`

Gets or sets stylus pressure.

**Value:** The source pressure, normally from zero through one; arbitrary values are retained.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousemotion-relative"></a>
### `public Vector2 Relative { get; set; }`

Gets or sets local movement since the previous event.

**Value:** The source content-scaled delta in pixels, retained without normalization.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousemotion-screenrelative"></a>
### `public Vector2 ScreenRelative { get; set; }`

Gets or sets unscaled screen-space movement since the previous event.

**Value:** The source delta in screen pixels that is not changed by [`InputEvent.XformedBy(Transform,Vector2)`](InputEvent.md#m-electron2d-inputevent-xformedby-electron2d-transform-electron2d-vector2).

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousemotion-velocity"></a>
### `public Vector2 Velocity { get; set; }`

Gets or sets local pointer velocity.

**Value:** The source content-scaled velocity in pixels per second.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousemotion-screenvelocity"></a>
### `public Vector2 ScreenVelocity { get; set; }`

Gets or sets unscaled screen-space pointer velocity.

**Value:** The source velocity in screen pixels per second that is not transformed. Captured native mode reports zero.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmousemotion-tilt"></a>
### `public Vector2 Tilt { get; set; }`

Gets or sets stylus tilt.

**Value:** The source tilt components, normally from minus one through one; arbitrary values are retained.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventmousemotion-accumulate-electron2d-inputevent"></a>
### `public override bool Accumulate(InputEvent withEvent)`

Attempts to merge a newer compatible motion event into this event.

**Parameters**

- `withEvent`: The newer event.

**Returns:** `true` when this event was updated; otherwise `false`.

**Exceptions**

- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the complete accumulated state is assigned.

<a id="m-electron2d-inputeventmousemotion-xformedby-electron2d-transform-electron2d-vector2"></a>
### `public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`

Returns this event transformed into another local coordinate space.

**Parameters**

- `transform`: The affine transform applied to local positions and local motion vectors.
- `localOffset`: An offset added before local positions are transformed.

**Returns:** A transformed copy for positional events; this same event for non-positional events.

**Exceptions**

- `ArgumentOutOfRangeException`: `transform` or `localOffset` contains NaN or infinity.

**Remarks:** Global and screen-space coordinates are not transformed. All derived positional/motion values are checked for finiteness before duplicating the event; overflow throws ArgumentOutOfRangeException without allocating a partial copy. The successful copy is caller-owned and has a distinct InstanceID. [Canvas coordinate checks](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs) cover this failure boundary across positional event types.

<a id="m-electron2d-inputeventmousemotion-astext"></a>
### `public override string AsText()`

Returns a localized position-and-velocity description. The source sentence uses invariant real components in parenthesized vector slots; a malformed translated template falls back to the source sentence.

**Returns:** The motion sentence with current position and velocity.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventmousemotion-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventmousemotion-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventmousemotion-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventMouse](InputEventMouse.md). Their lifecycle and error contracts remain applicable unless this page states an override.
