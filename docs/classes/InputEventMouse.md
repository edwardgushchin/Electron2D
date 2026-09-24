# InputEventMouse

Last updated: 2026-09-24

**Inherits:** [InputEventWithModifiers](InputEventWithModifiers.md)

**Inherited By:** [InputEventMouseButton](InputEventMouseButton.md), [InputEventMouseMotion](InputEventMouseMotion.md)

- **Source:** [`src/Core/Input/InputEventMouse.cs`](../../src/Core/Input/InputEventMouse.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class InputEventMouse : InputEventWithModifiers`

> Provides position, button-mask, and modifier state shared by mouse events.

## Description

Provides position, button-mask, and modifier state shared by mouse events.

- Responsibility: common mouse button mask, local position, and canvas or viewport global position; initializes inherited `Device` to mouse ID 32, including its typed revert default.
- Complete declared API: protected constructor; `ButtonMask`, `Position`, `GlobalPosition`; protected overrides `CopyEventStateTo` and `GetPropertyDescriptors`. All three values are stored typed descriptors.
- Invariants: the five named held-button bits are Left/Right/Middle/X1/X2, while a caller-supplied bitfield is stored without narrowing. Source positions are retained, including non-finite components; positional transforms still require finite local coordinates. Concrete transforms preserve `GlobalPosition`.
- Threading/lifecycle: caller-owned mutable Resource; disposed access fails.
- Verification: `VerifyInput` checks defaults, raw mask bits, non-finite positions, change notification and copies on both mouse subclasses. `CanvasCoordinateTests` checks viewport and item transforms; `ControlInputTests` checks default and transformed CanvasLayer GUI coordinates.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using InputEventMouse inputEvent = new InputEventMouseMotion { Position = new Vector2(20f, 30f) };
Vector2 pointer = inputEvent.Position;
```

## Constructors

| Member | Description |
| --- | --- |
| [`protected InputEventMouse()`](#m-electron2d-inputeventmouse-ctor) | Initializes a mouse event with the primary-mouse device identifier. |

## Properties

| Member | Description |
| --- | --- |
| [`public MouseButtonMask ButtonMask { get; set; }`](#p-electron2d-inputeventmouse-buttonmask) | Gets or sets the buttons held while this event occurred. |
| [`public Vector2 Position { get; set; }`](#p-electron2d-inputeventmouse-position) | Gets or sets the pointer position in the current local coordinate space. |
| [`public Vector2 GlobalPosition { get; set; }`](#p-electron2d-inputeventmouse-globalposition) | Gets or sets the pointer position in the containing window/viewport coordinate space. |

## Methods

| Member | Description |
| --- | --- |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventmouse-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventmouse-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventmouse-ctor"></a>
### `protected InputEventMouse()`

Initializes a mouse event with the primary-mouse device identifier.

## Property Descriptions

<a id="p-electron2d-inputeventmouse-buttonmask"></a>
### `public MouseButtonMask ButtonMask { get; set; }`

Gets or sets the buttons held while this event occurred.

**Value:** The source bitfield, normally a combination of the five named non-wheel mouse-button bits. Other bits are retained as supplied.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmouse-position"></a>
### `public Vector2 Position { get; set; }`

Gets or sets the pointer position in the current local coordinate space.

**Value:** The source local position in pixels, retained without normalization. `XformedBy` rejects non-finite local coordinates before copying.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventmouse-globalposition"></a>
### `public Vector2 GlobalPosition { get; set; }`

Gets or sets the pointer position in the containing window/viewport coordinate space.

**Value:** The source viewport or canvas position in pixels, retained without normalization and preserved by [`InputEvent.XformedBy(Transform,Vector2)`](InputEvent.md#m-electron2d-inputevent-xformedby-electron2d-transform-electron2d-vector2).

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventmouse-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventmouse-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventWithModifiers](InputEventWithModifiers.md). Their lifecycle and error contracts remain applicable unless this page states an override.

Viewport input localization resets GlobalPosition to its viewport-local Position. CanvasItem.MakeInputLocal preserves GlobalPosition while converting Position to the item. Control GUI dispatch then sets the borrowed local copy's GlobalPosition to the pointer in its CanvasLayer coordinates; the source stays unchanged. An overflow in that derived coordinate rejects callback delivery and disposes the temporary copy. Raw host events use client coordinates. Nested viewport input routing remains absent and is tracked in coverage.
