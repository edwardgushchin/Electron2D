# Popup menus

Last updated: 2026-10-06

Owns [PopupMenu](../classes/PopupMenu.md) under ADRs 0004, 0008, 0038 and 0083, using existing popup targets, text layout, themes, resources, scroll/search controls and Timer.

## Description

PopupMenu presents themed text, borrowed icons/shortcuts, checkbox/radio decorations, explicit multistate values, titled separators and direct child submenus through the existing embedded Window host. Configure `GUIEmbedSubwindows=true` on the containing viewport before attaching menus. Each menu owns an internal Panel, ScrollContainer, LineEdit, item canvas and one-shot Timer; ordinary child enumeration and packing omit these implementation nodes. Showing requires an attached embedder. The [popup host](popup-windows.md) defines viewport coordinates, modal routing, focus restoration, renderer target ownership and cancellation.

IDs are signed integers independent of current indices. ID -1 on insertion assigns the new index; activation falls back to the current index for other negative IDs. Getters, toggles, removal and focus use nonnegative indices; item setters accept indices counted from the end. `SetFocusedItem(-1)` clears focus. Reordering preserves the item and borrowed resource identities. Check/radio/state changes are explicit; activation does not toggle them. State/range values remain caller-controlled; positive-range toggling wraps at the upper bound. ItemCount supports 0 through 65536 and adds empty records with automatic IDs. Public operations reject disposed menus and attached mutations require the tree owner thread.

Eligible navigation wraps through enabled, non-separator, search-visible records. Exact ui_up/down/accept/right/left actions provide keyboard and controller input, including held-controller repeat. Right opens a submenu; Left returns to its parent. Mouse/touch activation preserves the pressed record and rejects a release on a different record. Initially held mouse buttons allow opening and selection in one gesture, with a 400 ms opening guard. Hover starts the owned submenu timer; movement toward its edge suspends closure for up to 0.5 seconds. Submenus prefer the appropriate left/right side and fit the usable parent rectangle. Borrowed submenus must be detached or direct children and cannot form cycles; detached ones are parented automatically. Clear/RemoveItem retain submenu nodes unless `Clear(true)` explicitly frees their distinct owners.

Activation applies the corresponding ordinary/check/state hide policy to this menu and eligible menu ancestors before IDPressed and IndexPressed. Required hide and event transitions still run when an observer throws, with failures aggregated afterward. IDFocused reports keyboard/controller navigation; programmatic focus and pointer hover do not emit it. MenuChanged follows committed item mutations. `ActivateItemByEvent` recursively matches enabled borrowed shortcuts and modifier-aware accelerators, ignores disabled shortcuts and disallowed echo, and restricts only shortcut matching for global-only requests. Icons/shortcuts remain caller-owned; resource changes invalidate layout and worker disposal is marshaled onto the owner thread. Accessors do not return disposed resources.

AllowSearch enables prefix navigation when the search field is hidden. The visible search field is independently enabled and uses the count of non-separator records for its minimum threshold. Search filters raw titles and recursively includes matching submenu branches. Exact matching uses contiguous tokens; fuzzy matching uses lowercase Unicode scalars, longest-token-first eager subsequences, a shared miss budget within each title, nonoverlapping token intervals and score culling. Check state, IDs and metadata survive filtering. Changing fuzzy policy or miss count reapplies the current query. Search resets when the menu hides or opens.

Theme fonts shape titles and accelerator labels through the shared TextLayout pipeline, including item language, direction and translation policy. Layout reserves icon/check/accelerator/submenu gutters, honors the tighter positive theme/item icon width limit and supports RTL. ShrinkWidth/ShrinkHeight control content fitting at popup time. The embedded viewport limits height; ScrollToItem exposes rows through the vertical scroll owner. Flat panel shadows expand the visible rectangle and restore the content rectangle after hiding, preserving an explicit visible resize. Theme, translation and direction notifications invalidate measurement.

## Example

```csharp
var root = new Window { Size = new(640, 360), GUIEmbedSubwindows = true };
var menu = new PopupMenu { Name = "Commands" };
menu.AddItem("Open", 10, Key.O);
menu.AddCheckItem("Show grid", 20);
menu.IDPressed += id => {
    if (id == 20) menu.ToggleItemChecked(menu.GetItemIndex(id));
};
root.AddChild(menu);
root.Ready += _ => menu.PopupCentered();
Engine.Run(root);
```

## Persistence and limits

The exact built-in PackedScene/file factory recreates the item count before seven indexed fields: text, ID, icon, checkable role, checked, disabled and separator. Scalar menu policies and inherited Window properties are stored. Generic metadata, shortcuts, item language/translation/direction, icon modulation/width, tooltip, indentation, state/range, accelerator and submenu bindings remain runtime configuration; child submenu nodes may be owned/packed separately, then rebound by the consumer. Internal controls/timers are reconstructed independently. Native/system menu registration requires the accepted ADR 0041 service decision and backend; its four declarations remain Blocked. Independent native child windows, live embedding-policy migration, inherited decoration parity, accessibility and editor/platform integrations remain distinct dependencies in coverage.

PopupMenuTests verifies creation families, typed null metadata, strict/end-relative indices, identity IDs, shortcut/global activation, routed pointer/touch, hover timing, long-menu scrolling, scalar/exact/fuzzy search, controller repeat/release, worker resource disposal, callback failure, scene packing and fresh-process execution. PopupMenuRenderingTests exercises native SDL keyboard input, shaped text/icon/check/radio/separator/selection and submenu composition on current Linux Wayland GPU and compatibility backends. Diagnostic images were visually inspected. Each backend measures zero managed bytes over 64 warmed check/radio focus/layout/render frames after 32 warmup frames; cold search/model edits and native allocations are outside that interval. Physical device input, other platforms and user acceptance remain unverified.

[TabContainer](tab-panels.md) now consumes the menu prerequisite through its header button. [OptionButton](dropdown-choices.md) now consumes menu items, selection, search and shortcuts. MenuButton and LineEdit context actions remain separate executable consumers.

Submenu binding now exposes only the node-based family. Three deprecated string-path declarations are removed under the current ADR 0004; their reference rows remain Excluded with the exact replacement.
