# AccessibilityServer.AccessibilityLiveMode

Last updated: 2026-10-26

- Declaration: `public enum AccessibilityServer.AccessibilityLiveMode`
- Source: [AccessibilityServer.Live.cs](../../src/Servers/Accessibility/AccessibilityServer.Live.cs)
- Owner: [AccessibilityServer](AccessibilityServer.md#native-live-and-popups)

## Description

Presentation priority for live region changes. Values are not flags; unknown values reject.

## Values

| Value | Number | Meaning |
| --- | ---: | --- |
| <a id="off"></a>`Off` | 0 | Suppress announcements. |
| <a id="polite"></a>`Polite` | 1 | Announce at the next opportunity. |
| <a id="assertive"></a>`Assertive` | 2 | Announce with immediate priority. |
