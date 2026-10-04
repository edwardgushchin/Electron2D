# BoundReplicationProperty

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [ReplicationProperty.cs](../../src/Core/Networking/ReplicationProperty.cs). **Component:** [Scene replication](../components/scene-replication.md).

Internal prepared property owner: maximum-size current/previous bytes, encoded change revision, spawn/mode flags and staged concrete decoded value. Captures only due periodic or watched properties; complete payload decoding precedes setters. Buffers are bounded during preparation.

SceneReplicationTests checks public callers and prepared native operation; this type exposes no native handles.
