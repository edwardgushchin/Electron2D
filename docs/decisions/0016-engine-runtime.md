# ADR 0016: Process-wide Engine runtime and host-driven scheduling

Last updated: 2026-09-21

## Status

Accepted. Extends ADR 0015 after the explicit implementation request for `Engine`; it does not move SDL ownership into Core.

## Context

`MainLoop` and `SceneTree` already provided complete owner-thread lifecycle and callback lanes, but the caller had to choose all fixed-step cadence, time scaling, catch-up, metrics, and global service lookup policy. The current reference `Engine` API centralizes those settings and observations, while its native main iteration and timer synchronizer turn real elapsed time into fixed and process callbacks.

Electron2D still has no SDL host, renderer, logger, script runtime, movie writer, or editor. A production implementation therefore needs deterministic scheduling over supplied elapsed time without fabricating platform behavior or adding inert compatibility flags.

## Decision

`Engine` is a sealed `ElectronObject` singleton with process lifetime. Disposal is rejected rather than leaving an unrecoverable disposed global. It attaches one `MainLoop`, initializes a created loop, drives frames, finalizes on stop, and never takes final disposal ownership.

The host supplies finite non-negative unscaled elapsed seconds. Engine uses a bounded, allocation-free fixed-step synchronizer with twelve frames of distribution history and configurable jitter tolerance. It caps long-stall catch-up, preserves the residual interpolation fraction, applies non-negative `TimeScale` to callback deltas rather than cadence, invokes fixed callbacks before process, and returns combined stop requests. No background thread, clock, wait, renderer, input pump, audio mixer, or collision simulation is introduced.

Runtime lifecycle is one atomic owner-thread state machine. Callback exceptions restore the running state; initialization and finalization failures detach the terminal loop. Process/physics counters are process-lifetime totals and define exactly whether attempted or completed callbacks count, while FPS/interpolation state is re-baselined for each successfully attached loop. Tick-frequency changes re-baseline timing rather than mixing histories based on different step sizes.

The reference singleton registry is adapted to ordinal string names and `ElectronObject`, with a generic typed lookup. It starts with a permanent self-registration named `Engine`; ADR 0019 adds the permanent `ProjectSettings` entry and project-backed timing values. The registry is thread-safe and treats user entries as explicitly non-owning. Untyped dictionaries are not introduced: assembly version data uses immutable `EngineVersionInfo`.

APIs that cannot act without an absent domain are not exposed as stored-but-unused state. Maximum-FPS waiting is deferred to the SDL host; draw counts to rendering; output flags to logging; author/license maps to generated distribution metadata; script backtraces/languages to scripting; movie paths to capture; and editor hints to an editor runtime.

## Consequences

- Applications have one consistent timing and `MainLoop` coordination point while retaining a host-controlled clock and thread.
- A `SceneTree` can be attached after its constructor has already initialized it; a custom created loop is initialized by `Engine.Start()`.
- Long host stalls cannot cause unlimited fixed callbacks in one frame, at the cost of deliberately slowing/dropping excess simulated time.
- Configuration and metrics can be inspected cross-thread, while callbacks remain owner-thread only.
- SDL integration can measure time, pump events, honor future maximum-FPS policy, call `AdvanceFrame()`, and stop on its result without changing Core scheduling semantics.
- Reference APIs tied to missing domains remain visible in documentation as deferred work rather than misleading executable surface.

## Rejected alternatives

- A background engine thread and internal stopwatch: the future SDL host must own platform pumping and wait/presentation policy.
- Leaving fixed-step scheduling entirely to every host: it duplicates timing, interpolation, catch-up, and metric semantics.
- A naive accumulator that ignores jitter tolerance: it leaves a documented timing property inert and distributes fixed steps poorly near cadence boundaries.
- Disposing a loop from `Stop()`: finalization ownership belongs to Engine, but final object/native-handle ownership remains with the creator.
- Allowing disposal and recreating Engine: static references and registered services would observe split global identity.
- Adding constant-return editor/draw/movie/script methods: they would be compatibility stubs, forbidden by the repository definition of done.

## Verification boundary

Managed checks use deterministic deltas to cover state transitions, ordering, failure recovery, stop behavior, catch-up bounds, interpolation, counters, FPS, registry concurrency, assembly metadata, thread affinity, and warmed zero-allocation frames. They do not establish real display cadence, SDL behavior, rendering, hard real-time guarantees, or owner acceptance.

## References

- [Godot Engine stable documentation](https://docs.godotengine.org/en/stable/classes/class_engine.html)
- [Godot Engine implementation](https://github.com/godotengine/godot/blob/master/core/config/engine.cpp)
- [Godot main timer synchronizer](https://github.com/godotengine/godot/blob/master/main/main_timer_sync.cpp)
- [Godot main iteration](https://github.com/godotengine/godot/blob/master/main/main.cpp)
