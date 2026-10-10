# PopupMenu

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Declaration:** `public partial class PopupMenu : Popup`. **Inherits:** [Popup](Popup.md). **Inherited By:** —. **Source:** [model](../../src/Scene/GUI/PopupMenu.cs), [layout](../../src/Scene/GUI/PopupMenu.Layout.cs), [input](../../src/Scene/GUI/PopupMenu.Input.cs), [search](../../src/Scene/GUI/PopupMenu.Search.cs), [storage](../../src/Scene/GUI/PopupMenu.Storage.cs). **Component:** [Popup menus](../components/popup-menus.md).

A themed, scrollable embedded command menu with typed items, search and submenus.

## Description

A [MenuBar](MenuBar.md) parent uses four typed private scene fields on each popup for its header title override, tooltip, disabled and hidden state. They follow child identity across scene pruning; top-level horizontal input forwards to the owning strip after submenu handling.

PopupMenu presents themed text, borrowed icons/shortcuts, checkbox/radio decorations, explicit multistate values, titled separators and direct child submenus through the existing embedded Window host. Configure `GUIEmbedSubwindows=true` on the containing viewport before attaching menus. Each menu owns an internal Panel, ScrollContainer, LineEdit, item canvas and one-shot Timer; ordinary child enumeration and packing omit these implementation nodes. Showing requires an attached embedder. The [popup host](../components/popup-windows.md) defines viewport coordinates, modal routing, focus restoration, renderer target ownership and cancellation.

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

## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public PopupMenu()` | Creates a hidden transparent menu with owned internal panel, scroll, search and timer controls. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public System.Boolean AllowSearch { get; set; }` | Gets or sets the AllowSearch policy. |
| `public System.Boolean HideOnCheckableItemSelection { get; set; }` | Gets or sets the HideOnCheckableItemSelection policy. |
| `public System.Boolean HideOnItemSelection { get; set; }` | Gets or sets the HideOnItemSelection policy. |
| `public System.Boolean HideOnStateItemSelection { get; set; }` | Gets or sets the HideOnStateItemSelection policy. |
| `public System.Int32 ItemCount { get; set; }` | Gets or sets the item count, preserving retained records and assigning new IDs from indices. |
| `public System.Boolean SearchBarEnabled { get; set; }` | Gets or sets the SearchBarEnabled policy. |
| `public System.Boolean SearchBarFuzzySearchEnabled { get; set; }` | Gets or sets the SearchBarFuzzySearchEnabled policy. |
| `public System.Int32 SearchBarFuzzySearchMaxMisses { get; set; }` | Gets or sets the nonnegative SearchBarFuzzySearchMaxMisses threshold. |
| `public System.Int32 SearchBarMinItemCount { get; set; }` | Gets or sets the nonnegative SearchBarMinItemCount threshold. |
| `public System.Boolean ShrinkHeight { get; set; }` | Gets or sets the ShrinkHeight policy. |
| `public System.Boolean ShrinkWidth { get; set; }` | Gets or sets the ShrinkWidth policy. |
| `public System.Double SubmenuPopupDelay { get; set; }` | Gets or sets the finite nonnegative submenu hover delay. |

## Methods and protected hooks

| Complete declaration | Contract |
| --- | --- |
| `public System.Boolean ActivateItemByEvent(Electron2D.InputEvent inputEvent, System.Boolean forGlobalOnly = false)` | Activates the first matching shortcut or accelerator, recursively searching submenus. |
| `public System.Void AddCheckItem(System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)` | Adds a checkbox item; ID -1 derives from its new index. |
| `public System.Void AddCheckShortcut(Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false)` | Adds a checkbox item titled from a borrowed shortcut; ID -1 derives from its new index. |
| `public System.Void AddIconCheckItem(Electron2D.Texture icon, System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)` | Adds a checkbox item with an icon; ID -1 derives from its new index. |
| `public System.Void AddIconCheckShortcut(Electron2D.Texture icon, Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false)` | Adds an icon checkbox item titled from a borrowed shortcut; ID -1 derives from its new index. |
| `public System.Void AddIconItem(Electron2D.Texture icon, System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)` | Adds an icon and text item; ID -1 derives from its new index. |
| `public System.Void AddIconRadioCheckItem(Electron2D.Texture icon, System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)` | Adds a radio item with an icon; ID -1 derives from its new index. |
| `public System.Void AddIconRadioCheckShortcut(Electron2D.Texture icon, Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false)` | Adds an icon radio item titled from a borrowed shortcut; ID -1 derives from its new index. |
| `public System.Void AddIconShortcut(Electron2D.Texture icon, Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false, System.Boolean allowEcho = false)` | Adds an icon item titled from a borrowed shortcut; ID -1 derives from its new index. |
| `public System.Void AddItem(System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)` | Adds a text item; ID -1 derives from its new index. |
| `public System.Void AddMultistateItem(System.String label, System.Int32 maxStates, System.Int32 defaultState = 0, System.Int32 id = -1, Electron2D.Key accelerator = None)` | Adds an item with an explicit state range; ID -1 derives from its new index. |
| `public System.Void AddRadioCheckItem(System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)` | Adds a radio item; ID -1 derives from its new index. |
| `public System.Void AddRadioCheckShortcut(Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false)` | Adds a radio item titled from a borrowed shortcut; ID -1 derives from its new index. |
| `public System.Void AddSeparator(System.String label = "", System.Int32 id = -1)` | Adds a separator with an optional title; ID -1 derives from its new index. |
| `public System.Void AddShortcut(Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false, System.Boolean allowEcho = false)` | Adds an item titled from a borrowed shortcut; ID -1 derives from its new index. |
| `public System.Void AddSubmenuNodeItem(System.String label, Electron2D.PopupMenu submenu, System.Int32 id = -1)` | Adds a submenu record, parenting a detached submenu when needed. |
| `public System.Void Clear(System.Boolean freeSubmenus = false)` | Clears items, optionally disposing their distinct submenu nodes. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Projects the inherited lifecycle/scene contract onto the menu owner. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Projects the inherited lifecycle/scene contract onto the menu owner. |
| `public System.Int32 GetFocusedItem()` | Gets the current focused index. |
| `public Electron2D.Key GetItemAccelerator(System.Int32 index)` | Gets an item's Accelerator value. |
| `public Electron2D.NodeAutoTranslateMode GetItemAutoTranslateMode(System.Int32 index)` | Gets an item's AutoTranslateMode value. |
| `public System.Int32 GetItemID(System.Int32 index)` | Gets an item's ID value. |
| `public Electron2D.Texture GetItemIcon(System.Int32 index)` | Gets the item's borrowed icon. |
| `public System.Int32 GetItemIconMaxWidth(System.Int32 index)` | Gets an item's IconMaxWidth value. |
| `public Electron2D.Color GetItemIconModulate(System.Int32 index)` | Gets an item's IconModulate value. |
| `public System.Int32 GetItemIndent(System.Int32 index)` | Gets an item's Indent value. |
| `public System.Int32 GetItemIndex(System.Int32 id)` | Returns the first item index with an exact ID. |
| `public System.String GetItemLanguage(System.Int32 index)` | Gets an item's Language value. |
| `public T GetItemMetadata<T>(System.Int32 index)` | Gets metadata stored with the same exact generic type. |
| `public System.Int32 GetItemMultistate(System.Int32 index)` | Gets an item's Multistate value. |
| `public System.Int32 GetItemMultistateMax(System.Int32 index)` | Gets an item's MultistateMax value. |
| `public Electron2D.Shortcut GetItemShortcut(System.Int32 index)` | Gets the item's borrowed shortcut. |
| `public Electron2D.PopupMenu GetItemSubmenuNode(System.Int32 index)` | Gets a live borrowed submenu. |
| `public System.String GetItemText(System.Int32 index)` | Gets an item's Text value. |
| `public Electron2D.TextDirection GetItemTextDirection(System.Int32 index)` | Gets an item's TextDirection value. |
| `public System.String GetItemTooltip(System.Int32 index)` | Gets an item's Tooltip value. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects the inherited lifecycle/scene contract onto the menu owner. |
| `public System.Boolean IsItemCheckable(System.Int32 index)` | Reports whether an item uses checkbox or radio decoration. |
| `public System.Boolean IsItemChecked(System.Int32 index)` | Reports an item's Checked state. |
| `public System.Boolean IsItemDisabled(System.Int32 index)` | Reports an item's Disabled state. |
| `public System.Boolean IsItemRadioCheckable(System.Int32 index)` | Reports whether an item uses radio decoration. |
| `public System.Boolean IsItemSeparator(System.Int32 index)` | Reports an item's Separator state. |
| `public System.Boolean IsItemShortcutDisabled(System.Int32 index)` | Reports an item's ShortcutDisabled state. |
| `protected override Electron2D.Vector2 OnGetContentsMinimumSize()` | Projects the inherited lifecycle/scene contract onto the menu owner. |
| `protected override System.Void OnInput(Electron2D.InputEvent inputEvent)` | Projects the inherited lifecycle/scene contract onto the menu owner. |
| `protected override System.Void OnNotification(System.Int32 what)` | Projects the inherited lifecycle/scene contract onto the menu owner. |
| `public System.Void RemoveItem(System.Int32 index)` | Removes one record without freeing its submenu. |
| `public System.Void ScrollToItem(System.Int32 index)` | Scrolls the item rectangle fully into the visible menu body. |
| `public System.Void SetFocusedItem(System.Int32 index)` | Changes focus decoration without emitting IDFocused. |
| `public System.Void SetItemAccelerator(System.Int32 index, Electron2D.Key value)` | Sets an item's Accelerator value after validation; negative indices count from the end. |
| `public System.Void SetItemAsCheckable(System.Int32 index, System.Boolean value)` | Sets checkbox decoration, or resets the item to plain text. |
| `public System.Void SetItemAsRadioCheckable(System.Int32 index, System.Boolean value)` | Sets radio decoration, or resets the item to plain text. |
| `public System.Void SetItemAsSeparator(System.Int32 index, System.Boolean value)` | Changes an item's Separator flag. |
| `public System.Void SetItemAutoTranslateMode(System.Int32 index, Electron2D.NodeAutoTranslateMode value)` | Sets an item's AutoTranslateMode value after validation; negative indices count from the end. |
| `public System.Void SetItemChecked(System.Int32 index, System.Boolean value)` | Changes an item's Checked flag. |
| `public System.Void SetItemDisabled(System.Int32 index, System.Boolean value)` | Changes an item's Disabled flag. |
| `public System.Void SetItemID(System.Int32 index, System.Int32 value)` | Sets an item's ID value after validation; negative indices count from the end. |
| `public System.Void SetItemIcon(System.Int32 index, Electron2D.Texture icon)` | Replaces a borrowed icon and its invalidation subscription. |
| `public System.Void SetItemIconMaxWidth(System.Int32 index, System.Int32 value)` | Sets an item's IconMaxWidth value after validation; negative indices count from the end. |
| `public System.Void SetItemIconModulate(System.Int32 index, Electron2D.Color value)` | Sets an item's IconModulate value after validation; negative indices count from the end. |
| `public System.Void SetItemIndent(System.Int32 index, System.Int32 value)` | Sets an item's Indent value after validation; negative indices count from the end. |
| `public System.Void SetItemIndex(System.Int32 index, System.Int32 targetIndex)` | Moves a record while preserving its ID and resource identities. |
| `public System.Void SetItemLanguage(System.Int32 index, System.String value)` | Sets an item's Language value after validation; negative indices count from the end. |
| `public System.Void SetItemMetadata<T>(System.Int32 index, T value)` | Sets runtime metadata with an exact generic type. |
| `public System.Void SetItemMultistate(System.Int32 index, System.Int32 value)` | Sets an item's Multistate value after validation; negative indices count from the end. |
| `public System.Void SetItemMultistateMax(System.Int32 index, System.Int32 value)` | Sets an item's MultistateMax value after validation; negative indices count from the end. |
| `public System.Void SetItemShortcut(System.Int32 index, Electron2D.Shortcut shortcut, System.Boolean global = false)` | Replaces a borrowed shortcut and its global matching policy. |
| `public System.Void SetItemShortcutDisabled(System.Int32 index, System.Boolean value)` | Changes an item's ShortcutDisabled flag. |
| `public System.Void SetItemSubmenuNode(System.Int32 index, Electron2D.PopupMenu submenu)` | Changes the submenu identity, parenting a detached child. |
| `public System.Void SetItemText(System.Int32 index, System.String value)` | Sets an item's Text value after validation; negative indices count from the end. |
| `public System.Void SetItemTextDirection(System.Int32 index, Electron2D.TextDirection value)` | Sets an item's TextDirection value after validation; negative indices count from the end. |
| `public System.Void SetItemTooltip(System.Int32 index, System.String value)` | Sets an item's Tooltip value after validation; negative indices count from the end. |
| `public System.Void ToggleItemChecked(System.Int32 index)` | Toggles the stored check state without activation. |
| `public System.Void ToggleItemMultistate(System.Int32 index)` | Advances a positive-range multistate item and wraps to zero. |

## Events

| Complete declaration | Contract |
| --- | --- |
| `public event System.Action<System.Int32> IDFocused` | Occurs when keyboard or controller navigation focuses an eligible item. |
| `public event System.Action<System.Int32> IDPressed` | Occurs after an eligible item's popup-hide policy completes, carrying its ID. |
| `public event System.Action<System.Int32> IndexPressed` | Occurs after IDPressed, carrying the activated zero-based item index. |
| `public event System.Action MenuChanged` | Occurs after an item mutation commits. |

## Constructors descriptions

### .ctor

```csharp
public PopupMenu()
```

Creates a hidden transparent menu with owned internal panel, scroll, search and timer controls.


## Properties descriptions

### AllowSearch

```csharp
public System.Boolean AllowSearch { get; set; }
```

Gets or sets the AllowSearch policy.

true initially.


### HideOnCheckableItemSelection

```csharp
public System.Boolean HideOnCheckableItemSelection { get; set; }
```

Gets or sets the HideOnCheckableItemSelection policy.

true initially.


### HideOnItemSelection

```csharp
public System.Boolean HideOnItemSelection { get; set; }
```

Gets or sets the HideOnItemSelection policy.

true initially.


### HideOnStateItemSelection

```csharp
public System.Boolean HideOnStateItemSelection { get; set; }
```

Gets or sets the HideOnStateItemSelection policy.

false initially.


### ItemCount

```csharp
public System.Int32 ItemCount { get; set; }
```

Gets or sets the item count, preserving retained records and assigning new IDs from indices.

Zero initially; between zero and 65536.


Throws `System.ArgumentOutOfRangeException`: The count is outside the supported range.

### SearchBarEnabled

```csharp
public System.Boolean SearchBarEnabled { get; set; }
```

Gets or sets the SearchBarEnabled policy.

false initially.


### SearchBarFuzzySearchEnabled

```csharp
public System.Boolean SearchBarFuzzySearchEnabled { get; set; }
```

Gets or sets the SearchBarFuzzySearchEnabled policy.

true initially.


### SearchBarFuzzySearchMaxMisses

```csharp
public System.Int32 SearchBarFuzzySearchMaxMisses { get; set; }
```

Gets or sets the nonnegative SearchBarFuzzySearchMaxMisses threshold.

2 initially.


Throws `System.ArgumentOutOfRangeException`: The value is negative.

### SearchBarMinItemCount

```csharp
public System.Int32 SearchBarMinItemCount { get; set; }
```

Gets or sets the nonnegative SearchBarMinItemCount threshold.

0 initially.


Throws `System.ArgumentOutOfRangeException`: The value is negative.

### ShrinkHeight

```csharp
public System.Boolean ShrinkHeight { get; set; }
```

Gets or sets the ShrinkHeight policy.

true initially.


### ShrinkWidth

```csharp
public System.Boolean ShrinkWidth { get; set; }
```

Gets or sets the ShrinkWidth policy.

true initially.


### SubmenuPopupDelay

```csharp
public System.Double SubmenuPopupDelay { get; set; }
```

Gets or sets the finite nonnegative submenu hover delay.

0.2 seconds initially.


## Methods and protected hooks descriptions

### ActivateItemByEvent

```csharp
public System.Boolean ActivateItemByEvent(Electron2D.InputEvent inputEvent, System.Boolean forGlobalOnly = false)
```

Activates the first matching shortcut or accelerator, recursively searching submenus.

True when an enabled item is activated.

- `inputEvent`: A live borrowed event.
- `forGlobalOnly`: Restricts shortcut matching to global shortcuts.

### AddCheckItem

```csharp
public System.Void AddCheckItem(System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)
```

Adds a checkbox item; ID -1 derives from its new index.

- `label`: The typed item label value.
- `id`: The typed item id value.
- `accelerator`: The typed item accelerator value.

### AddCheckShortcut

```csharp
public System.Void AddCheckShortcut(Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false)
```

Adds a checkbox item titled from a borrowed shortcut; ID -1 derives from its new index.

- `shortcut`: The typed item shortcut value.
- `id`: The typed item id value.
- `global`: The typed item global value.

### AddIconCheckItem

```csharp
public System.Void AddIconCheckItem(Electron2D.Texture icon, System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)
```

Adds a checkbox item with an icon; ID -1 derives from its new index.

- `icon`: The typed item icon value.
- `label`: The typed item label value.
- `id`: The typed item id value.
- `accelerator`: The typed item accelerator value.

### AddIconCheckShortcut

```csharp
public System.Void AddIconCheckShortcut(Electron2D.Texture icon, Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false)
```

Adds an icon checkbox item titled from a borrowed shortcut; ID -1 derives from its new index.

- `icon`: The typed item icon value.
- `shortcut`: The typed item shortcut value.
- `id`: The typed item id value.
- `global`: The typed item global value.

### AddIconItem

```csharp
public System.Void AddIconItem(Electron2D.Texture icon, System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)
```

Adds an icon and text item; ID -1 derives from its new index.

- `icon`: The typed item icon value.
- `label`: The typed item label value.
- `id`: The typed item id value.
- `accelerator`: The typed item accelerator value.

### AddIconRadioCheckItem

```csharp
public System.Void AddIconRadioCheckItem(Electron2D.Texture icon, System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)
```

Adds a radio item with an icon; ID -1 derives from its new index.

- `icon`: The typed item icon value.
- `label`: The typed item label value.
- `id`: The typed item id value.
- `accelerator`: The typed item accelerator value.

### AddIconRadioCheckShortcut

```csharp
public System.Void AddIconRadioCheckShortcut(Electron2D.Texture icon, Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false)
```

Adds an icon radio item titled from a borrowed shortcut; ID -1 derives from its new index.

- `icon`: The typed item icon value.
- `shortcut`: The typed item shortcut value.
- `id`: The typed item id value.
- `global`: The typed item global value.

### AddIconShortcut

```csharp
public System.Void AddIconShortcut(Electron2D.Texture icon, Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false, System.Boolean allowEcho = false)
```

Adds an icon item titled from a borrowed shortcut; ID -1 derives from its new index.

- `icon`: The typed item icon value.
- `shortcut`: The typed item shortcut value.
- `id`: The typed item id value.
- `global`: The typed item global value.
- `allowEcho`: The typed item allowEcho value.

### AddItem

```csharp
public System.Void AddItem(System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)
```

Adds a text item; ID -1 derives from its new index.

- `label`: The typed item label value.
- `id`: The typed item id value.
- `accelerator`: The typed item accelerator value.

### AddMultistateItem

```csharp
public System.Void AddMultistateItem(System.String label, System.Int32 maxStates, System.Int32 defaultState = 0, System.Int32 id = -1, Electron2D.Key accelerator = None)
```

Adds an item with an explicit state range; ID -1 derives from its new index.

- `label`: The typed item label value.
- `maxStates`: The typed item maxStates value.
- `defaultState`: The typed item defaultState value.
- `id`: The typed item id value.
- `accelerator`: The typed item accelerator value.

### AddRadioCheckItem

```csharp
public System.Void AddRadioCheckItem(System.String label, System.Int32 id = -1, Electron2D.Key accelerator = None)
```

Adds a radio item; ID -1 derives from its new index.

- `label`: The typed item label value.
- `id`: The typed item id value.
- `accelerator`: The typed item accelerator value.

### AddRadioCheckShortcut

```csharp
public System.Void AddRadioCheckShortcut(Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false)
```

Adds a radio item titled from a borrowed shortcut; ID -1 derives from its new index.

- `shortcut`: The typed item shortcut value.
- `id`: The typed item id value.
- `global`: The typed item global value.

### AddSeparator

```csharp
public System.Void AddSeparator(System.String label = "", System.Int32 id = -1)
```

Adds a separator with an optional title; ID -1 derives from its new index.

- `label`: The typed item label value.
- `id`: The typed item id value.

### AddShortcut

```csharp
public System.Void AddShortcut(Electron2D.Shortcut shortcut, System.Int32 id = -1, System.Boolean global = false, System.Boolean allowEcho = false)
```

Adds an item titled from a borrowed shortcut; ID -1 derives from its new index.

- `shortcut`: The typed item shortcut value.
- `id`: The typed item id value.
- `global`: The typed item global value.
- `allowEcho`: The typed item allowEcho value.

### AddSubmenuNodeItem

```csharp
public System.Void AddSubmenuNodeItem(System.String label, Electron2D.PopupMenu submenu, System.Int32 id = -1)
```

Adds a submenu record, parenting a detached submenu when needed.

- `label`: The title.
- `submenu`: A direct child or detached menu.
- `id`: An ID, or -1 for automatic.

### Clear

```csharp
public System.Void Clear(System.Boolean freeSubmenus = false)
```

Clears items, optionally disposing their distinct submenu nodes.

- `freeSubmenus`: Whether to detach and free submenu nodes.

### CreateSceneInstanceFactory

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

Projects the inherited hook onto internal menu layout/input, storage, construction or owned-state cleanup.

### Dispose

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

Projects the inherited hook onto internal menu layout/input, storage, construction or owned-state cleanup.

### GetFocusedItem

```csharp
public System.Int32 GetFocusedItem()
```

Gets the current focused index.

Minus one when no item is focused.


### GetItemAccelerator

```csharp
public Electron2D.Key GetItemAccelerator(System.Int32 index)
```

Gets an item's Accelerator value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemAutoTranslateMode

```csharp
public Electron2D.NodeAutoTranslateMode GetItemAutoTranslateMode(System.Int32 index)
```

Gets an item's AutoTranslateMode value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemID

```csharp
public System.Int32 GetItemID(System.Int32 index)
```

Gets an item's ID value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemIcon

```csharp
public Electron2D.Texture GetItemIcon(System.Int32 index)
```

Gets the item's borrowed icon.

The icon or null.

- `index`: An index.

### GetItemIconMaxWidth

```csharp
public System.Int32 GetItemIconMaxWidth(System.Int32 index)
```

Gets an item's IconMaxWidth value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemIconModulate

```csharp
public Electron2D.Color GetItemIconModulate(System.Int32 index)
```

Gets an item's IconModulate value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemIndent

```csharp
public System.Int32 GetItemIndent(System.Int32 index)
```

Gets an item's Indent value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemIndex

```csharp
public System.Int32 GetItemIndex(System.Int32 id)
```

Returns the first item index with an exact ID.

The index or minus one.

- `id`: The signed ID.

### GetItemLanguage

```csharp
public System.String GetItemLanguage(System.Int32 index)
```

Gets an item's Language value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemMetadata

```csharp
public T GetItemMetadata<T>(System.Int32 index)
```

Gets metadata stored with the same exact generic type.

The retained value.

- `index`: A nonnegative index.
- `T`: The stored type.

Throws `System.Collections.Generic.KeyNotFoundException`: No value of this exact type was stored.

### GetItemMultistate

```csharp
public System.Int32 GetItemMultistate(System.Int32 index)
```

Gets an item's Multistate value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemMultistateMax

```csharp
public System.Int32 GetItemMultistateMax(System.Int32 index)
```

Gets an item's MultistateMax value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemShortcut

```csharp
public Electron2D.Shortcut GetItemShortcut(System.Int32 index)
```

Gets the item's borrowed shortcut.

The shortcut or null.

- `index`: An index.

### GetItemSubmenuNode

```csharp
public Electron2D.PopupMenu GetItemSubmenuNode(System.Int32 index)
```

Gets a live borrowed submenu.

A direct submenu, or null.

- `index`: An index.

### GetItemText

```csharp
public System.String GetItemText(System.Int32 index)
```

Gets an item's Text value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemTextDirection

```csharp
public Electron2D.TextDirection GetItemTextDirection(System.Int32 index)
```

Gets an item's TextDirection value.

The stored typed value.

- `index`: A nonnegative item index.

### GetItemTooltip

```csharp
public System.String GetItemTooltip(System.Int32 index)
```

Gets an item's Tooltip value.

The stored typed value.

- `index`: A nonnegative item index.

### GetPropertyDescriptors

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the inherited hook onto internal menu layout/input, storage, construction or owned-state cleanup.

### IsItemCheckable

```csharp
public System.Boolean IsItemCheckable(System.Int32 index)
```

Reports whether an item uses checkbox or radio decoration.

True for either checkable role.

- `index`: An index.

### IsItemChecked

```csharp
public System.Boolean IsItemChecked(System.Int32 index)
```

Reports an item's Checked state.

The stored flag.

- `index`: A nonnegative index.

### IsItemDisabled

```csharp
public System.Boolean IsItemDisabled(System.Int32 index)
```

Reports an item's Disabled state.

The stored flag.

- `index`: A nonnegative index.

### IsItemRadioCheckable

```csharp
public System.Boolean IsItemRadioCheckable(System.Int32 index)
```

Reports whether an item uses radio decoration.

True for the radio role.

- `index`: An index.

### IsItemSeparator

```csharp
public System.Boolean IsItemSeparator(System.Int32 index)
```

Reports an item's Separator state.

The stored flag.

- `index`: A nonnegative index.

### IsItemShortcutDisabled

```csharp
public System.Boolean IsItemShortcutDisabled(System.Int32 index)
```

Reports an item's ShortcutDisabled state.

The stored flag.

- `index`: A nonnegative index.

### OnGetContentsMinimumSize

```csharp
protected override Electron2D.Vector2 OnGetContentsMinimumSize()
```

Projects the inherited hook onto internal menu layout/input, storage, construction or owned-state cleanup.

### OnInput

```csharp
protected override System.Void OnInput(Electron2D.InputEvent inputEvent)
```

Projects the inherited hook onto internal menu layout/input, storage, construction or owned-state cleanup.

### OnNotification

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

Projects the inherited hook onto internal menu layout/input, storage, construction or owned-state cleanup.

### RemoveItem

```csharp
public System.Void RemoveItem(System.Int32 index)
```

Removes one record without freeing its submenu.

- `index`: A nonnegative index.

### ScrollToItem

```csharp
public System.Void ScrollToItem(System.Int32 index)
```

Scrolls the item rectangle fully into the visible menu body.

- `index`: A nonnegative item index.

### SetFocusedItem

```csharp
public System.Void SetFocusedItem(System.Int32 index)
```

Changes focus decoration without emitting IDFocused.

- `index`: An index or -1 to clear.

### SetItemAccelerator

```csharp
public System.Void SetItemAccelerator(System.Int32 index, Electron2D.Key value)
```

Sets an item's Accelerator value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemAsCheckable

```csharp
public System.Void SetItemAsCheckable(System.Int32 index, System.Boolean value)
```

Sets checkbox decoration, or resets the item to plain text.

- `index`: An index, optionally negative.
- `value`: Whether it is checkable.

### SetItemAsRadioCheckable

```csharp
public System.Void SetItemAsRadioCheckable(System.Int32 index, System.Boolean value)
```

Sets radio decoration, or resets the item to plain text.

- `index`: An index, optionally negative.
- `value`: Whether it is radio checkable.

### SetItemAsSeparator

```csharp
public System.Void SetItemAsSeparator(System.Int32 index, System.Boolean value)
```

Changes an item's Separator flag.

- `index`: An index, optionally negative.
- `value`: The desired state.

### SetItemAutoTranslateMode

```csharp
public System.Void SetItemAutoTranslateMode(System.Int32 index, Electron2D.NodeAutoTranslateMode value)
```

Sets an item's AutoTranslateMode value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemChecked

```csharp
public System.Void SetItemChecked(System.Int32 index, System.Boolean value)
```

Changes an item's Checked flag.

- `index`: An index, optionally negative.
- `value`: The desired state.

### SetItemDisabled

```csharp
public System.Void SetItemDisabled(System.Int32 index, System.Boolean value)
```

Changes an item's Disabled flag.

- `index`: An index, optionally negative.
- `value`: The desired state.

### SetItemID

```csharp
public System.Void SetItemID(System.Int32 index, System.Int32 value)
```

Sets an item's ID value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemIcon

```csharp
public System.Void SetItemIcon(System.Int32 index, Electron2D.Texture icon)
```

Replaces a borrowed icon and its invalidation subscription.

- `index`: An index, optionally negative.
- `icon`: A live borrowed icon or null.

### SetItemIconMaxWidth

```csharp
public System.Void SetItemIconMaxWidth(System.Int32 index, System.Int32 value)
```

Sets an item's IconMaxWidth value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemIconModulate

```csharp
public System.Void SetItemIconModulate(System.Int32 index, Electron2D.Color value)
```

Sets an item's IconModulate value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemIndent

```csharp
public System.Void SetItemIndent(System.Int32 index, System.Int32 value)
```

Sets an item's Indent value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemIndex

```csharp
public System.Void SetItemIndex(System.Int32 index, System.Int32 targetIndex)
```

Moves a record while preserving its ID and resource identities.

- `index`: A source index.
- `targetIndex`: A destination index.

### SetItemLanguage

```csharp
public System.Void SetItemLanguage(System.Int32 index, System.String value)
```

Sets an item's Language value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemMetadata

```csharp
public System.Void SetItemMetadata<T>(System.Int32 index, T value)
```

Sets runtime metadata with an exact generic type.

- `index`: An index, optionally negative.
- `value`: The borrowed value, including typed null.
- `T`: The value type.

### SetItemMultistate

```csharp
public System.Void SetItemMultistate(System.Int32 index, System.Int32 value)
```

Sets an item's Multistate value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemMultistateMax

```csharp
public System.Void SetItemMultistateMax(System.Int32 index, System.Int32 value)
```

Sets an item's MultistateMax value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemShortcut

```csharp
public System.Void SetItemShortcut(System.Int32 index, Electron2D.Shortcut shortcut, System.Boolean global = false)
```

Replaces a borrowed shortcut and its global matching policy.

- `index`: An index, optionally negative.
- `shortcut`: A live shortcut or null.
- `global`: Whether global-only requests may activate it.

### SetItemShortcutDisabled

```csharp
public System.Void SetItemShortcutDisabled(System.Int32 index, System.Boolean value)
```

Changes an item's ShortcutDisabled flag.

- `index`: An index, optionally negative.
- `value`: The desired state.

### SetItemSubmenuNode

```csharp
public System.Void SetItemSubmenuNode(System.Int32 index, Electron2D.PopupMenu submenu)
```

Changes the submenu identity, parenting a detached child.

- `index`: An index, optionally negative.
- `submenu`: A direct or detached menu, or null.

### SetItemText

```csharp
public System.Void SetItemText(System.Int32 index, System.String value)
```

Sets an item's Text value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemTextDirection

```csharp
public System.Void SetItemTextDirection(System.Int32 index, Electron2D.TextDirection value)
```

Sets an item's TextDirection value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### SetItemTooltip

```csharp
public System.Void SetItemTooltip(System.Int32 index, System.String value)
```

Sets an item's Tooltip value after validation; negative indices count from the end.

- `index`: An item index.
- `value`: The new typed value.

### ToggleItemChecked

```csharp
public System.Void ToggleItemChecked(System.Int32 index)
```

Toggles the stored check state without activation.

- `index`: A nonnegative index.

### ToggleItemMultistate

```csharp
public System.Void ToggleItemMultistate(System.Int32 index)
```

Advances a positive-range multistate item and wraps to zero.

- `index`: A nonnegative index.

## Events descriptions

### IDFocused

```csharp
public event System.Action<System.Int32> IDFocused
```

Occurs when keyboard or controller navigation focuses an eligible item.


### IDPressed

```csharp
public event System.Action<System.Int32> IDPressed
```

Occurs after an eligible item's popup-hide policy completes, carrying its ID.


### IndexPressed

```csharp
public event System.Action<System.Int32> IndexPressed
```

Occurs after IDPressed, carrying the activated zero-based item index.


### MenuChanged

```csharp
public event System.Action MenuChanged
```

Occurs after an item mutation commits.


[Dropdown choices](../components/dropdown-choices.md) add OptionButton as an executable Button/PopupMenu consumer with three arrow theme keys and an exact scene/file factory. Selected item translation uses the shared Button text path. Disposed borrowed button icons read as null and clear on owner processing, avoiding the internal-process/deferred-cleanup race. Public shared-owner signatures remain unchanged.

The string-path submenu declarations were removed under ADR 0004 because their pinned source metadata is deprecated. Use the node-based AddSubmenuNodeItem/GetItemSubmenuNode/SetItemSubmenuNode family; coverage retains those three source rows as Excluded.

Under a GraphElement with ScalingMenus enabled, popup content and routed pointer coordinates follow GraphEdit.Zoom through the existing viewport canvas transform. The physical popup footprint, shadows and submenu placement scale with it; ordinary menus retain scale one. See [graph authoring](../components/graph-authoring.md).
