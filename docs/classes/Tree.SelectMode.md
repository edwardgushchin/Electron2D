# Tree.SelectMode

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.Tree.SelectMode`. **Source:** [source](../../src/Scene/GUI/Tree.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

## Description

Single = 0 selects one cell, Row = 1 selects the complete selectable row, and Multi = 2 keeps independently selected cells and a cursor. Single is the Tree default. ItemList selection has a different numeric/behavioral contract and keeps its own enum identity.

## Enumeration values

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.Tree.SelectMode Multi = 2` | Allows multiple selected cells. |
| `public const Electron2D.Tree.SelectMode Row = 1` | Selects an entire row. |
| `public const Electron2D.Tree.SelectMode Single = 0` | Selects a single cell. |

## Enumeration values descriptions

<a id="member-e1e73960f853"></a>

### Multi

`public const Electron2D.Tree.SelectMode Multi = 2`

Allows multiple selected cells.

<a id="member-c5c89775f187"></a>

### Row

`public const Electron2D.Tree.SelectMode Row = 1`

Selects an entire row.

<a id="member-6c74e8f3593c"></a>

### Single

`public const Electron2D.Tree.SelectMode Single = 0`

Selects a single cell.

## Lifecycle, invariants and verification

Attached operations require the owning scene thread and a live item/control. Invalid indices, invalid enum values, nonfinite required configuration and ownership/cycle violations throw before mutation. Item/metadata/resource lifetimes follow the description above. See [hierarchical cells](../components/hierarchical-cells.md) for runtime flow, exercised editor and native workflows, allocation boundaries and dependency limits; [Tree coverage](../coverage/classes/Tree.md) and [TreeItem coverage](../coverage/classes/TreeItem.md) account for the applicable comparison surface. Relevant decisions: ADRs 0004, 0008, 0014, 0023, 0038, 0046, 0051 and 0083.
