# AnimationNodeStateMachine

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationNodeStateMachine` · **Source:** [AnimationNodeStateMachine.cs](../../src/Scene/Animation/AnimationNodeStateMachine.cs).

**Inherits:** [AnimationRootNode](AnimationRootNode.md).

## Description

A reusable graph of animation states with per-tree playback and borrowed edge policies.

Owns Start/End boundary resources and borrows ordinary AnimationRootNode states and transition policies. Names are ordinal, nonblank and slash-free; positions are finite metadata and influence geometric travel cost. Add/replace reject containment cycles and nonroot resources. Remove/rename/replace commit topology before notifications, preserve borrowed lifetime and propagate graph invalidation despite observer failures. Rename retains selected/route state identity. Shallow copying preserves resources; deep copying uses Resource graph aliases. Boundaries cannot be replaced, renamed or removed. Graph offset is authoring metadata. Root startup resets at Start; Nested resets its selected timeline; Grouped requires a direct state-machine parent. End remains active and completed; a Nested state without outgoing edges exposes finite remaining time. ResetEnds controls boundary blending against rest/RESET except grouped boundaries. Playback is an owned read-only cell per tree/path; borrow it and reacquire after graph replacement. No packed-scene or disk graph factory is supplied.

See [state machines](../components/scene-animation.md#state-machines) for runtime order, integration and verification boundaries. Scene controllers execute on their SceneTree owner thread; resource authoring retains the Resource thread/lifetime contract. No external backend dependency or second scene clock is introduced.

## Example

This snippet requires the indicated live tree, borrowed clip definitions and library/target setup. AnimationStateMachineTests compiles and exercises this public workflow.

```csharp
using var machine = new AnimationNodeStateMachine();
machine.AddNode("idle", idleClipNode);
machine.AddNode("run", runClipNode);
machine.AddTransition("idle", "run", transition);
tree.TreeRoot = machine;
var playback = tree.GetParameter("", AnimationNodeStateMachine.Playback);
playback.Start("idle");
tree.Advance(0);
playback.Travel("run");
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeStateMachine()` | Creates the owned Start and End boundary states. |

## Constructor Descriptions

<a id="member-eec69c7f2116"></a>
### .ctor

`public AnimationNodeStateMachine()`

Creates the owned Start and End boundary states.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean AllowTransitionToSelf { get; set; }` | Gets or sets whether travel to the current state may teleport and reset it. |
| `public System.Boolean ResetEnds { get; set; }` | Gets or sets whether fades through Start/End blend against the mixer rest pose. |
| `public Electron2D.AnimationStateMachineType StateMachineType { get; set; }` | Gets or sets root, nested or grouped execution semantics. |

## Property Descriptions

<a id="member-539b57d83517"></a>
### AllowTransitionToSelf

`public System.Boolean AllowTransitionToSelf { get; set; }`

Gets or sets whether travel to the current state may teleport and reset it.

Value: Initially false.

System.ObjectDisposedException: The resource is disposed.

<a id="member-83125875960e"></a>
### ResetEnds

`public System.Boolean ResetEnds { get; set; }`

Gets or sets whether fades through Start/End blend against the mixer rest pose.

Value: Initially false; grouped boundaries always preserve the surrounding pose.

System.ObjectDisposedException: The resource is disposed.

<a id="member-ee0c1ef73aa5"></a>
### StateMachineType

`public Electron2D.AnimationStateMachineType StateMachineType { get; set; }`

Gets or sets root, nested or grouped execution semantics.

Value: Initially Root.

System.ArgumentOutOfRangeException: The enumeration is undefined.

System.ObjectDisposedException: The resource is disposed.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddNode(System.String name, Electron2D.AnimationNode node, Electron2D.Vector2 position = default)` | Adds a borrowed root resource as a named state. |
| `public System.Void AddTransition(System.String from, System.String to, Electron2D.AnimationNodeStateMachineTransition transition)` | Adds a unique directed borrowed policy between local states. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource lifecycle for this concrete type. |
| `public Electron2D.Vector2 GetGraphOffset()` | Returns graph-view offset metadata. |
| `public Electron2D.AnimationNode GetNode(System.String name)` | Returns the borrowed state resource, including owned boundaries. |
| `public System.String[] GetNodeList()` | Returns an independent ordinal-sorted state-name snapshot. |
| `public System.String GetNodeName(Electron2D.AnimationNode node)` | Returns the first name using this exact resource, or an empty string. |
| `public Electron2D.Vector2 GetNodePosition(System.String name)` | Returns a state's authoring coordinates. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource lifecycle for this concrete type. |
| `public Electron2D.AnimationNodeStateMachineTransition GetTransition(System.Int32 idx)` | Returns a borrowed edge policy. |
| `public System.Int32 GetTransitionCount()` | Returns the current edge count. |
| `public System.String GetTransitionFrom(System.Int32 idx)` | Returns an edge's source name. |
| `public System.String GetTransitionTo(System.Int32 idx)` | Returns an edge's destination name. |
| `public System.Boolean HasNode(System.String name)` | Tests exact local state membership. |
| `public System.Boolean HasTransition(System.String from, System.String to)` | Tests exact directed edge membership. |
| `protected override System.String OnGetCaption()` | Supplies the graph resource's caption. |
| `protected override Electron2D.AnimationNode OnGetChildByName(System.String name)` | Finds a named child definition. |
| `protected override System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, Electron2D.AnimationNode>> OnGetChildNodes()` | Returns ordered named child definitions; no scene nodes are created. |
| `protected override TValue OnGetParameterDefaultValue<TValue>(AnimationParameter<TValue> parameter)` | Supplies one parameter's typed default for a newly prepared tree instance. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()` | Supplies immutable typed parameter definitions. |
| `protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)` | Evaluates custom graph behavior; helpers can blend inputs, children and clips. |
| `public System.Void RemoveNode(System.String name)` | Removes an ordinary state and all incident edges, retaining borrowed resources. |
| `public System.Void RemoveTransition(System.String from, System.String to)` | Removes an exact edge. |
| `public System.Void RemoveTransitionByIndex(System.Int32 idx)` | Removes an edge while retaining its borrowed policy. |
| `public System.Void RenameNode(System.String name, System.String newName)` | Renames an ordinary state and its edges; prepared playback retains the state's identity. |
| `public System.Void ReplaceNode(System.String name, Electron2D.AnimationNode node)` | Replaces an ordinary state's borrowed resource while preserving position and edges. |
| `public System.Void SetGraphOffset(Electron2D.Vector2 offset)` | Stores finite graph-view offset metadata. |
| `public System.Void SetNodePosition(System.String name, Electron2D.Vector2 position)` | Sets finite state coordinates used by travel. |

## Method Descriptions

<a id="member-937245835877"></a>
### AddNode

`public System.Void AddNode(System.String name, Electron2D.AnimationNode node, Electron2D.Vector2 position = default)`

Adds a borrowed root resource as a named state.

name: A unique nonblank local name without slash.

node: A live root resource without containment cycles.

position: Finite authoring coordinates, used by travel cost.

System.ArgumentException: The name or resource is invalid.

System.ArgumentOutOfRangeException: The position is nonfinite.

System.ObjectDisposedException: The machine or child is disposed.

<a id="member-d45cfc13dc6f"></a>
### AddTransition

`public System.Void AddTransition(System.String from, System.String to, Electron2D.AnimationNodeStateMachineTransition transition)`

Adds a unique directed borrowed policy between local states.

from: An existing source other than End.

to: An existing destination other than Start or the source.

transition: A live borrowed edge policy.

System.ArgumentException: The edge is invalid or occupied.

System.Collections.Generic.KeyNotFoundException: A state is absent.

System.ObjectDisposedException: The machine or policy is disposed.

<a id="member-91485ff1590d"></a>
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

<a id="member-9e8366803a98"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-e1a52b3de108"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-fe3bc94f4b63"></a>
### GetGraphOffset

`public Electron2D.Vector2 GetGraphOffset()`

Returns graph-view offset metadata.

Returns: The stored coordinates.

System.ObjectDisposedException: The machine is disposed.

<a id="member-0ebe518c7a90"></a>
### GetNode

`public Electron2D.AnimationNode GetNode(System.String name)`

Returns the borrowed state resource, including owned boundaries.

name: An existing local name.

Returns: The state resource.

System.Collections.Generic.KeyNotFoundException: The state is absent.

System.ObjectDisposedException: The machine is disposed.

<a id="member-7302e3f34e77"></a>
### GetNodeList

`public System.String[] GetNodeList()`

Returns an independent ordinal-sorted state-name snapshot.

Returns: Names including Start and End.

System.ObjectDisposedException: The machine is disposed.

<a id="member-cc8fa3d4b61e"></a>
### GetNodeName

`public System.String GetNodeName(Electron2D.AnimationNode node)`

Returns the first name using this exact resource, or an empty string.

node: The borrowed resource identity.

Returns: A local name or empty string.

System.ObjectDisposedException: The machine is disposed.

<a id="member-27ea0bb6461f"></a>
### GetNodePosition

`public Electron2D.Vector2 GetNodePosition(System.String name)`

Returns a state's authoring coordinates.

name: An existing state.

Returns: The finite coordinates.

System.Collections.Generic.KeyNotFoundException: The state is absent.

System.ObjectDisposedException: The machine is disposed.

<a id="member-f99a8726260d"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

<a id="member-04d7e47fe6cd"></a>
### GetTransition

`public Electron2D.AnimationNodeStateMachineTransition GetTransition(System.Int32 idx)`

Returns a borrowed edge policy.

idx: An existing edge index.

Returns: The borrowed policy.

System.ArgumentOutOfRangeException: The index is outside the edge list.

System.ObjectDisposedException: The machine is disposed.

<a id="member-1913b5e3f8bd"></a>
### GetTransitionCount

`public System.Int32 GetTransitionCount()`

Returns the current edge count.

Returns: The count.

System.ObjectDisposedException: The machine is disposed.

<a id="member-b19d58b573d1"></a>
### GetTransitionFrom

`public System.String GetTransitionFrom(System.Int32 idx)`

Returns an edge's source name.

idx: An existing edge index.

Returns: The exact local source.

System.ArgumentOutOfRangeException: The index is outside the edge list.

System.ObjectDisposedException: The machine is disposed.

<a id="member-2d8411fa1a14"></a>
### GetTransitionTo

`public System.String GetTransitionTo(System.Int32 idx)`

Returns an edge's destination name.

idx: An existing edge index.

Returns: The exact local destination.

System.ArgumentOutOfRangeException: The index is outside the edge list.

System.ObjectDisposedException: The machine is disposed.

<a id="member-ca93f814b159"></a>
### HasNode

`public System.Boolean HasNode(System.String name)`

Tests exact local state membership.

name: The non-null name.

Returns: Whether the state exists.

System.ObjectDisposedException: The machine is disposed.

<a id="member-9c6ab745842c"></a>
### HasTransition

`public System.Boolean HasTransition(System.String from, System.String to)`

Tests exact directed edge membership.

from: The source name.

to: The destination name.

Returns: Whether the edge exists.

System.ObjectDisposedException: The machine is disposed.

<a id="member-1ad549fda102"></a>
### OnGetCaption

`protected override System.String OnGetCaption()`

Supplies the graph resource's caption.

Returns: The caption; defaults to Node.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-be0f0146ac4b"></a>
### OnGetChildByName

`protected override Electron2D.AnimationNode OnGetChildByName(System.String name)`

Finds a named child definition.

name: The exact local name.

Returns: The borrowed child or null.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-348dfd89a4f8"></a>
### OnGetChildNodes

`protected override System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, Electron2D.AnimationNode>> OnGetChildNodes()`

Returns ordered named child definitions; no scene nodes are created.

Returns: The graph's borrowed children.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-49021cf920dd"></a>
### OnGetParameterDefaultValue

`protected override TValue OnGetParameterDefaultValue<TValue>(AnimationParameter<TValue> parameter)`

Supplies one parameter's typed default for a newly prepared tree instance.

TValue: The exact parameter type.

parameter: The schema definition.

Returns: The initial typed value.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-d0cc202f92d8"></a>
### OnGetParameterList

`protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()`

Supplies immutable typed parameter definitions.

Returns: The node's parameter schema, including inherited timing slots.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-0d5c2525a3c2"></a>
### OnProcess

`protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)`

Evaluates custom graph behavior; helpers can blend inputs, children and clips.

time: Relative delta seconds, or absolute time when seeking.

seek: Whether time is absolute.

isExternalSeeking: Whether the seek originated outside startup.

testOnly: Whether persistent changes and mixer writes are suppressed.

Returns: The selected remaining duration, or zero when no input is evaluated.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-c6f99bcf6eb6"></a>
### RemoveNode

`public System.Void RemoveNode(System.String name)`

Removes an ordinary state and all incident edges, retaining borrowed resources.

name: An existing ordinary state.

System.ArgumentException: The state is reserved.

System.Collections.Generic.KeyNotFoundException: The state is absent.

System.ObjectDisposedException: The machine is disposed.

<a id="member-a2a3e1359fd6"></a>
### RemoveTransition

`public System.Void RemoveTransition(System.String from, System.String to)`

Removes an exact edge.

from: The source name.

to: The destination name.

System.ArgumentException: The edge is absent.

System.ObjectDisposedException: The machine is disposed.

<a id="member-259ec410ed13"></a>
### RemoveTransitionByIndex

`public System.Void RemoveTransitionByIndex(System.Int32 idx)`

Removes an edge while retaining its borrowed policy.

idx: An existing edge index.

System.ArgumentOutOfRangeException: The index is outside the edge list.

System.ObjectDisposedException: The machine is disposed.

<a id="member-23038122870d"></a>
### RenameNode

`public System.Void RenameNode(System.String name, System.String newName)`

Renames an ordinary state and its edges; prepared playback retains the state's identity.

name: An existing ordinary state.

newName: A unique valid local name.

System.ArgumentException: A name is invalid, occupied or reserved.

System.Collections.Generic.KeyNotFoundException: The source is absent.

System.ObjectDisposedException: The machine is disposed.

<a id="member-f2c5cb9864c5"></a>
### ReplaceNode

`public System.Void ReplaceNode(System.String name, Electron2D.AnimationNode node)`

Replaces an ordinary state's borrowed resource while preserving position and edges.

name: An existing ordinary state.

node: A live root resource without containment cycles.

System.ArgumentException: The state is reserved or the child is invalid.

System.Collections.Generic.KeyNotFoundException: The state is absent.

System.ObjectDisposedException: The machine or child is disposed.

<a id="member-b2dd84e3dbed"></a>
### SetGraphOffset

`public System.Void SetGraphOffset(Electron2D.Vector2 offset)`

Stores finite graph-view offset metadata.

offset: Finite coordinates.

System.ArgumentOutOfRangeException: The coordinates are nonfinite.

System.ObjectDisposedException: The machine is disposed.

<a id="member-e8e156440257"></a>
### SetNodePosition

`public System.Void SetNodePosition(System.String name, Electron2D.Vector2 position)`

Sets finite state coordinates used by travel.

name: An existing state, including boundaries.

position: Finite coordinates.

System.ArgumentOutOfRangeException: The coordinates are nonfinite.

System.Collections.Generic.KeyNotFoundException: The state is absent.

System.ObjectDisposedException: The machine is disposed.

## Field summary

| Complete C# signature | Contract |
| --- | --- |
| `public static readonly Electron2D.AnimationParameter<Electron2D.AnimationNodeStateMachinePlayback> Playback` | The read-only per-tree/path playback controller, owned by the tree. |

## Field Descriptions

<a id="member-5c9804c26993"></a>
### Playback

`public static readonly Electron2D.AnimationParameter<Electron2D.AnimationNodeStateMachinePlayback> Playback`

The read-only per-tree/path playback controller, owned by the tree.


## Verification and dependencies

[AnimationStateMachineTests](../../tests/Electron2D.Tests/AnimationStateMachineTests.cs) covers public authoring, defaults, edge guards, copies/aliases, route costs/fallback, timing/fades/boundaries, typed conditions, multiple groups, state events, independent sharing, rename/removal, preparation and observer failures, reentry, tested output and method/nested effects. Warm checks use 64 prepared command/route cycles, 64 cached grouped starts and 64 idle frames with zero owner managed bytes. Separate SDL-dummy/FAudio PCM checks verify state audio start/restart/stop and departed nonblended cues. Linux Wayland GPU/compatibility hosts each run twice and check six actual pixel poses and borrowed-resource cleanup. Native/driver allocator totals, physical listening, other platforms, file/editor and human acceptance remain unverified. [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed graph/condition/ownership contract.
