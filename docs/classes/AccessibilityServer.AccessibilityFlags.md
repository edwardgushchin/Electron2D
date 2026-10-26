# AccessibilityServer.AccessibilityFlags

Last updated: 2026-10-26

- Declaration: `public enum AccessibilityServer.AccessibilityFlags`
- Source: [AccessibilityServer.Enums.cs](../../src/Servers/Accessibility/AccessibilityServer.Enums.cs)
- Owner: [AccessibilityServer](AccessibilityServer.md)

## Description

Independent zero-based selectors; pass one value and a boolean to UpdateSetFlag, never combine their numbers as a bitmask. Native states consume eight selectors; TouchPassthrough and ClipsChildren drive semantic hit testing.

## Values

| Value | Number | Meaning |
| --- | ---: | --- |
| <a id="hidden"></a>`Hidden` | 0 | Identifies the hidden state. |
| <a id="multiselectable"></a>`Multiselectable` | 1 | Identifies the multiselectable state. |
| <a id="required"></a>`Required` | 2 | Identifies the required state. |
| <a id="visited"></a>`Visited` | 3 | Identifies the visited state. |
| <a id="busy"></a>`Busy` | 4 | Identifies the busy state. |
| <a id="modal"></a>`Modal` | 5 | Identifies the modal state. |
| <a id="touchpassthrough"></a>`TouchPassthrough` | 6 | Identifies the touch passthrough state. |
| <a id="readonly"></a>`ReadOnly` | 7 | Identifies the readonly state. |
| <a id="disabled"></a>`Disabled` | 8 | Identifies the disabled state. |
| <a id="clipschildren"></a>`ClipsChildren` | 9 | Identifies the clips children state. |

Use `AccessibilityServer.UpdateSetFlag(id, AccessibilityServer.AccessibilityFlags.Hidden, true)` in an active supported display owner. Invalid numeric selectors reject. Element lifetime and native availability follow the owning service.
