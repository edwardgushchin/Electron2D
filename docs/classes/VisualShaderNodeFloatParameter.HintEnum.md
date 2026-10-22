# VisualShaderNodeFloatParameter.HintEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeFloatParameter.cs](../../src/Scene/Resources/VisualShaderNodeFloatParameter.cs).

```csharp
public enum Electron2D.VisualShaderNodeFloatParameter.HintEnum
```

## Description

The numeric property hint exposed to tooling.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeFloatParameter.HintEnum Max = 3`](#member-1c0bd9aab7d8) | The nonselectable hint count. |
| [`public const Electron2D.VisualShaderNodeFloatParameter.HintEnum None = 0`](#member-54c5dac420b0) | No tooling hint. |
| [`public const Electron2D.VisualShaderNodeFloatParameter.HintEnum Range = 1`](#member-bd53a6b77583) | An advisory range. |
| [`public const Electron2D.VisualShaderNodeFloatParameter.HintEnum RangeStep = 2`](#member-f581d9002eaf) | An advisory range with an explicit step. |

<a id="member-1c0bd9aab7d8"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeFloatParameter.HintEnum Max = 3
```

The nonselectable hint count.

<a id="member-54c5dac420b0"></a>
### `None`

```csharp
public const Electron2D.VisualShaderNodeFloatParameter.HintEnum None = 0
```

No tooling hint.

<a id="member-bd53a6b77583"></a>
### `Range`

```csharp
public const Electron2D.VisualShaderNodeFloatParameter.HintEnum Range = 1
```

An advisory range.

<a id="member-f581d9002eaf"></a>
### `RangeStep`

```csharp
public const Electron2D.VisualShaderNodeFloatParameter.HintEnum RangeStep = 2
```

An advisory range with an explicit step.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed parameter definitions, copied defaults/hints, storage qualifiers, graph-local references and portable archives. The executable fixtures also observe real software-rendered pixels, typed updates/reset and all sixteen instance slots. See [the component contract](../components/visual-shaders.md#parameters-and-graph-local-references) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
