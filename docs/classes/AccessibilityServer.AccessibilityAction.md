# AccessibilityServer.AccessibilityAction

Last updated: 2026-10-26

- Declaration: `public enum AccessibilityServer.AccessibilityAction`
- Source: [AccessibilityServer.Enums.cs](../../src/Servers/Accessibility/AccessibilityServer.Enums.cs)
- Owner: [AccessibilityServer](AccessibilityServer.md)

## Description

Typed compiled actions run on the display owner. Click activates the ordinary button path; Focus and Blur reuse GUI focus. This is the executable first action subset; numeric SetValue is available through Action<double>; directed text selection and replacement use typed callbacks; typed unit/hint/point/offset scrolling has real native consumers; remaining value/color/custom/popup payload families retain their dependencies.

## Values

| Value | Number | Meaning |
| --- | ---: | --- |
| <a id="click"></a>`Click` | 0 | Activates the element. |
| <a id="focus"></a>`Focus` | 1 | Requests focus. |
| <a id="blur"></a>`Blur` | 2 | Releases focus. |
| <a id="decrement"></a>`Decrement` | 5 | No-argument numeric decrement. |
| <a id="increment"></a>`Increment` | 6 | No-argument numeric increment. |
| <a id="settextselection"></a>`SetTextSelection` | 9 | Anchor/focus line RIDs and scalar offsets via a four-argument callback. |
| <a id="replaceselectedtext"></a>`ReplaceSelectedText` | 10 | Selected-text replacement via a string callback. |
| <a id="scrollbackward"></a>`ScrollBackward` | 11 | No-argument sequential backward request. |
| <a id="scrolldown"></a>`ScrollDown` | 12 | Item/page downward request. |
| <a id="scrollforward"></a>`ScrollForward` | 13 | No-argument sequential forward request. |
| <a id="scrollleft"></a>`ScrollLeft` | 14 | Item/page leftward request. |
| <a id="scrollright"></a>`ScrollRight` | 15 | Item/page rightward request. |
| <a id="scrollup"></a>`ScrollUp` | 16 | Item/page upward request. |
| <a id="scrollintoview"></a>`ScrollIntoView` | 17 | Reveal request with a typed preferred edge. |
| <a id="scrolltopoint"></a>`ScrollToPoint` | 18 | Target placement at a window-client point. |
| <a id="setscrolloffset"></a>`SetScrollOffset` | 19 | Absolute offset in local scroll coordinates. |
| <a id="setvalue"></a>`SetValue` | 20 | Numeric request via Action<double>; other payload families remain incomplete. |

Use `AccessibilityServer.UpdateAddAction(id, AccessibilityServer.AccessibilityAction.Click, Activate)` in an active supported display owner. Invalid numeric selectors reject. Element lifetime and native availability follow the owning service.

[Native scrolling](AccessibilityServer.md#native-scrolling) defines coordinate spaces, source unit projection, endpoint actions, clipping and owner/lifetime rules.
