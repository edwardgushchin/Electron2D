# VisualShaderFloatOpType

Last updated: 2026-10-22

Source: [VisualShaderFloatOpType.cs](../../src/Scene/Resources/VisualShaderFloatOpType.cs).

```csharp
public enum Electron2D.VisualShaderFloatOpType
```

## Description

The floating component layout shared by fused arithmetic and derivative graph operations.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderFloatOpType Max = 4`](#member-1209278c1fa7) | The nonselectable layout-count sentinel. |
| [`public const Electron2D.VisualShaderFloatOpType Scalar = 0`](#member-71df5058f0d0) | A floating scalar. |
| [`public const Electron2D.VisualShaderFloatOpType Vector2 = 1`](#member-2cea61eddfdb) | Two floating components. |
| [`public const Electron2D.VisualShaderFloatOpType Vector3 = 2`](#member-ca291e911e2a) | Three floating components. |
| [`public const Electron2D.VisualShaderFloatOpType Vector4 = 3`](#member-8c887892b9fb) | Four floating components. |

<a id="member-1209278c1fa7"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderFloatOpType Max = 4
```

The nonselectable layout-count sentinel.

<a id="member-71df5058f0d0"></a>
### `Scalar`

```csharp
public const Electron2D.VisualShaderFloatOpType Scalar = 0
```

A floating scalar.

<a id="member-2cea61eddfdb"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderFloatOpType Vector2 = 1
```

Two floating components.

<a id="member-ca291e911e2a"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderFloatOpType Vector3 = 2
```

Three floating components.

<a id="member-8c887892b9fb"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderFloatOpType Vector4 = 3
```

Four floating components.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers derivative layouts/functions/precisions, converted defaults, shared fused arithmetic and portable archives. The executable fixtures also observe real software-rendered pixels, affine rates and fractional antialiasing edges. See [the component contract](../components/visual-shaders.md#screen-space-derivatives) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
