# ImporterMesh.Surface

Last updated: 2026-10-12

Private raw surface storage: copied typed arrays/targets, sorted private LOD views, exact authored flags, primitive/name and borrowed material. Source data stays immutable between complete surface replacements. Views share private raw channels; public queries copy all arrays. Compilation and deep/local resource policies are owned by ImporterMesh.

Source: [ImporterMesh.cs](../../src/Scene/Resources/ImporterMesh.cs). See [ImporterMesh](ImporterMesh.md) for lifecycle, execution and verification limits.
