# ADR 0015: Main-loop lifecycle and host boundary

Last updated: 2026-09-21

## Status

Accepted. Extended by [ADR 0016](0016-engine-runtime.md), which now supplies the explicitly requested process-wide scheduler without changing MainLoop's own lifecycle contract.

## Context

Electron2D had a complete caller-driven `SceneTree` frame pipeline but no base application-loop contract. The reference API defines an `Object`-derived `MainLoop` with initialization, process, physics-process, finalization, a permission-result signal, and twelve operating-system notification identifiers. Electron2D also needs an explicit boundary that a future SDL3-CS host can drive without embedding platform code in Core or Scene.

The existing `SceneTree` constructor activates its root immediately, and disposal owns hierarchy teardown. Any base-loop integration therefore must preserve those established behaviors, prevent lifecycle re-entry during construction and callbacks, and keep frame hot paths free of steady-state managed allocation.

## Decision

`MainLoop` is an abstract Core type derived from `ElectronObject`. It exposes explicit typed host entry points `Initialize()`, `Process(double)`, `PhysicsProcess(double)`, and `FinalizeLoop()`, with protected `On...` hooks corresponding to the reference virtual callbacks. `Process` and `PhysicsProcess` return their callback result unchanged as the host-stop request.

The lifecycle is a single owner-thread state machine. Initialization and finalization are one-shot and non-reentrant. A frame exception restores running state; initialization or finalization failure is terminal. `Dispose()` automatically finalizes only a successfully initialized running loop. A failed `OnInitialize()` owns rollback of its partial acquisition because no matched initialization exists for `OnFinalize()`.

The permission signal is a synchronous typed C# event with a protected publisher. The twelve system notification identifiers retain their reference values. Native event generation and permission requests are not implemented until the SDL application host exists.

`SceneTree` derives from `MainLoop`, completes base initialization as part of successful construction, maps the two loop hooks to its established frame lanes, and returns `false` because application-quit policy remains a host concern. Explicit finalization and disposal both close work acceptance, exit and dispose the hierarchy, dispose timers, and clear subscribers. System notifications are propagated depth-first to live attached nodes; platform-specific input-state effects remain deferred to the future Input domain.

The `SceneTree` frame scheduler and timer phase reuse owned lists after warm-up, and empty deferred queues are not swapped, eliminating the observed idle-frame allocations required by ADR 0014.

## Consequences

- Custom application loops can implement the four protected hooks without inheriting scene behavior.
- `SceneTree` can be driven through either `MainLoop.Process`/`PhysicsProcess` or its existing void frame wrappers.
- Explicit `FinalizeLoop()` releases `SceneTree` ownership but does not itself dispose the base object; `Dispose()` remains required for complete logical lifetime termination.
- Callback and teardown failures have deterministic state outcomes instead of allowing lifecycle retries.
- The future SDL host has one integration boundary for timing, stop requests, system notifications, and permission results.
- Core remains independent of SDL and Scene; Scene depends on Core.

## Rejected alternatives

- Keeping `SceneTree` directly derived from `ElectronObject`: it leaves no reusable application-loop contract and duplicates the future host boundary.
- Hiding all lifecycle entry points as assembly-internal: external hosts and custom loops could not drive the public engine library without a second wrapper API.
- Calling virtual initialization from the `MainLoop` constructor: derived state would be uninitialized and exception rollback unsafe.
- Retrying initialization or finalization after a callback exception: user state may already be partially mutated, so retry is not generally safe.
- Adding an Engine singleton, run thread, clock, exit-code service, or SDL wrapper in this change: at that point Engine had not been requested and scheduling policy required its own decision. ADR 0016 later adds the requested singleton and host-driven scheduler, but still rejects a hidden thread, clock, and SDL wrapper.
- Allocating fresh LINQ snapshots on every scene frame: it contradicts the accepted hot-path allocation requirement.

## Verification boundary

Managed executable checks cover state transitions, ordering preconditions, callback failures, owner-thread enforcement, permission delivery, constants, SceneTree integration, notification propagation, teardown, and warmed idle-frame allocation. They do not prove SDL event translation, real frame cadence, permission behavior on an operating system, hard real-time guarantees, or owner acceptance.

## References

- [Godot MainLoop stable documentation](https://docs.godotengine.org/en/stable/classes/class_mainloop.html)
- [Godot MainLoop implementation](https://github.com/godotengine/godot/blob/master/core/os/main_loop.cpp)
- [Godot SceneTree implementation](https://github.com/godotengine/godot/blob/master/scene/main/scene_tree.cpp)
