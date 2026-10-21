# VisualShaderNodeClamp.OpTypeEnum

Last updated: 2026-10-21

Source: [VisualShaderNodeClamp.cs](../../src/Scene/Resources/VisualShaderNodeClamp.cs).

```csharp
public enum Electron2D.VisualShaderNodeClamp.OpTypeEnum
```

## Description

The numeric component type processed by this operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Float = 0`](#member-bbd4c06577f7) | A floating scalar. |
| [`public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Int = 1`](#member-d3c547b87547) | A signed integer scalar. |
| [`public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Max = 6`](#member-5c01f5ddad02) | The nonselectable type-count sentinel. |
| [`public const Electron2D.VisualShaderNodeClamp.OpTypeEnum UInt = 2`](#member-91c2c004adfc) | An unsigned integer scalar. |
| [`public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Vector2 = 3`](#member-f68772302abf) | Two floating components. |
| [`public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Vector3 = 4`](#member-2ac8364f0570) | Three floating components. |
| [`public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Vector4 = 5`](#member-8769db1798fc) | Four floating components. |

<a id="member-bbd4c06577f7"></a>
### `Float`

```csharp
public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Float = 0
```

A floating scalar.

<a id="member-d3c547b87547"></a>
### `Int`

```csharp
public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Int = 1
```

A signed integer scalar.

<a id="member-5c01f5ddad02"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Max = 6
```

The nonselectable type-count sentinel.

<a id="member-91c2c004adfc"></a>
### `UInt`

```csharp
public const Electron2D.VisualShaderNodeClamp.OpTypeEnum UInt = 2
```

An unsigned integer scalar.

<a id="member-f68772302abf"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Vector2 = 3
```

Two floating components.

<a id="member-2ac8364f0570"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Vector3 = 4
```

Three floating components.

<a id="member-8769db1798fc"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderNodeClamp.OpTypeEnum Vector4 = 5
```

Four floating components.

## Lifecycle and verification

This numeric selector has value lifetime and is not disposable. Max is a nonselectable count sentinel. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers typed requests, mixed/dormant defaults, copy/archives, multi-output graphs and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
