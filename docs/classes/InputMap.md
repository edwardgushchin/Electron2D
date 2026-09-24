# InputMap

Last updated: 2026-09-24

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputMap.cs`](../../src/Core/Input/InputMap.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputMap : ElectronObject`

> Owns the process-wide mapping from named game actions to typed input-event bindings.

## Description

Owns the process-wide mapping from named game actions to typed input-event bindings.

`InputMap` is the non-disposable process-wide registry of ordinal action names, finite deadzones, and ordered typed event bindings. Binding `Resource` references remain caller-owned and live; they must not be disposed or mutated concurrently with matching. Disposing a registered binding outside matching removes that reference from every affected action.

The registry starts from six typed `input/ui_*` definitions in [`ProjectSettings`](ProjectSettings.md): `ui_focus_next` (Tab), `ui_focus_prev` (Shift+Tab), and `ui_left`, `ui_up`, `ui_right`, `ui_down` (matching arrow keys, D-pad buttons and signed left-stick axes). Controller defaults match any device. Their bindings are ordinary live action events and can be changed or erased through the public API. The root viewport consumes these actions for GUI focus navigation when a focused control leaves the event unhandled. `LoadFromProjectSettings` explicitly replaces the complete map from registered typed action definitions, after the project file has been loaded when required. It validates the candidate first, clears affected pressed contributions, and raises `ProjectSettingsLoaded` after commit.

Collection operations are lock-serialized and return snapshots. Binding resources remain caller-owned and mutable;
callers must not mutate or dispose a binding concurrently with matching. Action names use ordinal comparison.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
InputMap map = InputMap.Instance;
// Load registered project actions during setup, after loading the project settings file.
map.LoadFromProjectSettings();
map.AddAction("jump");
map.ActionAddEvent("jump", new InputEventKey { Keycode = Key.Space });
```

## Properties

| Member | Description |
| --- | --- |
| [`public static InputMap Instance { get; }`](#p-electron2d-inputmap-instance) | Gets the process-wide action map. |

## Methods

| Member | Description |
| --- | --- |
| [`public bool HasAction(string action)`](#m-electron2d-inputmap-hasaction-system-string) | Gets whether an action exists. |
| [`public void LoadFromProjectSettings()`](#m-electron2d-inputmap-loadfromprojectsettings) | Replaces all actions from registered typed project settings. |
| [`public IReadOnlyList<string> GetActions()`](#m-electron2d-inputmap-getactions) | Gets action names in registration order. |
| [`public void AddAction(string action, float deadzone = 0.2f)`](#m-electron2d-inputmap-addaction-system-string-system-single) | Adds an empty action. |
| [`public void EraseAction(string action)`](#m-electron2d-inputmap-eraseaction-system-string) | Removes an action and all of its bindings. |
| [`public float ActionGetDeadzone(string action)`](#m-electron2d-inputmap-actiongetdeadzone-system-string) | Gets an action's analog deadzone. |
| [`public void ActionSetDeadzone(string action, float deadzone)`](#m-electron2d-inputmap-actionsetdeadzone-system-string-system-single) | Changes an action's analog deadzone. |
| [`public void ActionAddEvent(string action, InputEvent event)`](#m-electron2d-inputmap-actionaddevent-system-string-electron2d-inputevent) | Adds an event binding to an action. |
| [`public bool ActionHasEvent(string action, InputEvent event)`](#m-electron2d-inputmap-actionhasevent-system-string-electron2d-inputevent) | Gets whether an action contains an exact event binding. |
| [`public void ActionEraseEvent(string action, InputEvent event)`](#m-electron2d-inputmap-actioneraseevent-system-string-electron2d-inputevent) | Removes the first exact matching binding from an action, if present. |
| [`public void ActionEraseEvents(string action)`](#m-electron2d-inputmap-actioneraseevents-system-string) | Removes every binding from an action. |
| [`public IReadOnlyList<InputEvent> ActionGetEvents(string action)`](#m-electron2d-inputmap-actiongetevents-system-string) | Gets an action's bindings in registration order. |
| [`public bool EventIsAction(InputEvent event, string action, bool exactMatch = false)`](#m-electron2d-inputmap-eventisaction-electron2d-inputevent-system-string-system-boolean) | Tests whether an event belongs to an action. |
| [`public string GetActionDescription(string action)`](#m-electron2d-inputmap-getactiondescription-system-string) | Gets a human-readable disjunction of an action's concrete bindings. |
| [`protected override void ValidateDisposal()`](#m-electron2d-inputmap-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action? ProjectSettingsLoaded`](#e-electron2d-inputmap-projectsettingsloaded) | Raised after a successful map replacement. |

## Constants

| Member | Description |
| --- | --- |

## Property Descriptions

<a id="p-electron2d-inputmap-instance"></a>
### `public static InputMap Instance { get; }`

Gets the process-wide action map.

**Value:** The same non-disposable instance for the lifetime of the process.

## Method Descriptions

<a id="m-electron2d-inputmap-loadfromprojectsettings"></a>
### `public void LoadFromProjectSettings()`

Reads registered `ProjectSetting<InputActionSettings>` definitions named `input/<action>` from `ProjectSettings.Instance`, applying active feature overrides. The six built-in UI actions are always registered; other actions require explicit typed registration. The version-one schema stores a finite deadzone and up to 32 ordered bindings per action. Duplicate exact bindings collapse after the 32-record limit is checked. A key location alone is invalid because it cannot identify a key. The replacement is prepared before the map changes, then installed under the map lock. Failure during preparation preserves bindings, pressed state and notification count. Every old or new action contribution is invalidated before the loaded event; a subscriber exception propagates after commit. This setup operation allocates. Loaded events are borrowed live references; previously obtained event references stay usable but are detached after a later reload.

**Exceptions**

- `InvalidDataException`: Wrong setting type, unsupported schema version, invalid action name or binding.
- `ObjectDisposedException`: The project settings registry has been disposed.

<a id="m-electron2d-inputmap-hasaction-system-string"></a>
### `public bool HasAction(string action)`

Gets whether an action exists.

**Parameters**

- `action`: The nonblank, case-sensitive action name.

**Returns:** `true` when registered.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.

<a id="m-electron2d-inputmap-getactions"></a>
### `public IReadOnlyList<string> GetActions()`

Gets action names in registration order.

**Returns:** An immutable snapshot.

<a id="m-electron2d-inputmap-addaction-system-string-system-single"></a>
### `public void AddAction(string action, float deadzone = 0.2f)`

Adds an empty action.

**Parameters**

- `action`: The nonblank, case-sensitive action name.
- `deadzone`: The analog threshold from zero through one.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `ArgumentOutOfRangeException`: `deadzone` is outside zero through one, NaN, or infinite.
- `InvalidOperationException`: The action already exists.

<a id="m-electron2d-inputmap-eraseaction-system-string"></a>
### `public void EraseAction(string action)`

Removes an action and all of its bindings.

**Parameters**

- `action`: The registered action name.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-inputmap-actiongetdeadzone-system-string"></a>
### `public float ActionGetDeadzone(string action)`

Gets an action's analog deadzone.

**Parameters**

- `action`: The registered action name.

**Returns:** A value from zero through one.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-inputmap-actionsetdeadzone-system-string-system-single"></a>
### `public void ActionSetDeadzone(string action, float deadzone)`

Changes an action's analog deadzone.

**Parameters**

- `action`: The registered action name.
- `deadzone`: The new threshold from zero through one.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `ArgumentOutOfRangeException`: `deadzone` is outside zero through one, NaN, or infinite.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-inputmap-actionaddevent-system-string-electron2d-inputevent"></a>
### `public void ActionAddEvent(string action, InputEvent event)`

Adds an event binding to an action.

**Parameters**

- `action`: The registered action name.
- `event`: A live action-compatible event describing the binding.

**Exceptions**

- `ArgumentException`: `action` is invalid or `event` is not action-compatible.
- `ArgumentNullException`: `event` or `action` is `null`.
- `InvalidOperationException`: The action already has 32 distinct bindings.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: `event` is disposing or disposed.

**Remarks:** An equal exact action binding is ignored. At most 32 bindings may belong to one action. Exact binding lookup uses action matching, so a synthetic action binding and a physical event stay distinct even when the synthetic event's public `IsMatch` returns `true` for that physical source.

<a id="m-electron2d-inputmap-actionhasevent-system-string-electron2d-inputevent"></a>
### `public bool ActionHasEvent(string action, InputEvent event)`

Gets whether an action contains an exact event binding.

**Parameters**

- `action`: The registered action name.
- `event`: The binding configuration to find.

**Returns:** `true` when an exact action binding exists.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `event` or `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: `event` is disposing or disposed.

<a id="m-electron2d-inputmap-actioneraseevent-system-string-electron2d-inputevent"></a>
### `public void ActionEraseEvent(string action, InputEvent event)`

Removes the first exact matching binding from an action, if present. An absent binding leaves the action and its pressed state unchanged; removing one invalidates cached action contributions. Existing binding snapshots retain their original event references.

**Parameters**

- `action`: The registered action name.
- `event`: The binding configuration to remove.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `event` or `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: `event` is disposing or disposed.

<a id="m-electron2d-inputmap-actioneraseevents-system-string"></a>
### `public void ActionEraseEvents(string action)`

Removes every binding from an action.

**Parameters**

- `action`: The registered action name.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-inputmap-actiongetevents-system-string"></a>
### `public IReadOnlyList<InputEvent> ActionGetEvents(string action)`

Gets an action's bindings in registration order.

**Parameters**

- `action`: The registered action name.

**Returns:** An immutable snapshot containing the original binding references.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.

<a id="m-electron2d-inputmap-eventisaction-electron2d-inputevent-system-string-system-boolean"></a>
### `public bool EventIsAction(InputEvent event, string action, bool exactMatch = false)`

Tests whether an event belongs to a registered action. Bindings are checked in registration order; the first matching binding supplies the effective press and strength.

**Parameters**

- `event`: The live event to test.
- `action`: The registered action name.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the event matches the action.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `event` or `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: `event` or a tested binding is disposing or disposed.

<a id="m-electron2d-inputmap-getactiondescription-system-string"></a>
### `public string GetActionDescription(string action)`

Gets a human-readable disjunction of an action's concrete bindings.

**Parameters**

- `action`: The registered action name.

**Returns:** A localized no-input message, or concrete binding descriptions joined by a localized ` or ` separator.

**Exceptions**

- `ArgumentException`: `action` is empty or whitespace.
- `ArgumentNullException`: `action` is `null`.
- `Collections.Generic.KeyNotFoundException`: The action is not registered.
- `ObjectDisposedException`: A binding is disposing or disposed.

**Remarks:** Synthetic [`InputEventAction`](InputEventAction.md) bindings are indirection and are omitted. The map's inherited translation domain and enabled flag control the no-input message and separator; each concrete event supplies its own description.

<a id="m-electron2d-inputmap-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Exceptions**

- `InvalidOperationException`: Always thrown because the singleton has process lifetime.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

The process-wide action map cannot be disposed.

## Event Descriptions

<a id="e-electron2d-inputmap-projectsettingsloaded"></a>
### `public event Action? ProjectSettingsLoaded`

Raised synchronously outside the map lock after successful replacement and pressed-state invalidation. It is not raised on validation failure. A throwing subscriber sees committed state.

## Constant Descriptions

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Lifecycle, errors, threading, and interactions

Configuration operations are serialized by one lock and return snapshots. Duplicate exact bindings are ignored; missing actions and invalid values throw before mutation. A successful map mutation or internal binding-change notification clears every affected action's cached runtime contributions through `Input` before fallible public `Resource.Changed` handlers run. Copying stored state into a registered binding follows the same ordering. Binding disposal removes it and clears contributions before public disposal handlers run. Typed action definitions persist through `ProjectSettings`; loading them into the live map is explicit.

Tests cover ordering, validation, deadzones, the 32-source ceiling, duplicates, exact modifiers, matching, failure-safe action-state invalidation, singleton lifetime, concurrent-safe snapshots, project-file round-trip, atomic reload/rollback, and warmed successful matching allocation. Native controller mapping databases are a distinct SDL gamepad trigger and are not represented here.
