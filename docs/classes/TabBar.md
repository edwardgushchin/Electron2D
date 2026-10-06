# TabBar

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public partial class Electron2D.TabBar : Control`. **Source:** [model](../../src/Scene/GUI/TabBar.cs), [layout/drawing](../../src/Scene/GUI/TabBar.Layout.cs), [input/drag](../../src/Scene/GUI/TabBar.Input.cs), [stored fields](../../src/Scene/GUI/TabBar.Storage.cs).

**Inherits:** [Control](Control.md), CanvasItem, Node, ElectronObject. **Component:** [Tab strips](../components/tab-strips.md).

## Description

A TabBar owns ordered tab records and one internal one-shot hover timer. Each record owns two prepared text-layout caches and borrows its title/auxiliary icons and exact typed runtime metadata. The two caches retain intrinsic and clipped text independently, avoiding reshaping when selection changes. Ordinary child enumeration and packing omit the timer; each instance creates its own. Borrowed texture changes invalidate geometry, and disposing a shared icon clears both roles without disposing other resources.

AddTab selects the first detached tab silently; attached first addition calls CurrentTab. Count edits clamp selection without selection events, clear resets both indices, and MoveTab tracks selected/previous identities without selection events. CurrentTab allows programmatic hidden/disabled selections; ordinary navigation skips them without wrapping. Deselection requires DeselectEnabled or no available tab. Every valid assignment emits TabSelected, including equal selection; changed selection then emits TabChanged. State/layout commit precedes callbacks, obsolete outer change events are suppressed after nested edits, and collected observer failures report after remaining delivery. Removing an attached current tab emits only TabChanged after choosing a forward/backward available replacement.

ClipTabs bounds own rendering and chooses whole visible tabs with navigation arrows; one oversized first tab is clipped. Maximum width trims shaped text while preserving icon/button/style content, so non-text content can exceed the requested cap. RTL mirrors the strip and arrow semantics, while TabAlignment remains logical leading/center/trailing. Hidden tabs do not consume space or create unnecessary trailing navigation. GetTabRect uses the cached offset even for an off-page tab; GetTabIdxAtPoint only accepts the displayed nonhidden page and excludes arrows. EnsureTabVisible ignores -1, hidden tabs and detached calls, otherwise changes scroll state without selecting. ScrollingEnabled gates wheels; arrows stay usable. Control's normal minimum/maximum/desired-size contract applies.

Pointer selection emits Selected, Changed when needed, then Clicked; right presses always emit TabRMBClicked on a tab, with optional selection before it. Close controls emit requests without removal. Disabled tabs cannot use their close control but still expose the auxiliary icon and middle-close request. A release over a different button cancels the original press. Focused action keys and held controllers navigate available tabs with mirrored RTL directions; controller repeat stops on release/focus loss. A foreign GUI drag may switch an enabled visible tab after the themed delay; tab drags do not switch on hover. Disabling the option stops pending switching.

Drag payloads use tab object identity, surviving source reordering and rejecting removal/disposal. Same-strip drops move the full record and emit ActiveTabRearranged before selection; equal positions do nothing. Transfer requires DragToRearrangeEnabled, matching group values other than -1, the same tree and capacity. It moves borrowed metadata/icons and owned layout caches without copying. Both collections commit before observers; source selection chooses a replacement and the destination selects an available moved tab. A real root-viewport drag adds a translated Label preview through the existing typed GUI route. Group -1 permits only same-strip drops.

Count is bounded to 65,536; indices are zero-based and negative getters/setters reject except explicit -1 selection/reveal. Attached mutations require the scene owner and reject capture phases. Shaping callbacks cannot edit or dispose the strip. Layout/draw queries may allocate on first preparation or changes; warmed repeated selection/layout/record/render reuses prepared state. Metadata uses SetTabMetadata<T>/GetTabMetadata<T> with exact T; missing or mismatched types throw KeyNotFoundException and typed null remains valid. There is no untyped public value container.

## Example

```csharp
var tabs = new TabBar { Name = "Documents", Size = new(300, 40) };
tabs.AddTab("Scene");
tabs.AddTab("Resources");
tabs.TabChanged += index => Console.WriteLine(tabs.GetTabTitle(index));
tabs.TabClosePressed += index => tabs.RemoveTab(index);
// Attach to an existing Window or Control using AddChild(tabs).
```

## Persistence

PackedScene and e2dscene files store count, configuration, CurrentTab, default All focus and indexed title/tooltip/icon/disabled fields. Count precedes indexed data; CurrentTab restores after fields so deselection/unavailable configuration is ready. Language/direction, hidden/button/max-icon state and typed metadata remain runtime fields, matching the declared stored profile. Every packed/file instance creates independent tab containers and an internal timer. TabBarTests includes actual file save, fresh-process load/run and failure validation.

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public TabBar()` | Creates an empty strip with All focus and one owned internal hover timer. |

## Constructor Descriptions

<a id="member-fad625b956c8"></a>
### .ctor

`public TabBar()`

Summary: Creates an empty strip with All focus and one owned internal hover timer.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean ClipTabs { get; set; }` | Gets or sets whether overflow tabs are hidden and scroll arrows are displayed. |
| `public System.Boolean CloseWithMiddleMouse { get; set; }` | Gets or sets whether a middle-button press requests closing the hovered tab. |
| `public System.Int32 CurrentTab { get; set; }` | Gets or sets the selected index, including programmatic hidden or disabled selections. |
| `public System.Boolean DeselectEnabled { get; set; }` | Gets or sets whether an available selection may be cleared. |
| `public System.Boolean DragToRearrangeEnabled { get; set; }` | Gets or sets whether typed tab drags can rearrange or transfer tabs. |
| `public System.Int32 MaxTabWidth { get; set; }` | Gets or sets the measured tab width cap, preserving non-text content. |
| `public System.Boolean ScrollToSelected { get; set; }` | Gets or sets automatic scrolling after selection or layout edits. |
| `public System.Boolean ScrollingEnabled { get; set; }` | Gets or sets wheel scrolling; arrow buttons remain usable. |
| `public System.Boolean SelectWithRMB { get; set; }` | Gets or sets whether a right-button press also selects its tab. |
| `public System.Boolean SwitchOnDragHover { get; set; }` | Gets or sets delayed selection while a foreign drag hovers a tab. |
| `public Electron2D.TabBar.AlignmentMode TabAlignment { get; set; }` | Gets or sets logical leading, centered or trailing alignment. |
| `public Electron2D.TabBar.CloseButtonDisplayPolicy TabCloseDisplayPolicy { get; set; }` | Gets or sets which visible tabs display close buttons. |
| `public System.Int32 TabCount { get; set; }` | Gets or sets the number of tabs, retaining the common prefix. |
| `public System.Int32 TabsRearrangeGroup { get; set; }` | Gets or sets the transfer group; minus one limits drops to this strip. |

## Property Descriptions

<a id="member-f4d957fc0aa4"></a>
### ClipTabs

`public System.Boolean ClipTabs { get; set; }`

Summary: Gets or sets whether overflow tabs are hidden and scroll arrows are displayed.

Value: true initially.

<a id="member-6e97447d0fa2"></a>
### CloseWithMiddleMouse

`public System.Boolean CloseWithMiddleMouse { get; set; }`

Summary: Gets or sets whether a middle-button press requests closing the hovered tab.

Value: true initially.

<a id="member-3d10959bb423"></a>
### CurrentTab

`public System.Int32 CurrentTab { get; set; }`

Summary: Gets or sets the selected index, including programmatic hidden or disabled selections.

Value: Minus one initially. Minus one is valid when deselection is enabled or every tab is unavailable.

Remarks: A nonnegative out-of-range value before initial count/tree entry is queued. Every valid assignment raises TabSelected; a changed index then raises TabChanged. Nested selection/structural callbacks suppress an obsolete outer change event. Observer failure occurs after state/layout commitment.

System.ArgumentOutOfRangeException: The index is invalid after initialization.

System.InvalidOperationException: Deselection is forbidden with an available tab.

<a id="member-84562b880688"></a>
### DeselectEnabled

`public System.Boolean DeselectEnabled { get; set; }`

Summary: Gets or sets whether an available selection may be cleared.

Value: False initially. Disabling it selects the next available tab if currently deselected.

<a id="member-1539de55528c"></a>
### DragToRearrangeEnabled

`public System.Boolean DragToRearrangeEnabled { get; set; }`

Summary: Gets or sets whether typed tab drags can rearrange or transfer tabs.

Value: false initially.

<a id="member-97175006b7c8"></a>
### MaxTabWidth

`public System.Int32 MaxTabWidth { get; set; }`

Summary: Gets or sets the measured tab width cap, preserving non-text content.

Value: Zero initially, meaning no explicit cap; nonnegative.

System.ArgumentOutOfRangeException: The requested cap is negative.

<a id="member-7544cf16b6fc"></a>
### ScrollToSelected

`public System.Boolean ScrollToSelected { get; set; }`

Summary: Gets or sets automatic scrolling after selection or layout edits.

Value: true initially.

<a id="member-be70576d37e0"></a>
### ScrollingEnabled

`public System.Boolean ScrollingEnabled { get; set; }`

Summary: Gets or sets wheel scrolling; arrow buttons remain usable.

Value: true initially.

<a id="member-c43c6ce0482f"></a>
### SelectWithRMB

`public System.Boolean SelectWithRMB { get; set; }`

Summary: Gets or sets whether a right-button press also selects its tab.

Value: false initially.

<a id="member-08b90d8aa6cd"></a>
### SwitchOnDragHover

`public System.Boolean SwitchOnDragHover { get; set; }`

Summary: Gets or sets delayed selection while a foreign drag hovers a tab.

Value: true initially.

<a id="member-b3fd2980b1b7"></a>
### TabAlignment

`public Electron2D.TabBar.AlignmentMode TabAlignment { get; set; }`

Summary: Gets or sets logical leading, centered or trailing alignment.

Value: Left initially; RTL mirrors the logical leading edge.

System.ArgumentOutOfRangeException: The value is Max or undefined.

<a id="member-e2b47b6695ad"></a>
### TabCloseDisplayPolicy

`public Electron2D.TabBar.CloseButtonDisplayPolicy TabCloseDisplayPolicy { get; set; }`

Summary: Gets or sets which visible tabs display close buttons.

Value: ShowNever initially.

Remarks: Close buttons raise requests; they do not remove tabs automatically.

System.ArgumentOutOfRangeException: The value is Max or undefined.

<a id="member-dc09b0e344d9"></a>
### TabCount

`public System.Int32 TabCount { get; set; }`

Summary: Gets or sets the number of tabs, retaining the common prefix.

Value: Zero initially; zero through 65,536. New slots have empty titles, inherited text direction and no icons.

Remarks: Count edits clamp selection without selection events and notify PropertyListChanged. The first count assignment resolves an earlier queued CurrentTab. Zero resets current/previous/scroll state.

System.ArgumentOutOfRangeException: Count is outside the bounded storage capacity.

<a id="member-2f60bc269664"></a>
### TabsRearrangeGroup

`public System.Int32 TabsRearrangeGroup { get; set; }`

Summary: Gets or sets the transfer group; minus one limits drops to this strip.

Value: -1 initially.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddTab(System.String title = "", Electron2D.Texture icon = null)` | Appends a tab, selecting the first one unless deselection is enabled. |
| `public System.Void ClearTabs()` | Removes every tab, resetting current/previous/scroll state without selection events. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `public System.Void EnsureTabVisible(System.Int32 tabIndex)` | Scrolls a nonhidden tab into view when this strip is attached and clipped. |
| `public System.Boolean GetOffsetButtonsVisible()` | Reports whether overflow navigation arrows are visible. |
| `public System.Int32 GetPreviousTab()` | Gets the selected index immediately before the last CurrentTab assignment. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `public Electron2D.Texture GetTabButtonIcon(System.Int32 tabIndex)` | Gets one tab's ButtonIcon configuration. |
| `public Electron2D.Texture GetTabIcon(System.Int32 tabIndex)` | Gets one tab's Icon configuration. |
| `public System.Int32 GetTabIconMaxWidth(System.Int32 tabIndex)` | Gets one tab's IconMaxWidth configuration. |
| `public System.Int32 GetTabIdxAtPoint(Electron2D.Vector2 point)` | Finds the last displayed visible tab containing a finite local point. |
| `public System.String GetTabLanguage(System.Int32 tabIndex)` | Gets one tab's Language configuration. |
| `public T GetTabMetadata<T>(System.Int32 tabIndex)` | Gets the exact typed runtime metadata stored on a tab. |
| `public System.Int32 GetTabOffset()` | Gets the current first visible scroll index. |
| `public Electron2D.Rect2 GetTabRect(System.Int32 tabIndex)` | Gets a tab's current local display rectangle, mirrored in RTL. |
| `public Electron2D.TextDirection GetTabTextDirection(System.Int32 tabIndex)` | Gets one tab's TextDirection configuration. |
| `public System.String GetTabTitle(System.Int32 tabIndex)` | Gets one tab's Title configuration. |
| `public System.String GetTabTooltip(System.Int32 tabIndex)` | Gets one tab's Tooltip configuration. |
| `public System.Boolean IsTabDisabled(System.Int32 tabIndex)` | Reports whether the tab is disabled. |
| `public System.Boolean IsTabHidden(System.Int32 tabIndex)` | Reports whether the tab is hidden. |
| `public System.Void MoveTab(System.Int32 from, System.Int32 to)` | Moves a tab to another existing index while preserving selected and previous tab identities. |
| `protected override System.Boolean OnCanDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `protected override System.Void OnDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `protected override Electron2D.DragPayload OnGetDragData(Electron2D.Vector2 atPosition)` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `protected override Electron2D.Vector2 OnGetMinimumSize()` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `protected override System.String OnGetTooltip(Electron2D.Vector2 atPosition)` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `protected override System.Void OnNotification(System.Int32 what)` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `public System.Void RemoveTab(System.Int32 tabIndex)` | Removes a tab and searches forward, then backward, for an available replacement selection. |
| `public System.Boolean SelectNextAvailable()` | Selects the next available tab without wrapping. |
| `public System.Boolean SelectPreviousAvailable()` | Selects the previous available tab without wrapping. |
| `public System.Void SetTabButtonIcon(System.Int32 tabIndex, Electron2D.Texture value)` | Changes one tab's ButtonIcon configuration. |
| `public System.Void SetTabDisabled(System.Int32 tabIndex, System.Boolean value)` | Changes a tab's disabled state without changing the current index. |
| `public System.Void SetTabHidden(System.Int32 tabIndex, System.Boolean value)` | Changes a tab's hidden state without changing the current index. |
| `public System.Void SetTabIcon(System.Int32 tabIndex, Electron2D.Texture value)` | Changes one tab's Icon configuration. |
| `public System.Void SetTabIconMaxWidth(System.Int32 tabIndex, System.Int32 value)` | Changes one tab's IconMaxWidth configuration. |
| `public System.Void SetTabLanguage(System.Int32 tabIndex, System.String value)` | Changes one tab's Language configuration. |
| `public System.Void SetTabMetadata<T>(System.Int32 tabIndex, T value)` | Stores runtime-only metadata with an exact compile-time type. |
| `public System.Void SetTabTextDirection(System.Int32 tabIndex, Electron2D.TextDirection value)` | Changes one tab's TextDirection configuration. |
| `public System.Void SetTabTitle(System.Int32 tabIndex, System.String value)` | Changes one tab's Title configuration. |
| `public System.Void SetTabTooltip(System.Int32 tabIndex, System.String value)` | Changes one tab's Tooltip configuration. |
| `protected override System.Void ValidateDisposal()` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |
| `protected override System.Void ValidateMutation()` | Inherited concrete hook; see lifecycle details below and the declaring base contract. |

## Method Descriptions

<a id="member-21124ac6d93d"></a>
### AddTab

`public System.Void AddTab(System.String title = "", Electron2D.Texture icon = null)`

Summary: Appends a tab, selecting the first one unless deselection is enabled.

title: Nonnull source title; empty is valid.

icon: Borrowed optional live texture.

<a id="member-2453bd416270"></a>
### ClearTabs

`public System.Void ClearTabs()`

Summary: Removes every tab, resetting current/previous/scroll state without selection events.

<a id="member-00ece01309cc"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-8f575559603d"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-fc96b2d8e091"></a>
### EnsureTabVisible

`public System.Void EnsureTabVisible(System.Int32 tabIndex)`

Summary: Scrolls a nonhidden tab into view when this strip is attached and clipped.

tabIndex: Minus one is ignored; otherwise a valid index.

Remarks: Does not change selection. Detached calls and already visible tabs do not change the offset.

<a id="member-1c786a7b2ab1"></a>
### GetOffsetButtonsVisible

`public System.Boolean GetOffsetButtonsVisible()`

Summary: Reports whether overflow navigation arrows are visible.

Returns: True for a clipped strip with tabs outside its current page.

<a id="member-738ad1d5eb54"></a>
### GetPreviousTab

`public System.Int32 GetPreviousTab()`

Summary: Gets the selected index immediately before the last CurrentTab assignment.

Returns: Minus one initially; structural operations retain the preceding tab's identity where possible.

<a id="member-f10e4052d941"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-6b1812d02ccd"></a>
### GetTabButtonIcon

`public Electron2D.Texture GetTabButtonIcon(System.Int32 tabIndex)`

Summary: Gets one tab's ButtonIcon configuration.

tabIndex: A valid zero-based index.

Returns: null initially.

<a id="member-90c741098cd9"></a>
### GetTabIcon

`public Electron2D.Texture GetTabIcon(System.Int32 tabIndex)`

Summary: Gets one tab's Icon configuration.

tabIndex: A valid zero-based index.

Returns: null initially.

<a id="member-01099eef8e01"></a>
### GetTabIconMaxWidth

`public System.Int32 GetTabIconMaxWidth(System.Int32 tabIndex)`

Summary: Gets one tab's IconMaxWidth configuration.

tabIndex: A valid zero-based index.

Returns: 0 initially.

<a id="member-30f512dc35df"></a>
### GetTabIdxAtPoint

`public System.Int32 GetTabIdxAtPoint(Electron2D.Vector2 point)`

Summary: Finds the last displayed visible tab containing a finite local point.

point: Local coordinates.

Returns: Minus one outside displayed tabs or inside an arrow area.

System.ArgumentException: The point is nonfinite.

<a id="member-47d2814f4181"></a>
### GetTabLanguage

`public System.String GetTabLanguage(System.Int32 tabIndex)`

Summary: Gets one tab's Language configuration.

tabIndex: A valid zero-based index.

Returns: "" initially.

<a id="member-8ab3dc6a11cb"></a>
### GetTabMetadata

`public T GetTabMetadata<T>(System.Int32 tabIndex)`

Summary: Gets the exact typed runtime metadata stored on a tab.

tabIndex: A valid zero-based index.

T: The exact stored type.

Returns: The stored value, including typed null.

System.Collections.Generic.KeyNotFoundException: The requested metadata type is absent.

<a id="member-d05391c1d9f7"></a>
### GetTabOffset

`public System.Int32 GetTabOffset()`

Summary: Gets the current first visible scroll index.

Returns: Zero initially.

<a id="member-cbd407353cf8"></a>
### GetTabRect

`public Electron2D.Rect2 GetTabRect(System.Int32 tabIndex)`

Summary: Gets a tab's current local display rectangle, mirrored in RTL.

tabIndex: A valid zero-based index.

Returns: Its measured rectangle; hidden or off-page tabs return their measured width at the zero cached offset.

<a id="member-4f7b46e55fd7"></a>
### GetTabTextDirection

`public Electron2D.TextDirection GetTabTextDirection(System.Int32 tabIndex)`

Summary: Gets one tab's TextDirection configuration.

tabIndex: A valid zero-based index.

Returns: TextDirection.Inherited initially.

<a id="member-df0fda08fe27"></a>
### GetTabTitle

`public System.String GetTabTitle(System.Int32 tabIndex)`

Summary: Gets one tab's Title configuration.

tabIndex: A valid zero-based index.

Returns: "" initially.

<a id="member-6f1d25fda5db"></a>
### GetTabTooltip

`public System.String GetTabTooltip(System.Int32 tabIndex)`

Summary: Gets one tab's Tooltip configuration.

tabIndex: A valid zero-based index.

Returns: "" initially.

<a id="member-a989898409b1"></a>
### IsTabDisabled

`public System.Boolean IsTabDisabled(System.Int32 tabIndex)`

Summary: Reports whether the tab is disabled.

tabIndex: A valid zero-based index.

Returns: False initially.

<a id="member-3729ad69ddc8"></a>
### IsTabHidden

`public System.Boolean IsTabHidden(System.Int32 tabIndex)`

Summary: Reports whether the tab is hidden.

tabIndex: A valid zero-based index.

Returns: False initially.

<a id="member-f4cc9338792c"></a>
### MoveTab

`public System.Void MoveTab(System.Int32 from, System.Int32 to)`

Summary: Moves a tab to another existing index while preserving selected and previous tab identities.

from: Source index.

to: Destination index after removal/reinsertion.

Remarks: No selection signal is emitted; PropertyListChanged reports the new indexed schema.

<a id="member-33f6e52f12fb"></a>
### OnCanDropData

`protected override System.Boolean OnCanDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-47206eec56e4"></a>
### OnDropData

`protected override System.Void OnDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-8d66e804a813"></a>
### OnGUIInput

`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-56e55005ecd9"></a>
### OnGetDragData

`protected override Electron2D.DragPayload OnGetDragData(Electron2D.Vector2 atPosition)`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-d463a60a6df4"></a>
### OnGetMinimumSize

`protected override Electron2D.Vector2 OnGetMinimumSize()`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-a406586fef3f"></a>
### OnGetTooltip

`protected override System.String OnGetTooltip(Electron2D.Vector2 atPosition)`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-6914031b083c"></a>
### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-70ef55b2ff48"></a>
### RemoveTab

`public System.Void RemoveTab(System.Int32 tabIndex)`

Summary: Removes a tab and searches forward, then backward, for an available replacement selection.

tabIndex: A valid zero-based index.

Remarks: Attached removal of the selected tab raises TabChanged, without TabSelected.

<a id="member-b160b692892d"></a>
### SelectNextAvailable

`public System.Boolean SelectNextAvailable()`

Summary: Selects the next available tab without wrapping.

Returns: True when an enabled visible candidate exists.

<a id="member-c410b7f1c6a7"></a>
### SelectPreviousAvailable

`public System.Boolean SelectPreviousAvailable()`

Summary: Selects the previous available tab without wrapping.

Returns: True when an enabled visible candidate exists.

<a id="member-35f3dcf617a3"></a>
### SetTabButtonIcon

`public System.Void SetTabButtonIcon(System.Int32 tabIndex, Electron2D.Texture value)`

Summary: Changes one tab's ButtonIcon configuration.

tabIndex: A valid zero-based index.

value: Borrowed optional live auxiliary button texture.

<a id="member-57d54de44ef3"></a>
### SetTabDisabled

`public System.Void SetTabDisabled(System.Int32 tabIndex, System.Boolean value)`

Summary: Changes a tab's disabled state without changing the current index.

tabIndex: A valid zero-based index.

value: The new state.

<a id="member-813372a4922d"></a>
### SetTabHidden

`public System.Void SetTabHidden(System.Int32 tabIndex, System.Boolean value)`

Summary: Changes a tab's hidden state without changing the current index.

tabIndex: A valid zero-based index.

value: The new state.

<a id="member-540513f8306c"></a>
### SetTabIcon

`public System.Void SetTabIcon(System.Int32 tabIndex, Electron2D.Texture value)`

Summary: Changes one tab's Icon configuration.

tabIndex: A valid zero-based index.

value: Borrowed optional live title icon.

<a id="member-87b44f90fa99"></a>
### SetTabIconMaxWidth

`public System.Void SetTabIconMaxWidth(System.Int32 tabIndex, System.Int32 value)`

Summary: Changes one tab's IconMaxWidth configuration.

tabIndex: A valid zero-based index.

value: Nonnegative per-tab icon cap; zero uses the theme cap.

<a id="member-390fa2055602"></a>
### SetTabLanguage

`public System.Void SetTabLanguage(System.Int32 tabIndex, System.String value)`

Summary: Changes one tab's Language configuration.

tabIndex: A valid zero-based index.

value: Nonnull shaping language; empty uses the current locale.

<a id="member-b51482b70990"></a>
### SetTabMetadata

`public System.Void SetTabMetadata<T>(System.Int32 tabIndex, T value)`

Summary: Stores runtime-only metadata with an exact compile-time type.

tabIndex: A valid zero-based index.

value: Borrowed value; never copied or disposed.

T: The exact value type reused when reading.

<a id="member-c18c84a9bda9"></a>
### SetTabTextDirection

`public System.Void SetTabTextDirection(System.Int32 tabIndex, Electron2D.TextDirection value)`

Summary: Changes one tab's TextDirection configuration.

tabIndex: A valid zero-based index.

value: A defined text direction; Inherited follows layout direction.

<a id="member-f15992c0530e"></a>
### SetTabTitle

`public System.Void SetTabTitle(System.Int32 tabIndex, System.String value)`

Summary: Changes one tab's Title configuration.

tabIndex: A valid zero-based index.

value: The nonnull source title.

<a id="member-aa2c1ae74373"></a>
### SetTabTooltip

`public System.Void SetTabTooltip(System.Int32 tabIndex, System.String value)`

Summary: Changes one tab's Tooltip configuration.

tabIndex: A valid zero-based index.

value: The nonnull tooltip; empty uses the translated title when truncated.

<a id="member-c3ecf04a9160"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

<a id="member-38193b3f5bf6"></a>
### ValidateMutation

`protected override System.Void ValidateMutation()`

Concrete inherited hook: retains the shared scene/input/property/disposal contract. The description above and component detail its tab-specific flow. See the base Control/Node documentation for parameters and hook timing.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<System.Int32> ActiveTabRearranged` | Occurs after a same-strip drop reorders an available tab, before its selection. |
| `public event System.Action<System.Int32> TabButtonPressed` | Occurs when the auxiliary icon receives a completed left-button click. |
| `public event System.Action<System.Int32> TabChanged` | Occurs after the selected index changes; removal of the selected attached tab also reports its replacement. |
| `public event System.Action<System.Int32> TabClicked` | Occurs after a left or enabled right-button tab selection. |
| `public event System.Action<System.Int32> TabClosePressed` | Requests closing a tab after its close button or a middle-button press. |
| `public event System.Action<System.Int32> TabHovered` | Occurs when pointer motion enters a different visible tab; exit emits no index. |
| `public event System.Action<System.Int32> TabRMBClicked` | Occurs when a right-button press hits a tab, independently of right-button selection. |
| `public event System.Action<System.Int32> TabSelected` | Occurs for every valid CurrentTab assignment, including equal assignments. |

## Event Descriptions

<a id="member-c5f3259aaae1"></a>
### ActiveTabRearranged

`public event System.Action<System.Int32> ActiveTabRearranged`

Summary: Occurs after a same-strip drop reorders an available tab, before its selection.

<a id="member-1614f789c139"></a>
### TabButtonPressed

`public event System.Action<System.Int32> TabButtonPressed`

Summary: Occurs when the auxiliary icon receives a completed left-button click.

<a id="member-8abf0cbd59f0"></a>
### TabChanged

`public event System.Action<System.Int32> TabChanged`

Summary: Occurs after the selected index changes; removal of the selected attached tab also reports its replacement.

<a id="member-f8ebb17dd63e"></a>
### TabClicked

`public event System.Action<System.Int32> TabClicked`

Summary: Occurs after a left or enabled right-button tab selection.

<a id="member-11cd81381c94"></a>
### TabClosePressed

`public event System.Action<System.Int32> TabClosePressed`

Summary: Requests closing a tab after its close button or a middle-button press.

<a id="member-7c22eafa50b9"></a>
### TabHovered

`public event System.Action<System.Int32> TabHovered`

Summary: Occurs when pointer motion enters a different visible tab; exit emits no index.

<a id="member-f11a6f42fed3"></a>
### TabRMBClicked

`public event System.Action<System.Int32> TabRMBClicked`

Summary: Occurs when a right-button press hits a tab, independently of right-button selection.

<a id="member-f50b5a58c262"></a>
### TabSelected

`public event System.Action<System.Int32> TabSelected`

Summary: Occurs for every valid CurrentTab assignment, including equal assignments.

## Lifecycle, errors and verification

The model owns containers and the internal timer; native fonts/glyph textures and icon resources remain borrowed. Theme changes, locale/layout direction, size and icon edits invalidate geometry. Source controls use coherent cached fields in current draw recording. Structural edits and first preparation allocate. Invalid arguments, disposed references, foreign owners, capture phases and shaping reentrancy reject with typed exceptions; callback failures can follow committed state and are reported without hiding cleanup. Disposal clears borrowed references/events and ordinary Node teardown disposes the timer.

TabBarTests exercises source defaults, equal/nested/failed selection, unavailable tabs, current/previous moves, typed metadata, shaping language/direction, capped geometry/tooltips, icon edits/disposal, RTL hit tests, page reveal, pointer/RMB/middle/close ordering and canceled wrong releases, focused actions, group transfer/stale payloads, hover-delay/disabled/disable behavior, packed and fresh-process file state, owner guards and timer cleanup. TabBarRenderingTests verifies native style/text/icon/close pixels, mirrored overflow/reveal and clipping on Linux Wayland GPU/compatibility. The saved PNGs were inspected visually; 64 warmed active selection/layout/record/render frames measure zero managed bytes on each renderer. Native driver allocations, real controller hardware, other platforms and owner visual acceptance remain unverified.

Native accessibility roles/actions and editor tab authoring still require those services. TabContainer is a separate child-panel consumer and remains tracked independently. See ADRs [0008](../decisions/scene.md#adr-0008), [0038](../decisions/input.md#adr-0038), [0046](../decisions/rendering.md#adr-0046) and [0083](../decisions/rendering.md#adr-0083).

[TabContainer](TabContainer.md) now owns a stable borrowed internal strip for actual Control pages. Page count and order belong to its scene children. Internal record sharing retains exact metadata and configuration during page transfer; the public TabBar API is unchanged.
