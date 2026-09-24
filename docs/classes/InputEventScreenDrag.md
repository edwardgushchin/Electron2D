# InputEventScreenDrag

Last updated: 2026-09-24

**Inherits:** [InputEventFromWindow](InputEventFromWindow.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventScreenDrag : InputEventFromWindow`

> Represents movement of one active touch or stylus contact.

## Description

Represents movement of one active touch or stylus contact.

- Responsibility: movement/stylus data for one active touch contact.
- Complete declared API: `Index`, `PenInverted`, `Position`, `Pressure`, `Relative`, `ScreenRelative`, `Velocity`, `ScreenVelocity`, `Tilt`; overrides `Accumulate`, `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. All nine declared values are stored typed descriptors.
- Accumulation/transform: equal contact indexes merge atomically, retain arithmetic overflow in both relative sums, adopt newest position/velocities, and emit one change notification. Only local position/relative/velocity transform; screen values remain unchanged.
- Text: `AsText` translates the source sentence containing signed index, local position and local velocity; pressure, relative motion and screen-space values do not appear.
- Invariants/errors: index is a signed identifier; source vectors, pressure and tilt are retained without range checks; positional transforms still require finite derived local coordinates, and disposed access fails.
- Verification: managed checks cover signed index, non-finite copies, accumulation overflow, compatible/incompatible events and transforms. A focused Wayland SDL test covers contact indexes, finite native input recovery, pressure, physical drag deltas and timestamp velocity; native pen eraser/tilt delivery and nested viewport scaling remain absent.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var inputEventScreenDrag = new InputEventScreenDrag();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventScreenDrag()`](#m-electron2d-inputeventscreendrag-ctor) | Initializes a new InputEventScreenDrag instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public int Index { get; set; }`](#p-electron2d-inputeventscreendrag-index) | Gets or sets the touch-contact index. |
| [`public bool PenInverted { get; set; }`](#p-electron2d-inputeventscreendrag-peninverted) | Gets or sets whether the eraser end of a stylus generated the event. |
| [`public Vector2 Position { get; set; }`](#p-electron2d-inputeventscreendrag-position) | Gets or sets the drag position in the current local coordinate space. |
| [`public float Pressure { get; set; }`](#p-electron2d-inputeventscreendrag-pressure) | Gets or sets stylus pressure. |
| [`public Vector2 Relative { get; set; }`](#p-electron2d-inputeventscreendrag-relative) | Gets or sets local drag movement since the previous event. |
| [`public Vector2 ScreenRelative { get; set; }`](#p-electron2d-inputeventscreendrag-screenrelative) | Gets or sets unscaled screen-space movement since the previous event. |
| [`public Vector2 Velocity { get; set; }`](#p-electron2d-inputeventscreendrag-velocity) | Gets or sets local drag velocity. |
| [`public Vector2 ScreenVelocity { get; set; }`](#p-electron2d-inputeventscreendrag-screenvelocity) | Gets or sets unscaled screen-space drag velocity. |
| [`public Vector2 Tilt { get; set; }`](#p-electron2d-inputeventscreendrag-tilt) | Gets or sets stylus tilt. |

## Methods

| Member | Description |
| --- | --- |
| [`public override bool Accumulate(InputEvent withEvent)`](#m-electron2d-inputeventscreendrag-accumulate-electron2d-inputevent) | Attempts to merge a newer compatible motion event into this event. |
| [`public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`](#m-electron2d-inputeventscreendrag-xformedby-electron2d-transform-electron2d-vector2) | Returns this event transformed into another local coordinate space. |
| [`public override string AsText()`](#m-electron2d-inputeventscreendrag-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventscreendrag-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventscreendrag-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventscreendrag-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventscreendrag-ctor"></a>
### `public InputEventScreenDrag()`

Initializes a new InputEventScreenDrag instance.

## Property Descriptions

<a id="p-electron2d-inputeventscreendrag-index"></a>
### `public int Index { get; set; }`

Gets or sets the touch-contact index.

**Value:** A signed identifier matching the corresponding touch event.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreendrag-peninverted"></a>
### `public bool PenInverted { get; set; }`

Gets or sets whether the eraser end of a stylus generated the event.

**Value:** `false` by default.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreendrag-position"></a>
### `public Vector2 Position { get; set; }`

Gets or sets the drag position in the current local coordinate space.

**Value:** The source position in pixels, retained without normalization.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreendrag-pressure"></a>
### `public float Pressure { get; set; }`

Gets or sets stylus pressure.

**Value:** The source pressure, normally from zero through one; arbitrary values are retained.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreendrag-relative"></a>
### `public Vector2 Relative { get; set; }`

Gets or sets local drag movement since the previous event.

**Value:** The source content-scaled delta in pixels.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreendrag-screenrelative"></a>
### `public Vector2 ScreenRelative { get; set; }`

Gets or sets unscaled screen-space movement since the previous event.

**Value:** The source delta in screen pixels that is not transformed.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreendrag-velocity"></a>
### `public Vector2 Velocity { get; set; }`

Gets or sets local drag velocity.

**Value:** The source content-scaled velocity in pixels per second.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreendrag-screenvelocity"></a>
### `public Vector2 ScreenVelocity { get; set; }`

Gets or sets unscaled screen-space drag velocity.

**Value:** The source velocity in screen pixels per second that is not transformed.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreendrag-tilt"></a>
### `public Vector2 Tilt { get; set; }`

Gets or sets stylus tilt.

**Value:** The source tilt components, normally from minus one through one; arbitrary values are retained.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventscreendrag-accumulate-electron2d-inputevent"></a>
### `public override bool Accumulate(InputEvent withEvent)`

Attempts to merge a newer compatible motion event into this event.

**Parameters**

- `withEvent`: The newer event.

**Returns:** `true` when this event was updated; otherwise `false`.

**Exceptions**

- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the complete accumulated state is assigned.

<a id="m-electron2d-inputeventscreendrag-xformedby-electron2d-transform-electron2d-vector2"></a>
### `public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`

Returns this event transformed into another local coordinate space.

**Parameters**

- `transform`: The affine transform applied to local positions and local motion vectors.
- `localOffset`: An offset added before local positions are transformed.

**Returns:** A transformed copy for positional events; this same event for non-positional events.

**Exceptions**

- `ArgumentOutOfRangeException`: `transform` or `localOffset` contains NaN or infinity.

**Remarks:** Global and screen-space coordinates are not transformed. All derived positional/motion values are checked for finiteness before duplicating the event; overflow throws ArgumentOutOfRangeException without allocating a partial copy. The successful copy is caller-owned and has a distinct InstanceID. [Canvas coordinate checks](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs) cover this failure boundary across positional event types.

<a id="m-electron2d-inputeventscreendrag-astext"></a>
### `public override string AsText()`

Returns a localized sentence containing signed contact index, local position and local velocity.

**Returns:** The drag sentence; exact all-float vector rounding remains under audit.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventscreendrag-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventscreendrag-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventscreendrag-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventFromWindow](InputEventFromWindow.md). Their lifecycle and error contracts remain applicable unless this page states an override.
