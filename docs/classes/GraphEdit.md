# GraphEdit

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.GraphEdit`. **Source:** [source](../../src/Scene/GUI/GraphEdit.cs). **Component:** [Graph authoring](../components/graph-authoring.md).

## Description

Interactive graph authoring over existing controls and retained canvas. Wiring/delete gestures issue typed application requests; applications explicitly create connections or mutate scene nodes. Grid/zoom/pan/minimap/menu, selection, movement, compressed-port queries, deterministic arrangement and logical nested frames execute. Name snapshots are copied; event name memory is borrowed only for callback duration.

## Members

| Declaration | Contract |
| --- | --- |
| [`public GraphEdit()`](#member-0d475bd05498) | Creates a clipped graph editor with owned navigation controls and drawing layers. |
| [`public event System.Action BeginNodeMove`](#member-0f3e5e32e95c) | Reports the BeginNodeMove graph interaction. |
| [`public event System.Action ConnectionDragEnded`](#member-3da4aa631ad7) | Reports the ConnectionDragEnded graph interaction. |
| [`public event System.Action<System.String, System.Int32, System.Boolean> ConnectionDragStarted`](#member-3bc9b056f963) | Reports the ConnectionDragStarted graph interaction. |
| [`public event System.Action<System.String, System.Int32, Electron2D.Vector2> ConnectionFromEmpty`](#member-c3d534e1101f) | Reports the ConnectionFromEmpty graph interaction. |
| [`public event System.Action<System.String, System.Int32, System.String, System.Int32> ConnectionRequest`](#member-338748649609) | Reports the ConnectionRequest graph interaction. |
| [`public event System.Action<System.String, System.Int32, Electron2D.Vector2> ConnectionToEmpty`](#member-c716efe1e04b) | Reports the ConnectionToEmpty graph interaction. |
| [`public event System.Action CopyNodesRequest`](#member-41b0b826a06e) | Reports the CopyNodesRequest graph interaction. |
| [`public event System.Action CutNodesRequest`](#member-31c3f3b1a237) | Reports the CutNodesRequest graph interaction. |
| [`public event System.Action<System.ReadOnlyMemory<System.String>> DeleteNodesRequest`](#member-0031b2ea47ca) | Reports the DeleteNodesRequest graph interaction. |
| [`public event System.Action<System.String, System.Int32, System.String, System.Int32> DisconnectionRequest`](#member-9b8e7478a249) | Reports the DisconnectionRequest graph interaction. |
| [`public event System.Action DuplicateNodesRequest`](#member-18c825c8a502) | Reports the DuplicateNodesRequest graph interaction. |
| [`public event System.Action EndNodeMove`](#member-7cbae018344e) | Reports the EndNodeMove graph interaction. |
| [`public event System.Action<Electron2D.GraphFrame, Electron2D.Rect2> FrameRectChanged`](#member-083ed4b0a895) | Reports the FrameRectChanged graph interaction. |
| [`public event System.Action<System.ReadOnlyMemory<System.String>, System.String> GraphElementsLinkedToFrameRequest`](#member-42cc6e022a88) | Reports the GraphElementsLinkedToFrameRequest graph interaction. |
| [`public event System.Action<Electron2D.Node> NodeDeselected`](#member-f40107363828) | Reports the NodeDeselected graph interaction. |
| [`public event System.Action<Electron2D.Node> NodeSelected`](#member-f4092262f4be) | Reports the NodeSelected graph interaction. |
| [`public event System.Action PasteNodesRequest`](#member-d92d1f1c3f3c) | Reports the PasteNodesRequest graph interaction. |
| [`public event System.Action<Electron2D.Vector2> PopupRequest`](#member-dd3771c02127) | Reports the PopupRequest graph interaction. |
| [`public event System.Action<Electron2D.Vector2> ScrollOffsetChanged`](#member-d9bdef63998e) | Reports the ScrollOffsetChanged graph interaction. |
| [`public System.Void AddValidConnectionType(System.Int32 fromType, System.Int32 toType)`](#member-de7443f7d931) | Adds an allowed directed pair of port types. |
| [`public System.Void AddValidLeftDisconnectType(System.Int32 type)`](#member-1a3992b0db47) | Adds an interactive left disconnect type. |
| [`public System.Void AddValidRightDisconnectType(System.Int32 type)`](#member-16fbd69785e7) | Adds an interactive right disconnect type. |
| [`public System.Void ArrangeNodes()`](#member-a6680b2fba80) | Places the selected nodes, or all nodes when none are selected, in stable directed layers. |
| [`public System.Void AttachGraphElementToFrame(System.String element, System.String frame)`](#member-46ab21db9dfa) | Attaches an element logically without reparenting its scene node. |
| [`public System.Void ClearConnections()`](#member-db455db49788) | Clears connection records without removing graph elements. |
| [`public System.Void ConnectNode(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort, System.Boolean keepAlive = false)`](#member-b20f297d3581) | Adds a directed record without applying interactive type policy. |
| [`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`](#member-7b866e75f816) | Inherited scene/input/layout/storage lifetime hook. |
| [`public System.Void DetachGraphElementFromFrame(System.String element)`](#member-2fbf0a61bd8a) | Removes an element's logical attachment. |
| [`public System.Void DisconnectNode(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort)`](#member-7d6aa9a33d23) | Removes the named directed record if present. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-98b3d16f2dd4) | Inherited scene/input/layout/storage lifetime hook. |
| [`public System.Void ForceConnectionDragEnd()`](#member-d16187c82c89) | Cancels a current connection drag and emits its end event once. |
| [`public System.String[] GetAttachedNodesOfFrame(System.String frame)`](#member-631394fa0142) | Copies the names directly attached to a frame. |
| [`public System.Nullable<Electron2D.GraphConnection> GetClosestConnectionAtPoint(Electron2D.Vector2 point, System.Single maxDistance = 4f)`](#member-710ef9417717) | Finds a visible connection nearest a viewport-local point. |
| [`public System.Int32 GetConnectionCount(System.String fromNode, System.Int32 fromPort)`](#member-e74186ff3803) | Counts records incident on the named node and port. |
| [`public Electron2D.Vector2[] GetConnectionLine(Electron2D.Vector2 fromNode, Electron2D.Vector2 toNode)`](#member-98cffb87a38d) | Builds a caller-owned connection line using the virtual geometry hook. |
| [`public Electron2D.GraphConnection[] GetConnectionListFromNode(System.String node)`](#member-3c42ff129f3f) | Copies connection records incident on a node. |
| [`public Electron2D.GraphConnection[] GetConnectionsIntersectingWithRect(Electron2D.Rect2 rect)`](#member-fd61bc2f5d25) | Copies visible connections intersecting a viewport-local rectangle. |
| [`public Electron2D.GraphFrame GetElementFrame(System.String element)`](#member-627186b5c2e1) | Finds the borrowed logical parent frame. |
| [`public Electron2D.HBoxContainer GetMenuHBox()`](#member-434227aff59d) | Returns the borrowed menu container for application controls. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-e3e0f5719a75) | Inherited scene/input/layout/storage lifetime hook. |
| [`public System.Boolean IsNodeConnected(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort)`](#member-42019992620c) | Tests whether a directed record exists. |
| [`public System.Boolean IsValidConnectionType(System.Int32 fromType, System.Int32 toType)`](#member-473c9440b7c2) | Tests an explicitly registered directed pair; equal types are accepted automatically by interactive wiring. |
| [`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`](#member-9052784d72bc) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected virtual Electron2D.Vector2[] OnGetConnectionLine(Electron2D.Vector2 fromPosition, Electron2D.Vector2 toPosition)`](#member-4a5a2379edeb) | Builds a customized local connection line. |
| [`protected virtual System.Boolean OnIsInInputHotzone(Electron2D.GraphNode node, System.Int32 port, Electron2D.Vector2 mousePosition)`](#member-18de286ca601) | Tests a graph-local pointer against an input port's theme-sized hit region. |
| [`protected virtual System.Boolean OnIsInOutputHotzone(Electron2D.GraphNode node, System.Int32 port, Electron2D.Vector2 mousePosition)`](#member-ad3d915b711e) | Tests a graph-local pointer against an output port's theme-sized hit region. |
| [`protected virtual System.Boolean OnIsNodeHoverValid(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort)`](#member-a59afe9d4864) | Applies additional connection validation after the port type policy. |
| [`protected override System.Void OnNotification(System.Int32 what)`](#member-c19c6009b871) | Inherited scene/input/layout/storage lifetime hook. |
| [`public System.Void RemoveValidConnectionType(System.Int32 fromType, System.Int32 toType)`](#member-f123b680fb39) | Removes an allowed directed pair of port types. |
| [`public System.Void RemoveValidLeftDisconnectType(System.Int32 type)`](#member-48316bbd41b5) | Removes an interactive left disconnect type. |
| [`public System.Void RemoveValidRightDisconnectType(System.Int32 type)`](#member-b162baaf4103) | Removes an interactive right disconnect type. |
| [`public System.Void SetConnectionActivity(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort, System.Single amount)`](#member-af892acc6158) | Assigns a connection's finite activity blend toward the activity theme color. |
| [`public System.Void SetSelected(Electron2D.Node node)`](#member-39964fb6bfed) | Selects one direct graph element, or clears selection with null. |
| [`protected override System.Void ValidateDisposal()`](#member-daaf78266c0d) | Inherited scene/input/layout/storage lifetime hook. |
| [`public System.Boolean ConnectionLinesAntialiased { get; set; }`](#member-80aa87f2f172) | Gets or sets the graph ConnectionLinesAntialiased policy. |
| [`public System.Single ConnectionLinesCurvature { get; set; }`](#member-3018dee40344) | Gets or sets the graph ConnectionLinesCurvature policy. |
| [`public System.Single ConnectionLinesThickness { get; set; }`](#member-da4f30221473) | Gets or sets the graph ConnectionLinesThickness policy. |
| [`public Electron2D.GraphConnection[] Connections { get; set; }`](#member-84a9328c6319) | Gets or replaces the directed connection snapshot. |
| [`public Electron2D.GraphEdit.GridPatternMode GridPattern { get; set; }`](#member-dc07cac2855c) | Gets or sets the graph GridPattern policy. |
| [`public System.Boolean MinimapEnabled { get; set; }`](#member-f01cfb17daf3) | Gets or sets the graph MinimapEnabled policy. |
| [`public System.Single MinimapOpacity { get; set; }`](#member-d462e63666a5) | Gets or sets the graph MinimapOpacity policy. |
| [`public Electron2D.Vector2 MinimapSize { get; set; }`](#member-bb2e29f00404) | Gets or sets the graph MinimapSize policy. |
| [`public Electron2D.GraphEdit.PanningSchemeMode PanningScheme { get; set; }`](#member-0f3dceb28797) | Gets or sets the graph PanningScheme policy. |
| [`public System.Boolean RightDisconnects { get; set; }`](#member-31ccb316ae9f) | Gets or sets the graph RightDisconnects policy. |
| [`public Electron2D.Vector2 ScrollOffset { get; set; }`](#member-db7ab74b2373) | Gets or sets finite viewport scroll in scaled graph pixels, without emitting the user-scroll event. |
| [`public System.Boolean ShowArrangeButton { get; set; }`](#member-5315d150cea4) | Gets or sets the graph ShowArrangeButton policy. |
| [`public System.Boolean ShowGrid { get; set; }`](#member-c923702c0462) | Gets or sets the graph ShowGrid policy. |
| [`public System.Boolean ShowGridButtons { get; set; }`](#member-856497a9c555) | Gets or sets the graph ShowGridButtons policy. |
| [`public System.Boolean ShowMenu { get; set; }`](#member-7a159cf8bbf2) | Gets or sets the graph ShowMenu policy. |
| [`public System.Boolean ShowMinimapButton { get; set; }`](#member-f741fdeb8d96) | Gets or sets the graph ShowMinimapButton policy. |
| [`public System.Boolean ShowZoomButtons { get; set; }`](#member-bdc91875ba0d) | Gets or sets the graph ShowZoomButtons policy. |
| [`public System.Boolean ShowZoomLabel { get; set; }`](#member-084c27151cf3) | Gets or sets the graph ShowZoomLabel policy. |
| [`public System.Int32 SnappingDistance { get; set; }`](#member-add702a223ca) | Gets or sets the graph SnappingDistance policy. |
| [`public System.Boolean SnappingEnabled { get; set; }`](#member-1cb67dea5fcb) | Gets or sets the graph SnappingEnabled policy. |
| [`public System.Collections.Generic.Dictionary<System.Int32, System.String> TypeNames { get; set; }`](#member-12af875fc572) | Gets or replaces a copied map of integer port type names. |
| [`public System.Single Zoom { get; set; }`](#member-7bcc977aad33) | Gets or sets zoom around the viewport center, clamped to ZoomMin and ZoomMax. |
| [`public System.Single ZoomMax { get; set; }`](#member-bd2bf70e7711) | Gets or sets the positive maximum zoom, no smaller than ZoomMin. |
| [`public System.Single ZoomMin { get; set; }`](#member-fe9306c7dbf2) | Gets or sets the positive minimum zoom, no greater than ZoomMax. |
| [`public System.Single ZoomStep { get; set; }`](#member-d1bbbee36638) | Gets or sets the graph ZoomStep policy. |

## Example

```csharp
using var graph = new GraphEdit { Size = new(720, 480) };
var source = new GraphNode { Name = "Source", Title = "Source", PositionOffset = new(60, 100) };
var target = new GraphNode { Name = "Target", Title = "Target", PositionOffset = new(360, 100) };
source.AddChild(new Label { Text = "Output" });
target.AddChild(new Label { Text = "Input" });
source.SetSlot(0, false, 0, Colors.White, true, 1, Colors.Cyan);
target.SetSlot(0, true, 1, Colors.Cyan, false, 0, Colors.White);
graph.AddChild(source);
graph.AddChild(target);
graph.ConnectionRequest += (from, output, to, input) => graph.ConnectNode(from, output, to, input);
graph.DisconnectionRequest += graph.DisconnectNode;
graph.ConnectNode("Source", 0, "Target", 0);
```

## Lifetime and verification

Attached reads and mutations use the scene owner thread. Own mutations reject active port/connection drawing; borrowed resources remain caller-owned. GraphNode and GraphFrame titlebars and title labels, and GraphEdit menu/layers cannot be disposed independently. GraphElement/GraphEdit disposal releases owned children and event subscriptions. See the component for executable checks, preparation boundaries and remaining dependencies.

## Member details

<a id="member-0d475bd05498"></a>
### `GraphEdit()`

```csharp
public GraphEdit()
```

Creates a clipped graph editor with owned navigation controls and drawing layers.

<a id="member-0f3e5e32e95c"></a>
### `BeginNodeMove`

```csharp
public event System.Action BeginNodeMove
```

Reports the BeginNodeMove graph interaction.

<a id="member-3da4aa631ad7"></a>
### `ConnectionDragEnded`

```csharp
public event System.Action ConnectionDragEnded
```

Reports the ConnectionDragEnded graph interaction.

<a id="member-3bc9b056f963"></a>
### `ConnectionDragStarted`

```csharp
public event System.Action<System.String, System.Int32, System.Boolean> ConnectionDragStarted
```

Reports the ConnectionDragStarted graph interaction.

<a id="member-c3d534e1101f"></a>
### `ConnectionFromEmpty`

```csharp
public event System.Action<System.String, System.Int32, Electron2D.Vector2> ConnectionFromEmpty
```

Reports the ConnectionFromEmpty graph interaction.

<a id="member-338748649609"></a>
### `ConnectionRequest`

```csharp
public event System.Action<System.String, System.Int32, System.String, System.Int32> ConnectionRequest
```

Reports the ConnectionRequest graph interaction.

<a id="member-c716efe1e04b"></a>
### `ConnectionToEmpty`

```csharp
public event System.Action<System.String, System.Int32, Electron2D.Vector2> ConnectionToEmpty
```

Reports the ConnectionToEmpty graph interaction.

<a id="member-41b0b826a06e"></a>
### `CopyNodesRequest`

```csharp
public event System.Action CopyNodesRequest
```

Reports the CopyNodesRequest graph interaction.

<a id="member-31c3f3b1a237"></a>
### `CutNodesRequest`

```csharp
public event System.Action CutNodesRequest
```

Reports the CutNodesRequest graph interaction.

<a id="member-0031b2ea47ca"></a>
### `DeleteNodesRequest`

```csharp
public event System.Action<System.ReadOnlyMemory<System.String>> DeleteNodesRequest
```

Reports the DeleteNodesRequest graph interaction.

<a id="member-9b8e7478a249"></a>
### `DisconnectionRequest`

```csharp
public event System.Action<System.String, System.Int32, System.String, System.Int32> DisconnectionRequest
```

Reports the DisconnectionRequest graph interaction.

<a id="member-18c825c8a502"></a>
### `DuplicateNodesRequest`

```csharp
public event System.Action DuplicateNodesRequest
```

Reports the DuplicateNodesRequest graph interaction.

<a id="member-7cbae018344e"></a>
### `EndNodeMove`

```csharp
public event System.Action EndNodeMove
```

Reports the EndNodeMove graph interaction.

<a id="member-083ed4b0a895"></a>
### `FrameRectChanged`

```csharp
public event System.Action<Electron2D.GraphFrame, Electron2D.Rect2> FrameRectChanged
```

Reports the FrameRectChanged graph interaction.

<a id="member-42cc6e022a88"></a>
### `GraphElementsLinkedToFrameRequest`

```csharp
public event System.Action<System.ReadOnlyMemory<System.String>, System.String> GraphElementsLinkedToFrameRequest
```

Reports the GraphElementsLinkedToFrameRequest graph interaction.

<a id="member-f40107363828"></a>
### `NodeDeselected`

```csharp
public event System.Action<Electron2D.Node> NodeDeselected
```

Reports the NodeDeselected graph interaction.

<a id="member-f4092262f4be"></a>
### `NodeSelected`

```csharp
public event System.Action<Electron2D.Node> NodeSelected
```

Reports the NodeSelected graph interaction.

<a id="member-d92d1f1c3f3c"></a>
### `PasteNodesRequest`

```csharp
public event System.Action PasteNodesRequest
```

Reports the PasteNodesRequest graph interaction.

<a id="member-dd3771c02127"></a>
### `PopupRequest`

```csharp
public event System.Action<Electron2D.Vector2> PopupRequest
```

Reports the PopupRequest graph interaction.

<a id="member-d9bdef63998e"></a>
### `ScrollOffsetChanged`

```csharp
public event System.Action<Electron2D.Vector2> ScrollOffsetChanged
```

Reports the ScrollOffsetChanged graph interaction.

<a id="member-de7443f7d931"></a>
### `AddValidConnectionType(System.Int32, System.Int32)`

```csharp
public System.Void AddValidConnectionType(System.Int32 fromType, System.Int32 toType)
```

Adds an allowed directed pair of port types.

- `fromType`: Output type.
- `toType`: Input type.

<a id="member-1a3992b0db47"></a>
### `AddValidLeftDisconnectType(System.Int32)`

```csharp
public System.Void AddValidLeftDisconnectType(System.Int32 type)
```

Adds an interactive left disconnect type.

- `type`: Port type identifier.

<a id="member-16fbd69785e7"></a>
### `AddValidRightDisconnectType(System.Int32)`

```csharp
public System.Void AddValidRightDisconnectType(System.Int32 type)
```

Adds an interactive right disconnect type.

- `type`: Port type identifier.

<a id="member-a6680b2fba80"></a>
### `ArrangeNodes()`

```csharp
public System.Void ArrangeNodes()
```

Places the selected nodes, or all nodes when none are selected, in stable directed layers.

Cycles share a bounded final layer; graph-name order provides deterministic placement.

<a id="member-46ab21db9dfa"></a>
### `AttachGraphElementToFrame(System.String, System.String)`

```csharp
public System.Void AttachGraphElementToFrame(System.String element, System.String frame)
```

Attaches an element logically without reparenting its scene node.

- `element`: Direct element name.
- `frame`: Direct frame name.

<a id="member-db455db49788"></a>
### `ClearConnections()`

```csharp
public System.Void ClearConnections()
```

Clears connection records without removing graph elements.

<a id="member-b20f297d3581"></a>
### `ConnectNode(System.String, System.Int32, System.String, System.Int32, System.Boolean)`

```csharp
public System.Void ConnectNode(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort, System.Boolean keepAlive = false)
```

Adds a directed record without applying interactive type policy.

Already present records are unchanged; invalid arguments throw typed exceptions.

- `fromNode`: Output node name.
- `fromPort`: Compressed output index.
- `toNode`: Input node name.
- `toPort`: Compressed input index.
- `keepAlive`: Retain missing endpoints.

<a id="member-7b866e75f816"></a>
### `CreateSceneInstanceFactory()`

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-2fbf0a61bd8a"></a>
### `DetachGraphElementFromFrame(System.String)`

```csharp
public System.Void DetachGraphElementFromFrame(System.String element)
```

Removes an element's logical attachment.

- `element`: Element name.

<a id="member-7d6aa9a33d23"></a>
### `DisconnectNode(System.String, System.Int32, System.String, System.Int32)`

```csharp
public System.Void DisconnectNode(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort)
```

Removes the named directed record if present.

- `fromNode`: Output name.
- `fromPort`: Output index.
- `toNode`: Input name.
- `toPort`: Input index.

<a id="member-98b3d16f2dd4"></a>
### `Dispose(System.Boolean)`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-d16187c82c89"></a>
### `ForceConnectionDragEnd()`

```csharp
public System.Void ForceConnectionDragEnd()
```

Cancels a current connection drag and emits its end event once.

<a id="member-631394fa0142"></a>
### `GetAttachedNodesOfFrame(System.String)`

```csharp
public System.String[] GetAttachedNodesOfFrame(System.String frame)
```

Copies the names directly attached to a frame.

Caller-owned name snapshot.

- `frame`: Frame name.

<a id="member-710ef9417717"></a>
### `GetClosestConnectionAtPoint(Electron2D.Vector2, System.Single)`

```csharp
public System.Nullable<Electron2D.GraphConnection> GetClosestConnectionAtPoint(Electron2D.Vector2 point, System.Single maxDistance = 4f)
```

Finds a visible connection nearest a viewport-local point.

A value snapshot or null.

- `point`: Finite local point.
- `maxDistance`: Finite nonnegative distance.

<a id="member-e74186ff3803"></a>
### `GetConnectionCount(System.String, System.Int32)`

```csharp
public System.Int32 GetConnectionCount(System.String fromNode, System.Int32 fromPort)
```

Counts records incident on the named node and port.

Incident record count.

- `fromNode`: Endpoint name.
- `fromPort`: Endpoint index.

<a id="member-98cffb87a38d"></a>
### `GetConnectionLine(Electron2D.Vector2, Electron2D.Vector2)`

```csharp
public Electron2D.Vector2[] GetConnectionLine(Electron2D.Vector2 fromNode, Electron2D.Vector2 toNode)
```

Builds a caller-owned connection line using the virtual geometry hook.

Copied local points.

- `fromNode`: Finite start position.
- `toNode`: Finite end position.

<a id="member-3c42ff129f3f"></a>
### `GetConnectionListFromNode(System.String)`

```csharp
public Electron2D.GraphConnection[] GetConnectionListFromNode(System.String node)
```

Copies connection records incident on a node.

Caller-owned snapshot.

- `node`: Node name.

<a id="member-fd61bc2f5d25"></a>
### `GetConnectionsIntersectingWithRect(Electron2D.Rect2)`

```csharp
public Electron2D.GraphConnection[] GetConnectionsIntersectingWithRect(Electron2D.Rect2 rect)
```

Copies visible connections intersecting a viewport-local rectangle.

Caller-owned record snapshots.

- `rect`: Finite rectangle; negative sizes are normalized.

<a id="member-627186b5c2e1"></a>
### `GetElementFrame(System.String)`

```csharp
public Electron2D.GraphFrame GetElementFrame(System.String element)
```

Finds the borrowed logical parent frame.

Frame or null.

- `element`: Element name.

<a id="member-434227aff59d"></a>
### `GetMenuHBox()`

```csharp
public Electron2D.HBoxContainer GetMenuHBox()
```

Returns the borrowed menu container for application controls.

The owned horizontal menu.

<a id="member-e3e0f5719a75"></a>
### `GetPropertyDescriptors()`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-42019992620c"></a>
### `IsNodeConnected(System.String, System.Int32, System.String, System.Int32)`

```csharp
public System.Boolean IsNodeConnected(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort)
```

Tests whether a directed record exists.

Whether it exists.

- `fromNode`: Output name.
- `fromPort`: Output index.
- `toNode`: Input name.
- `toPort`: Input index.

<a id="member-473c9440b7c2"></a>
### `IsValidConnectionType(System.Int32, System.Int32)`

```csharp
public System.Boolean IsValidConnectionType(System.Int32 fromType, System.Int32 toType)
```

Tests an explicitly registered directed pair; equal types are accepted automatically by interactive wiring.

Whether explicitly registered.

- `fromType`: Output type.
- `toType`: Input type.

<a id="member-9052784d72bc"></a>
### `OnGUIInput(Electron2D.InputEvent)`

```csharp
protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-4a5a2379edeb"></a>
### `OnGetConnectionLine(Electron2D.Vector2, Electron2D.Vector2)`

```csharp
protected virtual Electron2D.Vector2[] OnGetConnectionLine(Electron2D.Vector2 fromPosition, Electron2D.Vector2 toPosition)
```

Builds a customized local connection line.

An owned point array with at least two points.

- `fromPosition`: Finite start.
- `toPosition`: Finite end.

<a id="member-18de286ca601"></a>
### `OnIsInInputHotzone(Electron2D.GraphNode, System.Int32, Electron2D.Vector2)`

```csharp
protected virtual System.Boolean OnIsInInputHotzone(Electron2D.GraphNode node, System.Int32 port, Electron2D.Vector2 mousePosition)
```

Tests a graph-local pointer against an input port's theme-sized hit region.

Whether it hits.

- `node`: Direct graph node.
- `port`: Compressed input port.
- `mousePosition`: Graph-local pixels.

<a id="member-ad3d915b711e"></a>
### `OnIsInOutputHotzone(Electron2D.GraphNode, System.Int32, Electron2D.Vector2)`

```csharp
protected virtual System.Boolean OnIsInOutputHotzone(Electron2D.GraphNode node, System.Int32 port, Electron2D.Vector2 mousePosition)
```

Tests a graph-local pointer against an output port's theme-sized hit region.

Whether it hits.

- `node`: Direct graph node.
- `port`: Compressed output port.
- `mousePosition`: Graph-local pixels.

<a id="member-a59afe9d4864"></a>
### `OnIsNodeHoverValid(System.String, System.Int32, System.String, System.Int32)`

```csharp
protected virtual System.Boolean OnIsNodeHoverValid(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort)
```

Applies additional connection validation after the port type policy.

True by default.

- `fromNode`: Output name.
- `fromPort`: Output index.
- `toNode`: Input name.
- `toPort`: Input index.

<a id="member-c19c6009b871"></a>
### `OnNotification(System.Int32)`

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-f123b680fb39"></a>
### `RemoveValidConnectionType(System.Int32, System.Int32)`

```csharp
public System.Void RemoveValidConnectionType(System.Int32 fromType, System.Int32 toType)
```

Removes an allowed directed pair of port types.

- `fromType`: Output type.
- `toType`: Input type.

<a id="member-48316bbd41b5"></a>
### `RemoveValidLeftDisconnectType(System.Int32)`

```csharp
public System.Void RemoveValidLeftDisconnectType(System.Int32 type)
```

Removes an interactive left disconnect type.

- `type`: Port type identifier.

<a id="member-b162baaf4103"></a>
### `RemoveValidRightDisconnectType(System.Int32)`

```csharp
public System.Void RemoveValidRightDisconnectType(System.Int32 type)
```

Removes an interactive right disconnect type.

- `type`: Port type identifier.

<a id="member-af892acc6158"></a>
### `SetConnectionActivity(System.String, System.Int32, System.String, System.Int32, System.Single)`

```csharp
public System.Void SetConnectionActivity(System.String fromNode, System.Int32 fromPort, System.String toNode, System.Int32 toPort, System.Single amount)
```

Assigns a connection's finite activity blend toward the activity theme color.

- `fromNode`: Output name.
- `fromPort`: Output index.
- `toNode`: Input name.
- `toPort`: Input index.
- `amount`: Finite blend clamped to zero through one.

<a id="member-39964fb6bfed"></a>
### `SetSelected(Electron2D.Node)`

```csharp
public System.Void SetSelected(Electron2D.Node node)
```

Selects one direct graph element, or clears selection with null.

- `node`: A direct graph element or null.

<a id="member-daaf78266c0d"></a>
### `ValidateDisposal()`

```csharp
protected override System.Void ValidateDisposal()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-80aa87f2f172"></a>
### `ConnectionLinesAntialiased`

```csharp
public System.Boolean ConnectionLinesAntialiased { get; set; }
```

Gets or sets the graph ConnectionLinesAntialiased policy.

Initially true.

<a id="member-3018dee40344"></a>
### `ConnectionLinesCurvature`

```csharp
public System.Single ConnectionLinesCurvature { get; set; }
```

Gets or sets the graph ConnectionLinesCurvature policy.

Initially .5f.

<a id="member-da4f30221473"></a>
### `ConnectionLinesThickness`

```csharp
public System.Single ConnectionLinesThickness { get; set; }
```

Gets or sets the graph ConnectionLinesThickness policy.

Initially 4.

<a id="member-84a9328c6319"></a>
### `Connections`

```csharp
public Electron2D.GraphConnection[] Connections { get; set; }
```

Gets or replaces the directed connection snapshot.

<a id="member-dc07cac2855c"></a>
### `GridPattern`

```csharp
public Electron2D.GraphEdit.GridPatternMode GridPattern { get; set; }
```

Gets or sets the graph GridPattern policy.

Initially GridPatternMode.Lines.

<a id="member-f01cfb17daf3"></a>
### `MinimapEnabled`

```csharp
public System.Boolean MinimapEnabled { get; set; }
```

Gets or sets the graph MinimapEnabled policy.

Initially true.

<a id="member-d462e63666a5"></a>
### `MinimapOpacity`

```csharp
public System.Single MinimapOpacity { get; set; }
```

Gets or sets the graph MinimapOpacity policy.

Initially .65f.

<a id="member-bb2e29f00404"></a>
### `MinimapSize`

```csharp
public Electron2D.Vector2 MinimapSize { get; set; }
```

Gets or sets the graph MinimapSize policy.

Initially new(240,160).

<a id="member-0f3dceb28797"></a>
### `PanningScheme`

```csharp
public Electron2D.GraphEdit.PanningSchemeMode PanningScheme { get; set; }
```

Gets or sets the graph PanningScheme policy.

Initially PanningSchemeMode.ScrollZooms.

<a id="member-31ccb316ae9f"></a>
### `RightDisconnects`

```csharp
public System.Boolean RightDisconnects { get; set; }
```

Gets or sets the graph RightDisconnects policy.

Initially false.

<a id="member-db7ab74b2373"></a>
### `ScrollOffset`

```csharp
public Electron2D.Vector2 ScrollOffset { get; set; }
```

Gets or sets finite viewport scroll in scaled graph pixels, without emitting the user-scroll event.

<a id="member-5315d150cea4"></a>
### `ShowArrangeButton`

```csharp
public System.Boolean ShowArrangeButton { get; set; }
```

Gets or sets the graph ShowArrangeButton policy.

Initially true.

<a id="member-c923702c0462"></a>
### `ShowGrid`

```csharp
public System.Boolean ShowGrid { get; set; }
```

Gets or sets the graph ShowGrid policy.

Initially true.

<a id="member-856497a9c555"></a>
### `ShowGridButtons`

```csharp
public System.Boolean ShowGridButtons { get; set; }
```

Gets or sets the graph ShowGridButtons policy.

Initially true.

<a id="member-7a159cf8bbf2"></a>
### `ShowMenu`

```csharp
public System.Boolean ShowMenu { get; set; }
```

Gets or sets the graph ShowMenu policy.

Initially true.

<a id="member-f741fdeb8d96"></a>
### `ShowMinimapButton`

```csharp
public System.Boolean ShowMinimapButton { get; set; }
```

Gets or sets the graph ShowMinimapButton policy.

Initially true.

<a id="member-bdc91875ba0d"></a>
### `ShowZoomButtons`

```csharp
public System.Boolean ShowZoomButtons { get; set; }
```

Gets or sets the graph ShowZoomButtons policy.

Initially true.

<a id="member-084c27151cf3"></a>
### `ShowZoomLabel`

```csharp
public System.Boolean ShowZoomLabel { get; set; }
```

Gets or sets the graph ShowZoomLabel policy.

Initially false.

<a id="member-add702a223ca"></a>
### `SnappingDistance`

```csharp
public System.Int32 SnappingDistance { get; set; }
```

Gets or sets the graph SnappingDistance policy.

Initially 20.

<a id="member-1cb67dea5fcb"></a>
### `SnappingEnabled`

```csharp
public System.Boolean SnappingEnabled { get; set; }
```

Gets or sets the graph SnappingEnabled policy.

Initially true.

<a id="member-12af875fc572"></a>
### `TypeNames`

```csharp
public System.Collections.Generic.Dictionary<System.Int32, System.String> TypeNames { get; set; }
```

Gets or replaces a copied map of integer port type names.

<a id="member-7bcc977aad33"></a>
### `Zoom`

```csharp
public System.Single Zoom { get; set; }
```

Gets or sets zoom around the viewport center, clamped to ZoomMin and ZoomMax.

<a id="member-bd2bf70e7711"></a>
### `ZoomMax`

```csharp
public System.Single ZoomMax { get; set; }
```

Gets or sets the positive maximum zoom, no smaller than ZoomMin.

<a id="member-fe9306c7dbf2"></a>
### `ZoomMin`

```csharp
public System.Single ZoomMin { get; set; }
```

Gets or sets the positive minimum zoom, no greater than ZoomMax.

<a id="member-d1bbbee36638"></a>
### `ZoomStep`

```csharp
public System.Single ZoomStep { get; set; }
```

Gets or sets the graph ZoomStep policy.

Initially 1.2f.
