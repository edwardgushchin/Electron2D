# TabBar.CloseButtonDisplayPolicy

Last updated: 2026-10-06

**Declaration:** `public enum Electron2D.TabBar.CloseButtonDisplayPolicy`. **Source:** [TabBar.cs](../../src/Scene/GUI/TabBar.cs). **Owner:** [TabBar](TabBar.md).

## Description

Selects which tabs display a close-request button.

Max is an exclusive validation sentinel, rejected by the corresponding property setter. Values/defaults apply to TabBar; no extra native type is exposed.

## Value summary

| Complete declaration | Meaning |
| --- | --- |
| `public const Electron2D.TabBar.CloseButtonDisplayPolicy Max = 3` | Exclusive upper bound. |
| `public const Electron2D.TabBar.CloseButtonDisplayPolicy ShowActiveOnly = 1` | Show a close button on the current tab. |
| `public const Electron2D.TabBar.CloseButtonDisplayPolicy ShowAlways = 2` | Show a close button on every visible tab. |
| `public const Electron2D.TabBar.CloseButtonDisplayPolicy ShowNever = 0` | Show no close buttons. |

## Example

```csharp
var tabs = new TabBar { TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.ShowAlways };
```

These enum identities and invalid Max setters are exercised by TabBarTests. See the owner for lifecycle, drawing, persistence and current verification limits.
