# OptionButton

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public partial class OptionButton : Button`. **Inherits:** [Button](Button.md). **Inherited By:** —. **Source:** [model](../../src/Scene/GUI/OptionButton.cs), [layout](../../src/Scene/GUI/OptionButton.Layout.cs), [storage](../../src/Scene/GUI/OptionButton.Storage.cs). **Component:** [Dropdown choices](../components/dropdown-choices.md).

Displays one choice with an owned themed dropdown menu.

## Description

OptionButton : Button displays a selected item and owns one internal PopupMenu. The button starts in toggle mode with leading text alignment and press-edge activation. The menu is a stable borrowed object returned by GetPopup; ordinary child enumeration and packing omit it. Items own their records and borrow icons/shortcuts. IDs are signed 32-bit values independent of indices; insertion ID -1 derives from the new index. The item limit is 65,536. Exact generic metadata supports typed null and remains runtime-only; wrong/missing metadata or absent selected metadata throws KeyNotFoundException. Attached operations follow the tree owner/capture/disposal guards.

AddItem/AddIconItem create radio records and select the first selectable item without a user event; separators do not become the first selectable item. Enabled nonseparator records determine HasSelectableItems/GetSelectableItem. Programmatic Select accepts existing disabled/separator records or -1, updates radio checks and raw caption/icon, and emits no ItemSelected. Selected queues an initial out-of-range assignment until the first configured count; subsequent invalid assignments throw. Count growth retains selection, shrinking clamps a removed selection to the remaining last record, and RemoveItem clears a removed selected record. The selected identity survives removal of preceding items and direct popup reordering. Clear removes items and caption. This corrects the pinned stale-index removal and first-count growth reset rather than selecting a replacement accidentally.

User activation applies the menu's hide policy before selection delivery. ItemSelected reports the committed index only when attached; AllowReselect controls delivery for equal user choices. ItemFocused maps the menu's ID focus signal to its first matching index, so duplicate IDs follow that same first-ID behavior. Public item setters accept end-relative indices through PopupMenu, while getters/removal are strict; GetItemID(-1) returns -1. Current text/icon edits, direct popup changes and icon disposal refresh the caption. Required first-item selection, radio/caption/property-list work and subsequent selection delivery continue after observer failures, reporting aggregated errors afterward. Nested selection supersedes obsolete outer radio/caption updates.

ShowPopup opens beneath the button in viewport coordinates, retaining at least button width and fitting the shared menu height. An embedded Window host requires GUIEmbedSubwindows; independent native children remain an inherited popup prerequisite. Detached buttons return without opening. Keyboard/programmatic opening focuses the selected enabled nonseparator item or first selectable record after menu preparation; pointer opening scrolls to it and preserves grabbed-click behavior. The popup owns its panel, scroll, real search field and timer. Hiding the popup resets pressed toggle state; hiding the button hides its menu. Failed press-triggered presentation resets transient toggle state. Menu item shortcuts/accelerators use ordinary shortcut input when visible, enabled and non-echo; SetDisableShortcuts gates that stage, including the inherited button shortcut. Search policies forward to the owned real menu.

FitToLongestItem measures every translated item using the button font, icon policy and retained text layouts, adds the themed arrow and ensures width is at least the menu minimum. Turning it off uses the current Button content minimum. Selected translation follows each item's Inherit/Always/Disabled policy through the existing node domain. The arrow reserves the logical trailing margin, mirrors in RTL and supports state-font modulation through arrow, arrow_margin and modulate_arrow. Those three own theme keys augment inherited Button themes; fonts, icons and styles stay borrowed. A shared Button polling correction clears a disposed borrowed icon before internal processing and exposes it as null instead of throwing while deferred cleanup is pending.

## Example

```csharp
var root = new Window { Size = new(640, 360), GUIEmbedSubwindows = true };
var choice = new OptionButton { Name = "Quality", Position = new(20, 20) };
choice.AddItem("Low", 10);
choice.AddItem("High", 20);
choice.ItemSelected += index => Console.WriteLine(choice.GetItemID(index));
root.AddChild(choice);
Engine.Run(root);
```

## Persistence, integration and limits

PackedScene and the exact built-in file factory restore scalar policies, ItemCount, five indexed popup fields (text/icon/ID/disabled/separator), then Selected and its caption/radio state. Count creates real radio records before indexed restore. Text/Icon inherited descriptors are omitted because selection owns them; ActionMode/Alignment/ToggleMode restore their concrete defaults. Each scene instance recreates its independent internal menu. Metadata, item tooltip/translation mode, shortcuts and transient pressed/focus/input state remain runtime configuration. ResourceSaver/ResourceLoader fresh-process tests execute the reconstructed dropdown.

OptionButtonTests verifies defaults, first selectable/radio state, exact metadata, selected identity under remove/grow/shrink, end-relative edits, caption/icon lifetime, programmatic versus user/reselect events, focus/navigation, real popup/search/shortcut input, owner guards, nested/throwing observers and fresh scenes. Native tests use actual SDL Space/Down/Return events to open/navigate/activate, read back caption/icon/arrow and radio popup pixels on current Linux Wayland GPU and compatibility backends, and produce visually inspected PNGs. On each backend, 32 warmup plus 64 active selection/measurement/caption/render frames measure zero managed bytes. Cold structural changes, readback, native allocations, physical devices, other platforms and owner acceptance remain outside that result. The full own API executes; inherited accessibility/editor/native popup and migration work retains its separate coverage.

## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public OptionButton()` | Creates an empty toggle button with leading alignment, press-edge activation and an internal menu. |
| `public OptionButton(System.String text)` | Creates an empty choice button with initial source text. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public System.Boolean AllowReselect { get; set; }` | Gets or sets whether activating the current item reports selection again. |
| `public System.Boolean FitToLongestItem { get; set; }` | Gets or sets whether the longest item and popup width contribute to the button minimum. |
| `public System.Int32 ItemCount { get; set; }` | Gets or sets the item count, retaining records and selected identity. |
| `public System.Boolean SearchBarEnabled { get; set; }` | Gets or sets the owned menu's SearchBarEnabled policy. |
| `public System.Boolean SearchBarFuzzySearchEnabled { get; set; }` | Gets or sets the owned menu's SearchBarFuzzySearchEnabled policy. |
| `public System.Int32 SearchBarFuzzySearchMaxMisses { get; set; }` | Gets or sets the owned menu's SearchBarFuzzySearchMaxMisses policy. |
| `public System.Int32 SearchBarMinItemCount { get; set; }` | Gets or sets the owned menu's SearchBarMinItemCount policy. |
| `public System.Int32 Selected { get; set; }` | Gets or sets the selected index without a user-selection event; an initial out-of-range assignment queues. |

## Methods and protected hooks

| Complete declaration | Contract |
| --- | --- |
| `public System.Void AddIconItem(Electron2D.Texture icon, System.String label, System.Int32 id = -1)` | Adds a radio item with a borrowed icon. |
| `public System.Void AddItem(System.String label, System.Int32 id = -1)` | Adds a radio item, selecting it if it is the first selectable record. |
| `public System.Void AddSeparator(System.String text = "")` | Adds a nonselectable separator with an optional source title. |
| `public System.Void Clear()` | Clears item records, selection and caption without ItemSelected. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Projects inherited button/menu lifecycle, layout, storage or input. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Projects inherited button/menu lifecycle, layout, storage or input. |
| `public Electron2D.NodeAutoTranslateMode GetItemAutoTranslateMode(System.Int32 index)` | Gets an item's AutoTranslateMode value. |
| `public System.Int32 GetItemID(System.Int32 index)` | Gets an item's ID value. |
| `public Electron2D.Texture GetItemIcon(System.Int32 index)` | Gets an item's Icon value. |
| `public System.Int32 GetItemIndex(System.Int32 id)` | Gets the first record index with an exact ID. |
| `public T GetItemMetadata<T>(System.Int32 index)` | Gets runtime metadata with its exact generic type. |
| `public System.String GetItemText(System.Int32 index)` | Gets an item's Text value. |
| `public System.String GetItemTooltip(System.Int32 index)` | Gets an item's Tooltip value. |
| `public Electron2D.PopupMenu GetPopup()` | Gets the stable borrowed internal popup. Its lifetime remains owned by this button. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects inherited button/menu lifecycle, layout, storage or input. |
| `public System.Int32 GetSelectableItem(System.Boolean fromLast = false)` | Finds the first or last enabled nonseparator record. |
| `public System.Int32 GetSelectedID()` | Gets the current signed ID, or -1 without selection. |
| `public T GetSelectedMetadata<T>()` | Gets selected exact generic metadata. |
| `public System.Boolean HasSelectableItems()` | Reports whether any record is enabled and not a separator. |
| `public System.Boolean IsItemDisabled(System.Int32 index)` | Reports an item's Disabled flag. |
| `public System.Boolean IsItemSeparator(System.Int32 index)` | Reports an item's Separator flag. |
| `protected override Electron2D.Vector2 OnGetMinimumSize()` | Projects inherited button/menu lifecycle, layout, storage or input. |
| `protected override System.Void OnNotification(System.Int32 what)` | Projects inherited button/menu lifecycle, layout, storage or input. |
| `protected override System.Void OnPressed()` | Projects inherited button/menu lifecycle, layout, storage or input. |
| `protected override System.Void OnShortcutInput(Electron2D.InputEvent inputEvent)` | Projects inherited button/menu lifecycle, layout, storage or input. |
| `public System.Void RemoveItem(System.Int32 index)` | Removes one record, retaining selection by identity or clearing a removed selection. |
| `public System.Void Select(System.Int32 index)` | Selects an existing record or clears selection, without ItemSelected. |
| `public System.Void SetDisableShortcuts(System.Boolean disabled)` | Disables this button's shortcut input stage, including popup item shortcuts. |
| `public System.Void SetItemAutoTranslateMode(System.Int32 index, Electron2D.NodeAutoTranslateMode value)` | Sets an item's AutoTranslateMode value and refreshes a selected caption/icon when applicable. |
| `public System.Void SetItemDisabled(System.Int32 index, System.Boolean disabled)` | Changes an enabled/disabled flag without changing programmatic selection. |
| `public System.Void SetItemID(System.Int32 index, System.Int32 value)` | Sets an item's ID value and refreshes a selected caption/icon when applicable. |
| `public System.Void SetItemIcon(System.Int32 index, Electron2D.Texture value)` | Sets an item's Icon value and refreshes a selected caption/icon when applicable. |
| `public System.Void SetItemMetadata<T>(System.Int32 index, T value)` | Sets exact generic runtime metadata. |
| `public System.Void SetItemText(System.Int32 index, System.String value)` | Sets an item's Text value and refreshes a selected caption/icon when applicable. |
| `public System.Void SetItemTooltip(System.Int32 index, System.String value)` | Sets an item's Tooltip value and refreshes a selected caption/icon when applicable. |
| `public System.Void ShowPopup()` | Opens the owned menu beneath the button; keyboard/programmatic opening focuses selection or the first selectable item. |

## Events

| Complete declaration | Contract |
| --- | --- |
| `public event System.Action<System.Int32> ItemFocused` | Occurs after keyboard/controller navigation focuses a menu item, carrying its first matching ID index. |
| `public event System.Action<System.Int32> ItemSelected` | Occurs after a user activation commits the selected index and caption. |

## Constructor Descriptions

### .ctor

```csharp
public OptionButton()
```

Creates an empty toggle button with leading alignment, press-edge activation and an internal menu.


### .ctor

```csharp
public OptionButton(System.String text)
```

Creates an empty choice button with initial source text.


- `text`: Nonnull text; later selection owns the caption.

## Property Descriptions

### AllowReselect

```csharp
public System.Boolean AllowReselect { get; set; }
```

Gets or sets whether activating the current item reports selection again.

False initially.


### FitToLongestItem

```csharp
public System.Boolean FitToLongestItem { get; set; }
```

Gets or sets whether the longest item and popup width contribute to the button minimum.

True initially.


### ItemCount

```csharp
public System.Int32 ItemCount { get; set; }
```

Gets or sets the item count, retaining records and selected identity.

Zero through 65536; new slots use radio decoration and automatic IDs.


### SearchBarEnabled

```csharp
public System.Boolean SearchBarEnabled { get; set; }
```

Gets or sets the owned menu's SearchBarEnabled policy.

Uses the PopupMenu default and validation.


### SearchBarFuzzySearchEnabled

```csharp
public System.Boolean SearchBarFuzzySearchEnabled { get; set; }
```

Gets or sets the owned menu's SearchBarFuzzySearchEnabled policy.

Uses the PopupMenu default and validation.


### SearchBarFuzzySearchMaxMisses

```csharp
public System.Int32 SearchBarFuzzySearchMaxMisses { get; set; }
```

Gets or sets the owned menu's SearchBarFuzzySearchMaxMisses policy.

Uses the PopupMenu default and validation.


### SearchBarMinItemCount

```csharp
public System.Int32 SearchBarMinItemCount { get; set; }
```

Gets or sets the owned menu's SearchBarMinItemCount policy.

Uses the PopupMenu default and validation.


### Selected

```csharp
public System.Int32 Selected { get; set; }
```

Gets or sets the selected index without a user-selection event; an initial out-of-range assignment queues.

-1 initially. Existing disabled/separator records may be selected programmatically.


## Method Descriptions

### AddIconItem

```csharp
public System.Void AddIconItem(Electron2D.Texture icon, System.String label, System.Int32 id = -1)
```

Adds a radio item with a borrowed icon.


- `icon`: A live borrowed icon or null.
- `label`: Nonnull source title.
- `id`: Signed ID or -1 for automatic.

### AddItem

```csharp
public System.Void AddItem(System.String label, System.Int32 id = -1)
```

Adds a radio item, selecting it if it is the first selectable record.


- `label`: Nonnull source title.
- `id`: Signed ID; -1 derives from the new index.

### AddSeparator

```csharp
public System.Void AddSeparator(System.String text = "")
```

Adds a nonselectable separator with an optional source title.


- `text`: Nonnull source title.

### Clear

```csharp
public System.Void Clear()
```

Clears item records, selection and caption without ItemSelected.


### CreateSceneInstanceFactory

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

Projects the inherited press/shortcut/minimum/notification/factory/storage/disposal hook onto the owned dropdown contract.

### Dispose

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

Projects the inherited press/shortcut/minimum/notification/factory/storage/disposal hook onto the owned dropdown contract.

### GetItemAutoTranslateMode

```csharp
public Electron2D.NodeAutoTranslateMode GetItemAutoTranslateMode(System.Int32 index)
```

Gets an item's AutoTranslateMode value.

The stored value; resources remain borrowed.


- `index`: A nonnegative index.

### GetItemID

```csharp
public System.Int32 GetItemID(System.Int32 index)
```

Gets an item's ID value.

The stored value; resources remain borrowed.


- `index`: A nonnegative index; -1 returns -1.

### GetItemIcon

```csharp
public Electron2D.Texture GetItemIcon(System.Int32 index)
```

Gets an item's Icon value.

The stored value; resources remain borrowed.


- `index`: A nonnegative index.

### GetItemIndex

```csharp
public System.Int32 GetItemIndex(System.Int32 id)
```

Gets the first record index with an exact ID.

The first index or -1.


- `id`: The signed ID.

### GetItemMetadata

```csharp
public T GetItemMetadata<T>(System.Int32 index)
```

Gets runtime metadata with its exact generic type.

The borrowed value.


- `index`: A nonnegative index.
- `T`: The stored exact type.

### GetItemText

```csharp
public System.String GetItemText(System.Int32 index)
```

Gets an item's Text value.

The stored value; resources remain borrowed.


- `index`: A nonnegative index.

### GetItemTooltip

```csharp
public System.String GetItemTooltip(System.Int32 index)
```

Gets an item's Tooltip value.

The stored value; resources remain borrowed.


- `index`: A nonnegative index.

### GetPopup

```csharp
public Electron2D.PopupMenu GetPopup()
```

Gets the stable borrowed internal popup. Its lifetime remains owned by this button.

The owned menu.


### GetPropertyDescriptors

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the inherited press/shortcut/minimum/notification/factory/storage/disposal hook onto the owned dropdown contract.

### GetSelectableItem

```csharp
public System.Int32 GetSelectableItem(System.Boolean fromLast = false)
```

Finds the first or last enabled nonseparator record.

The index or -1.


- `fromLast`: Searches backward when true.

### GetSelectedID

```csharp
public System.Int32 GetSelectedID()
```

Gets the current signed ID, or -1 without selection.

The selected ID.


### GetSelectedMetadata

```csharp
public T GetSelectedMetadata<T>()
```

Gets selected exact generic metadata.

The borrowed value.


- `T`: The stored exact type.

Throws `System.Collections.Generic.KeyNotFoundException`: Selection or matching metadata is absent.

### HasSelectableItems

```csharp
public System.Boolean HasSelectableItems()
```

Reports whether any record is enabled and not a separator.

True when user selection is possible.


### IsItemDisabled

```csharp
public System.Boolean IsItemDisabled(System.Int32 index)
```

Reports an item's Disabled flag.

The stored flag.


- `index`: A nonnegative index.

### IsItemSeparator

```csharp
public System.Boolean IsItemSeparator(System.Int32 index)
```

Reports an item's Separator flag.

The stored flag.


- `index`: A nonnegative index.

### OnGetMinimumSize

```csharp
protected override Electron2D.Vector2 OnGetMinimumSize()
```

Projects the inherited press/shortcut/minimum/notification/factory/storage/disposal hook onto the owned dropdown contract.

### OnNotification

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

Projects the inherited press/shortcut/minimum/notification/factory/storage/disposal hook onto the owned dropdown contract.

### OnPressed

```csharp
protected override System.Void OnPressed()
```

Projects the inherited press/shortcut/minimum/notification/factory/storage/disposal hook onto the owned dropdown contract.

### OnShortcutInput

```csharp
protected override System.Void OnShortcutInput(Electron2D.InputEvent inputEvent)
```

Projects the inherited press/shortcut/minimum/notification/factory/storage/disposal hook onto the owned dropdown contract.

### RemoveItem

```csharp
public System.Void RemoveItem(System.Int32 index)
```

Removes one record, retaining selection by identity or clearing a removed selection.


- `index`: A nonnegative index.

### Select

```csharp
public System.Void Select(System.Int32 index)
```

Selects an existing record or clears selection, without ItemSelected.


- `index`: A nonnegative existing index or -1.

### SetDisableShortcuts

```csharp
public System.Void SetDisableShortcuts(System.Boolean disabled)
```

Disables this button's shortcut input stage, including popup item shortcuts.


- `disabled`: Whether shortcut handling is disabled.

### SetItemAutoTranslateMode

```csharp
public System.Void SetItemAutoTranslateMode(System.Int32 index, Electron2D.NodeAutoTranslateMode value)
```

Sets an item's AutoTranslateMode value and refreshes a selected caption/icon when applicable.


- `index`: An index, optionally counted from the end.
- `value`: The new typed value.

### SetItemDisabled

```csharp
public System.Void SetItemDisabled(System.Int32 index, System.Boolean disabled)
```

Changes an enabled/disabled flag without changing programmatic selection.


- `index`: An index, optionally negative.
- `disabled`: The flag.

### SetItemID

```csharp
public System.Void SetItemID(System.Int32 index, System.Int32 value)
```

Sets an item's ID value and refreshes a selected caption/icon when applicable.


- `index`: An index, optionally counted from the end.
- `value`: The new typed value.

### SetItemIcon

```csharp
public System.Void SetItemIcon(System.Int32 index, Electron2D.Texture value)
```

Sets an item's Icon value and refreshes a selected caption/icon when applicable.


- `index`: An index, optionally counted from the end.
- `value`: The new typed value.

### SetItemMetadata

```csharp
public System.Void SetItemMetadata<T>(System.Int32 index, T value)
```

Sets exact generic runtime metadata.


- `index`: An index, optionally counted from the end.
- `value`: The borrowed value, including typed null.
- `T`: The stored type.

### SetItemText

```csharp
public System.Void SetItemText(System.Int32 index, System.String value)
```

Sets an item's Text value and refreshes a selected caption/icon when applicable.


- `index`: An index, optionally counted from the end.
- `value`: The new typed value.

### SetItemTooltip

```csharp
public System.Void SetItemTooltip(System.Int32 index, System.String value)
```

Sets an item's Tooltip value and refreshes a selected caption/icon when applicable.


- `index`: An index, optionally counted from the end.
- `value`: The new typed value.

### ShowPopup

```csharp
public System.Void ShowPopup()
```

Opens the owned menu beneath the button; keyboard/programmatic opening focuses selection or the first selectable item.

Detached buttons return without opening. Attached presentation requires an embedded window host.


## Event Descriptions

### ItemFocused

```csharp
public event System.Action<System.Int32> ItemFocused
```

Occurs after keyboard/controller navigation focuses a menu item, carrying its first matching ID index.


### ItemSelected

```csharp
public event System.Action<System.Int32> ItemSelected
```

Occurs after a user activation commits the selected index and caption.

Nested embedded window routing descends to the owned popup. Anchor placement now adds the containing Window offset only when the popup embedder is outside that Window; MenuButtonTests executes both nested choice and command consumers. Shortcut callbacks retain their captured SceneTree so disposing an owner during command delivery cannot dereference a detached Tree.

Popup anchors include GetGlobalTransformWithCanvas before any embedder offset, so a changed canvas translation moves the command/choice popup by the same amount exactly once. MenuButtonTests verifies both consumers under a translated inner viewport.
