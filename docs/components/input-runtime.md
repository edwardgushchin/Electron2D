# Input runtime component

Last updated: 2026-09-23

## Scope

This component converts caller-supplied typed events into raw device state, named action state, per-process/per-physics transitions, and synchronous scene callbacks. It owns no native device or pump.

## Owned types

`Input`, `InputMap`, `InputEvent`, `InputEventAction`, `InputEventFromWindow`, `InputEventWithModifiers`, `InputEventKey`, `InputEventMouse`, `InputEventMouseButton`, `InputEventMouseMotion`, `InputEventJoypadButton`, `InputEventJoypadMotion`, `InputEventScreenTouch`, `InputEventScreenDrag`, `InputEventGesture`, `InputEventMagnifyGesture`, `InputEventPanGesture`, `Key`, `KeyModifierMask`, `KeyLocation`, `MouseButton`, `MouseButtonMask`, `JoyAxis`, and `JoyButton`.

## Runtime flow

1. A host constructs a live `InputEvent` and calls `Input.ParseInputEvent`.
2. An active MainLoop validates owner-thread and idle-running eligibility before mutation.
3. InputMap validates all relevant live bindings and collects matches under its configuration lock.
4. Input commits raw state and per-binding action contributions under its state lock.
5. Optional pointer emulation creates one device-`-1` event: the first active touch contact generates mouse input, while a left-button mouse press/drag/release can generate scene-only touch input. The generated event is dispatched before its source and is never emulated again. Generated mouse input also commits raw and mapped state; a generated touch does not.
6. If an Engine loop is active, the source event is synchronously forwarded to it, even if a generated-event callback failed. Callback failures from both events are combined.
7. For a Viewport root, SceneTree removes the final transform from window input before callbacks; temporary positional copies are disposed after synchronous dispatch, even on failure. Node-root input retains its original identity. SceneTree captures nodes, walks the snapshot in reverse depth-first order through input, unhandled-key, and unhandled stages, and revalidates membership before every callback.
8. Each process/physics frame clears only its own just-transition lane in `finally`.

## Dependencies and interactions

The component uses `Resource` for event duplication/change reporting, `PropertyDescriptor` for inherited stored event fields, `ElectronObject` for singleton lifecycle/property discovery, `Vector2`/`Transform`/`Mathf` for value operations, Engine/MainLoop for host integration, and Node/SceneTree for dispatch. There is no native backend dependency.

## Invariants

- No partially applied action mapping: loop eligibility and matching finish before raw/action mutation.
- No parse re-entry; state remains committed if later user callbacks fail.
- An emulated press owns a matching release even if its setting is disabled while held. Mouse-button masks remain per device so an emulated release cannot clear a physical hold.
- SDL-originated touch-to-mouse and mouse-to-touch duplicates are filtered before `Input`, so each native source produces one configured emulation path.
- Multiple bindings contribute independently; action strength is the maximum live contribution.
- Editing or copying a registered binding synchronously invalidates cached contributions for every action using that reference before fallible public change observers run.
- Disposing a registered binding outside matching removes it from every affected action and clears its contributions before public disposal observers run.
- Exact matching filters extra modifiers and analog direction; releases remove their mapped source.
- Synthetic action descriptions skip synthetic bindings during lookup and fall back to the action name, so cyclic description lookup cannot recurse.
- Node ordering and handled semantics are deterministic; callback exceptions are aggregated.
- Event resources remain caller-owned. Input neither disposes nor stores submitted event objects after transition identity expires.
- Every event value has a validated stored typed property descriptor, and constructor-specific device defaults are the corresponding revert values.
- Warmed non-emulated matching and traversal reuse bounded collections. Emulated events and positional viewport-conversion copies are short-lived owned resources and allocate.

## Current implementation status and exclusions

Keyboard, mouse buttons/motion, touch/drag, magnify/pan gestures, controller buttons/axes, direct action events, action maps, raw key/mouse/controller and action queries, vector composition, transition latches, release-all, and Node propagation are implemented.

The separate [Display server](display-server.md) now supplies native keyboard, mouse, wheel, and touch input and basic cursor/window control through this component's typed event surface. Mouse-motion accumulation, explicit flush, mouse/touch emulation, and SDL-origin duplicate filtering are implemented. Controller discovery/effects, sensors, MIDI, shortcuts, persistence, and GUI routing retain their separate prerequisites under [ADR 0038](../decisions/input.md#deferred-coverage-and-exact-implementation-triggers). No missing service is represented by a stub.

For native SDL key events, the display adapter records the event's logical key, maps its scancode to a physical key, and derives the unmodified layout label from that scancode separately. The label may be a non-Latin printable key and need not equal the logical key; non-Latin printable scalars are normalized to invariant uppercase key identities. Left/right control, shift, alt, and GUI scancodes produce the matching `KeyLocation` on press and release; other scancodes use `Unspecified`. The adapter currently leaves `InputEventKey.Unicode` at zero; committed text uses a separate event. SDL key events have no produced text scalar, while text-input events may contain multiple scalars or an IME commit without a key-event identifier. A native per-key Unicode source with verified IME/composition semantics is required in the first native keyboard/text adapter slice. Caller-created `InputEventKey` values may supply a Unicode scalar directly.

## Verification

`tests/Electron2D.Tests/Program.cs` covers validation, complete stored event-property discovery/defaults, duplicate bindings, the 32-source ceiling, modifier exactness, pre-mutation owner/execution rejection, state-before-callback ordering, reverse stage order, handled input, independent process/physics transitions, callback failure aggregation, parse re-entry, failure-safe binding invalidation, raw-strength vector composition, direct actions, mouse/controller state, atomic event accumulation, event transforms including unchanged pan delta, release-all, permanent singleton registration, and zero warmed non-emulated managed allocation. Emulation checks cover defaults, first-touch ownership across devices, pairing across setting changes, mapped synthetic mouse state, mouse-to-touch scene delivery, recursive suppression, and callback failure continuation. The optional SDL dummy-driver suite checks native pointer modifier translation.

The SDL dummy-driver suite also checks that a key's scancode label is independent of its logical keycode, that release clears label state, that non-Latin label code points retain their key identity in the mapper, and that all eight side-specific modifier scancodes plus an ordinary key carry the expected location on press and release. A physical keyboard layout has not been exercised.

Viewport localization preserves raw Input state and the original by-event transition identity; a positional copy has a distinct InstanceID. All six positional XformedBy implementations validate derived finite coordinates before Duplicate, preventing unowned partial event copies on overflow. [CanvasCoordinateTests](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs) verify this boundary, disposal on callback failure and local-coordinate borrowing; native injection is covered by CanvasCoordinateRenderingTests.
