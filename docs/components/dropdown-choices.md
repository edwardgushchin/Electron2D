# Dropdown choices

Last updated: 2026-10-06

Owns [OptionButton](../classes/OptionButton.md), composing Button, PopupMenu, text/theme and typed scene contracts under ADRs 0004, 0008, 0038, 0046 and 0083.

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
