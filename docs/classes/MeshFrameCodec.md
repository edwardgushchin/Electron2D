# MeshFrameCodec

Last updated: 2026-10-12

Internal numerical direction helper in [MeshFrameCodec.cs](../../src/Scene/Resources/MeshFrameCodec.cs). Computes widened finite planar normals and normalization, synthesizes missing tangents and packs/decodes ordinary octahedral direction/handedness words. Zero directions have deterministic positive-Z codes. Ordinary surface capture allocates canonical words, then region decoding reuses storage; compression shares synthesis and widened normalization through [MeshCompression](MeshCompression.md). It has no public API or native identities. See [mesh frame semantics](../components/meshes.md) for tests and limitations.
