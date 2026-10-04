# BoundReplicationProperty.Generic

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [ReplicationProperty.cs](../../src/Core/Networking/ReplicationProperty.cs). **Component:** [Scene replication](../components/scene-replication.md).

Internal typed Node getter/setter and codec implementation. Staged values retain their concrete generic type without boxing/universal values; apply clears the temporary reference and validates receiver lifetime. User setter failure cannot undo external side effects.

SceneReplicationTests checks public callers and prepared native operation; this type exposes no native handles.
