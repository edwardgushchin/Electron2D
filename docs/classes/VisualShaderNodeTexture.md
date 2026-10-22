# VisualShaderNodeTexture

Last updated: 2026-10-22

Source: [VisualShaderNodeTexture.cs](../../src/Scene/Resources/VisualShaderNodeTexture.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeTexture
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Samples an ordinary image, canvas channel, screen copy or connected sampler in an imported graph.

Unconnected UV uses the current UV (screen coordinates for Screen); unconnected LOD uses implicit sampling. Authored numeric defaults on those ports are retained source data and do not replace these implicit inputs. Port without a connected sampler produces zero. Sources and image interpretation change only the next import.

## Example

This authoring snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var graph = new VisualShader();
using var node = new VisualShaderNodeTexture();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
ResourceSaver.Save(graph, "res://Shaders/image-source.e2dres", SaverFlags.BundleResources);
```

Import the saved source through [the graph workflow](../components/visual-shaders.md#author-import-and-execute). Load the compiled resource and assign it to ShaderMaterial; named images use its existing typed setters or shader defaults.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeTexture()`](#member-32ddbad8b44c) | Creates a property-backed data-image sampler. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-48a1a303d183) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-f736482a46f7) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-fc75439dd9a8) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeTexture.SourceEnum Source { get; set; }`](#member-602e9d2ae05f) | Gets or selects the image producer. |
| [`public Electron2D.Texture Texture { get; set; }`](#member-68355099fe03) | Gets or assigns the borrowed ordinary source image. |
| [`public Electron2D.VisualShaderNodeTexture.TextureTypeEnum TextureType { get; set; }`](#member-e906c566370f) | Gets or selects the property image interpretation. |

<a id="member-32ddbad8b44c"></a>
### `.ctor`

```csharp
public VisualShaderNodeTexture()
```

Creates a property-backed data-image sampler.

<a id="member-48a1a303d183"></a>
### `CopyCustomStateTo`

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)
```

<a id="member-f736482a46f7"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-fc75439dd9a8"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-602e9d2ae05f"></a>
### `Source`

```csharp
public Electron2D.VisualShaderNodeTexture.SourceEnum Source { get; set; }
```

Gets or selects the image producer.

Texture initially. Screen and canvas channels produce zero outside the fragment stage.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selector is undefined or a nonselectable sentinel.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-68355099fe03"></a>
### `Texture`

```csharp
public Electron2D.Texture Texture { get; set; }
```

Gets or assigns the borrowed ordinary source image.

Null initially; import uses the image fallback when absent. Changes require reimport.

Exceptions:

- `T:System.ObjectDisposedException`: The node or supplied resource is disposed.

<a id="member-e906c566370f"></a>
### `TextureType`

```csharp
public Electron2D.VisualShaderNodeTexture.TextureTypeEnum TextureType { get; set; }
```

Gets or selects the property image interpretation.

Data initially; dormant for nonproperty sources.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selector is undefined or a nonselectable sentinel.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
