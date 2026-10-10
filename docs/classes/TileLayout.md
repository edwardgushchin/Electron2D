# TileLayout

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Source:** [TileLayout.cs](../../src/Scene/Resources/TileLayout.cs).

## Overview

Selects the coordinate system for offset grids.

## Syntax

```csharp
public enum TileLayout
```

## Values

| Member | Value | Meaning |
| --- | --- | --- |
| `Stacked` | 0 | Alternating rows or columns shift by half a cell. |
| `StackedOffset` | 1 | Alternating offsets start on the opposite parity. |
| `StairsRight` | 2 | Coordinates advance along a staircase toward the right. |
| `StairsDown` | 3 | Coordinates advance along a staircase downward. |
| `DiamondRight` | 4 | Coordinates advance along diamond axes toward the right. |
| `DiamondDown` | 5 | Coordinates advance along diamond axes downward. |

See [grid placement and neighbors](../components/tiles.md#grid-placement-and-neighbors) for use with TileSet and TileMapLayer. Geometry settings are stored; neighbor availability depends on the active shape and axis.
