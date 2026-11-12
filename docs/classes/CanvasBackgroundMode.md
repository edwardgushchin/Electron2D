# CanvasBackgroundMode

Select a mode with [CanvasEnvironment](CanvasEnvironment.md#background) or `RenderingServer.EnvironmentSetBackground`.

| Value | Mode | Behavior |
| ---: | --- | --- |
| 0 | `ClearColor` | Uses the rendering session's default clear color. |
| 1 | `Color` | Uses the environment's authored clear color. |
| 4 | `Keep` | Retains unprocessed canvas history. |
| 6 | `Max` | Terminal marker. |

See [canvas environment backgrounds](../components/canvas-environment-background.md) for native retention and viewport clear policy.
