# Input decisions

Last updated: 2026-09-21

This log owns durable decisions for input events, action mapping, process-wide input state, and scene input propagation. Current executable behavior is described by the [Input domain](../domains/input.md), [Input runtime component](../components/input-runtime.md), and class documents.

## ADR 0038: Typed input events, action state, and scene propagation

- Status: Accepted
- Date: 2026-09-21

### Context

Electron2D needs keyboard, mouse, touch, gesture, and controller input before a native SDL host exists. The stable Godot 4.7.2 Input/InputMap/InputEvent hierarchy is the coverage reference, but Electron2D has no Variant, Callable, viewport/GUI routing, native window system, controller manager, sensor service, MIDI product decision, or shortcut resource.

An earlier statement that a missing feature was merely "deferred" was not sufficient for context-free continuation. Every gap needs a concrete prerequisite and a rule that tells a later agent whether work is actionable.

### Decision

- `Input` and `InputMap` are non-disposable process-wide `ElectronObject` singletons registered permanently in `Engine`.
- Input events are typed mutable `Resource` subclasses. The implemented hierarchy is `InputEvent`; `InputEventAction`; `InputEventFromWindow`; `InputEventWithModifiers`; key, mouse button/motion, controller button/motion, screen touch/drag, and magnify/pan gesture types. Every event value is an inherited stored typed property descriptor whose revert default matches construction.
- Action names are ordinal strings. `InputMap` owns registration order and retains caller-owned live binding references. Configuration calls are lock-serialized; callers must not mutate or dispose a binding concurrently with matching. An all-device controller binding is a wildcard when it is the stored candidate, including exact binding lookup/deduplication.
- Controller-axis events remain motion values rather than button-like presses. Action matching applies each action's own deadzone, while `GetVector` composes raw strengths before applying its one circular deadzone.
- Action-map descriptions omit synthetic action indirection. A synthetic event description uses the first concrete binding or falls back to its action name, preventing recursive description lookup and duplicate alternatives.
- `Input.ParseInputEvent` serializes parsing, validates the active loop's owner thread and execution state, rejects re-entry and source-capacity overflow, resolves mappings before mutation, commits raw/action state before callbacks, and synchronously forwards the same caller-owned event to the active `MainLoop`.
- Registered binding changes use an internal invalidation channel before public `Resource.Changed` delivery, so a throwing user handler cannot preserve stale action contributions. Disposing a registered binding removes it from every affected action before public disposal observers. Motion accumulation commits all fields atomically before its single public change notification.
- Positional transforms require finite inputs. They transform local position and applicable local motion; pan gesture delta remains the host-reported value while only its position changes.
- `SceneTree` dispatches a captured hierarchy in reverse depth-first order through `OnInput`, then keyboard-only `OnUnhandledKeyInput`, then `OnUnhandledInput`. `SetInputAsHandled` stops the current and later stages. Removed/disposed nodes are skipped; callback failures are aggregated after eligible delivery continues.
- Node input participation is explicit and pause-aware through `InputEnabled`, `UnhandledKeyInputEnabled`, `UnhandledInputEnabled`, and `CanProcess()`.
- Process and physics just-pressed/just-released windows are independent. Each lane clears its own transition state in `MainLoop` `finally`, including failed callbacks. Warmed event matching and scene traversal reuse buffers and allocate no managed memory.
- Typed C# exceptions replace numeric error codes. Dynamic `Variant` event payloads, string-based calls, and untyped metadata are permanently excluded by ADR 0001.

### Deferred coverage and exact implementation triggers

The following items are **not actionable now** unless their trigger is present in the repository or the user explicitly authorizes the named prerequisite in the same task. They must be included in the prerequisite's first production-ready vertical slice unless the row says a later decision is also required.

| Deferred item | Exact missing dependency | Implementation trigger | When to implement |
| --- | --- | --- | --- |
| Native mouse mode, cursor shape/image, pointer warp, focus filtering, buffered accumulation, `FlushBufferedEvents`, and mouse/touch emulation policy | SDL window plus event-pump ownership and event-source deduplication | The first accepted SDL window/event-pump vertical slice has a real window, pump, coordinate conversion, and frame flush boundary | In that first SDL window/input-adapter slice; do not start earlier |
| Controller discovery, names, GUID/info, mapping database changes, ignored-device policy, connection-change event, vibration, duration/strength queries, and controller lights | SDL gamepad backend and lifecycle ownership | The first accepted SDL gamepad backend slice can open/close devices and receive connection events | In that first SDL gamepad slice; do not simulate devices before it |
| Accelerometer, gravity, gyroscope, magnetometer, and controller motion sensors | Mobile/gamepad sensor backend plus an accepted typed three-component sensor-value representation that does not introduce a 3D scene domain | Both the sensor-value ADR and a concrete SDL/mobile sensor adapter exist | In the first sensor slice after both prerequisites; not implied by ordinary gamepad work |
| MIDI event type, device enumeration, and message delivery | Product approval for a MIDI domain plus a selected native host API | A user-approved MIDI ADR names scope, host API, ownership, and platform matrix | Only after that separate decision; MIDI is not automatically part of SDL input work |
| Shortcut input event and shortcut matching | A concrete GUI/editor `Shortcut` resource and focus-routing component | The first accepted GUI shortcut vertical slice defines typed shortcut ownership | In that GUI slice; do not add an event with a null or inert shortcut |
| Loading action bindings from project settings and the corresponding loaded event | A versioned typed input-action persistence schema and ProjectSettings support for keyed action records | An accepted persistence ADR defines event unions, migration, and failure behavior | In the first action-persistence slice; runtime-only InputMap work does not unlock it |
| GUI control consumption and viewport-local input routing | Viewport/GUI focus, hit testing, and consumption semantics | The first production GUI/viewport input-routing slice exists | Integrate between `OnInput` and unhandled stages in that slice |

### Consequences

Gameplay can use deterministic typed input and scene callbacks before SDL integration. No method reports fictitious hardware success. A later agent can start a deferred row only by demonstrating its named trigger or by first obtaining the separate decision required by the row.

### Verification

The executable harness covers singleton lifetime, map validation and matching, the 32-source ceiling, complete event-property descriptors/defaults, modifiers, raw and mapped state, raw-strength analog vectors, independent transition lanes, direct action events, pre-mutation owner/execution rejection, reverse scene ordering, handled propagation, pause eligibility, re-entry rejection, callback failure continuation, failure-safe binding invalidation, atomic accumulation, transform semantics including unchanged pan delta, controller device state, release-all behavior, and zero warmed parsing/traversal allocation.
