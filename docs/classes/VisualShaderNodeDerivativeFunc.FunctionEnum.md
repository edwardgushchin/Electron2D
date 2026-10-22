# VisualShaderNodeDerivativeFunc.FunctionEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeDerivativeFunc.cs](../../src/Scene/Resources/VisualShaderNodeDerivativeFunc.cs).

```csharp
public enum Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum
```

## Description

The component-wise screen-space derivative operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum Max = 3`](#member-c187e55c7208) | The nonselectable function-count sentinel. |
| [`public const Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum Sum = 0`](#member-27267e0a6c7c) | Computes absolute X derivative plus absolute Y derivative. |
| [`public const Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum X = 1`](#member-9f20d96d1f8b) | Computes change along the framebuffer X direction. |
| [`public const Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum Y = 2`](#member-c0e9090e476d) | Computes change along the framebuffer Y direction. |

<a id="member-c187e55c7208"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum Max = 3
```

The nonselectable function-count sentinel.

<a id="member-27267e0a6c7c"></a>
### `Sum`

```csharp
public const Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum Sum = 0
```

Computes absolute X derivative plus absolute Y derivative.

<a id="member-9f20d96d1f8b"></a>
### `X`

```csharp
public const Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum X = 1
```

Computes change along the framebuffer X direction.

<a id="member-c0e9090e476d"></a>
### `Y`

```csharp
public const Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum Y = 2
```

Computes change along the framebuffer Y direction.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers derivative layouts/functions/precisions, converted defaults, shared fused arithmetic and portable archives. The executable fixtures also observe real software-rendered pixels, affine rates and fractional antialiasing edges. See [the component contract](../components/visual-shaders.md#screen-space-derivatives) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
