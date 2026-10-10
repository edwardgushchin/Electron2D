# TerrainMode

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Source:** [TerrainMode.cs](../../src/Scene/Resources/TerrainMode.cs).

## Overview

Selects the matching parts of cells during terrain painting. TileSet stores one mode per terrain set. TileData uses that mode, the grid shape and offset axis to report available peering bits.

## Syntax

```csharp
public enum TerrainMode
```

## Values

| Member | Value | Meaning |
| --- | --- | --- |
| MatchCornersAndSides | 0 | Match corners and sides; the default. |
| MatchCorners | 1 | Match shared corner values. |
| MatchSides | 2 | Match shared side values. |

```csharp
set.SetTerrainSetMode(0, TerrainMode.MatchSides);
layer.SetCellsTerrainConnect([new(0, 0), new(1, 0)], 0, 0);
```

The example assumes a live TileSet with terrain set zero, terrain zero and matching atlas alternatives, assigned to a TileMapLayer. See [terrain authoring and painting](../components/tiles.md#terrain-authoring-and-painting) for the complete workflow, validation and verified limits.
