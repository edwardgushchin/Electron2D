# VisualShaderNodeMatrixVecMult

Last updated: 2026-10-22

Source: [VisualShaderNodeMatrixVecMult.cs](../../src/Scene/Resources/VisualShaderNodeMatrixVecMult.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeMatrixVecMult
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Multiplies numeric matrix and vector data in either order with an explicit homogeneous component.

Matrix4x4 is numeric shader data. Multiplication retains native precision and the selected operand order.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeMatrixVecMult()`](#member-79c94ac1472e) | Creates the definition with explicit identity matrix and zero vector defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-fad989e0cee9) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-cf82346d5ca0) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum Operator { get; set; }`](#member-61554e150b8e) | Gets or selects the native operation without changing stored defaults. |

<a id="member-79c94ac1472e"></a>
### `.ctor`

```csharp
public VisualShaderNodeMatrixVecMult()
```

Creates the definition with explicit identity matrix and zero vector defaults.

<a id="member-fad989e0cee9"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-cf82346d5ca0"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-61554e150b8e"></a>
### `Operator`

```csharp
public Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum Operator { get; set; }
```

Gets or selects the native operation without changing stored defaults.

An unchanged selection is quiet. The new value commits before Changed observers execute.

MatrixTimesVector initially.

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
