# Typed resource and scene files

Last updated: 2026-10-26

## Live-scene authoring save

ResourceSaver.SaveScene is the shared live-scene save operation under ADRs 0013/0090. A nonempty explicit path or current root SceneFilePath selects the destination. Known SaverFlags, lifetime, scene-owner and capture access are validated before notification delivery. The retained saver prevents recursive/concurrent saving of the same root without holding its registry lock across callbacks.

Parent-first NotificationEditorPreSave (9001) reaches the whole live subtree, including internal/unowned nodes. Callback-authored values then enter existing isolated RESET-aware PackForSave and ordinary registered file Save. Owner filtering still selects saved nodes. After a successful write, SceneFilePath commits before cleanup; observer failure cannot undo the saved bytes. Temporary packed data is retired before parent-first NotificationEditorPostSave (9002), which is attempted whenever pre-save delivery began, including failed preparation/write. Original operation/retirement/post-save errors are retained in order. Callback effects remain application-owned rather than being implicitly rolled back. Ordinary Pack/PackForSave/Save and fresh instantiation emit neither notification.

```csharp
ResourceSaver.SaveScene(sceneRoot, "user://level.e2dscene", SaverFlags.Compress);
```

The public SaveScene consumer exercises real file saving, exact notification order, temporary live physical geometry versus persisted pre-save state, parent/internal delivery, input/owner/preflight failures, same-root reentry and actual concurrent detached saving, failed capture/write, paired original errors, cleanup after commitment and default-path recovery. A separate process registers the same exact compiled factories, loads the file, checks owner filtering and performs a real selected-backend geometry query and physical step. Cold authoring is separate from complete moving-frame managed-allocation measurement. All eighteen final public/root managed and strict NativeAOT profiles pass across CPU/software Vulkan/registered worlds and owner/separate policies, including eighteen fresh reader processes. Each profile measures 128 moving complete physical frames after 128 warm frames at strict 0/0 owner/all-thread managed bytes; total measured frames are 2304, with displacement (4,1) allowed .003 units for float integration. Notification IDs/order, file state, original exceptions and resource identity assertions are exact. Six PDB/Compile cohorts bind the published payload to current sources. Editor UI, unified project tooling, rendered authoring, native allocation and foreign/hardware/performance acceptance remain separate. Ignored receipts are retained in bin/physics-scene-save/2026-10-20/.

## Executable mesh skin integration

Exact ArrayMesh/ImmediateMesh registration uses bounded versioned geometry/skin/primary-and-secondary-UV bytes and typed Material arrays. Fresh .e2dscene processes preserve surface names, flags, skin weights and material aliases and execute the palette consumer. Malformed geometry fails before replacement.

## Physical skeletal integration

Exact PhysicalBone and SkeletonModificationPhysicalBones factories retain inherited node/body/joint properties and copied string[] path slots. Fresh-process scenes run actual joint-driven bone pose transfer. Pending command names and weak scene bindings are transient.

## Jiggle controller integration

Jiggle joint configuration and borrowed StackHolder child references now have exact registered schemas. Existing resource arrays and forced copy sessions preserve child aliases while excluding weak leases/live history. Fresh-process files execute nested controllers, with bounded blob validation before replacement.

## Executable IK integration

Registered TwoBoneIK/CCDIK/FABRIK modification resources persist exact selections and scalar/vector policies through bounded joint state, existing nullable resource arrays and independent PackedScene stack graphs. Fresh-process checks exercise actual solver behavior after restoration.

## Independent skeletal scene resources

Built-in Skeleton/Bone and stack/LookAt schemas restore authored rigs. A hidden versioned Polygon bone-record blob validates all paths/counts/finite weights before replacement. Scene stack descriptors force independent graph copies using the existing resource session, retain aliases and transfer encoded/copy-created resources to ordinary scene-owned lifetime. Fresh-process SkeletonTests loads and actually aims the bone.

## Scope and owned types

ResourceSaver and ResourceUID are permanent retained services with static operations. ResourceLoader now retains its ordered file-extension state on the same service model. ResourceFormatSaver/ResourceFormatLoader are ordinary caller-owned extensions; registration borrows them. ResourceFileTypes registers compiled factories and value codecs on retained ResourceLoader registry state. SaverFlags controls dependency paths, embedding, temporary paths, editor omission, byte order, compression and committed subresource paths.

The runtime producer writes `.e2dres` and `.e2dscene`. It supports registered typed schemas rather than arbitrary object serialization. Built-in resource factories cover Resource, PackedScene, PhysicsMaterial, Image, ImageTexture, AtlasTexture, FontFile, StyleBoxFlat/Line/Texture/Empty and Circle/Capsule/Segment/RectangleShape. The explicit node factory list is [ResourceFileTypes.Nodes.cs](../../src/Core/IO/ResourceFileTypes.Nodes.cs); application types register a static exact-type factory in every saving/loading process. Every persisted field uses a writable IsStored descriptor or dedicated metadata/payload schema. Application authors must opt in their state; registration alone does not discover fields.

## Runtime flow

1. Save snapshots the ordered format list, validates resource/path/flags and rejects saving the same resource recursively. Recognition chooses eligible extensions; failed candidates fall through and final failures aggregate. ChangePath exposes a temporary localized destination and restores both its old path and cache-registration role.
2. The archive encoder visits graph identities, writes external UID/type/fallback paths or embeds resources, and encodes typed properties plus PackedScene node tables. It preserves aliases and resource cycles. Reserved `__editor` properties can be omitted. Subresource IDs must be unique within the archive.
3. Encoding/checks complete before a temporary sibling file is flushed and atomically replaces the destination. The saved UID is registered and ReplaceSubresourcePaths applies only after replacement. Unsupported schema/codec failures preserve the old destination.
4. Loading validates version, lengths, flags, UID, bounded decompression and SHA-256 before allocating resource factories. All internal identities are allocated before properties; declared schema types and reference indices are checked. Scene tables validate parent/owner topology, names and duplicate sibling names.
5. Cache publication occurs after decode. Reuse returns the existing compatible wrapper. Ignore leaves its path unregistered; Replace refreshes the same exact type through CopyFromResource. Ordinary modes reuse external dependencies; Deep modes propagate. External cycles between files reject by canonical absolute path with a 64-file depth bound and require bundling.
6. Internal resources and newly loaded external dependencies belong to the decoded root. PackedScene instances retain the graph through private disposable leases. Copies of file roots retain their source graph for borrowed stored state; failed arbitrary custom copying retains both graphs until disposal or a subsequent complete replacement. Instantiation retains the source graph before invoking application factories. Template Replace retires its old graph; old instances keep it until their final root disposal. Node.ReplaceBy transfers these leases alongside local resource ownership. Reused dependencies remain borrowed.

## Version 1 encoding

The fixed 64-byte header uses little-endian fields: magic `E2DASSET` (0..7), version 1 (8..11), save flags (12..15), nonnegative Int64 UID (16..23), raw length (24..27), encoded length (28..31), SHA-256 of raw payload (32..63). The body is plain or bounded Deflate; SaveBigEndian changes numeric payload order, including property substreams. The total raw/encoded budget is 64 MiB. Resource and node tables are limited to 65,536 entries; value arrays have a 1,048,576 element bound and resource arrays/dictionaries use 65,536.

The payload starts with an Int32 resource count. Each entry stores stable type ID, resource name, local-scene byte, scene ID, external fallback path, Int64 external UID and length-prefixed payload. Strings use an Int32 UTF16 code-unit count followed by UInt16 code units in payload byte order. This preserves embedded NUL, BOM and unpaired CLR surrogates; the networking StreamPeer text decoder has different NUL/BOM behavior and is not the file decoder. An empty fallback denotes an internal definition; the root must be internal. External records carry no internal payload. Internal property records store name, stable value-type ID and a length-prefixed typed value. PackedScene records include registered node ID, name, parent/owner/sibling indices, persistent groups and typed property records. Image and ImageTexture use validated raw pixel schemas; font bytes/configuration use typed stored descriptors. Unsupported versions reject; there is no automatic migration or assembly loading.

Encoding is ordered and a saved UID is retained on subsequent writes. Binary fields can be inspected through typed metadata queries, Resource properties and SceneState. Human-oriented text authoring/diff export is a future tooling capability; no JSON or foreign scene-file interoperability is claimed.

## Typed values, UID and extensions

Built-in codecs cover primitive integer/floating values, strings, six vectors, Color, rectangles, Transform, selected nullable scalar/math values, byte/string/int/float/Vector2/Color arrays, int contours, resource arrays and string/int dictionaries. Enums preserve their numeric typed values. Node references are relative paths restored after topology construction. Custom codecs are explicit schema contracts and must validate input; reference-shaped custom values are snapshotted through their codec. Native pointers, RID, delegates and arbitrary ElectronObject values reject.

ResourceUID uses canonical base-34 `uid://` text and a process-local catalog with explicit Add/Set/Remove. Saved file headers retain identities between processes; loading or GetResourceIDForPath imports them. CreateIDForPath hashes project name and path with SHA-256. No background import scan, catalog disk database or arbitrary relocation discovery exists. A registered UID resolves before FileAccess/DirAccess/ProjectSettings directory path policy; unknown UIDs throw. Dependency tokens retain UID, optional stable type and fallback path; RenameDependencies rewrites metadata atomically without resource construction.

Formats are invoked synchronously from copied registration lists. Duplicate identities do not reorder entries; atFront gives new registrations priority. Disposed extensions are skipped; removal does not dispose them. Metadata queries honor the same priority. Custom loaders return live compatible resources; exact-type Replace copies into the existing wrapper. Custom copy/setter and Changed failures can occur after mutation under the existing Resource contract. Custom savers own their format's atomicity and validation; only the built-in archive implements the version-1 transaction above.

## Verification and remaining dependencies

[ResourceArchiveTests](../../tests/Electron2D.Tests/ResourceArchiveTests.cs) exercises endian/compression flags, graph aliases/cycles, resource/node schema, UID/dependency fallback, cache identity, deep ownership, file scene change/reload/spawner registration, pixel and font/layout payloads, UI theme reconstruction, scalar shape geometry, old-instance graph retention, custom-format priority/fallback/Replace, malformed headers/checksum, wrong types and atomic dependency rewrite. A separate process registers the compiled schemas, reads the saved file, instantiates it and runs ordinary SceneTree lifecycle. These are public runtime authoring and headless checks, not rendered/editor acceptance.

Further resource payloads require concrete stored schemas and factories, particularly audio imports, curves, materials, mesh/animation data and full Theme resources; existing leaf audio/security loading does not make their archive embedding implemented. Threaded load tokens/jobs, exported resource packs, import remapping, persistent event endpoints, inherited scene authoring, project CLI/editor and text diff tools retain separate triggers. All file/capture/registration operations allocate outside the frame path. Linux x64 and native font layout execute locally; foreign hosts, AOT, rendered archive scenes, unmeasured native allocations and human acceptance remain unverified.

## Decisions

- [ADR 0013](../decisions/resources.md#adr-0013): typed schemas, formats and file identities.
- [ADR 0014](../decisions/resources.md#adr-0014): private ownership leases and allocation boundary.
- [ADR 0023](../decisions/scene.md#adr-0023): stored scene model and detached lifecycle.
- [ADR 0090](../decisions/agent-native.md#adr-0090): fresh-process authoring evidence and verification limits.
- [ADR 0095](../decisions/singleton-services.md#adr-0095): static operations over retained objects.

Exported SceneState views also retain their file-backed graph snapshot through a private lease, including after template disposal. Dispose such a view when finished. Internal unexported views retain no extra ownership; later state/content transitions update exported-view retention. ResourceArchiveTests verifies the final resource snapshot and deterministic release.

Internal subresource paths are visible metadata; direct reload by a `file::subresource` path remains a separate ResourceLoader dependency requiring identity-table lookup with ownership rebasing. Load the file root and use its typed properties/SceneState in the current profile.

AudioBusLayout and all 27 concrete effect resources now have built-in direct schemas. Layout count fields prepare indexed bus/effect descriptors before dependent values load; aliases and borrowed external references keep ordinary archive semantics. Applying a loaded layout and generating later snapshots retain the existing internal file graph owner. AudioBusLayoutTests adds fresh-process default startup, cache/corruption and complete DSP schema roundtrips.

ColorPalette has a compiled built-in resource factory and stored Color[] schema. ColorPicker palette commands exercise save/load, and fresh-process ColorPicker scenes recreate the connected numeric GUI consumer; see [color authoring](color-authoring.md).

FontVariation is a registered resource factory. Stored configuration includes borrowed BaseFont graph references, copied numeric design coordinates, features, synthetic outline/baseline/spacing and palette settings. The numeric-coordinate codec writes sorted uint tags with finite floats and rejects negative/oversized counts, duplicate tags and nonfinite values on read. FontVariationTests loads and shapes a stored instance in a fresh process; serialization itself does not establish a native render or editor authoring gate.

Script archives store editable source text and a stable compiled type ID resolved against explicit host registration. Project Node/Resource registrations reuse exact file factories and typed stored property schemas. A fresh host registers its compiled classes before scene loading and executes the restored class’s ordinary callbacks; storage does not recompile or reassign CLR types.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.

## Particle resources

The connected CPU particle scene schema registers CPUParticles, Curve, Gradient and CanvasItemMaterial. Scalar curves retain exact bounded point/tangent/mode and limit records in a versioned byte-array property; gradients store arrays and interpolation policies. The ordinary reference table preserves shared curve aliases, and material blend/sheet configuration is copied independently. Fresh-process checks load and execute the particle scene with no process-local factory or live particle cache. ShaderMaterial storage remains its separate schema dependency.

All eight built-in Shape resource types have concrete registry factories and stored
geometry descriptors, including convex/concave polygons, separation rays and world
boundaries. Shape.CustomSolverBias is inherited storage. PhysicsContactPolicyTests
checks same-process save/load alongside duplicate and in-place copy; this check alone
does not establish fresh-process editor authoring acceptance.

## Tile resource graphs

TileSet/TileSetAtlasSource factories and source-owned TileData descriptors now preserve source identities, atlas regions, alternatives and physical layers. TileMapLayer stores authored cells and selected resources; body RIDs are rebuilt after tree entry. Vector2i arrays use a built-in value codec. TileMapLayerTests verifies portable resource graphs and fresh-process executable scenes; see [tile ownership and limits](tiles.md).

[Threaded resource loading](threaded-resource-loading.md) executes the WorkerThreadPool prerequisite under ADR 0013/0104. Preparation does not mutate registered cache objects; owner publication preserves cache modes and graph leases, and private remapping handles stored/PackedScene/shader references. AsyncGallery verifies separately authored scenes with real background dependencies and native rendered output.

## Source geometry producer

[Navigation baking](navigation-baking.md) now connects copied outline/source resources and registered schemas to actual clearance/carve/partition output, scene collection and owner-published worker results. NavigationRegion updates the same live map and emits completion. Separate-process BakedRoutes executes authored source geometry and rendered routes on Linux Wayland GPU/compatibility; remaining native/foreign/editor gates stay explicit.

Custom tile graphs store ordered definition names/configured type IDs, then each alternative's actual type IDs and typed values. Copies, cache replacement and standalone sources preserve actual values independently of configured types. Application codecs/resource factories prepare the same direct custom-data factories in fresh processes. See [typed custom data](tiles.md#typed-custom-data).

Tile drawing graphs store borrowed materials, Z/Y offsets, UVClipping and rendering quadrant/order policy through exact typed descriptors. Fresh-process TileDrawingTests reconstructs the authored graph; generated drawing items/commands stay transient.

[Scene collection tiles](../components/tiles.md#scene-collections) retain shared PackedScene graphs and compiled factory root validation, with deferred ordinary child placement and explicit editor/physics prerequisites.

ImmediateMesh saves completed surfaces and material graph aliases through AMS2 typed storage; AMS1 ArrayMesh data remains readable. Drafts are omitted. MeshUV2Tests verifies fresh-process factories and retained drawing for both mesh resources.

## Missing resource recovery

Version 2 introduced explicit scalar/resource/resource-array/node-reference roles and per-field byte order to the existing bounded archive. Version 4 definitions distinguish generic resources, Mesh and ordinary Texture. Meshes retain finite nonnegative Rect2 bounds; textures retain finite signed integer dimensions. Version 4 also records missing payloads independently from unavailable factory identities. Known version-1/2/3 resource graphs, including legacy texture roles, retain their original layouts. Allocate-before-apply, exact known schemas, checksums, endian/compression, atomic write and owned rollback remain enforced. Version-1 known graphs still load; version-1 unknown fields reject because their resource IDs cannot safely be identified and relocated.

ResourceLoader.SetAbortOnMissingResources defaults true. A synchronous archive graph or threaded request captures the policy once, including nested dependencies. False permits MissingResource for an unavailable generic factory, PlaceholderMesh for an unavailable mesh factory PlaceholderTexture for an unavailable ordinary texture factory and, in version 8, PlaceholderMaterial for an unavailable material factory. Missing external archive dependencies retain original factory/path/UID identity and aliases. Factory failures, corrupt files, unsupported Node references and incompatible concrete property types still fail; no assembly or CLR type is loaded from file data. Concrete material subtypes, Font/AudioStream/Shape and other exact subtype substitutions need their own typed producer. MissingNode recovery uses the version-7 connection and node-reference contract below.

MissingResource retains original factory identity and exact encoded scalar fields, including absent codecs. Resource fields are live borrowed identities, so saving a subresource as a new root, endian changes, duplication, local graphs and Replace remap references correctly. SetRecordedProperty/GetRecordedProperty use closed typed codecs/schemas and never dispatch into CLR members. RecordingProperties defaults false, permits exact-schema edits to existing fields and gates additions. Descriptor discovery exposes copied scalar bytes or typed base-resource identities; original schemas remain private wire metadata. OriginalClass is non-stored metadata and controls the saved factory identity. Explicit assignment authors a recorded-state binding and clears missing-payload provenance; automatic copying preserves it. An authored MissingResource with an empty OriginalClass still persists its recorded fields.

Missing external placeholders also retain name, scene-local policy and scene ID. Saving one unbundled retains its original dependency identity and permits recovery when the file returns. Bundling retains an explicit missing-payload marker: strict loading rejects and tolerant loading keeps the placeholder even if the original factory is known. This prevents absent data from becoming a successful empty concrete resource. External dependency validation compares original factory IDs as well as resource roles, so two identical placeholder wrapper types cannot hide an identity mismatch.

[PlaceholderTexture](../classes/PlaceholderTexture.md) additionally preserves opaque fields and graph references, logical dimensions and diagnostic pixels. Fresh processes can restore actual texture data from retained fields once the compiled factory is registered. The GPU/Compatibility texture consumers and limits are documented in [texture placeholders](texture-placeholders.md).

PlaceholderMesh has a real logical mesh RID and stored Rect2 AABB. It provides no surfaces or fabricated faces/pixels. Mesh.CreatePlaceholder copies source bounds. Missing custom Mesh factories retain their original fields privately; a later process with the compiled factory restores those fields and drawable geometry. Bounds edits and warmed drawing do not allocate new geometry.

MissingResourceTests covers strict/tolerant policy, recording/typed boundaries, scalar snapshots, null arrays, aliases/cycles, deep/shallow identity, subresource rerooting, endian/compression, Replace/corrupt rollback, absent external files, threaded policy capture and version-1 known graphs. Separate processes preserve an unknown graph/custom mesh and then restore their compiled factories. MissingResourceNativeTests checks known-green/absent-black/restored-blue pixels on GPU and compatibility renderers and measures 64 active plus 64 idle complete warmed frames. Evidence: `bin/missing-resource-validation/2026-10-12/`. Native allocation totals, foreign targets and human acceptance remain separate gates.

## Animation clip integration

Exact Animation/AnimationLibrary factories and compiled property binding registrations persist typed value/Bézier curves, resource keys, library aliases and colored markers. Double arrays now have a built-in portable codec and independent stored snapshots for key timing. See [typed clip persistence](scene-animation.md#typed-clip-persistence) for the registration, field budgets and remaining executable-schema triggers.

## Animation cue integration

The existing track schema supports compiled typed method callbacks/payloads, borrowed AudioStream cues and named child-player commands. Exact AudioStreamWAV storage preserves every authored format/metadata field and tags; file graphs retain stream and resource-argument aliases. See [cue persistence](scene-animation.md#cue-persistence) for IDs, budgets and native execution limits.

## Animation controllers

[Controller scene files](scene-animation.md#controller-scene-files) restore exact AnimationMixer/AnimationPlayer authored libraries, policies, playback options and transitions through dynamic stored descriptors. Whole-scene fresh-process loading reaches ordinary Ready autoplay and both renderer consumers. Runtime transport and selection are omitted; graph definition storage and reset-on-save transactions remain separate prerequisites.

Animation graph resources and AnimationTree scene controllers now persist authored topology, compiled writable parameter IDs and typed values through the existing archive. See [graph file persistence](scene-animation.md#graph-file-persistence). Resource validation runs after all embedded payloads and before publication.

RESET save preparation now executes through AnimationMixer.ResetOnSave and PackedScene.PackForSave. Enabled RESET value/Bézier tracks run at zero on detached nodes and isolated copied resources, capturing coupled effects while preserving source preview values/clocks/queues. Ordinary Pack captures the actual pose. Preparation failures preserve prior packed data; temporary resources are cleaned or transferred to the prepared scene. Compiled factories/setters/postprocessing execute, while lifecycle, local-resource setup and method/audio/nested event tracks are suppressed. Custom authoring state must be stored and resource duplication supported. AnimationResetSaveTests exercises policies, all controller families, coupled values/resources, errors, lifetime and portable saved scenes.

## Foldable panel scenes

Exact FoldableContainer and FoldableGroup registrations preserve typed configuration and per-instance local group aliases. Pack includes explicitly root-owned authored title controls and records their ordering as bounded typed relative Control references. Restoration validates canonical slots, resolves direct children after parenting and restores front-internal membership before hierarchy validation. Runtime implementation helpers and group member leases are excluded. Fresh-process scene tests execute the restored group and removable title controls.

Internal resource graph IDs are staged before persistence: first authored IDs remain stable, missing and duplicate IDs are generated without colliding with reserved authored values. IDs commit only after atomic writing, while source path replacement follows its explicit flag. Post-write path-hook failures attempt later paths and retain the committed IDs/file. Read validation rejects duplicate resolved embedded paths before factories or cache replacement, including legacy fallback indices. Public-only physical scene identity checks exercise actual saved aliases and backend simulation; exact executable evidence and platform limits are recorded with the current validation cohort.

The current resource-identity cohort passes eight managed profiles and six strict NativeAOT profiles, including twelve real fresh-process physical scene consumers with changed local creation order. Repeated saves retain corrected IDs, names and physical aliases; failed writes preserve source metadata, and malformed resolved paths reject before factories/cache replacement. Twelve brackets warm 128 and measure 128 active full frames with queries and ID reads at 0/0 owner/all-thread managed bytes. GPU runs use explicit lavapipe; hardware performance and native allocations remain separate gates. Exact manifests/results are retained in bin/physics-resource-identity/2026-10-14/validation.json.


Mounted res:// resources now share [resource pack resolution](resource-packs.md). PCK/ZIP overlays, synthetic directories, read-only packed metadata, relative scene dependencies and bitmap-font pages use the mounted bytes. Ordinary absolute paths and user:// retain physical access. Cached resources keep their normal cache-mode policy.

## Version 5 linked scene records

Version 5 introduced linked scene headers; the current writer uses version 8 as described below. Resource definitions retain version-4 role/provenance layout. Each scene node adds its PackedScene resource reference, source-root record index and original relative path after the sibling index; -1 denotes an absent reference/index. Ordinary versions 1–4 remain readable. The shared metadata reader validates the same header for class discovery and decoding; linked graphs validate before publication and construction. See [linked runtime scenes](linked-scenes.md#file-layout).

## Version 6 deferred scene records

[Deferred scenes](deferred-scenes.md) adds a late path after the version-5 linked header and a second typed field table after ordinary node properties. Deferred roots and skipped inherited records use this self-describing table without running a node prototype. It retains ordered scalar payloads with explicit encoding endianness, resource edges, resource arrays and node paths. Ordinary nodes write an empty second table. Readers retain versions 1–5, including UID and class discovery. Deferred paths are lookup strings and are not eager dependencies or bundled templates.

Internal leases retain pending file graphs across parent disposal and retain automatically loaded scene wrappers across instances, captures and exported SceneState views. Existing caller-owned cache entries stay borrowed; the weak cache does not own the template. File scene changes and reload use the same bounded lifetime transition.

[Factory result ownership](scripting.md#factory-result-ownership) now rejects a pre-existing registered Node/Resource result before issued tracking or decoding. Borrowed targets remain live; fresh default construction and failure cleanup keep their existing contracts.

## Version 7 missing-node and connection recovery

Version 7 keeps the version-6 node layout and resource definitions. Each authored connection stores its compiled schema ID, diagnostic event/handler labels, delivery flags, omitted-argument count, endpoint indices and exactly one self-describing `Bindings` field. That field uses the existing scalar/resource/resource-array role, byte-order and reference remapping rules. Legacy versions 1–6 retain their layouts. Unknown legacy connection adapters and role-free unknown version-1 fields fail explicitly rather than guessing a decoder.

Tolerant loading can construct [MissingNode](../classes/MissingNode.md) for unavailable compiled node factories. Its original portable identity and fields survive a resave; a later process with the factory and adapter registrations reconstructs real typed behavior and connections. Known compatible Node state still applies. Unavailable fields and nonexecuting connection records remain bounded authoring data. Failed known factories, corrupt records and incompatible schemas remain failures. Existing fields can change schema; adding names requires RecordingProperties. Explicit compiled connection recording gates new labels through RecordingSignals. These operations never dispatch arbitrary members by string.

Missing linked archives additionally produce OriginalScene and configuration warnings. Unavailable inherited records are skipped, ordinary outer-owned additions survive, and a resave flattens the absent instance to an ordinary Node, discarding unavailable overrides and incompatible connections. IgnoreDeep/ReplaceDeep honor the missing-file policy even while an older cached resource exists. This does not turn absent leaf codecs or third-party formats into a universal recovery backend.

## Localized resource files

Localized archives retain logical root/subresource paths and original source UID while decoding a selected physical file. Relative external references resolve beside that physical archive. Dependency discovery reports resolved fallback paths; rewrite accepts full paths or stored relative tokens and preserves relative encoding and registered UID priority. Archive and localized-resource checks exercise replacement and metadata rewrite through the real loader. See [localized resources](localized-resources.md).

Numeric shader matrix values use the BCL System.Numerics.Matrix4x4 carrier. Its portable codec writes sixteen explicit row-order float words using the selected archive byte order; nullable and value-array forms use the ordinary markers/counts. Matrix graph defaults/constants validate finiteness at their setter boundary. The general codec retains the same float-bit policy as other primitive codecs. MatrixValueCodecTests verifies exact words, both byte orders, null/empty arrays, nullable values and truncation; public graph archives verify component identity in fresh CLR and NativeAOT consumers.

Visual shader archives also register the concrete numeric parameter definitions and ParameterRef. Copied DefaultValue, qualifier/slot state, advisory ranges and enum labels remain authoring data; shared declarations retain aliases across stages. The compiled program carries its own validated defaults/hints, so a source edit requires reimport. See [graph parameters](visual-shaders.md#parameters-and-graph-local-references).

Derivative graph definitions register the same typed resource factory and preserve OpType, Function, Precision and numeric defaults/absence. MultiplyAdd and derivatives store the shared VisualShaderFloatOpType identity. Graphs written with the former MultiplyAdd nested enum identity require reauthoring/reimport; strict schema lookup rejects it instead of creating a public alias.

## Version 8 material recovery

Version 8 adds Material as resource role 3, without bounds; resource provenance/field records and version-7 scene/connection tables retain their layouts. Known versions 1–7 remain readable using their original role validation. Unknown legacy generic definitions cannot be inferred to be materials. Metadata, UID rewriting and discovery use the same bounded reader as loading.

[Material placeholders](material-placeholders.md) retain unknown factory fields, aliases/cycles, scene-local ownership and missing external payload provenance through copies, bundles and threaded loading. Strict mode remains the default; unavailable concrete subtypes, corrupt role/schema/provenance and factory failures still reject before cache publication. A returned factory restores retained fields in a fresh process; an unbundled external dependency can revive when its file returns.
