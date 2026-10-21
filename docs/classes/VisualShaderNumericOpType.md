# VisualShaderNumericOpType

Last updated: 2026-10-21

Source: [VisualShaderNumericOpType.cs](../../src/Scene/Resources/VisualShaderNumericOpType.cs).

```csharp
public enum Electron2D.VisualShaderNumericOpType
```

## Description

The floating component layout shared by range, threshold and interpolation graph operations.

Scalar-control layouts keep the operation's control inputs scalar while its data inputs and output use the selected vector width.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNumericOpType Max = 7`](#member-224cd56a0ce0) | The nonselectable layout-count sentinel. |
| [`public const Electron2D.VisualShaderNumericOpType Scalar = 0`](#member-52162213e3cc) | Scalar data and scalar controls. |
| [`public const Electron2D.VisualShaderNumericOpType Vector2 = 1`](#member-d4e01597a5cf) | Two-component data and controls. |
| [`public const Electron2D.VisualShaderNumericOpType Vector2Scalar = 2`](#member-ebed9451e85a) | Two-component data with scalar controls. |
| [`public const Electron2D.VisualShaderNumericOpType Vector3 = 3`](#member-42c7c04d730c) | Three-component data and controls. |
| [`public const Electron2D.VisualShaderNumericOpType Vector3Scalar = 4`](#member-5eb43f9ac7b8) | Three-component data with scalar controls. |
| [`public const Electron2D.VisualShaderNumericOpType Vector4 = 5`](#member-22a0fb1c36d1) | Four-component data and controls. |
| [`public const Electron2D.VisualShaderNumericOpType Vector4Scalar = 6`](#member-853e2414b507) | Four-component data with scalar controls. |

<a id="member-224cd56a0ce0"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNumericOpType Max = 7
```

The nonselectable layout-count sentinel.

<a id="member-52162213e3cc"></a>
### `Scalar`

```csharp
public const Electron2D.VisualShaderNumericOpType Scalar = 0
```

Scalar data and scalar controls.

<a id="member-d4e01597a5cf"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderNumericOpType Vector2 = 1
```

Two-component data and controls.

<a id="member-ebed9451e85a"></a>
### `Vector2Scalar`

```csharp
public const Electron2D.VisualShaderNumericOpType Vector2Scalar = 2
```

Two-component data with scalar controls.

<a id="member-42c7c04d730c"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderNumericOpType Vector3 = 3
```

Three-component data and controls.

<a id="member-5eb43f9ac7b8"></a>
### `Vector3Scalar`

```csharp
public const Electron2D.VisualShaderNumericOpType Vector3Scalar = 4
```

Three-component data with scalar controls.

<a id="member-22a0fb1c36d1"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderNumericOpType Vector4 = 5
```

Four-component data and controls.

<a id="member-853e2414b507"></a>
### `Vector4Scalar`

```csharp
public const Electron2D.VisualShaderNumericOpType Vector4Scalar = 6
```

Four-component data with scalar controls.

## Lifecycle and verification

This numeric selector has value lifetime and is not disposable. Max is a nonselectable count sentinel. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers typed requests, mixed/dormant defaults, copy/archives, multi-output graphs and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
