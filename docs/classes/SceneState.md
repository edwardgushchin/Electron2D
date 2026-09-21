# SceneState

Last updated: 2026-09-21

## Declaration

- Source: [`SceneState.cs`](../../src/Scene/Resources/SceneState.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class SceneState : ElectronObject`
- Domain: [Scene](../domains/scene.md)
- Component: [Packed scenes](../components/packed-scenes.md)

## Responsibility and ownership

`SceneState` is the read-only typed metadata view returned by [`PackedScene.GetState()`](PackedScene.md). It exposes node, property, group, path, owner, placeholder, nested-instance, and connection metadata without a dynamic value container or string-based mutation.

Callers cannot construct a state directly. The object owns no nodes or resources and cannot instantiate or mutate a scene. It retains the immutable packed-data reference currently published to it; resource-valued properties are borrowed references governed by normal [`Resource`](Resource.md) ownership.

## Public API

| Member | Current behavior |
| --- | --- |
| `SceneState? GetBaseSceneState()` | Always `null`; scene inheritance is absent |
| `int GetConnectionCount()` | Always `0`; persistent event endpoints are absent |
| `int GetNodeCount()` | Returns the captured node count, or `0` for an empty state |
| `IReadOnlyList<string> GetNodeGroups(int nodeIndex)` | Returns the immutable ordinal-sorted persistent-group snapshot |
| `int GetNodeIndex(int nodeIndex)` | Returns `-1`; instanced-subscene sibling override metadata is absent |
| `PackedScene? GetNodeInstance(int nodeIndex)` | Returns `null`; nested packed-scene metadata is absent |
| `string GetNodeInstancePlaceholder(int nodeIndex)` | Returns an empty string; placeholders are absent |
| `string GetNodeName(int nodeIndex)` | Returns the captured node name |
| `string GetNodeOwnerPath(int nodeIndex)` | Returns `.` for root-owned nodes and empty for no stored owner |
| `string GetNodePath(int nodeIndex, bool forParent = false)` | Returns the relative node path, or the stored parent path when requested; root is `.` |
| `int GetNodePropertyCount(int nodeIndex)` | Returns the stored-property count |
| `string GetNodePropertyName(int nodeIndex, int propertyIndex)` | Returns the typed descriptor name captured for that property |
| `Type GetNodePropertyType(int nodeIndex, int propertyIndex)` | Returns the exact declared value type |
| `TValue GetNodePropertyValue<TValue>(int nodeIndex, int propertyIndex)` | Returns the captured value when compatible with the requested type; no untyped getter exists |
| `string GetNodeType(int nodeIndex)` | Returns the unqualified captured runtime type name |
| `string GetPath()` | Returns the current source `PackedScene.ResourcePath`, or the last value observed before source disposal |
| `bool IsNodeInstancePlaceholder(int nodeIndex)` | Always `false` for runtime-authored nodes |

All access after this state is disposed throws `ObjectDisposedException`. Node and property index errors throw `ArgumentOutOfRangeException`; an incompatible generic property request throws `InvalidCastException`.

## Lifecycle and state transitions

A newly constructed `PackedScene` owns one empty live state. The same state object observes packed-data replacement, reset/copy, and resource-path changes. Disposing it makes that object terminal but does not affect the packed scene; the source creates a replacement on its next state access or update.

Disposing the source packed scene does not dispose externally held states. Such a state stops receiving updates and remains a readable final snapshot until the caller disposes it. Disposing a state never disposes stored resource references.

## Invariants and threading

- The public surface is read-only; no method changes packed data or node instances.
- Published packed data is immutable to callers. Group collections are read-only snapshots.
- Every read and source-driven replacement is serialized by the state lock and rechecks disposal.
- Concurrent reads are supported. Concurrent disposal causes reads to fail and source updates to replace the cached state safely.
- Resource values remain mutable resources; reading metadata does not make their derived state thread-safe.

## Dependencies and interactions

`SceneState` depends on `ElectronObject`, the internal immutable packed-scene records, `Type`, and standard read-only collections. It refers to `PackedScene` and stored `Resource` values but owns neither. It has no `SceneTree`, SDL, renderer, loader/saver, editor, or native dependency.

## Verification and limitations

The executable harness verifies empty/live state identity, path propagation, node ordering and paths, owner/group metadata, absent connection/placeholder/inheritance metadata, typed property name/type/value access, invalid indices/casts, successful repack observation, concurrent cached-state disposal and path updates, and survival after source disposal.

The type intentionally omits connection-detail accessors, editable-instance metadata, inherited base state, placeholder loading, node-reference remapping metadata, and file-format internals because those producers do not exist. `GetNodeType()` is a diagnostic unqualified name, not a reflection-based factory key.

## Decision

- [0023: Typed in-memory packed scenes](../decisions/scene.md#adr-0023)
