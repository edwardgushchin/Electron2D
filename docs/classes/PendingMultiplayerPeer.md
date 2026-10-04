# PendingMultiplayerPeer

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [SceneMultiplayer.cs](../../src/Core/Networking/SceneMultiplayer.cs). **Component:** [Scene multiplayer](../components/scene-multiplayer.md).

Internal value state for one direct authentication session: monotonic start, saved span callback and local/remote completion. SceneMultiplayer owns its dictionary; admission removes the record only after both confirmations. Prepared expiration scratch is separate from relay membership snapshots; failures remove pending state before callbacks.

SceneMultiplayerTests exercises authentication/rename/branch/reset and prepared message flow. No native handles are owned by this record.
