# AccessibilityServer.AccessibilityPopupType

Last updated: 2026-10-26

- Declaration: `public enum AccessibilityServer.AccessibilityPopupType`
- Source: [AccessibilityServer.Live.cs](../../src/Servers/Accessibility/AccessibilityServer.Live.cs)
- Owner: [AccessibilityServer](AccessibilityServer.md#native-live-and-popups)

## Description

Kind of content opened by a producer-owned popup action. Values are not flags; unknown values reject.

## Values

| Value | Number | Meaning |
| --- | ---: | --- |
| <a id="menu"></a>`Menu` | 0 | Command menu. |
| <a id="list"></a>`List` | 1 | Selection list. |
| <a id="tree"></a>`Tree` | 2 | Hierarchical content. |
| <a id="dialog"></a>`Dialog` | 3 | Dialog content. |
