# InputEventScreenTouch

Last updated: 2026-09-24

**Inherits:** [InputEventFromWindow](InputEventFromWindow.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventScreenTouch : InputEventFromWindow`

> Represents one touch contact beginning, ending, or being canceled.

## Description

Represents one touch contact beginning, ending, or being canceled.

- Responsibility: begin/end/cancel state for one touch contact.
- Complete declared API: signed `Index`, source `Position`, `Pressed`, `Canceled`, `DoubleTap`; overrides `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. All five declared values are stored typed descriptors.
- Transform/lifecycle: returns an independent duplicate with transformed position. A canceled contact is neither pressed nor released. Caller owns/disposes event resources.
- Errors/threading/verification: caller-supplied position values are retained; `XformedBy` rejects non-finite derived coordinates. Disposed access fails. Managed checks cover signed index, cancellation, double-tap state, copying and transform; the Wayland SDL test covers contact indexes, press/cancel/release and invalid-event recovery. The adapter does not detect native double taps; that remains a separate touch-recognition obligation.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var inputEventScreenTouch = new InputEventScreenTouch();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventScreenTouch()`](#m-electron2d-inputeventscreentouch-ctor) | Initializes a new InputEventScreenTouch instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public int Index { get; set; }`](#p-electron2d-inputeventscreentouch-index) | Gets or sets the touch-contact index. |
| [`public Vector2 Position { get; set; }`](#p-electron2d-inputeventscreentouch-position) | Gets or sets the touch position in the current local coordinate space. |
| [`public bool Pressed { get; set; }`](#p-electron2d-inputeventscreentouch-pressed) | Gets or sets whether the contact is pressed. |
| [`public bool Canceled { get; set; }`](#p-electron2d-inputeventscreentouch-canceled) | Gets or sets whether the platform canceled the contact. |
| [`public bool DoubleTap { get; set; }`](#p-electron2d-inputeventscreentouch-doubletap) | Gets or sets whether the contact begins a double tap. |

## Methods

| Member | Description |
| --- | --- |
| [`public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`](#m-electron2d-inputeventscreentouch-xformedby-electron2d-transform-electron2d-vector2) | Returns this event transformed into another local coordinate space. |
| [`public override string AsText()`](#m-electron2d-inputeventscreentouch-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventscreentouch-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventscreentouch-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventscreentouch-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventscreentouch-ctor"></a>
### `public InputEventScreenTouch()`

Initializes a new InputEventScreenTouch instance.

## Property Descriptions

<a id="p-electron2d-inputeventscreentouch-index"></a>
### `public int Index { get; set; }`

Gets or sets the touch-contact index.

**Value:** A signed identifier that remains stable for the life of one contact.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreentouch-position"></a>
### `public Vector2 Position { get; set; }`

Gets or sets the touch position in the current local coordinate space.

**Value:** The source position in pixels, retained without normalization. A requested positional transform rejects non-finite coordinates.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreentouch-pressed"></a>
### `public bool Pressed { get; set; }`

Gets or sets whether the contact is pressed.

**Value:** `false` for an end; `true` for a beginning.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreentouch-canceled"></a>
### `public bool Canceled { get; set; }`

Gets or sets whether the platform canceled the contact.

**Value:** A canceled contact is neither pressed nor released.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventscreentouch-doubletap"></a>
### `public bool DoubleTap { get; set; }`

Gets or sets whether the contact begins a double tap.

**Value:** `false` by default.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventscreentouch-xformedby-electron2d-transform-electron2d-vector2"></a>
### `public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`

Returns this event transformed into another local coordinate space.

**Parameters**

- `transform`: The affine transform applied to local positions and local motion vectors.
- `localOffset`: An offset added before local positions are transformed.

**Returns:** A transformed copy for positional events; this same event for non-positional events.

**Exceptions**

- `ArgumentOutOfRangeException`: `transform` or `localOffset` contains NaN or infinity.

**Remarks:** Global and screen-space coordinates are not transformed. All derived positional/motion values are checked for finiteness before duplicating the event; overflow throws ArgumentOutOfRangeException without allocating a partial copy. The successful copy is caller-owned and has a distinct InstanceID. [Canvas coordinate checks](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs) cover this failure boundary across positional event types.

<a id="m-electron2d-inputeventscreentouch-astext"></a>
### `public override string AsText()`

Returns a concise, human-readable representation of the event.

**Returns:** A non-null description suitable for bindings and diagnostics.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventscreentouch-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventscreentouch-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventscreentouch-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventFromWindow](InputEventFromWindow.md). Their lifecycle and error contracts remain applicable unless this page states an override.
