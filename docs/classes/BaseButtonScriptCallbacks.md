# BaseButtonScriptCallbacks

Last updated: 2026-10-28

**Base:** [ControlScriptCallbacks](ControlScriptCallbacks.md) · **Source:** [BaseButtonScriptCallbacks.cs](../../src/Scene/GUI/BaseButtonScriptCallbacks.cs) · **Component:** [Scripting](../components/scripting.md#live-button-callbacks)

`BaseButtonScriptCallbacks<TButton> where TButton : BaseButton`

Fresh single-use callback data installed during a compatible button's synchronous Script Bind. Inherited Control/canvas/node/object callbacks use the same owner-local installation and retirement.

| Member | Contract |
| --- | --- |
| `BaseButtonScriptCallbacks()` | Creates an unused set. |
| `Action<TButton>? Pressed` | Activation before the native OnPressed hook and Pressed signal. |
| `Action<TButton, bool>? Toggled` | Committed signalled toggle before native OnToggled and the Toggled signal. Silent state assignment omits it. |

Signal blocking suppresses signals only. Newer interactions, removal/disposal skip later phases of the old operation. Callback errors retain remaining current phases and the ordinary error aggregation. Replacement inside Toggled selects current code for the later Pressed phase; an executing callback can finish after retirement. Copies and file owners initialize independent state before typed storage restoration. These data are not engine objects or serialized delegates.

The abstract BaseButton role can describe a live binding attached to Button, TextureButton and compatible descendants. Such a source reports CanInstantiate false; the concrete native owner is created separately.
