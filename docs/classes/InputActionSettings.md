# InputActionSettings

Last updated: 2026-10-04

**Inherits:** `object`

**Inherited By:** —

**Source:** [`src/Core/Input/InputActionSettings.cs`](../../src/Core/Input/InputActionSettings.cs)

**Namespace:** `Electron2D`

**Declaration:** `public sealed class InputActionSettings`

## Description

This typed, versioned value is stored by a `ProjectSetting<InputActionSettings>` named `input/<action>`. [`ProjectSettings`](ProjectSettings.md) serializes a snapshot; [`InputMap.LoadFromProjectSettings`](InputMap.md#m-electron2d-inputmap-loadfromprojectsettings) validates it and replaces the live action map. Version 1 is the only supported version. Property initialization itself does not validate; a failed load preserves the old map. No migration contract exists for a later version.

## Example

```csharp
var jump = new ProjectSetting<InputActionSettings>("input/jump", new InputActionSettings());
ProjectSettings.Register(jump);
ProjectSettings.Set(jump, new InputActionSettings
{
    Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Space }],
});
InputMap.LoadFromProjectSettings();
```

Call this during project setup outside input dispatch. Register before loading project values if the project file contains the key.

## Constructors

| Member | Description |
| --- | --- |
| [`public InputActionSettings()`](#constructor) | Creates an empty version-one action. |

<a id="constructor"></a>
### `public InputActionSettings()`

Creates an action with the defaults shown below.

## Properties

| Member | Default | Description |
| --- | --- | --- |
| [`public int Version { get; init; }`](#version) | `1` | Serialized schema version. |
| [`public float Deadzone { get; init; }`](#deadzone) | `0.2f` | Finite analog threshold in `[0, 1]`. |
| [`public InputBindingSettings[] Bindings { get; init; }`](#bindings) | Empty | Ordered bindings, at most 32 entries. |

## Property Descriptions

<a id="version"></a>
### `public int Version { get; init; }`

Only `1` loads; an unsupported value throws `InvalidDataException` before replacement.

<a id="deadzone"></a>
### `public float Deadzone { get; init; }`

The action's analog threshold. NaN, infinity, and values outside `[0, 1]` fail load with `InvalidDataException`.

<a id="bindings"></a>
### `public InputBindingSettings[] Bindings { get; init; }`

The ordered typed binding list. Null entries and more than 32 entries fail load; exact duplicates collapse while retaining the first. The registry persists a snapshot, so mutate through `Set` and reload explicitly to apply changes.

## Lifecycle and verification

[`InputActionSettingsTests`](../../tests/Electron2D.Tests/InputActionSettingsTests.cs) checks project-file round-trip, five binding families, unknown JSON member rejection, active feature overrides, public process-registry loading, 33-record rejection before duplicate collapse, late invalid-binding rollback and map replacement in managed code. The contract is defined by [ADR 0038](../decisions/input.md#adr-0038); native devices and other platforms are not validated here.
