# CanvasSkeletonSkin

Last updated: 2026-10-07

- Declaration: `internal sealed class CanvasSkeletonSkin`
- Source: [CanvasSkeletonSkin.cs](../../src/Servers/Rendering/CanvasSkeletonSkin.cs)
- Component: [Skeletal animation](../components/skeletal-animation.md)

Retained polygon-owned prepared binding. Set stores a weak Polygon owner and source-index map. Prepare resolves its borrowed palette RID, rebuilding four strongest positive influences when the record, hierarchy or scene-path revision changes. Equal weights retain authored record order. Mismatched source counts and missing bone paths do not contribute. Bone indices/weights and deformed positions retain reusable capacity.

Prepare maps points from the polygon presentation basis into the skeleton basis, applies pose times inverse rest, normalizes positive influences with double accumulation and maps back. Zero total keeps original points. Singular item/skeleton bases keep the undeformed input; nonfinite deformation fails before writing output triangles. Bone TopLevel and interpolation use actual presentation transforms. Position supplies validated prepared local output. Color, UV, material and original triangulation are preserved. One viewport and CanvasLayer are required; cross-space paths reject explicitly. Both native triangle backends consume the same positions. SkeletonTests covers authoring, pixels and warmed managed allocation; large rigs/native allocation remain unmeasured.
