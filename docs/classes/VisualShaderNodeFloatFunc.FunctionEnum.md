# VisualShaderNodeFloatFunc.FunctionEnum

Last updated: 2026-10-21

Source: [VisualShaderNodeFloatFunc.cs](../../src/Scene/Resources/VisualShaderNodeFloatFunc.cs).

```csharp
public enum Electron2D.VisualShaderNodeFloatFunc.FunctionEnum
```

## Description

The available unary scalar functions with stable numeric identities.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Abs = 12`](#member-7716ca2a4d6d) | Computes absolute value. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Acos = 4`](#member-5021860e0404) | Computes inverse cosine. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Acosh = 20`](#member-00c4922d094a) | Computes inverse hyperbolic cosine. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Asin = 3`](#member-106247466bae) | Computes inverse sine. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Asinh = 21`](#member-179c6c710005) | Computes inverse hyperbolic sine. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Atan = 5`](#member-4e2ebe0ce4bb) | Computes inverse tangent. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Atanh = 22`](#member-6e52e1edd96d) | Computes inverse hyperbolic tangent. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Ceil = 16`](#member-f97e9a2d2925) | Rounds toward positive infinity. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Cos = 1`](#member-c005a7550f8c) | Computes cosine. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Cosh = 7`](#member-28ac17b280f3) | Computes hyperbolic cosine. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Degrees = 23`](#member-e438cb7d6023) | Converts radians to degrees. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Exp = 10`](#member-075720bf606b) | Computes the natural exponential. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Exp2 = 24`](#member-142440f8ad88) | Raises two to the input power. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Floor = 14`](#member-1649f0b3a2a2) | Rounds toward negative infinity. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Fract = 17`](#member-afeef3a770d0) | Subtracts floor from the input. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum InverseSqrt = 25`](#member-ccd73fffcabd) | Computes the reciprocal square root. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Log = 9`](#member-0b5f875b9bae) | Computes the natural logarithm. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Log2 = 26`](#member-6734178ac413) | Computes the base-two logarithm. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Max = 32`](#member-acde4bfff8aa) | The nonselectable function-count sentinel. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Negate = 19`](#member-2680e2f53ab9) | Negates the input. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum OneMinus = 31`](#member-9f6e19ce6f5c) | Subtracts the input from one. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Radians = 27`](#member-11ebf90299a0) | Converts degrees to radians. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Reciprocal = 28`](#member-0e0bbf0029bb) | Divides one by the input. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Round = 15`](#member-2459d8c42ca7) | Rounds to a nearest integer; tie direction follows the shader backend. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum RoundEven = 29`](#member-d34ac1b517d5) | Rounds to a nearest integer with ties to even. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Saturate = 18`](#member-f250f36ec651) | Restricts components to the zero-to-one interval. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Sign = 13`](#member-5d783f59319b) | Returns negative one, zero or one. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Sin = 0`](#member-2212b9f72f51) | Computes sine. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Sinh = 6`](#member-f983f14f607f) | Computes hyperbolic sine. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Sqrt = 11`](#member-4eea808dab1d) | Computes the square root. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Tan = 2`](#member-00366bd3f2d5) | Computes tangent. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Tanh = 8`](#member-dc220b5dedce) | Computes hyperbolic tangent. |
| [`public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Trunc = 30`](#member-3d1829e90cd6) | Rounds toward zero. |

<a id="member-7716ca2a4d6d"></a>
### `Abs`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Abs = 12
```

Computes absolute value.

<a id="member-5021860e0404"></a>
### `Acos`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Acos = 4
```

Computes inverse cosine.

<a id="member-00c4922d094a"></a>
### `Acosh`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Acosh = 20
```

Computes inverse hyperbolic cosine.

<a id="member-106247466bae"></a>
### `Asin`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Asin = 3
```

Computes inverse sine.

<a id="member-179c6c710005"></a>
### `Asinh`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Asinh = 21
```

Computes inverse hyperbolic sine.

<a id="member-4e2ebe0ce4bb"></a>
### `Atan`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Atan = 5
```

Computes inverse tangent.

<a id="member-6e52e1edd96d"></a>
### `Atanh`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Atanh = 22
```

Computes inverse hyperbolic tangent.

<a id="member-f97e9a2d2925"></a>
### `Ceil`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Ceil = 16
```

Rounds toward positive infinity.

<a id="member-c005a7550f8c"></a>
### `Cos`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Cos = 1
```

Computes cosine.

<a id="member-28ac17b280f3"></a>
### `Cosh`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Cosh = 7
```

Computes hyperbolic cosine.

<a id="member-e438cb7d6023"></a>
### `Degrees`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Degrees = 23
```

Converts radians to degrees.

<a id="member-075720bf606b"></a>
### `Exp`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Exp = 10
```

Computes the natural exponential.

<a id="member-142440f8ad88"></a>
### `Exp2`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Exp2 = 24
```

Raises two to the input power.

<a id="member-1649f0b3a2a2"></a>
### `Floor`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Floor = 14
```

Rounds toward negative infinity.

<a id="member-afeef3a770d0"></a>
### `Fract`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Fract = 17
```

Subtracts floor from the input.

<a id="member-ccd73fffcabd"></a>
### `InverseSqrt`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum InverseSqrt = 25
```

Computes the reciprocal square root.

<a id="member-0b5f875b9bae"></a>
### `Log`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Log = 9
```

Computes the natural logarithm.

<a id="member-6734178ac413"></a>
### `Log2`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Log2 = 26
```

Computes the base-two logarithm.

<a id="member-acde4bfff8aa"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Max = 32
```

The nonselectable function-count sentinel.

<a id="member-2680e2f53ab9"></a>
### `Negate`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Negate = 19
```

Negates the input.

<a id="member-9f6e19ce6f5c"></a>
### `OneMinus`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum OneMinus = 31
```

Subtracts the input from one.

<a id="member-11ebf90299a0"></a>
### `Radians`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Radians = 27
```

Converts degrees to radians.

<a id="member-0e0bbf0029bb"></a>
### `Reciprocal`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Reciprocal = 28
```

Divides one by the input.

<a id="member-2459d8c42ca7"></a>
### `Round`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Round = 15
```

Rounds to a nearest integer; tie direction follows the shader backend.

<a id="member-d34ac1b517d5"></a>
### `RoundEven`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum RoundEven = 29
```

Rounds to a nearest integer with ties to even.

<a id="member-f250f36ec651"></a>
### `Saturate`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Saturate = 18
```

Restricts components to the zero-to-one interval.

<a id="member-5d783f59319b"></a>
### `Sign`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Sign = 13
```

Returns negative one, zero or one.

<a id="member-2212b9f72f51"></a>
### `Sin`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Sin = 0
```

Computes sine.

<a id="member-f983f14f607f"></a>
### `Sinh`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Sinh = 6
```

Computes hyperbolic sine.

<a id="member-4eea808dab1d"></a>
### `Sqrt`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Sqrt = 11
```

Computes the square root.

<a id="member-00366bd3f2d5"></a>
### `Tan`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Tan = 2
```

Computes tangent.

<a id="member-dc220b5dedce"></a>
### `Tanh`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Tanh = 8
```

Computes hyperbolic tangent.

<a id="member-3d1829e90cd6"></a>
### `Trunc`

```csharp
public const Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Trunc = 30
```

Rounds toward zero.

## Lifecycle and verification

This owner-specific numeric selector has value lifetime and is not disposable. The Max/EnumSize sentinel is not selectable. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers typed requests, copy/archives, every scalar/vector function and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
