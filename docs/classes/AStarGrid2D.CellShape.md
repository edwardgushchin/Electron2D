# AStarGrid2D.CellShape

Last updated: 2026-09-24

**Owner:** [AStarGrid2D](AStarGrid2D.md) · **Source:** [AStarGrid2D.cs](../../src/Navigation/2D/AStarGrid2D.cs)

| Value | Integer | Cell position mapping |
| --- | ---: | --- |
| `Square` | 0 | Axis-aligned `ID * CellSize + Offset` |
| `IsometricRight` | 1 | Right-facing half-cell transform |
| `IsometricDown` | 2 | Down-facing half-cell transform |
| `Max` | 3 | Exclusive enum bound; rejected as a shape |

Changing [`Shape`](AStarGrid2D.md#properties) marks the grid dirty. Call `Update()` to rebuild world positions.
