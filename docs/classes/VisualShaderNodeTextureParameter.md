# VisualShaderNodeTextureParameter

Last updated: 2026-10-22

Source: [VisualShaderNodeTextureParameter.cs](../../src/Scene/Resources/VisualShaderNodeTextureParameter.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeTextureParameter
```

Inherits: [VisualShaderNodeParameter](VisualShaderNodeParameter.md).

Inherited by: [VisualShaderNodeImageParameter](VisualShaderNodeImageParameter.md), [VisualShaderNodeTextureArrayParameter](VisualShaderNodeTextureArrayParameter.md).

## Description

Base for named sampled-image graph parameters and their import-time image policies.

## Member summary

| Member | Contract |
| --- | --- |
| [`protected VisualShaderNodeTextureParameter()`](#member-247ca38682b5) | Initializes a data-image parameter with white fallback and shader-default sampling. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-28c8bf11b9de) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum ColorDefault { get; set; }`](#member-a745cacd6954) | Gets or selects the render-only fallback color. |
| [`public Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum TextureFilter { get; set; }`](#member-37acf8210cea) | Gets or selects the shader sampling filter. |
| [`public Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum TextureRepeat { get; set; }`](#member-1450ab475161) | Gets or selects shader image wrapping. |
| [`public Electron2D.VisualShaderNodeTextureParameter.TextureSourceEnum TextureSource { get; set; }`](#member-d36283a76ddb) | Gets or selects parameter storage or the fragment screen producer. |
| [`public Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum TextureType { get; set; }`](#member-7a6adafa337f) | Gets or selects the sampled image interpretation. |

<a id="member-247ca38682b5"></a>
### `.ctor`

```csharp
protected VisualShaderNodeTextureParameter()
```

Initializes a data-image parameter with white fallback and shader-default sampling.

<a id="member-28c8bf11b9de"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-a745cacd6954"></a>
### `ColorDefault`

```csharp
public Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum ColorDefault { get; set; }
```

Gets or selects the render-only fallback color.

White initially; retained but ignored for normal/flow interpretations. Screen sources require White at import.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selector is undefined or a nonselectable sentinel.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-37acf8210cea"></a>
### `TextureFilter`

```csharp
public Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum TextureFilter { get; set; }
```

Gets or selects the shader sampling filter.

Default initially; independent of CanvasItem parent sampling. An explicit Shader sampler state takes precedence.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selector is undefined or a nonselectable sentinel.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-1450ab475161"></a>
### `TextureRepeat`

```csharp
public Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum TextureRepeat { get; set; }
```

Gets or selects shader image wrapping.

Default initially; independent of CanvasItem parent sampling. An explicit Shader sampler state takes precedence.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selector is undefined or a nonselectable sentinel.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-d36283a76ddb"></a>
### `TextureSource`

```csharp
public Electron2D.VisualShaderNodeTextureParameter.TextureSourceEnum TextureSource { get; set; }
```

Gets or selects parameter storage or the fragment screen producer.

None initially. Incompatible screen shape, stage or image hints reject at import.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selector is undefined or a nonselectable sentinel.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-7a6adafa337f"></a>
### `TextureType`

```csharp
public Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum TextureType { get; set; }
```

Gets or selects the sampled image interpretation.

Data initially. Normal and flow defaults apply only to ordinary images; array images fall back to white.

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
