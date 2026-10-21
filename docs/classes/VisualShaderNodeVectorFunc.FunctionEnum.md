# VisualShaderNodeVectorFunc.FunctionEnum

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorFunc.cs](../../src/Scene/Resources/VisualShaderNodeVectorFunc.cs).

```csharp
public enum Electron2D.VisualShaderNodeVectorFunc.FunctionEnum
```

## Description

The available unary vector functions with stable numeric identities.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Abs = 4`](#member-89311f27aa92) | Computes absolute value. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Acos = 5`](#member-ef95a2b1aee2) | Computes inverse cosine. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Acosh = 6`](#member-a453f1a448bd) | Computes inverse hyperbolic cosine. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Asin = 7`](#member-feffe14710fa) | Computes inverse sine. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Asinh = 8`](#member-7ce144d4c109) | Computes inverse hyperbolic sine. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Atan = 9`](#member-fe353062025c) | Computes inverse tangent. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Atanh = 10`](#member-ff7bf987f175) | Computes inverse hyperbolic tangent. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Ceil = 11`](#member-4be7d1408884) | Rounds toward positive infinity. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Cos = 12`](#member-ac9c39c49cf9) | Computes cosine. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Cosh = 13`](#member-9bcb9ae5cf1f) | Computes hyperbolic cosine. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Degrees = 14`](#member-4c2d8aa0d90b) | Converts radians to degrees. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Exp = 15`](#member-7027753fe113) | Computes the natural exponential. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Exp2 = 16`](#member-dad72c248264) | Raises two to the input power. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Floor = 17`](#member-d44b8ea91916) | Rounds toward negative infinity. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Fract = 18`](#member-dc4f402c7cc5) | Subtracts floor from the input. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum InverseSqrt = 19`](#member-567b7e2abf4b) | Computes the reciprocal square root. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Log = 20`](#member-03d9b4e1fed6) | Computes the natural logarithm. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Log2 = 21`](#member-a98647df2dbf) | Computes the base-two logarithm. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Max = 33`](#member-fcf01e7ff536) | The nonselectable function-count sentinel. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Negate = 2`](#member-de69713b2fbe) | Negates the input. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Normalize = 0`](#member-baa59cffab68) | Divides the vector by its length. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum OneMinus = 32`](#member-a836f0e25a15) | Subtracts the input from one. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Radians = 22`](#member-230852562270) | Converts degrees to radians. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Reciprocal = 3`](#member-80759aebecf5) | Divides one by the input. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Round = 23`](#member-67c21bb201cc) | Rounds to a nearest integer; tie direction follows the shader backend. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum RoundEven = 24`](#member-ce63e758f56c) | Rounds to a nearest integer with ties to even. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Saturate = 1`](#member-a21ef2f6c9aa) | Restricts components to the zero-to-one interval. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Sign = 25`](#member-e928045205a4) | Returns negative one, zero or one. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Sin = 26`](#member-4aac1838c5e4) | Computes sine. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Sinh = 27`](#member-855b949a5619) | Computes hyperbolic sine. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Sqrt = 28`](#member-cbdcd746f033) | Computes the square root. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Tan = 29`](#member-aa9df87475b4) | Computes tangent. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Tanh = 30`](#member-4ba48709ba46) | Computes hyperbolic tangent. |
| [`public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Trunc = 31`](#member-17b59acbf088) | Rounds toward zero. |

<a id="member-89311f27aa92"></a>
### `Abs`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Abs = 4
```

Computes absolute value.

<a id="member-ef95a2b1aee2"></a>
### `Acos`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Acos = 5
```

Computes inverse cosine.

<a id="member-a453f1a448bd"></a>
### `Acosh`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Acosh = 6
```

Computes inverse hyperbolic cosine.

<a id="member-feffe14710fa"></a>
### `Asin`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Asin = 7
```

Computes inverse sine.

<a id="member-7ce144d4c109"></a>
### `Asinh`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Asinh = 8
```

Computes inverse hyperbolic sine.

<a id="member-fe353062025c"></a>
### `Atan`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Atan = 9
```

Computes inverse tangent.

<a id="member-ff7bf987f175"></a>
### `Atanh`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Atanh = 10
```

Computes inverse hyperbolic tangent.

<a id="member-4be7d1408884"></a>
### `Ceil`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Ceil = 11
```

Rounds toward positive infinity.

<a id="member-ac9c39c49cf9"></a>
### `Cos`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Cos = 12
```

Computes cosine.

<a id="member-9bcb9ae5cf1f"></a>
### `Cosh`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Cosh = 13
```

Computes hyperbolic cosine.

<a id="member-4c2d8aa0d90b"></a>
### `Degrees`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Degrees = 14
```

Converts radians to degrees.

<a id="member-7027753fe113"></a>
### `Exp`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Exp = 15
```

Computes the natural exponential.

<a id="member-dad72c248264"></a>
### `Exp2`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Exp2 = 16
```

Raises two to the input power.

<a id="member-d44b8ea91916"></a>
### `Floor`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Floor = 17
```

Rounds toward negative infinity.

<a id="member-dc4f402c7cc5"></a>
### `Fract`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Fract = 18
```

Subtracts floor from the input.

<a id="member-567b7e2abf4b"></a>
### `InverseSqrt`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum InverseSqrt = 19
```

Computes the reciprocal square root.

<a id="member-03d9b4e1fed6"></a>
### `Log`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Log = 20
```

Computes the natural logarithm.

<a id="member-a98647df2dbf"></a>
### `Log2`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Log2 = 21
```

Computes the base-two logarithm.

<a id="member-fcf01e7ff536"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Max = 33
```

The nonselectable function-count sentinel.

<a id="member-de69713b2fbe"></a>
### `Negate`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Negate = 2
```

Negates the input.

<a id="member-baa59cffab68"></a>
### `Normalize`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Normalize = 0
```

Divides the vector by its length.

<a id="member-a836f0e25a15"></a>
### `OneMinus`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum OneMinus = 32
```

Subtracts the input from one.

<a id="member-230852562270"></a>
### `Radians`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Radians = 22
```

Converts degrees to radians.

<a id="member-80759aebecf5"></a>
### `Reciprocal`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Reciprocal = 3
```

Divides one by the input.

<a id="member-67c21bb201cc"></a>
### `Round`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Round = 23
```

Rounds to a nearest integer; tie direction follows the shader backend.

<a id="member-ce63e758f56c"></a>
### `RoundEven`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum RoundEven = 24
```

Rounds to a nearest integer with ties to even.

<a id="member-a21ef2f6c9aa"></a>
### `Saturate`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Saturate = 1
```

Restricts components to the zero-to-one interval.

<a id="member-e928045205a4"></a>
### `Sign`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Sign = 25
```

Returns negative one, zero or one.

<a id="member-4aac1838c5e4"></a>
### `Sin`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Sin = 26
```

Computes sine.

<a id="member-855b949a5619"></a>
### `Sinh`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Sinh = 27
```

Computes hyperbolic sine.

<a id="member-cbdcd746f033"></a>
### `Sqrt`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Sqrt = 28
```

Computes the square root.

<a id="member-aa9df87475b4"></a>
### `Tan`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Tan = 29
```

Computes tangent.

<a id="member-4ba48709ba46"></a>
### `Tanh`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Tanh = 30
```

Computes hyperbolic tangent.

<a id="member-17b59acbf088"></a>
### `Trunc`

```csharp
public const Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Trunc = 31
```

Rounds toward zero.

## Lifecycle and verification

This owner-specific numeric selector has value lifetime and is not disposable. The Max/EnumSize sentinel is not selectable. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers typed requests, copy/archives, every scalar/vector function and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
