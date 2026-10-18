# MeshInstance

Last updated: 2026-10-18

**Namespace:** `Electron2D` · **Declaration:** `public class Electron2D.MeshInstance` · **Source:** [MeshInstance.cs](../../src/Scene/2D/MeshInstance.cs).

**Inherits:** [Entity](Entity.md).

## Executable mesh skin integration

RenderingServer.CanvasItemAttachSkeleton(GetCanvasItem(), paletteRID) now makes its retained Mesh consume actual four/eight skin data. The node owns neither mesh nor palette; attachment is transient and must be authored again after scene copying/loading.

## Description

Entity scene consumer borrowing Mesh and optional Texture. Setters use scene owner/capture guards; equal assignments are silent. Mesh identity preparation occurs before subscribing Changed, with mutation revalidation afterward. Texture commits before TextureChanged, and listener failure retains the assignment. OnDraw records DrawMesh with current resources, retaining live ArrayMesh geometry; per-surface material overrides the inherited canvas material. Mesh changes request redraw, but live replay also sees committed region edits even if an earlier Changed listener fails. Disposal detaches event handlers and clears borrowed fields without disposing resources. GetConfigurationWarnings reports a missing Mesh, including inherited warnings; assigning even an empty mesh clears this warning before warning listeners run. The exact scene factory and typed stored descriptors preserve borrowed/local Resource ownership.

## Example

```csharp
using var mesh = new ArrayMesh();
mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
{ Vertices = [new(0, 0), new(20, 0), new(0, 20)] });
var node = new MeshInstance { Mesh = mesh, Position = new(10, 10) };
// window.AddChild(node); Engine.Run(window);
```


Named shape values are stored on each node independently. Rendering mixes local positions according to the shared mesh mode before skinning, preserves the base numerical/other channels, and caches node-owned geometry. See [the complete deformation contract](../components/meshes.md#named-targets-and-instance-deformation).

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public MeshInstance()` | Creates a node without mesh or texture. |
| `public event System.Action TextureChanged` | Occurs after replacing the borrowed texture and scheduling redraw. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` |  |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `public System.Single GetBlendShapeValue(System.Int32 index)` | Gets the finite deformation weight of one shape on this instance. |
| `public override System.String[] GetConfigurationWarnings()` | Reports a missing mesh along with inherited scene warnings. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` |  |
| `protected override System.Void OnDraw()` |  |
| `public System.Void SetBlendShapeValue(System.Int32 index, System.Single value)` | Sets one finite deformation weight and validates resulting local geometry before mutation. |
| `protected override System.Void ValidateDisposal()` |  |
| `protected override System.Void ValidateMutation()` |  |
| `public Electron2D.Mesh Mesh { get; set; }` | Gets or sets the borrowed mesh. |
| `public Electron2D.Texture Texture { get; set; }` | Gets or sets the optional borrowed surface texture. |

## Member descriptions

### MeshInstance

`public MeshInstance()`

summary: Creates a node without mesh or texture.

### TextureChanged

`public event System.Action TextureChanged`

summary: Occurs after replacing the borrowed texture and scheduling redraw.

### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

### GetBlendShapeValue

`public System.Single GetBlendShapeValue(System.Int32 index)`

summary: Gets the finite deformation weight of one shape on this instance.

index: Existing zero-based shape index.

returns: Zero initially; weights belong to this node independently of shared mesh resources.

### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

summary: Reports a missing mesh along with inherited scene warnings.

returns: Independent warning strings; an empty mesh still counts as an assigned resource.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

### OnDraw

`protected override System.Void OnDraw()`

### SetBlendShapeValue

`public System.Void SetBlendShapeValue(System.Int32 index, System.Single value)`

summary: Sets one finite deformation weight and validates resulting local geometry before mutation.

index: Existing zero-based shape index.

value: Finite weight; values are not clamped or renormalized; absolute values at most 0.0001 have no drawing contribution.

remarks: Normalized and relative modes use the mesh's selected policy. Changes update this node's live positions before skinning; numerical normal/tangent, material, UV/custom and skin channels remain inherited. Prepared scalar changes and drawing reuse node-owned geometry storage.

### ValidateDisposal

`protected override System.Void ValidateDisposal()`

### ValidateMutation

`protected override System.Void ValidateMutation()`

### Mesh

`public Electron2D.Mesh Mesh { get; set; }`

summary: Gets or sets the borrowed mesh.

value: Null initially. Equal assignments are silent; replacement detaches the previous change listener.

System.ObjectDisposedException: The node or assigned mesh is disposed.

System.InvalidOperationException: Mutation is off-owner or capture-owned.

### Texture

`public Electron2D.Texture Texture { get; set; }`

summary: Gets or sets the optional borrowed surface texture.

value: Null initially. Equal assignments are silent; replacement commits before TextureChanged.

System.ObjectDisposedException: The node or assigned texture is disposed.

System.InvalidOperationException: Mutation is off-owner or capture-owned.

## LOD behavior

Authored index sets select from the full local-to-framebuffer basis using a one-pixel error threshold. MultiMesh selects each presentation instance independently; MeshInstance morph output retains LODs. Base queries and conservative bounds describe full geometry. Storage/callback ownership and detailed validation are documented in [authored LOD indices](../components/meshes.md#authored-lod-indices-and-scale-selection).

## Procedural mesh integration

Procedural mesh surfaces apply item and per-vertex skin transforms after authored local pixel-to-clip output. Explicit mesh/surface bounds cover generated output; CPU positions and collisions remain separate. See [the connected contract](../components/meshes.md#procedural-surfaces-without-positions).
