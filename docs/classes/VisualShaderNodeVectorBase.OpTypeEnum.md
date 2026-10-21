# VisualShaderNodeVectorBase.OpTypeEnum

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorBase.cs](../../src/Scene/Resources/VisualShaderNodeVectorBase.cs).

```csharp
public enum Electron2D.VisualShaderNodeVectorBase.OpTypeEnum
```

## Description

The number of numeric components processed by a vector operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeVectorBase.OpTypeEnum Max = 3`](#member-a687b6c51a3f) | The nonselectable component-count sentinel. |
| [`public const Electron2D.VisualShaderNodeVectorBase.OpTypeEnum Vector2 = 0`](#member-e28ac096414a) | Two numeric components. |
| [`public const Electron2D.VisualShaderNodeVectorBase.OpTypeEnum Vector3 = 1`](#member-668cf07e1d95) | Three numeric components. |
| [`public const Electron2D.VisualShaderNodeVectorBase.OpTypeEnum Vector4 = 2`](#member-293a1802aab3) | Four numeric components. |

<a id="member-a687b6c51a3f"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeVectorBase.OpTypeEnum Max = 3
```

The nonselectable component-count sentinel.

<a id="member-e28ac096414a"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderNodeVectorBase.OpTypeEnum Vector2 = 0
```

Two numeric components.

<a id="member-668cf07e1d95"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderNodeVectorBase.OpTypeEnum Vector3 = 1
```

Three numeric components.

<a id="member-293a1802aab3"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderNodeVectorBase.OpTypeEnum Vector4 = 2
```

Four numeric components.

## Lifecycle and verification

This owner-specific numeric selector has value lifetime and is not disposable. The Max/EnumSize sentinel is not selectable. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers typed requests, copy/archives, every scalar/vector function and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
