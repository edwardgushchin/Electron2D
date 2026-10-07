# RenderingSkeletonRegistry

Last updated: 2026-10-07

- Declaration: `internal static class RenderingSkeletonRegistry`
- Source: [RenderingSkeletonRegistry.cs](../../src/Servers/Rendering/RenderingSkeletonRegistry.cs)
- Component: [Skeletal animation](../components/skeletal-animation.md)

Register allocates a stable weak logical RID for a scene Skeleton independently of renderer initialization. Resolve returns the live borrowed rig or drops a stale identity; Contains participates in scene/owned FreeRID ownership validation and Remove closes its identity on disposal. A gate protects cold registration/removal and weak lookup; every 256 registrations sweeps stale owners using reusable scratch storage. Identity never owns the rig or native buffer. Caller-owned palettes now occupy distinct renderer-owned entries, with actual storage/read/replay/free operations. Retained Polygon commands consume the real borrowed identity and prepare the rig palette; tests verify lookup and native FreeRID rejection.

## Executable mesh skin integration

The registry now distinguishes weak scene palettes from caller-owned renderer palettes. Owned entries retain transform arrays/base and an active replay reader count; read views resolve current scene pose/inverse-rest storage. Owned mutation/free cannot touch scene entries or an active replay view.
