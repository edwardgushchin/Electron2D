# GraphEdit.GridPatternMode

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.GraphEdit.GridPatternMode`. **Source:** [source](../../src/Scene/GUI/GraphEdit.cs). **Component:** [Graph authoring](../components/graph-authoring.md).

## Description

Owner-specific Lines/Dots domain. The Mode suffix avoids the GridPattern property collision under ADR 0051; it is not shared with unrelated grid domains.

## Members

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.GraphEdit.GridPatternMode Dots = 1`](#member-ce5e38918537) | Draws grid dots. |
| [`public const Electron2D.GraphEdit.GridPatternMode Lines = 0`](#member-252432f729fa) | Draws grid lines. |

## Example

```csharp
using var graph = new GraphEdit { GridPattern = GraphEdit.GridPatternMode.Dots };
```

## Lifetime and verification

Attached reads and mutations use the scene owner thread. Own mutations reject active port/connection drawing; borrowed resources remain caller-owned. GraphNode and GraphFrame titlebars and title labels, and GraphEdit menu/layers cannot be disposed independently. GraphElement/GraphEdit disposal releases owned children and event subscriptions. See the component for executable checks, preparation boundaries and remaining dependencies.

## Member details

<a id="member-ce5e38918537"></a>
### `Dots`

```csharp
public const Electron2D.GraphEdit.GridPatternMode Dots = 1
```

Draws grid dots.

<a id="member-252432f729fa"></a>
### `Lines`

```csharp
public const Electron2D.GraphEdit.GridPatternMode Lines = 0
```

Draws grid lines.
