# SkeletonModificationJiggle.State

Last updated: 2026-10-07

- Declaration: `private struct State`
- Source: [SkeletonModificationJiggle.cs](../../src/Scene/Resources/SkeletonModificationJiggle.cs)
- Owner: [SkeletonModificationJiggle](SkeletonModificationJiggle.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

Transient value record containing BoneID/setup generation, Valid, Dynamic/LastOrigin/LastClear points and Velocity/Acceleration/Force. The owner retains a count-matched array. Seed/reset initialize all three points from a live bone origin and clear accumulated vectors. Each successful joint computes into a local record and publishes after its pose/request; numerical or query failure preserves that joint's previous record. Earlier successful state remains committed after later failure while the skeleton restores scene poses/requests. Layout/selection changes discard stale history; no live Bone or native handle is retained, copied or archived.

SkeletonJiggleTests exercises actual used values, history/reset/error commitment and prepared allocation. Native/foreign/large-rig acceptance remains separately scoped.
