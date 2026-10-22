# VisualShaderNodeTextureArray

Last updated: 2026-10-22

Source: [VisualShaderNodeTextureArray.cs](../../src/Scene/Resources/VisualShaderNodeTextureArray.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeTextureArray
```

Inherits: [VisualShaderNodeLayeredSample](VisualShaderNodeLayeredSample.md).

## Description

Samples independently stored image layers using numeric UV and layer coordinates.

## Example

This authoring snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var graph = new VisualShader();
using var node = new VisualShaderNodeTextureArray();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
ResourceSaver.Save(graph, "res://Shaders/image-source.e2dres", SaverFlags.BundleResources);
```

Import the saved source through [the graph workflow](../components/visual-shaders.md#author-import-and-execute). Load the compiled resource and assign it to ShaderMaterial; named images use its existing typed setters or shader defaults.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeTextureArray()`](#member-f2c75589afa2) | Creates a property-backed array-image sampler. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-6ec3d6b0883c) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-4d12a586cb2b) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-f477834a851d) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.TextureLayered TextureArray { get; set; }`](#member-bbc6d594da1f) | Gets or assigns the borrowed independent image array. |

<a id="member-f2c75589afa2"></a>
### `.ctor`

```csharp
public VisualShaderNodeTextureArray()
```

Creates a property-backed array-image sampler.

<a id="member-6ec3d6b0883c"></a>
### `CopyCustomStateTo`

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)
```

<a id="member-4d12a586cb2b"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-f477834a851d"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-bbc6d594da1f"></a>
### `TextureArray`

```csharp
public Electron2D.TextureLayered TextureArray { get; set; }
```

Gets or assigns the borrowed independent image array.

Null initially; import uses the image fallback when absent. Changes require reimport.

Exceptions:

- `T:System.ObjectDisposedException`: The node or supplied resource is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
