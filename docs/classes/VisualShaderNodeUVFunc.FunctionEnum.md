# VisualShaderNodeUVFunc.FunctionEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeUVFunc.cs](../../src/Scene/Resources/VisualShaderNodeUVFunc.cs).

```csharp
public enum Electron2D.VisualShaderNodeUVFunc.FunctionEnum
```

## Description

The coordinate operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public enum Electron2D.VisualShaderNodeUVFunc.FunctionEnum`](#member-50111ef4d480) | The coordinate operation. |
| [`public const Electron2D.VisualShaderNodeUVFunc.FunctionEnum Max = 2`](#member-a24d91ed91ff) | The nonselectable function count. |
| [`public const Electron2D.VisualShaderNodeUVFunc.FunctionEnum Panning = 0`](#member-93213ebf765c) | Adds offset multiplied by scale to UV. |
| [`public const Electron2D.VisualShaderNodeUVFunc.FunctionEnum Scaling = 1`](#member-36b5b81221cb) | Scales UV around a pivot. |

<a id="member-50111ef4d480"></a>
### `FunctionEnum`

```csharp
public enum Electron2D.VisualShaderNodeUVFunc.FunctionEnum
```

The coordinate operation.

<a id="member-a24d91ed91ff"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeUVFunc.FunctionEnum Max = 2
```

The nonselectable function count.

<a id="member-93213ebf765c"></a>
### `Panning`

```csharp
public const Electron2D.VisualShaderNodeUVFunc.FunctionEnum Panning = 0
```

Adds offset multiplied by scale to UV.

<a id="member-36b5b81221cb"></a>
### `Scaling`

```csharp
public const Electron2D.VisualShaderNodeUVFunc.FunctionEnum Scaling = 1
```

Scales UV around a pivot.

## Lifecycle and verification

This owner-specific selector has value lifetime. Max is a nonselectable bound; undefined casts are rejected by its owning property.

The public VisualShader.Consumer exercises both canvas stages, selector transitions, errors, source/compiled archives and composed image-effect materials. See [the operation contract](../components/visual-shaders.md#uv-and-color-image-effects) for formulas, native arithmetic and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
