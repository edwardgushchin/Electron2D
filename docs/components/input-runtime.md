# Input runtime component

Last updated: 2026-09-21

## Scope

This component converts caller-supplied typed events into raw device state, named action state, per-process/per-physics transitions, and synchronous scene callbacks. It owns no native device or pump.

## Owned types

`Input`, `InputMap`, `InputEvent`, `InputEventAction`, `InputEventFromWindow`, `InputEventWithModifiers`, `InputEventKey`, `InputEventMouse`, `InputEventMouseButton`, `InputEventMouseMotion`, `InputEventJoypadButton`, `InputEventJoypadMotion`, `InputEventScreenTouch`, `InputEventScreenDrag`, `InputEventGesture`, `InputEventMagnifyGesture`, `InputEventPanGesture`, `Key`, `KeyModifierMask`, `KeyLocation`, `MouseButton`, `MouseButtonMask`, `JoyAxis`, and `JoyButton`.

## Runtime flow

1. A host constructs a live `InputEvent` and calls `Input.ParseInputEvent`.
2. An active MainLoop validates owner-thread and idle-running eligibility before mutation.
3. InputMap validates all relevant live bindings and collects matches under its configuration lock.
4. Input commits raw state and per-binding action contributions under its state lock.
5. If an Engine loop is active, the event is synchronously forwarded to it.
6. SceneTree captures nodes, walks the snapshot in reverse depth-first order through input, unhandled-key, and unhandled stages, and revalidates membership before every callback.
7. Each process/physics frame clears only its own just-transition lane in `finally`.

## Dependencies and interactions

The component uses `Resource` for event duplication/change reporting, `PropertyDescriptor` for inherited stored event fields, `ElectronObject` for singleton lifecycle/property discovery, `Vector2`/`Transform`/`Mathf` for value operations, Engine/MainLoop for host integration, and Node/SceneTree for dispatch. There is no native backend dependency.

## Invariants

- No partially applied action mapping: loop eligibility and matching finish before raw/action mutation.
- No parse re-entry; state remains committed if later user callbacks fail.
- Multiple bindings contribute independently; action strength is the maximum live contribution.
- Editing or copying a registered binding synchronously invalidates cached contributions for every action using that reference before fallible public change observers run.
- Disposing a registered binding outside matching removes it from every affected action and clears its contributions before public disposal observers run.
- Exact matching filters extra modifiers and analog direction; releases remove their mapped source.
- Synthetic action descriptions skip synthetic bindings during lookup and fall back to the action name, so cyclic description lookup cannot recurse.
- Node ordering and handled semantics are deterministic; callback exceptions are aggregated.
- Event resources remain caller-owned. Input neither disposes nor stores submitted event objects after transition identity expires.
- Every event value has a validated stored typed property descriptor, and constructor-specific device defaults are the corresponding revert values.
- Warmed matching and traversal reuse bounded collections.

## Current implementation status and exclusions

Keyboard, mouse buttons/motion, touch/drag, magnify/pan gestures, controller buttons/axes, direct action events, action maps, raw key/mouse/controller and action queries, vector composition, transition latches, release-all, and Node propagation are implemented.

Hardware and absent-domain coverage is not stubbed. The exact prerequisite, unblock criterion, and required implementation slice for cursor/window functions, event accumulation/emulation, controller discovery/effects, sensors, MIDI, shortcuts, persistence, and GUI routing are normative in [ADR 0038](../decisions/input.md#deferred-coverage-and-exact-implementation-triggers).

## Verification

`tests/Electron2D.Tests/Program.cs` covers validation, complete stored event-property discovery/defaults, duplicate bindings, the 32-source ceiling, modifier exactness, pre-mutation owner/execution rejection, state-before-callback ordering, reverse stage order, handled input, independent process/physics transitions, callback failure aggregation, parse re-entry, failure-safe binding invalidation, raw-strength vector composition, direct actions, mouse/controller state, atomic event accumulation, event transforms including unchanged pan delta, release-all, permanent singleton registration, and zero warmed managed allocation.
