# RenderingSkeletonRegistry.Palette

Last updated: 2026-10-07

- Visibility: internal
- Source: [RenderingSkeletonRegistry.cs](../../src/Servers/Rendering/RenderingSkeletonRegistry.cs)
- Component: [Mesh surfaces](../components/meshes.md)

## Description

Renderer-owned arrays/base with active replay reader count. Zero allocation storage starts empty, resize clears matrices and equal capacity preserves values. A prepared mesh reader increments/decrements in finally; set/resize/free rejects during callbacks. Renderer close/free removes identity and later attachments replay original geometry. No backend pointer or public resource role.

## Verification

MeshSkinTests covers actual scene identity and palette consumer lifetime, native owned/borrowed guards and prepared replay. Native allocator/foreign targets/large-scene throughput/owner acceptance remain unverified.
