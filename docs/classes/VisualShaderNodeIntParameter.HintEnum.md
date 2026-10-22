# VisualShaderNodeIntParameter.HintEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeIntParameter.cs](../../src/Scene/Resources/VisualShaderNodeIntParameter.cs).

```csharp
public enum Electron2D.VisualShaderNodeIntParameter.HintEnum
```

## Description

The numeric property hint exposed to tooling.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeIntParameter.HintEnum Enum = 3`](#member-6c9948ebd278) | Named integer choices. |
| [`public const Electron2D.VisualShaderNodeIntParameter.HintEnum Max = 4`](#member-e71a30bdfe0d) | The nonselectable hint count. |
| [`public const Electron2D.VisualShaderNodeIntParameter.HintEnum None = 0`](#member-e7ef7ed629f3) | No tooling hint. |
| [`public const Electron2D.VisualShaderNodeIntParameter.HintEnum Range = 1`](#member-131c6410ccf2) | An advisory range. |
| [`public const Electron2D.VisualShaderNodeIntParameter.HintEnum RangeStep = 2`](#member-46a504f14988) | An advisory range with an explicit step. |

<a id="member-6c9948ebd278"></a>
### `Enum`

```csharp
public const Electron2D.VisualShaderNodeIntParameter.HintEnum Enum = 3
```

Named integer choices.

<a id="member-e71a30bdfe0d"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeIntParameter.HintEnum Max = 4
```

The nonselectable hint count.

<a id="member-e7ef7ed629f3"></a>
### `None`

```csharp
public const Electron2D.VisualShaderNodeIntParameter.HintEnum None = 0
```

No tooling hint.

<a id="member-131c6410ccf2"></a>
### `Range`

```csharp
public const Electron2D.VisualShaderNodeIntParameter.HintEnum Range = 1
```

An advisory range.

<a id="member-46a504f14988"></a>
### `RangeStep`

```csharp
public const Electron2D.VisualShaderNodeIntParameter.HintEnum RangeStep = 2
```

An advisory range with an explicit step.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed parameter definitions, copied defaults/hints, storage qualifiers, graph-local references and portable archives. The executable fixtures also observe real software-rendered pixels, typed updates/reset and all sixteen instance slots. See [the component contract](../components/visual-shaders.md#parameters-and-graph-local-references) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
