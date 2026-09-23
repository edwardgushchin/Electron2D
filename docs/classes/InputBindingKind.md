# InputBindingKind

Last updated: 2026-09-24

**Inherits:** `System.Enum`

**Source:** [`src/Core/Input/InputActionSettings.cs`](../../src/Core/Input/InputActionSettings.cs)

**Namespace:** `Electron2D`

**Declaration:** `public enum InputBindingKind`

## Description

Selects the concrete action-compatible [`InputEvent`](InputEvent.md) represented by an [`InputBindingSettings`](InputBindingSettings.md) record. Unknown numeric values fail when the project action map loads. The enum is a serialized discriminator; changing its values requires a schema migration under [ADR 0038](../decisions/input.md#adr-0038).

## Example

```csharp
var binding = new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Space };
```

## Enumeration descriptions

| Value | Meaning |
| --- | --- |
| `Key = 0` | Keyboard key and modifiers. |
| `MouseButton = 1` | Mouse button or wheel direction and modifiers. |
| `JoypadButton = 2` | Controller button, optionally on a specified device. |
| `JoypadMotion = 3` | Signed controller-axis direction. |
| `Action = 4` | Named synthetic action binding. |

Invalid kind and field combinations are rejected before [`InputMap`](InputMap.md) replaces its live map. [`InputActionSettingsTests`](../../tests/Electron2D.Tests/InputActionSettingsTests.cs) checks each listed event family in managed code.
