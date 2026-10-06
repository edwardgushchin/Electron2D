# TabBar.AlignmentMode

Last updated: 2026-10-06

**Declaration:** `public enum Electron2D.TabBar.AlignmentMode`. **Source:** [TabBar.cs](../../src/Scene/GUI/TabBar.cs). **Owner:** [TabBar](TabBar.md).

## Description

Selects alignment within the available tab strip.

Max is an exclusive validation sentinel, rejected by the corresponding property setter. Values/defaults apply to TabBar; no extra native type is exposed.

## Value summary

| Complete declaration | Meaning |
| --- | --- |
| `public const Electron2D.TabBar.AlignmentMode Center = 1` | Center the visible tabs. |
| `public const Electron2D.TabBar.AlignmentMode Left = 0` | Align to the logical leading edge. |
| `public const Electron2D.TabBar.AlignmentMode Max = 3` | Exclusive upper bound. |
| `public const Electron2D.TabBar.AlignmentMode Right = 2` | Align to the logical trailing edge. |

## Example

```csharp
var tabs = new TabBar { TabAlignment = TabBar.AlignmentMode.Center };
```

These enum identities and invalid Max setters are exercised by TabBarTests. See the owner for lifecycle, drawing, persistence and current verification limits.
