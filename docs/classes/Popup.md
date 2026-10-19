# Popup

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public class Popup : Window`. **Source:** [Popup.cs](../../src/Scene/GUI/Popup.cs). **Inherits:** [Window](Window.md). **Inherited By:** [PopupPanel](PopupPanel.md), [PopupMenu](PopupMenu.md). **Component:** [Embedded popup windows](../components/popup-windows.md).

A transient embedded popup with deferred cancellation and hide notification.

## Ownership, lifecycle and current profile

Configure `GUIEmbedSubwindows=true` on a containing viewport before attaching child windows. The viewport owns internal presentation layers; each public Window remains in its original scene hierarchy and owns its own GUI state and native render target. Images share the existing renderer/device, use completed native images, and never pass through per-frame CPU readback/upload. Hidden windows remain attached; temporary layers are released at a safe point after detachment or with their viewport during tree teardown. Public child enumeration and packing omit the presentation layers.

Ordinary desktop children use independent native hosts; popup descendants now use the native role described below. Live GUIEmbedSubwindows migration with affected attached children remains unavailable. Embedded chrome retains its existing close, drag and resize behavior.

The host routes pointer, touch and gesture coordinates into the selected window and retains button/contact capture outside its rectangle. Keyboard/controller actions go to the focused child viewport. Popup windows intercept outside clicks; Exclusive blocks dismissal and outside input. Opening and hiding update back-to-front ordering and window focus while preserving separate control-focus owners. Hiding a parent also hides its descendant windows. Exceptions from visibility/focus cleanup are collected after required state transitions. Public PushInput re-entry is rejected before routing callbacks. Text and composition can reach an embedded window's focused control; native IME requests use the containing native root and translated caret position.

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

## Own declaration summary

| Complete declaration | Contract |
| --- | --- |
| `public Popup()` | Creates a hidden, borderless, fixed-size transient popup with wrapped controls. |
| `public event System.Action PopupHide` | Occurs after this popup becomes hidden, before canvas visibility propagation completes. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` |  |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` |  |
| `protected override System.Void OnNotification(System.Int32 what)` |  |

## Member reference

### .ctor

```csharp
public Popup()
```

Creates a hidden, borderless, fixed-size transient popup with wrapped controls.


### PopupHide

```csharp
public event System.Action PopupHide
```

Occurs after this popup becomes hidden, before canvas visibility propagation completes.


### CreateSceneInstanceFactory

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```


### Dispose

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```


### GetPropertyDescriptors

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```


### OnNotification

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

[PopupMenu](PopupMenu.md) adds the executable themed item/search/submenu consumer. Direct popup input now follows inherited deferred cancellation, and internal control focus is assigned after canvas visibility propagation.

## Native popup presentation

Native desktop popups use real parent-relative SDL surfaces when `PopupWMHint` is enabled. `PopupOnParent` accepts parent-client geometry and works without fabricated Wayland desktop positions. Public native `Position`, explicit global `Popup(rect)` and display-centered placement retain the global-position capability guard; centered geometry uses the complete parent display, while fitting uses its usable area. Attached embedded children keep their existing viewport coordinates. Runtime control anchors compose the source viewport transform, including nested and scaled hosts.

The native role is fixed-size and borderless, cannot minimize/maximize/fullscreen, and follows its physical parent's display. Hide before changing native role or transparency. Native children recreate their identity after hide. Retiring or hiding a native parent first retires popup descendants; independent ordinary child windows survive parent hide. Popup visibility observer failures are reported after required retirement and committed visibility. SDL popup-done force-hides without emitting a user `CloseRequested`; ordinary close requests and scene cancellation retain their own behavior.

Transparent native PopupPanel/PopupMenu output executes through the compatibility backend. SDL 3.4.18's GPU API rejects transparent windows; GPU native menus require explicit `Transparent=false` before showing. The unsupported request releases its attempted native lifetime and preserves hidden state. Embedded transparent popups continue to execute on both renderers. Native titled-popup variants without the popup role, live embedding migration, foreign-platform acceptance and native system-menu service retain their exact coverage prerequisites.

`NativePopupWindowTests` uses `ELECTRON2D_TEST_NATIVE_POPUPS=gpu` or `compatibility` with the Release runner. It exercises native nested parenting, IDs, outside dismissal, owner-safe rectangles, no-focus and exclusive policies, forced retirement, callback failure, real menu/option/panel/context output, and fresh-process public scene loading. Active focus/render and idle/render measurements cover 64 owner-thread intervals after 32 warmup intervals. SDL event construction, cold model edits/readback, native allocator counts, compositor screenshot acceptance, physical input and other platforms are outside the measured interval.
