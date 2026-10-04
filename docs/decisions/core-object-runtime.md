# Electron2D core object and runtime decisions

Last updated: 2026-09-23

This bounded log owns the complete architectural records for core object and runtime. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0003](#adr-0003), [0005](#adr-0005), [0009](#adr-0009), [0010](#adr-0010), [0015](#adr-0015), [0016](#adr-0016).

<a id="adr-0003"></a>
## ADR 0003: Use deterministic IDisposable lifetime for engine objects

Last updated: 2026-09-20

- Status: Accepted; refined by [0009](core-object-runtime.md#adr-0009) and clarified by [0014](resources.md#adr-0014)
- Scope: `ElectronObject` and future resource-owning derived types

### Context

Godot objects use engine-managed validity and explicit `free()`. Electron2D runs on .NET, while future SDL windows, renderers, textures, audio devices, and other resources will own native handles that cannot wait for ordinary garbage collection.

### Decision

`ElectronObject` implements the standard extensible .NET dispose pattern:

- public non-virtual idempotent `Dispose()`;
- protected virtual `Dispose(bool disposing)` for derived cleanup;
- protected precondition validation before disposal begins, used by owner-thread-bound derived types;
- immediate disposed-state publication when disposal begins;
- a typed `Disposed` event after successful cleanup;
- `ThrowIfDisposed()` for live-object preconditions.

The base uses atomic state transitions so concurrent disposal runs cleanup at most once. It has no finalizer. Native handles must be owned by `SafeHandle`-derived wrappers, whose finalization protects against missed disposal without putting every engine object on the finalizer queue.

### Consequences

- Deterministic logical/native-resource ownership is explicit and compatible with `using`; managed object memory remains owned by the runtime.
- Repeated or concurrent `Dispose()` calls are safe.
- Operations can fail deterministically with `ObjectDisposedException`.
- Invalid disposal callers can be rejected without poisoning the object into a partially disposed state.
- Derived classes are responsible for releasing only resources they own and for calling the base override.
- If derived cleanup throws, cleanup is not retried and `Disposed` is not raised.

### Rejected alternatives

- Depend only on garbage collection: rejected because GC does not deterministically release native SDL resources.
- Add a finalizer to `ElectronObject`: rejected because the base owns no unmanaged handle and finalization overhead would apply to every engine object.
- Mirror Godot `free()` and invalid non-null references: rejected in favor of the standard C# lifetime contract.

<a id="adr-0005"></a>
## ADR 0005: Use numeric notifications and typed property descriptors

Last updated: 2026-09-20

- Status: Accepted
- Scope: Engine lifecycle notifications and tooling property exposure

### Context

Godot's `Object` exposes numeric notifications plus Variant-based property discovery, access, validation, and revert hooks. Electron2D needs recognizable lifecycle IDs and a future tooling boundary, but ADR 0001 rejects a universal `Variant` and string-addressed runtime mutation.

### Decision

- Engine lifecycle notifications keep stable Godot-compatible numeric IDs where a corresponding Electron2D lifecycle exists.
- `ElectronObject.Notify(int)` synchronously invokes the protected `OnNotification(int)` override.
- Pre-delete notification `1` is automatic. Post-initialization notification `0` is only reserved because a base constructor cannot safely call virtual members.
- Tooling properties use immutable `PropertyDescriptor<TOwner, TValue>` instances with typed delegates.
- Discovery returns heterogeneous descriptors through the non-generic `PropertyDescriptor` base, but value access remains generic and typed.
- Derived classes may filter or replace descriptors with `ValidateProperty`; they notify tooling of structural changes through a typed event.

### Consequences

- Lifecycle compatibility does not require a dynamic call or value system.
- Editor tooling can inspect property metadata while compile-time types govern reads and writes.
- Consumers cannot set a property by an arbitrary string name.
- Descriptor authors explicitly provide setters, validators, and revert factories; there is no reflection scanner.
- Notification callbacks are synchronous and exceptions propagate according to the caller's lifecycle contract.

### Rejected alternatives

- Recreate Godot property dictionaries and `Variant`: rejected by ADR 0001.
- Use reflection over every public property: rejected because editor exposure and mutability must be explicit.
- Invoke post-initialization from `ElectronObject` construction: rejected because virtual dispatch can observe an incompletely constructed derived object.

<a id="adr-0009"></a>
## ADR 0009: Permit teardown inspection on the disposing thread

Last updated: 2026-09-23

- Status: Accepted
- Scope: `ElectronObject.ThrowIfDisposed()` during deterministic teardown

### Context

ADR 0003 publishes `IsDisposed` as soon as disposal starts so concurrent callers cannot continue using an object. The same guard originally rejected the thread that had just won disposal. That made pre-delete notifications and derived teardown callbacks unable to read ordinary guarded state; an attached `Node` exit callback could fail merely by reading `Name` while direct disposal detached it.

### Decision

- `IsDisposed` remains `true` from the start of disposal.
- The atomic winner records its managed thread before invoking pre-delete and `Dispose(bool)`.
- `ThrowIfDisposed()` permits that exact thread while the state is `Disposing`.
- Every other thread is rejected during disposal, and every thread is rejected after the final `Disposed` state is published.
- The exception exists for teardown inspection and cleanup. It is not a promise that arbitrary re-entrant mutation during disposal is safe.

### Consequences

- Pre-delete, exit-tree, and derived cleanup callbacks can read the object's identity and guarded state needed to detach or release resources.
- Concurrent callers still observe disposal immediately through `IsDisposed` and guarded operations.
- Cleanup remains at-most-once and uses no new public API or dependency.

### Rejected alternatives

- Publish `IsDisposed` only after cleanup: rejected because concurrent callers could continue mutating an object being torn down.
- Make all getters bypass lifetime checks: rejected because it would weaken post-disposal failure behavior globally.
- Suppress node exit callbacks during direct disposal: rejected because direct and queued deletion should retain lifecycle delivery.

<a id="adr-0010"></a>
## ADR 0010: Add owned one-shot and deferred wrappers for typed events

Last updated: 2026-09-23

- Status: Accepted
- Scope: Typed event subscription lifecycle and delivery policy
- Supersedes: The statement in [0002](product.md#adr-0002) that deferred and one-shot delivery are not event features

### Context

ADR 0002 selected ordinary typed C# events and intentionally rejected a string-addressed signal registry. Native events provide synchronous multicast delivery and duplicate handler entries, but they do not provide a reusable ownership token, atomic one-shot consumption, cancellation of queued callbacks, or a standard way to route a handler through the scene safe point.

These capabilities are useful independently of scripting and serialization. They must preserve compile-time event signatures and must not introduce dynamic calls, reflection-based invocation, or untyped argument lists.

### Decision

- `EventConnection` wraps typed event add/remove accessors and implements `IDisposable`.
- Public overloads support the zero-, one-, and two-argument event shapes used by the engine.
- `oneShot: true` atomically consumes and disconnects the wrapper before invoking or scheduling the handler.
- An optional `Action<Action>` scheduler provides deferred delivery without making Core depend on Scene; callers normally pass `SceneTree.Defer`.
- Disposing a connection cancels deferred callbacks that have not begun, but does not wait for running callbacks.
- Engine events with payloads use sender-first typed signatures. Events carrying only their source use that source as their sole argument.
- Native duplicate delegate semantics are retained instead of adding a reference-count option.
- Persistent connections remain deferred until a typed schema can represent stable publisher, event, subscriber, and handler identities. ADR 0023's in-memory packed scenes intentionally omit runtime delegate subscriptions.

### Consequences

- Immediate, deferred, one-shot, and combined deferred one-shot delivery remain statically typed.
- One connection token owns one wrapper and gives subscribers an explicit lifecycle boundary.
- The scheduler determines delivery thread, safe point, ordering, and exception aggregation.
- Failed custom event removal can leave an inert wrapper in the publisher; the terminal token prevents further user-handler calls.
- Existing child-related `Node` events now pass the publishing parent first and the affected child second.
- There is still no `Connect`, `Disconnect`, event-name lookup, callable rebinding, or flags enum.

### Rejected alternatives

- Add a dynamic signal bus and connection flags: rejected because it duplicates C# events and conflicts with ADR 0001.
- Put deferred scheduling directly in Core: rejected because the scene safe point belongs to `SceneTree`.
- Depend on Reactive Extensions: rejected because the required lifecycle and scheduling policy is small and covered by the standard library.
- Serialize delegates or closures: rejected because they do not provide stable scene endpoint identities.

<a id="adr-0015"></a>
## ADR 0015: Main-loop lifecycle and host boundary

Last updated: 2026-09-21

### Status

Accepted. Extended by [ADR 0016](core-object-runtime.md#adr-0016), which supplies the process-wide scheduler, and [ADR 0038](input.md#adr-0038), which adds internal typed input dispatch and per-lane transition completion without changing MainLoop's public lifecycle contract.

### Context

Electron2D had a complete caller-driven `SceneTree` frame pipeline but no base application-loop contract. The reference API defines an `Object`-derived `MainLoop` with initialization, process, physics-process, finalization, a permission-result signal, and twelve operating-system notification identifiers. Electron2D also needs an explicit boundary that a future SDL3-CS host can drive without embedding platform code in Core or Scene.

The existing `SceneTree` constructor activates its root immediately, and disposal owns hierarchy teardown. Any base-loop integration therefore must preserve those established behaviors, prevent lifecycle re-entry during construction and callbacks, and keep frame hot paths free of steady-state managed allocation.

### Decision

`MainLoop` is an abstract Core type derived from `ElectronObject`. It exposes explicit typed host entry points `Initialize()`, `Process(double)`, `PhysicsProcess(double)`, and `FinalizeLoop()`, with protected `On...` hooks corresponding to the reference virtual callbacks. `Process` and `PhysicsProcess` return their callback result unchanged as the host-stop request.

The lifecycle is a single owner-thread state machine. Initialization and finalization are one-shot and non-reentrant. A frame exception restores running state; initialization or finalization failure is terminal. `Dispose()` automatically finalizes only a successfully initialized running loop. A failed `OnInitialize()` owns rollback of its partial acquisition because no matched initialization exists for `OnFinalize()`.

The permission signal is a synchronous typed C# event with a protected publisher. The twelve system notification identifiers retain their reference values. Native event generation and permission requests are not implemented until the SDL application host exists.

`SceneTree` derives from `MainLoop`, completes base initialization as part of successful construction, maps the two loop hooks to its established frame lanes, and returns `false` because application-quit policy remains a host concern. Explicit finalization and disposal both close work acceptance, exit and dispose the hierarchy, dispose timers, and clear subscribers. System notifications are propagated depth-first to live attached nodes. ADR 0038 later adds typed scene input propagation and exact SDL-host triggers for platform focus effects.

The `SceneTree` frame scheduler and timer phase reuse owned lists after warm-up, and empty deferred queues are not swapped, eliminating the observed idle-frame allocations required by ADR 0014.

### Consequences

- Custom application loops can implement the four protected hooks without inheriting scene behavior.
- `SceneTree` can be driven through either `MainLoop.Process`/`PhysicsProcess` or its existing void frame wrappers.
- Explicit `FinalizeLoop()` releases `SceneTree` ownership but does not itself dispose the base object; `Dispose()` remains required for complete logical lifetime termination.
- Callback and teardown failures have deterministic state outcomes instead of allowing lifecycle retries.
- The first executable SDL consumer uses this integration boundary for timing and stop requests; platform permission results remain separate work.
- Core remains independent of SDL and Scene; Scene depends on Core.

### Rejected alternatives

- Keeping `SceneTree` directly derived from `ElectronObject`: it leaves no reusable application-loop contract and duplicates the future host boundary.
- Hiding all lifecycle entry points as assembly-internal: external hosts and custom loops could not drive the public engine library without a second wrapper API.
- Calling virtual initialization from the `MainLoop` constructor: derived state would be uninitialized and exception rollback unsafe.
- Retrying initialization or finalization after a callback exception: user state may already be partially mutated, so retry is not generally safe.
- Adding an Engine singleton, run thread, clock, exit-code service, or SDL wrapper in this change: at that point Engine had not been requested and scheduling policy required its own decision. ADR 0016 later adds the singleton and scheduler, and now Engine.Run coordinates the windowed clock/pump on the calling main thread; no background game thread is created.
- Allocating fresh LINQ snapshots on every scene frame: it contradicts the accepted hot-path allocation requirement.

### Verification boundary

Managed executable checks cover state transitions, ordering preconditions, callback failures, owner-thread enforcement, permission delivery, constants, SceneTree integration, notification propagation, teardown, and warmed idle-frame allocation. They do not prove SDL event translation, real frame cadence, permission behavior on an operating system, hard real-time guarantees, or owner acceptance.

### References

- [Godot MainLoop stable documentation](https://docs.godotengine.org/en/stable/classes/class_mainloop.html)
- [Godot MainLoop implementation](https://github.com/godotengine/godot/blob/master/core/os/main_loop.cpp)
- [Godot SceneTree implementation](https://github.com/godotengine/godot/blob/master/scene/main/scene_tree.cpp)

<a id="adr-0016"></a>
## ADR 0016: Process-wide Engine runtime and host-driven scheduling

Last updated: 2026-09-22

### Status

Accepted. Extends ADR 0015 after the explicit implementation request for `Engine`; the windowed lifecycle is now coordinated by Engine.Run while native SDL ownership stays in DisplayServer through Window.

### Context

`MainLoop` and `SceneTree` already provided complete owner-thread lifecycle and callback lanes, but the caller had to choose all fixed-step cadence, time scaling, catch-up, metrics, and global service lookup policy. The current reference `Engine` API centralizes those settings and observations, while its native main iteration and timer synchronizer turn real elapsed time into fixed and process callbacks.

At this decision’s implementation point, Electron2D had no SDL host, renderer, logger, script runtime, movie writer, or editor. A production implementation therefore needs deterministic scheduling over supplied elapsed time without fabricating platform behavior or adding inert compatibility flags.

### Decision

`Engine` is a sealed `ElectronObject` singleton with process lifetime. Public operations delegate statically to that retained object under [ADR 0095](singleton-services.md#adr-0095); callers use `Engine.Run(window)` without a public `Instance` accessor. Disposal is rejected rather than leaving an unrecoverable disposed global. Its manual Start/AdvanceFrame/Stop path attaches one `MainLoop`, initializes a created loop, drives frames, and finalizes without taking final disposal ownership. Its normal Engine.Run(Window) path consumes the root, opens the native window through Window, creates and publishes SceneTree before ready, drives events and frames, and disposes the scene and native resources on exit or failure.

For manual embedding, the host supplies finite non-negative unscaled elapsed seconds; Run measures these with Stopwatch. Engine uses a bounded, allocation-free fixed-step synchronizer with twelve frames of distribution history and configurable jitter tolerance. It caps long-stall catch-up, preserves the residual interpolation fraction, applies non-negative `TimeScale` to callback deltas rather than cadence, invokes fixed callbacks before process, and returns combined stop requests. Run uses an owner-thread monotonic clock, native event pump and interruptible frame wait; no background game thread, renderer, audio mixer or collision simulation is introduced.

Runtime lifecycle is one atomic owner-thread state machine. Callback exceptions restore the running state; initialization and finalization failures detach the terminal loop. Process/physics counters are process-lifetime totals and define exactly whether attempted or completed callbacks count, while FPS/interpolation state is re-baselined for each successfully attached loop. Tick-frequency changes re-baseline timing rather than mixing histories based on different step sizes.

The reference singleton registry is adapted to ordinal string names and `ElectronObject`, with a generic typed lookup. It starts with a permanent self-registration named `Engine`; ADR 0019 adds the permanent `ProjectSettings` entry and project-backed timing values. The registry is thread-safe and treats user entries as explicitly non-owning. Untyped dictionaries are not introduced: assembly version data uses immutable `EngineVersionInfo`.

APIs that cannot act without an absent domain are not exposed as stored-but-unused state. MaxFPS now controls Run waiting (zero means unlimited), with no effect on manual AdvanceFrame; persistent project-backed maximum-FPS configuration remains unimplemented; draw counts to rendering; output flags to logging; author/license maps to generated distribution metadata; script backtraces/languages to scripting; movie paths to capture; and editor hints to an editor runtime.

### Consequences

- Applications have one timing/lifecycle entry point; manual embedding retains a host-controlled clock and thread.
- A `SceneTree` can be attached after its constructor has already initialized it; a custom created loop is initialized by `Engine.Start()`.
- Long host stalls cannot cause unlimited fixed callbacks in one frame, at the cost of deliberately slowing/dropping excess simulated time.
- Configuration and metrics can be inspected cross-thread, while callbacks remain owner-thread only.
- Engine.Run pumps native events before frames, applies MaxFPS in unscaled monotonic time, checks SceneTree quit during waits, and reserves the engine through cleanup. Direct lifecycle interference during Run is rejected.
- Reference APIs tied to missing domains remain visible in documentation as deferred work rather than misleading executable surface.

### Rejected alternatives

- A background game thread: native window and scene work remains on the calling main thread; Run uses Stopwatch there. Presentation awaits rendering.
- Leaving fixed-step scheduling entirely to every host: it duplicates timing, interpolation, catch-up, and metric semantics.
- A naive accumulator that ignores jitter tolerance: it leaves a documented timing property inert and distributes fixed steps poorly near cadence boundaries.
- Disposing a loop from `Stop()`: finalization ownership belongs to Engine, but final object/native-handle ownership remains with the creator.
- Allowing disposal and recreating Engine: static references and registered services would observe split global identity.
- Adding constant-return editor/draw/movie/script methods: they would be compatibility stubs, forbidden by the repository definition of done.

### Verification boundary

Managed checks use deterministic deltas to cover state transitions, ordering, failure recovery, stop behavior, catch-up bounds, interpolation, counters, FPS, registry concurrency, assembly metadata, thread affinity, and warmed zero-allocation frames. They do not establish real display cadence, SDL behavior, rendering, hard real-time guarantees, or owner acceptance.

### References

- [Godot Engine stable documentation](https://docs.godotengine.org/en/stable/classes/class_engine.html)
- [Godot Engine implementation](https://github.com/godotengine/godot/blob/master/core/config/engine.cpp)
- [Godot main timer synchronizer](https://github.com/godotengine/godot/blob/master/main/main_timer_sync.cpp)
- [Godot main iteration](https://github.com/godotengine/godot/blob/master/main/main.cpp)

<a id="adr-0050"></a>
## ADR 0050: Typed weak references to engine objects

Last updated: 2026-09-24

### Status

Accepted.

### Context

The reference weak-reference utility observes an engine object by identity without extending its lifetime and returns null after destruction. Electron2D uses managed memory and deterministic `Dispose`; a plain BCL weak reference can still return an object after its logical engine lifetime has ended.

### Decision

`WeakRef<T> : ElectronObject`, with `T : ElectronObject`, owns a standard `System.WeakReference<T>` and no target resource. Its constructor is the typed C# projection of the reference weakref factory and accepts null or an already disposed object as an empty reference. `GetRef()` returns the live typed target, or null after target disposal or collection. The returned reference is strong for the caller while held; the wrapper never disposes or retains its target. Disposing the wrapper follows ADR 0003 and rejects later calls.

Only engine objects cross this public weak boundary. Arbitrary CLR object and value types have their ordinary BCL facilities; no universal `Variant` weak container is introduced. Collection timing remains controlled by the .NET GC; deterministic disposal makes a target unavailable immediately even while a strong managed reference still exists.

### Consequences and verification limits

- The type is executable without a renderer, platform service, package, or separate assembly.
- A target can be disposed concurrently after `GetRef()` checks it; callers still validate the target before a later mutation.
- Managed tests cover live identity, null/disposed targets, wrapper disposal and GC collection. Native-host and other-platform acceptance remains separate.

### Related decisions

- [0001: Typed C# without Variant](product.md#adr-0001)
- [0003: Deterministic object lifetime](core-object-runtime.md#adr-0003)
- [0013: Resource ownership](resources.md#adr-0013)
