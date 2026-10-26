# ItemList

Last updated: 2026-10-25

**Inherits:** [Control](Control.md), CanvasItem, [Node](Node.md), ElectronObject · **Component:** [Scrolling](../components/scrolling.md)

**Declaration:** `public partial class ItemList : Control` · **Source:** [ItemList.cs](../../src/Scene/GUI/ItemList.cs), [ItemList.Layout.cs](../../src/Scene/GUI/ItemList.Layout.cs), [ItemList.Input.cs](../../src/Scene/GUI/ItemList.Input.cs), [ItemList.Visual.cs](../../src/Scene/GUI/ItemList.Visual.cs)

ItemList stores ordered text/icon entries, shapes each visible label with the current theme font, draws selected and hovered rows, and owns internal horizontal and vertical scroll bars. The list clips its own drawing to its control rectangle. The bars returned by `GetHScrollBar` and `GetVScrollBar` are borrowed children; ordinary child enumeration and scene packing omit them. `ClipContents` and `FocusMode` initially equal true and All.

```csharp
var list = new ItemList { Size = new(180, 120) };
list.AddItem("First");
list.AddItem("Second");
list.ItemSelected += index => Console.WriteLine(list.GetItemText(index));
```

`ItemCount`, `AddItem`, `AddIconItem`, `RemoveItem`, `MoveItem`, `Clear` and `SortItemsByText` change the model. Per-item text, icon, icon region/transpose/modulate, background/foreground colors, selectable/disabled state, language, text direction, auto-translation mode and tooltip settings have typed getters and setters. Negative indices count from the end only on the documented setters. Icons are borrowed; a changed texture remeasures its item, and disposing one clears all references to it. `FixedIconSize` reserves an allocation and preserves the source aspect ratio inside it. `SetItemMetadata<T>`, `GetItemMetadata<T>`, `TryGetItemMetadata<T>` and `FindMetadata<T>` retain exact typed runtime values without an untyped public container.

`SelectionMode` selects Single, Multi or Toggle behavior. `Current` is the keyboard-navigation index; programmatic `Select`, `Deselect` and `DeselectAll` do not emit GUI-selection events. Pointer, wheel, pan, focused action keys and incremental Unicode search use the root GUI route. `ItemSelected`, `MultiSelected`, `ItemClicked`, `ItemActivated` and `EmptyClicked` report user input. `AllowReselect`, `AllowRMBSelect` and `AllowSearch` control those input paths. The default search interval comes from `ProjectSettings.IncrementalSearchMaxIntervalMsec`.

`MaxColumns`, `FixedColumnWidth`, `SameColumnWidth`, `MaxTextLines`, `AutoWidth`, `AutoHeight`, `WraparoundItems`, `TextOverrunBehavior`, `IconDisplayMode`, `IconScale`, `HintMode` and `TileScrollHint` configure measured rows and drawing. `GetItemRect` returns panel-offset content coordinates independent of scrolling; `GetItemAtPosition` uses the displayed, scrolled and RTL-mirrored position. `ForceUpdateListSize` synchronously refreshes measurement and scroll ranges. `EnsureCurrentIsVisible` queues an adjustment; `CenterOnCurrent` centers selected axes immediately.

PackedScene captures count, indexed text/icon/selectable/disabled fields and list configuration, then recreates the two internal bars. Selection and typed metadata remain runtime state. [ItemListTests](../../tests/Electron2D.Tests/ItemListTests.cs) covers model, resources, themes, RTL hit testing, input, search and packing. [ItemListRenderingTests](../../tests/Electron2D.Tests/ItemListRenderingTests.cs) verifies pixels and clipping on Linux Wayland GPU and compatibility. [Prepared natural measurement and scrolling](../components/item-list-layout.md) separate intrinsic text/icon measurement from visible shaping. Automatic minima, mixed overflow, RTL content and signed centering execute through the public consumer. [Complete input](../components/item-list-input.md) connects keyboard/range/search transitions, native committed text and callback mutation boundaries. Native allocator totals, physical input hardware, other platforms and owner visual acceptance retain separate limits; see [coverage](../coverage/classes/ItemList.md).

Vertical hints share [VerticalScrollHintMode](VerticalScrollHintMode.md) with Tree under ADR 0051.

Inherited-policy item labels use [Control.TranslationContext](Control.md#automatic-translation-context) when automatic translation supplies no explicit context. Set it before assigning text; changing the context alone preserves prepared text until a translation-change notification or another layout invalidation. Always item policy uses explicit `Tr` and keeps its explicit context; Disabled preserves the source.

Column width excludes the outer horizontal separation. Top-icon captions wrap only at a positive FixedColumnWidth during measurement; MaxTextLines caps visible lines and reserves line_separation for every configured line. Final columns of complete rows may expand their hit/background rectangle; incomplete and unlimited final rows retain their measured width. Text and icons keep their content placement independently of that expansion. Both bars consume the other axis's page, and enlarging the list resets stale scroll values. EnsureCurrentIsVisible applies at draw time to both axes; CenterOnCurrent uses the signed RTL horizontal range. Failed or recursively queried measurement stays dirty for retry.

Input event ordering, eligibility details, deferred selected-item release, configured Shift range steps, search casing and timeout, and failure/mutation boundaries are documented in [ItemList input](../components/item-list-input.md). Repeated prepared input reuses retained prefix capacity; caller input construction and cold growth may allocate. OnTextInput consumes focused native commits before inherited TextInput subscribers. The five ItemList events are cleared when the list is disposed.

## Input properties

| Signature | Initial value | Behavior |
| --- | --- | --- |
| `public bool AllowReselect { get; set; }` | `false` | Repeats selection notification when the ordinary pointer branch hits its selected item. |
| `public bool AllowRMBSelect { get; set; }` | `false` | Allows right-click selection and the focused context-menu action. |
| `public bool AllowSearch { get; set; }` | `true` | Enables source-prefix search from Unicode keys and native committed text. |

Setting an input policy requires the scene owner and a mutable live list. Changing it from an item callback retires the rest of that input operation. Disposed access raises ObjectDisposedException; attached off-owner mutation raises InvalidOperationException.

## Event descriptions

| Signature | Delivery |
| --- | --- |
| `public event Action<int>? ItemSelected` | Input-driven Single selection; argument is the current index after selection. |
| `public event Action<int, bool>? MultiSelected` | Input-driven Multi/Toggle notification; keyboard ranges can report requested selection while eligibility prevents a selected flag. |
| `public event Action<int, Vector2, MouseButton>? ItemClicked` | Enabled-item pointer press or context-menu action; list-local pointer position or the public content rectangle origin. |
| `public event Action<int>? ItemActivated` | Enabled-item left double-click or accept action, including unselectable items. |
| `public event Action<Vector2, MouseButton>? EmptyClicked` | Pointer press outside every item; wheel notification precedes range mutation. |

### ItemSelected

Programmatic Select, Deselect, DeselectAll and Current assignments are silent. A new Single pointer selection reports ItemSelected before ItemClicked; AllowReselect enables repeated selection reporting. Keyboard navigation reports the selected current item. Typed search matching an ineligible source-text entry reports the retained Current, which can be minus one.

### MultiSelected

Command/Control-click toggles one selected item. Toggle mode toggles each eligible clicked item. Mouse Shift ranges add eligible entries between Current and the pointer target. Keyboard Shift ranges replace the requested interval in ascending index order and can report true for an entry that cannot become selected. A plain press on an already selected Multi item waits for left release, then reports its exclusive selection; motion, model resizing/reordering or removal cancel that pending index.

### ItemClicked

Ordinary selection notifications precede the click. Disabled items produce no click. Right-click remains observable when AllowRMBSelect is false. Selected-item deferred presses have no click event. The context action requires AllowRMBSelect and an enabled Current. Wheel clicks report before scrolling and can reach this event over an enabled entry.

### ItemActivated

A left double-click follows ordinary selection and click notifications. Command/Control-toggle and mouse-range branches finish with their click and omit activation. Disabled entries never activate. Mutating an earlier selection/click callback prevents activation using an old index.

### EmptyClicked

The list supplies local position and the pressed button. It does not automatically deselect the list; a subscriber may do so. Wheel presses also report this event over empty space before trying the selected scroll axis.

All five events use the ordinary typed-event gateway. Blocking preserves state mutation and the current delegate snapshot; exceptions stop the current emission. A callback may alter or dispose the list, after which the operation retires its old indices. Subscriber fields are cleared on disposal. The protected `OnGUIInput(InputEvent)` and `OnTextInput(string)` overrides retain owner checks, reject direct recursive input and release their recursion guard on failure. Focused native commits search before inherited TextInput subscribers. Full transition details and executable checks are in [ItemList input](../components/item-list-input.md).

Native scrolling publishes actual positive or negative RTL offsets and row/item/page steps. Requests reuse the owned bars and preserve the current item and selected set; native collection Selection/Table semantics remain separate. See [native scrolling](AccessibilityServer.md#native-scrolling) for payload, units, ownership and verification limits.
