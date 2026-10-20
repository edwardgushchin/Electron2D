# PrimitiveMesh

Last updated: 2026-10-20

**Namespace:** `Electron2D` · **Declaration:** `public abstract class PrimitiveMesh : Mesh` · **Source:** [PrimitiveMesh.cs](../../src/Scene/Resources/PrimitiveMesh.cs).

**Inherits:** [Mesh](Mesh.md) → [Resource](Resource.md).

## Description

Generates one retained positioned triangle surface through a typed C# callback. The first geometry/count/RID query builds it; earlier RequestUpdate calls leave generation pending. Later requests rebuild synchronously. Callback input arrays and query output arrays are independent copies. Successful publication precedes Changed; failed callbacks or validation retain the previous surface. A throwing observer does not undo publication. Settings may be read from the callback, which runs outside the private settings lock. Nested generation, initial surface queries and shared-control/reset/copy mutation during generation throw InvalidOperationException; an older publication remains readable. Callers synchronize complete parameter-edit/update sequences across threads.

The [PrimitiveMeshes example](../../examples/PrimitiveMeshes/README.md) implements a planar disk with radius/segment parameters, a resource duplication factory, stored descriptors and parameter copying. Scene storage requires explicit ResourceFileTypes registration of the concrete consumer. No built-in 3D shapes or baking interface is implied. Cold generation, duplication, resource files and initial preparation may allocate. Prepared replay retains the ordinary mesh snapshot.

## Constructors

| Signature | Contract |
| --- | --- |
| `protected PrimitiveMesh()` | Initializes pending generation, no material, no optional controls. |

## Properties

| Signature | Contract |
| --- | --- |
| `public bool FlipFaces { get; set; }` | False initially; reverses winding and supplied normals. Sequential triangles gain reversed indices while every vertex channel keeps its order. |
| `public bool AddUV2 { get; set; }` | False initially; fills absent secondary coordinates from primary UVs. Authored UV2 is always retained; no primary UV means no fallback. |
| `public float UV2Padding { get; set; }` | Two initially; fallback UV2 is `UV * (1 - UV2Padding / 1024)`. Any finite padding is accepted; overflowing generated values reject. |
| `public Material? Material { get; set; }` | Borrowed, null initially. A live edit changes the surface material and notifies without rebuilding. Before generation it only stores the value. Disposed assigned materials reject. |
| `public Rect2 CustomAABB { get; set; }` | Separate finite nonnegative local visibility override; zero restores automatic bounds. Equal assignments also commit and notify. Does not change source geometry or regenerate. |

Changed shared geometry controls request an update; equal values are silent. Invalid surface indices throw ArgumentOutOfRangeException. Disposed-resource operations throw ObjectDisposedException. Mesh, canvas commands, MeshInstance, MultiMesh and borrowed RenderingServer queries consume the generated surface and its visibility metadata through the existing renderer.

## Methods

| Signature | Contract |
| --- | --- |
| `protected abstract MeshSurfaceData OnCreateMeshArray()` | Returns non-null finite positioned complete triangles with valid optional frame/color/UV/custom/skin/index channels. Input is copied and validated before postprocessing. |
| `public MeshSurfaceData GetMeshArrays()` | Returns the same processed surface data as SurfaceGetArrays(0), in caller-owned arrays. |
| `public void RequestUpdate()` | Leaves initial generation pending, or synchronously rebuilds an existing surface. Errors preserve the last publication for retry. |
| `public override RID GetRID()` | Generates on first use, then returns the stable borrowed mesh identity. |

Inherited surface callbacks are sealed around the single triangle surface. Mesh source queries GetAABB, GetFaces and CreatePlaceholder describe source geometry independently of CustomAABB. Normal/tangent and skin channels use ordinary mesh codecs. Resource ResetState clears only the generated cache; all stored controls, derived parameters, visibility and borrowed material remain. The next geometry query rebuilds and publishes.

## Resource extension

Override CreateDuplicateInstance with a typed concrete factory. Override CopyCustomStateTo, call base, then copy the concrete generator parameters; shared geometry is regenerated rather than shared. The base preserves FlipFaces, AddUV2, UV2Padding and CustomAABB and applies the ordinary shallow/deep/local policy to Material. Add stored parameter descriptors for file loading. Missing consumer duplication factories fail explicitly. Borrowed materials survive mesh disposal.

## Verification

See the [mesh component](../components/meshes.md#generated-planar-primitives) for the exercised native/managed profiles and limits.
