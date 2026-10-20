# Compiled C# source assets

Last updated: 2026-10-20

[Script](../classes/Script.md) describes a compiled project Node or Resource and its source/type/schema association under [ADR 0091](../decisions/scripting.md#adr-0091). C# callbacks execute directly on the constructed project class. Source text and shared resource metadata remain separate from the state of a player or effect instance.

## Registration, source and construction

Script.RegisterNode/RegisterResource combine stable portable IDs, static exact factories, existing ResourceFileTypes registrations and explicit user PropertyDescriptor schemas. RegisterAbstract describes a project base without a constructor. A primary source path has one registered script identity; other partial-type source documents remain checksum-validated dependencies. Registration uses the compiled module MVID and a matching portable PDB type-document/sequence-point association. It verifies C# document language and compiler SHA-256/SHA-1 checksums; each source is bounded to eight MiB. Unknown IDs, duplicate source/type/ID registrations, incompatible schemas or unavailable symbols fail explicitly. Symbols/source availability on packaged foreign targets remains a prerequisite, not an inferred capability.

ResourceLoader recognizes `.cs` and compatible Script/Resource views. Uncached loading validates all partial documents before publishing a resource into the weak path cache. Reuse returns the existing compiled resource; Ignore returns an independent wrapper. Replace validates first, so changed/uncompiled files preserve the previous live wrapper. Metadata discovery returns the exact registered project class and Script resource. Source setters and atomic UTF-8 source saving do not reload CLR code. Compiled class registration remains valid even when editable resource text differs; build/restart is the executable class replacement workflow.

Script.New returns the exact project type through its static factory, or through a caller's typed constructor expression. It validates actual type, lifetime and user schema and disposes an incompatible returned instance. Native inherited descriptors use the existing object/property pipeline. Metadata query arrays/maps are copied and remain cold operations. Type/method/event discovery uses ordinary CLR metadata, without string invocation or an interpreter. Constants use caller-selected exact value types. Defaults use registered typed descriptors and a live owner context so borrowed resource defaults retain a meaningful lifetime.

Ordinary nested resource copies keep the same Script asset for all deep modes, including builtin/external paths and scalar/array/dictionary references. Explicit force/root copies retain independent text with the same compiled association; their cached targets do not replace ordinary edges. Scene-local flags govern acquired per-scene wrappers and their disposal, while ordinary non-local code remains borrowed and observes source edits. The actual RESET save-authoring path isolates source snapshots. [Public graph consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ScriptGraphs.cs) exercise both force orders and these scene/physical lifetime paths; ScriptTests passes the real registered asset through the same matrix and constructs the actual compiled project Node.

## Remaining owner association

The Script asset/catalog prerequisite is implemented. Script.New constructs and validates the exact CLR instance, but neither that path nor file reconstruction records an owner-to-Script resource association. ElectronObject has no script getter or assignment operation. Their next prerequisite is the typed live-binding identity/lifetime/initialization contract in ADR 0091, rather than creating another Script resource or type catalog. Existing constructed objects continue to execute after an asset wrapper is disposed.

The pinned [Object initialization callback](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Object.xml) runs when an owner's script is instantiated and can follow object construction. Ordinary C# constructors and typed New are executable construction paths, but they do not certify the missing associated-script initialization/assignment behavior. Required arguments, file/duplication creation, ordering and failure semantics must accompany that binding. The inherited initialization and getter/assignment rows remain open; no additional initializer is called from a base constructor.

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
