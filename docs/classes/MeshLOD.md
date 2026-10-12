# MeshLOD

Last updated: 2026-10-12

Internal sorted threshold/geometry view in [Mesh.LODs.cs](../../src/Scene/Resources/Mesh.LODs.cs), owned by [mesh surfaces](../components/meshes.md#authored-lod-indices-and-scale-selection). Capture validates positive finite distinct thresholds, shorter nonempty indexed topology and every vertex index before copying alternative indices. Views share only private base channel arrays; ordinary packed updates remain visible without fresh captures. Copy produces independent public dictionary/index arrays. Select uses widened greatest-singular-value arithmetic for the complete framebuffer basis and chooses the largest local error at most one pixel. Source structure and ownership are fixed until an owning surface is replaced. There are no native identities or shader handles.
