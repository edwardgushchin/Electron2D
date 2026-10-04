# InputEventAction

Last updated: 2026-10-04

**Inherits:** [InputEvent](InputEvent.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventAction.cs`](../../src/Core/Input/InputEventAction.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventAction : InputEvent`

> Represents a named input action being pressed or released.

## Description

Represents a named input action being pressed or released.

- Responsibility: injects a named registered action independently of hardware bindings.
- Complete declared API: `Action`, signed `EventIndex` (default `-1`), `Pressed`, clamped finite `Strength`; overrides `IsAction`, `IsMatch`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. All four values are stored typed descriptors. A negative index uses the slot after mapped bindings; parsing rejects a resolved index beyond the 32-source capacity before mutation.
- Lifecycle/state: parsing a press adds one device/index source and parsing a release removes it; a zero-strength press remains logically pressed. Negative `EventIndex` values select the slot after configured bindings, while nonnegative indexes and the device identify independent pressed sources. Public `IsMatch` can recognize a physical event through the named action, while exact map binding lookup only equates synthetic events with the same action name. `AsText` prefers the action's first non-action binding text and falls back to its own name, avoiding recursive descriptions when synthetic events are registered as bindings.
- Errors/threading: null action assignment, non-finite strength, disposed use, parsing an unregistered action or exceeding the 32-source capacity throws. Caller coordinates mutation.
- Verification: constructor and descriptor defaults, notification and revert behavior, invalid strength rollback, signed index storage with dispatch capacity failure, finite clamping, zero-strength press, independent indexed sources, duplication, binding invalidation and description order are covered by managed checks. Concrete binding wording and numeric formatting have completed managed audits; native delivery and other-platform text remain separate verification limits.

This event is useful for deterministic simulation and remapping. Parsing it updates action state directly and does
not require a hardware binding, but the action itself must already exist in [`InputMap`](InputMap.md).

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var action = new InputEventAction { Action = "jump", Pressed = true, Strength = 1f };
Input.ParseInputEvent(action);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventAction()`](#m-electron2d-inputeventaction-ctor) | Initializes a new InputEventAction instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public string Action { get; set; }`](#p-electron2d-inputeventaction-action) | Gets or sets the action name. |
| [`public int EventIndex { get; set; }`](#p-electron2d-inputeventaction-eventindex) | Gets or sets the corresponding binding index. |
| [`public bool Pressed { get; set; }`](#p-electron2d-inputeventaction-pressed) | Gets or sets whether the action is pressed. |
| [`public float Strength { get; set; }`](#p-electron2d-inputeventaction-strength) | Gets or sets the analog action strength. |

## Methods

| Member | Description |
| --- | --- |
| [`public override bool IsAction(string action, bool exactMatch = false)`](#m-electron2d-inputeventaction-isaction-system-string-system-boolean) | Gets whether this event names an action. |
| [`public override bool IsMatch(InputEvent event, bool exactMatch = true)`](#m-electron2d-inputeventaction-ismatch-electron2d-inputevent-system-boolean) | Tests whether another event matches this named action. |
| [`public override string AsText()`](#m-electron2d-inputeventaction-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventaction-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventaction-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventaction-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventaction-ctor"></a>
### `public InputEventAction()`

Initializes a new InputEventAction instance.

## Property Descriptions

<a id="p-electron2d-inputeventaction-action"></a>
### `public string Action { get; set; }`

Gets or sets the action name.

**Value:** A case-sensitive name; the default is empty until configured.

**Exceptions**

- `ArgumentNullException`: The assigned value is `null`.
- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventaction-eventindex"></a>
### `public int EventIndex { get; set; }`

Gets or sets the corresponding binding index.

**Value:** Any signed integer is retained. Negative values select the slot after configured bindings; the default is `-1`.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

**Remarks:** `Input.ParseInputEvent` rejects a resolved index of 32 or greater before mutating input state. Action name, device and selected index identify the source that a later event releases.

<a id="p-electron2d-inputeventaction-pressed"></a>
### `public bool Pressed { get; set; }`

Gets or sets whether the action is pressed.

**Value:** `false` for a release; `true` for a press.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventaction-strength"></a>
### `public float Strength { get; set; }`

Gets or sets the analog action strength.

**Value:** A value clamped to zero through one. A released event contributes zero regardless of this property.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is NaN or infinite.
- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the clamped value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventaction-isaction-system-string-system-boolean"></a>
### `public override bool IsAction(string action, bool exactMatch = false)`

Gets whether this event names an action.

**Parameters**

- `action`: The nonblank, case-sensitive action name to compare.
- `exactMatch`: Ignored because a direct action has no modifier or direction ambiguity.

**Returns:** `true` when `action` equals [`InputEventAction.Action`](InputEventAction.md#p-electron2d-inputeventaction-action).

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `ObjectDisposedException`: The event is disposing or disposed.

**Remarks:** The name does not need to be registered in [`InputMap`](InputMap.md) for this comparison.

<a id="m-electron2d-inputeventaction-ismatch-electron2d-inputevent-system-boolean"></a>
### `public override bool IsMatch(InputEvent event, bool exactMatch = true)`

Tests whether another event matches this event's named action, including a physical source bound to that action.

**Parameters**

- `event`: The event to compare.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the other event matches the named action. `InputMap.ActionHasEvent` uses exact action-binding matching and keeps physical and synthetic bindings distinct.

**Exceptions**

- `ArgumentNullException`: `event` is `null`.
- `ObjectDisposedException`: Either event is disposing or disposed.

<a id="m-electron2d-inputeventaction-astext"></a>
### `public override string AsText()`

Returns the first concrete binding's text. If no concrete binding exists, returns the action name; an unnamed action returns an empty string. Synthetic bindings are skipped to prevent recursive descriptions.

**Returns:** The first concrete binding's text, the action name, or an empty string when unnamed.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventaction-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventaction-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventaction-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEvent](InputEvent.md). Their lifecycle and error contracts remain applicable unless this page states an override.
