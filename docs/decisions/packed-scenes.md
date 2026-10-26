# Packed scenes and runtime hierarchy duplication

Last updated: 2026-10-26

This bounded document owns ADR 0023. The [decision index](index.md) routes adjacent scene and resource contracts.

<a id="adr-0023"></a>
## ADR 0023: Typed in-memory packed scenes

Last updated: 2026-10-26

### Status

Accepted.

- Refines: [0013: Managed typed Resource contract](resources.md#adr-0013)
- Extends: [0005: Notifications and typed editor properties](core-object-runtime.md#adr-0005)
- Preserves: [0010: Typed event connections](core-object-runtime.md#adr-0010)

### Context

Electron2D needs reusable 2D scene templates before it has an asset loader/saver or editor. The accepted architecture excludes dynamic values, reflection-driven string calls, public manual reference counting, hidden scene activation, and fictional APIs for absent domains. Existing `Node`, `PropertyDescriptor`, `Resource`, and deterministic disposal contracts already provide most required runtime mechanisms, but they did not yet define scene ownership, stored-property selection, derived node construction, local resource setup, or failure rollback.

ADR 0013 deliberately deferred automatic local-to-scene behavior and avoided a Resources-to-Scene dependency until a real consumer existed. This component is that consumer. ADR 0013 now records the implemented scene association and root ownership; both active decisions describe the current contract.

### Decision

Add one runtime-only packed-scene component to the Scene domain and the existing `Electron2D.dll`:

- `PackedScene : Resource` stores one in-memory typed hierarchy and creates detached instances.
- `SceneState : ElectronObject` exposes a live read-only typed metadata view.
- `PackedSceneEditState` retains stable runtime/editor mode identities; only `Disabled` executes.

#### Capture and storage

`Node.Owner` is the storage-selection boundary. The root is always stored; traversal is parent-first depth-first. A descendant is selected when its owner is the capture root or a current descendant of that root marked as an editable instance. An excluded or internal branch remains excluded under the existing internal-child policy. The root does not own itself. EditorDescription uses an ordinary stored string descriptor. DisplayFolded remains transient. Nonroot scene-instance origin paths and enabled editable-instance flags use reserved typed node storage records, restored before parenting; flag capture requires an actual linked template or a nonempty instance origin, matching the instance role. Runtime-instantiated subscene roots retain their actual PackedScene identity. Packing an owned subscene root stores that borrowed template link; editable descendants already supplied by that instance store their original relative identity and apply their captured typed state to reused nodes. Locally added nodes retain ordinary factories. The current full stored-property capture policy remains unchanged: selected nodes save their complete state, while unselected subscene contents come from the linked template. Removed inherited nodes and groups are not deletion records. Root inheritance and non-Disabled PackedSceneEditState modes remain separate gaps. Persistent group flags are captured; runtime-only groups are not. Authored typed connections under ADR 0010 are captured in publisher traversal/registration order; endpoints outside the selected graph are omitted. Connections are restored after reference resolution, before lifecycle notification. Archive version 3 adds this table; versions 1 and 2 load with no connections.

Version-5 scene records persist linked PackedScene references, reused-node source paths and actual sibling placement for additions/overrides inside instances. Versions 1–4 retain their concrete layouts. A linked template graph is validated before construction for unavailable/empty templates, cycles and unsafe recursion depth. Child instances finish their own restoration, local-resource setup and root notification before outer overrides; the construction barrier remains active through the enclosing operation. Overrides reuse compatible already-local resources once per source and local scene, preserving inherited aliases and cycles. New local copies belong to that same nested root. Rollback owns all returned nested nodes and created resources, but never the borrowed templates. Connections already supplied by a nested template are not captured a second time.

#### Deferred linked instances

The Node load-placeholder flag is an authoring producer for an actual linked instance. Capture stores its scene path instead of a PackedScene resource edge. Runtime loading creates an InstancePlaceholder : Node without loading that template or constructing its node types. Pending values use the existing bounded typed archive field schemas; version 6 adds deferred paths/fields while retaining versions 1–5. Inherited nodes absent under that placeholder are not constructed; their overrides and records below those absent parents are skipped, matching the pinned TYPE_INSTANTIATED path. Ordinary locally added children whose parent exists remain ordinary children of the transform-free placeholder. CreateInstance restores the template plus the placeholder root's pending values, not skipped inherited-child records.

The reference stored-values dictionary is projected as GetStoredPropertyNames (always in application order) and GetStoredValue of T, consistent with SceneState's typed metadata queries. Node references are observed as paths. Pending property descriptors remain typed; no public Variant or universal property dispatch is introduced. Ordinary Node properties apply to the placeholder itself; values unavailable on that role remain pending for the created instance.

CreateInstance requires an attached nonroot placeholder on the scene owner. Its optional custom PackedScene replaces the default path lookup. It creates a sibling at the placeholder's previous position; only the replace option removes and queues the placeholder. Existing ordinary placeholder children are not transferred. Name and multiplayer authority follow the placeholder. Pending node references resolve inside the created hierarchy first, then relative to the placeholder. Pending root fields preserve their typed values, arrays, references and local-resource ownership. Construction/property failures occur before structural replacement; lifecycle callback errors after commitment preserve structural progress and aggregate cleanup failures. Typed authored connection schemas must accept the actual placeholder role; unavailable deferred endpoints cannot impersonate an unconstructed compiled subtype.

ResourcePreloader additionally recovers its reserved canonical named Resource slots under [ADR 0013](resources.md#adr-0013); this is an exact owner/schema binding, not reflected member dispatch.

Stored node state comes only from writable `PropertyDescriptor<TOwner, TValue>` instances explicitly marked `stored: true`. Strings, resources, reference-free value types and the explicit string/int/float/vector/color/contour array profiles are accepted. Typed Node references on node owners are captured as relative paths, without retaining the original node. Arbitrary objects, other collections, delegates, dynamic values and reflection-discovered members are rejected. Names and hierarchy metadata have dedicated fields.

Derived node types opt in through `CreateSceneInstanceFactory()`. The factory must be static, outlive the source, and return a fresh default node of the exact source runtime type. Factory results must have construction provenance in that invocation under [ADR 0013](resources.md#adr-0013); an unrelated object created concurrently cannot be acquired. Factory execution carries a context-local barrier that rejects both new-`SceneTree` construction and entry into an existing active tree before a node is returned. Capture stores the source identity; issuance rejects the source and any node already returned for that packed state.

`Pack(null)` preserves current state. For a non-null call, clearing occurs when capture begins; a later error intentionally leaves an empty packed scene. This follows the selected compatibility behavior and is documented rather than made transactionally different. Change notification occurs after the attempt and cannot roll back committed state.

#### Reconstruction and lifetime

Instantiation creates nodes parent-first, restores typed properties and persistent groups before parenting, assigns `Owner` after hierarchy construction, then restores node references and validates built-in cross-property state before scene-local resources. Forward references therefore resolve within each new hierarchy; missing targets become null and a target of an incompatible declared type rejects instantiation. SceneState exposes a stored node reference as a string path (empty for null), not as an original or partially instantiated node. The result is detached and does not enter or become ready in a `SceneTree`.

During reconstruction, each captured property must match a writable stored descriptor on the fresh target with the exact captured value type. Control/Window theme overrides have one bounded extension under [ADR 0083](rendering.md#adr-0083): when a fresh target has no descriptor yet, the engine can reconstruct only the reserved `ThemeColorOverride/`, `ThemeConstantOverride/`, `ThemeFontSizeOverride/`, `ThemeIconOverride/`, `ThemeStyleBoxOverride/` and `ThemeFontOverride/` families with exact `Color?`, `int?`, `int?`, `Texture`, `StyleBox` and `Font` value types. The descriptor is newly bound to the target's typed theme API. Unknown prefixes, other node roles, non-stored entries and mismatched types still fail. Captured source-owner delegates are never reused, and this does not add Variant values or general string member dispatch.

The returned root owns the created hierarchy and all resource duplicates created for that instance. Non-local resources remain shared regardless of whether their path is built-in or external. Scene-local duplication follows the local flag through nested graphs; built-in classification remains the separate ordinary DuplicateDeep(Internal) policy. Explicit force-duplicate hooks and isolated authoring capture retain their existing rules. Resource duplication preserves aliases and cycles across selected duplicated edges; `GetLocalScene()` is assigned before each local setup callback. This is a borrowed weak immutable object identity, not ownership of the hierarchy. Root disposal or collection makes the query return null; root replacement transfers the association and resource ownership together. Setup occurs once per local duplicate before notification `20`. Only the root of each template receives that notification after its complete hierarchy/resource restoration; linked children complete before enclosing overlays.

Direct and nested ordinary scene edges select their local flag before remap-cache lookup. An earlier forced acquisition never redirects a non-local borrowed resource. Aliases/cycles remap across selected local/forced edges; isolated authoring scopes still select all resources. ADR 0013 owns the corresponding ordinary shallow/deep per-edge rules.

Reconstruction validates topology before and after the final notification. An internal construction barrier prevents unfinished nodes from being disposed or entering any active tree, either as the root or as a child. Linear topology validation catches attachment to an unrelated detached hierarchy. Failure attempts to remove and dispose every returned node and resource duplicate acquired by the operation, then aggregates cleanup errors without disposing shared source resources. Factory-side allocations that are never returned cannot become engine-owned cleanup targets.

`SceneState` follows the current packed resource across replacement and path changes. A disposed state is replaced. An externally held state remains readable as the final snapshot after the source packed scene is disposed.

#### Runtime hierarchy duplication

Node.Duplicate executes the complete owner-specific DuplicateFlags family: Signals1, Groups2, Scripts4, UseInstantiation8, InternalState16 and Default15. Zero is valid. Copy ordinary children regardless of authoring Owner; internal children originate in fresh constructors/templates. Preserve names and source/editable/placeholder metadata. Raw copies have no authoring owners; instantiated contents retain their template owners. Linked instances use their actual PackedScene or saved source path, reuse inherited identities and apply source properties/additions at every depth. Preserve already supplied template state when its copy flag is disabled. Copy source compiled types through the existing static exact-type scene factories when Scripts is enabled; otherwise construct the nearest engine base through its registered concrete factory for fresh nodes. Compiled types already supplied by a linked template remain until Scripts selects the source role. Replace differing constructed roles using the existing detached replacement path; do not mutate CLR identity or introduce live Script binding. Required static factory hooks remain the accepted typed C# construction boundary.

Reuse typed property capture/restoration and known intrinsic descriptor families. Copy stored writable properties by default; InternalState also copies writable nonstored typed descriptors under the same accepted value profile. Clone supported array containers, share ordinary Resource edges, shallow-copy each always-duplicate Resource edge, remap references inside the copied hierarchy and retain borrowed external references. Inherited translation domains stay inherited. Retain source/ancestor/authoring-owner file and owned-resource leases before callbacks so subtree copies survive disposal of their source scene. Reuse the existing internal graph-ownership lease; scene ownership can append new resources and transfers with node replacement. Release on the last owner, preserving weak local-scene associations rather than rebinding shared resources to the copy. Isolated save preparation can transfer only exclusive direct ownership.

Signals copies active registered authored typed subscriptions after all properties/references. Match existing constructor/template bindings by schema, mapped receiver and typed encoded payload, consuming each baseline subscription once; preserve duplicate authored multiplicity and external receivers. Ordinary CLR subscriptions stay on their original objects. Incompatible endpoints removed by Scripts omission cannot retain their former compiled callback. Groups retains both persistence modes. No new dynamic property or event dispatch is introduced.

The result is detached with fresh node identities and no Ready delivery; actual linked-template instantiation keeps its own scene-instantiated notification. Source capture rejects mutation/reentry, factories retain the context construction barrier, and returned nodes stay protected from disposal/activation during reconstruction. Reject unknown flags, borrowed/reused/invalid factories and incompatible stored schemas. Validate ordinary topology after restoration and clean every acquired node, forced resource and lease on failure; external nodes/resources remain borrowed. Node.Duplicate is cold authoring/spawning work and has no zero-allocation claim. Public file authoring, fresh-process loading, headless callbacks and rendered output remain separate executable verification modes under ADR 0090.

#### Dependency refinement

`PackedScene` lives in Scene and depends on Resources. To implement the established local-resource callback contract, `Resource.GetLocalScene()` now exposes the owning `Node`, so the Resource base has a narrow reciprocal dependency on the Scene node abstraction. This is an intentional in-assembly cycle, not a package or assembly cycle: both domains still ship in `Electron2D.dll`, and Resources does not depend on `SceneTree`, packed-scene internals, rendering, or editor code.

The dependency is restricted to local-scene association, borrows the existing Core weak identity under [ADR 0050](core-object-runtime.md#adr-0050) and is cleared on resource disposal. Broad asset code should not add more Scene dependencies without a later ADR.

#### Missing-node recovery

Tolerant archive loading creates the non-publicly-constructible `MissingNode : Node` for an unavailable compiled node factory, retaining its original factory identity, typed bounded field records and authored connection records. Strict loading rejects instead. Ordinary inherited Node state still executes; unavailable compiled behavior does not. OriginalClass and OriginalScene are transient recovery identities. Saving an unavailable linked-scene placeholder discards the missing instance and unavailable inherited overrides, with configuration warnings; it does not pretend to recover that template.

RecordingProperties gates creation of new archived field names; existing names remain editable through typed stored-value operations. RecordingSignals gates new diagnostic event identities in explicitly recorded compiled connection bindings. These are bounded archive authoring operations, never universal member invocation or dynamic event emission. A recorded connection to an unavailable endpoint has no executing subscription; ordinary typed subscriptions continue to use their actual EventConnection tokens. Endpoint disposal and duplicate/capture policies own both kinds of authored records.

Archive version 7 makes connection labels, delivery policy and the single bound payload field self-describing, preserving reference roles, aliases, codecs and byte order. Unknown bindings can be retained only with recovered missing endpoints in tolerant mode. A later fresh process with the original factories and compiled connection registration restores real typed subscriptions. Versions 1–6 keep their layouts; absent legacy field roles or unknown legacy connection schemas cannot be inferred and fail explicitly. No assembly discovery or reflection loading is introduced.

#### Events and absent domains

Ordinary C# event subscribers and EventConnection tokens remain runtime objects. Authored SceneConnectionBinding subscriptions store only registered compiled schema IDs, selected endpoint indices and typed bound payloads under ADR 0010. They are restored before lifecycle notification; source delegates and Nodes are never serialized.

ResourceSaver/ResourceLoader persist the typed PackedScene model as version-8 archives under ADR 0013, with version-1/2/3/4/5/6/7 layouts retained. Registered compiled factory IDs replace in-memory delegates in files; no assembly loading or script serialization occurs. File roots own decoded graphs and instances retain internal leases under ADR 0014, including replacement/disposal. Import remapping, inherited-root authoring and non-Disabled edit states remain absent. Missing-node recovery executes as specified above. Deferred linked instances execute as specified above. Actual linked runtime subscenes execute as specified above. Programmatic descriptions, transient folding, explicit edited-scene membership and flattened editable-owner capture execute as described above. Generic, Mesh, ordinary Texture and Material missing-resource recovery executes under ADR 0013, including version-4 missing-payload provenance and the version-8 Material role; other exact asset substitutions retain their stated prerequisites. The unsupported `PackedSceneEditState` values fail explicitly.

### Consequences

- Runtime code can construct reusable 2D scene templates without waiting for an editor or asset format.
- Stored state is explicit and compile-time typed; new fields are not serialized accidentally.
- Derived nodes need a small static factory override and stored descriptors for constructor-independent reconstruction.
- Source nodes can be disposed after packing. Stored shared resources retain their ordinary resource lifetime and may still be observed by later instances.
- Per-instance local resource graphs have deterministic root ownership and setup order.
- Scene capture and instantiation are allocation-heavy orchestration paths, not frame-loop primitives.
- The Resources↔Scene type dependency is real, documented, narrow, and contained inside one managed assembly.
- Future file serialization must consume this typed model or supersede it explicitly; it must not silently introduce dynamic values, reflection calls, or persistent delegate capture.

### Rejected alternatives

- Reflection over node properties: rejected because it cannot express storage, ownership, validation, or reference remapping safely.
- A dynamic value container or string `Get`/`Set`/`Call`: rejected by ADR 0001.
- Require parameterless constructors through runtime activation: rejected because it hides factory failures and relies on reflection.
- Capture live source nodes or instance-bound factory delegates: rejected because the snapshot must outlive source disposal.
- Copy all C# event subscribers: rejected because delegates do not provide stable serializable endpoint identity or ownership.
- Activate a new `SceneTree` inside `Instantiate()`: rejected because scene lifecycle and host ownership must remain explicit.
- Add inert loader/saver/editor flags without their executable producer, stored state and consumer: rejected. Deferred instance producers/consumers follow the bounded contract above.
- Split packed scenes or resources into another managed assembly: rejected because the current product ships one Electron2D-owned DLL.

### Verification

The executable harness covers the current in-memory contract: empty and unsupported modes, capture selection/order, owner/path/group metadata, typed stored values, source disposal independence, repeated instances, local-resource aliasing/setup/ownership, live-state and path races, duplication, capture failure semantics, capture mutation rejection, factory closure/type/identity rejection, setup cleanup, detached-parent and active-tree escape rollback, and final snapshot survival. Repository verification also checks formatting, Release compilation, generated XML, documentation inventory, internal links, and the absence of prohibited production naming.

MissingNodeTests additionally checks role-aware unavailable factories, linked-scene loss warnings, typed field editing, recording gates, inactive connection metadata, duplication, fresh-process resave and restored compiled processing/connections. Independent versions 3–7 and malformed payloads cover the exercised binary layouts. Actual software-rendered Wayland/X11 GPU/compatibility frames verify healthy sibling output and ordinary cleanup beside recovered roles. These checks do not establish universal format compatibility, editor behavior, very large scene performance, platform asset packaging, script state, hardware/native allocator or owner acceptance.
