# MeshSimplifier

Last updated: 2026-10-29

Internal cold attribute-aware simplification in a pinned native kernel. Captures normalized numeric normals, clamped rendered RGB, UV/handedness seams and exact custom/skin/target distinctions; poses affect only temporary XY error positions. Widened arithmetic rejects nonfinite transformed bounds. Native indices/errors validate before replacement, and native allocation failure surfaces explicitly. Generated error thresholds remain strictly increasing. No renderer or scene handle is exposed.

Source: [MeshSimplifier.cs](../../src/Scene/Resources/MeshSimplifier.cs). See [ImporterMesh](ImporterMesh.md) for lifecycle, execution and verification limits.

Temporary native storage uses the existing allocation ledger through an immutable
caller allocator. The adapter preserves the pinned kernel's throwing allocation
contract; failure drains prior scratch blocks and returns the existing typed
error sentinel. Scalar/error semantics and pristine topology remain unchanged.
Registration is prepared before use and old native bundles reject explicitly.
All three private C entry points use the C calling convention. See
[allocation qualification](../coverage/performance-producers.md#mesh-simplification-allocation-domain-2026-10-29).

## Procedural mesh integration

Position-free sources retain their authored LODs because geometric simplification requires supplied positions. See [the connected contract](../components/meshes.md#procedural-surfaces-without-positions).
