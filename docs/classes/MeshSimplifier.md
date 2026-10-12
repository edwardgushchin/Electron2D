# MeshSimplifier

Last updated: 2026-10-12

Internal cold attribute-aware simplification in a pinned native kernel. Captures normalized numeric normals, clamped rendered RGB, UV/handedness seams and exact custom/skin/target distinctions; poses affect only temporary XY error positions. Widened arithmetic rejects nonfinite transformed bounds. Native indices/errors validate before replacement, and native allocation failure surfaces explicitly. Generated error thresholds remain strictly increasing. No renderer or scene handle is exposed.

Source: [MeshSimplifier.cs](../../src/Scene/Resources/MeshSimplifier.cs). See [ImporterMesh](ImporterMesh.md) for lifecycle, execution and verification limits.
