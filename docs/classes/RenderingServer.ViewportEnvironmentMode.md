# RenderingServer.ViewportEnvironmentMode

Pass this mode to [ViewportSetEnvironmentMode](RenderingServer.md#canvas-glow) for a live viewport.

| Value | Name | Behavior |
| ---: | --- | --- |
| 0 | `Disabled` | Bypasses the world environment for the viewport. |
| 1 | `Enabled` | Uses the world environment when the target supports it. |
| 2 | `Inherit` | Uses the parent viewport's mode. An inheriting root enables the environment. |
| 3 | `Max` | Terminal enum marker. |
