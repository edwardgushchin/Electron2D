# VisualShaderNodeMultiplyAdd.OpTypeEnum

Last updated: 2026-10-21

Source: [VisualShaderNodeMultiplyAdd.cs](../../src/Scene/Resources/VisualShaderNodeMultiplyAdd.cs).

```csharp
public enum Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum
```

## Description

The numeric component type processed by this operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Max = 4`](#member-bb41df54691a) | The nonselectable type-count sentinel. |
| [`public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Scalar = 0`](#member-e32755603e9b) | A floating scalar. |
| [`public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Vector2 = 1`](#member-96f0ccb8e833) | Two floating components. |
| [`public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Vector3 = 2`](#member-489c20f5fdd8) | Three floating components. |
| [`public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Vector4 = 3`](#member-351bf09caffd) | Four floating components. |

<a id="member-bb41df54691a"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Max = 4
```

The nonselectable type-count sentinel.

<a id="member-e32755603e9b"></a>
### `Scalar`

```csharp
public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Scalar = 0
```

A floating scalar.

<a id="member-96f0ccb8e833"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Vector2 = 1
```

Two floating components.

<a id="member-489c20f5fdd8"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Vector3 = 2
```

Three floating components.

<a id="member-351bf09caffd"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum Vector4 = 3
```

Four floating components.

## Lifecycle and verification

This numeric selector has value lifetime and is not disposable. Max is a nonselectable count sentinel. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers typed requests, mixed/dormant defaults, copy/archives, multi-output graphs and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
