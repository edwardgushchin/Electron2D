# VisualShaderNode

Last updated: 2026-10-21

Source: [`VisualShaderNode.cs`](../../src/Scene/Resources/VisualShaderNode.cs).

```csharp
public abstract class Electron2D.VisualShaderNode
```

Inherits: [Resource](Resource.md).

Inherited by: [VisualShaderNodeConstant](VisualShaderNodeConstant.md), [VisualShaderNodeFloatOp](VisualShaderNodeFloatOp.md), [VisualShaderNodeInput](VisualShaderNodeInput.md), [VisualShaderNodeOutput](VisualShaderNodeOutput.md), [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

A typed numeric operation in an offline-compiled visual shader graph.

Nodes are borrowed by graphs. Authoring is cold, single-threaded work. Edits emit Changed and affect the next import; existing compiled shader programs retain their immutable code.

## Member summary

| Member | Contract |
| --- | --- |
| [`protected VisualShaderNode()`](#member-ff366100755e) | Initializes a numeric graph node. |
| [`public System.Void ClearDefaultInputValues()`](#member-c67c6dc2c295) | Clears all explicit input defaults. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-58cd5cdb598d) | Inherited typed Resource lifecycle implementation. |
| [`public System.Int32 GetDefaultInputPort(Electron2D.VisualShaderNode.PortType type)`](#member-ab8c82594b83) | Gets the default connection target used when adding this node from an existing port. |
| [`public System.Collections.Generic.IReadOnlyList<Electron2D.VisualShaderPortDefault> GetDefaultInputValues()`](#member-714d2adc90b7) | Returns immutable typed copies of all explicitly authored input defaults. |
| [`public Nullable<T> GetInputPortDefaultValue<T>(System.Int32 port)`](#member-d281ffbdd704) | Reads a copied authored input default. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-ee30536b1566) | Inherited typed Resource lifecycle implementation. |
| [`public System.Void RemoveInputPortDefaultValue(System.Int32 port)`](#member-2e3abedf6c5c) | Removes one explicit input default, restoring the operation's initial value. |
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

- `System.ObjectDisposedException`: The node is disposed.

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

- `type`: The originating numeric port type.
- `System.ArgumentOutOfRangeException`: The numeric port type is undefined.
- `System.ObjectDisposedException`: The node is disposed.

<a id="member-714d2adc90b7"></a>
### `GetDefaultInputValues`

```csharp
public System.Collections.Generic.IReadOnlyList<Electron2D.VisualShaderPortDefault> GetDefaultInputValues()
```

Returns immutable typed copies of all explicitly authored input defaults.

Entries in ascending input order using canonical port types. Absent defaults are omitted.

- `System.ObjectDisposedException`: The node is disposed.

<a id="member-d281ffbdd704"></a>
### `GetInputPortDefaultValue`

```csharp
public Nullable<T> GetInputPortDefaultValue<T>(System.Int32 port)
```

Reads a copied authored input default.

The authored value, or null after its default entry has been removed.

- `T`: The exact numeric port type, or Color for a four-component port.
- `port`: The input index.
- `System.ArgumentException`: The requested type does not match the input.
- `System.ArgumentOutOfRangeException`: The input does not exist.
- `System.ObjectDisposedException`: The node is disposed.

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

Removes one explicit input default, restoring the operation's initial value.

- `port`: The input index.
- `System.ArgumentOutOfRangeException`: The input does not exist.
- `System.ObjectDisposedException`: The node is disposed.

<a id="member-bae84faeacfa"></a>
### `SetDefaultInputValues`

```csharp
public System.Void SetDefaultInputValues(System.Collections.Generic.IReadOnlyList<Electron2D.VisualShaderPortDefault> values)
```

Atomically replaces the complete explicit input-default table.

- `values`: Unique, correctly typed entries for existing inputs; an empty collection clears the table.
- `System.ArgumentNullException`: The collection or an entry is null.
- `System.ArgumentException`: An entry is duplicated, mistyped or invalid.
- `System.ArgumentOutOfRangeException`: An input does not exist.
- `System.ObjectDisposedException`: The node is disposed.

<a id="member-ee57fa5c0933"></a>
### `SetInputPortDefaultValue`

```csharp
public System.Void SetInputPortDefaultValue<T>(System.Int32 port, T value)
```

Copies a typed default into an input port.

- `T`: The exact numeric input type; Color also represents a four-component port.
- `port`: The input index.
- `value`: The finite numeric default.
- `System.ArgumentException`: The value has the wrong type or nonfinite components.
- `System.ArgumentOutOfRangeException`: The input does not exist.
- `System.ObjectDisposedException`: The node is disposed.

<a id="member-32ed460be1b5"></a>
### `SetInputPortDefaultValue`

```csharp
public System.Void SetInputPortDefaultValue<T, TPrevious>(System.Int32 port, T value, TPrevious previousValue)
```

Converts a previous numeric default to a newly selected port value type.

- `T`: The target port value type.
- `TPrevious`: The previous numeric value type.
- `port`: The input index.
- `value`: The target role and fallback value.
- `previousValue`: The old numeric value; expanding a vector repeats its last component. Boolean conversion retains the fallback.
- `System.ArgumentException`: Either value is invalid or the target type does not match.
- `System.ArgumentOutOfRangeException`: The input does not exist.
- `System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
