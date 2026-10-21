# VisualShaderIntegerOperator

Last updated: 2026-10-21

Source: [VisualShaderIntegerOperator.cs](../../src/Scene/Resources/VisualShaderIntegerOperator.cs).

```csharp
public enum Electron2D.VisualShaderIntegerOperator
```

## Description

The integer operation shared by signed and unsigned numeric graph definitions.

The input node determines signed interpretation. Division, ordering and right shifts retain that domain's native rules.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderIntegerOperator Add = 0`](#member-c1f2f44ddda3) | Adds the two integer inputs. |
| [`public const Electron2D.VisualShaderIntegerOperator BitwiseAnd = 7`](#member-7cdc2ffbf4eb) | Keeps bits set in both inputs. |
| [`public const Electron2D.VisualShaderIntegerOperator BitwiseLeftShift = 10`](#member-c3683664db25) | Shifts the first input left by the second input, discarding high bits. |
| [`public const Electron2D.VisualShaderIntegerOperator BitwiseOr = 8`](#member-c8256023e184) | Keeps bits set in either input. |
| [`public const Electron2D.VisualShaderIntegerOperator BitwiseRightShift = 11`](#member-e0350e295a27) | Shifts right with sign extension for signed inputs and zero fill for unsigned inputs. |
| [`public const Electron2D.VisualShaderIntegerOperator BitwiseXor = 9`](#member-c73def712261) | Keeps bits set in exactly one input. |
| [`public const Electron2D.VisualShaderIntegerOperator Divide = 3`](#member-f4aee4b5359a) | Divides the first input by the second; signed division truncates toward zero. |
| [`public const Electron2D.VisualShaderIntegerOperator EnumSize = 12`](#member-6b6f1a40773b) | The nonselectable operator-count sentinel. |
| [`public const Electron2D.VisualShaderIntegerOperator Max = 5`](#member-d0aa18890174) | Selects the larger integer input. |
| [`public const Electron2D.VisualShaderIntegerOperator Min = 6`](#member-4331e1f63caa) | Selects the smaller integer input. |
| [`public const Electron2D.VisualShaderIntegerOperator Modulo = 4`](#member-55ccbd1206b9) | Computes the native integer remainder; portable canvas execution requires nonnegative operands and a nonzero divisor. |
| [`public const Electron2D.VisualShaderIntegerOperator Multiply = 2`](#member-0fbe5b8a1676) | Multiplies the two integer inputs. |
| [`public const Electron2D.VisualShaderIntegerOperator Subtract = 1`](#member-a9f215f5c6cb) | Subtracts the second integer input from the first. |

<a id="member-c1f2f44ddda3"></a>
### `Add`

```csharp
public const Electron2D.VisualShaderIntegerOperator Add = 0
```

Adds the two integer inputs.

<a id="member-7cdc2ffbf4eb"></a>
### `BitwiseAnd`

```csharp
public const Electron2D.VisualShaderIntegerOperator BitwiseAnd = 7
```

Keeps bits set in both inputs.

<a id="member-c3683664db25"></a>
### `BitwiseLeftShift`

```csharp
public const Electron2D.VisualShaderIntegerOperator BitwiseLeftShift = 10
```

Shifts the first input left by the second input, discarding high bits.

<a id="member-c8256023e184"></a>
### `BitwiseOr`

```csharp
public const Electron2D.VisualShaderIntegerOperator BitwiseOr = 8
```

Keeps bits set in either input.

<a id="member-e0350e295a27"></a>
### `BitwiseRightShift`

```csharp
public const Electron2D.VisualShaderIntegerOperator BitwiseRightShift = 11
```

Shifts right with sign extension for signed inputs and zero fill for unsigned inputs.

<a id="member-c73def712261"></a>
### `BitwiseXor`

```csharp
public const Electron2D.VisualShaderIntegerOperator BitwiseXor = 9
```

Keeps bits set in exactly one input.

<a id="member-f4aee4b5359a"></a>
### `Divide`

```csharp
public const Electron2D.VisualShaderIntegerOperator Divide = 3
```

Divides the first input by the second; signed division truncates toward zero.

<a id="member-6b6f1a40773b"></a>
### `EnumSize`

```csharp
public const Electron2D.VisualShaderIntegerOperator EnumSize = 12
```

The nonselectable operator-count sentinel.

<a id="member-d0aa18890174"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderIntegerOperator Max = 5
```

Selects the larger integer input.

<a id="member-4331e1f63caa"></a>
### `Min`

```csharp
public const Electron2D.VisualShaderIntegerOperator Min = 6
```

Selects the smaller integer input.

<a id="member-55ccbd1206b9"></a>
### `Modulo`

```csharp
public const Electron2D.VisualShaderIntegerOperator Modulo = 4
```

Computes the native integer remainder; portable canvas execution requires nonnegative operands and a nonzero divisor.

<a id="member-0fbe5b8a1676"></a>
### `Multiply`

```csharp
public const Electron2D.VisualShaderIntegerOperator Multiply = 2
```

Multiplies the two integer inputs.

<a id="member-a9f215f5c6cb"></a>
### `Subtract`

```csharp
public const Electron2D.VisualShaderIntegerOperator Subtract = 1
```

Subtracts the second integer input from the first.

## Lifecycle and verification

This value type is shared by the signed and unsigned operation resources. The operand kind selects native signed/unsigned arithmetic. EnumSize is nonselectable; Max executes the maximum operation.

The public VisualShader.Consumer covers every signed/unsigned selector, exact default bits and absence, copy/archives, byte-encoded software pixels in both stages, and UV-derived integer computation. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
