# VisualShader

Last updated: 2026-10-22

Source: [VisualShader.cs](../../src/Scene/Resources/VisualShader.cs).

```csharp
public sealed class Electron2D.VisualShader
```

Inherits: [Shader](Shader.md).

## Description

Typed editable shader graphs and their last successfully imported program.

Graph edits are source authoring. Save the resource and run ShaderImport with --visual-shader to produce a new compiled resource. Existing materials keep the previous immutable program until reloaded. Added nodes are borrowed; reserved outputs and factory-created replacements belong to the graph. Authoring is single-threaded cold work.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShader`](#member-5b40f778f238) | Typed editable shader graphs and their last successfully imported program. |
| [`public const System.Int32 NodeIDInvalid = -1`](#member-d8958f3c4b8c) | The sentinel for an absent node. |
| [`public const System.Int32 NodeIDOutput = 0`](#member-12489fb369a6) | The reserved output node in each stage. |
| [`public VisualShader()`](#member-517e90a99215) | Creates independent owned outputs for the supported source stages; canvas mode is initially selected. |
| [`public System.Void AddNode(Electron2D.VisualShader.Type type, Electron2D.VisualShaderNode node, Electron2D.Vector2 position, System.Int32 id)`](#member-d838266863d0) | Adds a borrowed numeric operation at a finite authoring position. |
| [`public System.Boolean CanConnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-aa675f576da3) | Tests whether a new numeric connection is valid and acyclic. |
| [`public System.Void ConnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-b0802f781262) | Adds a validated numeric connection. |
| [`public System.Void ConnectNodesForced(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-c9afcad3894c) | Records an edge between existing ports without rejecting graph cycles. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-760215bda1bf) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-7ad2dafde8d0) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Void DisconnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-491eb1eabc43) | Removes an exact edge if present. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-f7d98cacf90d) | Uses the inherited Resource construction or stored-property contract. |
| [`public override Electron2D.Shader.Mode GetMode()`](#member-ac38931cfbb4) | Returns the authored domain, independently of the retained compiled program. |
| [`public Electron2D.VisualShaderNode GetNode(Electron2D.VisualShader.Type type, System.Int32 id)`](#member-4783f6cc0b46) | Returns a borrowed operation in a stage. |
| [`public Electron2D.VisualShaderConnection[] GetNodeConnections(Electron2D.VisualShader.Type type)`](#member-fc569b224695) | Copies connections in authoring order. |
| [`public System.Int32[] GetNodeList(Electron2D.VisualShader.Type type)`](#member-094aeedb9035) | Copies the stage's node IDs in ascending order. |
| [`public Electron2D.Vector2 GetNodePosition(Electron2D.VisualShader.Type type, System.Int32 id)`](#member-cd67579285ae) | Reads a node's finite authoring position. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-e8e49be7ac36) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Int32 GetValidNodeID(Electron2D.VisualShader.Type type)`](#member-a3751654a775) | Returns the next unused ID after the largest stage ID, with two as the minimum. |
| [`public System.Boolean IsNodeConnection(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-7d3c760afd8d) | Tests whether an exact edge is stored. |
| [`protected override System.Void RemapResourceReferences(System.Func<Electron2D.Resource, Electron2D.Resource> remap)`](#member-3b08d4afa739) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Void RemoveNode(Electron2D.VisualShader.Type type, System.Int32 id)`](#member-bbd77fa617e9) | Removes a borrowed operation and all incident connections. |
| [`public System.Void ReplaceNode(Electron2D.VisualShader.Type type, System.Int32 id, System.String newClass)`](#member-c7a10e092377) | Replaces an operation using an explicitly registered compiled resource factory. |
| [`public System.Void SetMode(Electron2D.Shader.Mode mode)`](#member-ebc2b739c95e) | Selects the source domain and removes input/output boundary connections. |
| [`public System.Void SetNodePosition(Electron2D.VisualShader.Type type, System.Int32 id, Electron2D.Vector2 position)`](#member-a985cea8c0e1) | Changes a node's finite authoring position without changing compiled code. |

<a id="member-5b40f778f238"></a>
### `VisualShader`

```csharp
public sealed class Electron2D.VisualShader
```

Typed editable shader graphs and their last successfully imported program.

Graph edits are source authoring. Save the resource and run ShaderImport with --visual-shader to produce a new compiled resource. Existing materials keep the previous immutable program until reloaded. Added nodes are borrowed; reserved outputs and factory-created replacements belong to the graph. Authoring is single-threaded cold work.

<a id="member-d8958f3c4b8c"></a>
### `NodeIDInvalid`

```csharp
public const System.Int32 NodeIDInvalid = -1
```

The sentinel for an absent node.

<a id="member-12489fb369a6"></a>
### `NodeIDOutput`

```csharp
public const System.Int32 NodeIDOutput = 0
```

The reserved output node in each stage.

<a id="member-517e90a99215"></a>
### `.ctor`

```csharp
public VisualShader()
```

Creates independent owned outputs for the supported source stages; canvas mode is initially selected.

<a id="member-d838266863d0"></a>
### `AddNode`

```csharp
public System.Void AddNode(Electron2D.VisualShader.Type type, Electron2D.VisualShaderNode node, Electron2D.Vector2 position, System.Int32 id)
```

Adds a borrowed numeric operation at a finite authoring position.

Parameters:

- `type`: The source stage.
- `node`: A live operation; output nodes cannot be added.
- `position`: Position metadata in graph coordinates.
- `id`: A unique ID of at least two.

Exceptions:

- `T:System.ArgumentException`: The node, position or ID is invalid or already occupied.
- `T:System.InvalidOperationException`: The graph has exhausted its node budget.
- `T:System.ObjectDisposedException`: This graph or the supplied node is disposed.

<a id="member-aa675f576da3"></a>
### `CanConnectNodes`

```csharp
public System.Boolean CanConnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)
```

Tests whether a new numeric connection is valid and acyclic.

False for absent/disposed nodes, invalid ports, duplicates, self edges or cycles.

Parameters:

- `type`: The source stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The stage is undefined.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-b0802f781262"></a>
### `ConnectNodes`

```csharp
public System.Void ConnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)
```

Adds a validated numeric connection.

Several edges may target an input; the last authored edge supplies its value at import.

Parameters:

- `type`: The source stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.

Exceptions:

- `T:System.ArgumentException`: The connection is invalid, duplicated or cyclic.
- `T:System.ArgumentOutOfRangeException`: The stage is undefined.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-c9afcad3894c"></a>
### `ConnectNodesForced`

```csharp
public System.Void ConnectNodesForced(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)
```

Records an edge between existing ports without rejecting graph cycles.

Duplicate edges are unchanged. This bypasses type compatibility; import rejects incompatible ports and reachable cycles atomically.

Parameters:

- `type`: The source stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.

Exceptions:

- `T:System.ArgumentException`: A selected node or port is absent or disposed.
- `T:System.InvalidOperationException`: The connection budget is exhausted.
- `T:System.ArgumentOutOfRangeException`: The stage is undefined.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-760215bda1bf"></a>
### `CopyCustomStateTo`

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)
```

<a id="member-7ad2dafde8d0"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-491eb1eabc43"></a>
### `DisconnectNodes`

```csharp
public System.Void DisconnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)
```

Removes an exact edge if present.

Parameters:

- `type`: The source stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The stage is undefined.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-f7d98cacf90d"></a>
### `Dispose`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-ac38931cfbb4"></a>
### `GetMode`

```csharp
public override Electron2D.Shader.Mode GetMode()
```

Returns the authored domain, independently of the retained compiled program.

The selected CanvasItem, Particles or TextureBlit source domain.

Exceptions:

- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-4783f6cc0b46"></a>
### `GetNode`

```csharp
public Electron2D.VisualShaderNode GetNode(Electron2D.VisualShader.Type type, System.Int32 id)
```

Returns a borrowed operation in a stage.

The live borrowed operation.

Parameters:

- `type`: The source stage.
- `id`: An existing node ID, including zero for the output.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `T:System.ObjectDisposedException`: This graph or the node is disposed.

<a id="member-fc569b224695"></a>
### `GetNodeConnections`

```csharp
public Electron2D.VisualShaderConnection[] GetNodeConnections(Electron2D.VisualShader.Type type)
```

Copies connections in authoring order.

An independent typed edge array.

Parameters:

- `type`: The source stage.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The stage is undefined.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-094aeedb9035"></a>
### `GetNodeList`

```csharp
public System.Int32[] GetNodeList(Electron2D.VisualShader.Type type)
```

Copies the stage's node IDs in ascending order.

An independent ID array, including the reserved output.

Parameters:

- `type`: The source stage.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The stage is undefined.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-cd67579285ae"></a>
### `GetNodePosition`

```csharp
public Electron2D.Vector2 GetNodePosition(Electron2D.VisualShader.Type type, System.Int32 id)
```

Reads a node's finite authoring position.

The graph-coordinate position.

Parameters:

- `type`: The source stage.
- `id`: An existing node ID.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-e8e49be7ac36"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-a3751654a775"></a>
### `GetValidNodeID`

```csharp
public System.Int32 GetValidNodeID(Electron2D.VisualShader.Type type)
```

Returns the next unused ID after the largest stage ID, with two as the minimum.

The next ID; holes below the largest ID are preserved.

Parameters:

- `type`: The source stage.

Exceptions:

- `T:System.OverflowException`: The largest ID is Int32.MaxValue.
- `T:System.ArgumentOutOfRangeException`: The stage is undefined.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-7d3c760afd8d"></a>
### `IsNodeConnection`

```csharp
public System.Boolean IsNodeConnection(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)
```

Tests whether an exact edge is stored.

True only for the exact stored edge.

Parameters:

- `type`: The source stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The stage is undefined.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-3b08d4afa739"></a>
### `RemapResourceReferences`

```csharp
protected override System.Void RemapResourceReferences(System.Func<Electron2D.Resource, Electron2D.Resource> remap)
```

<a id="member-bbd77fa617e9"></a>
### `RemoveNode`

```csharp
public System.Void RemoveNode(Electron2D.VisualShader.Type type, System.Int32 id)
```

Removes a borrowed operation and all incident connections.

Parameters:

- `type`: The source stage.
- `id`: An existing non-output ID.

Exceptions:

- `T:System.ArgumentException`: The reserved output was selected.
- `T:System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-c7a10e092377"></a>
### `ReplaceNode`

```csharp
public System.Void ReplaceNode(Electron2D.VisualShader.Type type, System.Int32 id, System.String newClass)
```

Replaces an operation using an explicitly registered compiled resource factory.

An unchanged runtime type is a no-op. Positions and stored edges remain; import rejects any edges whose ports no longer exist. The graph owns the replacement until its final graph alias is removed. No assembly loading, member reflection or runtime source compilation occurs.

Parameters:

- `type`: The source stage.
- `id`: An existing node ID of at least two.
- `newClass`: The exact registered type ID, for example VisualShaderNodeVec3Constant.

Exceptions:

- `T:System.ArgumentException`: The ID or registered class does not identify an ordinary graph operation.
- `T:System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `T:System.InvalidOperationException`: The compiled factory violates its freshness/type contract.
- `T:System.ObjectDisposedException`: The graph is disposed.
- `T:System.AggregateException`: Independent observer and retirement failures occur after commitment.

<a id="member-ebc2b739c95e"></a>
### `SetMode`

```csharp
public System.Void SetMode(Electron2D.Shader.Mode mode)
```

Selects the source domain and removes input/output boundary connections.

An equal mode is unchanged. All nodes, positions, numeric defaults and ordinary links remain, including inactive stages. The prior compiled program remains executable until a successful import. Schema and Changed observers run after commitment, even if another observer fails.

Parameters:

- `mode`: A supported shader domain.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The domain is undefined.
- `T:System.ObjectDisposedException`: This graph is disposed.

<a id="member-a985cea8c0e1"></a>
### `SetNodePosition`

```csharp
public System.Void SetNodePosition(Electron2D.VisualShader.Type type, System.Int32 id, Electron2D.Vector2 position)
```

Changes a node's finite authoring position without changing compiled code.

Parameters:

- `type`: The source stage.
- `id`: An existing node ID.
- `position`: Finite graph coordinates.

Exceptions:

- `T:System.ArgumentException`: The position is nonfinite.
- `T:System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `T:System.ObjectDisposedException`: This graph is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
