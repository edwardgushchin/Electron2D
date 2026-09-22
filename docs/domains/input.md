# Input domain

Last updated: 2026-09-23

## Responsibility

Input owns typed input-event values, the process-wide action map, raw and mapped input state, frame-latched transitions, and delivery into the active node hierarchy. It is platform-neutral production code in `Electron2D.dll`; the separate Display domain now provides an SDL native event source.

## Component inventory

| Component | Responsibility | State |
| --- | --- | --- |
| [Input runtime](../components/input-runtime.md) | Typed events, action bindings/state, raw device state, frame transitions, and SceneTree propagation | Implemented and verified; hardware-host gaps have exact triggers |

Production types are [`Input`](../classes/Input.md), [`InputMap`](../classes/InputMap.md), the [`InputEvent`](../classes/InputEvent.md) hierarchy, and the seven input enums listed in the inventory.

## Public surface

- `Input`: raw key/mouse/controller queries, named action queries and injection, axes/vectors, event parsing, pointer accumulation and flush, mouse/touch emulation policy, and release-all.
- `InputMap`: action registration, deadzones, binding management, matching, and descriptions.
- `InputEvent` hierarchy: typed stored property discovery, action matching, text, duplication, accumulation, coordinate transforms, device/window/modifier data, and concrete keyboard, pointer, touch, gesture, controller, and direct-action payloads.
- Input enums: complete key identifiers/modifier masks, key location, mouse buttons/mask, and standardized/raw controller axes/buttons.
- `SceneNode`/`SceneTree` integration: explicit opt-in callbacks and handled propagation.

## Dependency direction

Input depends on Core object/resource lifecycle and math. `Engine` registers the two process singletons and `MainLoop` owns transition-window completion. Scene consumes Input events for node delivery. Input has no dependency on SDL, a renderer, GUI, audio, physics, networking, or an editor.

## Domain-wide invariants

- Parsing is serialized and non-reentrant; mapping is resolved before state mutation.
- Raw and action state is committed before scene callbacks and is not rolled back when callbacks fail.
- Emulated events use device ID `-1`, precede their source events, and do not recursively generate input. Their active press is paired with a release when the setting changes during contact.
- Action names are ordinal; strengths/deadzones are finite and bounded.
- Process and physics transition windows are independent and clear even after a frame callback fails.
- Scene input is owner-thread, pause-aware, reverse depth-first, membership-revalidated, and stoppable through handled state.
- Binding configuration and queries are lock-serialized. Registered binding resources remain live caller-owned references and cannot be disposed or mutated concurrently with matching.
- The warmed non-emulated mapped event and scene traversal path performs no steady-state managed allocation; creating a generated pointer event allocates its short-lived resource.

## Current limitations

The Display domain now implements native cursor/window operations and direct SDL event pumping, including mouse-motion accumulation, explicit input flush, mouse/touch emulation, and deduplication of SDL-generated pointer counterparts. Controller lifecycle/effects, sensors, MIDI, shortcuts, project-setting action persistence, and GUI/viewport consumption remain absent. None is represented by a stub. [ADR 0038](../decisions/input.md#adr-0038) names the exact implementation trigger and actionability rule for every gap.

The SDL keyboard adapter supplies distinct logical, physical, and current-layout label keys. Its label is derived from the unmodified scancode and can preserve non-Latin key identity. Left/right control, shift, alt, and GUI scancodes set the corresponding `KeyLocation`; all other scancodes are `Unspecified`. Native key events currently leave `Unicode` at zero; text input is delivered separately. SDL key events contain no produced text scalar, and a text-input event can represent multiple scalars or an IME commit without identifying a key press. A native per-key Unicode source with verified IME/composition semantics is required in the first native keyboard/text adapter slice. Caller-created typed key events may carry a Unicode scalar.

The API has executable verification on Linux as managed code, including emulation state, event order, release pairing, and callback failures. Separate SDL dummy-driver tests cover synthetic keyboard delivery and native pointer modifier translation into Input; no physical keyboard/mouse/touch/controller test, mobile sensor test, controller vibration, MIDI device, or native-host matrix has been exercised.

The dummy suite verifies independent scancode labels and logical keycodes, release of label state, non-Latin label conversion, and `KeyLocation` on press and release for eight modifier scancodes plus an ordinary key. This is synthetic input and does not establish behavior for a physical keyboard or every host layout.

## Relevant decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0014: Managed Resource lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
