# MeshMorphPose

Last updated: 2026-10-12

Internal independent mesh deformation state in [MeshMorphPose.cs](../../src/Scene/Resources/MeshMorphPose.cs). Borrows the source resource and captures copied custom callback state with revision/reentry checks; ArrayMesh reads live private channels under its gate. Owns reusable deformed ArrayMesh output and preserves base attributes, canonical frame/UV records, skin and LOD indices. Scalar weights preflight widened finite positions/bounds before publication. Source layout or compression-scale changes rebuild cold storage; prepared scalar/source-region refresh reuses it. Disposal closes output without disposing borrowed sources/materials. See [deformation](../components/meshes.md#named-targets-and-instance-deformation) and [compressed attributes](../components/meshes.md#compressed-attributes) for execution and limits.
