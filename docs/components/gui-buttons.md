# GUI buttons and shortcuts

Last updated: 2026-10-04

The component turns pointer, touch, action and shortcut input into themed application actions. It owns [BaseButton](../classes/BaseButton.md), [ButtonGroup](../classes/ButtonGroup.md), [Button](../classes/Button.md), [CheckBox](../classes/CheckBox.md), [CheckButton](../classes/CheckButton.md) and [TextureButton](../classes/TextureButton.md). [Shortcut](../classes/Shortcut.md) and [InputEventShortcut](../classes/InputEventShortcut.md) integrate the input-resource and scene-routing boundary. It reuses Control, Theme, Font, StyleBox, Texture and BitMap; it adds no backend or managed dependency.

## Runtime flow

- SceneTree delivers ordinary input, root GUI input, typed shortcut input, unhandled-key input and general unhandled input in order. Handling stops later stages; captured traversal and callback-failure policy remain shared with the scene system.
- BaseButton retains transient press/touch/hover/feedback state separately from the toggle value. Press/release action policy, button masks, groups, disabled state, focus and shortcuts all enter the same activation hooks and typed events. ButtonGroup borrows weak runtime membership and preserves one selected member unless unpressing is allowed.
- Button shapes its text through the existing TextLayout and combines theme state styles, font/outline colors and optional icons. CheckBox adds check/radio assets; CheckButton adds a toggle indicator. TextureButton independently selects state textures, applies seven stretch modes and visual flips, and maps hit tests through its optional bitmap.
- Borrowed-resource callbacks invalidate layouts and retained drawing. Attached controls hold balanced renderer residency counts for known state textures and their atlas chains, releasing them on replacement, exit and disposal. Internal content revisions and exact atlas-chain dependencies recover after an earlier observer throws; background changes defer work onto the scene owner thread. A query cannot consume the pending redraw obligation.
- Control tooltips use an unscaled delay and the same theme/text canvas. An internal overlay has presentation priority over game CanvasLayers, including maximum-index layers, while preserving ordering inside its contents. TooltipPanel and TooltipLabel are ordinary themed consumers. Custom results transfer ownership only when fresh and detached; cancellation queues deletion at a safe point, and final tree teardown releases remaining owned content.

## State and failure boundaries

A ShortcutContext is a weak node identity. Missing context is global; an expired configured context is inactive. Packed scenes store node references as relative paths and resolve them only after the complete new hierarchy exists. Button groups, resource aliases and scene-local copies preserve existing Resource rules. Transient input attempts, membership and tooltip nodes are runtime state. Pausing or disabling processing cancels a blocked button's transient touch/press attempt without changing a toggle selection; this pre-release correction prevents a suppressed release from stranding the next touch. Always-processing controls remain active.

Callbacks may mutate state, dispose resources or reenter another action. Generation checks prevent an older activation, group selection or tooltip fitting pass from overwriting a newer result. Invalid geometry and disposed resources fail at their established typed boundaries. A changed property remains committed when its notification handler fails.

## Verification boundaries

The focused managed suites cover shortcut identity and alternatives, weak contexts and copies, button state/event ordering, geometry, bitmap hit masks, resource callback failures, reentry, scene storage and tooltip ownership. TextureButton has Linux Wayland GPU/compatibility pixel checks for seven stretch modes, flips, states, focus and atlas replacement, plus 64 warmed active frames with zero managed allocation. Button/CheckBox/CheckButton pass ten Linux Wayland GPU/compatibility visual/input phases and 64 warmed active frames with zero managed allocation using distinct state textures. Tooltip native checks pass on both Wayland backends and the dummy software compatibility driver with Nearest sampling: real glyphs, a maximum-layer occluder, custom contents, edge placement, native pointer transparency and 64 warmed modulation frames. Whole-slice format/build/coverage verification is recorded with the completed change.

Warmed local button/group state, routed shortcuts and reusable hover buffers allocate zero managed bytes in their measured checks. Full routed pointer input deliberately retains the existing temporary Resource-copy ownership contract under [ADR 0038](../decisions/input.md#adr-0038): 64 measured cycles allocate 180,736 bytes, exactly equal to independently measuring their three localized event copies per cycle. The test rejects allocations beyond those copies; this is not a zero-allocation claim for complete pointer dispatch. Native allocations, broad-GUI performance, other-platform execution and owner acceptance remain separate gates.

MenuButton and OptionButton require a real PopupMenu; LinkButton URI activation requires the OS URL-opening service. Independent native popup windows, accessibility semantics, drag/drop and scroll gesture ownership remain separate domains. The current root tooltip host does not claim those APIs. [ADR 0083](../decisions/rendering.md#adr-0083) owns themes, [ADR 0046](../decisions/rendering.md#adr-0046) owns text and [ADR 0023](../decisions/scene.md#adr-0023) owns typed scene storage.

Embedded Controls now reuse the same button/focus/tooltip hooks in independent viewport contexts. Connected SubViewportContainer sections forward input and share drag targets/previews; native cursor/keyboard focus pixels are checked by SubViewportContainerTests. Temporary input Resource-copy and native/other-platform limits remain unchanged.

## Text-field interaction

[LineEdit](../classes/LineEdit.md) integrates Control focus, GUIInput, TextInput, IMECompositionChanged and drag hooks for a real single-line editor. Its editing/display tests cover managed authoring and native X11 GPU/compatibility hosts. Popup menus, native virtual keyboards and native symbol-picker presentation retain their exact separate services; the LineEdit coverage class remains Partial.

[Dropdown choices](dropdown-choices.md) add OptionButton as a concrete Button/PopupMenu consumer. The shared Button text path respects selected item translation, and owner polling clears disposed borrowed icons before internal processing can access them. ButtonTests and the new choice tests cover worker disposal; the shared public API is unchanged.
