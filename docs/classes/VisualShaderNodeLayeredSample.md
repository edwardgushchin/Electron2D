# VisualShaderNodeLayeredSample

Last updated: 2026-10-22

Source: [VisualShaderNodeLayeredSample.cs](../../src/Scene/Resources/VisualShaderNodeLayeredSample.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeLayeredSample
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

Inherited by: [VisualShaderNodeTextureArray](VisualShaderNodeTextureArray.md).

## Description

Base for independent image-layer sampling in an imported visual graph.

Unconnected coordinates use the current UV and layer zero. Unconnected LOD uses implicit sampling; authored numeric defaults remain retained source data. Port without a connected sampler produces zero.

## Member summary

| Member | Contract |
| --- | --- |
| [`protected VisualShaderNodeLayeredSample()`](#member-f328f3ecba08) | Initializes a property-backed layered sampler. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-6af535771fb9) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeLayeredSample.SourceEnum Source { get; set; }`](#member-52f12c1b4414) | Gets or selects the independent image source. |

<a id="member-f328f3ecba08"></a>
### `.ctor`

```csharp
protected VisualShaderNodeLayeredSample()
```

Initializes a property-backed layered sampler.

<a id="member-6af535771fb9"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-52f12c1b4414"></a>
### `Source`

```csharp
public Electron2D.VisualShaderNodeLayeredSample.SourceEnum Source { get; set; }
```

Gets or selects the independent image source.

Texture initially. UV and layer default to the current UV and layer zero; unconnected Port produces zero.

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
