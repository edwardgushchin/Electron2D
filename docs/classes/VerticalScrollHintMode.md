# VerticalScrollHintMode

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.VerticalScrollHintMode`. **Source:** [source](../../src/Scene/GUI/ItemList.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

## Description

Shared vertical hint identity for Tree and ItemList under ADR 0051. Disabled = 0 is the default; Both = 1, Top = 2 and Bottom = 3 choose reachable edges with hidden vertical content. ScrollContainer has a separate two-axis contract. Hints consume the owning widget theme and TileScrollHint.

## Enumeration values

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.VerticalScrollHintMode Both = 1` | Allows hints at both reachable vertical edges. |
| `public const Electron2D.VerticalScrollHintMode Bottom = 3` | Allows the bottom hint only. |
| `public const Electron2D.VerticalScrollHintMode Disabled = 0` | Hides directional hints. |
| `public const Electron2D.VerticalScrollHintMode Top = 2` | Allows the top hint only. |

## Enumeration values descriptions

<a id="member-cbe46c2a00e2"></a>

### Both

`public const Electron2D.VerticalScrollHintMode Both = 1`

Allows hints at both reachable vertical edges.

<a id="member-7b95daac82ce"></a>

### Bottom

`public const Electron2D.VerticalScrollHintMode Bottom = 3`

Allows the bottom hint only.

<a id="member-15172d09dd9d"></a>

### Disabled

`public const Electron2D.VerticalScrollHintMode Disabled = 0`

Hides directional hints.

<a id="member-abece884dba5"></a>

### Top

`public const Electron2D.VerticalScrollHintMode Top = 2`

Allows the top hint only.

## Lifecycle, invariants and verification

Attached operations require the owning scene thread and a live item/control. Invalid indices, invalid enum values, nonfinite required configuration and ownership/cycle violations throw before mutation. Item/metadata/resource lifetimes follow the description above. See [hierarchical cells](../components/hierarchical-cells.md) for runtime flow, exercised editor and native workflows, allocation boundaries and dependency limits; [Tree coverage](../coverage/classes/Tree.md) and [TreeItem coverage](../coverage/classes/TreeItem.md) account for the applicable comparison surface. Relevant decisions: ADRs 0004, 0008, 0014, 0023, 0038, 0046, 0051 and 0083.
