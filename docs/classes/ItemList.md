# ItemList

Last updated: 2026-09-30

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

PackedScene captures count, indexed text/icon/selectable/disabled fields and list configuration, then recreates the two internal bars. Selection and typed metadata remain runtime state. [ItemListTests](../../tests/Electron2D.Tests/ItemListTests.cs) covers model, resources, themes, RTL hit testing, input, search and packing. [ItemListRenderingTests](../../tests/Electron2D.Tests/ItemListRenderingTests.cs) verifies pixels and clipping on Linux Wayland GPU and compatibility. Mixed automatic sizing and two-axis overflow, full keyboard/signal ordering, callback reentry, native allocations, other platforms and owner visual acceptance remain unverified; see [coverage](../coverage/classes/ItemList.md).
