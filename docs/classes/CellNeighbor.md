# CellNeighbor

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Source:** [CellNeighbor.cs](../../src/Scene/Resources/CellNeighbor.cs).

## Overview

Names clockwise cell edge and corner directions; available directions depend on grid shape.

## Syntax

```csharp
public enum CellNeighbor
```

## Values

| Member | Value | Meaning |
| --- | --- | --- |
| `RightSide` | 0 | Right edge. |
| `RightCorner` | 1 | Right corner. |
| `BottomRightSide` | 2 | Bottom right edge. |
| `BottomRightCorner` | 3 | Bottom right corner. |
| `BottomSide` | 4 | Bottom edge. |
| `BottomCorner` | 5 | Bottom corner. |
| `BottomLeftSide` | 6 | Bottom left edge. |
| `BottomLeftCorner` | 7 | Bottom left corner. |
| `LeftSide` | 8 | Left edge. |
| `LeftCorner` | 9 | Left corner. |
| `TopLeftSide` | 10 | Top left edge. |
| `TopLeftCorner` | 11 | Top left corner. |
| `TopSide` | 12 | Top edge. |
| `TopCorner` | 13 | Top corner. |
| `TopRightSide` | 14 | Top right edge. |
| `TopRightCorner` | 15 | Top right corner. |

See [grid placement and neighbors](../components/tiles.md#grid-placement-and-neighbors) for use with TileSet and TileMapLayer. Geometry settings are stored; neighbor availability depends on the active shape and axis.
