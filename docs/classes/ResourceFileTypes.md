# ResourceFileTypes

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public static class Electron2D.ResourceFileTypes`. **Source:** [ResourceFileTypes.cs](../../src/Core/IO/ResourceFileTypes.cs).

**Inherits:** None (static registry or enum).

## Executable mesh skin integration

ArrayMesh exact file factory and Material resource-array registration now support bounded geometry/skin bytes with separate material graphs and fresh-process executable mesh consumers.

## Physical skeletal integration

Registered PhysicalBone and SkeletonModificationPhysicalBones factories support exact fresh-process scenes with copied consumer path arrays and authored physics/joint properties.

## Skeletal registrations

Built-in exact factories/descriptors include Skeleton, Bone, SkeletonModificationStack and SkeletonModificationLookAt. A typed nullable SkeletonModification array codec preserves modification slots/aliases. Rest, constraints and copied Polygon bone records persist, while transient bindings/palette/overrides do not. Custom derived modification resources still require explicit registered factories and copy/schema contracts.

## Description

Exact factories now include SkeletonModificationJiggle and SkeletonModificationStackHolder. Existing nullable modification/resource codecs encode nested child references and shared aliases; Jiggle stores copied bounded joint configuration without simulation history. Holder custom copies force child graph duplication. Fresh-process SkeletonJiggleTests verifies both exact types and actual nested execution.

Exact built-in factories now include SkeletonModificationTwoBoneIK, SkeletonModificationCCDIK and SkeletonModificationFABRIK. Existing modification arrays preserve them polymorphically; versioned bounded joint blobs store paths/indices, constraints, magnets and final-orientation settings with no weak caches/scratch. SkeletonIKTests loads and executes all registered types in a fresh process.

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

Built-in direct resource factories now include AudioBusLayout and all 27 concrete AudioEffect resources. Their typed stored controls and indexed schemas persist independently of processing state; custom effects still require explicit compiled registration.

The built-in direct node factory list now also includes TabBar, recreating its typed count/indexed scene state and independent internal timer. TabBarTests exercises e2dscene save plus fresh-process load/run.

The embedded popup slice adds Popup/PopupPanel theme/type and exact file-factory integration; focused LineEdit text/IME now resolves the containing native root while retaining popup-local control focus. See [the component](../components/popup-windows.md) for the applicable portion and limits.

The [PopupMenu consumer](../components/popup-menus.md) uses all 37 declared menu theme keys, inherited Window popup hooks and an exact built-in scene/file factory. Internal item/search controls receive focus after visibility propagation; public runtime signatures of these owners are unchanged.

[Tab panels](../components/tab-panels.md) add TabContainer as an executable consumer with all 31 declared theme keys, Container fitting/maximum propagation and exact scene/file factories. Indexed restore uses a private typed schema count before owned child construction. Public signatures of these shared owners are unchanged.

[Dropdown choices](../components/dropdown-choices.md) add OptionButton as an executable Button/PopupMenu consumer with three arrow theme keys and an exact scene/file factory. Selected item translation uses the shared Button text path. Disposed borrowed button icons read as null and clear on owner processing, avoiding the internal-process/deferred-cleanup race. Public shared-owner signatures remain unchanged.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.

## CPU particle graph schemas

Built-in factories now include CPUParticles, scalar Curve, Gradient and CanvasItemMaterial. Their stored schemas reconstruct particle configuration, scalar point snapshots, ramp arrays and sheet/blend settings in a fresh process. Native particle state and delegates are omitted; the generic initial-velocity curve and ordinary curve aliases remain typed resource references. Other unregistered concrete resource types keep their explicit application registration prerequisites.

NavigationAgent is a registered typed Node factory. Twenty-five source descriptors persist through PackedScene fresh-process reconstruction; agent RID, runtime map override, current query result and progression state are not archive fields.

NavigationObstacle persists five source properties through its registered scene factory; Radius/Vertices/Velocity feed actual shared avoidance after reconstruction.

The built-in shape registrations include ConvexPolygonShape, ConcavePolygonShape,
SeparationRayShape and WorldBoundaryShape beside the four primitive types. Their
stored geometry and inherited CustomSolverBias use ordinary descriptors and exact
concrete factories; PhysicsContactPolicyTests exercises all eight .e2dres round trips.
