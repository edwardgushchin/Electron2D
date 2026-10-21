# VisualShader

Last updated: 2026-10-21

Source: [`VisualShader.cs`](../../src/Scene/Resources/VisualShader.cs).

```csharp
public sealed class Electron2D.VisualShader
```

Inherits: [Shader](Shader.md).

## Description

A typed canvas graph and its last successfully imported shader program.

Graph edits are source authoring. Save the resource and run ShaderImport with --visual-shader to produce a new compiled resource. Existing materials keep the previous immutable program until reloaded. Added nodes are borrowed; reserved outputs and factory-created replacements belong to the graph. Authoring is single-threaded cold work.

## Example

```csharp
using var graph = new VisualShader();
using var color = new VisualShaderNodeColorConstant { Constant = new Color(.25f, .5f, .75f, 1) };
graph.AddNode(VisualShader.Type.Fragment, color, new Vector2(10, 20), 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/color-source.e2dres", SaverFlags.BundleResources);
```

Compile the saved graph with `ShaderImport color-source.e2dres canvas color.e2dres --visual-shader`, then load `color.e2dres` as a VisualShader and assign it to ShaderMaterial. The complete compiled workflow is exercised by `tests/VisualShader.Consumer`.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const System.Int32 NodeIDInvalid = -1`](#member-d8958f3c4b8c) | The sentinel for an absent node. |
| [`public const System.Int32 NodeIDOutput = 0`](#member-12489fb369a6) | The reserved output node in each stage. |
| [`public VisualShader()`](#member-517e90a99215) | Creates two empty canvas graphs with independent owned output nodes. |
| [`public System.Void AddNode(Electron2D.VisualShader.Type type, Electron2D.VisualShaderNode node, Electron2D.Vector2 position, System.Int32 id)`](#member-d838266863d0) | Adds a borrowed numeric operation at a finite authoring position. |
| [`public System.Boolean CanConnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-aa675f576da3) | Tests whether a new numeric connection is valid and acyclic. |
| [`public System.Void ConnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-b0802f781262) | Adds a validated numeric connection. |
| [`public System.Void ConnectNodesForced(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-c9afcad3894c) | Records an edge between existing ports without rejecting graph cycles. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-760215bda1bf) | Inherited typed Resource lifecycle implementation. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-7ad2dafde8d0) | Inherited typed Resource lifecycle implementation. |
| [`public System.Void DisconnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-491eb1eabc43) | Removes an exact edge if present. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-f7d98cacf90d) | Inherited typed Resource lifecycle implementation. |
| [`public Electron2D.VisualShaderNode GetNode(Electron2D.VisualShader.Type type, System.Int32 id)`](#member-4783f6cc0b46) | Returns a borrowed operation in a stage. |
| [`public Electron2D.VisualShaderConnection[] GetNodeConnections(Electron2D.VisualShader.Type type)`](#member-fc569b224695) | Copies connections in authoring order. |
| [`public System.Int32[] GetNodeList(Electron2D.VisualShader.Type type)`](#member-094aeedb9035) | Copies the stage's node IDs in ascending order. |
| [`public Electron2D.Vector2 GetNodePosition(Electron2D.VisualShader.Type type, System.Int32 id)`](#member-cd67579285ae) | Reads a node's finite authoring position. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-e8e49be7ac36) | Inherited typed Resource lifecycle implementation. |
| [`public System.Int32 GetValidNodeID(Electron2D.VisualShader.Type type)`](#member-a3751654a775) | Returns the next unused ID after the largest stage ID, with two as the minimum. |
| [`public System.Boolean IsNodeConnection(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)`](#member-7d3c760afd8d) | Tests whether an exact edge is stored. |
| [`protected override System.Void RemapResourceReferences(System.Func<Electron2D.Resource, Electron2D.Resource> remap)`](#member-3b08d4afa739) | Inherited typed Resource lifecycle implementation. |
| [`public System.Void RemoveNode(Electron2D.VisualShader.Type type, System.Int32 id)`](#member-bbd77fa617e9) | Removes a borrowed operation and all incident connections. |
| [`public System.Void ReplaceNode(Electron2D.VisualShader.Type type, System.Int32 id, System.String newClass)`](#member-c7a10e092377) | Replaces an operation using an explicitly registered compiled resource factory. |
| [`public System.Void SetNodePosition(Electron2D.VisualShader.Type type, System.Int32 id, Electron2D.Vector2 position)`](#member-a985cea8c0e1) | Changes a node's finite authoring position without changing compiled code. |

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

Creates two empty canvas graphs with independent owned output nodes.


<a id="member-d838266863d0"></a>
### `AddNode`

```csharp
public System.Void AddNode(Electron2D.VisualShader.Type type, Electron2D.VisualShaderNode node, Electron2D.Vector2 position, System.Int32 id)
```

Adds a borrowed numeric operation at a finite authoring position.

- `type`: The canvas stage.
- `node`: A live operation; output nodes cannot be added.
- `position`: Position metadata in graph coordinates.
- `id`: A unique ID of at least two.
- `System.ArgumentException`: The node, position or ID is invalid or already occupied.
- `System.InvalidOperationException`: The graph has exhausted its node budget.
- `System.ObjectDisposedException`: This graph or the supplied node is disposed.

<a id="member-aa675f576da3"></a>
### `CanConnectNodes`

```csharp
public System.Boolean CanConnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)
```

Tests whether a new numeric connection is valid and acyclic.

False for absent/disposed nodes, invalid ports, duplicates, self edges or cycles.

- `type`: The canvas stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.
- `System.ArgumentOutOfRangeException`: The stage is undefined.
- `System.ObjectDisposedException`: This graph is disposed.

<a id="member-b0802f781262"></a>
### `ConnectNodes`

```csharp
public System.Void ConnectNodes(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)
```

Adds a validated numeric connection.

Several edges may target an input; the last authored edge supplies its value at import.

- `type`: The canvas stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.
- `System.ArgumentException`: The connection is invalid, duplicated or cyclic.
- `System.ArgumentOutOfRangeException`: The stage is undefined.
- `System.ObjectDisposedException`: This graph is disposed.

<a id="member-c9afcad3894c"></a>
### `ConnectNodesForced`

```csharp
public System.Void ConnectNodesForced(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)
```

Records an edge between existing ports without rejecting graph cycles.

Duplicate edges are unchanged. Import rejects reachable cycles atomically.

- `type`: The canvas stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.
- `System.ArgumentException`: A selected node or port is absent or disposed.
- `System.InvalidOperationException`: The connection budget is exhausted.
- `System.ArgumentOutOfRangeException`: The stage is undefined.
- `System.ObjectDisposedException`: This graph is disposed.

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

- `type`: The canvas stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.
- `System.ArgumentOutOfRangeException`: The stage is undefined.
- `System.ObjectDisposedException`: This graph is disposed.

<a id="member-f7d98cacf90d"></a>
### `Dispose`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```


<a id="member-4783f6cc0b46"></a>
### `GetNode`

```csharp
public Electron2D.VisualShaderNode GetNode(Electron2D.VisualShader.Type type, System.Int32 id)
```

Returns a borrowed operation in a stage.

The live borrowed operation.

- `type`: The canvas stage.
- `id`: An existing node ID, including zero for the output.
- `System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `System.ObjectDisposedException`: This graph or the node is disposed.

<a id="member-fc569b224695"></a>
### `GetNodeConnections`

```csharp
public Electron2D.VisualShaderConnection[] GetNodeConnections(Electron2D.VisualShader.Type type)
```

Copies connections in authoring order.

An independent typed edge array.

- `type`: The canvas stage.
- `System.ArgumentOutOfRangeException`: The stage is undefined.
- `System.ObjectDisposedException`: This graph is disposed.

<a id="member-094aeedb9035"></a>
### `GetNodeList`

```csharp
public System.Int32[] GetNodeList(Electron2D.VisualShader.Type type)
```

Copies the stage's node IDs in ascending order.

An independent ID array, including the reserved output.

- `type`: The canvas stage.
- `System.ArgumentOutOfRangeException`: The stage is undefined.
- `System.ObjectDisposedException`: This graph is disposed.

<a id="member-cd67579285ae"></a>
### `GetNodePosition`

```csharp
public Electron2D.Vector2 GetNodePosition(Electron2D.VisualShader.Type type, System.Int32 id)
```

Reads a node's finite authoring position.

The graph-coordinate position.

- `type`: The canvas stage.
- `id`: An existing node ID.
- `System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `System.ObjectDisposedException`: This graph is disposed.

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

- `type`: The canvas stage.
- `System.OverflowException`: The largest ID is Int32.MaxValue.
- `System.ArgumentOutOfRangeException`: The stage is undefined.
- `System.ObjectDisposedException`: This graph is disposed.

<a id="member-7d3c760afd8d"></a>
### `IsNodeConnection`

```csharp
public System.Boolean IsNodeConnection(Electron2D.VisualShader.Type type, System.Int32 fromNode, System.Int32 fromPort, System.Int32 toNode, System.Int32 toPort)
```

Tests whether an exact edge is stored.

True only for the exact stored edge.

- `type`: The canvas stage.
- `fromNode`: The source ID.
- `fromPort`: The source output.
- `toNode`: The destination ID.
- `toPort`: The destination input.
- `System.ArgumentOutOfRangeException`: The stage is undefined.
- `System.ObjectDisposedException`: This graph is disposed.

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

- `type`: The canvas stage.
- `id`: An existing non-output ID.
- `System.ArgumentException`: The reserved output was selected.
- `System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `System.ObjectDisposedException`: This graph is disposed.

<a id="member-c7a10e092377"></a>
### `ReplaceNode`

```csharp
public System.Void ReplaceNode(Electron2D.VisualShader.Type type, System.Int32 id, System.String newClass)
```

Replaces an operation using an explicitly registered compiled resource factory.

An unchanged runtime type is a no-op. Positions and stored edges remain; import rejects any edges whose ports no longer exist. The graph owns the replacement until its final graph alias is removed. No assembly loading, member reflection or runtime source compilation occurs.

- `type`: The canvas stage.
- `id`: An existing node ID of at least two.
- `newClass`: The exact registered type ID, for example VisualShaderNodeVec3Constant.
- `System.ArgumentException`: The ID or registered class does not identify an ordinary graph operation.
- `System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `System.InvalidOperationException`: The compiled factory violates its freshness/type contract.
- `System.ObjectDisposedException`: The graph is disposed.
- `System.AggregateException`: Independent observer and retirement failures occur after commitment.

<a id="member-a985cea8c0e1"></a>
### `SetNodePosition`

```csharp
public System.Void SetNodePosition(Electron2D.VisualShader.Type type, System.Int32 id, Electron2D.Vector2 position)
```

Changes a node's finite authoring position without changing compiled code.

- `type`: The canvas stage.
- `id`: An existing node ID.
- `position`: Finite graph coordinates.
- `System.ArgumentException`: The position is nonfinite.
- `System.ArgumentOutOfRangeException`: The stage or node does not exist.
- `System.ObjectDisposedException`: This graph is disposed.

## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
