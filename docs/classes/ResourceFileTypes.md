# ResourceFileTypes

Last updated: 2026-10-05

**Namespace:** `Electron2D`. **Declaration:** `public static class Electron2D.ResourceFileTypes`. **Source:** [ResourceFileTypes.cs](../../src/Core/IO/ResourceFileTypes.cs).

**Inherits:** None (static registry or enum).

## Description

Registers stable compiled factories and typed portable value codecs for resource/scene files.

Registration is allocating setup. IDs are ordinal and immutable; file data never loads assemblies, invokes reflected members or constructs arbitrary CLR types. Register the same schemas before saving and in every loading process. Factories are direct static delegates and must return fresh exact-type instances.

See [typed resource files](../components/resource-files.md) for the complete storage, type/factory, graph, UID, cache, failure, ownership and thread contract.

## Example

This public excerpt requires its existing file/directory or the named application extension types; ResourceArchiveTests supplies executable complete producers and consumers.

```csharp
ResourceFileTypes.RegisterResource<GameData>("game.Data.v1", GameData.Create);
ResourceFileTypes.RegisterNode<GameActor>("game.Actor.v1", GameActor.Create);
// Application types declare static factories and typed stored descriptors.
```

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public static System.Void RegisterNode<TNode>(System.String id, Func<TNode> factory)` | Registers a compiled node factory used by file-backed PackedScene. |
| `public static System.Void RegisterResource<TResource>(System.String id, Func<TResource> factory)` | Registers a compiled resource factory with storage-enabled typed properties. |
| `public static System.Void RegisterValueCodec<TValue>(Action<Electron2D.StreamPeerBuffer, TValue> write, Func<Electron2D.StreamPeerBuffer, TValue> read)` | Registers explicit portable encoding for a concrete stored value type. |

## Method Descriptions

<a id="member-ef7438ceff71"></a>
### RegisterNode

`public static System.Void RegisterNode<TNode>(System.String id, Func<TNode> factory)`

Registers a compiled node factory used by file-backed PackedScene.

- `id`: Stable portable schema ID.
- `factory`: Static exact-type constructor.

- `TNode`: Exact concrete node type.

<a id="member-46d5cd5f5482"></a>
### RegisterResource

`public static System.Void RegisterResource<TResource>(System.String id, Func<TResource> factory)`

Registers a compiled resource factory with storage-enabled typed properties.

- `id`: Stable portable schema ID.
- `factory`: Static exact-type constructor.

- `TResource`: Exact concrete resource type.

<a id="member-a3793b035cff"></a>
### RegisterValueCodec

`public static System.Void RegisterValueCodec<TValue>(Action<Electron2D.StreamPeerBuffer, TValue> write, Func<Electron2D.StreamPeerBuffer, TValue> read)`

Registers explicit portable encoding for a concrete stored value type.

Built-in codecs cannot be replaced. Application codecs are part of the agreed file schema.

- `write`: Direct value writer; observes the stream's endian setting.
- `read`: Direct value reader with validation.

- `TValue`: Concrete scalar/math/array value type; no delegate, native pointer, node or resource.

## Lifecycle, failures and verification

Permanent service objects cannot be disposed or unregistered; format extensions are caller-owned and borrowed only while registered. Factories must return fresh exact live identities; node factories are detached, without caller-owned tree membership. Unknown schemas, incompatible property/resource types, invalid flags/UIDs, missing files and corrupt payloads reject explicitly. Metadata, registration, snapshots, save/load and scene instantiation allocate outside the frame interval. File cache publication follows complete decoding; arbitrary custom copy or observer failure follows Resource commitment rules. Read the component for concrete bounds and remaining payload integrations.

ResourceArchiveTests exercises registered public formats, graph/scene persistence, separate-process lifecycle, UID/dependency rewrite, cache replacement, font/theme/pixel/geometry consumers and rollback/lifetime edges on Linux x64. It does not establish editor, rendered archive scenes, foreign-host/AOT, all-resource payloads, unmeasured native allocations or human acceptance.
