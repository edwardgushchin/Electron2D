# MeshMorphPose

Last updated: 2026-10-18

Internal independent mesh deformation state in [MeshMorphPose.cs](../../src/Scene/Resources/MeshMorphPose.cs). Borrows the source resource and captures copied custom callback state with revision/reentry checks; ArrayMesh reads live private channels under its gate. Owns reusable deformed ArrayMesh output and preserves base attributes, canonical frame/UV records, skin and LOD indices. Scalar weights preflight widened finite positions/bounds before publication. Source layout or compression-scale changes rebuild cold storage; prepared scalar/source-region refresh reuses it. Disposal closes output without disposing borrowed sources/materials. See [deformation](../components/meshes.md#named-targets-and-instance-deformation) and [compressed attributes](../components/meshes.md#compressed-attributes) for execution and limits.

Manual ArrayMesh.CustomAABB changes propagate to the prepared pose independently of deformed source bounds. Resetting the source override restores automatic visibility without replacing per-instance geometry or sharing mutable pose storage.

## Procedural mesh integration

Procedural morph output retains logical counts, absent positions, numerical direction data and authored levels. Independent target masks permit absent synthesized base tangents; no position target is fabricated. See [the connected contract](../components/meshes.md#procedural-surfaces-without-positions).
