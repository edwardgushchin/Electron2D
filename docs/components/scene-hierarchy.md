# Scene hierarchy component

Last updated: 2026-10-16

## Child-name collisions

AddChild and AddSibling preserve unique names and resolve collisions across
ordinary/internal children. The existing typed overloads use generated
`@Class@number` names; new bool overloads select `forceReadableName`. Readable
names start at suffix 2 or advance the existing decimal suffix, with leading
width and arbitrary-length carry. NodeNameNumberSeparator samples live feature
overrides for no separator, space, underscore or dash (0–3, default 0). Existing
typed constructors retain their nonblank class-name default.

Name assignment uses readable suffixes. Reparent and ReplaceBy use generated
insertion names, including children transferred into an already populated
replacement. Object/RID identity and internal partitions remain stable. Structural,
owner, lifetime, capture and numerical-borrow validation precede naming; replacing
an ancestor into its descendant is rejected before mutation. Names/path caches
commit before callbacks, and callback errors retain the committed progress.
Public name assignment replaces each `.`, `:`, `@`, `/`, `"` and `%` with `_`
before suffix resolution. Null/empty/whitespace-only inputs reject before
mutation. Every accepted assignment, including the same input, propagates path
notifications through detached or active descendants. Only active nodes then
raise Renamed and the tree node/change events. Failures are combined after the
remaining descendants and event phases are attempted. Stored scene names bypass
fresh authoring normalization, retaining exact internal `@Class@number` identities;
other reserved path characters reject on archive input.
Replacement attempts all saved ownership assignments and combines their errors
with earlier transfer failures, so one rejecting target cannot skip restoring a
live child that returned to the original node.

Path-rename propagation now reuses depth-specific child snapshots instead of
allocating a new array per non-leaf rename. It preserves the original capture
after each node's notification, nested renames, removal during delivery and error
recovery. Every traversal clears borrowed child references in finally; first
capacity/depth growth remains cold work. Readable/generated strings on actual
collisions remain allocated authoring results.

The [public naming consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.NodeNames.cs)
checks four separators, leading/40-digit suffixes, feature overrides, internal
sibling order, physical roles, stable identity, renaming errors, reparent/replacement,
owner/numerical rejection and retained traversal lifetime. Packed scene names and
the setting survive actual file saving and fresh-process loading. Its 128 warm /
128 measured complete moving frames include alternating unique names, callbacks
and path lookup, requiring 0/0 owner/all-thread bytes. At 6 scene units/second and
1/60-second ticks, expected travel is 12.8 units with .01-unit float accumulation
tolerance. Run the public consumer with `--node-names [--cpu|--gpu] [--threaded]`;
without a built-in selector it uses the independent registered solver. Headless
software execution remains separate from hardware/window and full physics gates.

Current Linux x64 verification passes 12 managed Debug/Release and six strict
Release NativeAOT naming profiles, each with fresh-process scene/config loads.
Six public path and six Debug orphan profiles also pass at 0/0 warm bytes; three
legacy hierarchy/scheduling/resource selectors pass. A separate three-process
threaded CPU-authority/software-GPU prediction check reaches tick 600 with late
admission at 310, zero warm step/replay bytes and final convergence. Actual CPU
numerical lanes are instrumented; GPU numerical thread identity is not asserted
by that probe. Eleven portable-PDB/source sets match the exercised runtime/consumer/
test/AOT host. Release runtime, format, compiled coverage and wiki checks pass.
Artifacts and assembly identities: `bin/physics-node-names/2026-10-15/`.

The assignment follow-up also checks Unicode and typed-descriptor normalization,
blank-input rollback, unchanged names in detached/active trees, and five simultaneous
path/node/tree failures with ordered continued delivery. Physical roles retain
InstanceID and public path lookup through normalized and unchanged assignments.
Stored generated names survive exact packed/file/fresh-process reconstruction;
publicly reassigning that same generated text applies ordinary normalization.
The measured full frames now include both changed and unchanged clean assignments.
Current Linux x64 follow-up passes 18 naming profiles, twelve path/orphan
regressions and four hierarchy/scheduling/resource selectors, plus Release/API/wiki
and eleven PDB/source sets. Its isolated mixed 600/310 process check passes;
the retained concurrent-build control misses local GPU wake phases. A later
[coordinated network check](physics-network-example.md#deterministic-validation-phases-and-control-handoff)
now exercises those original phases during Release compilation and under explicit
slow polling. Ordinary slow-client throughput remains a separate acceptance gate.
Naming evidence: `bin/physics-node-name-assignment/2026-10-15/`;
network follow-up: `bin/physics-network-phases/2026-10-15/`.

## Orphan node diagnostics

`Node.GetOrphanNodeIDs()` and `PrintOrphanNodes()` share a Debug-only weak node
registry. All live detached nodes participate, including roots, internal children,
compiled subclasses and physics siblings. Owner metadata and direct physical-space
attachment do not make a detached node part of a SceneTree. Active, disposing and
collected nodes are omitted. Release builds register no nodes, return an empty
ID array and produce no diagnostic output, as required by ADR 0008.

IDs are caller-owned `ulong[]` snapshots ordered by InstanceID. Printing captures
root-relative hierarchy names and actual CLR class names before ordinary
`OS.LogMessage` callbacks run; callback edits/disposal do not invalidate remaining
rows. Script file identity is not manufactured by this diagnostic. Collection
and concurrent construction/disposal do not leave strong registry roots. These
are cold nearby observations, with ordinary allocated snapshots in Debug, rather
than atomic tree or solver-state inspection.

The [public-only orphan consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.NodeOrphans.cs)
checks detached/active membership, internal and inherited roles, caller array
isolation, both logging sinks and logger reentry, explicit physical attachment,
failed deletion, abandoned/failed-construction weak collection and concurrent
construction/scans. Its full-frame bracket excludes cold diagnostic scans and
requires actual motion plus 128 measured owner/all-thread zero-byte frames after
128 warm frames. Reproduce with the consumer's `--node-orphans`, optional
`--cpu`/`--gpu` (otherwise registered), and optional `--threaded`, in Debug and
Release. GPU execution requires the explicit software ICD used by the current
physics checks; window and target-device acceptance remain separate.

Current Linux x64 checks pass 12 managed and 12 strict NativeAOT profiles across
both build configurations, three backends and both numerical thread policies.
All 24 full-frame brackets report 0/0 managed bytes. Six Debug node-search
regressions also retain 0/0 bytes. Eleven portable-PDB/source sets match the
exercised runtime, consumer and small AOT host; artifacts and identities are in
`bin/physics-node-orphans/2026-10-15/`. Release runtime build, format, compiled
API coverage and wiki tests/generation/check pass. These checks close the two
inherited diagnostic rows, not the remaining physical/numerical, native-memory,
foreign-platform, hardware or real-window gates.

## Exact node paths

`GetPath()` returns a cached immutable absolute string only while the node belongs to
an active SceneTree; detached nodes return an empty string. The first query creates
only the requested node's string. Renaming any ancestor, changing parents, entering
or leaving the tree clears affected caches before subsequent observer access. Exit
callbacks can still read their active path; TreeExited sees an empty path. Callback
failures leave the committed hierarchy and path state coherent. Reordering siblings
does not invalidate a path. Previously returned strings remain caller-owned values.

`GetNode`, its typed overload, `GetNodeOrNull` and `HasNode` share a span-based exact
resolver, including internal and unowned children. Relative paths, repeated slashes,
`.`/`..`, active absolute root names and owner-scoped `%Name` retain borrowed node
identity. Empty input and a lone colon do not resolve; whitespace matches literally.
Valid property subnames are ignored for node traversal; `:Name` addresses this node,
and a trailing colon is accepted. Null throws ArgumentNullException, empty intermediate
subnames throw ArgumentException, required misses throw KeyNotFoundException and
wrong typed targets throw InvalidCastException. The resource endpoint remains a
separate cold descriptor query with its documented required-input validation.

Attached path reads accept the owner or the receiver's current processing group.
Other workers reject before reading even a cached string. Exact traversal can cross
group boundaries through immutable names; it does not invoke target callbacks.
`GetPathTo` retains its source/target unique-path rules and supports detached nodes
in the same hierarchy; its returned relative path is an explicit allocated snapshot.

[The public NodePaths consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.NodePaths.cs)
checks grammar, unique/typed identities, internal children, rename/reparent/detach,
failed activation/exit/rename callbacks, pre-entry multiplayer root binding, thread guards and processing groups through moving physical nodes
and their sibling roles. Each profile warms 128 full physics frames then measures
128 more with strict zero owner/all-thread managed bytes for cached paths, required
and nullable lookups, unique names and misses. Backend numerical performance,
native allocation, real-window output and other platforms remain separate gates.
The same public consumer passes all six CPU/software-Vulkan/registered serial and
separate-thread profiles in both managed execution and strict NativeAOT. Process
maps distinguish CoreCLR from the native executable, confirm lavapipe only in GPU
profiles and show no physical NVIDIA library; numerical worker threads appear only
in separate-thread profiles.

## Resource endpoints and subtree operations

`GetNodeAndResource(string path)` resolves slash-separated node names followed by colon-separated typed property names. It returns a C# tuple `(Node? Node, Resource? Resource, string RemainingPath)` rather than a heterogeneous array. Each resource-valued property becomes the next owner. The result retains the last borrowed resource and stops at the first existing scalar, node, other non-resource value or null, returning that component and later components as a colon-prefixed remainder. It does not validate the remainder. A missing node/property returns a null Node; a resource already found remains in the tuple. `HasNodeAndResource` tests the Node field, so an existing null property is a successful remainder. Getter/schema errors remain visible; disposed resolved resources reject access.

Node names retain ordinary relative, absolute active-tree and owner-unique rules. `:Texture` addresses the calling node; a single trailing colon is accepted. Empty intermediate components reject. GetNode/GetNodeOrNull/HasNode now parse the same subname syntax and use only its node portion. All resource property access uses the exposed `PropertyDescriptor<TOwner,TValue>` schema, including derived types and read-only descriptors. This is a cold query that discovers descriptors; no hot path allocation claim is made.

`PropagateCall(Action<Node>, bool parentFirst=false)` runs a compiled operation on every node in a subtree, including internal children. Generic overloads filter by a Node role and optionally pass one typed argument, including tuples. Children-first is the default; parent-first visits each parent before descendants. Ordered child traversal uses the existing hierarchy barrier: changing the currently traversed child list, reparenting/disposal through that parent, and disposal of the current node reject, while ordinary properties may change. Mutating a descendant before its own traversal retains the existing live hierarchy semantics. Nested calls retain independent depth. Callback failures are combined after the remaining nodes are attempted, and finally restores every traversal guard. Attached calls require the scene owner.

SubtreeResourceAuthoringTests verifies path/error/null/last-resource semantics, custom typed getters, relative/absolute/unique names, all operation orders and roles, typed arguments, internal children, structural/disposal guards, error continuation, guard release and 1000 warmed propagation calls with zero managed allocation. Edited Sprite/AtlasTexture state survives `.e2dscene` save/load/reconstruction. Linux Wayland GPU and hardware compatibility hosts verify edited atlas pixels and propagated positions. Other platforms, full editor automation, large hierarchy stack limits, native allocation and human acceptance remain unverified. Node duplicate and persistent connections remain their own executable prerequisites.

## Scope and owned types

The accepted hierarchy is implemented under [ADR 0008](../decisions/scene.md#adr-0008). One ordered Node tree hosts neutral logic, canvas objects and spatial gameplay objects.

| Type | Base | Responsibility |
| --- | --- | --- |
| [Node](../classes/Node.md) | ElectronObject | Hierarchy, lifecycle, owner-scoped unique paths, groups, tree diagnostics and notification propagation, process/input callbacks, ownership and deletion. |
| [CanvasItem](../classes/CanvasItem.md) | Node | Abstract drawing base, visibility, Z/Y order, behind-parent drawing, modulation/materials and transform queries/notifications. |
| [Entity](../classes/Entity.md) | CanvasItem | Concrete position, rotation, scale, skew and spatial helpers. |
| [Control](../classes/Control.md) | CanvasItem | Rectangular layout, pivot transform, resize, root viewport pointer/focus/hover and action navigation, plus cursor policy and descendant clipping; remaining GUI behavior is incomplete. |
| [LayoutPreset](../classes/LayoutPreset.md) | enum | Sixteen predefined four-anchor arrangements for Control. |
| [GrowDirection](../classes/GrowDirection.md) | enum | Fixed-edge policy when a control grows to its minimum size. |
| [LayoutDirection](../classes/LayoutDirection.md) | enum | Explicit, inherited and locale-derived horizontal layout policies. |
| [LayoutPresetMode](../classes/LayoutPresetMode.md) | enum | Intrinsic-minimum or retained width/height policy for offset presets. |
| [RecursiveBehavior](../classes/RecursiveBehavior.md) | enum | Inherited focus eligibility for direct Control subtrees. |
| [RecursiveBehavior](../classes/RecursiveBehavior.md) | enum | Inherited pointer eligibility for direct Control subtrees. |
| [ProcessMode](../classes/ProcessMode.md) | enum | Pause-aware processing policy on Node. |
| [NodeAutoTranslateMode](../classes/NodeAutoTranslateMode.md) | enum | Inherited automatic translation policy on Node. |

[Sprite](../classes/Sprite.md) and [Parallax](../classes/Parallax.md) derive from Entity. [Timer](../classes/Timer.md) and [Viewport](../classes/Viewport.md) derive from Node; [Window](../classes/Window.md) derives from Viewport. [CanvasLayer](../classes/CanvasLayer.md) derives directly from Node and establishes an independent canvas. [Camera](../classes/Camera.md) derives from Entity and owns viewport tracking; CollisionShape remains a future spatial type. Control has executable layout, transform, root viewport pointer routing, hover/cursor selection, keyboard focus and Tab/arrow navigation. The root Viewport exposes the focused Control, explicit release and a focus-change event; Control receives focus notifications before its focus events. Buttons, typed themes, box/grid layout and scroll containers now execute through this hierarchy; broader GUI routing remains incomplete.

`Node.InternalMode` partitions children into Front, ordinary and Back ranges. Ordinary public child queries omit internal children by default, while lifecycle, processing, input, theme and canvas traversals include them. PackedScene captures ordinary owned children and constructors recreate internal implementation children. ScrollContainer uses five real internal children for bars, hints and focus decoration. [ScrollInternalNodeTests](../../tests/Electron2D.Tests/ScrollInternalNodeTests.cs) covers order, movement, processing and capture.

[RemoteTransform](../classes/RemoteTransform.md) also derives from Entity. It weakly targets another spatial node by path, resolves the target on tree entry or an explicit cache refresh, and transfers selected transform components through the existing queued global or synchronous local notification path. It rejects hierarchy feedback and remote-target cycles; PackedScene stores the path and policy, not the live target.

`Node.ReplaceBy` installs a detached replacement at the old sibling index, fires `ReplacingBy`, and moves the old node's children in order. The old node remains alive and detached. Optional group copying retains persistence flags; owner references and packed-scene-local resources follow the replacement. Active-root/ancestor replacement fails before mutation; naming collisions use the shared insertion policy. A selected current or edited scene clears when the old node leaves the tree.

`Node.PhysicsInterpolationMode` resolves Inherit, On and Off through the parent chain; a root inherits On and Control starts Off. `IsPhysicsInterpolatedAndEnabled` also requires the tree-wide switch. `ResetPhysicsInterpolation` delivers notification 2001 and resets presentation history without changing logical transforms. Pause and application-suspend notifications reset active histories. SceneTree snapshots eligible canvas items around physics callbacks, and the renderer samples the resulting poses on both backends; a current Camera's viewport transform follows the same fraction.

`Control.SetAnchorsPreset` applies all four anchors in the pinned side order. Its default preserves the current rectangle by adjusting offsets; `keepOffsets` leaves the offsets and immediately reflows within the current parent area. `SetOffsetsPreset` places all four edges with the selected size mode and signed margin without changing anchors; `SetAnchorsAndOffsetsPreset` composes both steps. `SetAnchorAndOffset` updates one side. `ControlLayoutTests` checks all sixteen arrangements, four size modes, LTR/RTL, arbitrary anchors, parent and viewport origins, failures and later parent resize. Complete GUI container layout remains separate.

Control resolves componentwise minima and maxima from virtual hooks, finite custom values and direct-parent maximum propagation. Negative maximum components are unbounded. Reflow grows to the minimum, then shrinks to the maximum if enabled; the three growth directions choose which edges move. Attached visible changes coalesce through the scene tree's deferred queue before their size-change events. `ControlLayoutTests` checks conflicting bounds, notification order, derived values, parent propagation, top-level escape, packed-scene policy and invalid inputs. Container/Box layout now consumes size flags and propagated bounds under [ADR 0081](../decisions/rendering.md#adr-0081). Desired-size consumers and wrapping windows remain distinct gaps.

Direction-aware Control layout mirrors the resolved rectangle after horizontal size bounds. Explicit LTR/RTL and same-domain inheritance are executable; `Position` and `Size` setters preserve physical coordinates while RTL is active. Locale modes consult the selected or managed UI culture and registered translation catalogs. Direction policy changes propagate notification 49 parent-first and reflow affected controls. `ControlLayoutTests` checks parent/child overrides, domain boundaries, locale catalogs, packed state, invalid values and notification order. Root/forced project policies, Window inheritance, exact native locale aliases and automatic reflow on global culture change remain open.

The shared Control rectangle-edit path now supports `SetPosition`, `SetSize` and `SetGlobalPosition` with optional keep-offset anchor recomputation, pairwise begin/end offset edits and `ResetSize`. Position and size properties delegate to the same behavior. Negative finite size requests clamp to the effective minimum before maximum constraints; keep-offset writes require nonzero parent width and height. `ControlLayoutTests` checks stable offsets, adaptive anchors after parent resize, transformed global placement, RTL/translated viewport coordinates, finite-input rollback and bounds. Complete container sizing remains separate.

Control's ordinary pivot now combines `PivotOffset` with `PivotOffsetRatio * Size`. The optional offset transform composes after that pivot. Visual-only mode leaves logical/global transforms and GUI hit regions unchanged while the canvas renderer moves this item's retained geometry and descendants; disabling visual-only moves both. Values persist while the offset is disabled and through PackedScene. `ControlLayoutTests` verifies matrix order, relative resize behavior, hit testing and state; focused dummy and Wayland scene-hierarchy pixels verify actual child drawing in compatibility and GPU/Y-sort paths.

## Runtime flow

Node owns ordered children of any Node subtype. SceneTree activates the tree parent-first, delivers ready child-first and exits child-first. Paths, groups, Owner metadata, process/input settings, typed child events and factories use Node. Node also reports depth-first tree order, prints subtree paths or Unicode branches, and propagates manual notifications parent-first. Propagation prevents direct child-list changes while visiting a node and collects callback failures after attempting other descendants. Engine supplies scaled/original deltas; Timer and Tween reuse these scheduling lanes. Frame/input/lifecycle mutation guards and failure-continuing cleanup remain in the neutral layer.

`Node.UniqueNameInOwner` registers a `%Name` path within the node's owner scope. A second claim of the same name loses; renaming, owner changes and detach update lookup immediately. `GetPathTo(useUniquePath: true)` uses the pinned target-side shortcut first and then the source-side rule. PackedScene restores flags before owners and resolves them when ownership is assigned. [NodeUniqueNameTests](../../tests/Electron2D.Tests/NodeUniqueNameTests.cs) covers scope, conflicts, path generation, packing and attached thread guards. The current owner-scope lookup scans the subtree; index it only if profiling establishes a problem.

Node inherits its translation domain and automatic-translation mode from the nearest parent unless either is explicitly overridden. SceneTree samples the typed root setting before entry; entry and later mode/domain changes notify affected nodes. PackedScene stores explicit domain overrides and the auto-translation mode, preserving live inheritance for other descendants.

CanvasItem adds retained drawing and canvas state. A direct canvas parent contributes transform, modulation and material; a neutral Node breaks those chains. Global transform, Z accumulation and canvas sampler inheritance stop at TopLevel. CanvasItem filters/repeat resolve through direct canvas parents, then Viewport defaults; neutral nodes break canvas inheritance. Toggling TopLevel preserves local state and recomputes global coordinates. Visibility follows direct canvas parents, including TopLevel, and the containing window. Window owns native visibility independently; its changes notify canvas roots, including roots below neutral nodes.

Canvas roots follow scene order, each subtree before the next root. Effective Z takes precedence over behind-parent and nested local Y ordering. These canvas settings are stored by PackedScene and do not change process or ordinary Node input order; root Control pointer targeting uses canvas layer and effective Z. See [canvas rendering](canvas-rendering.md#canvas-ordering) for group boundaries.

Canvas membership has explicit entry/exit notifications independent of manual tree notifications. Entry is parent-first; exit is child-first. TopLevel rebinds only that item. Visibility delivery skips locally hidden branches, Hidden follows effective hide transitions, and showing schedules redraw. The [canvas component](canvas-rendering.md#canvas-lifecycle) records ordering and failure behavior.

CanvasItem.GetTransform is abstract. Entity implements it with an engine-owned Transform and adds the writable spatial properties. Control implements it through a rectangle, pivot, rotation and scale. A direct Control child reflows when its parent rectangle changes; a root Control follows its viewport size. An Entity child consumes either parent's transform. Local geometry changes use the separate ItemRectChanged event and protected NotifyItemRectChanged helper; Entity transforms do not emit that event. Global transform invalidation stops at neutral and TopLevel children. Global notifications coalesce in the scene queue; a delivery failure does not skip later queued items. Local notifications run synchronously only while attached and enabled.

The local-transform query audit confirms the pinned pure-virtual role: Entity and Control overrides return their own local matrices and enforce attached owner-thread reads; custom CanvasItem subclasses must enforce that guard in their override. ControlLayoutTests covers the sibling alongside Entity, and the full Wayland renderer suite exercised mixed hierarchy and GUI transforms on both backends.

Entity.Reparent overrides the neutral operation. It validates a destination inverse before mutation when retaining global state, preserves structural lifecycle checks, then restores its local transform after attachment. A neutral reparent has no spatial state to preserve. GetRelativeTransformToParent separately multiplies local transforms along an uninterrupted spatial-node chain. Translate adds in parent space; MoveLocalX/Y move along the current local basis.

The audited Entity scale/movement slice follows pinned `Node2D` behavior: local Scale and ApplyScale replace near-zero components by positive `1e-5`; GlobalScale scales the existing global basis directions, including reflection, before parent inverse conversion and local clamping. Raw Transform assignment can retain a singular basis. MoveLocalX/Y normalize nonzero axes with representable squared length by default, retain its magnitude with `scaled=true`, and still commit Position and deliver enabled local notifications for a zero axis. Managed tests cover reflected/nonuniform parents, tiny and zero axes, invalid input and callback delivery on Linux/.NET 8; the coordinate and angle slices complete its own declared member rows.

The coordinate slice checks local and global position/transform values, translation and point conversion through direct canvas parents. `GlobalPosition` and `GlobalTranslate` inverse-transform only the requested point, leaving the local basis unchanged under skewed parents; `TopLevel` and neutral parents bypass that conversion. `GetRelativeTransformToParent` traverses the structural Entity chain even across `TopLevel`; ADR 0008 records its typed invalid-ancestor result. Attached reads enforce owner-thread access. Nine mapped rows are Implemented on Linux/.NET 8; the angle slice closes the remaining own members and type row.

The final Entity angle slice follows the pinned local and global rotation/skew setters. A global setter modifies a temporary global basis, inverts through the direct canvas parent, then commits only the corresponding local angle; unrelated local scale, skew or rotation and position stay unchanged under nonuniform and reflected parents. `GetAngleTo` inverse-transforms and scale-compensates the target; `LookAt` adds that angle. Managed tests cover signed degrees, coincident target, invalid/singular rollback, owner-thread precedence, packed spatial state and zero warmed detached managed allocations. All 23 own Node2D members and the type row are Implemented on Linux/.NET 8. Inherited canvas/scene gaps and native or other-platform acceptance remain separate.

The focused Linux/Wayland scene-hierarchy pixel check passed on compatibility and GPU backends during this audit. It exercises parent, neutral and direct canvas placement; angular setter pixels, native ABI, other platforms and visual owner acceptance remain unverified.

RenderingServer traverses all Nodes and records only CanvasItems. NotificationDraw/Draw/OnDraw recording, QueueRedraw and rectangle/line/polyline/dash/arc/circle/ellipse/polygon/primitive/texture methods belong to CanvasItem; Texture draw methods accept that base. Retained resource notifications atomically schedule owner-thread recording. Commands borrow Texture and Material; renderer backends own native caches.

PackedScene captures any Node root. Each inheritance layer contributes only its own stored descriptors. Neutral and spatial nodes have separate default factories; derived types still supply an explicit static exact-type factory. Reconstruction is detached, shared resources remain borrowed, and the root owns scene-local duplicates.

## Dependencies and invariants

- All types remain in Electron2D.dll. Node depends on Core object lifetime, descriptors, input values, SceneTree scheduling and the narrow scene-local Resource contract. CanvasItem adds math and retained graphics; Entity adds concrete spatial state.
- No transform, visibility, material or drawing declarations are added to Node or Timer.
- Attached mutation uses the scene owner thread. Packed capture/instantiation and disposal retain the existing barriers; invalid lifecycle mutation is rejected before state changes.
- Children have one parent and unique nonempty names. Parenting rejects cycles. Owner is null or a strict ancestor. Scene roots own descendant disposal; callback failures do not stop remaining cleanup stages.
- Geometry/transform inputs are finite; inverse-dependent operations reject singular transforms. ZIndex is bounded by -4096..4096.
- Window.Position is native desktop position; Window does not inherit canvas state. Nested SubViewport and embedded child Window ownership execute; independent native child windows retain their platform/backend prerequisite.

## Verification and limits

[SceneHierarchyTests](../../tests/Electron2D.Tests/SceneHierarchyTests.cs) exercises inheritance, API boundaries, mixed-tree transforms/visibility/Z, custom canvas placement, reparenting, timer/tween lifecycle, neutral packing and callback failure continuation. [NodeReplacementTests](../../tests/Electron2D.Tests/NodeReplacementTests.cs) covers replacement order, groups, owner references, local-resource lifetime, invalid inputs and callback failure. [RemoteTransformTests](../../tests/Electron2D.Tests/RemoteTransformTests.cs) covers transfer policy, path-cache lifetime, invalid targets, cycles, packing, callback failure and owner-thread rejection in managed execution. The existing [runtime checks](../../tests/Electron2D.Tests/Program.cs) cover inherited behavior and run without native dependencies. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) verifies pixels on both GPU and compatibility backends under Linux Wayland and dummy/software. The full Wayland rendering runtime suite in this run passed, including later scene-hierarchy stages. [CanvasOrderingTests](../../tests/Electron2D.Tests/CanvasOrderingTests.cs) adds 31 cases for behind-parent, nested Y, canvas-root ordering and drawing-callback mutation on both native backends and dummy/software. Native tests use programmatic inputs/readback and do not establish physical-input, visual owner or other-platform acceptance.

This hierarchy preserves separate responsibilities; it does not finish every reference member. Full per-member behavior across GUI layout/theme/input, drawing/canvas, collision physics and packed/inherited authoring remains tracked in [coverage](../coverage/index.md). Nested offscreen viewports and embedded windows execute; independent native child windows retain their distinct prerequisite. No compatibility aliases or inert placeholders stand in for missing behavior.

## Decisions

- [0008: Scene inheritance and API correspondence](../decisions/scene.md#adr-0008)
- [0006 and 0011: Tree scheduling and failure cleanup](../decisions/scene.md#adr-0006)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
- [0028: Rendering](../decisions/rendering.md#adr-0028)
- [0031: Scene composition](../decisions/scene.md#adr-0031)

Pixel-snapping integration is described by [the canvas component](canvas-rendering.md#pixel-snapping). Viewport owns independent transform/vertex policies; rendering preserves logical node transforms, while Sprite local queries honor attached transform snapping. Project defaults initialize the explicit root Window at construction.

Node.GetConfigurationWarnings and UpdateConfigurationWarnings provide typed diagnostics. SceneTree.EditedSceneRoot limits warning-change events to the selected live subtree, and DebugPathsHint drives optional path drawing. Selection is borrowed and cleared on exit; diagnostics do not create an editor. Contracts and executable evidence: [Scene paths diagnostics](scene-paths.md#configuration-diagnostics-and-path-drawing).

## Transform invalidation and delivery

CanvasItem.GetGlobalTransform caches the direct-parent composition; local edits invalidate the affected canvas chain, stopping at neutral and TopLevel boundaries. Queueing occurs only on a valid-to-invalid transition with NotifyTransformChanges enabled. Initial tree entry queues once regardless of that flag. Reading global state resolves invalidation without consuming the queue entry. ForceUpdateTransform consumes only the receiver's pending entry, before callback delivery, without recomputing coordinates, flushing children or requesting redraw. Local notifications are synchronous, attached-only and controlled by NotifyLocalTransformChanges; invalidation precedes them. Entity equal transform assignments are not suppressed. The two typed events project the corresponding numeric notifications through the base handler, including explicit manual notifications.

Audit sources: Godot 4.7.2-stable at `ed1daf0bf001b61586d9930840f2f1394092c079`, [canvas_item.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/canvas_item.cpp), [canvas_item.h](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/canvas_item.h), [node_2d.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/node_2d.cpp), and [scene_tree.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/scene_tree.cpp). The executable force method removes and notifies one queued item; the broader XML wording does not imply recursive forced flushing. Disabling notification policy does not cancel pending delivery, and force does not clear global invalidation.

Each delivery pass preserves the native pending-list order: capture the next entry before dispatch; an append behind an existing successor may run in this pass, while an append from the current tail waits for another pass. Cancellation advances the saved cursor before unlinking an entry, preventing removal or explicit forcing of the next item from stranding later callbacks. User callbacks can explicitly force a newly queued update; global invalidation still coalesces ordinary repeated writes. Callback exceptions retain consumed state and allow later pending items under ADR 0011. Mathematical values are current before delivery; there is no renderer delay for ordinary Entity geometry.

[CanvasTransformNotificationTests](../../tests/Electron2D.Tests/CanvasTransformNotificationTests.cs) covers policy defaults, entry/exit/rollback, equal assignments, cache invalidation and custom placement, force/coalescing, policy toggles, manual typed events, hierarchy boundaries, captures/threads/disposal, callback errors and reentry, phase ordering, timers/deferred/deletion and zero warm allocation. CameraTests, PathTests and SceneHierarchyTests consume the corrected event timing. [Native camera checks](../../tests/Electron2D.Tests/CanvasTransformNotificationRenderingTests.cs) distinguish a post-flush queued camera movement from an explicitly forced update in the same submission and verify retained drawing.

Managed tests and the focused native command `ELECTRON2D_TEST_RENDER=1 ELECTRON2D_TEST_TRANSFORM_NOTIFICATIONS=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` passed on GPU/default, GPU/HLSL, GPU/GLSL and compatibility. The same focus with `dummy` passed software compatibility. Other native platforms, physical input and owner visual acceptance are not claimed. The earlier transform-notification run did not test interpolation; the dedicated managed and native interpolation checks above cover it. Independent viewport rendering remains a separate gap.

Final self-contained linux-x64 verification: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-transform-publish`, then from that directory `env -u LD_LIBRARY_PATH PATH=/usr/bin:/bin ELECTRON2D_TEST_RENDER=1 SDL_VIDEODRIVER=wayland /tmp/electron2d-transform-publish/Electron2D.Tests`. The complete renderer suite, including forced/queued camera pixels and HLSL/GLSL, passed. The same executable with `ELECTRON2D_TEST_TRANSFORM_NOTIFICATIONS=1 SDL_VIDEODRIVER=dummy` passed software compatibility. Execution uses the packaged runtime and shader artifacts. Repeated SDL/GTK initialization still emits its existing nonfatal locale warning.

## Canvas render time

[Canvas animation intervals](canvas-rendering.md#animation-intervals-and-rectangles) use captured scaled process steps and a live typed rollover setting; ordered transform state is replayed alongside retained geometry. The clock is per Engine.Run and also supplies the optional GPU fragment [TIME built-in](shader-materials.md#render-time).

## Reused captured action batches

SceneTree.Defer and deferred group operations enqueue under the existing lifetime/work lock. Two action queues swap roles when a nonempty batch is captured; the owner drains the captured queue outside the lock and recycles its capacity. Work enqueued by a callback or another thread after capture waits for another flush. Later capacity growth and user callbacks can allocate. [BoxContainerTests](../../tests/Electron2D.Tests/BoxContainerTests.cs) verifies next-batch/cross-thread semantics and zero bytes over 64 prepared batch cycles, alongside the existing concurrent disposal and lifetime tests. Queued deletions retain their separate ownership mechanism.

[Embedded viewport containers and GUI](canvas-rendering.md#embedded-viewport-containers-and-gui) now execute native SubViewportContainer composition, stretch/shrink, independent GUI state and connected input/drag routing. Public input reentry remains rejected; native subwindows, multiview, editor/file workflows and other-platform acceptance remain separate.

[Typed scene multiplayer](scene-multiplayer.md) integrates default/borrowed branch interfaces before node entry, idle-frame polling before node callbacks, Node authority/configured RPC, structural path invalidation before observers and detach/default cleanup after node exit. Programmatic native WS/WSS tests execute client/server branches within one tree; this does not establish replication or editor/rendered/agent acceptance.

[Typed scene spawning and replication](scene-replication.md) now executes custom/automatic in-memory scene factories, pre-Ready state, late admission, visibility/RPC integration, property policies and real periodic/delta batching through SceneMultiplayer. Public branch replacement rebinds typed configurations after committed cleanup, including callback errors. File scene authoring and other acceptance gates remain separate.

RESET save preparation now executes through AnimationMixer.ResetOnSave and PackedScene.PackForSave. Enabled RESET value/Bézier tracks run at zero on detached nodes and isolated copied resources, capturing coupled effects while preserving source preview values/clocks/queues. Ordinary Pack captures the actual pose. Preparation failures preserve prior packed data; temporary resources are cleaned or transferred to the prepared scene. Compiled factories/setters/postprocessing execute, while lifecycle, local-resource setup and method/audio/nested event tracks are suppressed. Custom authoring state must be stored and resource duplication supported. AnimationResetSaveTests exercises policies, all controller families, coupled values/resources, errors, lifetime and portable saved scenes.

The internal resource/subtree correspondence was inspected against pinned [Node resource traversal and propagation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/node.cpp) and [node path grammar](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/string/node_path.cpp). Typed tuples/delegates/descriptors preserve the applicable result, order and property boundaries under ADRs 0001/0004/0005/0008/0011.


## Owned name and type searches

Node.FindChild and FindChildren now expose recursive=true and owned=true on both
ordinary and typed overloads. Search includes internal front/back children in
depth-first tree order, starts below the receiver, and prunes an entire branch
whose first node has no Owner when ownership filtering is enabled. A nonnull
Owner may be any valid ancestor; it need not equal the search receiver. Set
owned=false to include runtime-created branches. Matching is against names only,
is case-sensitive, and supports * plus ? for one Unicode character except a period.
FindParent applies the same matching to nearest-first ancestors, excluding self.

Typed FindChildren<TNode> replaces dynamic type-name filtering with assignability,
including compiled application subclasses, and permits an empty name pattern for
type-only selection. An empty ordinary all-results or single-result pattern is an
argument error; an empty parent pattern matches nothing. Null arguments reject.
FindChildren returns a fresh read-only collection of borrowed node identities;
later hierarchy/name changes leave the snapshot intact. Collection creation is
outside the zero-allocation contract of repeated first-result/parent reads.
Attached searches require the scene owner or the receiver's current processing
group. Worker-side recursive traversal rejects a foreign group boundary; parent name matching
starts from the guarded receiver. Disposed receivers reject.

The public-only NodeSearch consumer checks ownership pruning and live edits,
internal order, recursion, current worker-group searches and foreign-group
rejection, physical sibling/type identities, Unicode/case/period
patterns, snapshots and invalid thread/lifetime calls. Real physics callbacks use
the inherited methods, with actual body travel and 128 warm/128 measured complete
search/physics frames at strict zero owner/all-thread managed bytes. Current
executed profiles and exact evidence are retained under
bin/physics-node-search/2026-10-15. These queries do not close the remaining
inherited/extension or hardware/window/performance gates.


On 2026-10-15 the final source passes six managed and six strict NativeAOT
CPU/software-GPU/registered serial/separate profiles. Each executes the real
processing worker, cold foreign-group rejection and 128 warm/128 measured complete
physical/search frames at 0/0 owner/all-thread managed bytes. Process maps confirm
CoreCLR only for managed hosts, explicit lavapipe only for GPU profiles, no NVIDIA
and numerical solver threads only with the separate policy. The native runs use
one unchanged 125-file publish without an overlay or LD_LIBRARY_PATH.

The complete test host also passes with source-built native prerequisites. The
older bootstrap native package lacked its mesh library; that rejected launch is
retained. A focused seven-family resource cohort and the full run distinguish
borrowed non-local resources from explicitly local copies in AnimatedSprite,
StyleBoxTexture and Theme, preserving aliases and scene retirement in both cases.
These correct obsolete fixture expectations; the resource runtime policy stays
unchanged. Three audited inherited reference rows now become Implemented, with
four updated owned-parameter signatures and no increase in declaration count.
API/wiki checks pass at 854 types, 14247 declarations and 872 wiki files.
Exact commands, failed candidates, process proofs and source/binary manifests:
bin/physics-node-search/2026-10-15/validation.json. Full inherited/extension,
foreign/native-memory and hardware/window/performance acceptance remain open.

[Active exclusive windows](popup-windows.md#active-exclusive-window-chains) add
`Node.GetLastExclusiveWindow()` across physical and ordinary Node roles. The
query is active-tree scoped and shares the active containing Window used by
GetWindow.

## Active viewport ownership

Node retains one active viewport reference, published before membership and enter
callbacks and cleared before TreeExited, including callback-error cleanup.
Viewport nodes select themselves; ordinary descendants use their active parent's
association. Detached nodes, including viewports/windows, return null. Reparenting
and cross-tree transfer recompute the association; manual notifications and saved
scene data do not write it.

GetViewport reads this cached borrowed reference without walking the hierarchy or
requiring the node's processing group. Access to the returned object still follows
that object's guards. GetWindow checks the queried node's owner/current-group
scope and follows active viewport parents to the nearest Window. A subgroup may
read its own node's containing window even when that Window belongs to another
group; unrelated thread/group calls reject. GetLastExclusiveWindow and popup
parenting consume the same active association.

The public [viewport ownership consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ViewportOwnership.cs)
exercises physical and ordinary Node roles, nested offscreen/window boundaries,
entry/exit/reparent, callback failure, manual notifications, cross-tree transfer,
weak retirement, worker reads and fresh-process PackedScene reconstruction.
Its warm contract measures actual complete physical frames and both owner/all-thread
managed bytes. Hardware/native allocator, foreign-platform and broad inherited
acceptance remain separate gates.

Current Linux x64 evidence is `bin/physics-viewport-ownership/2026-10-16/validation.json`:
twelve CoreCLR/strict NativeAOT CPU/software-GPU/registered profiles, each with
serial or separate physics scheduling and its own fresh scene-load process.
Each parent measures 128 full physical frames after 128 warmup frames, requiring
zero owner/all-thread managed bytes, unchanged live RID/world, 128 integration
callbacks and 12.8 units of motion within 0.01 for accumulated single-precision
integration. CPU/registered processes run with an unavailable Vulkan ICD; GPU
parents and fresh children pin lavapipe. Existing hierarchy/replacement, viewport,
container, layout, drag and popup regressions also pass. Actual offscreen popup
pixels and 64 warmed placement/render frames pass on GPU/lavapipe and SDL software;
these renderer checks establish no compositor, physical-input or RTX performance
acceptance.

## Entry, readiness and departure

Entry runs parent-first: enter notification, OnEnterTree, TreeEntered, tree NodeAdded, parent ChildEnteredTree, then descendants. OnEnterTree is a managed tree-transition hook; a manual enter notification does not invoke it. Existing class-specific setup in typed overrides still follows ordinary C# base calls.

After the hierarchy enters, readiness visits children first. Every attachment receives PostEnterTree after descendant ready delivery and before its own optional ready notification/OnReady/Ready. Readiness is consumed before the hook. Reentry keeps the one-shot state; RequestReady rearms only that node, including requests from its own ready hook. Children added during entry wait for the parent's ready pass. Children added during ready can complete immediately. Attachment-qualified snapshots skip a removed/reentered sibling's old entry. IsNodeReady and RequestReady permit the owner/current processing group; mutation rejects capture and cannot wake a physical body. Detached/disposed IsNodeReady retains its stored value.

Departure visits siblings in reverse order, children before parents. OnExitTree and TreeExiting precede the exit notification and engine membership/physical release. NodeRemoved then ChildExitingTree still observe the old Tree association. The complete subtree clears those associations before TreeExited runs in a second reverse child-first pass. Ordinary removal also clears its root Parent and invalid owners first. Pending exit guards prevent activation, hierarchy escape and disposal during both passes, including intermediate structural callbacks. Manual exit notification does not invoke OnExitTree. Callback failures aggregate after remaining transitions/cleanup; constructor failure additionally restores consumed ready state.

The public-only --node-lifecycle consumer checks exact neutral/spatial/RigidBody ordering with internal children, reentry, self requests, insertion and stale snapshots, 11 activation-failure stages, six simultaneous exit errors, live body/Area/joint exit bindings, lifecycle barriers, actual worker ready metadata, fresh file activation and 128 warmed/measured moving frames with strict zero owner/all-thread bytes. CPU/software-Vulkan/registered serial/separate and CoreCLR/NativeAOT profiles are recorded in bin/physics-node-lifecycle/2026-10-16/validation.json. Native/driver allocation, hardware/window/performance, foreign platforms and full inherited physics remain separate gates.

The managed CanvasLifecycleTests fixture separately verifies numeric manual enter/exit delivery without typed tree hooks or canvas membership changes; run it with ELECTRON2D_TEST_CANVAS_LIFECYCLE=1. The original full regression rejection and the corrected complete rerun are retained with the lifecycle receipt.
