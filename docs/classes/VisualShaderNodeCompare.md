# VisualShaderNodeCompare

Last updated: 2026-10-22

Source: [VisualShaderNodeCompare.cs](../../src/Scene/Resources/VisualShaderNodeCompare.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeCompare
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Compares two numeric graph inputs and produces a Boolean scalar.

Floating equality uses a strict absolute tolerance. Vector comparisons reduce exact component results with All or Any. Boolean and matrix ordering functions produce false. The tolerance default remains stored while inactive.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeCompare()`](#member-9afb073822b7) | Creates a floating equality definition with zero operands and a 0.00001 tolerance. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-24bea080f260) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-6e6791b9547f) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeCompare.ConditionEnum Condition { get; set; }`](#member-5ee1466c5f5a) | Gets or selects the vector reduction, retained independently of the input type. |
| [`public Electron2D.VisualShaderNodeCompare.FunctionEnum Function { get; set; }`](#member-b6033260f3a5) | Gets or selects the comparison function without changing stored defaults. |
| [`public Electron2D.VisualShaderValueType Type { get; set; }`](#member-923fb52b555b) | Gets or selects the compared value type, converting both operand defaults atomically. |

<a id="member-9afb073822b7"></a>
### `.ctor`

```csharp
public VisualShaderNodeCompare()
```

Creates a floating equality definition with zero operands and a 0.00001 tolerance.

<a id="member-24bea080f260"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-6e6791b9547f"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-5ee1466c5f5a"></a>
### `Condition`

```csharp
public Electron2D.VisualShaderNodeCompare.ConditionEnum Condition { get; set; }
```

Gets or selects the vector reduction, retained independently of the input type.

Invalid values leave state unchanged. Unchanged selection is quiet; observers run after commit.

All initially; scalar comparisons ignore this selection.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The condition is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-b6033260f3a5"></a>
### `Function`

```csharp
public Electron2D.VisualShaderNodeCompare.FunctionEnum Function { get; set; }
```

Gets or selects the comparison function without changing stored defaults.

Invalid values leave state unchanged. Unchanged selection is quiet; observers run after commit.

Equal initially. Only Float Equal/NotEqual expose the third tolerance input.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The function is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-923fb52b555b"></a>
### `Type`

```csharp
public Electron2D.VisualShaderValueType Type { get; set; }
```

Gets or selects the compared value type, converting both operand defaults atomically.

Vector expansion repeats the last component. Boolean conversions use the target's initial default. Matrix layout changes use identity operands; conversions from matrices use the target defaults. Repeated selection is quiet; Changed runs after the new type and defaults commit.

Float initially. A changed type recreates absent operands and preserves the dormant tolerance.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selection is undefined or the count sentinel.
- `T:System.OverflowException`: An operand cannot be represented by the target integer type; state is unchanged.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed requests, all sixteen matrix components, copy/archive state, row/column layout and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
