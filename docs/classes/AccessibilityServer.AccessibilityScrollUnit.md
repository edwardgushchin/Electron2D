# AccessibilityServer.AccessibilityScrollUnit

Last updated: 2026-10-26

- Declaration: `public enum AccessibilityServer.AccessibilityScrollUnit`
- Source: [AccessibilityServer.Enums.cs](../../src/Servers/Accessibility/AccessibilityServer.Enums.cs)
- Owner: [AccessibilityServer](AccessibilityServer.md)

## Description

Selects the amount for a directional scroll request. Item means one source row/tab/font or configured small step; Page means one current viewport. Native requests deliver defined values through the corresponding compiled callback. Callback selection and request lifetime follow [native scrolling](AccessibilityServer.md#native-scrolling). These values are not flags.

## Values

| Value | Number | Meaning |
| --- | ---: | --- |
| <a id="item"></a>`Item` | 0 | One source-defined small step. |
| <a id="page"></a>`Page` | 1 | One viewport page. |

[The executable public scene](../../tests/Electron2D.Tests/AccessibilityScrollTests.cs) registers typed callbacks and verifies requests with an external native client.
