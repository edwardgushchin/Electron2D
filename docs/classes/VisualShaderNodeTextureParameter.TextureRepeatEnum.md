# VisualShaderNodeTextureParameter.TextureRepeatEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeTextureParameter.cs](../../src/Scene/Resources/VisualShaderNodeTextureParameter.cs).

```csharp
public enum Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum
```

## Description

The shader image wrapping policy.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum Default = 0`](#member-c64bb443bcf6) | The canvas shader default: clamp to edge. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum Disabled = 2`](#member-00c40b6a9101) | Clamp UV coordinates to the image edge. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum Enabled = 1`](#member-c6ae139d0250) | Repeat UV coordinates. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum Max = 3`](#member-e7f329dfd13e) | The nonselectable repeat count. |

<a id="member-c64bb443bcf6"></a>
### `Default`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum Default = 0
```

The canvas shader default: clamp to edge.

<a id="member-00c40b6a9101"></a>
### `Disabled`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum Disabled = 2
```

Clamp UV coordinates to the image edge.

<a id="member-c6ae139d0250"></a>
### `Enabled`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum Enabled = 1
```

Repeat UV coordinates.

<a id="member-e7f329dfd13e"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureRepeatEnum Max = 3
```

The nonselectable repeat count.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
