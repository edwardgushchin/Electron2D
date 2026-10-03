# RenderingMultiMeshRegistry

Last updated: 2026-10-03

Internal rendering helper; source [RenderingMultiMeshRegistry.cs](../../src/Servers/Rendering/RenderingMultiMeshRegistry.cs).

Process-wide logical RID registry with weak borrowed resources and strong renderer-owned entries. Owned lookup rejects foreign/borrowed mutation/free. PhysicsTick snapshots live targets into a prepared list under the registry gate, releases that gate, then updates each resource; this avoids registry/resource lock inversion. Registration grows capacity cold, every 256 registrations prunes stale entries, and warmed tick capture reuses storage. Reentrant capture rejects; disposal invalidates lookup.

[ADR 0092](../decisions/mesh.md#adr-0092) and [mesh component](../components/meshes.md#repeated-instance-resources) define ownership and verification. This helper adds no public API. MultiMeshTests and MultiMeshRenderingTests exercise successful, invalid, callback-failure and warmed paths.
