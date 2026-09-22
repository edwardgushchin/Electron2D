# Scene domain

Last updated: 2026-09-22

The current consolidation of hierarchy, canvas and spatial behavior in Node has not been approved by the user; [ADR 0008](../decisions/scene.md#adr-0008) records the unresolved inheritance decision. The API below describes existing code.

## Responsibility

Scene owns Electron2D's primary Node-based game-object model, reusable typed in-memory scenes, and the active [`MainLoop`](../classes/MainLoop.md) implementation that delivers lifecycle, frame, pause, deferred-work, and deletion phases. A game object, composed subsystem, or complete world is represented by a Node hierarchy; the same hierarchy can be packed and instantiated for reuse. It is a 2D-only runtime domain for Windows, macOS, Linux (X11/Wayland), Android, iOS, and Web and compiles into the single `Electron2D.dll` assembly.

Its production sources live under `src/Scene/Main/`, `src/Scene/2D/`, `src/Scene/Animation/`, and `src/Scene/Resources/`, matching their engine-module ownership without changing the flat public `Electron2D` namespace.

## Component inventory

| Component | Responsibility | State |
| --- | --- | --- |
| [Canvas rendering](../components/canvas-rendering.md) | Sprite texture/frame/region nodes and retained Node drawing | Executable; inherited canvas policies incomplete |
| [Window runtime](../components/window-runtime.md) | Native root window, presentation policies, platform events and client/input boundary | Implemented root slice; rendering and multiwindow incomplete |
| [Unified 2D node](../components/unified-node.md) | Hierarchy, 2D transforms, paths, groups, visibility/Z state, process/input policy, lifecycle endpoints, and deletion requests | Implemented and verified |
| [Scene tree](../components/scene-tree.md) | Active-root ownership, exception-safe lifecycle, pause state, frame/input dispatch, events/counts, reusable Node timers, lightweight one-shot timers, typed group operations, deferred work, and deletion execution | Implemented and verified |
| [Tweening](../components/tweening.md) | Typed property/method interpolation, sequencing, callbacks, waits, nested timelines, loops, and frame policies | Implemented and verified |
| [Packed scenes](../components/packed-scenes.md) | Typed in-memory owned-hierarchy capture, live metadata, detached reconstruction, and per-instance local resources | Implemented and verified |

Production types include [`Sprite`](../classes/Sprite.md), [`Window`](../classes/Window.md), [`Viewport`](../classes/Viewport.md), [`Node`](../classes/Node.md), [`NodeProcessMode`](../classes/NodeProcessMode.md), [`SceneTree`](../classes/SceneTree.md), [`Timer`](../classes/Timer.md), [`TimerProcessCallback`](../classes/TimerProcessCallback.md), [`SceneTreeTimer`](../classes/SceneTreeTimer.md), [`GroupCallFlags`](../classes/GroupCallFlags.md), [`Tween`](../classes/Tween.md), its four nested enum types, [`Tweener`](../classes/Tweener.md), its six concrete task types, [`PackedScene`](../classes/PackedScene.md), [`SceneState`](../classes/SceneState.md), and [`PackedSceneEditState`](../classes/PackedSceneEditState.md).

## Public surface

- `Node`: the primary public game-object base and one combined Godot-style `Node` + `Node2D` abstraction with ordered hierarchy, lifecycle, local/global `Transform` transforms, `Vector2` spatial helpers, retained canvas drawing, paths/search/groups, processing/input configuration, and queued deletion.
- `Sprite`: borrowed texture drawing, sheet frames, atlas regions, local bounds/opacity, change notifications and typed PackedScene state.
- `NodeProcessMode`: inherited, pausable, paused-only, always, and disabled process policies.
- `SceneTree`: concrete main loop and active hierarchy owner with failure-safe lifecycle/finalization, typed input/system-notification propagation, pause state, caller-driven process/physics frames, frame/tree events and counters, typed group work, timers, deferred actions, and deletion flushing.
- `Timer`: reusable hierarchy-owned countdown with selected frame lane, one-shot/repeat, autostart, local/tree pause, optional time-scale bypass, and typed timeout event.
- `TimerProcessCallback`: stable physics/process lane selection for `Timer`.
- `SceneTreeTimer`: lightweight one-shot delay advanced by one selected frame lane and automatically disposed after timeout.
- `GroupCallFlags`: immediate/reverse/deferred/unique policy for typed group operations.
- `Tween` and tweeners: typed SceneTree-driven sequential/parallel interpolation, callbacks, waits, nested timelines, looping, pause/lane/time-scale policy, and completion events.
- `PackedScene`: `Resource` that captures any reusable typed owned-node hierarchy, from one composed game object through a complete level, and reconstructs independent detached instances.
- `SceneState`: live read-only typed metadata view for current packed data.
- `PackedSceneEditState`: instantiation policy whose runtime `Disabled` value is implemented and whose editor values fail explicitly.

## Dependency direction

- Scene depends on Core's `Mathf`/`Vector2`/`Transform` math, Resources including `Resource`, and .NET collections and filesystem-name matching.
- Resources has a narrow reciprocal dependency on `Node` for `Resource.GetLocalScene()` under ADR 0023. This is an intentional in-assembly type cycle, not another managed assembly.
- Scene depends on the Input domain's typed event values and process-wide service boundary for propagation.
- Window now depends on the backend-neutral DisplayServer API for its native lifetime. Scene delegates drawing to the backend-neutral RenderingServer and has no direct SDL3-CS dependency, audio, collision physics, asset loading/saving, file serialization, scripting, networking, or Localization.
- Future gameplay, rendering, GUI input, and 2D physics types may depend on Scene.
- Scene must not introduce 3D types. Separation of non-spatial, canvas and spatial classes remains unresolved in ADR 0008; the current consolidation is not an approved constraint.
- Scene lifecycle and game-state semantics must not vary by target platform; native event generation remains a host boundary.

## Domain-wide invariants

- `Node` hierarchies are the primary public game-object and world model. Reusable objects and complete levels use the same `PackedScene` capture and instantiation boundary; Scene does not expose a competing entity hierarchy.
- A node has at most one parent and one active `SceneTree`; cycles and cross-tree insertion are rejected before mutation.
- An active root can be disposed only by its owning `SceneTree`.
- Sibling names are ordinal-unique, and path separators/reserved path tokens cannot be names.
- SceneTree-managed enter runs parent-first, ready runs child-first and once unless explicitly reset, and exit runs child-first. Lifecycle snapshots revalidate membership and lifecycle re-entry is rejected. Constructor failure terminally closes the failed tree, rolls membership and newly consumed ready state back, disposes activation-created timers, and invalidates activation-created tweens; later lifecycle failures complete their state transition and are aggregated. Manual `Notify(int)` dispatch is outside that state machine.
- Attached state mutation, lifecycle delivery, frame execution, flushing, and disposal use the tree's creating thread. Deferred and deletion requests may be enqueued from other threads.
- A non-top-level global transform is the ancestor global transform composed with the local transform. Transform inputs must be finite; operations needing an inverse reject singular transforms.
- Public and engine-internal process/physics callbacks are opt-in, synchronous, pause-aware, and ordered by their independent priority then captured tree order. Internal built-in work precedes the same node's public callback and receives both scaled and original Engine deltas.
- Input callbacks are opt-in, synchronous, pause-aware, reverse depth-first, ordered regular/key-unhandled/general-unhandled, membership-revalidated, failure-aggregating, and stoppable through current-dispatch handled state.
- Queue acceptance is atomic with tree-disposal closure. Deferred work queued during a flush waits for the next flush. Captured queued deletion runs after deferred actions, survives detachment, transfers safely between trees, and disposes the complete subtree despite detach callback failures.
- Frame and flush execution cannot be re-entered or started during lifecycle delivery. Reusable Timer nodes advance during internal node processing; lightweight tree timers advance after node callbacks and before deferred work. Pause delivery visits each eligible node at most once and rejects opposite re-entry.
- Tweens use one captured process/physics batch after lightweight timers. They are owner-thread mutable, become invalid after completion/killing/failure, attempt every parallel sibling on failure, and never create a second scheduler or background clock.
- Typed group operations run in hierarchy/reverse order, revalidate membership, and can be deferred and coalesced without reflection or untyped values.
- Typed node events pass their publisher first when an additional payload is present; Core event connections can schedule handlers through `SceneTree.Defer`.
- Packed-scene capture stores only root-owned branches, static exact-type factories, persistent groups, and explicitly storage-enabled typed properties. It stores no live source nodes or event subscribers.
- Packed-scene instances are reconstructed detached. Node factories and unfinished instances cannot activate a `SceneTree`; scene-local resource graphs preserve aliases/cycles, know their new root before setup, and are disposed with that root.
- Capture blocks source hierarchy mutation. Failed reconstruction attempts cleanup of every returned node and resource duplicate it acquired, reports cleanup failures, and never returns a partial result.
- `SceneTree` is initialized when construction succeeds, returns no quit request from its two inherited frame lanes, and releases all owned scene state from explicit finalization or disposal.
- System notifications are propagated depth-first to live attached nodes; native generation and platform-specific input effects belong to the absent SDL host/backend covered by ADR 0038.
- The warmed idle process and physics frame paths reuse scheduler/timer storage and do not allocate managed memory.

## Current limitations

- A caller may supply deltas directly through inherited `Process`/`PhysicsProcess` or wrappers. Core `Engine` can instead apply time scaling and fixed-step accumulation from host-supplied elapsed time. There is still no automatic SDL pump/clock, frame-wait policy, or background scene thread.
- Visibility and Z ordering govern the retained commands consumed by the canvas renderer.
- Root-window drawing and its input/client Viewport are integrated. There is no independent offscreen viewport, GUI input routing, collision/rigid-body physics, automatic scene switching, scene file loader/saver, RPC/multiplayer, accessibility backend, or scripting. Typed root-tree input propagation is implemented; Tweening is runtime-only and has no editor/serialization surface.
- Packed scenes are in-memory only. Nested/inherited scene authoring, placeholders, editable instances, persistent event endpoint storage, node-reference remapping, UID/import integration, and every editor edit mode remain absent.
- Paths are typed as `string`, not a separate `NodePath`; groups are strings; wildcard search covers names with `*` and `?`.
- A detached node may remember `QueueFree`, but deletion occurs only after attachment to a tree and a flush/frame boundary.
- There is no six-target host/package/test matrix; current executable verification is Linux-only.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies transform and hierarchy behavior, lifecycle order and failure rollback, cleanup continuation, inherited loop driving/finalization, typed input ordering/handled state/failures/re-entry/allocation, system-notification propagation, tree/frame events and counters, typed group operations, both timer models, typed tween sequencing/lifetime/failure behavior, paths/search/groups, visibility/Z, pause-aware internal/public process ordering, owner-thread enforcement, deferred batch isolation, concurrent enqueue/disposal stress, queued deletion, direct deterministic disposal, zero warmed idle/active-Timer/active-Tween/input allocations, packed owned-branch capture/state/instantiation, local resources, factory/capture rejection, and packed rollback. It does not prove renderer, SDL/native input, visual behavior, real-time cadence, disk scene compatibility, editor behavior, or large-scene performance.

## Relevant decisions

- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0004: 2D scene-oriented API in one Electron2D-owned assembly](../decisions/product.md#adr-0004)
- [0012: Vendored SDL3-CS and Box2D.NET](../decisions/product.md#adr-0012)
- [0005: Notifications and typed editor properties](../decisions/core-object-runtime.md#adr-0005)
- [0006: Scene-tree deferred work and queued deletion](../decisions/scene.md#adr-0006)
- [0008: Unresolved scene inheritance](../decisions/scene.md#adr-0008)
- [0010: Typed event connections](../decisions/core-object-runtime.md#adr-0010)
- [0011: SceneTree production contract](../decisions/scene.md#adr-0011)
- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
- [0023: Typed in-memory packed scenes](../decisions/scene.md#adr-0023)
- [0026: Separate Transform foundational type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform value and affine semantics](../decisions/core-math.md#adr-0029)
- [0031: Node trees and reusable scenes as the primary game-object model](../decisions/scene.md#adr-0031)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0037: Typed SceneTree tween scheduling](../decisions/scene.md#adr-0037)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)

## Windowed lifecycle

The [Window runtime component](../components/window-runtime.md) provides Window : Viewport : Node, root native ownership, presentation mode, four executable native policies, optional screen selection, client/decorated geometry, IME/taskbar requests, window events and scene input handling. Window.ModeEnum and Window.Flags describe the mode/policy identifiers. Capability failures stay explicit; declared policy IDs do not imply implemented native integration. Engine.Run consumes the configured window and children. The root canvas renders retained rectangles, lines, textures and GPU shader materials after scene processing. Offscreen viewports, nested windows, GUI and content scaling are still absent.
