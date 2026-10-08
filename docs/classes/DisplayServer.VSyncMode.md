# DisplayServer.VSyncMode

Last updated: 2026-10-09

**Namespace:** `Electron2D`. **Declaration:** `public enum DisplayServer.VSyncMode`. **Source:** [DisplayServer.VSync.cs](../../src/Servers/Display/DisplayServer.VSync.cs). **Component:** [Display server](../components/display-server.md).

| Value | Identity | Behavior |
| --- | --- | --- |
| Disabled | 0 | Immediate presentation without a native vertical-blank wait. |
| Enabled | 1 | Ordered vertical-blank presentation. |
| Adaptive | 2 | Adaptive renderer synchronization where supported; otherwise Enabled. |
| Mailbox | 3 | Latest-image vertical-blank presentation where supported; otherwise Enabled. |

Use `DisplayServer.WindowSetVSyncMode` and `WindowGetVSyncMode`. Unsupported modes report the actual Enabled fallback. The compositor and driver can independently control final display timing. This policy is independent of Engine.MaxFPS.
