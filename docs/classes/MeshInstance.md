# MeshInstance

Last updated: 2026-10-04

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

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public MeshInstance()` | Creates a node without mesh or texture. |

## Constructors descriptions

### .ctor

`public MeshInstance()`

Summary: Creates a node without mesh or texture.


## Properties

| Complete signature | Contract |
| --- | --- |
| `public Electron2D.Mesh Mesh { get; set; }` | Gets or sets the borrowed mesh. |
| `public Electron2D.Texture Texture { get; set; }` | Gets or sets the optional borrowed surface texture. |

## Properties descriptions

### Mesh

`public Electron2D.Mesh Mesh { get; set; }`

Summary: Gets or sets the borrowed mesh.

Value: Null initially. Equal assignments are silent; replacement detaches the previous change listener.

System.ObjectDisposedException: The node or assigned mesh is disposed.

System.InvalidOperationException: Mutation is off-owner or capture-owned.


### Texture

`public Electron2D.Texture Texture { get; set; }`

Summary: Gets or sets the optional borrowed surface texture.

Value: Null initially. Equal assignments are silent; replacement commits before TextureChanged.

System.ObjectDisposedException: The node or assigned texture is disposed.

System.InvalidOperationException: Mutation is off-owner or capture-owned.


## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Concrete inherited contract; see Description. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Concrete inherited contract; see Description. |
| `public override System.String[] GetConfigurationWarnings()` | Reports a missing mesh along with inherited scene warnings. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Concrete inherited contract; see Description. |
| `protected override System.Void OnDraw()` | Concrete inherited contract; see Description. |

## Methods and protected extension points descriptions

### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Summary: Reports a missing mesh along with inherited scene warnings.

Returns: Independent warning strings; an empty mesh still counts as an assigned resource.


### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### OnDraw

`protected override System.Void OnDraw()`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


## Events

| Complete signature | Contract |
| --- | --- |
| `public event System.Action TextureChanged` | Occurs after replacing the borrowed texture and scheduling redraw. |

## Events descriptions

### TextureChanged

`public event System.Action TextureChanged`

Summary: Occurs after replacing the borrowed texture and scheduling redraw.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) owns typed arrays, pinned exercised byte packing and deferred deformation/channel consumers. [Mesh component](../components/meshes.md) records current scope and executable verification. [MeshTests](../../tests/Electron2D.Tests/MeshTests.cs) checks copied state, updates/rollback, topology, callbacks, lifetime, duplication/scene storage and warmed replay. [MeshRenderingTests](../../tests/Electron2D.Tests/MeshRenderingTests.cs) checks real rendered pixels, primitive profiles, owned server lifetime and active/idle frames on GPU and compatibility. Native allocator totals, other platforms and owner acceptance remain unverified.

The borrowed Mesh.GetAABB returns local Rect2 visibility bounds for every stored vertex, including unreferenced vertices and point/line topologies. Prepared bounds/replay do not copy live ArrayMesh data. MeshInstance resource-change listeners use worker-safe invalidation; attached node setters retain owner/capture guards.
