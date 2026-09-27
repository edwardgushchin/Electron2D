# Input domain

Last updated: 2026-09-27

## Responsibility

Input owns typed input-event values, the process-wide action map, raw and mapped input state, frame-latched transitions, and delivery into the active node hierarchy. Its pointer controls delegate to the active Display domain, which provides the SDL native event source and owns native cursor/window state. Input itself does not call SDL.

## Component inventory

| Component | Responsibility | State |
| --- | --- | --- |
| [Input runtime](../components/input-runtime.md) | Typed events, action bindings/state, raw device state, frame transitions, and SceneTree propagation | Implemented and verified; hardware-host gaps have exact triggers |

Production types are [`Input`](../classes/Input.md), [`InputMap`](../classes/InputMap.md), [`JoypadInfo`](../classes/JoypadInfo.md), [`InputActionSettings`](../classes/InputActionSettings.md), [`InputBindingSettings`](../classes/InputBindingSettings.md), [`InputBindingKind`](../classes/InputBindingKind.md), the [`InputEvent`](../classes/InputEvent.md) hierarchy, and the seven input enums listed in the inventory.

## Public surface

- `Input`: raw key/mouse/controller queries, named action queries and injection, axes/vectors, event parsing, pointer accumulation and flush, mouse/touch emulation policy, native mouse mode/cursor controls and warp, connected controller metadata, mapping, vibration/LED requests, focus policy, and release-all.
- `InputMap`: case-sensitive action registration, default `ui_*` focus navigation bindings, typed project-action loading and its loaded event, validated deadzones, immutable collection snapshots of borrowed binding references, binding management, matching, and descriptions.
- `InputEvent` hierarchy: typed stored property discovery, action matching, text, duplication, accumulation, coordinate transforms, device/window/modifier data, and concrete keyboard, pointer, touch, gesture, controller, and direct-action payloads. Direct action events retain four typed values, use a device/index source for presses and releases, and choose the first concrete binding for their description. Gesture and mouse payloads retain source values; native magnify/pan recognition remains absent. Root GUI mouse copies use CanvasLayer-global coordinates.
- Mouse-button and motion source values are retained; the SDL adapter validates native pointer coordinates and wheel amounts before dispatch. Native stylus fields and some viewport/mode paths still need their named integrations and checks.
- Touch and drag source values are retained; SDL touch input validates native coordinates and movement before contact tracking. Native double-tap recognition, pen data and nested viewport routing remain open.
- Base event state uses independent pressed/canceled flags; canceled matching events can report an action release through the map. Non-positional transforms return the original event, while positional conversions own disposable copies.
- Touch and drag events store signed contact indexes; the native display source generates indexes for physical contacts.
- Input enums: complete key identifiers/modifier masks, key location, mouse buttons/mask, and standardized/raw controller axes/buttons.
- `Node`/`SceneTree` integration: explicit opt-in callbacks, root viewport Control targeting, hover, focus ownership/notification, action navigation and handled propagation.

## Dependency direction

Input depends on Core object/resource lifecycle, math and typed ProjectSettings snapshots. `Engine` registers the two process singletons and `MainLoop` owns transition-window completion. Scene consumes Input events for node delivery. Pointer controls query `DisplayServer.Instance` and use its owner-thread native operations; Input stores the default cursor policy while DisplayServer owns the current native cursor. Input has no direct SDL, renderer, audio, physics, networking, or editor dependency.

## Domain-wide invariants

- Parsing is serialized and non-reentrant; mapping is resolved before state mutation.
- Raw and action state is committed before scene callbacks and is not rolled back when callbacks fail.
- Emulated events use device ID `-1`, precede their source events, and do not recursively generate input. Their active press is paired with a release when the setting changes during contact.
- Action names are ordinal; strengths/deadzones are finite and bounded.
- Project-action reload applies active typed feature overrides, rejects unidentified key bindings and over-capacity record lists, and validates the whole candidate before replacing the map or notifying listeners.
- Public event comparison and exact action-binding lookup have distinct rules for synthetic action events; the map never collapses a physical binding into a synthetic one solely because it triggers the named action.
- Key display uses defined key names, platform modifier labels, localized unset/physical markers and lowercase locations. The action map localizes the no-input message and separator while concrete binding text remains owned by each event family.
- Mouse-button display uses nine localized names, an arbitrary numeric fallback and a localized double-click suffix; mouse motion uses a translated position/velocity template, with float-precision rounding checked against the pinned Linux algorithm. Controller events retain source button/pressure/axis values; raw axis presses use the fixed 0.5 toggle threshold while mapped actions use their own deadzones. Controller text and signed raw per-device queries have managed checks; project bindings retain their stricter version-one limits, and negative physical-device queries fail explicitly. Touch and gesture source sentences, states, signed indexes and translation now execute; shared float formatting has a 41,514-value deterministic comparison; other platforms remain unverified.
- Process and physics transition windows are independent and clear even after a frame callback fails.
- Scene input is owner-thread, pause-aware, reverse depth-first for Node stages, and stoppable through handled state. Root viewport Control targeting uses geometry and focus between the Node stages.
- Binding configuration and queries are lock-serialized. Registered binding resources remain live caller-owned references and cannot be disposed or mutated concurrently with matching.
- The warmed non-emulated mapped event and scene traversal path performs no steady-state managed allocation; generated pointer events and positional viewport-conversion copies allocate short-lived resources.

## Current limitations

The Display domain implements native cursor/window operations and direct SDL event pumping, including mouse-motion accumulation, explicit input flush, mouse/touch emulation, and deduplication of SDL-generated pointer counterparts. Input exposes mouse modes, 17 cursor shapes, custom image slots and warp via that domain. The cursor-default call retains a separate default and root-viewport Control hover overrides it; synthetic motion refresh remains absent. Wayland rejects pointer warp; supported backend movement is unverified. Root viewport Control pointer/touch/gesture and shortcut routing, hover, keyboard focus and managed Tab/arrow/D-pad/left-stick navigation are implemented; stationary-pointer geometry changes, exact directional ranking, scroll clipping, exact drawing order and nested viewport routing remain absent. Virtual SDL gamepad and raw joystick delivery, lifecycle, vibration and LED requests run on dummy and Linux Wayland; physical devices, platform-specific info, sensors and MIDI remain unverified or absent. Typed project action definitions load explicitly; project file loading does not automatically replace the live map. [ADR 0038](../decisions/input.md#adr-0038) names the exact implementation trigger and actionability rule for every gap.

The SDL keyboard adapter supplies distinct logical, physical, and current-layout label keys. Its label is derived from the unmodified scancode and can preserve non-Latin key identity. Left/right control, shift, alt, and GUI scancodes set the corresponding `KeyLocation`; all other scancodes are `Unspecified`. Native key events currently leave `Unicode` at zero; text input is delivered separately. SDL key events contain no produced text scalar, and a text-input event can represent multiple scalars or an IME commit without identifying a key press. A native per-key Unicode source with verified IME/composition semantics is required in the first native keyboard/text adapter slice. Caller-created typed key events may carry a Unicode scalar.

On native key events, a modifier key excludes its own family from the modifier mask and retains other held families. The inherited modifier-event device default is 16; mouse and gesture descendants override it through their typed descriptors. Targeted SDL dummy and Wayland checks verify both sides of the four modifier families, while physical keyboard behavior is unverified.

The API has executable verification on Linux as managed code, including emulation state, event order, release pairing, and callback failures. Separate SDL dummy-driver tests cover synthetic keyboard delivery and native pointer modifier translation into Input; no physical keyboard/mouse/touch/controller test, mobile sensor test, MIDI device, or complete native-host matrix has been exercised. Virtual controller rumble and LED callbacks passed on dummy and Wayland.

The dummy suite verifies independent scancode labels and logical keycodes, release of label state, non-Latin label conversion, and `KeyLocation` on press and release for eight modifier scancodes plus an ordinary key. This is synthetic input and does not establish behavior for a physical keyboard or every host layout.

## Relevant decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0014: Managed Resource lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)

SceneTree root-viewport localization occurs after raw Input state is committed and preserves the original by-event identity. CanvasItem local conversion is an explicit scene query; positional copies have distinct identities and ownership. See [canvas coordinates](../components/canvas-rendering.md#viewport-coordinates) for native verification and failure behavior.

The permanent typed ui_home/ui_end defaults use Key.Home/Key.End and the existing project-setting reload path. [Slider](../classes/Slider.md) consumes them as range endpoint requests, alongside matching-axis direction actions; unhandled opposite-axis input remains available to GUI focus traversal. These actions remain rebindable. Slider repeat uses the existing internal scene-processing lane and controller action state.

The [GUI buttons and shortcuts component](../components/gui-buttons.md) connects the existing theme/text canvas with button actions, groups, texture masks, shortcut resources and tooltip presentation. Its verification section records the measured input-copy allocation boundary and current native gates.
