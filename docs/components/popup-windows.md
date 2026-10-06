# Embedded popup windows

Last updated: 2026-10-06

Owns [Window](../classes/Window.md), [Viewport](../classes/Viewport.md) embedded ownership and the [Popup](../classes/Popup.md)/[PopupPanel](../classes/PopupPanel.md) consumers under ADRs 0008, 0038, 0040 and 0083.

## Ownership, lifecycle and current profile

Configure `GUIEmbedSubwindows=true` on a containing viewport before attaching child windows. The viewport owns internal presentation layers; each public Window remains in its original scene hierarchy and owns its own GUI state and native render target. Images share the existing renderer/device, use completed native images, and never pass through per-frame CPU readback/upload. Hidden windows remain attached; temporary layers are released at a safe point after detachment or with their viewport during tree teardown. Public child enumeration and packing omit the presentation layers.

Independent native child windows are not implemented: showing a child without an embedder fails explicitly before visibility commits. Live migration between embedded and independent-native modes is also absent; changing GUIEmbedSubwindows while embedded windows are attached throws. These capabilities retain Partial/Blocked coverage. Native flags that are meaningful only for independent OS windows retain their native integration prerequisite. Full decoration/theme parity, embedded pointer entry/exit signals, cross-window drag sections and platform-specific window modes are separate remaining capabilities; the embedded baseline draws a title/close decoration and supports owner-thread title dragging and border resizing.

The host routes pointer, touch and gesture coordinates into the selected window and retains button/contact capture outside its rectangle. Keyboard/controller actions go to the focused child viewport. Popup windows intercept outside clicks; Exclusive blocks dismissal and outside input. Opening and hiding update back-to-front ordering and window focus while preserving separate control-focus owners. Hiding a parent also hides its descendant windows. Exceptions from visibility/focus cleanup are collected after required state transitions. Public PushInput re-entry is rejected before routing callbacks. Direct popup PushInput applies the same deferred cancellation as host-routed input. Visibility propagation completes before menu internal controls receive focus. Text and composition can reach an embedded window's focused control; native IME requests use the containing native root and translated caret position.

Popup geometry is expressed in its embedding viewport coordinates. Centered methods resolve control minimums after AboutToPopup, then center the resulting size. Popup fits into the usable host rectangle and applies maximum sizes. Ratio arguments reject nonfinite values and values outside (0,1]. Popup requests on a root or detached Window fail. Exclusive helper methods require a parentless dialog, choose the active exclusive descendant owner and undo parenting when showing fails.

PopupHide follows committed hidden state. Close requests and exact non-echo ui_cancel defer hide on SceneTree; repeated pending requests collapse. PopupPanel adds a borrowed `panel` StyleBox, content margins and shadow insets. It fits direct non-top-level controls, inherits texture sampling, mirrors horizontal shadow offsets for RTL, restores the content rectangle after hiding, and preserves an explicit resize made while visible. Theme and direction notifications refresh layout after lookup invalidation. Control and Window share LayoutDirection under ADR 0051; same-domain controls inherit Window direction.

## Persistence and verification

PackedScene stores configured geometry, visibility and native-independent window policies, theme, direction and public child controls. Popup/PopupPanel exact built-in factories reconstruct fresh internal panel/presentation owners. Choose the packed scene root as Owner for content intended to be captured. ResourceSaver/ResourceLoader preserve this scene in a fresh process and the reconstructed popup runs on SceneTree.

PopupTests covers defaults, host rejection, centering/clamping, routed clicks, deferred cancellation/outside hide, Exclusive, nested focus restoration, observer failure after commit, RTL/shadow sizing, decoration dragging/close, text focus, PackedScene and fresh-process archive execution. PopupRenderingTests exercises actual transparent/themed and nested textures, visibility changes and renderer cleanup on Linux Wayland GPU and compatibility; generated diagnostic PNGs were visually inspected. Warm placement/render checks measure managed allocations separately. Physical pointer/touch/IME use, native allocator totals, other targets and user acceptance remain separate verification work.

## Example

```csharp
var window = new Window { Size = new(640, 360), GUIEmbedSubwindows = true };
var popup = new PopupPanel { Name = "Message", Size = new(240, 100) };
popup.AddChild(new Label { Text = "Ready", Name = "Content" });
window.AddChild(popup);
window.Ready += _ => popup.PopupCentered();
Engine.Run(window);
```

The [PopupMenu consumer](popup-menus.md) now executes with item, shortcut, submenu, scroll and search behavior. [TabContainer](tab-panels.md) now consumes the popup host; MenuButton, OptionButton and LineEdit context actions remain dependent consumers. Native popup-menu hints do not imply a NativeMenu service or platform menu integration; ADR 0041 retains their separate decision gate.

Command menu buttons now use underlying menu-bar hover while their popup owns pointer input. Nested embedded hosts route through each Window to the final target and release intermediate event copies. The shared behavior is exercised by MenuButton and OptionButton nested tests.
