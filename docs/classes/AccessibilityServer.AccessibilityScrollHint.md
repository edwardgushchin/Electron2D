# AccessibilityServer.AccessibilityScrollHint

Last updated: 2026-10-26

- Declaration: `public enum AccessibilityServer.AccessibilityScrollHint`
- Source: [AccessibilityServer.Enums.cs](../../src/Servers/Accessibility/AccessibilityServer.Enums.cs)
- Owner: [AccessibilityServer](AccessibilityServer.md)

## Description

Suggests where the target should appear inside a scrollable viewport. The unpreferred axis is exposed with the smallest useful movement. Native anywhere placement is adapted by the bridge; it is not an additional enum value. Callback selection and request lifetime follow [native scrolling](AccessibilityServer.md#native-scrolling). These values are not flags.

## Values

| Value | Number | Meaning |
| --- | ---: | --- |
| <a id="topleft"></a>`TopLeft` | 0 | Align top and left. |
| <a id="bottomright"></a>`BottomRight` | 1 | Align bottom and right. |
| <a id="topedge"></a>`TopEdge` | 2 | Prefer the top edge. |
| <a id="bottomedge"></a>`BottomEdge` | 3 | Prefer the bottom edge. |
| <a id="leftedge"></a>`LeftEdge` | 4 | Prefer the left edge. |
| <a id="rightedge"></a>`RightEdge` | 5 | Prefer the right edge. |

[The executable public scene](../../tests/Electron2D.Tests/AccessibilityScrollTests.cs) registers typed callbacks and verifies requests with an external native client.
