# RenderingSkeletonRegistry

Last updated: 2026-10-07

- Declaration: `internal static class RenderingSkeletonRegistry`
- Source: [RenderingSkeletonRegistry.cs](../../src/Servers/Rendering/RenderingSkeletonRegistry.cs)
- Component: [Skeletal animation](../components/skeletal-animation.md)

Register allocates a stable weak logical RID for a scene Skeleton independently of renderer initialization. Resolve returns the live borrowed rig or drops a stale identity; Contains guards borrowed FreeRID attempts and Remove closes its identity on disposal. A gate protects cold registration/removal and weak lookup; every 256 registrations sweeps stale owners using reusable scratch storage. Identity never owns the rig or native buffer. The registry has no caller-owned skeleton producer: that server-owned storage/attachment family retains exact coverage prerequisites. Retained Polygon commands consume the real borrowed identity and prepare the rig palette; tests verify lookup and native FreeRID rejection.
