# CanvasItemScriptCallbacks

Last updated: 2026-10-28

**Base:** [NodeScriptCallbacks](NodeScriptCallbacks.md) · **Source:** [CanvasItemScriptCallbacks.cs](../../src/Scene/Main/CanvasItemScriptCallbacks.cs) · **Component:** [Scripting](../components/scripting.md#live-canvas-recording)

`CanvasItemScriptCallbacks<TItem> where TItem : CanvasItem`

Single-use immutable callback data for a compatible canvas receiver, installed only during that owner's registered synchronous Script Bind. It inherits ordinary node callbacks and Retired state cleanup.

| Member | Contract |
| --- | --- |
| `CanvasItemScriptCallbacks()` | Creates an unused set. |
| `Action<TItem>? Draw { get; init; }` | Records after the native OnDraw hook, in the same scope/list as NotificationDraw and the synchronous Draw signal. Signal blocking preserves this direct hook. |

Commands retain ordinary drawing validation and lifetime. Retained replay does not reinvoke the callback; failure discards partial commands and leaves a retry. Installation/removal invalidates recording, including replacement inside Draw. The active callback can finish while the next recording uses the new binding. CollisionObject/RigidBody callback sets inherit this role. Callback data are not engine objects or serialized instances: copy/file reconstruction executes Bind to prepare independent state.

[Native qualification](../components/scripting.md#live-canvas-recording) distinguishes real rendered pixels, physical identity and render-interval allocation from whole-goal numerical/network/performance and foreign-platform acceptance.
