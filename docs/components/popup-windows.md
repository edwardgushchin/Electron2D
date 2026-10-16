# Embedded popup windows

Last updated: 2026-10-16

Owns [Window](../classes/Window.md), [Viewport](../classes/Viewport.md) embedded ownership and the [Popup](../classes/Popup.md)/[PopupPanel](../classes/PopupPanel.md) consumers under ADRs 0008, 0038, 0040 and 0083.

## Ownership, lifecycle and current profile

Configure `GUIEmbedSubwindows=true` on a containing viewport before attaching child windows. The viewport owns internal presentation layers; each public Window remains in its original scene hierarchy and owns its own GUI state and native render target. Images share the existing renderer/device, use completed native images, and never pass through per-frame CPU readback/upload. Hidden windows remain attached; temporary layers are released at a safe point after detachment or with their viewport during tree teardown. Public child enumeration and packing omit the presentation layers.

Independent native child windows are not implemented: showing a child without an embedder fails explicitly before visibility commits. Live migration between embedded and independent-native modes is also absent; changing GUIEmbedSubwindows while embedded windows are attached throws. These capabilities retain Partial/Blocked coverage. Native flags that are meaningful only for independent OS windows retain their native integration prerequisite. Full decoration/theme parity, embedded pointer entry/exit signals, cross-window drag sections and platform-specific window modes are separate remaining capabilities; the embedded baseline draws a title/close decoration and supports owner-thread title dragging and border resizing.

The host routes pointer, touch and gesture coordinates into the selected window and retains button/contact capture outside its rectangle. Keyboard/controller actions go to the focused child viewport. Popup windows intercept outside clicks; Exclusive blocks dismissal and outside input. Opening and hiding update back-to-front ordering and window focus while preserving separate control-focus owners. Hiding a parent also hides its descendant windows. Exceptions from visibility/focus cleanup are collected after required state transitions. Public PushInput re-entry is rejected before routing callbacks. Direct popup PushInput applies the same deferred cancellation as host-routed input. Visibility propagation completes before menu internal controls receive focus. Text and composition can reach an embedded window's focused control; native IME requests use the containing native root and translated caret position.

Popup geometry is expressed in its embedding viewport coordinates. Centered methods resolve control minimums after AboutToPopup, then center the resulting size. Popup fits into the usable host rectangle and applies maximum sizes. Ratio arguments reject nonfinite values and values outside (0,1]. Popup requests on a root or detached Window fail. Exclusive helper methods require a parentless dialog, choose the active exclusive descendant owner and undo parenting when showing fails.

PopupHide follows committed hidden state. Close requests and exact non-echo ui_cancel defer hide on SceneTree; repeated pending requests collapse. PopupPanel adds a borrowed `panel` StyleBox, content margins and shadow insets. It fits direct non-top-level controls, inherits texture sampling, mirrors horizontal shadow offsets for RTL, restores the content rectangle after hiding, and preserves an explicit resize made while visible. Theme and direction notifications refresh layout after lookup invalidation. Control and Window share LayoutDirection under ADR 0051; same-domain controls inherit Window direction.

### Active exclusive window chains

`Node.GetLastExclusiveWindow()` starts at the caller's containing Window and
follows retained active exclusive transient-child links. It returns null for
detached/no-window nodes and enforces the scene owner or the caller's own
processing group. These links follow transient parent ownership, independently
of display/focus ordering. Enabling Transient/Exclusive on a visible attached
window selects it; hidden, nontransient and nonexclusive children do not extend
the chain. The containing Window comes from Node's active viewport association,
which is null after detachment. The latest conflicting activation wins with a diagnostic. Removing
that selection leaves the parent without an active exclusive child rather than
silently selecting an older visible sibling.

Flag and visibility changes, enter/exit, reparent and disposal keep those links
current before callbacks. Window membership cleanup attempts transient, embedded
and theme work even after an observer fails. Embedded focus routing skips
disposing/retired windows, avoiding activity through a cleared publisher.

All five `PopupExclusive*` methods select the same final owner. Their common
rollback covers both attachment and popup failures. If detachment also fails,
an AggregateException retains the original and cleanup errors; committed
detachment still removes transient selection. Window's independent-native child
capability and broader GUI behavior remain separate from this hierarchy query.

The public physics consumer runs this contract in twelve Linux x64 processes:
CoreCLR and strict NativeAOT, CPU/software-GPU/registered backends, each with
serial and separate physics scheduling. It covers nine physical/inherited roles,
all five popup entry points, wrong processing groups, callback failures,
detachment/reparent and weak retirement. Each profile measures 128 complete
physical frames after 128 warmup frames and requires zero owner/all-thread managed
bytes, 128 real integration callbacks, unchanged body RID and 12.8 units of motion
within 0.01 to allow accumulated single-precision integration error. CPU and registered profiles use an unavailable Vulkan ICD; GPU profiles
pin lavapipe. Native/driver allocation totals and hardware throughput are not
established by these checks.

The current Linux offscreen popup regression also passes managed behavior and
fresh-process PackedScene loading. Actual GPU-renderer output on lavapipe and
compatibility output on SDL software both verify the exclusive chain, themed
blue/green nesting and hide pixels, followed by 64 warmed placement/render frames
with zero owner-thread managed bytes. Both diagnostic images were inspected.
This composition fixture uses Nearest filtering and disables PopupWindow's
application-focus close policy so offscreen focus events do not close its subject;
the managed popup suite retains the default input/close behavior. This does not
establish compositor, physical input or hardware-window performance acceptance.

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

## Project layout direction

Window and Control use one domain-aware resolver. An inherited node follows the nearest Window or Control only while ancestor translation domains match. With no such ancestor, `ProjectSettings.RootNodeLayoutDirection` selects 0 application locale, 1 LTR, 2 RTL or 3 system UI locale. `ForceRightToLeftLayoutDirection` forces inherited and application/system locale modes to RTL; explicit LTR/RTL remain authoritative. Each SceneTree samples both settings and feature overrides before entry. Detached nodes use current settings. Authored per-node directions survive scene packing; project defaults do not overwrite them.

Layout-direction and translation notifications reflow geometry and invalidate text; global locale/catalog edits require an explicit translation notification under ADR 0007. Existing locale direction still requires a matching registered catalog or fallback language. [ProjectLayoutDirectionTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionTests.cs) covers resolution, lifetime and reflow; [ProjectLayoutDirectionNativeTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionNativeTests.cs) checks GUI/text pixels on Linux Wayland GPU/compatibility. Foreign targets and owner visual acceptance remain unverified.

Window content scaling now separates logical visible coordinates, raster target dimensions and client presentation. Both native renderers fit the completed content into a client-sized presentation surface with black margins; the borrowed viewport texture keeps its own raster resolution. Window/embedded input removes the complete presentation transform, while rendering excludes that transform and uses raster scale/density. Automatic text raster scale follows the viewport; explicit oversampling remains authoritative. See [Window content scale](../classes/Window.md#content-scale) and the dedicated unit/native checks for the exact policies and platform limits.
