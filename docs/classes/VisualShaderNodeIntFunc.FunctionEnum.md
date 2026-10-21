# VisualShaderNodeIntFunc.FunctionEnum

Last updated: 2026-10-21

Source: [VisualShaderNodeIntFunc.cs](../../src/Scene/Resources/VisualShaderNodeIntFunc.cs).

```csharp
public enum Electron2D.VisualShaderNodeIntFunc.FunctionEnum
```

## Description

The selected integer function.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum Abs = 0`](#member-635d8cf571e3) | Returns the native signed absolute value. |
| [`public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum BitwiseNot = 3`](#member-eecab3ae9185) | Complements every input bit. |
| [`public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum Max = 4`](#member-b51e8851ec05) | The nonselectable function-count sentinel. |
| [`public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum Negate = 1`](#member-1a2a7d97d9c6) | Negates the signed input. |
| [`public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum Sign = 2`](#member-90d604f19703) | Returns negative one, zero or one according to the signed input. |

<a id="member-635d8cf571e3"></a>
### `Abs`

```csharp
public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum Abs = 0
```

Returns the native signed absolute value.

<a id="member-eecab3ae9185"></a>
### `BitwiseNot`

```csharp
public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum BitwiseNot = 3
```

Complements every input bit.

<a id="member-b51e8851ec05"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum Max = 4
```

The nonselectable function-count sentinel.

<a id="member-1a2a7d97d9c6"></a>
### `Negate`

```csharp
public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum Negate = 1
```

Negates the signed input.

<a id="member-90d604f19703"></a>
### `Sign`

```csharp
public const Electron2D.VisualShaderNodeIntFunc.FunctionEnum Sign = 2
```

Returns negative one, zero or one according to the signed input.

## Lifecycle and verification

This numeric selector has value lifetime and is not disposable. Max is a nonselectable count sentinel. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers every signed/unsigned selector, exact default bits and absence, copy/archives, byte-encoded software pixels in both stages, and UV-derived integer computation. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
