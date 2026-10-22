# VisualShaderNodeTexture.SourceEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeTexture.cs](../../src/Scene/Resources/VisualShaderNodeTexture.cs).

```csharp
public enum Electron2D.VisualShaderNodeTexture.SourceEnum
```

## Description

The sampled image producer.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeTexture.SourceEnum Canvas = 2`](#member-62af1f1b4f27) | The current canvas diffuse image. |
| [`public const Electron2D.VisualShaderNodeTexture.SourceEnum CanvasNormal = 3`](#member-b69e1e6b5b46) | The current canvas normal image. |
| [`public const Electron2D.VisualShaderNodeTexture.SourceEnum Max = 8`](#member-61b964b80f37) | The nonselectable source-table bound. |
| [`public const Electron2D.VisualShaderNodeTexture.SourceEnum Port = 5`](#member-8d621c990997) | The sampler connected to input two. |
| [`public const Electron2D.VisualShaderNodeTexture.SourceEnum Screen = 1`](#member-915bb8f5f10f) | The fragment backbuffer copy. |
| [`public const Electron2D.VisualShaderNodeTexture.SourceEnum Texture = 0`](#member-31e23ebd7813) | The borrowed Texture property. |

<a id="member-62af1f1b4f27"></a>
### `Canvas`

```csharp
public const Electron2D.VisualShaderNodeTexture.SourceEnum Canvas = 2
```

The current canvas diffuse image.

<a id="member-b69e1e6b5b46"></a>
### `CanvasNormal`

```csharp
public const Electron2D.VisualShaderNodeTexture.SourceEnum CanvasNormal = 3
```

The current canvas normal image.

<a id="member-61b964b80f37"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeTexture.SourceEnum Max = 8
```

The nonselectable source-table bound.

<a id="member-8d621c990997"></a>
### `Port`

```csharp
public const Electron2D.VisualShaderNodeTexture.SourceEnum Port = 5
```

The sampler connected to input two.

<a id="member-915bb8f5f10f"></a>
### `Screen`

```csharp
public const Electron2D.VisualShaderNodeTexture.SourceEnum Screen = 1
```

The fragment backbuffer copy.

<a id="member-31e23ebd7813"></a>
### `Texture`

```csharp
public const Electron2D.VisualShaderNodeTexture.SourceEnum Texture = 0
```

The borrowed Texture property.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
