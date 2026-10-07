# MultiMeshInstance

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public class Electron2D.MultiMeshInstance` · **Source:** [MultiMeshInstance.cs](../../src/Scene/2D/MultiMeshInstance.cs).

**Inherits:** [Entity](Entity.md).

## Executable mesh skin integration

A transient attached palette now deforms its underlying mesh through CanvasMesh before each instance transform. Conservative replay avoids rest-bound culling of deformed geometry; resource-local GetAABB still reports authored geometry.

## Description

Entity scene consumer borrowing MultiMesh and optional Texture. Setters enforce attached owner/capture guards, prepare logical identity before subscription and revalidate mutation. Equal replacements are silent. Texture changes commit before TextureChanged; callback failure retains assignment. Resource changes use worker-safe canvas invalidation, and retained replay reads current instance/mesh data even when Changed listener failure prevents later listeners.

OnDraw records DrawMultiMesh with inherited transform/modulate/material/clip/order/sampling policies. Entry/reset and inherited policy changes refresh shared resource interpolation. Exact typed factory/descriptors preserve ResourceLocalToScene graph copying. GetConfigurationWarnings includes inherited warnings and reports a missing MultiMesh; an empty assigned resource clears that warning. Disposal detaches listeners without disposing borrowed resources. Native GPU and compatibility execute ordinary geometry; GPU supports raw custom fragment data while compatibility rejects shaders.

## Example

```csharp
using var instances = new MultiMesh { Mesh = mesh, InstanceCount = 1 }; // Existing live Mesh.
instances.SetInstanceTransform2D(0, Transform.Identity);
var node = new MultiMeshInstance { MultiMesh = instances, Position = new(30, 20) };
// Add node to a Window and run Engine.Run(window).
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public MultiMeshInstance()` | Creates a node without mesh or texture. |

## Constructors descriptions

<a id="member-5cb94ce84119"></a>
### .ctor

`public MultiMeshInstance()`

Creates a node without mesh or texture.

## Properties

| Complete signature | Contract |
| --- | --- |
| `public Electron2D.MultiMesh MultiMesh { get; set; }` | Gets or sets the borrowed instance resource. |
| `public Electron2D.Texture Texture { get; set; }` | Gets or sets the optional borrowed surface texture. |

## Properties descriptions

<a id="member-36b140dd950e"></a>
### MultiMesh

`public Electron2D.MultiMesh MultiMesh { get; set; }`

Gets or sets the borrowed instance resource.

Value: Null initially. Equal assignments are silent; replacement detaches the previous change listener.

System.ObjectDisposedException: The node or assigned instance resource is disposed.

System.InvalidOperationException: Mutation is off-owner or capture-owned.

<a id="member-1ed456c3f028"></a>
### Texture

`public Electron2D.Texture Texture { get; set; }`

Gets or sets the optional borrowed surface texture.

Value: Null initially. Equal assignments are silent; replacement commits before TextureChanged.

System.ObjectDisposedException: The node or assigned texture is disposed.

System.InvalidOperationException: Mutation is off-owner or capture-owned.

## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Inherited resource/node hook; see Description for ownership and lifecycle. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited resource/node hook; see Description for ownership and lifecycle. |
| `public override System.String[] GetConfigurationWarnings()` | Reports a missing mesh along with inherited scene warnings. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited resource/node hook; see Description for ownership and lifecycle. |
| `protected override System.Void OnDraw()` | Inherited resource/node hook; see Description for ownership and lifecycle. |
| `protected override System.Void OnNotification(System.Int32 what)` | Inherited resource/node hook; see Description for ownership and lifecycle. |

## Methods and protected extension points descriptions

<a id="member-f5babab22216"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Inherited resource/node hook; see Description for ownership and lifecycle.

<a id="member-8e0172908012"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Inherited resource/node hook; see Description for ownership and lifecycle.

<a id="member-439b94ef4a68"></a>
### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Reports a missing mesh along with inherited scene warnings.

Returns: Independent warning strings; an empty mesh still counts as an assigned resource.

<a id="member-0cbd3017820a"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited resource/node hook; see Description for ownership and lifecycle.

<a id="member-3906be1294e1"></a>
### OnDraw

`protected override System.Void OnDraw()`

Inherited resource/node hook; see Description for ownership and lifecycle.

<a id="member-44fa274412c9"></a>
### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`

Inherited resource/node hook; see Description for ownership and lifecycle.

## Events

| Complete signature | Contract |
| --- | --- |
| `public event System.Action TextureChanged` | Occurs after replacing the borrowed texture and scheduling redraw. |

## Events descriptions

<a id="member-2b72db030bb6"></a>
### TextureChanged

`public event System.Action TextureChanged`

Occurs after replacing the borrowed texture and scheduling redraw.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) and [mesh component](../components/meshes.md#repeated-instance-resources) define ownership, typed bounds, callbacks and backend limits. MultiMeshTests checks resource/scene failures and warmed replay; MultiMeshRenderingTests checks native pixels, actual intermediate poses, owned identities and 256-instance active/idle frames. Native allocator totals, structural allocation, dense-scene cost, other targets and owner acceptance remain unverified. Scene-file persistence remains a separate dependency.
