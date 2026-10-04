# MultiplayerPathCache

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [SceneMultiplayer.cs](../../src/Core/Networking/SceneMultiplayer.cs). **Component:** [Scene multiplayer](../components/scene-multiplayer.md).

Internal immutable cached node and copied UTF8 path. SceneMultiplayer resolves a path on first use, validates branch containment, and reuses identity/bytes in steady dispatch. Structural changes/root reassignment/removal invalidate caches before external TreeChanged delivery, so throwing observers cannot preserve stale routes.

SceneMultiplayerTests exercises authentication/rename/branch/reset and prepared message flow. No native handles are owned by this record.
