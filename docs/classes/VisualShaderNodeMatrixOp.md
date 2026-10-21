# VisualShaderNodeMatrixOp

Last updated: 2026-10-22

Source: [VisualShaderNodeMatrixOp.cs](../../src/Scene/Resources/VisualShaderNodeMatrixOp.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeMatrixOp
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Applies native arithmetic to two four-by-four numeric matrix inputs.

Matrix4x4 is numeric shader data. Arithmetic retains native precision and undefined-domain rules; component division requires nonzero divisors for portable results.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeMatrixOp()`](#member-efdebe0c0dd3) | Creates the definition with two explicit identity matrix defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-dc9ab6b3e86e) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-539b28fc8f12) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Operator { get; set; }`](#member-12cb3766339c) | Gets or selects the native operation without changing stored defaults. |

<a id="member-efdebe0c0dd3"></a>
### `.ctor`

```csharp
public VisualShaderNodeMatrixOp()
```

Creates the definition with two explicit identity matrix defaults.

<a id="member-dc9ab6b3e86e"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-539b28fc8f12"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-12cb3766339c"></a>
### `Operator`

```csharp
public Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Operator { get; set; }
```

Gets or selects the native operation without changing stored defaults.

An unchanged selection is quiet. The new value commits before Changed observers execute.

Multiply initially.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The value is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed requests, all sixteen matrix components, copy/archive state, row/column layout and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
