# RenderingMeshRegistry

Last updated: 2026-10-07

- Visibility: internal
- Source: [RenderingMeshRegistry.cs](../../src/Servers/Rendering/RenderingMeshRegistry.cs)
- Component: [Mesh surfaces](../components/meshes.md)

## Executable mesh skin integration

Owned meshes now accept actual packed skin-region edits. Borrowed resource mesh ownership remains protected; disposal and renderer teardown close identities independently of palette lifetime.

## Description

Weak logical resource mesh identities and retained caller-owned ArrayMesh entries have distinct ownership. Borrowed mesh identities cannot be mutated/freed by the renderer; native teardown/free closes owned entries. Periodic cold stale sweeping bounds abandoned weak entries. MeshSkinTests verifies actual owned skin edits and ordinary MeshTests cover borrowing/disposal.
