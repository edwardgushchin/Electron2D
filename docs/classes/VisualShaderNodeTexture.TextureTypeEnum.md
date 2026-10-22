# VisualShaderNodeTexture.TextureTypeEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeTexture.cs](../../src/Scene/Resources/VisualShaderNodeTexture.cs).

```csharp
public enum Electron2D.VisualShaderNodeTexture.TextureTypeEnum
```

## Description

The interpretation of a property-backed image.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeTexture.TextureTypeEnum Color = 1`](#member-d6f8382531cb) | Encoded color, converted to linear RGB in HDR canvas output. |
| [`public const Electron2D.VisualShaderNodeTexture.TextureTypeEnum Data = 0`](#member-695c00c0eb95) | Raw sampled components. |
| [`public const Electron2D.VisualShaderNodeTexture.TextureTypeEnum Max = 3`](#member-fbfddaa5a863) | The nonselectable type count. |
| [`public const Electron2D.VisualShaderNodeTexture.TextureTypeEnum NormalMap = 2`](#member-8846fac098f1) | Raw normal components and a flat-normal fallback. |

<a id="member-d6f8382531cb"></a>
### `Color`

```csharp
public const Electron2D.VisualShaderNodeTexture.TextureTypeEnum Color = 1
```

Encoded color, converted to linear RGB in HDR canvas output.

<a id="member-695c00c0eb95"></a>
### `Data`

```csharp
public const Electron2D.VisualShaderNodeTexture.TextureTypeEnum Data = 0
```

Raw sampled components.

<a id="member-fbfddaa5a863"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeTexture.TextureTypeEnum Max = 3
```

The nonselectable type count.

<a id="member-8846fac098f1"></a>
### `NormalMap`

```csharp
public const Electron2D.VisualShaderNodeTexture.TextureTypeEnum NormalMap = 2
```

Raw normal components and a flat-normal fallback.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
