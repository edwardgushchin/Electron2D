# VisualShaderNodeUIntFunc.FunctionEnum

Last updated: 2026-10-21

Source: [VisualShaderNodeUIntFunc.cs](../../src/Scene/Resources/VisualShaderNodeUIntFunc.cs).

```csharp
public enum Electron2D.VisualShaderNodeUIntFunc.FunctionEnum
```

## Description

The selected integer function.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeUIntFunc.FunctionEnum BitwiseNot = 1`](#member-953f49999913) | Complements every input bit. |
| [`public const Electron2D.VisualShaderNodeUIntFunc.FunctionEnum Max = 2`](#member-2f35e8c23c99) | The nonselectable function-count sentinel. |
| [`public const Electron2D.VisualShaderNodeUIntFunc.FunctionEnum Negate = 0`](#member-c7427fa541ac) | Negates the unsigned input modulo its 32-bit representation. |

<a id="member-953f49999913"></a>
### `BitwiseNot`

```csharp
public const Electron2D.VisualShaderNodeUIntFunc.FunctionEnum BitwiseNot = 1
```

Complements every input bit.

<a id="member-2f35e8c23c99"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeUIntFunc.FunctionEnum Max = 2
```

The nonselectable function-count sentinel.

<a id="member-c7427fa541ac"></a>
### `Negate`

```csharp
public const Electron2D.VisualShaderNodeUIntFunc.FunctionEnum Negate = 0
```

Negates the unsigned input modulo its 32-bit representation.

## Lifecycle and verification

This numeric selector has value lifetime and is not disposable. Max is a nonselectable count sentinel. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers every signed/unsigned selector, exact default bits and absence, copy/archives, byte-encoded software pixels in both stages, and UV-derived integer computation. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
