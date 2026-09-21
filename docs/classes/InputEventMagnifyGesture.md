# InputEventMagnifyGesture

Last updated: 2026-09-21

**Inherits:** [InputEventGesture](InputEventGesture.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventMagnifyGesture : InputEventGesture`

> Represents a two-contact magnification gesture.

## Description

Represents a two-contact magnification gesture.

- Responsibility: pinch magnification around a local position.
- Complete declared API: finite positive `Factor` (default `1`); overrides `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. `Factor` is a stored typed descriptor.
- Transform: returns an independent duplicate with transformed position; factor/modifiers/window/device are preserved.
- Errors/threading/verification: zero, negative, non-finite, or disposed access fails; factor boundaries, copy, text, and transform are covered.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var inputEventMagnifyGesture = new InputEventMagnifyGesture();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventMagnifyGesture()`](#m-electron2d-inputeventmagnifygesture-ctor) | Initializes a new InputEventMagnifyGesture instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public float Factor { get; set; }`](#p-electron2d-inputeventmagnifygesture-factor) | Gets or sets the magnification delta. |

## Methods

| Member | Description |
| --- | --- |
| [`public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`](#m-electron2d-inputeventmagnifygesture-xformedby-electron2d-transform-electron2d-vector2) | Returns this event transformed into another local coordinate space. |
| [`public override string AsText()`](#m-electron2d-inputeventmagnifygesture-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventmagnifygesture-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventmagnifygesture-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventmagnifygesture-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventmagnifygesture-ctor"></a>
### `public InputEventMagnifyGesture()`

Initializes a new InputEventMagnifyGesture instance.

## Property Descriptions

<a id="p-electron2d-inputeventmagnifygesture-factor"></a>
### `public float Factor { get; set; }`

Gets or sets the magnification delta.

**Value:** A finite positive factor; values above one magnify and values below one reduce.

**Exceptions**

- `ArgumentOutOfRangeException`: The value is not finite or is less than or equal to zero.
- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventmagnifygesture-xformedby-electron2d-transform-electron2d-vector2"></a>
### `public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`

Returns this event transformed into another local coordinate space.

**Parameters**

- `transform`: The affine transform applied to local positions and local motion vectors.
- `localOffset`: An offset added before local positions are transformed.

**Returns:** A transformed copy for positional events; this same event for non-positional events.

**Exceptions**

- `ArgumentOutOfRangeException`: `transform` or `localOffset` contains NaN or infinity.

**Remarks:** Global and screen-space coordinates are not transformed.

<a id="m-electron2d-inputeventmagnifygesture-astext"></a>
### `public override string AsText()`

Returns a concise, human-readable representation of the event.

**Returns:** A non-null description suitable for bindings and diagnostics.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventmagnifygesture-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventmagnifygesture-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventmagnifygesture-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventGesture](InputEventGesture.md). Their lifecycle and error contracts remain applicable unless this page states an override.
