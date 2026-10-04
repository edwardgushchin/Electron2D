# ReplicationPeerState

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [MultiplayerSynchronizer.cs](../../src/Scene/Multiplayer/MultiplayerSynchronizer.cs). **Component:** [Scene replication](../components/scene-replication.md).

Internal prepared per-synchronizer/peer receipt array, delta timestamp and inbound periodic sequence state. Receipts commit only after successful packet send. Membership/configuration prepares storage; repeated states reuse it.

SceneReplicationTests checks public callers and prepared native operation; this type exposes no native handles.
