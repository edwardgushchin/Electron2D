# TileShape

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Source:** [TileShape.cs](../../src/Scene/Resources/TileShape.cs).

## Overview

Selects the grid cell outline.

## Syntax

```csharp
public enum TileShape
```

## Values

| Member | Value | Meaning |
| --- | --- | --- |
| `Square` | 0 | Axis-aligned square cells. |
| `Isometric` | 1 | Diamond cells with half overlap. |
| `HalfOffsetSquare` | 2 | Rectangular cells in alternating offset rows or columns. |
| `Hexagon` | 3 | Six-sided cells with one-quarter overlap. |

See [grid placement and neighbors](../components/tiles.md#grid-placement-and-neighbors) for use with TileSet and TileMapLayer. Geometry settings are stored; neighbor availability depends on the active shape and axis.
