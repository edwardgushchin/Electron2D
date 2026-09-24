# AStarGrid.DiagonalMode

Last updated: 2026-09-24

**Owner:** [AStarGrid](AStarGrid.md) · **Source:** [AStarGrid.cs](../../src/Navigation/2D/AStarGrid.cs)

| Value | Integer | Diagonal eligibility |
| --- | ---: | --- |
| `Always` | 0 | Destination need only be walkable |
| `Never` | 1 | Cardinal neighbors only |
| `AtLeastOneWalkable` | 2 | Either adjacent cardinal cell must be walkable |
| `OnlyIfNoObstacles` | 3 | Both adjacent cardinal cells must be walkable |
| `Max` | 4 | Exclusive enum bound; rejected as a mode |

The default [`Diagonals`](AStarGrid.md#properties) value is `Always`. Changing it affects the next path query without rebuilding cells.
