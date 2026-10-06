# Tab panels

Last updated: 2026-10-06

Owns [TabContainer](../classes/TabContainer.md) and its [TabPosition](../classes/TabContainer.TabPosition.md), composing existing TabBar, Control/Container, Button, theme and Popup contracts under ADRs 0004, 0008, 0038, 0081 and 0083.

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
