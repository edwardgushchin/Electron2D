# TreeItem

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.TreeItem`. **Source:** [source](../../src/Scene/GUI/TreeItem.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

**Inherits:** [ElectronObject](ElectronObject.md). This type is sealed.

## Description

A TreeItem is an owned ElectronObject, not a scene Node. There is no public constructor: obtain a root or child from Tree/CreateChild. Its cells resize with the owning Tree's column count. Removing a direct child detaches the entire branch without disposal; AddChild accepts only an unowned detached branch and rejects cycles. MoveBefore/MoveAfter may transfer a complete branch between trees and resize its cells; a root or descendant destination is rejected. GetChildren copies the array while borrowing the items. Negative GetChild indices count from the end; traversal can wrap, skips locally hidden branches and optionally respects folding.

Cells retain independent String, Check, Range, Icon or Custom modes, editable/selectable flags, text/shaping configuration, borrowed graphics and button lists. A mode change resets numeric bounds/value, checked state, icon/text and icon maximum width, while retaining other cell options. Structured parser option arrays are copied on set/get. Description and button descriptions round-trip as runtime authoring data; publishing them to native semantic services remains a separate service prerequisite. Metadata<T> stores and retrieves the exact generic type, including null payloads; an absent/type-mismatched value throws KeyNotFoundException. No universal dynamic value or string method dispatch is exposed.

SetRange snaps to the absolute zero-based grid for a positive step, then clamps to the configured bounds. Nonpositive steps disable snapping. Configuration must be finite and does not reclamp the existing value. The value retains IEEE behavior, including NaN; infinity clamps. GetRangeConfig returns Min/Max/Step/Exponential as a typed tuple and faithfully returns the configured exponential flag. Range text choices define their own ID bounds and a zero step. Check and indeterminate state clear each other only when the assigned state changes. PropagateCheck snapshots the subtree, commits descendant and ancestor tri-state values, then emits in invoker/depth-first/ancestor order; observer failures aggregate after later items have received notification.

CallRecursive captures a depth-first subtree and invokes a typed Action<TreeItem> on each still-live item, continuing after failures. Attached reads/writes follow the owning scene thread. Structural changes and cell layout changes are rejected during Tree drawing/layout; returned fonts, textures, styles, items and delegates are borrowed. Custom drawing receives a local cell rectangle and the item; use GetTree().GetCustomDrawingCanvasItem() inside that call. Runtime cells and the item graph are not scene-serialized. Disposal recursively releases attached child objects and managed references without disposing borrowed resources.

## Example

Executable authoring snippet in an application using `Electron2D`:

```csharp
using var tree = new Tree { Columns = 2 };
var row = tree.CreateItem().CreateChild();
row.SetMetadata(0, 42);
row.SetCellMode(1, TreeItem.TreeCellMode.Range);
row.SetRangeConfig(1, 0, 100, 0.5);
row.SetRange(1, 3.3); // 3.5 on the zero-based grid
var configuration = row.GetRangeConfig(1);
```

## Methods

| Declaration | Contract |
| --- | --- |
| `public System.Void AddButton(System.Int32 column, Electron2D.Texture button, System.Int32 id = -1, System.Boolean disabled = false, System.String tooltipText = "", System.String description = "")` | Adds a borrowed icon button to a cell. |
| `public System.Void AddChild(Electron2D.TreeItem child)` | Attaches an unowned detached branch as the final child. |
| `public System.Void CallRecursive(System.Action<Electron2D.TreeItem> callback)` | Applies a typed callback to a captured depth-first subtree, continuing after callback errors. |
| `public System.Void ClearButtons()` | Clears all buttons on this row. |
| `public System.Void ClearCustomBGColor(System.Int32 column)` | Disables the background override. |
| `public System.Void ClearCustomColor(System.Int32 column)` | Disables the cell foreground override. |
| `public Electron2D.TreeItem CreateChild(System.Int32 index = -1)` | Creates a child at a clamped index, or appends for negative indices. |
| `public System.Void Deselect(System.Int32 column)` | Deselects this cell through its owning control. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |
| `public System.Void EraseButton(System.Int32 column, System.Int32 buttonIndex)` | Removes a button by its cell index. |
| `public Electron2D.NodeAutoTranslateMode GetAutoTranslateMode(System.Int32 column)` | Returns one cell's AutoTranslateMode configuration. |
| `public Electron2D.TextAutowrapMode GetAutowrapMode(System.Int32 column)` | Returns one cell's AutowrapMode configuration. |
| `public Electron2D.TextLineBreakFlags GetAutowrapTrimFlags(System.Int32 column)` | Returns one cell's AutowrapTrimFlags configuration. |
| `public Electron2D.Texture GetButton(System.Int32 column, System.Int32 buttonIndex)` | Returns a live borrowed button texture. |
| `public System.Int32 GetButtonByID(System.Int32 column, System.Int32 id)` | Returns the first index with an exact signal ID. |
| `public Electron2D.Color GetButtonColor(System.Int32 column, System.Int32 id)` | Returns the button tint by button index. |
| `public System.Int32 GetButtonCount(System.Int32 column)` | Returns cell button count. |
| `public System.Int32 GetButtonID(System.Int32 column, System.Int32 buttonIndex)` | Returns a button's retained signal ID. |
| `public System.String GetButtonTooltipText(System.Int32 column, System.Int32 buttonIndex)` | Returns source button tooltip text. |
| `public Electron2D.TreeItem.TreeCellMode GetCellMode(System.Int32 column)` | Returns one cell's CellMode configuration. |
| `public Electron2D.TreeItem GetChild(System.Int32 index)` | Returns one direct child, accepting negative indices from the end. |
| `public System.Int32 GetChildCount()` | Returns the number of direct children. |
| `public Electron2D.TreeItem[] GetChildren()` | Returns an independent array of borrowed direct children. |
| `public Electron2D.Color GetCustomBGColor(System.Int32 column)` | Returns the retained background color. |
| `public Electron2D.Color GetCustomColor(System.Int32 column)` | Returns the retained foreground color. |
| `public System.Action<Electron2D.TreeItem, Electron2D.Rect2> GetCustomDrawCallback(System.Int32 column)` | Returns the retained typed cell draw callback. |
| `public Electron2D.Font GetCustomFont(System.Int32 column)` | Returns one cell's CustomFont configuration. |
| `public System.Int32 GetCustomFontSize(System.Int32 column)` | Returns one cell's CustomFontSize configuration. |
| `public Electron2D.StyleBox GetCustomStyleBox(System.Int32 column)` | Returns one cell's CustomStyleBox configuration. |
| `public System.String GetDescription(System.Int32 column)` | Returns one cell's Description configuration. |
| `public System.Boolean GetExpandRight(System.Int32 column)` | Returns one cell's ExpandRight configuration. |
| `public Electron2D.TreeItem GetFirstChild()` | Returns the first direct child or null. |
| `public Electron2D.Texture GetIcon(System.Int32 column)` | Returns one cell's Icon configuration. |
| `public System.Int32 GetIconMaxWidth(System.Int32 column)` | Returns one cell's IconMaxWidth configuration. |
| `public Electron2D.Color GetIconModulate(System.Int32 column)` | Returns one cell's IconModulate configuration. |
| `public Electron2D.Texture GetIconOverlay(System.Int32 column)` | Returns one cell's IconOverlay configuration. |
| `public Electron2D.Rect2 GetIconRegion(System.Int32 column)` | Returns one cell's IconRegion configuration. |
| `public System.Int32 GetIndex()` | Returns this item's sibling index. |
| `public System.String GetLanguage(System.Int32 column)` | Returns one cell's Language configuration. |
| `public T GetMetadata<T>(System.Int32 column)` | Retrieves a runtime cell payload of the exact generic type. |
| `public Electron2D.TreeItem GetNext()` | Returns the next sibling or null. |
| `public Electron2D.TreeItem GetNextInTree(System.Boolean wrap = false)` | Returns the following visible-in-hierarchy item in depth-first order, ignoring folding. |
| `public Electron2D.TreeItem GetNextVisible(System.Boolean wrap = false)` | Returns the following presented row, respecting folding. |
| `public Electron2D.TreeItem GetParent()` | Returns the parent item or null for a root/detached branch. |
| `public Electron2D.TreeItem GetPrev()` | Returns the preceding sibling or null. |
| `public Electron2D.TreeItem GetPrevInTree(System.Boolean wrap = false)` | Returns the preceding visible-in-hierarchy item, ignoring folding. |
| `public Electron2D.TreeItem GetPrevVisible(System.Boolean wrap = false)` | Returns the preceding presented row, respecting folding. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |
| `public System.Double GetRange(System.Int32 column)` | Returns the retained numeric value. |
| `public System.ValueTuple<System.Double, System.Double, System.Double, System.Boolean> GetRangeConfig(System.Int32 column)` | Returns the complete typed numeric configuration, including the configured scale policy. |
| `public Electron2D.StructuredTextParser GetStructuredTextBIDIOverride(System.Int32 column)` | Returns one cell's StructuredTextBIDIOverride configuration. |
| `public System.String[] GetStructuredTextBIDIOverrideOptions(System.Int32 column)` | Returns one cell's StructuredTextBIDIOverrideOptions configuration. |
| `public System.String GetSuffix(System.Int32 column)` | Returns one cell's Suffix configuration. |
| `public System.String GetText(System.Int32 column)` | Returns one cell's Text configuration. |
| `public Electron2D.HorizontalAlignment GetTextAlignment(System.Int32 column)` | Returns one cell's TextAlignment configuration. |
| `public Electron2D.TextDirection GetTextDirection(System.Int32 column)` | Returns one cell's TextDirection configuration. |
| `public Electron2D.TextOverrunBehavior GetTextOverrunBehavior(System.Int32 column)` | Returns one cell's TextOverrunBehavior configuration. |
| `public System.String GetTooltipText(System.Int32 column)` | Returns one cell's TooltipText configuration. |
| `public Electron2D.Tree GetTree()` | Returns the current borrowed owning control or null when detached. |
| `public System.Boolean IsAcceptingChildren()` | Reports the child-drop policy. |
| `public System.Boolean IsAnyCollapsed(System.Boolean onlyVisible = false)` | Reports whether this branch contains a collapsed item. |
| `public System.Boolean IsButtonDisabled(System.Int32 column, System.Int32 buttonIndex)` | Reports the button activation policy. |
| `public System.Boolean IsChecked(System.Int32 column)` | Reports the checkbox state. |
| `public System.Boolean IsCustomSetAsButton(System.Int32 column)` | Reports the cell's CustomSetAsButton policy. |
| `public System.Boolean IsEditMultiline(System.Int32 column)` | Reports the cell's EditMultiline policy. |
| `public System.Boolean IsEditable(System.Int32 column)` | Reports the cell's Editable policy. |
| `public System.Boolean IsIndeterminate(System.Int32 column)` | Reports the indeterminate checkbox state. |
| `public System.Boolean IsSelectable(System.Int32 column)` | Reports the cell's Selectable policy. |
| `public System.Boolean IsSelected(System.Int32 column)` | Reports this cell's selection state. |
| `public System.Boolean IsVisibleInTree()` | Returns whether this row and its ancestors are locally visible. |
| `public System.Void MoveAfter(Electron2D.TreeItem item)` | Moves this complete branch after another nonroot item. |
| `public System.Void MoveBefore(Electron2D.TreeItem item)` | Moves this complete branch before another nonroot item. |
| `public System.Void PropagateCheck(System.Int32 column, System.Boolean emitSignal = true)` | Propagates a checkbox value through descendants and recomputes ancestor tri-state values. |
| `public System.Void RemoveChild(Electron2D.TreeItem child)` | Detaches a direct branch, retaining its cells and children. |
| `public System.Void Select(System.Int32 column, System.Boolean setAsCursor = true)` | Selects this cell through its owning control. |
| `public System.Void SetAcceptChildren(System.Boolean allowed)` | Sets whether drop presentation treats this row as accepting children. |
| `public System.Void SetAutoTranslateMode(System.Int32 column, Electron2D.NodeAutoTranslateMode mode)` | Sets one cell's AutoTranslateMode configuration and refreshes its presentation. |
| `public System.Void SetAutowrapMode(System.Int32 column, Electron2D.TextAutowrapMode autowrapMode)` | Sets one cell's AutowrapMode configuration and refreshes its presentation. |
| `public System.Void SetAutowrapTrimFlags(System.Int32 column, Electron2D.TextLineBreakFlags flags)` | Sets one cell's AutowrapTrimFlags configuration and refreshes its presentation. |
| `public System.Void SetButton(System.Int32 column, System.Int32 buttonIndex, Electron2D.Texture button)` | Replaces a button's borrowed texture. |
| `public System.Void SetButtonColor(System.Int32 column, System.Int32 buttonIndex, Electron2D.Color color)` | Sets a finite button tint. |
| `public System.Void SetButtonDescription(System.Int32 column, System.Int32 buttonIndex, System.String description)` | Sets source button semantic description. |
| `public System.Void SetButtonDisabled(System.Int32 column, System.Int32 buttonIndex, System.Boolean disabled)` | Sets button activation policy. |
| `public System.Void SetButtonTooltipText(System.Int32 column, System.Int32 buttonIndex, System.String tooltip)` | Sets source button tooltip text. |
| `public System.Void SetCellMode(System.Int32 column, Electron2D.TreeItem.TreeCellMode mode)` | Changes the cell mode and resets its numeric, checked, text/icon and icon-width fields. |
| `public System.Void SetChecked(System.Int32 column, System.Boolean value)` | Sets checkbox state and clears indeterminate state only when the checked state changes. |
| `public System.Void SetCollapsedRecursive(System.Boolean enable)` | Sets folding for this whole branch. |
| `public System.Void SetCustomAsButton(System.Int32 column, System.Boolean enable)` | Sets the cell's CustomAsButton policy. |
| `public System.Void SetCustomBGColor(System.Int32 column, Electron2D.Color color, System.Boolean justOutline = false)` | Sets a cell background or outline override. |
| `public System.Void SetCustomColor(System.Int32 column, Electron2D.Color color)` | Sets a cell foreground override. |
| `public System.Void SetCustomDrawCallback(System.Int32 column, System.Action<Electron2D.TreeItem, Electron2D.Rect2> callback)` | Sets a draw callback borrowing this item and its local cell rectangle. |
| `public System.Void SetCustomFont(System.Int32 column, Electron2D.Font font)` | Borrows a live custom font, or null to use the owning theme. |
| `public System.Void SetCustomFontSize(System.Int32 column, System.Int32 fontSize)` | Sets custom font size in pixels; nonpositive values use the owning theme. |
| `public System.Void SetCustomStyleBox(System.Int32 column, Electron2D.StyleBox styleBox)` | Borrows a live cell style, or null to omit it. |
| `public System.Void SetDescription(System.Int32 column, System.String description)` | Retains a runtime semantic description; native publication requires the semantic service. |
| `public System.Void SetEditMultiline(System.Int32 column, System.Boolean enable)` | Sets the cell's EditMultiline policy. |
| `public System.Void SetEditable(System.Int32 column, System.Boolean enable)` | Sets the cell's Editable policy. |
| `public System.Void SetExpandRight(System.Int32 column, System.Boolean enable)` | Allows text and cell geometry to span consecutive empty noneditable String cells to its right. |
| `public System.Void SetIcon(System.Int32 column, Electron2D.Texture texture)` | Borrows a live icon, or null to omit it. |
| `public System.Void SetIconMaxWidth(System.Int32 column, System.Int32 width)` | Sets one cell's IconMaxWidth configuration and refreshes its presentation. |
| `public System.Void SetIconModulate(System.Int32 column, Electron2D.Color modulate)` | Sets one cell's IconModulate configuration and refreshes its presentation. |
| `public System.Void SetIconOverlay(System.Int32 column, Electron2D.Texture texture)` | Borrows a live lower-right icon overlay, or null to omit it. |
| `public System.Void SetIconRegion(System.Int32 column, Electron2D.Rect2 region)` | Sets one cell's IconRegion configuration and refreshes its presentation. |
| `public System.Void SetIndeterminate(System.Int32 column, System.Boolean indeterminate)` | Sets indeterminate state and clears checked state when this state changes. |
| `public System.Void SetLanguage(System.Int32 column, System.String language)` | Sets one cell's Language configuration and refreshes its presentation. |
| `public System.Void SetMetadata<T>(System.Int32 column, T metadata)` | Stores a runtime cell payload under its exact generic type. |
| `public System.Void SetRange(System.Int32 column, System.Double value)` | Sets the numeric value, snapping on an absolute zero-based grid then clamping. |
| `public System.Void SetRangeConfig(System.Int32 column, System.Double min, System.Double max, System.Double step, System.Boolean expr = false)` | Configures finite numeric bounds, step and exponential editing scale without reclamping the retained value. |
| `public System.Void SetSelectable(System.Int32 column, System.Boolean enable)` | Sets the cell's Selectable policy. |
| `public System.Void SetStructuredTextBIDIOverride(System.Int32 column, Electron2D.StructuredTextParser parser)` | Sets one cell's StructuredTextBIDIOverride configuration and refreshes its presentation. |
| `public System.Void SetStructuredTextBIDIOverrideOptions(System.Int32 column, System.String[] args)` | Sets one cell's StructuredTextBIDIOverrideOptions configuration and refreshes its presentation. |
| `public System.Void SetSuffix(System.Int32 column, System.String text)` | Sets one cell's Suffix configuration and refreshes its presentation. |
| `public System.Void SetText(System.Int32 column, System.String text)` | Sets source text; in Range mode comma-separated label:id choices define bounds and disable snapping. |
| `public System.Void SetTextAlignment(System.Int32 column, Electron2D.HorizontalAlignment alignment)` | Sets one cell's TextAlignment configuration and refreshes its presentation. |
| `public System.Void SetTextDirection(System.Int32 column, Electron2D.TextDirection direction)` | Sets one cell's TextDirection configuration and refreshes its presentation. |
| `public System.Void SetTextOverrunBehavior(System.Int32 column, Electron2D.TextOverrunBehavior behavior)` | Sets one cell's TextOverrunBehavior configuration and refreshes its presentation. |
| `public System.Void SetTooltipText(System.Int32 column, System.String tooltip)` | Sets one cell's TooltipText configuration and refreshes its presentation. |
| `public System.Void UncollapseTree()` | Unfolds this item and every ancestor. |
| `protected override System.Void ValidateDisposal()` | Inherited typed scene/lifecycle extension point; preserves the base owner contract. |

## Methods descriptions

<a id="member-14433333f5d6"></a>

### AddButton(System.Int32, Electron2D.Texture, System.Int32, System.Boolean, System.String, System.String)

`public System.Void AddButton(System.Int32 column, Electron2D.Texture button, System.Int32 id = -1, System.Boolean disabled = false, System.String tooltipText = "", System.String description = "")`

Adds a borrowed icon button to a cell.

**Param `column`:** Existing column.

**Param `button`:** Live texture.

**Param `id`:** Signal ID, or -1 for the current button index.

**Param `disabled`:** Disables activation.

**Param `tooltipText`:** Source tooltip.

**Param `description`:** Semantic description.

<a id="member-e445ce834fc1"></a>

### AddChild(Electron2D.TreeItem)

`public System.Void AddChild(Electron2D.TreeItem child)`

Attaches an unowned detached branch as the final child.

**Param `child`:** Live detached item; cycles are rejected.

<a id="member-d985525abcf0"></a>

### CallRecursive(System.Action<Electron2D.TreeItem>)

`public System.Void CallRecursive(System.Action<Electron2D.TreeItem> callback)`

Applies a typed callback to a captured depth-first subtree, continuing after callback errors.

**Param `callback`:** An action borrowing each still-live item.

<a id="member-f231361c4b24"></a>

### ClearButtons()

`public System.Void ClearButtons()`

Clears all buttons on this row.

<a id="member-43cbf97d919a"></a>

### ClearCustomBGColor(System.Int32)

`public System.Void ClearCustomBGColor(System.Int32 column)`

Disables the background override.

**Param `column`:** Existing column.

<a id="member-21172fd46442"></a>

### ClearCustomColor(System.Int32)

`public System.Void ClearCustomColor(System.Int32 column)`

Disables the cell foreground override.

**Param `column`:** Existing column.

<a id="member-0baf7d8f83d0"></a>

### CreateChild(System.Int32)

`public Electron2D.TreeItem CreateChild(System.Int32 index = -1)`

Creates a child at a clamped index, or appends for negative indices.

**Param `index`:** Insertion index or -1.

**Returns:** New owned child.

<a id="member-96f29825bfda"></a>

### Deselect(System.Int32)

`public System.Void Deselect(System.Int32 column)`

Deselects this cell through its owning control.

**Param `column`:** Existing column.

<a id="member-94b71b3245f9"></a>

### Dispose(System.Boolean)

`protected override System.Void Dispose(System.Boolean disposing)`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

<a id="member-afe4c6a21aec"></a>

### EraseButton(System.Int32, System.Int32)

`public System.Void EraseButton(System.Int32 column, System.Int32 buttonIndex)`

Removes a button by its cell index.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Existing button index.

<a id="member-d765df062315"></a>

### GetAutoTranslateMode(System.Int32)

`public Electron2D.NodeAutoTranslateMode GetAutoTranslateMode(System.Int32 column)`

Returns one cell's AutoTranslateMode configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-1c0a4c3d89a8"></a>

### GetAutowrapMode(System.Int32)

`public Electron2D.TextAutowrapMode GetAutowrapMode(System.Int32 column)`

Returns one cell's AutowrapMode configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-6d1b8518c4e9"></a>

### GetAutowrapTrimFlags(System.Int32)

`public Electron2D.TextLineBreakFlags GetAutowrapTrimFlags(System.Int32 column)`

Returns one cell's AutowrapTrimFlags configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-efa4b313478d"></a>

### GetButton(System.Int32, System.Int32)

`public Electron2D.Texture GetButton(System.Int32 column, System.Int32 buttonIndex)`

Returns a live borrowed button texture.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Button index.

**Returns:** Texture or null after disposal.

<a id="member-daf0dc101cf6"></a>

### GetButtonByID(System.Int32, System.Int32)

`public System.Int32 GetButtonByID(System.Int32 column, System.Int32 id)`

Returns the first index with an exact signal ID.

**Param `column`:** Existing column.

**Param `id`:** Signal ID.

**Returns:** Index or -1.

<a id="member-1d7557232d2a"></a>

### GetButtonColor(System.Int32, System.Int32)

`public Electron2D.Color GetButtonColor(System.Int32 column, System.Int32 id)`

Returns the button tint by button index.

**Param `column`:** Existing column.

**Param `id`:** Button index despite the reference parameter name.

**Returns:** Finite color.

<a id="member-1ef371be8830"></a>

### GetButtonCount(System.Int32)

`public System.Int32 GetButtonCount(System.Int32 column)`

Returns cell button count.

**Param `column`:** Existing column.

**Returns:** Count.

<a id="member-a61314af24b9"></a>

### GetButtonID(System.Int32, System.Int32)

`public System.Int32 GetButtonID(System.Int32 column, System.Int32 buttonIndex)`

Returns a button's retained signal ID.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Button index.

**Returns:** Signal ID.

<a id="member-b90231b92b70"></a>

### GetButtonTooltipText(System.Int32, System.Int32)

`public System.String GetButtonTooltipText(System.Int32 column, System.Int32 buttonIndex)`

Returns source button tooltip text.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Button index.

**Returns:** Source tooltip.

<a id="member-be1fccb6f4b3"></a>

### GetCellMode(System.Int32)

`public Electron2D.TreeItem.TreeCellMode GetCellMode(System.Int32 column)`

Returns one cell's CellMode configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-eb76593125b5"></a>

### GetChild(System.Int32)

`public Electron2D.TreeItem GetChild(System.Int32 index)`

Returns one direct child, accepting negative indices from the end.

**Param `index`:** Child index.

**Returns:** Borrowed item.

<a id="member-4822342f721a"></a>

### GetChildCount()

`public System.Int32 GetChildCount()`

Returns the number of direct children.

**Returns:** Child count.

<a id="member-e3382f942534"></a>

### GetChildren()

`public Electron2D.TreeItem[] GetChildren()`

Returns an independent array of borrowed direct children.

**Returns:** Child order.

<a id="member-cd08c8d55a79"></a>

### GetCustomBGColor(System.Int32)

`public Electron2D.Color GetCustomBGColor(System.Int32 column)`

Returns the retained background color.

**Param `column`:** Existing column.

**Returns:** Configured color.

<a id="member-0467e6a7b2f3"></a>

### GetCustomColor(System.Int32)

`public Electron2D.Color GetCustomColor(System.Int32 column)`

Returns the retained foreground color.

**Param `column`:** Existing column.

**Returns:** Configured color.

<a id="member-3b60afe4949a"></a>

### GetCustomDrawCallback(System.Int32)

`public System.Action<Electron2D.TreeItem, Electron2D.Rect2> GetCustomDrawCallback(System.Int32 column)`

Returns the retained typed cell draw callback.

**Param `column`:** Existing column.

**Returns:** Callback or null.

<a id="member-f33caf8f270f"></a>

### GetCustomFont(System.Int32)

`public Electron2D.Font GetCustomFont(System.Int32 column)`

Returns one cell's CustomFont configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-8e675fc13e1c"></a>

### GetCustomFontSize(System.Int32)

`public System.Int32 GetCustomFontSize(System.Int32 column)`

Returns one cell's CustomFontSize configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-12313fa14d20"></a>

### GetCustomStyleBox(System.Int32)

`public Electron2D.StyleBox GetCustomStyleBox(System.Int32 column)`

Returns one cell's CustomStyleBox configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-a0b8a07dc407"></a>

### GetDescription(System.Int32)

`public System.String GetDescription(System.Int32 column)`

Returns one cell's Description configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-9628f672d7ba"></a>

### GetExpandRight(System.Int32)

`public System.Boolean GetExpandRight(System.Int32 column)`

Returns one cell's ExpandRight configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-4797e7eea89b"></a>

### GetFirstChild()

`public Electron2D.TreeItem GetFirstChild()`

Returns the first direct child or null.

**Returns:** Borrowed child.

<a id="member-b1004bdf1913"></a>

### GetIcon(System.Int32)

`public Electron2D.Texture GetIcon(System.Int32 column)`

Returns one cell's Icon configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-42ff9964d07b"></a>

### GetIconMaxWidth(System.Int32)

`public System.Int32 GetIconMaxWidth(System.Int32 column)`

Returns one cell's IconMaxWidth configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-54cf3de35a99"></a>

### GetIconModulate(System.Int32)

`public Electron2D.Color GetIconModulate(System.Int32 column)`

Returns one cell's IconModulate configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-4c506d671a6b"></a>

### GetIconOverlay(System.Int32)

`public Electron2D.Texture GetIconOverlay(System.Int32 column)`

Returns one cell's IconOverlay configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-a8422e2a5c4a"></a>

### GetIconRegion(System.Int32)

`public Electron2D.Rect2 GetIconRegion(System.Int32 column)`

Returns one cell's IconRegion configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-73e86c651911"></a>

### GetIndex()

`public System.Int32 GetIndex()`

Returns this item's sibling index.

**Returns:** Zero for a root/detached item.

<a id="member-2fd4edb3596e"></a>

### GetLanguage(System.Int32)

`public System.String GetLanguage(System.Int32 column)`

Returns one cell's Language configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-ea983dfe54d8"></a>

### GetMetadata(System.Int32)

`public T GetMetadata<T>(System.Int32 column)`

Retrieves a runtime cell payload of the exact generic type.

**Param `column`:** Existing column.

**Typeparam `T`:** Stored type.

**Returns:** Borrowed payload.

<a id="member-6c42023010b5"></a>

### GetNext()

`public Electron2D.TreeItem GetNext()`

Returns the next sibling or null.

**Returns:** Borrowed sibling.

<a id="member-ebbce0f10878"></a>

### GetNextInTree(System.Boolean)

`public Electron2D.TreeItem GetNextInTree(System.Boolean wrap = false)`

Returns the following visible-in-hierarchy item in depth-first order, ignoring folding.

**Param `wrap`:** Wraps to the branch start.

**Returns:** Borrowed item or null.

<a id="member-91fbd2644b26"></a>

### GetNextVisible(System.Boolean)

`public Electron2D.TreeItem GetNextVisible(System.Boolean wrap = false)`

Returns the following presented row, respecting folding.

**Param `wrap`:** Wraps to the branch start.

**Returns:** Borrowed item or null.

<a id="member-a01f5b48787c"></a>

### GetParent()

`public Electron2D.TreeItem GetParent()`

Returns the parent item or null for a root/detached branch.

**Returns:** Borrowed parent.

<a id="member-16ea2628bae9"></a>

### GetPrev()

`public Electron2D.TreeItem GetPrev()`

Returns the preceding sibling or null.

**Returns:** Borrowed sibling.

<a id="member-f017aa41b289"></a>

### GetPrevInTree(System.Boolean)

`public Electron2D.TreeItem GetPrevInTree(System.Boolean wrap = false)`

Returns the preceding visible-in-hierarchy item, ignoring folding.

**Param `wrap`:** Wraps to the branch end.

**Returns:** Borrowed item or null.

<a id="member-5e3783680f43"></a>

### GetPrevVisible(System.Boolean)

`public Electron2D.TreeItem GetPrevVisible(System.Boolean wrap = false)`

Returns the preceding presented row, respecting folding.

**Param `wrap`:** Wraps to the branch end.

**Returns:** Borrowed item or null.

<a id="member-3ba3de8d7e3b"></a>

### GetPropertyDescriptors()

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

<a id="member-69ac524b1ad2"></a>

### GetRange(System.Int32)

`public System.Double GetRange(System.Int32 column)`

Returns the retained numeric value.

**Param `column`:** Existing column.

**Returns:** Numeric value, zero initially.

<a id="member-15796a8e6a5f"></a>

### GetRangeConfig(System.Int32)

`public System.ValueTuple<System.Double, System.Double, System.Double, System.Boolean> GetRangeConfig(System.Int32 column)`

Returns the complete typed numeric configuration, including the configured scale policy.

**Param `column`:** Existing column.

**Returns:** Minimum, maximum, step and exponential scale.

<a id="member-99f294553e72"></a>

### GetStructuredTextBIDIOverride(System.Int32)

`public Electron2D.StructuredTextParser GetStructuredTextBIDIOverride(System.Int32 column)`

Returns one cell's StructuredTextBIDIOverride configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-7c26036a694a"></a>

### GetStructuredTextBIDIOverrideOptions(System.Int32)

`public System.String[] GetStructuredTextBIDIOverrideOptions(System.Int32 column)`

Returns one cell's StructuredTextBIDIOverrideOptions configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value; copied array.

<a id="member-0449949417bf"></a>

### GetSuffix(System.Int32)

`public System.String GetSuffix(System.Int32 column)`

Returns one cell's Suffix configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-0a706f59ce1d"></a>

### GetText(System.Int32)

`public System.String GetText(System.Int32 column)`

Returns one cell's Text configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-c2baeea117d3"></a>

### GetTextAlignment(System.Int32)

`public Electron2D.HorizontalAlignment GetTextAlignment(System.Int32 column)`

Returns one cell's TextAlignment configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-9656711b13a6"></a>

### GetTextDirection(System.Int32)

`public Electron2D.TextDirection GetTextDirection(System.Int32 column)`

Returns one cell's TextDirection configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-59a4823cdfb4"></a>

### GetTextOverrunBehavior(System.Int32)

`public Electron2D.TextOverrunBehavior GetTextOverrunBehavior(System.Int32 column)`

Returns one cell's TextOverrunBehavior configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-486027a37877"></a>

### GetTooltipText(System.Int32)

`public System.String GetTooltipText(System.Int32 column)`

Returns one cell's TooltipText configuration.

**Param `column`:** Existing column.

**Returns:** Configured typed value.

<a id="member-b30dd67929ea"></a>

### GetTree()

`public Electron2D.Tree GetTree()`

Returns the current borrowed owning control or null when detached.

**Returns:** The owning Tree.

<a id="member-cc19ae565098"></a>

### IsAcceptingChildren()

`public System.Boolean IsAcceptingChildren()`

Reports the child-drop policy.

**Returns:** True initially.

<a id="member-825f330f4cd1"></a>

### IsAnyCollapsed(System.Boolean)

`public System.Boolean IsAnyCollapsed(System.Boolean onlyVisible = false)`

Reports whether this branch contains a collapsed item.

**Param `onlyVisible`:** Omits locally hidden branches.

**Returns:** Collapse presence.

<a id="member-883e146025af"></a>

### IsButtonDisabled(System.Int32, System.Int32)

`public System.Boolean IsButtonDisabled(System.Int32 column, System.Int32 buttonIndex)`

Reports the button activation policy.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Button index.

**Returns:** Disabled state.

<a id="member-ff573fb9802b"></a>

### IsChecked(System.Int32)

`public System.Boolean IsChecked(System.Int32 column)`

Reports the checkbox state.

**Param `column`:** Existing column.

**Returns:** Checked state.

<a id="member-dc4abf4cca9c"></a>

### IsCustomSetAsButton(System.Int32)

`public System.Boolean IsCustomSetAsButton(System.Int32 column)`

Reports the cell's CustomSetAsButton policy.

**Param `column`:** Existing column.

**Returns:** Configured state.

<a id="member-8b41f1fa1fdd"></a>

### IsEditMultiline(System.Int32)

`public System.Boolean IsEditMultiline(System.Int32 column)`

Reports the cell's EditMultiline policy.

**Param `column`:** Existing column.

**Returns:** Configured state.

<a id="member-8054a2e929c7"></a>

### IsEditable(System.Int32)

`public System.Boolean IsEditable(System.Int32 column)`

Reports the cell's Editable policy.

**Param `column`:** Existing column.

**Returns:** Configured state.

<a id="member-0c3964eb3fa7"></a>

### IsIndeterminate(System.Int32)

`public System.Boolean IsIndeterminate(System.Int32 column)`

Reports the indeterminate checkbox state.

**Param `column`:** Existing column.

**Returns:** Indeterminate state.

<a id="member-955f90b785a6"></a>

### IsSelectable(System.Int32)

`public System.Boolean IsSelectable(System.Int32 column)`

Reports the cell's Selectable policy.

**Param `column`:** Existing column.

**Returns:** Configured state.

<a id="member-8392ea45daf0"></a>

### IsSelected(System.Int32)

`public System.Boolean IsSelected(System.Int32 column)`

Reports this cell's selection state.

**Param `column`:** Existing column.

**Returns:** Selection state.

<a id="member-190f5e42feb4"></a>

### IsVisibleInTree()

`public System.Boolean IsVisibleInTree()`

Returns whether this row and its ancestors are locally visible.

**Returns:** Visibility independent of folding.

<a id="member-ca0a9266bee4"></a>

### MoveAfter(Electron2D.TreeItem)

`public System.Void MoveAfter(Electron2D.TreeItem item)`

Moves this complete branch after another nonroot item.

**Param `item`:** Destination sibling.

<a id="member-6d403e00f09e"></a>

### MoveBefore(Electron2D.TreeItem)

`public System.Void MoveBefore(Electron2D.TreeItem item)`

Moves this complete branch before another nonroot item.

**Param `item`:** Destination sibling.

<a id="member-18d2501a0088"></a>

### PropagateCheck(System.Int32, System.Boolean)

`public System.Void PropagateCheck(System.Int32 column, System.Boolean emitSignal = true)`

Propagates a checkbox value through descendants and recomputes ancestor tri-state values.

**Param `column`:** Existing column.

**Param `emitSignal`:** Publishes each propagated item through the owning Tree.

<a id="member-bfb724b34348"></a>

### RemoveChild(Electron2D.TreeItem)

`public System.Void RemoveChild(Electron2D.TreeItem child)`

Detaches a direct branch, retaining its cells and children.

**Param `child`:** Direct child.

<a id="member-a6ed6107282f"></a>

### Select(System.Int32, System.Boolean)

`public System.Void Select(System.Int32 column, System.Boolean setAsCursor = true)`

Selects this cell through its owning control.

**Param `column`:** Existing column.

**Param `setAsCursor`:** Sets the selection cursor.

<a id="member-7456fea92294"></a>

### SetAcceptChildren(System.Boolean)

`public System.Void SetAcceptChildren(System.Boolean allowed)`

Sets whether drop presentation treats this row as accepting children.

**Param `allowed`:** Child-drop policy.

<a id="member-f16e80ae32c4"></a>

### SetAutoTranslateMode(System.Int32, Electron2D.NodeAutoTranslateMode)

`public System.Void SetAutoTranslateMode(System.Int32 column, Electron2D.NodeAutoTranslateMode mode)`

Sets one cell's AutoTranslateMode configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `mode`:** Typed configuration.

<a id="member-efa41554c444"></a>

### SetAutowrapMode(System.Int32, Electron2D.TextAutowrapMode)

`public System.Void SetAutowrapMode(System.Int32 column, Electron2D.TextAutowrapMode autowrapMode)`

Sets one cell's AutowrapMode configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `autowrapMode`:** Typed configuration.

<a id="member-c7402ff83293"></a>

### SetAutowrapTrimFlags(System.Int32, Electron2D.TextLineBreakFlags)

`public System.Void SetAutowrapTrimFlags(System.Int32 column, Electron2D.TextLineBreakFlags flags)`

Sets one cell's AutowrapTrimFlags configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `flags`:** Typed configuration.

<a id="member-7451ce6d81d3"></a>

### SetButton(System.Int32, System.Int32, Electron2D.Texture)

`public System.Void SetButton(System.Int32 column, System.Int32 buttonIndex, Electron2D.Texture button)`

Replaces a button's borrowed texture.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Button index.

**Param `button`:** Live texture.

<a id="member-bfe8f5d363cb"></a>

### SetButtonColor(System.Int32, System.Int32, Electron2D.Color)

`public System.Void SetButtonColor(System.Int32 column, System.Int32 buttonIndex, Electron2D.Color color)`

Sets a finite button tint.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Button index.

**Param `color`:** Finite color.

<a id="member-da564189ed3c"></a>

### SetButtonDescription(System.Int32, System.Int32, System.String)

`public System.Void SetButtonDescription(System.Int32 column, System.Int32 buttonIndex, System.String description)`

Sets source button semantic description.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Button index.

**Param `description`:** Nonnull description.

<a id="member-2fa550ed4474"></a>

### SetButtonDisabled(System.Int32, System.Int32, System.Boolean)

`public System.Void SetButtonDisabled(System.Int32 column, System.Int32 buttonIndex, System.Boolean disabled)`

Sets button activation policy.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Button index.

**Param `disabled`:** Disabled state.

<a id="member-73121c948b84"></a>

### SetButtonTooltipText(System.Int32, System.Int32, System.String)

`public System.Void SetButtonTooltipText(System.Int32 column, System.Int32 buttonIndex, System.String tooltip)`

Sets source button tooltip text.

**Param `column`:** Existing column.

**Param `buttonIndex`:** Button index.

**Param `tooltip`:** Nonnull tooltip.

<a id="member-0ebddeac86c7"></a>

### SetCellMode(System.Int32, Electron2D.TreeItem.TreeCellMode)

`public System.Void SetCellMode(System.Int32 column, Electron2D.TreeItem.TreeCellMode mode)`

Changes the cell mode and resets its numeric, checked, text/icon and icon-width fields.

**Param `column`:** Existing column.

**Param `mode`:** Typed configuration.

<a id="member-4f1463bdc816"></a>

### SetChecked(System.Int32, System.Boolean)

`public System.Void SetChecked(System.Int32 column, System.Boolean value)`

Sets checkbox state and clears indeterminate state only when the checked state changes.

**Param `column`:** Existing column.

**Param `value`:** Desired check state.

<a id="member-bc69760227e7"></a>

### SetCollapsedRecursive(System.Boolean)

`public System.Void SetCollapsedRecursive(System.Boolean enable)`

Sets folding for this whole branch.

**Param `enable`:** Collapsed state.

<a id="member-d13ce2f51acc"></a>

### SetCustomAsButton(System.Int32, System.Boolean)

`public System.Void SetCustomAsButton(System.Int32 column, System.Boolean enable)`

Sets the cell's CustomAsButton policy.

**Param `column`:** Existing column.

**Param `enable`:** Desired policy.

<a id="member-ef5163de102c"></a>

### SetCustomBGColor(System.Int32, Electron2D.Color, System.Boolean)

`public System.Void SetCustomBGColor(System.Int32 column, Electron2D.Color color, System.Boolean justOutline = false)`

Sets a cell background or outline override.

**Param `column`:** Existing column.

**Param `color`:** Finite color.

**Param `justOutline`:** Draws only an outline.

<a id="member-60431fb04a0b"></a>

### SetCustomColor(System.Int32, Electron2D.Color)

`public System.Void SetCustomColor(System.Int32 column, Electron2D.Color color)`

Sets a cell foreground override.

**Param `column`:** Existing column.

**Param `color`:** Finite color.

<a id="member-c842d6e7a817"></a>

### SetCustomDrawCallback(System.Int32, System.Action<Electron2D.TreeItem, Electron2D.Rect2>)

`public System.Void SetCustomDrawCallback(System.Int32 column, System.Action<Electron2D.TreeItem, Electron2D.Rect2> callback)`

Sets a draw callback borrowing this item and its local cell rectangle.

**Param `column`:** Existing column.

**Param `callback`:** Callback, or null. Use GetTree().Draw* inside this scope.

<a id="member-4acdc13f3e9e"></a>

### SetCustomFont(System.Int32, Electron2D.Font)

`public System.Void SetCustomFont(System.Int32 column, Electron2D.Font font)`

Borrows a live custom font, or null to use the owning theme.

**Param `column`:** Existing column.

**Param `font`:** Typed configuration.

<a id="member-4604a28fc975"></a>

### SetCustomFontSize(System.Int32, System.Int32)

`public System.Void SetCustomFontSize(System.Int32 column, System.Int32 fontSize)`

Sets custom font size in pixels; nonpositive values use the owning theme.

**Param `column`:** Existing column.

**Param `fontSize`:** Typed configuration.

<a id="member-1c5409ccfacb"></a>

### SetCustomStyleBox(System.Int32, Electron2D.StyleBox)

`public System.Void SetCustomStyleBox(System.Int32 column, Electron2D.StyleBox styleBox)`

Borrows a live cell style, or null to omit it.

**Param `column`:** Existing column.

**Param `styleBox`:** Typed configuration.

<a id="member-a7e406aa37be"></a>

### SetDescription(System.Int32, System.String)

`public System.Void SetDescription(System.Int32 column, System.String description)`

Retains a runtime semantic description; native publication requires the semantic service.

**Param `column`:** Existing column.

**Param `description`:** Typed configuration.

<a id="member-79d31686f5f1"></a>

### SetEditMultiline(System.Int32, System.Boolean)

`public System.Void SetEditMultiline(System.Int32 column, System.Boolean enable)`

Sets the cell's EditMultiline policy.

**Param `column`:** Existing column.

**Param `enable`:** Desired policy.

<a id="member-0876c4715b8b"></a>

### SetEditable(System.Int32, System.Boolean)

`public System.Void SetEditable(System.Int32 column, System.Boolean enable)`

Sets the cell's Editable policy.

**Param `column`:** Existing column.

**Param `enable`:** Desired policy.

<a id="member-a7179f254e9d"></a>

### SetExpandRight(System.Int32, System.Boolean)

`public System.Void SetExpandRight(System.Int32 column, System.Boolean enable)`

Allows text and cell geometry to span consecutive empty noneditable String cells to its right.

**Param `column`:** Existing column.

**Param `enable`:** Typed configuration.

<a id="member-cb1498b756a2"></a>

### SetIcon(System.Int32, Electron2D.Texture)

`public System.Void SetIcon(System.Int32 column, Electron2D.Texture texture)`

Borrows a live icon, or null to omit it.

**Param `column`:** Existing column.

**Param `texture`:** Typed configuration.

<a id="member-9ff0e701735a"></a>

### SetIconMaxWidth(System.Int32, System.Int32)

`public System.Void SetIconMaxWidth(System.Int32 column, System.Int32 width)`

Sets one cell's IconMaxWidth configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `width`:** Typed configuration.

<a id="member-134accc5387d"></a>

### SetIconModulate(System.Int32, Electron2D.Color)

`public System.Void SetIconModulate(System.Int32 column, Electron2D.Color modulate)`

Sets one cell's IconModulate configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `modulate`:** Typed configuration.

<a id="member-68defb63770d"></a>

### SetIconOverlay(System.Int32, Electron2D.Texture)

`public System.Void SetIconOverlay(System.Int32 column, Electron2D.Texture texture)`

Borrows a live lower-right icon overlay, or null to omit it.

**Param `column`:** Existing column.

**Param `texture`:** Typed configuration.

<a id="member-bf27ea260087"></a>

### SetIconRegion(System.Int32, Electron2D.Rect2)

`public System.Void SetIconRegion(System.Int32 column, Electron2D.Rect2 region)`

Sets one cell's IconRegion configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `region`:** Typed configuration.

<a id="member-98e82dccb034"></a>

### SetIndeterminate(System.Int32, System.Boolean)

`public System.Void SetIndeterminate(System.Int32 column, System.Boolean indeterminate)`

Sets indeterminate state and clears checked state when this state changes.

**Param `column`:** Existing column.

**Param `indeterminate`:** Desired tri-state state.

<a id="member-1d5c26a6dcde"></a>

### SetLanguage(System.Int32, System.String)

`public System.Void SetLanguage(System.Int32 column, System.String language)`

Sets one cell's Language configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `language`:** Typed configuration.

<a id="member-40c7951e9b11"></a>

### SetMetadata(System.Int32, T)

`public System.Void SetMetadata<T>(System.Int32 column, T metadata)`

Stores a runtime cell payload under its exact generic type.

**Param `column`:** Existing column.

**Param `metadata`:** Borrowed payload.

**Typeparam `T`:** Payload type.

<a id="member-a50b43f3ea67"></a>

### SetRange(System.Int32, System.Double)

`public System.Void SetRange(System.Int32 column, System.Double value)`

Sets the numeric value, snapping on an absolute zero-based grid then clamping.

**Param `column`:** Existing column.

**Param `value`:** IEEE numeric value.

<a id="member-004b9d49c1f1"></a>

### SetRangeConfig(System.Int32, System.Double, System.Double, System.Double, System.Boolean)

`public System.Void SetRangeConfig(System.Int32 column, System.Double min, System.Double max, System.Double step, System.Boolean expr = false)`

Configures finite numeric bounds, step and exponential editing scale without reclamping the retained value.

**Param `column`:** Existing column.

**Param `min`:** Finite minimum.

**Param `max`:** Finite maximum.

**Param `step`:** Finite step; nonpositive disables snapping.

**Param `expr`:** Exponential edit scale.

<a id="member-0b386558d343"></a>

### SetSelectable(System.Int32, System.Boolean)

`public System.Void SetSelectable(System.Int32 column, System.Boolean enable)`

Sets the cell's Selectable policy.

**Param `column`:** Existing column.

**Param `enable`:** Desired policy.

<a id="member-5d2de2c69269"></a>

### SetStructuredTextBIDIOverride(System.Int32, Electron2D.StructuredTextParser)

`public System.Void SetStructuredTextBIDIOverride(System.Int32 column, Electron2D.StructuredTextParser parser)`

Sets one cell's StructuredTextBIDIOverride configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `parser`:** Typed configuration.

<a id="member-89f6ea30520f"></a>

### SetStructuredTextBIDIOverrideOptions(System.Int32, System.String[])

`public System.Void SetStructuredTextBIDIOverrideOptions(System.Int32 column, System.String[] args)`

Sets one cell's StructuredTextBIDIOverrideOptions configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `args`:** Typed configuration.

<a id="member-1946d576a6e9"></a>

### SetSuffix(System.Int32, System.String)

`public System.Void SetSuffix(System.Int32 column, System.String text)`

Sets one cell's Suffix configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `text`:** Typed configuration.

<a id="member-bc5e65e74a5c"></a>

### SetText(System.Int32, System.String)

`public System.Void SetText(System.Int32 column, System.String text)`

Sets source text; in Range mode comma-separated label:id choices define bounds and disable snapping.

**Param `column`:** Existing column.

**Param `text`:** Typed configuration.

<a id="member-ddce5cd249f0"></a>

### SetTextAlignment(System.Int32, Electron2D.HorizontalAlignment)

`public System.Void SetTextAlignment(System.Int32 column, Electron2D.HorizontalAlignment alignment)`

Sets one cell's TextAlignment configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `alignment`:** Typed configuration.

<a id="member-f45e3ff6746f"></a>

### SetTextDirection(System.Int32, Electron2D.TextDirection)

`public System.Void SetTextDirection(System.Int32 column, Electron2D.TextDirection direction)`

Sets one cell's TextDirection configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `direction`:** Typed configuration.

<a id="member-6a8b9c453669"></a>

### SetTextOverrunBehavior(System.Int32, Electron2D.TextOverrunBehavior)

`public System.Void SetTextOverrunBehavior(System.Int32 column, Electron2D.TextOverrunBehavior behavior)`

Sets one cell's TextOverrunBehavior configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `behavior`:** Typed configuration.

<a id="member-93f5b4d61722"></a>

### SetTooltipText(System.Int32, System.String)

`public System.Void SetTooltipText(System.Int32 column, System.String tooltip)`

Sets one cell's TooltipText configuration and refreshes its presentation.

**Param `column`:** Existing column.

**Param `tooltip`:** Typed configuration.

<a id="member-b2b575aacb5e"></a>

### UncollapseTree()

`public System.Void UncollapseTree()`

Unfolds this item and every ancestor.

<a id="member-26cb025eacf9"></a>

### ValidateDisposal()

`protected override System.Void ValidateDisposal()`

Overrides the inherited owner contract for this concrete type. See the base class and lifecycle description.

## Properties

| Declaration | Contract |
| --- | --- |
| `public System.Boolean Collapsed { get; set; }` | Gets or sets whether this branch's children are folded. |
| `public System.Int32 CustomMinimumHeight { get; set; }` | Gets or sets a nonnegative row minimum height in pixels. |
| `public System.Boolean DisableFolding { get; set; }` | Gets or sets whether the row has its own folding control. |
| `public System.Boolean Visible { get; set; }` | Gets or sets local branch visibility. |

## Properties descriptions

<a id="member-e8256503f40b"></a>

### Collapsed

`public System.Boolean Collapsed { get; set; }`

Gets or sets whether this branch's children are folded.

**Value:** False initially.

<a id="member-342f131322f8"></a>

### CustomMinimumHeight

`public System.Int32 CustomMinimumHeight { get; set; }`

Gets or sets a nonnegative row minimum height in pixels.

**Value:** Zero initially.

<a id="member-8d147bc4326c"></a>

### DisableFolding

`public System.Boolean DisableFolding { get; set; }`

Gets or sets whether the row has its own folding control.

**Value:** False initially.

<a id="member-8d205c38ee00"></a>

### Visible

`public System.Boolean Visible { get; set; }`

Gets or sets local branch visibility.

**Value:** True initially; hidden ancestors hide descendants.

## Lifecycle, invariants and verification

Attached operations require the owning scene thread and a live item/control. Invalid indices, invalid enum values, nonfinite required configuration and ownership/cycle violations throw before mutation. Item/metadata/resource lifetimes follow the description above. See [hierarchical cells](../components/hierarchical-cells.md) for runtime flow, exercised editor and native workflows, allocation boundaries and dependency limits; [Tree coverage](../coverage/classes/Tree.md) and [TreeItem coverage](../coverage/classes/TreeItem.md) account for the applicable comparison surface. Relevant decisions: ADRs 0004, 0008, 0014, 0023, 0038, 0046, 0051 and 0083.
