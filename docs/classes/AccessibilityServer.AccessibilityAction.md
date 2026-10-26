# AccessibilityServer.AccessibilityAction

Last updated: 2026-10-26

- Declaration: `public enum AccessibilityServer.AccessibilityAction`
- Source: [AccessibilityServer.Enums.cs](../../src/Servers/Accessibility/AccessibilityServer.Enums.cs)
- Owner: [AccessibilityServer](AccessibilityServer.md)

## Description

Typed no-argument compiled actions run on the display owner. Click activates the ordinary button path; Focus and Blur reuse GUI focus. This is the executable first action subset; value/text/selection/scroll/custom payload actions remain absent.

## Values

| Value | Number | Meaning |
| --- | ---: | --- |
| <a id="click"></a>`Click` | 0 | Activates the element. |
| <a id="focus"></a>`Focus` | 1 | Requests focus. |
| <a id="blur"></a>`Blur` | 2 | Releases focus. |

Use `AccessibilityServer.UpdateAddAction(id, AccessibilityServer.AccessibilityAction.Click, Activate)` in an active supported display owner. Invalid numeric selectors reject. Element lifetime and native availability follow the owning service.
