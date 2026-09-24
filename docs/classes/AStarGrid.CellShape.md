# AStarGrid.CellShape

Last updated: 2026-09-24

**Owner:** [AStarGrid](AStarGrid.md) · **Source:** [AStarGrid.cs](../../src/Navigation/2D/AStarGrid.cs)

| Value | Integer | Cell position mapping |
| --- | ---: | --- |
| `Square` | 0 | Axis-aligned `ID * CellSize + Offset` |
| `IsometricRight` | 1 | Right-facing half-cell transform |
| `IsometricDown` | 2 | Down-facing half-cell transform |
| `Max` | 3 | Exclusive enum bound; rejected as a shape |

Changing [`Shape`](AStarGrid.md#properties) marks the grid dirty. Call `Update()` to rebuild world positions.
