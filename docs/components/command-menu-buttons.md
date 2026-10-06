# Command menu buttons

Last updated: 2026-10-06

MenuButton : Button owns one internal PopupMenu and supplies command menus for toolbar and menu-bar consumers. The popup's item model, check/radio state, shortcuts, submenus, search, scrolling and themes remain the existing PopupMenu implementation. This control is also an executable prerequisite for FileDialog's drive and file-sort menus.

## Presentation and input

Defaults are Flat=true, ToggleMode=true, ActionMode=ButtonPress, FocusMode=Accessibility and shortcut input enabled. SwitchOnHover defaults to false. GetPopup returns a stable borrowed node; the button owns its lifetime. AboutToPopup runs before geometry or visibility and may populate records. Detached presentation returns without opening. Reentrant presentation is rejected and failed presentation synchronizes the actual popup visibility back to the pressed state.

ShowPopup positions below the button in the actual embedding viewport, aligns the trailing edge in RTL and uses the popup's content minimum width. When at least four button heights remain beneath the anchor, MaxSize constrains menu height to that space; the common popup layout fits/clips into its host. Keyboard/programmatic opening focuses the first enabled nonseparator command after popup preparation. Pointer opening preserves grabbed-click behavior and starts without item focus. Pressing toggles the open menu. Hiding the owner or exiting its tree hides the popup; external popup hide also resets pressed state before later hide observers can fail.

Item shortcuts are processed before the button's inherited shortcut stage. Visibility, enabled state and SetDisableShortcuts gate the owner stage. Individual PopupMenu AllowEcho policy governs item echo; the button's own shortcut retains its inherited echo rules. Menu activation emits command ID/index events and preserves explicit check/state edits. A callback may dispose its button; shortcut handling retains the captured tree rather than dereferencing the detached owner afterward.

While a menu is open and SwitchOnHover is enabled, related enabled menu buttons which also opt in may replace it on the next internal process phase. The accepted hierarchy relation is either the active owner's parent containing the target, or the target's parent containing the active popup. Unrelated branches, disabled/hidden/paused targets and overlays which win the shared hit test are skipped. The new menu has no item focus after switching. Required hide/present/focus cleanup continues after callback failures, and failures remain aggregated.

SceneTree updates the menu-bar hover through its existing canvas-order, clipping and visibility hit test while an embedded popup owns pointer delivery. Nested embedded window routing now descends to the final focused/pointer target and disposes intermediate transformed input copies. Popup ownership under an inner embedding Window therefore works for both MenuButton and OptionButton. OptionButton's anchor correction adds a window offset only when the actual popup embedder is outside that window.

## Storage and verification

PackedScene and the built-in file factory recreate the independent internal popup. Stored button defaults, SwitchOnHover and ItemCount precede seven indexed popup fields: text, icon, ID, disabled, separator, checked and checkable kind (0 plain, 1 checkbox, 2 radio). Runtime shortcut-disable choice, delegates, metadata, shortcuts and transient popup/focus state are not stored. Icons/resources remain borrowed. Configuration warnings combine the button and popup queries.

MenuButtonTests checks defaults, keyboard/pointer presentation, item events, hover gates/hierarchy, stale pointer capture, echo/disabled shortcuts, owner hide/disposal, failures/reentry, nested Window/OptionButton input and fresh saved scenes. Native tests inject real SDL pointer motion/click and keyboard events, read back toolbar/menu pixels on current Linux Wayland GPU and compatibility targets and inspect PNGs. The 32-warmup/64-active-frame measurement varies caption and item focus with the menu visible and a stable target size. Cold presentation, viewport creation/resizing, pointer-copy allocation, native memory and other target/human acceptance remain outside that zero-managed-allocation result.

Own MenuButton members execute in this embedded profile. The class remains Partial for inherited Control accessibility/editor facilities, PopupMenu native/system menu services and independent native child Window hosting. FileDialog still requires the scoped browser/filter/open/save and trash/native-options layers recorded in its coverage; this button is a foundation for that workflow.

Decisions: [0008](../decisions/scene.md#adr-0008), [0038](../decisions/input.md#adr-0038), [0040](../decisions/display.md#adr-0040), [0083](../decisions/rendering.md#adr-0083). Owned type: [MenuButton](../classes/MenuButton.md).

Popup anchors include GetGlobalTransformWithCanvas before any embedder offset, so a changed canvas translation moves the command/choice popup by the same amount exactly once. MenuButtonTests verifies both consumers under a translated inner viewport.
