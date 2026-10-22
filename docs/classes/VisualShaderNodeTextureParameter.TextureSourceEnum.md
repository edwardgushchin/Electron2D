# VisualShaderNodeTextureParameter.TextureSourceEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeTextureParameter.cs](../../src/Scene/Resources/VisualShaderNodeTextureParameter.cs).

```csharp
public enum Electron2D.VisualShaderNodeTextureParameter.TextureSourceEnum
```

## Description

The image storage owner.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureSourceEnum Max = 4`](#member-ef1369cd2118) | The nonselectable source-table bound. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureSourceEnum None = 0`](#member-ba9996c2685f) | The material or registered global image. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureSourceEnum Screen = 1`](#member-62f171ac610d) | The renderer-owned fragment backbuffer. |

<a id="member-ef1369cd2118"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureSourceEnum Max = 4
```

The nonselectable source-table bound.

<a id="member-ba9996c2685f"></a>
### `None`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureSourceEnum None = 0
```

The material or registered global image.

<a id="member-62f171ac610d"></a>
### `Screen`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureSourceEnum Screen = 1
```

The renderer-owned fragment backbuffer.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
