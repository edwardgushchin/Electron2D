# VisualShaderNodeLayeredSample.SourceEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeLayeredSample.cs](../../src/Scene/Resources/VisualShaderNodeLayeredSample.cs).

```csharp
public enum Electron2D.VisualShaderNodeLayeredSample.SourceEnum
```

## Description

The independent image-layer producer.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeLayeredSample.SourceEnum Max = 2`](#member-f75d60736f3e) | The nonselectable source count. |
| [`public const Electron2D.VisualShaderNodeLayeredSample.SourceEnum Port = 1`](#member-9741ea8f63b5) | The sampler connected to input two. |
| [`public const Electron2D.VisualShaderNodeLayeredSample.SourceEnum Texture = 0`](#member-785dbe029aea) | The borrowed image array. |

<a id="member-f75d60736f3e"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeLayeredSample.SourceEnum Max = 2
```

The nonselectable source count.

<a id="member-9741ea8f63b5"></a>
### `Port`

```csharp
public const Electron2D.VisualShaderNodeLayeredSample.SourceEnum Port = 1
```

The sampler connected to input two.

<a id="member-785dbe029aea"></a>
### `Texture`

```csharp
public const Electron2D.VisualShaderNodeLayeredSample.SourceEnum Texture = 0
```

The borrowed image array.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
