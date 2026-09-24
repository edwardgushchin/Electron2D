# InputEventMagnifyGesture

Last updated: 2026-09-24

**Inherits:** [InputEventGesture](InputEventGesture.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventMagnifyGesture : InputEventGesture`

> Represents a two-contact magnification gesture.

## Description

Represents a two-contact magnification gesture.

- Responsibility: pinch magnification around a local position.
- Complete declared API: source `Factor` (default `1`); overrides `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. `Factor` is a stored typed descriptor.
- Transform: returns an independent duplicate with transformed position; factor/modifiers/window/device are preserved.
- Text: `AsText` translates the source sentence with local position and the stored factor promoted to full real text, including zero, negative and non-finite values.
- Errors/threading/verification: zero, negative, and non-finite factors are retained as source values; disposed access fails. `VerifyInputEvents` checks these boundaries, copy and committed change delivery; coordinate transforms retain their separate finite guard.

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

**Value:** The source factor; normally values above one magnify and values between zero and one reduce. Zero, negative and non-finite values are retained without normalization.

**Exceptions**

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

**Remarks:** Global and screen-space coordinates are not transformed. All derived positional/motion values are checked for finiteness before duplicating the event; overflow throws ArgumentOutOfRangeException without allocating a partial copy. The successful copy is caller-owned and has a distinct InstanceID. [Canvas coordinate checks](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs) cover this failure boundary across positional event types.

<a id="m-electron2d-inputeventmagnifygesture-astext"></a>
### `public override string AsText()`

Returns the localized magnification sentence with local position and factor.

**Returns:** The magnification text with the stored factor promoted to double for formatting.

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
