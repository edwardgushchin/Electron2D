# PackedScene

Last updated: 2026-09-21

## Declaration

- Source: [`PackedScene.cs`](../../src/Scene/Resources/PackedScene.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class PackedScene : Resource`
- Domain: [Scene](../domains/scene.md)
- Component: [Packed scenes](../components/packed-scenes.md)

## Responsibility and ownership

`PackedScene` stores an in-memory, typed snapshot of one node hierarchy and constructs independent detached runtime instances from it. It is a managed [`Resource`](Resource.md) in `Electron2D.dll`; it is not a text/binary scene file, loader, saver, import artifact, or editor document.

The snapshot owns no source [`Node`](Node.md). It retains source-independent static node factories, immutable node metadata, typed stored-property values, and references to resources used by those values. Shared source resources remain caller-owned. During instantiation, the returned root owns all created child nodes and every duplicated scene resource; disposing that root disposes the complete hierarchy and those duplicates.

## Public API

| Member | Current behavior |
| --- | --- |
| `PackedScene()` | Creates an empty resource and its initial live empty [`SceneState`](SceneState.md) |
| `bool CanInstantiate()` | Reports whether the current state contains at least one node |
| `SceneState GetState()` | Returns the current live read-only metadata object; repeated calls reuse it until that state is disposed |
| `Node Instantiate(PackedSceneEditState editState = Disabled)` | Reconstructs and returns a detached hierarchy; only runtime `Disabled` mode is accepted |
| `void Pack(Node root)` | Replaces current contents with a typed snapshot of `root` and the descendant branches owned by that root |

Inherited resource API remains available. Shallow `Duplicate()` and `CopyFromResource()` share immutable packed data. Deep duplication remaps stored resource references through the existing graph-preserving resource duplication session. `ResetState()` empties the packed contents. A duplicate never inherits the resource path or scene ID.

## Protected extension API

| Member | Current behavior |
| --- | --- |
| `Resource CreateDuplicateInstance()` | Creates an empty `PackedScene` target for the inherited typed duplication workflow |
| `void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Shares immutable packed data for a shallow copy and remaps stored resource references for a deep copy; updates the target state and emits its `Changed` event |
| `void OnResetState()` | Replaces packed data with the empty state and emits `Changed` |
| `void OnResourcePathChanged(string path)` | Publishes the currently committed `ResourcePath` to the live `SceneState`; replaces a concurrently disposed state when necessary |

These are sealed-class overrides used by inherited `Resource` operations, not further subclassing points.

## Capture selection and stored state

The root is always included and does not own itself. Capture then walks children depth-first in sibling order. A descendant branch is included only when its first node has `Owner` equal to the packed root; a rejected branch and all of its descendants are pruned, even if a deeper node names the root as owner.

For each included node, capture stores:

- a source-independent static factory for the exact runtime type and the source instance identity used to reject source reuse;
- the node name, relative path, parent/owner indices, and persistent groups;
- every writable [`PropertyDescriptor`](PropertyDescriptor.md) whose `IsStored` flag is `true`, except `Name`, which has its dedicated field;
- runtime-only empty metadata for nested-scene instances and placeholders, which are not authored by the current implementation.

Supported stored values are strings, `Resource` subtypes, and value types that contain no managed references. This includes [`Color`](Color.md), [`Rect2`](Rect2.md), and [`Transform2D`](Transform2D.md), whose ordinary/HDR, negative, or affine components are copied exactly. Reference-shaped values such as arbitrary objects, collections, delegates, and node references are rejected with `NotSupportedException`; no reflection-driven discovery or invocation, dynamic value container, or string-addressed property call is used.

`Pack(null)` fails before capture and preserves the previous state. Once a non-null capture begins, the published state is cleared first. Any later factory/property/schema/capture failure leaves the packed scene empty. Both the empty transition and a successful replacement are visible to a live `SceneState`. `Changed` is emitted after the attempt; a throwing handler does not roll back the already committed result.

## Instantiation flow

1. The current immutable packed data and path classification are captured under the packed-scene lock.
2. Exact-type nodes are created parent-first. Factory execution is guarded so neither a new `SceneTree` nor entry into an existing tree can begin in that execution context before the node is returned. A factory result must be new for this packed state, live, detached, parentless, childless, unowned, unqueued, and free of a scene path. Returning the source, a previously issued node, a wrong type, or state retained through a capturing delegate is rejected.
3. Stored properties are resolved against the new node's current typed descriptor schema and restored before parenting. Persistent groups are then applied.
4. Nodes are parented in captured order, then `Owner` references are assigned.
5. Scene-local resource graphs are duplicated with alias/cycle preservation. Their owning root is assigned before setup callbacks, and each local duplicate is set up once.
6. The root adopts every created resource. An external packed-scene path is copied only to the root's `SceneFilePath`; built-in paths remain empty.
7. The complete hierarchy is revalidated, the root alone receives `Node.NotificationSceneInstantiated`, and topology is revalidated again before return.

The returned root has no parent and belongs to no `SceneTree`. Enter/ready lifecycle starts only when the caller later installs or attaches it. Source event subscribers and [`EventConnection`](EventConnection.md) tokens are not copied; persistent typed event endpoints have no schema yet.

If any factory, setter, parenting callback, resource duplication/setup callback, notification, validation, or cleanup step fails, instantiation attempts to dispose every node result and resource duplicate whose ownership it acquired. Every new node remains under an instantiation barrier until final validation: it cannot be disposed or enter any `SceneTree` while unfinished. Topology validation detects temporary attachment to an unrelated detached hierarchy, and rollback removes and disposes the escaped node. Cleanup attempts every owned item and aggregates rollback failures with the original exception. A factory remains responsible for side effects or allocations it performs before throwing instead of returning a node.

## SceneState lifecycle

`GetState()` exposes one live metadata object. Successful repacks, reset/copy transitions, and path changes update that object. Disposing a returned state does not dispose the packed scene; the next access or update creates a replacement. Disposing the packed scene does not dispose an externally held state, which retains its final immutable data and last path.

The state is a view, not an ownership transfer. A resource value returned through `SceneState.GetNodePropertyValue<TValue>()` is the stored resource reference and remains governed by normal `Resource` ownership.

## Threading and reentrancy

Packed data access, capture replacement, state replacement, and path propagation are serialized by an internal lock. Concurrent instantiations use the immutable data snapshot observed when each call begins. Factory issuance and `SceneState` reads are safe for concurrent calls. A concurrent state disposal is recovered by replacing the cached state.

Callbacks execute synchronously on the caller's thread. Packing an attached hierarchy therefore requires its `SceneTree` owner thread; detached custom node/resource state has no automatic synchronization. Capture marks the complete source hierarchy so node setters, structural mutation, disposal, deletion requests, and inherited mutable object state are rejected until capture ends. Derived stored-property setters must honor `Node.EnsureMutable()`.

No hard real-time or allocation-free guarantee is made for packing or instantiation. Factories, descriptors, resource duplication, validation, and callbacks allocate managed objects and may run arbitrary user code.

## Errors and invariants

- Empty instantiation and incompatible stored schemas throw `InvalidOperationException`.
- Undefined edit-state values throw `ArgumentOutOfRangeException`; defined editor modes throw `NotSupportedException`.
- Null roots throw `ArgumentNullException` without changing data.
- Unsupported stored value shapes and derived nodes without an explicit reusable factory throw `NotSupportedException`.
- Disposed nodes/resources and disposal races throw `ObjectDisposedException`.
- Factory, descriptor, resource, notification, event, and cleanup exceptions propagate; multiple primary/cleanup failures are aggregated.
- Packed data never stores a live source node, instance-bound factory, arbitrary object graph, event delegate, or `SceneTree`.

## Dependencies and interactions

`PackedScene` depends on `Resource`, `Node`, `PropertyDescriptor`, `SceneState`, `PackedSceneEditState`, standard collections, weak identity tracking, and deterministic disposal. It uses `Resource`'s internal graph duplication machinery for local resources. It does not depend on SDL3-CS, rendering, input, audio, physics, a filesystem scene format, an asset loader/saver, scripting, networking, or an editor.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` covers empty state, runtime and invalid edit modes, owned-branch pruning, paths/owners/groups, typed state queries including HDR `Color`, `Rect2`, and `Transform2D` restoration, source disposal independence, scene-local aliasing and setup, repeated independent instances, packed-scene duplication, live-state replacement and disposal races, concurrent path updates, failed capture clearing, capture mutation rejection, capturing/source-returning/wrong-type/reused factories, detached-parent and active-tree escape rollback, setup failure cleanup, and state survival after packed-scene disposal.

The current contract is runtime-only and in-memory. It has no `.tscn`/binary loader or saver, resource UID/import remapping, inherited-scene authoring, nested packed-scene overrides, editable instances, placeholders, missing-resource recovery, script state, persistent event endpoint schema, or editor mode implementation. `SceneState` exposes honest empty/null answers for those absent metadata categories.

## Decision

- [0023: Typed in-memory packed scenes](../decisions/scene.md#adr-0023)
- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0029: Typed Transform2D value and affine semantics](../decisions/core-math.md#adr-0029)
