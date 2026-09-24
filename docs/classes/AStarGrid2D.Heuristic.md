# AStarGrid2D.Heuristic

Last updated: 2026-09-24

**Owner:** [AStarGrid2D](AStarGrid2D.md) · **Source:** [AStarGrid2D.cs](../../src/Navigation/2D/AStarGrid2D.cs)

| Value | Integer | ID-space distance |
| --- | ---: | --- |
| `Euclidean` | 0 | Straight-line distance; default for cost and estimate |
| `Manhattan` | 1 | Horizontal plus vertical distance |
| `Octile` | 2 | Maximum distance plus the diagonal correction on the minimum |
| `Chebyshev` | 3 | Maximum horizontal or vertical distance |
| `Max` | 4 | Exclusive enum bound; rejected as a heuristic |

[`DefaultComputeHeuristic`](AStarGrid2D.md#properties) and [`DefaultEstimateHeuristic`](AStarGrid2D.md#properties) are independent. Protected typed hooks override their respective defaults during path search.
