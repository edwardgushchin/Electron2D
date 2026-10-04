# SpawnedNodeRecord

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [MultiplayerSpawner.cs](../../src/Scene/Multiplayer/MultiplayerSpawner.cs). **Component:** [Scene replication](../components/scene-replication.md).

Internal tracked spawner/node identity, copied spawn arguments, ordered scene/factory selector, remote source and visible peer set. Local node exit untracks and sends despawn; remote records own parented instances through the configured scene lifetime. Network reset frees remotes and preserves local configurations.

SceneReplicationTests checks public callers and prepared native operation; this type exposes no native handles.
