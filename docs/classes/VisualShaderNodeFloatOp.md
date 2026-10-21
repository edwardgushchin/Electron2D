# VisualShaderNodeFloatOp

Last updated: 2026-10-21

Source: [VisualShaderNodeFloatOp.cs](../../src/Scene/Resources/VisualShaderNodeFloatOp.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeFloatOp
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Applies one binary floating-point operation to two graph inputs.

Arithmetic executes in the imported shader, including its native NaN/infinity rules.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeFloatOp()`](#member-8149cc356b66) | Creates an addition operation with two zero defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-601d692e825b) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-31a98d1e6aa1) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeFloatOp.OperatorEnum Operator { get; set; }`](#member-230236d2a117) | Gets or selects the binary scalar operation. |

<a id="member-8149cc356b66"></a>
### `.ctor`

```csharp
public VisualShaderNodeFloatOp()
```

Creates an addition operation with two zero defaults.

<a id="member-601d692e825b"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-31a98d1e6aa1"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-230236d2a117"></a>
### `Operator`

```csharp
public Electron2D.VisualShaderNodeFloatOp.OperatorEnum Operator { get; set; }
```

Gets or selects the binary scalar operation.

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
