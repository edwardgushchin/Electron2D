# NinePatchRect.AxisStretchMode

Last updated: 2026-09-26

**Owner:** [NinePatchRect](NinePatchRect.md) · **Source:** [NinePatchRect.cs](../../src/Scene/GUI/NinePatchRect.cs)

| Value | Integer | Center-axis mapping |
| --- | ---: | --- |
| `Stretch` | 0 | One source center mapped across the destination center. |
| `Tile` | 1 | Natural source pixel period, with a partial final tile when needed. |
| `TileFit` | 2 | Repeat count max(1, floor(destinationCenter/sourceCenter+0.5)); complete tiles scale to fit. |

Horizontal and vertical policies are independent; Stretch is the default. Corners preserve source pixel size and false DrawCenter omits only both-center pieces. Invalid values reject before state changes. [Managed/native tests](../../tests/Electron2D.Tests/NinePatchRenderingTests.cs) cover all nine combinations under [ADR 0079](../decisions/rendering.md#adr-0079), with the current finite CPU geometry ceiling documented on the owner page.
