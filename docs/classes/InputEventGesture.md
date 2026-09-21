# InputEventGesture

Last updated: 2026-09-21

**Inherits:** [InputEventWithModifiers](InputEventWithModifiers.md)

**Inherited By:** [InputEventMagnifyGesture](InputEventMagnifyGesture.md), [InputEventPanGesture](InputEventPanGesture.md)

- **Source:** [`src/Core/Input/InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class InputEventGesture : InputEventWithModifiers`

> Provides local position and modifier state shared by multi-touch gesture events.

## Description

Provides local position and modifier state shared by multi-touch gesture events.

- Responsibility: common finite local `Position` plus modifier/window/device data for multi-touch gestures; constructor changes the inherited keyboard default device to touch device zero.
- Complete declared API: protected constructor; `Position`; protected overrides `CopyEventStateTo` and `GetPropertyDescriptors`. `Position` is a stored typed descriptor.
- Lifecycle/threading: caller-owned mutable Resource; disposed/non-finite access fails; no internal synchronization.
- Verification: concrete magnify/pan copy and transform tests exercise the base contract.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using InputEventGesture gesture = new InputEventPanGesture { Position = new Vector2(20f, 30f) };
Vector2 origin = gesture.Position;
```

## Constructors

| Member | Description |
| --- | --- |
| [`protected InputEventGesture()`](#m-electron2d-inputeventgesture-ctor) | Initializes a gesture event with the primary touch-device identifier. |

## Properties

| Member | Description |
| --- | --- |
| [`public Vector2 Position { get; set; }`](#p-electron2d-inputeventgesture-position) | Gets or sets the gesture position in the current local coordinate space. |

## Methods

| Member | Description |
| --- | --- |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventgesture-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventgesture-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventgesture-ctor"></a>
### `protected InputEventGesture()`

Initializes a gesture event with the primary touch-device identifier.

## Property Descriptions

<a id="p-electron2d-inputeventgesture-position"></a>
### `public Vector2 Position { get; set; }`

Gets or sets the gesture position in the current local coordinate space.

**Value:** A finite position in pixels.

**Exceptions**

- `ArgumentOutOfRangeException`: The value contains NaN or infinity.
- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventgesture-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventgesture-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventWithModifiers](InputEventWithModifiers.md). Their lifecycle and error contracts remain applicable unless this page states an override.
