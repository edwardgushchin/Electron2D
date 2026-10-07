# TreeItem.TreeCellMode

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.TreeItem.TreeCellMode`. **Source:** [source](../../src/Scene/GUI/TreeItem.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

## Description

String = 0 is the default shaped text mode; Check = 1 adds tri-state check presentation, Range = 2 selects numeric or text-choice editing, Icon = 3 centers a borrowed icon, and Custom = 4 invokes a typed draw callback and application editor interaction. Changing the mode resets its mode-dependent fields.

## Enumeration values

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.TreeItem.TreeCellMode Check = 1` | Text with a checkbox. |
| `public const Electron2D.TreeItem.TreeCellMode Custom = 4` | Typed custom drawing or popup interaction. |
| `public const Electron2D.TreeItem.TreeCellMode Icon = 3` | Borrowed icon. |
| `public const Electron2D.TreeItem.TreeCellMode Range = 2` | A numeric value or text-defined choice. |
| `public const Electron2D.TreeItem.TreeCellMode String = 0` | Shaped text, optionally editable. |

## Enumeration values descriptions

<a id="member-62b50e5730d6"></a>

### Check

`public const Electron2D.TreeItem.TreeCellMode Check = 1`

Text with a checkbox.

<a id="member-a47630606414"></a>

### Custom

`public const Electron2D.TreeItem.TreeCellMode Custom = 4`

Typed custom drawing or popup interaction.

<a id="member-fb6e3b3214b0"></a>

### Icon

`public const Electron2D.TreeItem.TreeCellMode Icon = 3`

Borrowed icon.

<a id="member-de20a03ccc6d"></a>

### Range

`public const Electron2D.TreeItem.TreeCellMode Range = 2`

A numeric value or text-defined choice.

<a id="member-68e6b351b65d"></a>

### String

`public const Electron2D.TreeItem.TreeCellMode String = 0`

Shaped text, optionally editable.

## Lifecycle, invariants and verification

Attached operations require the owning scene thread and a live item/control. Invalid indices, invalid enum values, nonfinite required configuration and ownership/cycle violations throw before mutation. Item/metadata/resource lifetimes follow the description above. See [hierarchical cells](../components/hierarchical-cells.md) for runtime flow, exercised editor and native workflows, allocation boundaries and dependency limits; [Tree coverage](../coverage/classes/Tree.md) and [TreeItem coverage](../coverage/classes/TreeItem.md) account for the applicable comparison surface. Relevant decisions: ADRs 0004, 0008, 0014, 0023, 0038, 0046, 0051 and 0083.
