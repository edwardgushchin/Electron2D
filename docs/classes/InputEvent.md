# InputEvent

Last updated: 2026-09-21

**Inherits:** [Resource](Resource.md)

**Inherited By:** [InputEventAction](InputEventAction.md), [InputEventFromWindow](InputEventFromWindow.md), [InputEventJoypadButton](InputEventJoypadButton.md), [InputEventJoypadMotion](InputEventJoypadMotion.md)

- **Source:** [`src/Core/Input/InputEvent.cs`](../../src/Core/Input/InputEvent.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class InputEvent : Resource`

> Provides the abstract base contract for all engine input events.

## Description

Provides the abstract base contract for all engine input events.

`InputEvent` is the mutable, duplicable Resource base for caller-owned input payloads and action bindings. It has no native handle.

Events are mutable resources so action bindings can be configured in memory. A platform host creates concrete
events and passes them to [`Input.ParseInputEvent(InputEvent)`](Input.md#m-electron2d-input-parseinputevent-electron2d-inputevent); this class has no dependency on a native backend.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using InputEvent inputEvent = new InputEventKey { Keycode = Key.Space, Pressed = true };
Console.WriteLine(inputEvent.AsText());
```

## Constructors

| Member | Description |
| --- | --- |
| [`protected InputEvent()`](#m-electron2d-inputevent-ctor) | Initializes a new InputEvent instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public int Device { get; set; }`](#p-electron2d-inputevent-device) | Gets or sets the source device identifier. |
| [`protected bool PressedState { get; set; }`](#p-electron2d-inputevent-pressedstate) | Gets or sets the raw press state used by concrete button-like events. |
| [`protected bool CanceledState { get; set; }`](#p-electron2d-inputevent-canceledstate) | Gets or sets the raw cancellation state used by concrete cancelable events. |

## Methods

| Member | Description |
| --- | --- |
| [`public virtual bool IsAction(string action, bool exactMatch = false)`](#m-electron2d-inputevent-isaction-system-string-system-boolean) | Gets whether this event matches a registered action. |
| [`public bool IsActionPressed(string action, bool allowEcho = false, bool exactMatch = false)`](#m-electron2d-inputevent-isactionpressed-system-string-system-boolean-system-boolean) | Gets whether this event presses a registered action. |
| [`public bool IsActionReleased(string action, bool exactMatch = false)`](#m-electron2d-inputevent-isactionreleased-system-string-system-boolean) | Gets whether this event releases a registered action. |
| [`public float GetActionStrength(string action, bool exactMatch = false)`](#m-electron2d-inputevent-getactionstrength-system-string-system-boolean) | Gets the deadzone-adjusted strength contributed by this event to an action. |
| [`public float GetActionRawStrength(string action, bool exactMatch = false)`](#m-electron2d-inputevent-getactionrawstrength-system-string-system-boolean) | Gets the strength contributed by this event before action deadzone remapping. |
| [`public bool IsCanceled()`](#m-electron2d-inputevent-iscanceled) | Gets whether the event was canceled by its source. |
| [`public bool IsPressed()`](#m-electron2d-inputevent-ispressed) | Gets whether the event represents a non-canceled press. |
| [`public bool IsReleased()`](#m-electron2d-inputevent-isreleased) | Gets whether the event represents a non-canceled release. |
| [`public virtual bool IsEcho()`](#m-electron2d-inputevent-isecho) | Gets whether this is an operating-system key-repeat event. |
| [`public bool IsActionType()`](#m-electron2d-inputevent-isactiontype) | Gets whether this event type may be bound to an input action. |
| [`public virtual bool IsMatch(InputEvent event, bool exactMatch = true)`](#m-electron2d-inputevent-ismatch-electron2d-inputevent-system-boolean) | Tests whether this event has the same binding configuration as another event. |
| [`public virtual bool Accumulate(InputEvent withEvent)`](#m-electron2d-inputevent-accumulate-electron2d-inputevent) | Attempts to merge a newer compatible motion event into this event. |
| [`public virtual InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`](#m-electron2d-inputevent-xformedby-electron2d-transform-electron2d-vector2) | Returns this event transformed into another local coordinate space. |
| [`public abstract string AsText()`](#m-electron2d-inputevent-astext) | Returns a concise, human-readable representation of the event. |
| [`protected abstract InputEvent CreateEventInstance()`](#m-electron2d-inputevent-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected virtual void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputevent-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected static void ValidateFinite(Vector2 value, string parameterName)`](#m-electron2d-inputevent-validatefinite-electron2d-vector2-system-string) | Rejects non-finite two-dimensional event data. |
| [`protected static void ValidateFinite(Transform value, string parameterName)`](#m-electron2d-inputevent-validatefinite-electron2d-transform-system-string) | Rejects a non-finite event-coordinate transform. |
| [`protected void EmitInputChanged(bool propertyListChanged = false)`](#m-electron2d-inputevent-emitinputchanged-system-boolean) | Invalidates registered action state, optionally reports property-list changes, and emits one content-change event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputevent-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override Resource CreateDuplicateInstance()`](#m-electron2d-inputevent-createduplicateinstance) | Creates a fresh default instance used as the target of duplication. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource, Resource> duplicateSubresource, Func<Resource, Resource> forceDuplicateSubresource)`](#m-electron2d-inputevent-copycustomstateto-electron2d-resource-system-boolean-electron2d-deepduplicatemode-system-func-electron2d-resource-electron2d-resource-system-func-electron2d-resource-electron2d-resource) | Copies derived stored state into a duplicate or copy target. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-inputevent-dispose-system-boolean) | Releases resources owned by a derived class. |
| [`public override string ToString()`](#m-electron2d-inputevent-tostring) | Returns [`InputEvent.AsText`](InputEvent.md#m-electron2d-inputevent-astext). |

## Constants

| Member | Description |
| --- | --- |
| [`public const int DeviceIdEmulation = -1`](#f-electron2d-inputevent-deviceidemulation) | Identifies input synthesized from another pointing-device family. |
| [`public const int DeviceIdKeyboard = 16`](#f-electron2d-inputevent-deviceidkeyboard) | Identifies the primary keyboard. |
| [`public const int DeviceIdMouse = 32`](#f-electron2d-inputevent-deviceidmouse) | Identifies the primary mouse. |

## Constructor Descriptions

<a id="m-electron2d-inputevent-ctor"></a>
### `protected InputEvent()`

Initializes a new InputEvent instance.

## Property Descriptions

<a id="p-electron2d-inputevent-device"></a>
### `public int Device { get; set; }`

Gets or sets the source device identifier.

**Value:** A host-defined identifier. Negative values are reserved for synthesized or internal events.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputevent-pressedstate"></a>
### `protected bool PressedState { get; set; }`

Gets or sets the raw press state used by concrete button-like events.

<a id="p-electron2d-inputevent-canceledstate"></a>
### `protected bool CanceledState { get; set; }`

Gets or sets the raw cancellation state used by concrete cancelable events.

## Method Descriptions

<a id="m-electron2d-inputevent-isaction-system-string-system-boolean"></a>
### `public virtual bool IsAction(string action, bool exactMatch = false)`

Gets whether this event matches a registered action.

**Parameters**

- `action`: The nonblank, case-sensitive action name.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the process-wide input map contains a matching binding.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-isactionpressed-system-string-system-boolean-system-boolean"></a>
### `public bool IsActionPressed(string action, bool allowEcho = false, bool exactMatch = false)`

Gets whether this event presses a registered action.

**Parameters**

- `action`: The nonblank, case-sensitive action name.
- `allowEcho`: Whether a repeated keyboard event may count as a press.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the event matches, is pressed, and satisfies the echo policy.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-isactionreleased-system-string-system-boolean"></a>
### `public bool IsActionReleased(string action, bool exactMatch = false)`

Gets whether this event releases a registered action.

**Parameters**

- `action`: The nonblank, case-sensitive action name.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the event matches and is a non-canceled release.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-getactionstrength-system-string-system-boolean"></a>
### `public float GetActionStrength(string action, bool exactMatch = false)`

Gets the deadzone-adjusted strength contributed by this event to an action.

**Parameters**

- `action`: The nonblank, case-sensitive action name.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** A value from zero through one, or zero when the event does not match.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-getactionrawstrength-system-string-system-boolean"></a>
### `public float GetActionRawStrength(string action, bool exactMatch = false)`

Gets the strength contributed by this event before action deadzone remapping.

**Parameters**

- `action`: The nonblank, case-sensitive action name.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** A value from zero through one, or zero when the event does not match.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-iscanceled"></a>
### `public bool IsCanceled()`

Gets whether the event was canceled by its source.

**Returns:** `true` for a canceled event; otherwise `false`.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-ispressed"></a>
### `public bool IsPressed()`

Gets whether the event represents a non-canceled press.

**Returns:** `true` only when the event is pressed and not canceled.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-isreleased"></a>
### `public bool IsReleased()`

Gets whether the event represents a non-canceled release.

**Returns:** `true` only when the event is not pressed and not canceled.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-isecho"></a>
### `public virtual bool IsEcho()`

Gets whether this is an operating-system key-repeat event.

**Returns:** `false` except for a repeating [`InputEventKey`](InputEventKey.md).

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-isactiontype"></a>
### `public bool IsActionType()`

Gets whether this event type may be bound to an input action.

**Returns:** `true` for key, mouse-button, gamepad-button, gamepad-motion, and action events.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-ismatch-electron2d-inputevent-system-boolean"></a>
### `public virtual bool IsMatch(InputEvent event, bool exactMatch = true)`

Tests whether this event has the same binding configuration as another event.

**Parameters**

- `event`: The event to compare.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the binding configurations match.

**Exceptions**

- `ArgumentNullException`: `event` is `null`.
- `ObjectDisposedException`: Either event is disposing or disposed.

<a id="m-electron2d-inputevent-accumulate-electron2d-inputevent"></a>
### `public virtual bool Accumulate(InputEvent withEvent)`

Attempts to merge a newer compatible motion event into this event.

**Parameters**

- `withEvent`: The newer event.

**Returns:** `true` when this event was updated; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `withEvent` is `null`.
- `ObjectDisposedException`: Either event is disposing or disposed.

<a id="m-electron2d-inputevent-xformedby-electron2d-transform-electron2d-vector2"></a>
### `public virtual InputEvent XformedBy(Transform transform, Vector2 localOffset = default)`

Returns this event transformed into another local coordinate space.

**Parameters**

- `transform`: The affine transform applied to local positions and local motion vectors.
- `localOffset`: An offset added before local positions are transformed.

**Returns:** A transformed copy for positional events; this same event for non-positional events.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

**Remarks:** Global and screen-space coordinates are not transformed.

<a id="m-electron2d-inputevent-astext"></a>
### `public abstract string AsText()`

Returns a concise, human-readable representation of the event.

**Returns:** A non-null description suitable for bindings and diagnostics.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputevent-createeventinstance"></a>
### `protected abstract InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputevent-copyeventstateto-electron2d-inputevent"></a>
### `protected virtual void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputevent-validatefinite-electron2d-vector2-system-string"></a>
### `protected static void ValidateFinite(Vector2 value, string parameterName)`

Rejects non-finite two-dimensional event data.

**Parameters**

- `value`: The value to validate.
- `parameterName`: The public argument or property-setter value name.

**Exceptions**

- `ArgumentOutOfRangeException`: `value` contains NaN or infinity.

<a id="m-electron2d-inputevent-validatefinite-electron2d-transform-system-string"></a>
### `protected static void ValidateFinite(Transform value, string parameterName)`

Rejects a non-finite event-coordinate transform.

**Parameters**

- `value`: The transform to validate.
- `parameterName`: The public parameter name.

**Exceptions**

- `ArgumentOutOfRangeException`: `value` contains NaN or infinity.

<a id="m-electron2d-inputevent-emitinputchanged-system-boolean"></a>
### `protected void EmitInputChanged(bool propertyListChanged = false)`

Invalidates registered action state, optionally reports property-list changes, and emits one content-change event.

**Parameters**

- `propertyListChanged`: Whether the available property metadata also changed.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A public property-list or content-change handler throws after internal action state is invalidated.

<a id="m-electron2d-inputevent-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

<a id="m-electron2d-inputevent-createduplicateinstance"></a>
### `protected override Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

**Returns:** A live resource of the exact same runtime type with empty path and scene ID.

**Exceptions**

- `NotSupportedException`: The runtime type derives from [`Resource`](Resource.md) and has not overridden this method.

**Remarks:** The base implementation supports only an exact [`Resource`](Resource.md) instance. Every derived class must
override this method, even when it adds no state, so duplication support is explicit.

<a id="m-electron2d-inputevent-copycustomstateto-electron2d-resource-system-boolean-electron2d-deepduplicatemode-system-func-electron2d-resource-electron2d-resource-system-func-electron2d-resource-electron2d-resource"></a>
### `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource, Resource> duplicateSubresource, Func<Resource, Resource> forceDuplicateSubresource)`

Copies derived stored state into a duplicate or copy target.

**Parameters**

- `target`: A live resource with the exact same runtime type.
- `deep`: Whether typed collection containers should be cloned recursively.
- `subresourceMode`: The nested-resource policy for this copy.
- `duplicateSubresource`: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource.
Pass every nested resource through this function when `deep` is `true`.
- `forceDuplicateSubresource`: A graph-preserving function that duplicates a nested resource even when the current policy would share it.
Use it for typed properties whose contract requires duplication; assign the original reference directly for
properties whose contract forbids duplication.

**Exceptions**

- `NotSupportedException`: A derived resource has not explicitly implemented custom-state copying.

**Remarks:** The base implementation supports only an exact [`Resource`](Resource.md) instance. Derived implementations must
copy all stored custom state and call the base implementation only when they intentionally want its validation.
Assigning the original nested-resource reference directly expresses a never-duplicate property.

<a id="m-electron2d-inputevent-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

Removes this event from every action map entry before inherited resource cleanup and public disposal notification.

<a id="m-electron2d-inputevent-tostring"></a>
### `public override string ToString()`

Returns [`InputEvent.AsText`](InputEvent.md#m-electron2d-inputevent-astext).

**Returns:** The current human-readable event description.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

## Constant Descriptions

<a id="f-electron2d-inputevent-deviceidemulation"></a>
### `public const int DeviceIdEmulation = -1`

Identifies input synthesized from another pointing-device family.

<a id="f-electron2d-inputevent-deviceidkeyboard"></a>
### `public const int DeviceIdKeyboard = 16`

Identifies the primary keyboard.

<a id="f-electron2d-inputevent-deviceidmouse"></a>
### `public const int DeviceIdMouse = 32`

Identifies the primary mouse.

## Inherited API

Public and protected members inherited from [Resource](Resource.md). Their lifecycle and error contracts remain applicable unless this page states an override.
