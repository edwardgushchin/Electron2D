# AStarGrid.Heuristic

Last updated: 2026-09-24

**Owner:** [AStarGrid](AStarGrid.md) · **Source:** [AStarGrid.cs](../../src/Navigation/2D/AStarGrid.cs)

| Value | Integer | ID-space distance |
| --- | ---: | --- |
| `Euclidean` | 0 | Straight-line distance; default for cost and estimate |
| `Manhattan` | 1 | Horizontal plus vertical distance |
| `Octile` | 2 | Maximum distance plus the diagonal correction on the minimum |
| `Chebyshev` | 3 | Maximum horizontal or vertical distance |
| `Max` | 4 | Exclusive enum bound; rejected as a heuristic |

[`DefaultComputeHeuristic`](AStarGrid.md#properties) and [`DefaultEstimateHeuristic`](AStarGrid.md#properties) are independent. Protected typed hooks override their respective defaults during path search.
