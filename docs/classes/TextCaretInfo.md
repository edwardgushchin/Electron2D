# TextCaretInfo

Last updated: 2026-10-19

- **Source:** [TextCaretInfo.cs](../../src/Servers/Text/TextCaretInfo.cs)
- **Declaration:** `public readonly struct TextCaretInfo`

An immutable result returned by TextServer.ShapedTextGetCarets. LeadingRect/LeadingDirection describe the preceding shaped cluster; TrailingRect/TrailingDirection describe the following cluster. Positions are relative to the buffer baseline. Signed advance sizes preserve LTR/RTL; the cross size is half the line height at internal cluster edges and the full height at a buffer edge or inside a ligature. Inside-cluster directions are Auto. Missing sides have zero rectangles and Auto directions. Vertical shapes exchange advance/cross axes coherently. The result owns no resources and may be copied; producing it still requires a live owner-thread buffer. See [editing behavior](../components/text-server.md#caret-selection-and-navigation).

## Properties

| Property | Type | Contract |
| --- | --- | --- |
| LeadingRect | Rect2 | Baseline-relative preceding-cluster caret. |
| LeadingDirection | TextDirection | Preceding cluster direction, or Auto. |
| TrailingRect | Rect2 | Baseline-relative following-cluster caret. |
| TrailingDirection | TextDirection | Following cluster direction, or Auto. |
