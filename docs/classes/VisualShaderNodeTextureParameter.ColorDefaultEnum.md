# VisualShaderNodeTextureParameter.ColorDefaultEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeTextureParameter.cs](../../src/Scene/Resources/VisualShaderNodeTextureParameter.cs).

```csharp
public enum Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum
```

## Description

The render-only fallback for missing data/color images.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum Black = 1`](#member-363ef7b294a7) | Opaque black. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum Max = 3`](#member-8fa059d0422c) | The nonselectable fallback count. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum Transparent = 2`](#member-488b2f1a82b2) | Transparent black. |
| [`public const Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum White = 0`](#member-f9d439cb73ae) | Opaque white. |

<a id="member-363ef7b294a7"></a>
### `Black`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum Black = 1
```

Opaque black.

<a id="member-8fa059d0422c"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum Max = 3
```

The nonselectable fallback count.

<a id="member-488b2f1a82b2"></a>
### `Transparent`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum Transparent = 2
```

Transparent black.

<a id="member-f9d439cb73ae"></a>
### `White`

```csharp
public const Electron2D.VisualShaderNodeTextureParameter.ColorDefaultEnum White = 0
```

Opaque white.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
