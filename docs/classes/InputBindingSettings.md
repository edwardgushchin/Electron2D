# InputBindingSettings

Last updated: 2026-09-24

**Inherits:** `object`

**Inherited By:** —

**Source:** [`src/Core/Input/InputActionSettings.cs`](../../src/Core/Input/InputActionSettings.cs)

**Namespace:** `Electron2D`

**Declaration:** `public sealed class InputBindingSettings`

## Description

A serializable, typed event identity within [`InputActionSettings`](InputActionSettings.md). `Kind` selects exactly one family of fields. Only identifying fields are stored; pressed state and callback state are not. `ProjectSettings` snapshots the value and [`InputMap.LoadFromProjectSettings`](InputMap.md#m-electron2d-inputmap-loadfromprojectsettings) validates every binding before replacing the live map. Unknown JSON members, invalid combinations, and invalid event values fail rather than being ignored. Version-one controller bindings keep button IDs `0..127`, axis IDs `0..9`, and finite nonzero axis directions in `[-1,1]`; caller-created controller events can retain broader raw values.

## Example

```csharp
var binding = new InputBindingSettings
{
    Kind = InputBindingKind.Key,
    Keycode = Key.Space,
    Modifiers = KeyModifierMask.Shift,
};
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputBindingSettings()`](#constructor) | Creates a key binding with default fields. |

<a id="constructor"></a>
### `public InputBindingSettings()`

Creates the binding with the defaults shown below.

## Properties

| Member | Default | Role |
| --- | --- | --- |
| [`public InputBindingKind Kind { get; init; }`](#kind) | `Key` | Concrete event family. |
| [`public int? Device { get; init; }`](#device) | `null` | Source device or event-family default. |
| [`public Key Keycode { get; init; }`](#keycode) | `None` | Logical key. |
| [`public Key PhysicalKeycode { get; init; }`](#physicalkeycode) | `None` | Physical key. |
| [`public Key KeyLabel { get; init; }`](#keylabel) | `None` | Localized key label. |
| [`public KeyLocation Location { get; init; }`](#location) | `Unspecified` | Physical key location. |
| [`public KeyModifierMask Modifiers { get; init; }`](#modifiers) | `0` | Key or mouse modifiers. |
| [`public MouseButton MouseButtonIndex { get; init; }`](#mousebuttonindex) | `None` | Mouse button or wheel direction. |
| [`public JoyButton JoyButtonIndex { get; init; }`](#joybuttonindex) | `A` | Controller button. |
| [`public JoyAxis JoyAxis { get; init; }`](#joyaxis) | `LeftX` | Controller axis. |
| [`public float AxisValue { get; init; }`](#axisvalue) | `0` | Signed controller-axis direction. |
| [`public string Action { get; init; }`](#action) | `""` | Named synthetic action. |

## Property descriptions

<a id="kind"></a>
### `public InputBindingKind Kind { get; init; }`

Selects the concrete event type. Unrecognized numeric values fail load.

<a id="device"></a>
### `public int? Device { get; init; }`

Null uses the event constructor's default. `-1` matches any controller for controller bindings; values below `-1` fail load.

<a id="keycode"></a>
### `public Key Keycode { get; init; }`

Logical key identity for `Key`; at least one of `Keycode`, `PhysicalKeycode`, or `KeyLabel` must identify a key.

<a id="physicalkeycode"></a>
### `public Key PhysicalKeycode { get; init; }`

Physical key identity for `Key`.

<a id="keylabel"></a>
### `public Key KeyLabel { get; init; }`

Current-layout key label for `Key`.

<a id="location"></a>
### `public KeyLocation Location { get; init; }`

Physical key location for `Key`; an undefined value fails load.
A location without `Keycode`, `PhysicalKeycode`, or `KeyLabel` also fails load because it cannot identify a key.

<a id="modifiers"></a>
### `public KeyModifierMask Modifiers { get; init; }`

Allowed on `Key` and `MouseButton`: Shift, Alt, Control, Meta, or portable CommandOrControl. CommandOrControl cannot be combined with explicit Control or Meta.

<a id="mousebuttonindex"></a>
### `public MouseButton MouseButtonIndex { get; init; }`

Button or wheel direction for `MouseButton`; `None` fails load.

<a id="joybuttonindex"></a>
### `public JoyButton JoyButtonIndex { get; init; }`

Controller button for `JoypadButton`; `A` is a valid default. Version-one loading accepts raw IDs `0..127`.

<a id="joyaxis"></a>
### `public JoyAxis JoyAxis { get; init; }`

Controller axis for `JoypadMotion`; version-one loading accepts raw IDs `0..9`.

<a id="axisvalue"></a>
### `public float AxisValue { get; init; }`

Finite nonzero signed direction in `[-1,1]` for `JoypadMotion`; version-one loading validates this separately from the event's broader raw-value storage.

<a id="action"></a>
### `public string Action { get; init; }`

Nonblank ordinal name for `Action`.

## Lifecycle and verification

The value is serialized by the typed project setting; it is not itself a live input event. A later map reload creates new borrowed event resources. [`InputActionSettingsTests`](../../tests/Electron2D.Tests/InputActionSettingsTests.cs) checks five event-family round trips, duplicate collapse, rejection of an unidentified key after a valid candidate, invalid binding rollback, feature overrides and old binding detachment. See [ADR 0038](../decisions/input.md#adr-0038) for schema versioning.
