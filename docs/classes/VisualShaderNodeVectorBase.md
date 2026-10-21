# VisualShaderNodeVectorBase

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorBase.cs](../../src/Scene/Resources/VisualShaderNodeVectorBase.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeVectorBase
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

Inherited by: [VisualShaderNodeVectorFunc](VisualShaderNodeVectorFunc.md), [VisualShaderNodeVectorOp](VisualShaderNodeVectorOp.md).

## Description

A numeric vector graph operation with a selectable component count.

These are numeric shader components, independent of the engine's planar scene domain.

## Member summary

| Member | Contract |
| --- | --- |
| [`protected VisualShaderNodeVectorBase()`](#member-d570ea6991a5) | Initializes a three-component numeric operation. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-1aa11052a63e) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeVectorBase.OpTypeEnum OpType { get; set; }`](#member-1b062dd6ba59) | Gets or selects the component count, converting existing input defaults. |

<a id="member-d570ea6991a5"></a>
### `.ctor`

```csharp
protected VisualShaderNodeVectorBase()
```

Initializes a three-component numeric operation.

<a id="member-1aa11052a63e"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-1b062dd6ba59"></a>
### `OpType`

```csharp
public Electron2D.VisualShaderNodeVectorBase.OpTypeEnum OpType { get; set; }
```

Gets or selects the component count, converting existing input defaults.

An unchanged selection emits no event. Changing width recreates removed defaults with zero vectors, or (0,0,0,1) for four components. Cross product requires three components.

Vector3 initially; expanding a default repeats its last previous component.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The component-count selection is undefined.
- `T:System.NotSupportedException`: The selected cross-product width is not three.
- `T:System.ObjectDisposedException`: The operation is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graph additions borrow them; ordinary disposal guards apply. Defaults, selectors and width survive duplication and typed archives. Editing requires reimport before rendering changes.

The public VisualShader.Consumer covers typed requests, copy/archives, every scalar/vector function and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
