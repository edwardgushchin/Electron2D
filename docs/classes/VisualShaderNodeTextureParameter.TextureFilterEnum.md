# VisualShaderNodeTextureParameter.TextureFilterEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeTextureParameter.cs](../../src/Scene/Resources/VisualShaderNodeTextureParameter.cs).

```csharp
public enum Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum
```

## Description

The shader sampler filtering policy.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum Default = 0`](#member-ecbe42e8e3e0) | The canvas shader default: linear base-level sampling. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum Linear = 2`](#member-c6874ec9c3b8) | Linear base-level sampling. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum LinearMipmap = 4`](#member-b359b05a1e03) | Linear texels with mip levels under the project mip-selection policy. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum LinearMipmapAnisotropic = 6`](#member-a75503728e87) | Linear mipmapped sampling with anisotropy. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum Max = 7`](#member-8b862c9bb607) | The nonselectable filter count. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum Nearest = 1`](#member-91e7c7934705) | Nearest base-level sampling. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum NearestMipmap = 3`](#member-f54dff7f7e6d) | Nearest texels with mip levels under the project mip-selection policy. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum NearestMipmapAnisotropic = 5`](#member-d5714a296d64) | Nearest mipmapped sampling with anisotropy. |

<a id="member-ecbe42e8e3e0"></a>
### `Default`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum Default = 0
```

The canvas shader default: linear base-level sampling.

<a id="member-c6874ec9c3b8"></a>
### `Linear`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum Linear = 2
```

Linear base-level sampling.

<a id="member-b359b05a1e03"></a>
### `LinearMipmap`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum LinearMipmap = 4
```

Linear texels with mip levels under the project mip-selection policy.

<a id="member-a75503728e87"></a>
### `LinearMipmapAnisotropic`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum LinearMipmapAnisotropic = 6
```

Linear mipmapped sampling with anisotropy.

<a id="member-8b862c9bb607"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum Max = 7
```

The nonselectable filter count.

<a id="member-91e7c7934705"></a>
### `Nearest`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum Nearest = 1
```

Nearest base-level sampling.

<a id="member-f54dff7f7e6d"></a>
### `NearestMipmap`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum NearestMipmap = 3
```

Nearest texels with mip levels under the project mip-selection policy.

<a id="member-d5714a296d64"></a>
### `NearestMipmapAnisotropic`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureFilterEnum NearestMipmapAnisotropic = 5
```

Nearest mipmapped sampling with anisotropy.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
