# VisualShaderNodeTextureArrayParameter

Last updated: 2026-10-22

Source: [VisualShaderNodeTextureArrayParameter.cs](../../src/Scene/Resources/VisualShaderNodeTextureArrayParameter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeTextureArrayParameter
```

Inherits: [VisualShaderNodeTextureParameter](VisualShaderNodeTextureParameter.md).

## Description

Declares a named independent image array sampler in an imported visual graph.

The image is supplied by ShaderMaterial, the shader default or the global registry. Unassigned images use the retained render-only fallback; graph edits require reimport.

## Example

This authoring snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var graph = new VisualShader();
using var node = new VisualShaderNodeTextureArrayParameter { ParameterName = "image_value" };
using var sample = new VisualShaderNodeTextureArray { Source = VisualShaderNodeLayeredSample.SourceEnum.Port };
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.AddNode(VisualShader.Type.Fragment, sample, Vector2.Zero, 3);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 0, 0);
ResourceSaver.Save(graph, "res://Shaders/image-source.e2dres", SaverFlags.BundleResources);
```

Import the saved source through [the graph workflow](../components/visual-shaders.md#author-import-and-execute). Load the compiled resource and assign it to ShaderMaterial; named images use its existing typed setters or shader defaults.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeTextureArrayParameter()`](#member-0bb87acfdbcb) | Creates a data sampler with shader-default filtering and wrapping. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-0e525c34b72e) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-0bb87acfdbcb"></a>
### `.ctor`

```csharp
public VisualShaderNodeTextureArrayParameter()
```

Creates a data sampler with shader-default filtering and wrapping.

<a id="member-0e525c34b72e"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
