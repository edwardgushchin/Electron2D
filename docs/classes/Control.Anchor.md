# Control.Anchor

Last updated: 2026-11-14

**Inherits:** System.Enum · **Inherited By:** —

**Declaration:** `public enum Anchor` nested in [Control](Control.md) · **Source:** [Control.Anchor.cs](../../src/Scene/GUI/Control.Anchor.cs)

## Description

Selects the beginning or end of either parent axis by casting the enum value to `float` and passing it to `Control.SetAnchor`. The `Side` argument selects the rectangle edge to edit. The named endpoint uses the same offset retention, crossing and reflow rules as a fractional anchor.

## Example

```csharp
panel.SetAnchor(Side.Right, (float)Control.Anchor.End, keepOffset: true);
panel.SetOffset(Side.Right, -12);
```

## Enumeration descriptions

| Value | Behavior |
| --- | --- |
| `Begin = 0` | Uses fraction zero of the selected parent axis. |
| `End = 1` | Uses fraction one of the selected parent axis. |

[ControlLayoutTests](../../tests/Electron2D.Tests/ControlLayoutTests.Sides.cs) checks the numeric identities, and live geometry.
