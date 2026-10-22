# VisualShaderNodeParameter.QualifierEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeParameter.cs](../../src/Scene/Resources/VisualShaderNodeParameter.cs).

```csharp
public enum Electron2D.VisualShaderNodeParameter.QualifierEnum
```

## Description

The storage owner of a graph parameter.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeParameter.QualifierEnum Global = 1`](#member-3180c40aff35) | A renderer-session global value. |
| [`public const Electron2D.VisualShaderNodeParameter.QualifierEnum Instance = 2`](#member-1335c07452f0) | One value per canvas item, with an automatically assigned slot. |
| [`public const Electron2D.VisualShaderNodeParameter.QualifierEnum InstanceIndex = 3`](#member-ce0aad54f458) | One value per canvas item, with an explicit slot. |
| [`public const Electron2D.VisualShaderNodeParameter.QualifierEnum Max = 4`](#member-271e340ddaba) | The nonselectable qualifier count. |
| [`public const Electron2D.VisualShaderNodeParameter.QualifierEnum None = 0`](#member-2aba3c225187) | One value per shared material. |

<a id="member-3180c40aff35"></a>
### `Global`

```csharp
public const Electron2D.VisualShaderNodeParameter.QualifierEnum Global = 1
```

A renderer-session global value.

<a id="member-1335c07452f0"></a>
### `Instance`

```csharp
public const Electron2D.VisualShaderNodeParameter.QualifierEnum Instance = 2
```

One value per canvas item, with an automatically assigned slot.

<a id="member-ce0aad54f458"></a>
### `InstanceIndex`

```csharp
public const Electron2D.VisualShaderNodeParameter.QualifierEnum InstanceIndex = 3
```

One value per canvas item, with an explicit slot.

<a id="member-271e340ddaba"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeParameter.QualifierEnum Max = 4
```

The nonselectable qualifier count.

<a id="member-2aba3c225187"></a>
### `None`

```csharp
public const Electron2D.VisualShaderNodeParameter.QualifierEnum None = 0
```

One value per shared material.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed parameter definitions, copied defaults/hints, storage qualifiers, graph-local references and portable archives. The executable fixtures also observe real software-rendered pixels, typed updates/reset and all sixteen instance slots. See [the component contract](../components/visual-shaders.md#parameters-and-graph-local-references) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
