# VisualShaderNode

Last updated: 2026-10-21

Source: [VisualShaderNode.cs](../../src/Scene/Resources/VisualShaderNode.cs).

```csharp
public abstract class Electron2D.VisualShaderNode
```

Inherits: [Resource](Resource.md).

Inherited by: [VisualShaderNodeClamp](VisualShaderNodeClamp.md), [VisualShaderNodeConstant](VisualShaderNodeConstant.md), [VisualShaderNodeDotProduct](VisualShaderNodeDotProduct.md), [VisualShaderNodeFloatFunc](VisualShaderNodeFloatFunc.md), [VisualShaderNodeFloatOp](VisualShaderNodeFloatOp.md), [VisualShaderNodeInput](VisualShaderNodeInput.md), [VisualShaderNodeIntFunc](VisualShaderNodeIntFunc.md), [VisualShaderNodeIntOp](VisualShaderNodeIntOp.md), [VisualShaderNodeMix](VisualShaderNodeMix.md), [VisualShaderNodeMultiplyAdd](VisualShaderNodeMultiplyAdd.md), [VisualShaderNodeOutput](VisualShaderNodeOutput.md), [VisualShaderNodeRemap](VisualShaderNodeRemap.md), [VisualShaderNodeSmoothStep](VisualShaderNodeSmoothStep.md), [VisualShaderNodeStep](VisualShaderNodeStep.md), [VisualShaderNodeUIntFunc](VisualShaderNodeUIntFunc.md), [VisualShaderNodeUIntOp](VisualShaderNodeUIntOp.md), [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

A typed numeric operation in an offline-compiled visual shader graph.

Nodes are borrowed by graphs. Authoring is cold, single-threaded work. Edits emit Changed and affect the next import; existing compiled shader programs retain their immutable code.

## Member summary

| Member | Contract |
| --- | --- |
| [`protected VisualShaderNode()`](#member-ff366100755e) | Initializes a numeric graph node. |
| [`public System.Void ClearDefaultInputValues()`](#member-c67c6dc2c295) | Clears all explicit input defaults. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-58cd5cdb598d) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Int32 GetDefaultInputPort(Electron2D.VisualShaderNode.PortType type)`](#member-ab8c82594b83) | Gets the default connection target used when adding this node from an existing port. |
| [`public System.Collections.Generic.IReadOnlyList<Electron2D.VisualShaderPortDefault> GetDefaultInputValues()`](#member-714d2adc90b7) | Returns immutable typed copies of all explicitly authored input defaults. |
| [`public Nullable<T> GetInputPortDefaultValue<T>(System.Int32 port)`](#member-d281ffbdd704) | Reads a copied authored input default. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-ee30536b1566) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Void RemoveInputPortDefaultValue(System.Int32 port)`](#member-2e3abedf6c5c) | Removes one explicit input default so compilation uses the port's zero-valued fallback. |
| [`public System.Void SetDefaultInputValues(System.Collections.Generic.IReadOnlyList<Electron2D.VisualShaderPortDefault> values)`](#member-bae84faeacfa) | Atomically replaces the complete explicit input-default table. |
| [`public System.Void SetInputPortDefaultValue<T>(System.Int32 port, T value)`](#member-ee57fa5c0933) | Copies a typed default into an input port. |
| [`public System.Void SetInputPortDefaultValue<T, TPrevious>(System.Int32 port, T value, TPrevious previousValue)`](#member-32ed460be1b5) | Converts a previous numeric default to a newly selected port value type. |

<a id="member-ff366100755e"></a>
### `.ctor`

```csharp
protected VisualShaderNode()
```

Initializes a numeric graph node.

<a id="member-c67c6dc2c295"></a>
### `ClearDefaultInputValues`

```csharp
public System.Void ClearDefaultInputValues()
```

Clears all explicit input defaults.

Exceptions:

- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-58cd5cdb598d"></a>
### `CopyCustomStateTo`

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)
```

<a id="member-ab8c82594b83"></a>
### `GetDefaultInputPort`

```csharp
public System.Int32 GetDefaultInputPort(Electron2D.VisualShaderNode.PortType type)
```

Gets the default connection target used when adding this node from an existing port.

Input zero, as specified by the base node connection policy.

Parameters:

- `type`: The originating numeric port type.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The numeric port type is undefined.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-714d2adc90b7"></a>
### `GetDefaultInputValues`

```csharp
public System.Collections.Generic.IReadOnlyList<Electron2D.VisualShaderPortDefault> GetDefaultInputValues()
```

Returns immutable typed copies of all explicitly authored input defaults.

Entries in ascending input order using canonical port types. Absent defaults are omitted; composition nodes also retain entries for temporarily inactive components.

Exceptions:

- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-d281ffbdd704"></a>
### `GetInputPortDefaultValue`

```csharp
public Nullable<T> GetInputPortDefaultValue<T>(System.Int32 port)
```

Reads a copied authored input default.

The authored value, or null after its default entry has been removed.

Type parameters:

- `T`: The exact numeric port type, or Color for a four-component port.

Parameters:

- `port`: The input index, including a reserved composition component.

Exceptions:

- `T:System.ArgumentException`: The requested type does not match the input.
- `T:System.ArgumentOutOfRangeException`: The input is neither active nor a reserved composition component.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-ee30536b1566"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-2e3abedf6c5c"></a>
### `RemoveInputPortDefaultValue`

```csharp
public System.Void RemoveInputPortDefaultValue(System.Int32 port)
```

Removes one explicit input default so compilation uses the port's zero-valued fallback.

Parameters:

- `port`: The input index, including a reserved composition component.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The input is neither active nor a reserved composition component.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-bae84faeacfa"></a>
### `SetDefaultInputValues`

```csharp
public System.Void SetDefaultInputValues(System.Collections.Generic.IReadOnlyList<Electron2D.VisualShaderPortDefault> values)
```

Atomically replaces the complete explicit input-default table.

Parameters:

- `values`: Unique, correctly typed entries for existing or reserved inputs; an empty collection clears the table.

Exceptions:

- `T:System.ArgumentNullException`: The collection or an entry is null.
- `T:System.ArgumentException`: An entry is duplicated, mistyped or invalid.
- `T:System.ArgumentOutOfRangeException`: An input does not exist.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-ee57fa5c0933"></a>
### `SetInputPortDefaultValue`

```csharp
public System.Void SetInputPortDefaultValue<T>(System.Int32 port, T value)
```

Copies a typed default into an input port.

Type parameters:

- `T`: The exact numeric input type; Color also represents a four-component port.

Parameters:

- `port`: The input index, including a reserved composition component.
- `value`: The finite numeric default.

Exceptions:

- `T:System.ArgumentException`: The value has the wrong type or nonfinite components.
- `T:System.ArgumentOutOfRangeException`: The input is neither active nor a reserved composition component.
- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-32ed460be1b5"></a>
### `SetInputPortDefaultValue`

```csharp
public System.Void SetInputPortDefaultValue<T, TPrevious>(System.Int32 port, T value, TPrevious previousValue)
```

Converts a previous numeric default to a newly selected port value type.

Type parameters:

- `T`: The target port value type.
- `TPrevious`: The previous numeric value type.

Parameters:

- `port`: The input index, including a reserved composition component.
- `value`: The target role and fallback value.
- `previousValue`: The old numeric value; expanding a vector repeats its last component. Boolean conversion retains the fallback.

Exceptions:

- `T:System.ArgumentException`: Either value is invalid or the target type does not match.
- `T:System.ArgumentOutOfRangeException`: The input is neither active nor a reserved composition component.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graph additions borrow them; ordinary disposal guards apply. Defaults, selectors and width survive duplication and typed archives. Editing requires reimport before rendering changes.

The public VisualShader.Consumer covers typed requests, mixed/dormant defaults, copy/archives, multi-output graphs and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
