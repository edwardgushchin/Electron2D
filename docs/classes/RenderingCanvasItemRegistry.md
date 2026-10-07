# RenderingCanvasItemRegistry

Last updated: 2026-10-07

- Visibility: internal
- Source: [RenderingCanvasItemRegistry.cs](../../src/Servers/Rendering/RenderingCanvasItemRegistry.cs)
- Component: [Mesh surfaces](../components/meshes.md)

## Description

Cold weak CanvasItem-to-RID registry. IDs remain stable until node disposal; periodic stale sweeping prevents abandoned handles from retaining entries indefinitely. Resolve validates live nodes; ordinary scene owner/capture checks remain on the actual operations. No caller-owned generic canvas object is created.

## Verification

MeshSkinTests covers actual scene identity and palette consumer lifetime, native owned/borrowed guards and prepared replay. Native allocator/foreign targets/large-scene throughput/owner acceptance remain unverified.
