# TileOffsetAxis

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Source:** [TileOffsetAxis.cs](../../src/Scene/Resources/TileOffsetAxis.cs).

## Overview

Selects the axis along which alternating cells are offset.

## Syntax

```csharp
public enum TileOffsetAxis
```

## Values

| Member | Value | Meaning |
| --- | --- | --- |
| `Horizontal` | 0 | Rows are offset horizontally. |
| `Vertical` | 1 | Columns are offset vertically. |

See [grid placement and neighbors](../components/tiles.md#grid-placement-and-neighbors) for use with TileSet and TileMapLayer. Geometry settings are stored; neighbor availability depends on the active shape and axis.
