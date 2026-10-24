# VisualShaderNodeReroute

Last updated: 2026-10-25

Source: [VisualShaderNodeReroute.cs](../../src/Scene/Resources/VisualShaderNodeReroute.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeReroute : VisualShaderNode
```

## Description

Forwards one input to one output without conversion inside the node. Validated graph connections select its role and propagate that role through downstream reroutes. Scalar, signed/unsigned integer, Boolean, Vector2/3/4, numeric Matrix4x4 and opaque sampled-image roles execute through the ordinary offline importer in canvas vertex/fragment, five particle lifecycle stages and TextureBlit.

## Members

<a id="constructor"></a>
### Constructor

```csharp
public VisualShaderNodeReroute()
```

Creates a scalar reroute with an explicit zero default.

<a id="get-port-type"></a>
### GetPortType

```csharp
public VisualShaderNode.PortType GetPortType()
```

Returns the current role of both port zero inputs/outputs. Throws ObjectDisposedException after disposal. A validated connection into a reroute selects the upstream output role across the entire downstream reroute branch, provided every terminal input is compatible. An unconnected source reroute can instead adopt its destination role. Feasibility checks do not mutate the graph. Invalid edits retain all previous roles, defaults and edges.

```csharp
using var graph = new VisualShader();
using var matrix = new VisualShaderNodeMatrixConstant();
using var route = new VisualShaderNodeReroute();
graph.AddNode(VisualShader.Type.Fragment, matrix, Vector2.Zero, 2);
graph.AddNode(VisualShader.Type.Fragment, route, Vector2.Zero, 3);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0);
// route.GetPortType() returns VisualShaderNode.PortType.Matrix.
```

## Defaults, storage and failure behavior

A changed role resets its default to typed zero, or identity for a matrix; sampler roles have no numeric default. An unchanged role preserves authored defaults and explicit absence. Disconnection retains the selected role/default. Inherited typed default methods validate the current role. Forced connections retain explicit roles and bypass inference; import rejects incompatible ports and reachable cycles before publishing a compiled artifact.

Sampled-image chains forward the original graph-local symbol, preserving ordinary versus array shape and image policy. No local sampler or numerical substitute is emitted. An unbound sampler remains absent and uses the downstream node's existing absence policy.

Graph edits commit the whole branch and edge before publishing change events; observer exceptions cannot leave a partly changed chain. Borrowed definition aliases share the selected role, including across stages/graphs. As with other shared definitions, edits can invalidate another borrowing graph; that graph must pass import validation again. Portable archives and deep duplicates preserve the role, typed default table, absence and resource aliases.

See [the author/import workflow and verification limits](../components/visual-shaders.md#typed-reroute-chains). Source authoring is cold work; executing imported materials runs no graph interpreter or runtime compiler.
