# TabContainer

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public partial class TabContainer : Container`. **Inherits:** [Container](Container.md). **Inherited By:** —. **Source:** [model](../../src/Scene/GUI/TabContainer.cs), [layout](../../src/Scene/GUI/TabContainer.Layout.cs), [input](../../src/Scene/GUI/TabContainer.Input.cs), [storage](../../src/Scene/GUI/TabContainer.Storage.cs). **Component:** [Tab panels](../components/tab-panels.md).

Hosts actual child control pages beneath a themed tab header.

## Description

TabContainer owns page selection and layout for ordinary direct non-top-level Control children. Neutral Node, top-level and internal children do not define tabs. Its stable internal TabBar and popup Button are implementation children omitted by ordinary enumeration and scene packing. GetTabBar returns a borrowed strip; use container/page APIs for count and ordering. Page indices follow scene-child order, including after MoveChild, removal or disposal. Icons and exact generic metadata stay borrowed; metadata remains runtime-only. The two position values retain the container's nested TabPosition domain, while alignment reuses TabBar.AlignmentMode and focus uses shared FocusMode.

The first page becomes current unless deselection is enabled. Detached CurrentTab assignments queue for tree entry; the getter continues reporting the strip's actual selection. Attached assignments use TabBar's current/previous, equal-selection and nested-callback rules. Selected/Changed follow committed page visibility; equal assignments report Selected only. Keyboard/controller/pointer/touch and overflow/reveal remain the exercised TabBar consumers. Only the selected page is locally visible. Manually showing a page selects it; hiding the current one deselects when enabled, restores the sole page, or selects another available page. Disabled pages remain programmatically selectable. Hidden tabs retain their scene child and trigger the same page visibility/replacement policy. Changes while a container is hidden are applied before child visibility propagation.

Default titles follow child names. An explicit title is retained across renames until set equal to the current child name, which restores automatic naming. Renames while detached refresh at tree entry. Title, icon, disabled and hidden assignments may be queued for nonnegative indices before Ready, supporting scene restore before child construction; other per-tab getters/setters require an existing index. Count and pending state are bounded to 65,536. GetTabControl returns null for negative or unavailable indices; GetTabIdxFromControl returns -1 for non-page controls. Hit coordinates for GetTabIdxAtPoint are strip-local. All attached access requires the tree owner thread; mutation/capture/disposal guards remain inherited.

TabContainer forwards all declared strip style/icon/color/font/constant theme keys to the internal TabBar, translating icon_separation to h_separation. Panel and tabbar_background remain container roles. The header respects logical alignment, side margins, overflow arrows, popup width and RTL; TabsPosition places it above or below content. TabsVisible hides the header and leaves the panel. The selected page fits through Container's fill/shrink flags and reset of rotation/scale, inside panel margins and header height. Propagated maximum bounds subtract those allocations. Locally hidden pages contribute to minimum size only when UseHiddenTabsForMinSize is true; desired-size forwarding uses the existing Control pipeline. AllTabsInFront preserves the obsolete source contract: always false, with validated assignments having no effect, because the header always draws in front.

SetPopup borrows a weak live Popup identity and clears for null or a non-popup Node. The caller parents/configures the popup before opening; an embedded Window host requires GUIEmbedSubwindows. The header button reports PrePopupPressed, then places the popup beside/below or above the button according to header position and RTL. Disposal clears the binding and affordance, marshaling worker disposal to the scene owner. Binding itself is runtime configuration. Popup sizing/input/native limitations remain in the popup component.

Typed page drags reuse the strip preview, insertion geometry, marker, availability, group and same-tree rules. A same-container drop moves the actual Control child and reports ActiveTabRearranged before selecting it. Cross-container transfer reparents the child and moves the complete existing tab record, including exact generic metadata, tooltip, icon/button icon, width, language/direction and hidden/disabled flags. Payloads follow child and record identities after ordering edits; stale, disposed, removed or wrong-group sources reject. Source/target scene callbacks may run during parenting. Required transitions continue after observer errors and report aggregated failures; ownership altered by a callback is respected. Shared typed records are internal and introduce no public transfer capability. Reentrant synchronization and page layout settle through reused buffers with a 64-pass limit, avoiding uncontrolled recursion and obsolete outer selection events.

## Example

```csharp
var root = new Window { Size = new(640, 360), GUIEmbedSubwindows = true };
var tabs = new TabContainer { Name = "Panels", Size = new(600, 320) };
tabs.AddChild(new Label { Name = "Overview", Text = "Overview content" });
tabs.AddChild(new Label { Name = "Settings", Text = "Settings content" });
root.AddChild(tabs);
var menu = new PopupMenu { Name = "PanelMenu" };
menu.AddItem("Refresh");
root.AddChild(menu);
tabs.SetPopup(menu);
Engine.Run(root);
```

## Persistence and verification limits

PackedScene captures scalar tab policies, current selection and four indexed fields: title, icon, disabled and hidden. A private typed _tab_schema_count descriptor enables pending indexed fields on fresh instances before owned child Controls are created; it never creates dummy pages or adds a public count setter. Default titles have the child name as revert value. Choose the container/scene root as Owner for pages to capture. Exact built-in in-memory and file factories recreate separate internal owners. Metadata, tooltip, button icon, icon width, language/direction, weak popup binding and transient drag/layout state remain runtime configuration. Fresh ResourceSaver/ResourceLoader processes execute the selected reconstructed page.

TabContainerTests exercises ordinary/top-level/neutral eligibility, pending selection/indexed fields, automatic/custom titles, show/hide/deselect/disabled/hidden transitions, ordering and disposal, exact typed null metadata, bottom/header-hidden layout, minimum policy, routed popup clicks/disposal, owner-thread rejection, identity-stable page drags, callback failure and reentrant child additions. Native tests use SDL keyboard input and actual root Engine.Run targets on current Linux Wayland GPU and compatibility backends, verify exclusive selected-page pixels, header/popup affordance and bottom/RTL geometry, and save visually inspected PNGs. Each backend measures zero managed bytes across 64 warmed selection/layout/visibility/render frames after 32 warmup frames. Cold theme/model/structural work and readback allocate. Physical hardware input, native allocations, large-page performance, other targets and owner acceptance remain unverified.

The complete own API executes on the current backends. Native accessibility remains inherited Control/Container service work; editor authoring and broader platform acceptance remain separate. Popup bindings inherit independent native-child and embedding-policy limitations. No architectural decision or vendor dependency change was required.

## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public TabContainer()` | Creates an empty page owner with internal tab strip and popup button. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public System.Boolean AllTabsInFront { get; set; }` | Gets the obsolete front-order flag; assigning it has no effect because tabs always draw in front. |
| `public System.Boolean ClipTabs { get; set; }` | Gets or sets the internal strip ClipTabs policy. |
| `public System.Int32 CurrentTab { get; set; }` | Gets or sets the selected page index; detached assignments are applied on tree entry. |
| `public System.Boolean DeselectEnabled { get; set; }` | Gets or sets the internal strip DeselectEnabled policy. |
| `public System.Boolean DragToRearrangeEnabled { get; set; }` | Gets or sets the internal strip DragToRearrangeEnabled policy. |
| `public System.Boolean SwitchOnDragHover { get; set; }` | Gets or sets the internal strip SwitchOnDragHover policy. |
| `public Electron2D.TabBar.AlignmentMode TabAlignment { get; set; }` | Gets or sets the internal strip TabAlignment policy. |
| `public Electron2D.FocusMode TabFocusMode { get; set; }` | Gets or sets the internal strip TabFocusMode policy. |
| `public Electron2D.TabContainer.TabPosition TabsPosition { get; set; }` | Gets or sets the header position. |
| `public System.Int32 TabsRearrangeGroup { get; set; }` | Gets or sets the internal strip TabsRearrangeGroup policy. |
| `public System.Boolean TabsVisible { get; set; }` | Gets or sets whether the header is shown. |
| `public System.Boolean UseHiddenTabsForMinSize { get; set; }` | Gets or sets whether locally hidden pages contribute to the minimum size. |

## Methods and protected hooks

| Complete declaration | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Projects the inherited factory/storage/layout/disposal contract onto tab pages. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Projects the inherited factory/storage/layout/disposal contract onto tab pages. |
| `protected override Electron2D.Control.SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Projects the inherited factory/storage/layout/disposal contract onto tab pages. |
| `protected override Electron2D.Control.SizeFlags[] GetAllowedSizeFlagsVertical()` | Projects the inherited factory/storage/layout/disposal contract onto tab pages. |
| `public Electron2D.Control GetCurrentTabControl()` | Gets the selected borrowed page. |
| `public Electron2D.Popup GetPopup()` | Gets the live borrowed popup binding. |
| `public System.Int32 GetPreviousTab()` | Gets the selection preceding the latest assignment. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects the inherited factory/storage/layout/disposal contract onto tab pages. |
| `public Electron2D.TabBar GetTabBar()` | Gets the borrowed internal tab strip. Page count and ordering are owned by this container. |
| `public Electron2D.Texture GetTabButtonIcon(System.Int32 tabIndex)` | Gets a tab's ButtonIcon setting. |
| `public Electron2D.Control GetTabControl(System.Int32 tabIndex)` | Gets a borrowed page by index. |
| `public System.Int32 GetTabCount()` | Gets the number of ordinary non-top-level child pages. |
| `public Electron2D.Texture GetTabIcon(System.Int32 tabIndex)` | Gets a tab's Icon setting. |
| `public System.Int32 GetTabIconMaxWidth(System.Int32 tabIndex)` | Gets a tab's IconMaxWidth setting. |
| `public System.Int32 GetTabIdxAtPoint(Electron2D.Vector2 point)` | Finds a tab from a point in the strip's local coordinates. |
| `public System.Int32 GetTabIdxFromControl(Electron2D.Control control)` | Finds the index of a direct page. |
| `public T GetTabMetadata<T>(System.Int32 tabIndex)` | Gets metadata stored with the same exact generic type. |
| `public System.String GetTabTitle(System.Int32 tabIndex)` | Gets a tab's Title setting. |
| `public System.String GetTabTooltip(System.Int32 tabIndex)` | Gets a tab's Tooltip setting. |
| `public System.Boolean IsTabDisabled(System.Int32 tabIndex)` | Reports a tab's Disabled flag. |
| `public System.Boolean IsTabHidden(System.Int32 tabIndex)` | Reports a tab's Hidden flag. |
| `protected override Electron2D.Vector2 OnGetMinimumSize()` | Projects the inherited factory/storage/layout/disposal contract onto tab pages. |
| `protected override System.Void OnNotification(System.Int32 what)` | Projects the inherited factory/storage/layout/disposal contract onto tab pages. |
| `public System.Boolean SelectNextAvailable()` | Selects the next enabled visible tab without wrapping. |
| `public System.Boolean SelectPreviousAvailable()` | Selects the preceding enabled visible tab without wrapping. |
| `public System.Void SetPopup(Electron2D.Node popup)` | Binds a borrowed Popup, clearing the binding for null or a non-popup node. |
| `public System.Void SetTabButtonIcon(System.Int32 tabIndex, Electron2D.Texture value)` | Sets a tab's ButtonIcon setting. |
| `public System.Void SetTabDisabled(System.Int32 tabIndex, System.Boolean disabled)` | Sets a disabled flag; programmatic selection remains permitted. |
| `public System.Void SetTabHidden(System.Int32 tabIndex, System.Boolean hidden)` | Sets header visibility, preserving the page record and applying selection/visibility policy. |
| `public System.Void SetTabIcon(System.Int32 tabIndex, Electron2D.Texture icon)` | Sets a borrowed icon, including pending indexed scene configuration. |
| `public System.Void SetTabIconMaxWidth(System.Int32 tabIndex, System.Int32 value)` | Sets a tab's IconMaxWidth setting. |
| `public System.Void SetTabMetadata<T>(System.Int32 tabIndex, T value)` | Sets exact generic runtime metadata. |
| `public System.Void SetTabTitle(System.Int32 tabIndex, System.String title)` | Sets a source title, retaining an override until it equals the child name. |
| `public System.Void SetTabTooltip(System.Int32 tabIndex, System.String value)` | Sets a tab's Tooltip setting. |

## Events

| Complete declaration | Contract |
| --- | --- |
| `public event System.Action<System.Int32> ActiveTabRearranged` | Occurs when a same-container drop rearranges its selected page. |
| `public event System.Action PrePopupPressed` | Occurs immediately before the configured popup opens. |
| `public event System.Action<System.Int32> TabButtonPressed` | Occurs when a tab auxiliary button is activated. |
| `public event System.Action<System.Int32> TabChanged` | Occurs when selection changes after page visibility commits. |
| `public event System.Action<System.Int32> TabClicked` | Occurs when a tab is clicked. |
| `public event System.Action<System.Int32> TabHovered` | Occurs when pointer enters a tab. |
| `public event System.Action<System.Int32> TabSelected` | Occurs when a valid assignment selects a tab, including an equal assignment. |

## Enumerations

[TabPosition](TabContainer.TabPosition.md) defines Top, Bottom and the nonselectable Max sentinel.

## Constructor Descriptions

### .ctor

```csharp
public TabContainer()
```

Creates an empty page owner with internal tab strip and popup button.


## Property Descriptions

### AllTabsInFront

```csharp
public System.Boolean AllTabsInFront { get; set; }
```

Gets the obsolete front-order flag; assigning it has no effect because tabs always draw in front.

Always false.


### ClipTabs

```csharp
public System.Boolean ClipTabs { get; set; }
```

Gets or sets the internal strip ClipTabs policy.

true initially.


### CurrentTab

```csharp
public System.Int32 CurrentTab { get; set; }
```

Gets or sets the selected page index; detached assignments are applied on tree entry.

-1 with no pages; first page otherwise. -1 requires deselection or no available tab.


### DeselectEnabled

```csharp
public System.Boolean DeselectEnabled { get; set; }
```

Gets or sets the internal strip DeselectEnabled policy.

false initially.


### DragToRearrangeEnabled

```csharp
public System.Boolean DragToRearrangeEnabled { get; set; }
```

Gets or sets the internal strip DragToRearrangeEnabled policy.

false initially.


### SwitchOnDragHover

```csharp
public System.Boolean SwitchOnDragHover { get; set; }
```

Gets or sets the internal strip SwitchOnDragHover policy.

true initially.


### TabAlignment

```csharp
public Electron2D.TabBar.AlignmentMode TabAlignment { get; set; }
```

Gets or sets the internal strip TabAlignment policy.

Left initially.


### TabFocusMode

```csharp
public Electron2D.FocusMode TabFocusMode { get; set; }
```

Gets or sets the internal strip TabFocusMode policy.

All initially.


### TabsPosition

```csharp
public Electron2D.TabContainer.TabPosition TabsPosition { get; set; }
```

Gets or sets the header position.

Top initially; Max is invalid.


### TabsRearrangeGroup

```csharp
public System.Int32 TabsRearrangeGroup { get; set; }
```

Gets or sets the internal strip TabsRearrangeGroup policy.

-1 initially.


### TabsVisible

```csharp
public System.Boolean TabsVisible { get; set; }
```

Gets or sets whether the header is shown.

True initially; hidden headers leave the page panel.


### UseHiddenTabsForMinSize

```csharp
public System.Boolean UseHiddenTabsForMinSize { get; set; }
```

Gets or sets whether locally hidden pages contribute to the minimum size.

False initially.


## Method Descriptions

### CreateSceneInstanceFactory

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

Projects inherited tab-page layout, factory/storage, size flags or owned-state cleanup; see the class lifecycle contract above.

### Dispose

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

Projects inherited tab-page layout, factory/storage, size flags or owned-state cleanup; see the class lifecycle contract above.

### GetAllowedSizeFlagsHorizontal

```csharp
protected override Electron2D.Control.SizeFlags[] GetAllowedSizeFlagsHorizontal()
```

Projects inherited tab-page layout, factory/storage, size flags or owned-state cleanup; see the class lifecycle contract above.

### GetAllowedSizeFlagsVertical

```csharp
protected override Electron2D.Control.SizeFlags[] GetAllowedSizeFlagsVertical()
```

Projects inherited tab-page layout, factory/storage, size flags or owned-state cleanup; see the class lifecycle contract above.

### GetCurrentTabControl

```csharp
public Electron2D.Control GetCurrentTabControl()
```

Gets the selected borrowed page.

The selected control or null.


### GetPopup

```csharp
public Electron2D.Popup GetPopup()
```

Gets the live borrowed popup binding.

The popup, or null after disposal/collection.


### GetPreviousTab

```csharp
public System.Int32 GetPreviousTab()
```

Gets the selection preceding the latest assignment.

The previous index or -1.


### GetPropertyDescriptors

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects inherited tab-page layout, factory/storage, size flags or owned-state cleanup; see the class lifecycle contract above.

### GetTabBar

```csharp
public Electron2D.TabBar GetTabBar()
```

Gets the borrowed internal tab strip. Page count and ordering are owned by this container.

The stable internal strip.


### GetTabButtonIcon

```csharp
public Electron2D.Texture GetTabButtonIcon(System.Int32 tabIndex)
```

Gets a tab's ButtonIcon setting.

The stored value; resources are borrowed.


- `tabIndex`: A valid nonnegative index.

### GetTabControl

```csharp
public Electron2D.Control GetTabControl(System.Int32 tabIndex)
```

Gets a borrowed page by index.

The direct child or null.


- `tabIndex`: An index; negative or unavailable indices return null.

### GetTabCount

```csharp
public System.Int32 GetTabCount()
```

Gets the number of ordinary non-top-level child pages.

The page count.


### GetTabIcon

```csharp
public Electron2D.Texture GetTabIcon(System.Int32 tabIndex)
```

Gets a tab's Icon setting.

The stored value; resources are borrowed.


- `tabIndex`: A valid nonnegative index.

### GetTabIconMaxWidth

```csharp
public System.Int32 GetTabIconMaxWidth(System.Int32 tabIndex)
```

Gets a tab's IconMaxWidth setting.

The stored value; resources are borrowed.


- `tabIndex`: A valid nonnegative index.

### GetTabIdxAtPoint

```csharp
public System.Int32 GetTabIdxAtPoint(Electron2D.Vector2 point)
```

Finds a tab from a point in the strip's local coordinates.

The visible tab index or -1.


- `point`: Finite strip-local coordinates.

### GetTabIdxFromControl

```csharp
public System.Int32 GetTabIdxFromControl(Electron2D.Control control)
```

Finds the index of a direct page.

The page index or -1.


- `control`: A live borrowed control.

### GetTabMetadata

```csharp
public T GetTabMetadata<T>(System.Int32 tabIndex)
```

Gets metadata stored with the same exact generic type.

The borrowed value.


- `tabIndex`: A valid index.
- `T`: The stored value type.

Throws `System.Collections.Generic.KeyNotFoundException`: No metadata of the exact type is stored.

### GetTabTitle

```csharp
public System.String GetTabTitle(System.Int32 tabIndex)
```

Gets a tab's Title setting.

The stored value; resources are borrowed.


- `tabIndex`: A valid nonnegative index.

### GetTabTooltip

```csharp
public System.String GetTabTooltip(System.Int32 tabIndex)
```

Gets a tab's Tooltip setting.

The stored value; resources are borrowed.


- `tabIndex`: A valid nonnegative index.

### IsTabDisabled

```csharp
public System.Boolean IsTabDisabled(System.Int32 tabIndex)
```

Reports a tab's Disabled flag.

The stored flag.


- `tabIndex`: A valid index.

### IsTabHidden

```csharp
public System.Boolean IsTabHidden(System.Int32 tabIndex)
```

Reports a tab's Hidden flag.

The stored flag.


- `tabIndex`: A valid index.

### OnGetMinimumSize

```csharp
protected override Electron2D.Vector2 OnGetMinimumSize()
```

Projects inherited tab-page layout, factory/storage, size flags or owned-state cleanup; see the class lifecycle contract above.

### OnNotification

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

Projects inherited tab-page layout, factory/storage, size flags or owned-state cleanup; see the class lifecycle contract above.

### SelectNextAvailable

```csharp
public System.Boolean SelectNextAvailable()
```

Selects the next enabled visible tab without wrapping.

Whether selection succeeded.


### SelectPreviousAvailable

```csharp
public System.Boolean SelectPreviousAvailable()
```

Selects the preceding enabled visible tab without wrapping.

Whether selection succeeded.


### SetPopup

```csharp
public System.Void SetPopup(Electron2D.Node popup)
```

Binds a borrowed Popup, clearing the binding for null or a non-popup node.


- `popup`: A live node; a popup must be parented by the consumer before opening.

### SetTabButtonIcon

```csharp
public System.Void SetTabButtonIcon(System.Int32 tabIndex, Electron2D.Texture value)
```

Sets a tab's ButtonIcon setting.


- `tabIndex`: A valid nonnegative index.
- `value`: The new value; resources remain borrowed.

### SetTabDisabled

```csharp
public System.Void SetTabDisabled(System.Int32 tabIndex, System.Boolean disabled)
```

Sets a disabled flag; programmatic selection remains permitted.


- `tabIndex`: A nonnegative index.
- `disabled`: Whether user selection is disabled.

### SetTabHidden

```csharp
public System.Void SetTabHidden(System.Int32 tabIndex, System.Boolean hidden)
```

Sets header visibility, preserving the page record and applying selection/visibility policy.


- `tabIndex`: A nonnegative index.
- `hidden`: Whether the tab is hidden.

### SetTabIcon

```csharp
public System.Void SetTabIcon(System.Int32 tabIndex, Electron2D.Texture icon)
```

Sets a borrowed icon, including pending indexed scene configuration.


- `tabIndex`: A nonnegative index.
- `icon`: A live borrowed texture or null.

### SetTabIconMaxWidth

```csharp
public System.Void SetTabIconMaxWidth(System.Int32 tabIndex, System.Int32 value)
```

Sets a tab's IconMaxWidth setting.


- `tabIndex`: A valid nonnegative index.
- `value`: The new value; resources remain borrowed.

### SetTabMetadata

```csharp
public System.Void SetTabMetadata<T>(System.Int32 tabIndex, T value)
```

Sets exact generic runtime metadata.


- `tabIndex`: A valid index.
- `value`: The borrowed value, including typed null.
- `T`: The stored value type.

### SetTabTitle

```csharp
public System.Void SetTabTitle(System.Int32 tabIndex, System.String title)
```

Sets a source title, retaining an override until it equals the child name.


- `tabIndex`: A nonnegative index; may be pending before Ready.
- `title`: Nonnull title.

### SetTabTooltip

```csharp
public System.Void SetTabTooltip(System.Int32 tabIndex, System.String value)
```

Sets a tab's Tooltip setting.


- `tabIndex`: A valid nonnegative index.
- `value`: The new value; resources remain borrowed.

## Event Descriptions

### ActiveTabRearranged

```csharp
public event System.Action<System.Int32> ActiveTabRearranged
```

Occurs when a same-container drop rearranges its selected page.


### PrePopupPressed

```csharp
public event System.Action PrePopupPressed
```

Occurs immediately before the configured popup opens.


### TabButtonPressed

```csharp
public event System.Action<System.Int32> TabButtonPressed
```

Occurs when a tab auxiliary button is activated.


### TabChanged

```csharp
public event System.Action<System.Int32> TabChanged
```

Occurs when selection changes after page visibility commits.


### TabClicked

```csharp
public event System.Action<System.Int32> TabClicked
```

Occurs when a tab is clicked.


### TabHovered

```csharp
public event System.Action<System.Int32> TabHovered
```

Occurs when pointer enters a tab.


### TabSelected

```csharp
public event System.Action<System.Int32> TabSelected
```

Occurs when a valid assignment selects a tab, including an equal assignment.
