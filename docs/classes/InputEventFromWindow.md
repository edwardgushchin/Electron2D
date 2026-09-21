# InputEventFromWindow

Last updated: 2026-09-21

**Inherits:** [InputEvent](InputEvent.md)

**Inherited By:** [InputEventScreenDrag](InputEventScreenDrag.md), [InputEventScreenTouch](InputEventScreenTouch.md), [InputEventWithModifiers](InputEventWithModifiers.md)

- **Source:** [`src/Core/Input/InputEvent.cs`](../../src/Core/Input/InputEvent.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class InputEventFromWindow : InputEvent`

> Provides the abstract base for input events received from a window.

## Description

Provides the abstract base for input events received from a window.

- Responsibility: adds a host-defined `long WindowId`; zero means primary/unspecified.
- Complete declared API: `WindowId { get; set; }`; protected overrides `CopyEventStateTo` and `GetPropertyDescriptors`. `WindowId` is a stored typed descriptor.
- Lifecycle/errors: mutable Resource state; disposed access throws; copying preserves the identifier.
- Threading/dependencies: caller-coordinated, backend-neutral; a real window registry begins with the SDL window-host trigger in ADR 0038.
- Verification: duplication and concrete positional-event tests exercise preservation. Inherited API is documented by [`InputEvent`](InputEvent.md).

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using InputEventFromWindow inputEvent = new InputEventMouseButton { WindowId = 1 };
Console.WriteLine(inputEvent.WindowId);
```

## Constructors

| Member | Description |
| --- | --- |
| [`protected InputEventFromWindow()`](#m-electron2d-inputeventfromwindow-ctor) | Initializes a new InputEventFromWindow instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public long WindowId { get; set; }`](#p-electron2d-inputeventfromwindow-windowid) | Gets or sets the receiving window identifier. |

## Methods

| Member | Description |
| --- | --- |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventfromwindow-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventfromwindow-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventfromwindow-ctor"></a>
### `protected InputEventFromWindow()`

Initializes a new InputEventFromWindow instance.

## Property Descriptions

<a id="p-electron2d-inputeventfromwindow-windowid"></a>
### `public long WindowId { get; set; }`

Gets or sets the receiving window identifier.

**Value:** A host-defined identifier; zero denotes the primary or unspecified window.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventfromwindow-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventfromwindow-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEvent](InputEvent.md). Their lifecycle and error contracts remain applicable unless this page states an override.
