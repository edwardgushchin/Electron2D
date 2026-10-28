# ControlScriptCallbacks

Last updated: 2026-10-28

**Base:** [CanvasItemScriptCallbacks](CanvasItemScriptCallbacks.md) · **Source:** [ControlScriptCallbacks.cs](../../src/Scene/GUI/ControlScriptCallbacks.cs) · **Component:** [Scripting](../components/scripting.md#live-control-callbacks)

`ControlScriptCallbacks<TControl> where TControl : Control`

Single-use immutable callback data installed during the owner's synchronous registered Script Bind. Inherited notification, lifecycle, input and canvas callbacks share one retirement and independent per-owner state.

| Members | Contract |
| --- | --- |
| `ControlScriptCallbacks()` | Creates an unused set. |
| `GetMinimumSize`, `GetMaximumSize` | Intrinsic finite sizes; negative maximum components remain unbounded. |
| `HasPoint`, `GetCursorShape` | Control-local hit testing and validated cursor selection, including native derived controls. |
| `GetTooltip`, `GetTooltipAutoTranslateModeAt`, `MakeCustomTooltip` | Untranslated text, default-label translation policy and transferred detached custom content. |
| `StructuredTextParser` | Validated Unicode-scalar ranges for the existing custom text parser. |
| `GUIInput` | Borrowed local event after signal subscribers and before native handling. Acceptance/removal stops later stages. |
| `GetDragData`, `CanDropData`, `DropData` | Borrowed typed pointer/accessible payloads. Explicit drag forwarding keeps precedence. Accessible positions are positive-infinite. |
| `AccessibilityGetContextualInfo`, `GetAccessibilityContainerName` | Non-null semantic help and immediate-child position text. |
| `TextInput`, `IMECompositionChanged` | Commit/preedit callbacks after native hooks and before their signals; errors retain ordinary aggregation. |

Present queries replace native query hooks; null/false results retain their meaning. Missing callbacks retain native behavior. Internal cursor-refresh/capture events omit the GUI signal and live input callback. Replacement/detachment/disposal removes all installed control slots before Retired. Copies and file reconstruction bind independent state before typed storage restoration. These data are not engine objects or serialized callback delegates.
