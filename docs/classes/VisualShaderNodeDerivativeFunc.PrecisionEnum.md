# VisualShaderNodeDerivativeFunc.PrecisionEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeDerivativeFunc.cs](../../src/Scene/Resources/VisualShaderNodeDerivativeFunc.cs).

```csharp
public enum Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum
```

## Description

The neighboring invocation policy requested from the fragment backend.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum Coarse = 1`](#member-526e4421a1bb) | Uses the coarse derivative operation. |
| [`public const Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum Fine = 2`](#member-6f35609b1126) | Uses the fine derivative operation. |
| [`public const Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum Max = 3`](#member-caf3355ced50) | The nonselectable precision-count sentinel. |
| [`public const Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum None = 0`](#member-20ed1f8361c3) | Uses the backend's default derivative policy. |

<a id="member-526e4421a1bb"></a>
### `Coarse`

```csharp
public const Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum Coarse = 1
```

Uses the coarse derivative operation.

<a id="member-6f35609b1126"></a>
### `Fine`

```csharp
public const Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum Fine = 2
```

Uses the fine derivative operation.

<a id="member-caf3355ced50"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum Max = 3
```

The nonselectable precision-count sentinel.

<a id="member-20ed1f8361c3"></a>
### `None`

```csharp
public const Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum None = 0
```

Uses the backend's default derivative policy.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers derivative layouts/functions/precisions, converted defaults, shared fused arithmetic and portable archives. The executable fixtures also observe real software-rendered pixels, affine rates and fractional antialiasing edges. See [the component contract](../components/visual-shaders.md#screen-space-derivatives) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
