# VisualShaderNodeParticleMeshEmitter

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleMeshEmitter.cs](../../src/Scene/Resources/VisualShaderNodeParticleMeshEmitter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParticleMeshEmitter
```

Inherits: [VisualShaderNodeParticleEmitter](VisualShaderNodeParticleEmitter.md).

## Description

Samples aligned vertex channels from selected surfaces of a borrowed planar mesh.

Import bakes only connected channels into owned numeric images. Missing channels and empty meshes produce zero. Surface order and vertex order are retained; index buffers do not weight vertex selection.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeParticleMeshEmitter`](#member-7f71eac241d8) | Samples aligned vertex channels from selected surfaces of a borrowed planar mesh. |
| [`public VisualShaderNodeParticleMeshEmitter()`](#member-f1ec46832f05) | Creates an empty emitter selecting all surfaces. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-34b5f0f606f3) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-7ad75a449445) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-b9bec4f70259) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-2e3f6d4bab12) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.Mesh Mesh { get; set; }`](#member-a9c3b70b3472) | Gets or sets the borrowed mesh defining the emission channels. |
| [`public System.Int32 SurfaceIndex { get; set; }`](#member-ab5040cdff85) | Gets or sets the selected surface when UseAllSurfaces is false. |
| [`public System.Boolean UseAllSurfaces { get; set; }`](#member-224c3a6ae5ed) | Gets or sets whether every surface contributes vertices in source order. |

<a id="member-7f71eac241d8"></a>
### `VisualShaderNodeParticleMeshEmitter`

```csharp
public sealed class Electron2D.VisualShaderNodeParticleMeshEmitter
```

Samples aligned vertex channels from selected surfaces of a borrowed planar mesh.

Import bakes only connected channels into owned numeric images. Missing channels and empty meshes produce zero. Surface order and vertex order are retained; index buffers do not weight vertex selection.

<a id="member-f1ec46832f05"></a>
### `.ctor`

```csharp
public VisualShaderNodeParticleMeshEmitter()
```

Creates an empty emitter selecting all surfaces.

<a id="member-34b5f0f606f3"></a>
### `CopyCustomStateTo`

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)
```

<a id="member-7ad75a449445"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-b9bec4f70259"></a>
### `Dispose`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-2e3f6d4bab12"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-a9c3b70b3472"></a>
### `Mesh`

```csharp
public Electron2D.Mesh Mesh { get; set; }
```

Gets or sets the borrowed mesh defining the emission channels.

Null initially; the caller retains ownership.

Exceptions:

- `T:System.ObjectDisposedException`: The node or supplied mesh is disposed.

<a id="member-ab5040cdff85"></a>
### `SurfaceIndex`

```csharp
public System.Int32 SurfaceIndex { get; set; }
```

Gets or sets the selected surface when UseAllSurfaces is false.

Zero initially; an absent surface produces an empty emitter.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The index is negative.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-224c3a6ae5ed"></a>
### `UseAllSurfaces`

```csharp
public System.Boolean UseAllSurfaces { get; set; }
```

Gets or sets whether every surface contributes vertices in source order.

True initially; SurfaceIndex is retained while inactive.

Exceptions:

- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
