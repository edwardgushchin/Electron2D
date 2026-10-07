# CanvasPolygon

Last updated: 2026-10-07

- Declaration: `internal sealed class CanvasPolygon`
- Source: [CanvasPolygon.cs](../../src/Servers/Rendering/CanvasPolygon.cs)
- Component: [Canvas rendering](../components/canvas-rendering.md#polygon-commands)

## Prepared skin replay

AttachSkin retains an optional [CanvasSkeletonSkin](CanvasSkeletonSkin.md) helper/source map after normal Polygon recording. Set clears activation for a reused slot, then the current owner reattaches it. Append preflights deformed local positions and expands the existing indices; this changes position only. Missing/disabled bindings use original vertices. Ordinary pose replay and prepared forced recording reuse arrays; inversion remains the existing cold contour path.

## Description

Internal retained vertex/index storage owned by CanvasItem. It is not a public resource or an independently disposable object. CanvasItem keeps one reusable instance per recorded polygon/primitive position, resets its cursor before recording and clears the pool on disposal. Borrowed textures stay in CanvasCommand, not this object. All access runs on the recording/render owner thread.

## Internal API

| Member | Contract |
| --- | --- |
| `CanvasVertex[] Vertices`, `int VertexCount` | Copied local vertices, finite colors and normalized UVs; trailing capacity is unused. |
| `int[] Indices`, `int IndexCount` | Filled triangle indices, with reusable capacity. |
| `void Set(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, bool primitive)` | Copies validated attributes; triangulates a contour through shared Geometry logic or creates fixed triangle/quad indices. |
| `void SetTriangles(ReadOnlySpan<CanvasVertex> triangles)` | Copies validated triangle vertices from internal scene drawing and writes sequential indices without contour triangulation. |
| `void RemapUV(Rect2 mapping)` | Applies normalized atlas origin/scale to stored UVs, rejecting nonfinite output. |
| `void Append(List<CanvasVertex> output, Transform transform, Color modulation, bool snap)` | Appends transformed/modulated triangles, or one-pixel point/line geometry. |

## Runtime behavior

Set is called only after CanvasItem validates counts, values, resource lifetime and drawing scope. Triangulate uses a reusable index array, normalizes winding, visits consecutive ears and relaxes the convexity/edge test after a stalled pass to permit degenerate final ears. A second stalled pass throws ArgumentException. Self-intersections and holes have no supported contract. Double intermediates keep finite float coordinates from overflowing the determinant calculation. Ear selection has cubic worst-case complexity; large constantly redrawn contours need separate performance work.

Line uses SetTriangles through the internal CanvasItem triangle recorder. It preserves each vertex's color and UV and bypasses contour triangulation, then follows the same retained command, atlas remap, transform and backend batch path.

RemapUV runs before the command is committed. Append rejects transformed positions or modulated colors that overflow. Point/line endpoints transform before expansion so their width stays one framebuffer pixel; coincident two-point lines produce nothing. Filled geometry retains its stored triangulation while positions and colors transform every submission. Vertex snapping does not modify the recorded local values.

Recording failure may leave scratch contents changed, but no command references that uncommitted pool slot. Existing recorded commands have distinct pool entries. An uncaught drawing callback failure clears all commands; retry starts at slot zero. Growth can allocate, while fixed-capacity redraw, triangulation and replay do not.

## Verification

[CanvasPolygonTests](../../tests/Electron2D.Tests/CanvasPolygonTests.cs) covers area/winding, attributes, invalid contours, pooling, failure/retry, custom callback reentry, lifetime, transforms and allocation. [CanvasPolygonRenderingTests](../../tests/Electron2D.Tests/CanvasPolygonRenderingTests.cs) covers native sampling and pixels. Public use is documented through [CanvasItem](CanvasItem.md#drawpolygon); this internal helper has no user-facing example or public compatibility row.

## Low-level primitive integration

SetIndexedTriangles reuses copied MeshSurfaceData arrays for raw producer storage, with complete draw triples, independent optional four-slot channels and UNORM16 quantization. Bounds include original input points; classic contour/scene skin remains separate. Replay calls the same CanvasMesh palette helper with an owner lease. See [the executable primitive contract](../components/canvas-rendering.md#low-level-primitive-commands).
