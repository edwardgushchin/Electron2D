# InputEventPanGesture

Last updated: 2026-09-24

**Inherits:** [InputEventGesture](InputEventGesture.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventPanGesture : InputEventGesture`

> Represents a two-contact panning gesture.

## Description

Represents a two-contact panning gesture.

- Responsibility: two-dimensional pan delta around a local gesture position.
- Complete declared API: `Delta`; overrides `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. `Delta` is a stored typed descriptor.
- Transform: returns an independent duplicate and transforms its position affinely; the platform-reported pan delta is copied unchanged.
- Text: `AsText` translates the source sentence with local position and unscaled platform delta.
- Errors/threading/verification: source delta components, including non-finite values, are retained; disposed access fails. `VerifyInputEvents` checks storage/copy and `CanvasCoordinateTests` checks transform behavior.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var inputEventPanGesture = new InputEventPanGesture();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventPanGesture()`](#m-electron2d-inputeventpangesture-ctor) | Initializes a new InputEventPanGesture instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public Vector2 Delta { get; set; }`](#p-electron2d-inputeventpangesture-delta) | Gets or sets the local panning amount since the previous gesture event. |

## Methods

| Member | Description |
| --- | --- |
| [`public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`](#m-electron2d-inputeventpangesture-xformedby-electron2d-transform-electron2d-vector2) | Returns this event transformed into another local coordinate space. |
| [`public override string AsText()`](#m-electron2d-inputeventpangesture-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventpangesture-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventpangesture-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventpangesture-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventpangesture-ctor"></a>
### `public InputEventPanGesture()`

Initializes a new InputEventPanGesture instance.

## Property Descriptions

<a id="p-electron2d-inputeventpangesture-delta"></a>
### `public Vector2 Delta { get; set; }`

Gets or sets the local panning amount since the previous gesture event.

**Value:** The source local-space delta, retained without normalization; the pan transform copies it unchanged.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventpangesture-xformedby-electron2d-transform-electron2d-vector2"></a>
### `public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`

Returns this event transformed into another local coordinate space.

**Parameters**

- `transform`: The affine transform applied to local positions and local motion vectors.
- `localOffset`: An offset added before local positions are transformed.

**Returns:** A transformed copy for positional events; this same event for non-positional events.

**Exceptions**

- `ArgumentOutOfRangeException`: `transform` or `localOffset` contains NaN or infinity.

**Remarks:** Global and screen-space coordinates are not transformed. All derived positional/motion values are checked for finiteness before duplicating the event; overflow throws ArgumentOutOfRangeException without allocating a partial copy. The successful copy is caller-owned and has a distinct InstanceID. [Canvas coordinate checks](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs) cover this failure boundary across positional event types.

<a id="m-electron2d-inputeventpangesture-astext"></a>
### `public override string AsText()`

Returns the localized panning sentence with local position and platform delta.

**Returns:** The panning text; exact all-float vector rounding remains under audit.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventpangesture-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventpangesture-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventpangesture-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventGesture](InputEventGesture.md). Their lifecycle and error contracts remain applicable unless this page states an override.
