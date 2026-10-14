# Touch game controls

Last updated: 2026-10-14

## Scope and runtime flow

[TouchScreenButton](../classes/TouchScreenButton.md) is an Entity/CanvasItem consumer of screen touch/drag, borrowed textures, bitmap/Shape hit masks and scene Input actions. [VirtualJoystick](../classes/VirtualJoystick.md) is a themed Control consumer of the same contacts, emitting four analog directions and tap/flick outcomes. [VirtualJoystickMode](../classes/VirtualJoystickMode.md), [JoystickVisibility](../classes/JoystickVisibility.md) and [TouchButtonVisibility](../classes/TouchButtonVisibility.md) retain distinct semantic domains.

SDL native contacts enter existing Input and Viewport routing. Button input runs in the Node stage and joystick input in local GUI coordinates. Each retained action contribution has an owner and slot, so release does not stomp peers, physical input or manual ActionPress. Button action events queue after traversal to preserve input snapshots; they are short-lived disposable events and do not reparse global Input. Joystick motion publishes prepared analog strengths without event creation. Gesture state and required cleanup commit before callbacks. Rebinding, hide, pause, disable, focus loss, exit and disposal release only owned slots. Canceled contacts have no tap/flick outcome. Resources and theme styles are borrowed; attached buttons retain visual residency.

## Dependencies and invariants

The component depends on the retained Input aggregate, SceneTree/Viewport input lane, Entity canvas transforms, Shape/BitMap hit queries, Theme/StyleBox drawing and typed scene archives. It adds no backend dependency or public singleton accessor. Authored descriptors/factories reconstruct exact nodes and shared resources; runtime contact/vector state is absent. Selected authoring input suppresses gameplay output while decoration remains visible. Geometry and event coordinates must be finite, sizes nonnegative and enum values defined. Zero clamp yields zero analog output.

## Verification and limits

TouchControlTests covers multi-source actions, signed contacts, masks/passby, fixed/dynamic/following scaling, duplicate directional names, dead-zone rescaling, cancel/failure/reentry, owner/capture validation and exact bundled scenes in a fresh process. TouchControlNativeTests pumps independent SDL contacts, checks actual normal/pressed texture and joystick-tip pixels on Linux GPU/compatibility, and measures 64 warmed prepared local motion/layout/render frames and 64 idle owner frames. Positional input copies lie outside that zero-byte interval. Discrete generated button events allocate; positional viewport copies are an inherited boundary. Native allocations, real mobile touchscreens, foreign rendering, semantic accessibility and user acceptance remain separate gates.

Accepted contracts: ADR 0038, ADR 0083, ADR 0008, ADR 0014, ADR 0051 and ADR 0090. API correspondence and dependency status remain in docs/coverage.

An active contact uses the existing internal processing lane to retire input at the next frame when inherited InputEnabled, effective pointer policy, viewport input disabling or authoring selection prevents further contact delivery. This lane stops after retirement; ordinary user processing flags remain independent.

A public-only self-contained NuGet consumer authors a bundled window/mask scene, reloads it in a fresh child process, supplies input between frames through an owner synchronization context with Engine.RunAsync, verifies directional/button state and captures real GPU pixels. The consumer runs outside its publish directory with LD_LIBRARY_PATH unset; the Linux audit covers 70 ELF files and 78 notices. Input pushed from render callbacks rejects reentry by design.
