# VisualShaderNodeTextureParameter.TextureTypeEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeTextureParameter.cs](../../src/Scene/Resources/VisualShaderNodeTextureParameter.cs).

```csharp
public enum Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum
```

## Description

The sampled image interpretation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum Anisotropy = 3`](#member-03e83e4882da) | Raw flow-map components. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum Color = 1`](#member-60e44512be70) | Encoded RGB, converted to linear in HDR canvas output. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum Data = 0`](#member-e40e4ad4b43d) | Raw components. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum Max = 4`](#member-88fdba03bb07) | The nonselectable type count. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum NormalMap = 2`](#member-774179b790c6) | Raw normal components. |

<a id="member-03e83e4882da"></a>
### `Anisotropy`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum Anisotropy = 3
```

Raw flow-map components.

<a id="member-60e44512be70"></a>
### `Color`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum Color = 1
```

Encoded RGB, converted to linear in HDR canvas output.

<a id="member-e40e4ad4b43d"></a>
### `Data`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum Data = 0
```

Raw components.

<a id="member-88fdba03bb07"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum Max = 4
```

The nonselectable type count.

<a id="member-774179b790c6"></a>
### `NormalMap`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.TextureTypeEnum NormalMap = 2
```

Raw normal components.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
