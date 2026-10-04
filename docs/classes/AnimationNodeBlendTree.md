# AnimationNodeBlendTree

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationNodeBlendTree` · **Source:** [AnimationNodeBlendTree.cs](../../src/Scene/Animation/AnimationNodeBlendTree.cs).

**Inherits:** [AnimationRootNode](AnimationRootNode.md).

## Description

A named directed graph of borrowed animation resources with one owned output resource.

The reserved output exists on construction and cannot be renamed, removed or used as a source. AddNode borrows a named resource. Each source may feed at most one destination input; destinations may have multiple distinct inputs. Connect rejects occupied ports and cycles. Remove clears referring connections; Rename updates connection names and per-tree paths. Removing an input shifts existing connections by index, including equal captions. GraphOffset and positions are finite authoring metadata and do not change sampling. A duplicate owns a distinct output and connection containers.

Graph resources borrow child resources; the blend tree owns only its reserved output. Resource duplication copies definition containers, with shallow/deep resource aliases governed by Resource. Evaluation state and typed parameter cells belong to each AnimationTree and graph path. A definition may be shared sequentially across trees; concurrent processing of the same definition is unsupported. Scene controllers and parameter writes obey the inherited Node owner-thread rule. Resource edits invalidate prepared graph/binding state; cold preparation may allocate. No graph factory, disk format, editor UI or expression evaluator is supplied.

Input/child/parameter names use ordinal comparison. Missing names, invalid input indices, wrong typed keys, duplicate schemas and disconnected required inputs fail explicitly. Disposed resources throw ObjectDisposedException; processing helpers outside evaluation throw InvalidOperationException. Nonfinite times/weights/authoring positions reject. Graph cycles reject before connection/self-containment edits or during preparation for custom graphs. Mutation commits before observer notification; TreeChanged reaches every subscribed tree even when another handler throws, collecting graph notification failures. Authoring callback exceptions propagate; the caller can repair the committed graph. Disposal detaches observers and borrows child resources. Nested graph processing guards connection cycles; inherited mixer Advance rejects recursion and invalidates stale property commits after reentrant edits.

## Example

The complete public authoring pattern below requires existing live clips and a scene root. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) compiles it and the per-node variants.

```csharp
// Existing live clips a and b have typed property tracks targeting the same root.
using var library = new AnimationLibrary();
library.AddAnimation("a", a); library.AddAnimation("b", b);
using var graph = new AnimationNodeBlendTree();
using var first = new AnimationNodeAnimation { Animation = "a" };
using var second = new AnimationNodeAnimation { Animation = "b" };
using var blend = new AnimationNodeBlend2();
graph.AddNode("a", first); graph.AddNode("b", second); graph.AddNode("mix", blend);
graph.ConnectNode("mix", 0, "a"); graph.ConnectNode("mix", 1, "b");
graph.ConnectNode("output", 0, "mix");
var controller = new AnimationTree { TreeRoot = graph };
controller.AddAnimationLibrary("", library);
sceneRoot.AddChild(controller); // Existing owner-thread scene root/targets.
controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, 0.5);
controller.Advance(0);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeBlendTree()` | Creates the owned reserved output node. |

## Constructor Descriptions

<a id="member-d2b5d7d868e1"></a>
### .ctor

`public AnimationNodeBlendTree()`

Creates the owned reserved output node.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.Vector2 GraphOffset { get; set; }` | Gets or sets finite editor graph-offset metadata. |

## Property Descriptions

<a id="member-3cbc5f7f602e"></a>
### GraphOffset

`public Electron2D.Vector2 GraphOffset { get; set; }`

Gets or sets finite editor graph-offset metadata.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.ArgumentOutOfRangeException: A numeric value is nonfinite or outside the stated domain.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddNode(System.String name, Electron2D.AnimationNode node, Electron2D.Vector2 position = default)` | Adds a borrowed graph resource. |
| `public System.Void ConnectNode(System.String inputNode, System.Int32 inputIndex, System.String outputNode)` | Connects a source node to one destination input, rejecting occupancy and cycles. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `public System.Void DisconnectNode(System.String inputNode, System.Int32 inputIndex)` | Disconnects an existing destination input. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `public Electron2D.AnimationNode GetNode(System.String name)` | Returns a borrowed named resource, including the owned output resource. |
| `public System.String[] GetNodeList()` | Returns an independent ordinal-sorted array of local names. |
| `public Electron2D.Vector2 GetNodePosition(System.String name)` | Returns finite authoring-position metadata. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `public System.Boolean HasNode(System.String name)` | Tests exact local name membership. |
| `protected override System.String OnGetCaption()` | Supplies the graph resource's caption. |
| `protected override Electron2D.AnimationNode OnGetChildByName(System.String name)` | Finds a named child definition. |
| `protected override System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, Electron2D.AnimationNode>> OnGetChildNodes()` | Returns ordered named child definitions; no scene nodes are created. |
| `protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)` | Evaluates custom graph behavior; helpers can blend inputs, children and clips. |
| `public System.Void RemoveNode(System.String name)` | Removes a borrowed node and every connection referencing it; output cannot be removed. |
| `public System.Void RenameNode(System.String name, System.String newName)` | Renames a borrowed node and updates its connection names. |
| `public System.Void SetNodePosition(System.String name, Electron2D.Vector2 position)` | Changes authoring-position metadata without changing playback. |

## Method Descriptions

<a id="member-e83cc1e25a08"></a>
### AddNode

`public System.Void AddNode(System.String name, Electron2D.AnimationNode node, Electron2D.Vector2 position = default)`

Adds a borrowed graph resource.

name: A nonempty unique local name without slash; output is reserved.

node: A live resource other than this graph.

position: Finite authoring position metadata.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.ArgumentException: The name/port is invalid or already occupied.

<a id="member-601286f44c9e"></a>
### ConnectNode

`public System.Void ConnectNode(System.String inputNode, System.Int32 inputIndex, System.String outputNode)`

Connects a source node to one destination input, rejecting occupancy and cycles.

inputNode: The destination name, including output.

inputIndex: The destination input index.

outputNode: The source name; output is not a source.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.ArgumentException: The name/port is invalid or already occupied.

<a id="member-2cbfc38e875f"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`

Copies derived stored state into a duplicate or copy target.

target: A live resource with the exact same runtime type.

deep: Whether typed collection containers should be cloned recursively.

subresourceMode: The nested-resource policy for this copy.

duplicateSubresource: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource. Pass every nested resource through this function when deep is true.

forceDuplicateSubresource: A graph-preserving function that duplicates a nested resource even when the current policy would share it. Use it for typed properties whose contract requires duplication; assign the original reference directly for properties whose contract forbids duplication.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Derived implementations must copy all stored custom state and call the base implementation only when they intentionally want its validation. Assigning the original nested-resource reference directly expresses a never-duplicate property.

System.NotSupportedException: A derived resource has not explicitly implemented custom-state copying.

<a id="member-3359c0752f4e"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-2af9a428a161"></a>
### DisconnectNode

`public System.Void DisconnectNode(System.String inputNode, System.Int32 inputIndex)`

Disconnects an existing destination input.

inputNode: The existing destination name.

inputIndex: The input index.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-f70c48b4aa40"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-d0d48525f8eb"></a>
### GetNode

`public Electron2D.AnimationNode GetNode(System.String name)`

Returns a borrowed named resource, including the owned output resource.

name: The exact local name.

Returns: The live graph resource.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-26b6b844a3b6"></a>
### GetNodeList

`public System.String[] GetNodeList()`

Returns an independent ordinal-sorted array of local names.

Returns: The graph names, including output.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-5be888198673"></a>
### GetNodePosition

`public Electron2D.Vector2 GetNodePosition(System.String name)`

Returns finite authoring-position metadata.

name: The existing name.

Returns: The stored position.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-90e5b00d3cdf"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

<a id="member-d1a4fab636a3"></a>
### HasNode

`public System.Boolean HasNode(System.String name)`

Tests exact local name membership.

name: The non-null name.

Returns: Whether the node exists.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-1226dc146708"></a>
### OnGetCaption

`protected override System.String OnGetCaption()`

Supplies the graph resource's caption.

Returns: The caption; defaults to Node.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-a40ab4a78b7c"></a>
### OnGetChildByName

`protected override Electron2D.AnimationNode OnGetChildByName(System.String name)`

Finds a named child definition.

name: The exact local name.

Returns: The borrowed child or null.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-8905baacd134"></a>
### OnGetChildNodes

`protected override System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, Electron2D.AnimationNode>> OnGetChildNodes()`

Returns ordered named child definitions; no scene nodes are created.

Returns: The graph's borrowed children.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-84d442adf7b6"></a>
### OnProcess

`protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)`

Evaluates custom graph behavior; helpers can blend inputs, children and clips.

time: Relative delta seconds, or absolute time when seeking.

seek: Whether time is absolute.

isExternalSeeking: Whether the seek originated outside startup.

testOnly: Whether persistent changes and mixer writes are suppressed.

Returns: The selected remaining duration, or zero when no input is evaluated.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-8a942d434798"></a>
### RemoveNode

`public System.Void RemoveNode(System.String name)`

Removes a borrowed node and every connection referencing it; output cannot be removed.

name: The existing non-output name.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-3d560b2ca13a"></a>
### RenameNode

`public System.Void RenameNode(System.String name, System.String newName)`

Renames a borrowed node and updates its connection names.

name: The existing non-output name.

newName: The valid unoccupied replacement name.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.ArgumentException: The name/port is invalid or already occupied.

<a id="member-2c687e3a1a7e"></a>
### SetNodePosition

`public System.Void SetNodePosition(System.String name, Electron2D.Vector2 position)`

Changes authoring-position metadata without changing playback.

name: The existing local name.

position: The finite position.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.ArgumentOutOfRangeException: A numeric value is nonfinite or outside the stated domain.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<System.String> NodeChanged` | Occurs when a named child's content or input definition changes. |

## Event Descriptions

<a id="member-9e9d1c1c66a0"></a>
### NodeChanged

`public event System.Action<System.String> NodeChanged`

Occurs when a named child's content or input definition changes.

## Constant summary

| Complete C# signature | Contract |
| --- | --- |
| `public const System.Int32 ConnectionErrorConnectionExists = 5` | An input or source connection is already occupied. |
| `public const System.Int32 ConnectionErrorNoInput = 1` | The destination does not exist. |
| `public const System.Int32 ConnectionErrorNoInputIndex = 2` | The destination input index is invalid. |
| `public const System.Int32 ConnectionErrorNoOutput = 3` | The source is missing or is the reserved output resource. |
| `public const System.Int32 ConnectionErrorSameNode = 4` | The source and destination are identical. |
| `public const System.Int32 ConnectionOK = 0` | A valid connection. |

## Constant Descriptions

<a id="member-de4293d40468"></a>
### ConnectionErrorConnectionExists

`public const System.Int32 ConnectionErrorConnectionExists = 5`

An input or source connection is already occupied.

<a id="member-2092b7594b00"></a>
### ConnectionErrorNoInput

`public const System.Int32 ConnectionErrorNoInput = 1`

The destination does not exist.

<a id="member-42a83e88dcd3"></a>
### ConnectionErrorNoInputIndex

`public const System.Int32 ConnectionErrorNoInputIndex = 2`

The destination input index is invalid.

<a id="member-6fc239ad1e5b"></a>
### ConnectionErrorNoOutput

`public const System.Int32 ConnectionErrorNoOutput = 3`

The source is missing or is the reserved output resource.

<a id="member-a472ee41c5e9"></a>
### ConnectionErrorSameNode

`public const System.Int32 ConnectionErrorSameNode = 4`

The source and destination are identical.

<a id="member-acfa0068ab46"></a>
### ConnectionOK

`public const System.Int32 ConnectionOK = 0`

A valid connection.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines, transitions, OneShot, BlendSpaces, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
