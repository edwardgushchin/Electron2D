# Tree

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.Tree`. **Source:** [source](../../src/Scene/GUI/Tree.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

**Inherits:** [Control](Control.md). **Inherited By:** none currently.

## Description

Tree owns one non-node root and an arbitrary retained branch hierarchy. CreateItem creates that root once and subsequently appends a child of it; CreateChild, removal and cross-tree moves operate on TreeItems. Attached items inherit the scene owner thread. Removing a branch keeps its data alive and clears its old tree's transient selection/edit references. Clear and disposal destroy attached branches; detached branches belong to the caller. Borrowed font, texture, style and metadata payloads are never disposed by the control.

Columns retain title, language/direction/alignment, minimum, expansion ratio and clipping. The row layout uses existing Font/TextLayout Unicode shaping and structured BIDI ranges, scalar text wrapping/overrun, icons and per-cell buttons. Local hit queries, RTL mirroring, scroll fitting and recorded glyph/icon/style geometry share prepared rows. Titles, selection/cursor/hover, checks, ranges, custom cells, relationship/drop lines and scroll hints consume the typed theme. Custom callbacks draw through the current Tree CanvasItem scope; mutating hierarchy/layout during building/drawing throws before the mutation. A callback may retain its item, but must not keep a drawing scope beyond that call.

Single selects one cell, Row selects all selectable cells of one row, and Multi separates selection flags from the cursor. SetSelected replaces other selections, whereas TreeItem.Select in Multi adds a cell and optionally moves the cursor without emitting MultiSelected/CellSelected. Single selection publishes CellSelected before ItemSelected; Row publishes only ItemSelected. Input-driven Multi changes publish CellSelected for the cursor before MultiSelected. Deselect in Multi retains its cursor; DeselectAll clears it. Selection state is committed before observer notification, and later notification lanes continue after errors. Columns are zero-based; an absent selected/edited column is -1. Cell button signal IDs may repeat and are independent of GetPressedButton's index.

Built-in editable String cells use owned LineEdit or TextEdit; multiline commits with Ctrl/Meta+Enter. Numeric Range cells accept the existing scalar expression parser and synchronize a real HSlider, including configured exponential editing. A nonempty range text defines comma-separated label or label:id choices hosted by PopupMenu. Enter commits and Escape cancels string/numeric edits; slider changes commit immediately. Custom editing publishes the application's popup request and its global canvas rectangle. Editors execute as embedded subwindows when a containing viewport enables GUIEmbedSubwindows. Independent native child-window hosting retains its precise backend prerequisite.

Pointer, keyboard, incremental search, wheel input, tooltips, fold modifiers, configurable drop feedback, hover unfolding and edge scrolling use the ordinary Control/Viewport route. The search timeout comes from ProjectSettings.IncrementalSearchMaxIntervalMsec. Items expose a child-drop policy; the application supplies/accepts actual drag payloads through existing Control APIs. Runtime items, callbacks, metadata, selections and editor sessions are omitted from PackedScene. Control configuration and complete column profiles round-trip through scene factories, including fresh-process loading.

Authoring and shaping are cold. Prepared drawing and ordinary selection reuse layout/resource buffers. Depth/visibility/folding are propagated once per cold rebuild; depth-first sibling traversal still uses linear sibling lookup, giving a worst-case O(sibling count squared) cold traversal ceiling. The current backend proof covers Linux desktop GPU and compatibility; physical/native allocator, semantic accessibility, editor-authored item graphs and foreign targets remain separate.

## Example

Executable authoring snippet in an application using `Electron2D`:

```csharp
using var tree = new Tree { Columns = 2, HideRoot = true, Size = new(320, 200) };
var root = tree.CreateItem();
var actor = root.CreateChild();
actor.SetText(0, "Actor");
actor.SetCellMode(1, TreeItem.TreeCellMode.Check);
actor.SetEditable(1, true);
tree.SetSelected(actor, 0);
// Attach tree to the application's Window to render and route native input.
```

## Constructors

| Declaration | Contract |
| --- | --- |
| `public Tree()` | Creates an empty clipped, focusable tree with required scrollbars and cell editors. |

## Constructors descriptions

<a id="member-0ec463a82718"></a>

### Tree()

`public Tree()`

Creates an empty clipped, focusable tree with required scrollbars and cell editors.

## Events

| Declaration | Contract |
| --- | --- |
| `public event System.Action<Electron2D.TreeItem, System.Int32, System.Int32, Electron2D.MouseButton> ButtonClicked` | Reports an activated cell button. |
| `public event System.Action CellSelected` | Occurs when a selection cursor cell changes. |
| `public event System.Action<Electron2D.TreeItem, System.Int32> CheckPropagatedToItem` | Reports committed checkbox propagation. |
| `public event System.Action<System.Int32, Electron2D.MouseButton> ColumnTitleClicked` | Reports a clicked title. |
| `public event System.Action<Electron2D.MouseButton> CustomItemClicked` | Reports custom-cell activation. |
| `public event System.Action<System.Boolean> CustomPopupEdited` | Requests the application's custom editor at GetCustomPopupRect. |
| `public event System.Action<Electron2D.Vector2, Electron2D.MouseButton> EmptyClicked` | Reports an empty-content click. |
| `public event System.Action ItemActivated` | Occurs on item activation. |
| `public event System.Action<Electron2D.TreeItem> ItemCollapsed` | Reports a changed folded branch. |
| `public event System.Action ItemEdited` | Occurs after user cell editing; GetEdited identifies its target. |
| `public event System.Action ItemIconDoubleClicked` | Occurs on a double-clicked cell icon. |
| `public event System.Action<Electron2D.Vector2, Electron2D.MouseButton> ItemMouseSelected` | Reports pointer selection. |
| `public event System.Action ItemSelected` | Occurs after a single selection. |
| `public event System.Action<Electron2D.TreeItem, System.Int32, System.Boolean> MultiSelected` | Reports a committed multiple cell selection change. |
| `public event System.Action NothingSelected` | Occurs after clicking empty content in single/row mode. |

## Events descriptions

<a id="member-dde3f5a86500"></a>

### ButtonClicked

`public event System.Action<Electron2D.TreeItem, System.Int32, System.Int32, Electron2D.MouseButton> ButtonClicked`

Reports an activated cell button.

<a id="member-f327f20b6b50"></a>

### CellSelected

`public event System.Action CellSelected`

Occurs when a selection cursor cell changes.

<a id="member-b06bc6e2f579"></a>

### CheckPropagatedToItem

`public event System.Action<Electron2D.TreeItem, System.Int32> CheckPropagatedToItem`

Reports committed checkbox propagation.

<a id="member-8e82c35c4226"></a>

### ColumnTitleClicked

`public event System.Action<System.Int32, Electron2D.MouseButton> ColumnTitleClicked`

Reports a clicked title.

<a id="member-746be4ab7374"></a>

### CustomItemClicked

`public event System.Action<Electron2D.MouseButton> CustomItemClicked`

Reports custom-cell activation.

<a id="member-762ef28991f9"></a>

### CustomPopupEdited

`public event System.Action<System.Boolean> CustomPopupEdited`

Requests the application's custom editor at GetCustomPopupRect.

<a id="member-39c70ae04374"></a>

### EmptyClicked

`public event System.Action<Electron2D.Vector2, Electron2D.MouseButton> EmptyClicked`

Reports an empty-content click.

<a id="member-c3ad905e4559"></a>

### ItemActivated

`public event System.Action ItemActivated`

Occurs on item activation.

<a id="member-3579df914e2f"></a>

### ItemCollapsed

`public event System.Action<Electron2D.TreeItem> ItemCollapsed`

Reports a changed folded branch.

<a id="member-0d9e2962d0d5"></a>

### ItemEdited

`public event System.Action ItemEdited`

Occurs after user cell editing; GetEdited identifies its target.

<a id="member-9fa03e1206c8"></a>

### ItemIconDoubleClicked

`public event System.Action ItemIconDoubleClicked`

Occurs on a double-clicked cell icon.

<a id="member-5f4a54d99b68"></a>

### ItemMouseSelected

`public event System.Action<Electron2D.Vector2, Electron2D.MouseButton> ItemMouseSelected`

Reports pointer selection.

<a id="member-defc66caf90e"></a>

### ItemSelected

`public event System.Action ItemSelected`

Occurs after a single selection.

<a id="member-2a9847f64a48"></a>

### MultiSelected

`public event System.Action<Electron2D.TreeItem, System.Int32, System.Boolean> MultiSelected`

Reports a committed multiple cell selection change.

<a id="member-6dc39d003dd3"></a>

### NothingSelected

`public event System.Action NothingSelected`

Occurs after clicking empty content in single/row mode.

## Methods

| Declaration | Contract |
| --- | --- |
| `public System.Void Clear()` | Disposes the entire owned item hierarchy and cancels editing. |
| `public Electron2D.TreeItem CreateItem(Electron2D.TreeItem parent = null, System.Int32 index = -1)` | Creates the root of an empty control or a child of the supplied/default root. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |
| `public System.Void DeselectAll()` | Deselects every row/cell; multiple mode also clears the cursor. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |
| `public System.Boolean EditSelected(System.Boolean forceEdit = false)` | Activates the current cell's built-in or application editor. |
| `public System.Void EnsureCursorIsVisible()` | Scrolls the current selection cursor into view on both axes. |
| `public System.Int32 GetButtonIDAtPosition(Electron2D.Vector2 position)` | Returns a cell-button signal ID under a local point. |
| `public System.Int32 GetColumnAtPosition(Electron2D.Vector2 position)` | Returns the cell column under a presented row. |
| `public System.Int32 GetColumnExpandRatio(System.Int32 column)` | Returns a column expansion ratio. |
| `public System.String GetColumnTitle(System.Int32 column)` | Returns a source column title. |
| `public Electron2D.HorizontalAlignment GetColumnTitleAlignment(System.Int32 column)` | Returns title alignment. |
| `public Electron2D.TextDirection GetColumnTitleDirection(System.Int32 column)` | Returns title text direction. |
| `public System.String GetColumnTitleLanguage(System.Int32 column)` | Returns title shaping language. |
| `public System.String GetColumnTitleTooltipText(System.Int32 column)` | Returns a source title tooltip. |
| `public System.Int32 GetColumnWidth(System.Int32 column)` | Returns the current fitted pixel column width, saturating at Int32.MaxValue. |
| `public Electron2D.CanvasItem GetCustomDrawingCanvasItem()` | Returns the typed borrowed canvas used by cell custom drawing. |
| `public Electron2D.Rect2 GetCustomPopupRect()` | Returns the most recent custom-editor rectangle in global canvas coordinates. |
| `public System.Int32 GetDropSectionAtPosition(Electron2D.Vector2 position)` | Returns enabled row drop presentation at a local point. |
| `public Electron2D.TreeItem GetEdited()` | Returns the last edited item or null. |
| `public System.Int32 GetEditedColumn()` | Returns the last edited column. |
| `public Electron2D.Rect2 GetItemAreaRect(Electron2D.TreeItem item, System.Int32 column = -1, System.Int32 buttonIndex = -1)` | Returns a presented row, cell or button rectangle in local control coordinates. |
| `public Electron2D.TreeItem GetItemAtPosition(Electron2D.Vector2 position)` | Returns a presented item at a local control coordinate. |
| `public Electron2D.TreeItem GetNextSelected(Electron2D.TreeItem from)` | Returns the next selected row in depth-first hierarchy order, regardless of folding. |
| `public System.Int32 GetPressedButton()` | Returns the currently pressed cell-button index. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |
| `public Electron2D.TreeItem GetRoot()` | Returns the stable borrowed root or null. |
| `public Electron2D.Vector2 GetScroll()` | Returns the current pixel scroll offsets. |
| `public Electron2D.TreeItem GetSelected()` | Returns the selection cursor item or null. |
| `public System.Int32 GetSelectedColumn()` | Returns the cursor column. |
| `public System.Boolean IsColumnClippingContent(System.Int32 column)` | Reports column content clipping. |
| `public System.Boolean IsColumnExpanding(System.Int32 column)` | Reports column expansion. |
| `protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |
| `protected override Electron2D.Vector2 OnGetMinimumSize()` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |
| `protected override System.String OnGetTooltip(Electron2D.Vector2 atPosition)` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |
| `protected override System.Void OnNotification(System.Int32 what)` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |
| `public System.Void ScrollToItem(Electron2D.TreeItem item, System.Boolean centerOnItem = false)` | Unfolds ancestors and scrolls to a row. |
| `public System.Void SetColumnClipContent(System.Int32 column, System.Boolean enable)` | Sets whether intrinsic text width contributes to a column minimum. |
| `public System.Void SetColumnCustomMinimumWidth(System.Int32 column, System.Int32 minWidth)` | Sets a nonnegative explicit column minimum. |
| `public System.Void SetColumnExpand(System.Int32 column, System.Boolean expand)` | Sets whether spare width expands a column. |
| `public System.Void SetColumnExpandRatio(System.Int32 column, System.Int32 ratio)` | Sets a positive column expansion ratio. |
| `public System.Void SetColumnTitle(System.Int32 column, System.String title)` | Sets a column's source title. |
| `public System.Void SetColumnTitleAlignment(System.Int32 column, Electron2D.HorizontalAlignment titleAlignment)` | Sets title alignment. |
| `public System.Void SetColumnTitleDirection(System.Int32 column, Electron2D.TextDirection direction)` | Sets title text direction. |
| `public System.Void SetColumnTitleLanguage(System.Int32 column, System.String language)` | Sets title shaping language. |
| `public System.Void SetColumnTitleTooltipText(System.Int32 column, System.String tooltipText)` | Sets a title's source tooltip. |
| `public System.Void SetSelected(Electron2D.TreeItem item, System.Int32 column)` | Selects only the given cell or row under the current policy. |

## Methods descriptions

<a id="member-886c1d9d5625"></a>

### Clear()

`public System.Void Clear()`

Disposes the entire owned item hierarchy and cancels editing.

<a id="member-1ef5f509aca1"></a>

### CreateItem(Electron2D.TreeItem, System.Int32)

`public Electron2D.TreeItem CreateItem(Electron2D.TreeItem parent = null, System.Int32 index = -1)`

Creates the root of an empty control or a child of the supplied/default root.

**Param `parent`:** Parent owned by this control, or null.

**Param `index`:** Clamped insertion index; negative appends.

**Returns:** New owned item.

<a id="member-2dea518ee735"></a>

### CreateSceneInstanceFactory()

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

<a id="member-fcf560d7ab22"></a>

### DeselectAll()

`public System.Void DeselectAll()`

Deselects every row/cell; multiple mode also clears the cursor.

<a id="member-27ed66344bdb"></a>

### Dispose(System.Boolean)

`protected override System.Void Dispose(System.Boolean disposing)`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

<a id="member-46f6ca8718f2"></a>

### EditSelected(System.Boolean)

`public System.Boolean EditSelected(System.Boolean forceEdit = false)`

Activates the current cell's built-in or application editor.

**Param `forceEdit`:** Overrides the cell Editable flag.

**Returns:** Whether this mode can be edited.

<a id="member-82036e87d55d"></a>

### EnsureCursorIsVisible()

`public System.Void EnsureCursorIsVisible()`

Scrolls the current selection cursor into view on both axes.

<a id="member-40e49a539eb4"></a>

### GetButtonIDAtPosition(Electron2D.Vector2)

`public System.Int32 GetButtonIDAtPosition(Electron2D.Vector2 position)`

Returns a cell-button signal ID under a local point.

**Param `position`:** Local point.

**Returns:** Signal ID or -1.

<a id="member-8d032882164c"></a>

### GetColumnAtPosition(Electron2D.Vector2)

`public System.Int32 GetColumnAtPosition(Electron2D.Vector2 position)`

Returns the cell column under a presented row.

**Param `position`:** Local point.

**Returns:** Column or -1.

<a id="member-0586e7a16bd7"></a>

### GetColumnExpandRatio(System.Int32)

`public System.Int32 GetColumnExpandRatio(System.Int32 column)`

Returns a column expansion ratio.

**Param `column`:** Existing column.

**Returns:** One initially.

<a id="member-9e47f2f43501"></a>

### GetColumnTitle(System.Int32)

`public System.String GetColumnTitle(System.Int32 column)`

Returns a source column title.

**Param `column`:** Existing column.

**Returns:** Title.

<a id="member-49d18e6986da"></a>

### GetColumnTitleAlignment(System.Int32)

`public Electron2D.HorizontalAlignment GetColumnTitleAlignment(System.Int32 column)`

Returns title alignment.

**Param `column`:** Existing column.

**Returns:** Alignment.

<a id="member-d3b8957e2009"></a>

### GetColumnTitleDirection(System.Int32)

`public Electron2D.TextDirection GetColumnTitleDirection(System.Int32 column)`

Returns title text direction.

**Param `column`:** Existing column.

**Returns:** Direction.

<a id="member-385fd0f76273"></a>

### GetColumnTitleLanguage(System.Int32)

`public System.String GetColumnTitleLanguage(System.Int32 column)`

Returns title shaping language.

**Param `column`:** Existing column.

**Returns:** Language tag.

<a id="member-1d9f2103df1d"></a>

### GetColumnTitleTooltipText(System.Int32)

`public System.String GetColumnTitleTooltipText(System.Int32 column)`

Returns a source title tooltip.

**Param `column`:** Existing column.

**Returns:** Tooltip.

<a id="member-c0f8cb9ed725"></a>

### GetColumnWidth(System.Int32)

`public System.Int32 GetColumnWidth(System.Int32 column)`

Returns the current fitted pixel column width, saturating at Int32.MaxValue.

**Param `column`:** Existing column.

**Returns:** Pixel width.

<a id="member-2ed0f7bb2149"></a>

### GetCustomDrawingCanvasItem()

`public Electron2D.CanvasItem GetCustomDrawingCanvasItem()`

Returns the typed borrowed canvas used by cell custom drawing.

**Returns:** This control; Draw operations require its active custom-draw scope.

<a id="member-6f9cef0988bc"></a>

### GetCustomPopupRect()

`public Electron2D.Rect2 GetCustomPopupRect()`

Returns the most recent custom-editor rectangle in global canvas coordinates.

**Returns:** Custom popup area.

<a id="member-8b11fa51f41d"></a>

### GetDropSectionAtPosition(Electron2D.Vector2)

`public System.Int32 GetDropSectionAtPosition(Electron2D.Vector2 position)`

Returns enabled row drop presentation at a local point.

**Param `position`:** Local point.

**Returns:** -1 above, 0 on, 1 below, or -100 when unavailable.

<a id="member-63bcff40fce7"></a>

### GetEdited()

`public Electron2D.TreeItem GetEdited()`

Returns the last edited item or null.

**Returns:** Borrowed edited item.

<a id="member-00537524acc0"></a>

### GetEditedColumn()

`public System.Int32 GetEditedColumn()`

Returns the last edited column.

**Returns:** Column index or -1.

<a id="member-334290e5e01b"></a>

### GetItemAreaRect(Electron2D.TreeItem, System.Int32, System.Int32)

`public Electron2D.Rect2 GetItemAreaRect(Electron2D.TreeItem item, System.Int32 column = -1, System.Int32 buttonIndex = -1)`

Returns a presented row, cell or button rectangle in local control coordinates.

**Param `item`:** Owned item.

**Param `column`:** Column or -1 for complete row.

**Param `buttonIndex`:** Button index or -1 for the cell.

**Returns:** Local rectangle, empty for folded/hidden rows.

<a id="member-8a02797a5409"></a>

### GetItemAtPosition(Electron2D.Vector2)

`public Electron2D.TreeItem GetItemAtPosition(Electron2D.Vector2 position)`

Returns a presented item at a local control coordinate.

**Param `position`:** Finite local point.

**Returns:** Borrowed row or null.

<a id="member-21cd13ff2ff1"></a>

### GetNextSelected(Electron2D.TreeItem)

`public Electron2D.TreeItem GetNextSelected(Electron2D.TreeItem from)`

Returns the next selected row in depth-first hierarchy order, regardless of folding.

**Param `from`:** Previous item or null to begin.

**Returns:** Borrowed selected item or null.

<a id="member-9c3159f27ed6"></a>

### GetPressedButton()

`public System.Int32 GetPressedButton()`

Returns the currently pressed cell-button index.

**Returns:** Index or -1.

<a id="member-8d812445245c"></a>

### GetPropertyDescriptors()

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

<a id="member-16a32ed40867"></a>

### GetRoot()

`public Electron2D.TreeItem GetRoot()`

Returns the stable borrowed root or null.

**Returns:** Root item.

<a id="member-5c600e1dbde0"></a>

### GetScroll()

`public Electron2D.Vector2 GetScroll()`

Returns the current pixel scroll offsets.

**Returns:** Horizontal/vertical offsets.

<a id="member-c4c66638c3ed"></a>

### GetSelected()

`public Electron2D.TreeItem GetSelected()`

Returns the selection cursor item or null.

**Returns:** Borrowed cursor item.

<a id="member-2be3ccc8be6e"></a>

### GetSelectedColumn()

`public System.Int32 GetSelectedColumn()`

Returns the cursor column.

**Returns:** Column index.

<a id="member-c6badcd3a7b8"></a>

### IsColumnClippingContent(System.Int32)

`public System.Boolean IsColumnClippingContent(System.Int32 column)`

Reports column content clipping.

**Param `column`:** Existing column.

**Returns:** False initially.

<a id="member-4083cb8e82b7"></a>

### IsColumnExpanding(System.Int32)

`public System.Boolean IsColumnExpanding(System.Int32 column)`

Reports column expansion.

**Param `column`:** Existing column.

**Returns:** True initially.

<a id="member-5a4b27b22c4f"></a>

### OnGUIInput(Electron2D.InputEvent)

`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

<a id="member-6a8c71bdb500"></a>

### OnGetMinimumSize()

`protected override Electron2D.Vector2 OnGetMinimumSize()`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

<a id="member-05984c311b19"></a>

### OnGetTooltip(Electron2D.Vector2)

`protected override System.String OnGetTooltip(Electron2D.Vector2 atPosition)`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

<a id="member-bf67c2fbea53"></a>

### OnNotification(System.Int32)

`protected override System.Void OnNotification(System.Int32 what)`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

<a id="member-841cea5a210b"></a>

### ScrollToItem(Electron2D.TreeItem, System.Boolean)

`public System.Void ScrollToItem(Electron2D.TreeItem item, System.Boolean centerOnItem = false)`

Unfolds ancestors and scrolls to a row.

**Param `item`:** Owned item.

**Param `centerOnItem`:** Centers the row vertically.

<a id="member-11c501401cf1"></a>

### SetColumnClipContent(System.Int32, System.Boolean)

`public System.Void SetColumnClipContent(System.Int32 column, System.Boolean enable)`

Sets whether intrinsic text width contributes to a column minimum.

**Param `column`:** Existing column.

**Param `enable`:** Clips overflowing cell content.

<a id="member-a2e7c541b0ba"></a>

### SetColumnCustomMinimumWidth(System.Int32, System.Int32)

`public System.Void SetColumnCustomMinimumWidth(System.Int32 column, System.Int32 minWidth)`

Sets a nonnegative explicit column minimum.

**Param `column`:** Existing column.

**Param `minWidth`:** Pixel width.

<a id="member-720fb855ce84"></a>

### SetColumnExpand(System.Int32, System.Boolean)

`public System.Void SetColumnExpand(System.Int32 column, System.Boolean expand)`

Sets whether spare width expands a column.

**Param `column`:** Existing column.

**Param `expand`:** Expansion policy.

<a id="member-8db11283670c"></a>

### SetColumnExpandRatio(System.Int32, System.Int32)

`public System.Void SetColumnExpandRatio(System.Int32 column, System.Int32 ratio)`

Sets a positive column expansion ratio.

**Param `column`:** Existing column.

**Param `ratio`:** Positive weight.

<a id="member-af22e30ed25f"></a>

### SetColumnTitle(System.Int32, System.String)

`public System.Void SetColumnTitle(System.Int32 column, System.String title)`

Sets a column's source title.

**Param `column`:** Existing column.

**Param `title`:** Nonnull source text.

<a id="member-f8866522f70d"></a>

### SetColumnTitleAlignment(System.Int32, Electron2D.HorizontalAlignment)

`public System.Void SetColumnTitleAlignment(System.Int32 column, Electron2D.HorizontalAlignment titleAlignment)`

Sets title alignment.

**Param `column`:** Existing column.

**Param `titleAlignment`:** Defined alignment.

<a id="member-a5d4111534c7"></a>

### SetColumnTitleDirection(System.Int32, Electron2D.TextDirection)

`public System.Void SetColumnTitleDirection(System.Int32 column, Electron2D.TextDirection direction)`

Sets title text direction.

**Param `column`:** Existing column.

**Param `direction`:** Defined direction.

<a id="member-cdcfd5c5b97f"></a>

### SetColumnTitleLanguage(System.Int32, System.String)

`public System.Void SetColumnTitleLanguage(System.Int32 column, System.String language)`

Sets title shaping language.

**Param `column`:** Existing column.

**Param `language`:** Language tag or empty.

<a id="member-9ec35f4fa569"></a>

### SetColumnTitleTooltipText(System.Int32, System.String)

`public System.Void SetColumnTitleTooltipText(System.Int32 column, System.String tooltipText)`

Sets a title's source tooltip.

**Param `column`:** Existing column.

**Param `tooltipText`:** Nonnull tooltip.

<a id="member-a12c1d02edf6"></a>

### SetSelected(Electron2D.TreeItem, System.Int32)

`public System.Void SetSelected(Electron2D.TreeItem item, System.Int32 column)`

Selects only the given cell or row under the current policy.

**Param `item`:** Owned item.

**Param `column`:** Existing column.

## Properties

| Declaration | Contract |
| --- | --- |
| `public System.Boolean AllowRMBSelect { get; set; }` | Gets or sets right-button selection before context actions. |
| `public System.Boolean AllowReselect { get; set; }` | Gets or sets whether selecting an already selected cell publishes selection again. |
| `public System.Boolean AllowSearch { get; set; }` | Gets or sets timed incremental text search. |
| `public System.Boolean AutoTooltip { get; set; }` | Gets or sets source-text fallback for empty explicit tooltips. |
| `public System.Boolean ColumnTitlesVisible { get; set; }` | Gets or sets column header presentation. |
| `public System.Int32 Columns { get; set; }` | Gets or resizes the positive number of cell columns. |
| `public Electron2D.TreeDropModeFlags DropModeFlags { get; set; }` | Gets or sets drop feedback flags. |
| `public System.Boolean EnableDragUnfolding { get; set; }` | Gets or sets timed unfolding while a drop hovers a folded branch. |
| `public System.Boolean EnableRecursiveFolding { get; set; }` | Gets or sets recursive folding with modified pointer/keyboard input. |
| `public System.Boolean HideFolding { get; set; }` | Gets or sets whether all fold affordances are hidden. |
| `public System.Boolean HideRoot { get; set; }` | Gets or sets whether the root row is omitted while its visible children are presented. |
| `public Electron2D.VerticalScrollHintMode HintMode { get; set; }` | Gets or sets eligible vertical scroll hints. |
| `public System.Boolean ScrollHorizontalEnabled { get; set; }` | Gets or sets horizontal scrollbar operation. |
| `public System.Boolean ScrollVerticalEnabled { get; set; }` | Gets or sets vertical scrollbar operation. |
| `public Electron2D.Tree.SelectMode SelectionMode { get; set; }` | Gets or sets the cell/row/multiple selection policy. |
| `public System.Boolean TileScrollHint { get; set; }` | Gets or sets repeated scroll-hint texture presentation. |

## Properties descriptions

<a id="member-ba789209f573"></a>

### AllowRMBSelect

`public System.Boolean AllowRMBSelect { get; set; }`

Gets or sets right-button selection before context actions.

**Value:** False initially.

<a id="member-e192fa585817"></a>

### AllowReselect

`public System.Boolean AllowReselect { get; set; }`

Gets or sets whether selecting an already selected cell publishes selection again.

**Value:** False initially.

<a id="member-b5f4e5abab61"></a>

### AllowSearch

`public System.Boolean AllowSearch { get; set; }`

Gets or sets timed incremental text search.

**Value:** True initially.

<a id="member-436e0803b657"></a>

### AutoTooltip

`public System.Boolean AutoTooltip { get; set; }`

Gets or sets source-text fallback for empty explicit tooltips.

**Value:** True initially.

<a id="member-668e29ed0fe8"></a>

### ColumnTitlesVisible

`public System.Boolean ColumnTitlesVisible { get; set; }`

Gets or sets column header presentation.

**Value:** False initially.

<a id="member-e389c8edbe14"></a>

### Columns

`public System.Int32 Columns { get; set; }`

Gets or resizes the positive number of cell columns.

**Value:** One initially.

<a id="member-9455fb6db292"></a>

### DropModeFlags

`public Electron2D.TreeDropModeFlags DropModeFlags { get; set; }`

Gets or sets drop feedback flags.

**Value:** Disabled initially.

<a id="member-b65eed38e3a0"></a>

### EnableDragUnfolding

`public System.Boolean EnableDragUnfolding { get; set; }`

Gets or sets timed unfolding while a drop hovers a folded branch.

**Value:** True initially.

<a id="member-c36971640d70"></a>

### EnableRecursiveFolding

`public System.Boolean EnableRecursiveFolding { get; set; }`

Gets or sets recursive folding with modified pointer/keyboard input.

**Value:** True initially.

<a id="member-4f976e2b4f04"></a>

### HideFolding

`public System.Boolean HideFolding { get; set; }`

Gets or sets whether all fold affordances are hidden.

**Value:** False initially.

<a id="member-93b5f0d9a10c"></a>

### HideRoot

`public System.Boolean HideRoot { get; set; }`

Gets or sets whether the root row is omitted while its visible children are presented.

**Value:** False initially.

<a id="member-37eab8f43cdb"></a>

### HintMode

`public Electron2D.VerticalScrollHintMode HintMode { get; set; }`

Gets or sets eligible vertical scroll hints.

**Value:** Disabled initially.

<a id="member-ac3d1dd21f8a"></a>

### ScrollHorizontalEnabled

`public System.Boolean ScrollHorizontalEnabled { get; set; }`

Gets or sets horizontal scrollbar operation.

**Value:** True initially.

<a id="member-904e4b4e4972"></a>

### ScrollVerticalEnabled

`public System.Boolean ScrollVerticalEnabled { get; set; }`

Gets or sets vertical scrollbar operation.

**Value:** True initially.

<a id="member-73d73ff8ee12"></a>

### SelectionMode

`public Electron2D.Tree.SelectMode SelectionMode { get; set; }`

Gets or sets the cell/row/multiple selection policy.

**Value:** Single initially.

<a id="member-44985ccf5762"></a>

### TileScrollHint

`public System.Boolean TileScrollHint { get; set; }`

Gets or sets repeated scroll-hint texture presentation.

**Value:** False initially.

## Lifecycle, invariants and verification

Attached operations require the owning scene thread and a live item/control. Invalid indices, invalid enum values, nonfinite required configuration and ownership/cycle violations throw before mutation. Item/metadata/resource lifetimes follow the description above. See [hierarchical cells](../components/hierarchical-cells.md) for runtime flow, exercised editor and native workflows, allocation boundaries and dependency limits; [Tree coverage](../coverage/classes/Tree.md) and [TreeItem coverage](../coverage/classes/TreeItem.md) account for the applicable comparison surface. Relevant decisions: ADRs 0004, 0008, 0014, 0023, 0038, 0046, 0051 and 0083.
