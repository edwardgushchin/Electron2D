# Compiled C# source assets

Last updated: 2026-10-27

[Script](../classes/Script.md) describes a compiled project Node or Resource and its source/type/schema association under [ADR 0091](../decisions/scripting.md#adr-0091). C# callbacks execute directly on the constructed project class. Source text and shared resource metadata remain separate from the state of a player or effect instance.

## Registration, source and construction

Script.RegisterNode/RegisterResource combine stable portable IDs, static exact factories, existing ResourceFileTypes registrations and explicit user PropertyDescriptor schemas. RegisterAbstract describes a project base without a constructor. A primary source path has one registered script identity; other partial-type source documents remain checksum-validated dependencies. Registration uses the compiled module MVID and a matching portable PDB type-document/sequence-point association. It verifies C# document language and compiler SHA-256/SHA-1 checksums; each source is bounded to eight MiB. Unknown IDs, duplicate source/type/ID registrations, incompatible schemas or unavailable symbols fail explicitly. Symbols/source availability on packaged foreign targets remains a prerequisite, not an inferred capability.

ResourceLoader recognizes `.cs` and compatible Script/Resource views. Uncached loading validates all partial documents before publishing a resource into the weak path cache. Reuse returns the existing compiled resource; Ignore returns an independent wrapper. Replace validates first, so changed/uncompiled files preserve the previous live wrapper. Metadata discovery returns the exact registered project class and Script resource. Source setters and atomic UTF-8 source saving do not reload CLR code. Compiled class registration remains valid even when editable resource text differs; build/restart is the executable class replacement workflow.

Script.New returns the exact project type through its static factory, or through a caller's typed constructor expression. It validates actual type, lifetime and user schema and disposes an incompatible returned instance. Native inherited descriptors use the existing object/property pipeline. Metadata query arrays/maps are copied and remain cold operations. Type/method/event discovery uses ordinary CLR metadata, without string invocation or an interpreter. Constants use caller-selected exact value types. Defaults use registered typed descriptors and a live owner context so borrowed resource defaults retain a meaningful lifetime.

Ordinary nested resource copies keep the same Script asset for all deep modes, including builtin/external paths and scalar/array/dictionary references. Explicit force/root copies retain independent text with the same compiled association; their cached targets do not replace ordinary edges. Scene-local flags govern acquired per-scene wrappers and their disposal, while ordinary non-local code remains borrowed and observes source edits. The actual RESET save-authoring path isolates source snapshots. [Public graph consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ScriptGraphs.cs) exercise both force orders and these scene/physical lifetime paths; ScriptTests passes the real registered asset through the same matrix and constructs the actual compiled project Node.

## Compiled owner source identity

`ElectronObject.GetScript()` returns the exact borrowed Script used for construction, or null for ordinary construction without an asset. Script.New records it before the derived constructor runs. Nested exact-type factories keep the innermost actual source; execution-context flow reaches joined workers, and closed scopes release source references and cannot bind later objects. The getter performs no metadata discovery, device access, callback or managed allocation.

Node copies retain the source when the Scripts flag is present. Resource copies keep ordinary Script edges shared under every deep mode. PackedScene source references participate in the same resource scope as stored properties: a scene-local source is duplicated once per instance, retains aliases across owners and receives that instance's local root. Version-9 scene/resource archives retain the concrete source through a typed resource-table reference. Loading prepares the sealed scalar-only Script before invoking its owner's exact factory; malformed IDs, wrong resource roles and mismatched compiled types reject. Earlier archive versions retain null where no association was recorded. Cache publication remaps source identities, and same-type file replacement adopts its restored source in the resource change batch.

The owner retains the source through cancelled disposal and releases it on committed disposal. It never disposes the borrowed asset. Explicit source disposal leaves compiled callbacks executing and GetScript may return that disposed identity. Source edits do not replace the class or association. Persistent saving or scene duplication requires live resource references, as with other borrowed resources.

Live SetScript replacement and extension reload remain open. Source-publication notifications below implement only the existing file/remapping paths; the complete ScriptChanged row remains Partial. Compiled C# initialization follows the constructor contract below; no additional initializer runs from a base constructor.

## Source publication notifications

File replacement and prepared graph publication report a changed source identity after acquiring its file-resource ownership. Equal identities and cache reuse emit nothing. PropertyListChanged precedes ScriptChanged; file copying follows with its final coalesced Resource.Changed. Public CopyFromResource copies data while retaining its existing source, so it adds no script notification. Initial construction, source text editing and explicit asset disposal also add no signal.

Each emission observes the ordinary signal-blocking state. Observer failures propagate after the source change is committed; a file-copy operation error and final Changed error retain their original identities in an ordered AggregateException. Retirement during the first source notification suppresses subsequent source delivery and rejects publication of the retired result. Resource batching accepts committed disposal without manufacturing an unbalanced-batch failure. Prepared graphs transfer dependency ownership before notifying; a callback that retires a prepared owner rejects that graph and preserves borrowed cached dependencies.

[Script source-publication checks](../../tests/Electron2D.Tests/ScriptTests.SourceChanges.cs) exercise these paths through actual compiled source assets and threaded loaders. The public notification consumer separately exercises reset/copy retirement without requiring source-symbol discovery. Live assignment/detachment remains the missing producer for the complete signal contract under [ADR 0091](../decisions/scripting.md#source-publication-notifications).

## Compiled C# initialization

The initialization entry point is the project class's ordinary C# constructor, including its base chain and typed arguments. The pinned [C# script bridge](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs#L103) selects and invokes that constructor after owner association; it does not dispatch a separate `_init` virtual method. [ADR 0091](../decisions/scripting.md#adr-0091) specifies the typed Electron2D projection.

Script.New invokes its declared factory or explicit constructor expression once. Project base/derived constructor bodies see the same stable identity and selected source. Engine-controlled schema validation, stored restoration and subsequent tree activation follow constructor completion. Explicit self-publication and other user-authored constructor side effects retain ordinary C# responsibility and are not rolled back. Default copy/scene/file factories execute their declared constructor before state restoration. Explicit typed factories may provide their own valid arguments.

RegisterNode, RegisterResource and RegisterNodeWithRPC accept a null default factory. This registers source/type/schema/RPC metadata without constructing a prototype or creating a file factory. A concrete required-argument type can then use New(() => new Player(arguments)); CanInstantiate remains true for the concrete class, without promising that a zero-argument call can initialize it. New without a constructor and reconstruction without an explicit copy/scene/file factory reject, preserving the existing owner. Saving/loading the Script asset retains this metadata-only compiled association.

Constructor errors propagate as the original exception. No result is acquired or schema-inspected after a throw. Ordinary C# constructor code must release partially acquired resources before throwing; the engine does not invoke virtual user cleanup on a partial instance. The invocation closes its source context on failure. Validation of a returned newly constructed result retains its separate cleanup policy. Source edits/disposal and ordinary frames never rerun initialization. Live reassignment/change delivery and extension reload remain separate open workflows.

## Storage, lifetime and consumers

Script archive data stores source text and stable CompiledTypeID; decoder construction resolves that identity against the host's explicit catalog. User scene/resource factories and typed stored properties continue to use the existing archive ownership, node references and exact CLR class reconstruction. A new process must register its same compiled classes/schema before loading the scene. Script duplication shares immutable compiled registration while copying resource text/configuration. Disposal invalidates the resource's own metadata/construction operations but does not unload process-held compiled types or stop already constructed ordinary Node/Resource instances.

[ToolAttribute](../classes/ToolAttribute.md) and [GlobalClassAttribute](../classes/GlobalClassAttribute.md) expose class-role metadata. Native editor scheduling/discovery is separate. RichTextLabel.InstallEffect(Script) creates a label-owned compiled RichTextEffect through the same typed factory. Per-glyph processing uses the existing virtual hook and bound glyph state. Removing that owned effect from CustomEffects or disposing the label retires it. Direct InstallEffect(RichTextEffect) continues borrowing its caller-owned effect.

## Selection and verification boundary

Three candidates were compared after FontVariation: FontFile (90 raw own rows) would add bitmap/glyph/atlas authoring but needs mutable indexed font caches and bitmap shaping/storage; SystemFont (18 rows) needs platform matching/fallback plus remaining raster/MSDF modes; Script/CSharpScript (21 raw rows including one deprecated method) can connect existing CLR nodes, typed schemas, loaders and fresh-process archive factories immediately. The compiled-source asset/factory workflow was selected for direct authoring value and Resource effect integration. Reload/live attachment and RPC are separate prerequisites, not metadata-only success claims.

ScriptTests exercises source/type/abstract/tool/global-name metadata, typed constants/defaults, exact/default/argument factories, inherited schemas, ordinary callbacks, disposal, source saving and archive identity. Its child process loads the saved scene, restores values and node references, and executes actual ready/process callbacks. Native scene/effect and prepared allocation results must be recorded after the exercised hosts complete. Metadata/build checks alone do not establish native pixels, editor acceptance, hot reload or foreign-platform execution.

The source association uses the public [portable PDB format](https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md) and the [compiler type-document record](https://github.com/dotnet/roslyn/blob/main/src/Dependencies/CodeAnalysis.Debugging/PortableCustomDebugInfoKinds.cs). It introduces no compiler SDK/package or runtime assembly scanner.

Native Linux Wayland GPU and compatibility hosts execute the loaded compiled Node and Script-created effect. Actual cyan glyph pixels and lifecycle teardown pass; 64 prepared effect/render intervals measure zero managed bytes on both backends. Visually inspected `/tmp/e2d-script-gpu.png` and `/tmp/e2d-script-compat.png` share SHA-256 `ed365da186da51f7b124fde891fb541d51b467c905d7d071288dba1e07662cc1`. Native allocator counts, foreign source/symbol linkage, physical input and owner acceptance are unverified. Changed-source mismatch preserves cached compiled assets; a deliberately invalid independent C# project reports compiler failure without replacing loaded classes. Constructor-role mismatch cleanup and ready activation rollback are exercised.

Source registration explicitly requires a file-backed assembly, matching portable PDB and complete untrimmed project declarations through RequiresAssemblyFiles and RequiresUnreferencedCode. It captures project method/event/literal-field metadata once after validating source association; queries return independent snapshots. This preserves private and inherited declarations and does not claim source registration for embedded NativeAOT assemblies. Static ResourceFileTypes scene factories remain independent of this source/PDB workflow.

## Factory result ownership

Both Script.New overloads require the returned object to be constructed during
that synchronous invocation, matching the existing Resource duplication and
scene-construction freshness boundary. Construction must belong to that invocation, including ordinary Task flow or engine worker tasks; unrelated concurrent objects reject under [ADR 0013](../decisions/resources.md#adr-0013). A borrowed result rejects before
property/schema callbacks or cleanup, regardless of exact or incorrect CLR type.
The shared registered file factory applies the same rule before its issued-object
tracking or decode. This prevents a failed script constructor from disposing an
active body, shape, material, world or borrowed effect, and prevents archive decode
from acquiring a previously created resource. A rejected result does not poison
later fresh construction.

A newly returned invalid object remains the operation's cleanup target. If its
validation and cleanup both fail, AggregateException retains both causes; a lone
constructor/schema failure preserves the original exception. Valid constructors
with arguments and registered Node/Resource factories retain their existing
behavior. Construction and metadata validation are cold allocating work.

The public API consumer PhysicsScriptFactoryTests exercises existing exact/wrong
objects, registered/argument constructors, real RichTextLabel effect adoption,
resource-file decode/retry and schema/cleanup failures. CPU without GPU,
software-Vulkan GPU and registered worlds run owner/separate modes. After cold
controls and 128 warmup frames, 128 measured complete physics/process/query frames
require zero owner/all-thread managed bytes while the preserved compiled actor
and physical body keep executing. Original ScriptTests additionally checks source,
metadata, argument construction and fresh-process scene reconstruction.

This positive source-asset construction profile requires the existing file-backed
CLR assembly, matching portable PDB and untrimmed metadata contract. NativeAOT
companion resource/scene checks exercise the ordinary registered file-factory
boundary and unbound Script graphs; they do not establish NativeAOT source
registration or positive Script.New construction. Live owner binding, Reload,
full Node.Duplicate and final native/hardware/network acceptance remain open.

The related World copy regression also asserts the current single-error retirement
contract: an isolated selected Finish failure remains its original exception while
the accepted new runtime stays committed. Paired failures retain aggregation.

## Compiled RPC declarations and execution

Script.RegisterNodeWithRPC and RegisterAbstractWithRPC extend the existing source catalog with copied typed RPCRegistration arrays. Script.GetRPCConfig returns effective class declarations without constructing an owner; unbound/Resource roles return empty. Most-derived IDs shadow base declarations, and later base registration is visible. Validation rejects null/default tokens, incompatible receiver roles, duplicate IDs and registrations over 65536 declarations before reserving source/type/file identity. Tokens/codecs/captures are retained with the process catalog.

Every actual project Node uses these defaults, including ordinary construction and fresh file-scene factories. Per-node RPCConfig policy/removal remains local, suppresses inherited defaults, and never changes Script metadata; these runtime overrides require constructor/factory authoring for reconstruction. A removed ID can then accept another compatible token. Node disposal releases per-instance callback captures. Warm send/receive dispatch uses direct typed codecs and catalog lookup without metadata snapshot allocation. Source text edits do not reload declarations.

ScriptRPCTests authors a compressed big-endian scene, starts a fresh process and exchanges real native WS/ENet RPC through the reconstructed classes. It checks inherited dispatch, authority/removal, failed callback scope restoration and 64 prepared active plus idle intervals with zero managed owner bytes. The native window path loads the file scene and verifies actual RPC-driven cyan pixels on software GPU/Compatibility under Wayland/X11. These are local programmatic authoring, headless simulation and rendered checks; they do not establish foreign symbols, physical GPU/mobile, routed deployment, native allocation or human/editor acceptance.

## Source identity qualification, 2026-10-27

Twenty accepted profiles in `bin/script-construction-identity/2026-10-27/qualification.json` cover compiled source construction/metadata, source edits and disposal, nested/worker provenance, cancelled/committed retirement, zero-byte prepared reads, Node/Resource copies, scene-local aliases, actual archive and fresh-process reconstruction, malformed source references, failed cache replacement, version-eight assets, resource/local/linked/deferred/missing/material/texture regressions, and physical factory/lifecycle paths. The direct `dotnet` missing-resource harness lacks a child assembly argument; the unchanged apphost path executes both fresh children successfully. Failed cache replacement and CLR host packaging attempts retain separate receipts and are not counted among accepted profiles.

Four CLR and four strict NativeAOT public profiles use CPU without GPU discovery and real NVIDIA Vulkan GPU, each on owner/separate physical threads. Together with four CLR compiled-factory profiles they execute 2,560 measured complete frames at zero owner/all-thread managed bytes. Native profiles exercise null source observation on ordinary compiled physical owners; positive registered source assets remain the desktop portable-PDB capability above. This does not claim source-free or NativeAOT registration. Portable-symbol source proofs bind the retained CLR cohorts and native input snapshots to the authored C# files. Native maps exclude CoreCLR; all GPU receipts exclude software Vulkan and report no new Xid/fallen-off entries. These functional runs do not establish mass-scene throughput or wider native/platform/network acceptance.

## Initialization qualification, 2026-10-27

`bin/script-initialization/2026-10-27/` retains the pinned constructor bridge/source hashes, source-bound CLR and strict NativeAOT cohorts, manifests and checks. Fourteen CLR profiles cover full script/source/required-argument/copy/archive/fresh-process behavior, the existing compiled RPC fresh-process suite, and CPU/NVIDIA GPU owner/separate initialization, factory and public construction/notification consumers. Four strict NativeAOT public profiles verify ordinary compiled construction and reconstruction on CPU/no-device and NVIDIA GPU in both numerical lanes. Positive registered source construction remains the desktop portable-PDB capability; NativeAOT/source-free registration is not inferred.

The twelve CLR physical profiles and four native controls execute 3,072 complete measured frames, each interval requiring zero owner/all-thread managed bytes. Required constructor state drives actual movement and point queries after explicit source disposal, with the same RID and one initialization. These are functional/allocation checks; they do not certify broad networking, native retained memory, foreign platforms or mass-scene/window performance. The nullable factory input does not add a new dispatcher, per-frame wrapper or reflected constructor invocation.


## Source publication qualification, 2026-10-27

`bin/script-change-publication/2026-10-27/validation.json` retains eighteen accepted CLR profiles and four strict NativeAOT public controls. Actual source assets exercise equality/cache reuse, ordinary data-copy preservation, source/property/final-resource event ordering, blocked signals, exact combined observer failures, committed ownership, callback retirement and threaded remapping. Resource base, scene/local, archive and locale suites verify the affected copy/publication paths; compiled RPC, initialization and factory controls retain their existing behavior.

CPU runs exclude device discovery; GPU runs map NVIDIA Vulkan with software implementations excluded. Owner/separate profiles execute 3,072 complete measured physical frames at zero owner/all-thread managed bytes. Native controls cover ordinary compiled notifications and reset/copy retirement; positive registered source behavior remains the portable-PDB desktop capability. Source proofs bind 2,693 runtime documents, 1,024 root-test documents and both public-consumer files to the retained CLR binaries, with separate proofs for native inputs. The complete ScriptChanged row remains Partial; these functional/allocation checks do not qualify live assignment/detachment, mass-scene performance, broad networking or foreign platforms.


## Native extension reload boundary

`Object.NOTIFICATION_EXTENSION_RELOADED` is an open extension-host dependency.
The pinned [Object constant](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Object.xml#L1022)
applies to extension classes and their descendants. Its automatic producer is
[GDExtension.finish_reload](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/extension/gdextension.cpp#L925),
with instances tracked by registered extension class, independently of an attached
C# Script. The [extension module](../coverage/classes/GDExtension.md) and
[manager](../coverage/classes/GDExtensionManager.md) remain Blocked on their own
coverage pages; this inherited notification has the same concrete prerequisite.

Reload invalidates removed method bindings/classes, rebinds surviving instances,
restores every retained instance's stored state and then sends notification 2.
The producer re-resolves each object identity and skips deleted instances. Its
saved instance state is cleared after notification. An implementation must define
the typed equivalent of module/class registration, owner/base identity, instance
construction/rebinding, stored state/references and those publication phases.
Removed types/methods, stale delegates and failure during replacement also require
an explicit lifecycle policy. A constant plus manual Notify does not supply it.

A rejected 2026-10-27 experiment attached a .NET MetadataUpdateHandler to the
existing weak ObjectCensus. A real `dotnet watch` edit changed a live CPU RigidBody's
virtual method from 1 to 2; instance ID 5, RID 2, authored field 37 and owner thread
1 survived. The host log shows its update handler, successful update batch and
application exit 0. It demonstrates CLR method-code replacement. It does not
register an extension module, replace an extension instance or restore module-owned
state. Broadcasting that notification to ordinary compiled project classes would
add events to the existing scripting contract. The candidate constant, handler,
census snapshot and test consumer were removed from the production tree; its
source, DLL/PDB, logs and raw receipts remain under
`bin/compiled-class-reload/2026-10-27/`.

The first probe reached its application timeout without a reload notification;
its watcher did not report handling the edit. A second probe in a normal test
source directory used verbose watcher output and a later edit, and succeeded.
Those outcomes are retained separately; they do not establish why the first edit
was missed. Only the successful second run proves actual code replacement.

This audit changes the inherited row from Unimplemented to Blocked with the
specific extension-host trigger. It neither excludes it nor promotes it to
Implemented. Live SetScript/ScriptChanged and Script implementation reload remain
distinct open workflows under ADR 0091. The current physics ledger remains
1362/1369 (99.49%), seven open (five Partial, two Blocked), 92 Excluded.
