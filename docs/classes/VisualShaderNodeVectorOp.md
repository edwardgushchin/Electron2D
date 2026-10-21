# VisualShaderNodeVectorOp

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorOp.cs](../../src/Scene/Resources/VisualShaderNodeVectorOp.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVectorOp
```

Inherits: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

Applies one binary component-wise vector operation to two graph inputs.

Arithmetic executes in the imported shader, including its native NaN/infinity rules.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVectorOp()`](#member-535aceb313b6) | Creates an addition operation with two zero Vector3 defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-c92b3046eb9e) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-1bf1a522679d) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeVectorOp.OperatorEnum Operator { get; set; }`](#member-9ccc1a9b8b97) | Gets or selects the binary vector operation. |

<a id="member-535aceb313b6"></a>
### `.ctor`

```csharp
public VisualShaderNodeVectorOp()
```

Creates an addition operation with two zero Vector3 defaults.

<a id="member-c92b3046eb9e"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-1bf1a522679d"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-9ccc1a9b8b97"></a>
### `Operator`

```csharp
public Electron2D.VisualShaderNodeVectorOp.OperatorEnum Operator { get; set; }
```

Gets or selects the binary vector operation.

Add initially.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The operation is undefined.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graph additions borrow them; ordinary disposal guards apply. Defaults, selectors and width survive duplication and typed archives. Editing requires reimport before rendering changes.

The public VisualShader.Consumer covers typed requests, copy/archives, every scalar/vector function and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
