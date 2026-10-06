# TreeDropModeFlags

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.TreeDropModeFlags`. **Source:** [source](../../src/Scene/GUI/Tree.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

## Description

Disabled = 0 suppresses drop feedback. OnItem = 1 permits the middle of an accepting row; Inbetween = 2 permits before/after presentation. Flags may combine. Drop sections are -1, 0, 1 or -100 when unavailable; this enum presents a target and does not accept application payloads by itself.

## Enumeration values

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.TreeDropModeFlags Disabled = 0` | Disables drop presentation. |
| `public const Electron2D.TreeDropModeFlags Inbetween = 2` | Allows dropping above or below an item. |
| `public const Electron2D.TreeDropModeFlags OnItem = 1` | Allows dropping onto an item. |

## Enumeration values descriptions

<a id="member-6838520b2ac9"></a>

### Disabled

`public const Electron2D.TreeDropModeFlags Disabled = 0`

Disables drop presentation.

<a id="member-cc1da3a9ab0f"></a>

### Inbetween

`public const Electron2D.TreeDropModeFlags Inbetween = 2`

Allows dropping above or below an item.

<a id="member-b81486468dc9"></a>

### OnItem

`public const Electron2D.TreeDropModeFlags OnItem = 1`

Allows dropping onto an item.

## Lifecycle, invariants and verification

Attached operations require the owning scene thread and a live item/control. Invalid indices, invalid enum values, nonfinite required configuration and ownership/cycle violations throw before mutation. Item/metadata/resource lifetimes follow the description above. See [hierarchical cells](../components/hierarchical-cells.md) for runtime flow, exercised editor and native workflows, allocation boundaries and dependency limits; [Tree coverage](../coverage/classes/Tree.md) and [TreeItem coverage](../coverage/classes/TreeItem.md) account for the applicable comparison surface. Relevant decisions: ADRs 0004, 0008, 0014, 0023, 0038, 0046, 0051 and 0083.
