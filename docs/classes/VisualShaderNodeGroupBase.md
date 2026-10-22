# VisualShaderNodeGroupBase

Last updated: 2026-10-22

Source: [VisualShaderNodeGroupBase.cs](../../src/Scene/Resources/VisualShaderNodeGroupBase.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeGroupBase
```

Inherits: [VisualShaderNodeResizableBase](VisualShaderNodeResizableBase.md).

## Description

Owns ordered, named, typed input and output ports for source graph expressions.

Port IDs are contiguous indices. Insert/remove renumbers the following ports; graph edges keep their stored indices and callers repair affected connections. Numeric defaults move with inputs, convert on type changes and retain explicit absence. Sampler ports have no numeric defaults. Mutations validate before committing and publish Changed and the stored-property schema afterwards.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeExpression();
node.AddInputPort(0, VisualShaderNode.PortType.Scalar, "amount");
node.AddOutputPort(0, VisualShaderNode.PortType.Vector3, "color");
node.SetInputPortDefaultValue(0, .5f);
node.Size = new Vector2(240, 160);
node.Expression = "color = vec3(amount);";
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/expression-source.e2dres", SaverFlags.BundleResources);
```

Import through [the graph workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public abstract class Electron2D.VisualShaderNodeGroupBase`](#member-0fe1f929fa63) | Owns ordered, named, typed input and output ports for source graph expressions. |
| [`protected VisualShaderNodeGroupBase()`](#member-337c14018c78) | Initializes empty port lists. |
| [`public System.Void AddInputPort(System.Int32 id, Electron2D.VisualShaderNode.PortType type, System.String name)`](#member-0555c461fa03) | Inserts one input port and renumbers subsequent ports. |
| [`public System.Void AddOutputPort(System.Int32 id, Electron2D.VisualShaderNode.PortType type, System.String name)`](#member-9352a3d7ac10) | Inserts one output port and renumbers subsequent ports. |
| [`public System.Void ClearInputPorts()`](#member-95cb8da0e93e) | Clears all input ports and their numeric defaults. |
| [`public System.Void ClearOutputPorts()`](#member-4be8ecf0aa9e) | Clears all output ports. |
| [`public System.Int32 GetFreeInputPortID()`](#member-1a9d3afe8012) | Returns the next appendable input ID. |
| [`public System.Int32 GetFreeOutputPortID()`](#member-b0071b92765b) | Returns the next appendable output ID. |
| [`public System.Int32 GetInputPortCount()`](#member-4e74edb5acbf) | Returns the number of input ports. |
| [`public System.String GetInputs()`](#member-1f9b9ffd380d) | Returns the canonical ordered input port description. |
| [`public System.Int32 GetOutputPortCount()`](#member-02dd5e6a6a52) | Returns the number of output ports. |
| [`public System.String GetOutputs()`](#member-cc29c68d226e) | Returns the canonical ordered output port description. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-2f833de974cd) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Boolean HasInputPort(System.Int32 id)`](#member-446c570157c0) | Reports whether an input index exists. |
| [`public System.Boolean HasOutputPort(System.Int32 id)`](#member-83ea9c229c58) | Reports whether an output index exists. |
| [`public System.Boolean IsValidPortName(System.String name)`](#member-7b455fd2c855) | Checks an unused ASCII identifier against both port lists. |
| [`public System.Void RemoveInputPort(System.Int32 id)`](#member-e048dd19dd91) | Removes one input port and renumbers subsequent ports. |
| [`public System.Void RemoveOutputPort(System.Int32 id)`](#member-aea3b0d9cb82) | Removes one output port and renumbers subsequent ports. |
| [`public System.Void SetInputPortName(System.Int32 id, System.String name)`](#member-95cc59abbabb) | Renames one input port without changing its index or value role. |
| [`public System.Void SetInputPortType(System.Int32 id, Electron2D.VisualShaderNode.PortType type)`](#member-60a7ea0e4314) | Changes one input port value role atomically. |
| [`public System.Void SetInputs(System.String inputs)`](#member-06aa89db3c88) | Atomically replaces the complete input port description. |
| [`public System.Void SetOutputPortName(System.Int32 id, System.String name)`](#member-03efc22fab68) | Renames one output port without changing its index or value role. |
| [`public System.Void SetOutputPortType(System.Int32 id, Electron2D.VisualShaderNode.PortType type)`](#member-54e73bb9e765) | Changes one output port value role atomically. |
| [`public System.Void SetOutputs(System.String outputs)`](#member-c53fcb0d4d73) | Atomically replaces the complete output port description. |

<a id="member-0fe1f929fa63"></a>
### `VisualShaderNodeGroupBase`

```csharp
public abstract class Electron2D.VisualShaderNodeGroupBase
```

Owns ordered, named, typed input and output ports for source graph expressions.

Port IDs are contiguous indices. Insert/remove renumbers the following ports; graph edges keep their stored indices and callers repair affected connections. Numeric defaults move with inputs, convert on type changes and retain explicit absence. Sampler ports have no numeric defaults. Mutations validate before committing and publish Changed and the stored-property schema afterwards.

<a id="member-337c14018c78"></a>
### `.ctor`

```csharp
protected VisualShaderNodeGroupBase()
```

Initializes empty port lists.

<a id="member-0555c461fa03"></a>
### `AddInputPort`

```csharp
public System.Void AddInputPort(System.Int32 id, Electron2D.VisualShaderNode.PortType type, System.String name)
```

Inserts one input port and renumbers subsequent ports.

Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.

Parameters:

- `id`: A nonnegative insertion index; indices at or beyond the count append.
- `type`: The exact value role, excluding Max.
- `name`: A unique ASCII name across both port lists.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: An index or value role is invalid.
- `T:System.ArgumentNullException`: The name is null.
- `T:System.ArgumentException`: The name is invalid or already used.
- `T:System.InvalidOperationException`: The 4096-port bound is reached.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-9352a3d7ac10"></a>
### `AddOutputPort`

```csharp
public System.Void AddOutputPort(System.Int32 id, Electron2D.VisualShaderNode.PortType type, System.String name)
```

Inserts one output port and renumbers subsequent ports.

Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.

Parameters:

- `id`: A nonnegative insertion index; indices at or beyond the count append.
- `type`: The exact value role, excluding Max.
- `name`: A unique ASCII name across both port lists.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: An index or value role is invalid.
- `T:System.ArgumentNullException`: The name is null.
- `T:System.ArgumentException`: The name is invalid or already used.
- `T:System.InvalidOperationException`: The 4096-port bound is reached.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-95cb8da0e93e"></a>
### `ClearInputPorts`

```csharp
public System.Void ClearInputPorts()
```

Clears all input ports and their numeric defaults.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-4be8ecf0aa9e"></a>
### `ClearOutputPorts`

```csharp
public System.Void ClearOutputPorts()
```

Clears all output ports.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-1a9d3afe8012"></a>
### `GetFreeInputPortID`

```csharp
public System.Int32 GetFreeInputPortID()
```

Returns the next appendable input ID.

The current input count.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-b0071b92765b"></a>
### `GetFreeOutputPortID`

```csharp
public System.Int32 GetFreeOutputPortID()
```

Returns the next appendable output ID.

The current output count.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-4e74edb5acbf"></a>
### `GetInputPortCount`

```csharp
public System.Int32 GetInputPortCount()
```

Returns the number of input ports.

The next free input index.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-1f9b9ffd380d"></a>
### `GetInputs`

```csharp
public System.String GetInputs()
```

Returns the canonical ordered input port description.

Rows formatted as id,type,name; using numeric PortType identities, with no spaces added.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-02dd5e6a6a52"></a>
### `GetOutputPortCount`

```csharp
public System.Int32 GetOutputPortCount()
```

Returns the number of output ports.

The next free output index.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-cc29c68d226e"></a>
### `GetOutputs`

```csharp
public System.String GetOutputs()
```

Returns the canonical ordered output port description.

Rows formatted as id,type,name; using numeric PortType identities, with no spaces added.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-2f833de974cd"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-446c570157c0"></a>
### `HasInputPort`

```csharp
public System.Boolean HasInputPort(System.Int32 id)
```

Reports whether an input index exists.

False for negative and out-of-range indices.

Parameters:

- `id`: The input index.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-83ea9c229c58"></a>
### `HasOutputPort`

```csharp
public System.Boolean HasOutputPort(System.Int32 id)
```

Reports whether an output index exists.

False for negative and out-of-range indices.

Parameters:

- `id`: The output index.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-7b455fd2c855"></a>
### `IsValidPortName`

```csharp
public System.Boolean IsValidPortName(System.String name)
```

Checks an unused ASCII identifier against both port lists.

Shader keyword/builtin conflicts are diagnosed during source compilation.

True for a unique identifier beginning with a letter or underscore, followed by letters, digits or underscores; at most 128 characters.

Parameters:

- `name`: The proposed case-sensitive name.

Exceptions:

- `T:System.ArgumentNullException`: The name is null.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-e048dd19dd91"></a>
### `RemoveInputPort`

```csharp
public System.Void RemoveInputPort(System.Int32 id)
```

Removes one input port and renumbers subsequent ports.

Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.

Parameters:

- `id`: An existing zero-based port index.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: An index or value role is invalid.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-aea3b0d9cb82"></a>
### `RemoveOutputPort`

```csharp
public System.Void RemoveOutputPort(System.Int32 id)
```

Removes one output port and renumbers subsequent ports.

Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.

Parameters:

- `id`: An existing zero-based port index.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: An index or value role is invalid.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-95cc59abbabb"></a>
### `SetInputPortName`

```csharp
public System.Void SetInputPortName(System.Int32 id, System.String name)
```

Renames one input port without changing its index or value role.

Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.

Parameters:

- `id`: An existing zero-based port index.
- `name`: A unique ASCII name; selecting the same name is quiet.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: An index or value role is invalid.
- `T:System.ArgumentNullException`: The name is null.
- `T:System.ArgumentException`: The name is invalid or already used.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-60a7ea0e4314"></a>
### `SetInputPortType`

```csharp
public System.Void SetInputPortType(System.Int32 id, Electron2D.VisualShaderNode.PortType type)
```

Changes one input port value role atomically.

Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.

Parameters:

- `id`: An existing zero-based port index.
- `type`: The exact value role, excluding Max; selecting the same role is quiet.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: An index or value role is invalid.
- `T:System.OverflowException`: An existing default cannot convert to the selected integer type.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-06aa89db3c88"></a>
### `SetInputs`

```csharp
public System.Void SetInputs(System.String inputs)
```

Atomically replaces the complete input port description.

A final semicolon is optional. Get returns canonical rows. Existing defaults retain their indexed values and convert to new roles; absent defaults remain absent. Malformed replacements preserve the prior state.

Parameters:

- `inputs`: Semicolon-separated id,type,name rows with contiguous IDs starting at zero.

Exceptions:

- `T:System.ArgumentNullException`: The description is null.
- `T:System.ArgumentException`: Names, IDs, roles or the 4096-port budget are invalid.
- `T:System.OverflowException`: An existing default cannot convert to the selected integer type.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-03efc22fab68"></a>
### `SetOutputPortName`

```csharp
public System.Void SetOutputPortName(System.Int32 id, System.String name)
```

Renames one output port without changing its index or value role.

Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.

Parameters:

- `id`: An existing zero-based port index.
- `name`: A unique ASCII name; selecting the same name is quiet.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: An index or value role is invalid.
- `T:System.ArgumentNullException`: The name is null.
- `T:System.ArgumentException`: The name is invalid or already used.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-54e73bb9e765"></a>
### `SetOutputPortType`

```csharp
public System.Void SetOutputPortType(System.Int32 id, Electron2D.VisualShaderNode.PortType type)
```

Changes one output port value role atomically.

Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.

Parameters:

- `id`: An existing zero-based port index.
- `type`: The exact value role, excluding Max; selecting the same role is quiet.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: An index or value role is invalid.
- `T:System.OverflowException`: An existing default cannot convert to the selected integer type.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-c53fcb0d4d73"></a>
### `SetOutputs`

```csharp
public System.Void SetOutputs(System.String outputs)
```

Atomically replaces the complete output port description.

A final semicolon is optional. Get returns canonical rows. Existing defaults retain their indexed values and convert to new roles; absent defaults remain absent. Malformed replacements preserve the prior state.

Parameters:

- `outputs`: Semicolon-separated id,type,name rows with contiguous IDs starting at zero.

Exceptions:

- `T:System.ArgumentNullException`: The description is null.
- `T:System.ArgumentException`: Names, IDs, roles or the 4096-port budget are invalid.
- `T:System.OverflowException`: An existing default cannot convert to the selected integer type.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer exercises typed defaults, copies, factories, typed source/port limits, source/compiled archives and executable GLSL procedures. See [the operation contract](../components/visual-shaders.md#glsl-expression-blocks) for ports, pixel units, port mutation, scope, control flow and source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
