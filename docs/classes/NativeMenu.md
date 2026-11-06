# NativeMenu

Last updated: 2026-11-06

**Namespace:** `Electron2D`. **Declaration:** `public sealed partial class NativeMenu : ElectronObject`. **Source:** [owner](../../src/Servers/Menus/NativeMenu.cs), [API/XML](../../src/Servers/Menus/NativeMenu.API.cs), [Windows](../../src/Servers/Menus/NativeMenu.Windows.cs). **Component:** [Native popup menus](../components/native-menus.md). **Decision:** [ADR 0117](../decisions/native-menus.md#adr-0117).

Display-owned OS popup menus with static public operations. No public constructor or Instance accessor exists. All operations require the live display owner except observational IsAvailable. RIDs, callbacks and icon snapshots retire with that exact session.

| Public family | Behavior |
| --- | --- |
| IsAvailable; HasFeature(Feature) | Session observation and actual GlobalMenu/PopupMenu/OpenCloseCallback/HoverCallback/KeyCallback capability. Windows supports the three popup capabilities; global/key remain false. |
| CreateMenu, HasMenu, FreeMenu, Clear | Independent opaque owners, deterministic detach/free, no RID reuse. |
| AddItem, AddCheckItem, AddRadioCheckItem | Ordinary or exact typed tag/Action<T> activation; returns logical insertion index. |
| AddIconItem, AddIconCheckItem, AddIconRadioCheckItem | Same activation families with borrowed readable Texture. |
| AddMultistateItem | Explicit application State/MaxStates metadata. |
| AddSeparator, AddSubmenuItem | Native separator or one-parent acyclic child. Generic submenu overload stores typed metadata. |
| GetItemCount; GetItemText/SetItemText; SetItemIndex/RemoveItem | Stable logical records including hidden items, native mutations and movement. |
| IsItemCheckable/SetItemCheckable; IsItemRadioCheckable/SetItemRadioCheckable | Explicit ordinary/checkbox/radio decoration role. |
| IsItemChecked/SetItemChecked; IsItemDisabled/SetItemDisabled; IsItemHidden/SetItemHidden | Actual native state with retained logical indices. |
| GetItemState/SetItemState; GetItemMaxStates/SetItemMaxStates | Unmodified signed application metadata; no implicit cycling. |
| GetItemSubmenu/SetItemSubmenu; GetItemIcon/SetItemIcon | Borrow child identity/icon source; detach before freeing children, refresh copied icons before presentation. |
| GetItemCallback/SetItemCallback; SetItemHoverCallbacks | Action or exact typed Action<T>; original delegate getters and mouse-hover delivery. |
| GetItemTag<T>/SetItemTag<T>; FindItemIndexWithTag<T> | Exact generic values, including typed null, and type-safe replacement. |
| FindItemIndexWithText, FindItemIndexWithSubmenu | First exact logical index or -1. |
| GetPopupOpenCallback/SetPopupOpenCallback; GetPopupCloseCallback/SetPopupCloseCallback | Native initialization and post-close delivery; open may populate, close may mutate/reopen. |
| Popup, IsOpened, GetSize, SetInterfaceDirection | Modal native UI, actual open state, last measured native extent and OS RTL. |

Insertion uses -1 for append and clamps other insertion indices; item access requires a nonnegative in-range index. Text rejects embedded NUL. Generic insertion requires an explicit callback argument to avoid lambda/tag overload ambiguity. For integer submenu tags call AddSubmenuItem<int> explicitly. Generic callbacks require the exact stored tag type. Borrowed values are never disposed by the service.

Popup closes its native reservation before required close callbacks and activation. Item/owner retirement during close suppresses stale selection. Errors aggregate after required cleanup; managed exceptions never cross the native callback ABI. Empty menus have no native window. System-menu IDs, global registration, keyboard callbacks/accelerators and native tooltip/indent/minimum-width operations are not exposed as inert fields. The PopupMenu bridge forwards keyboard focus through IDFocused and cancels the OS tree on Hide/Visible=false; temporary cleanup retains its exact service owner. See the component for fallback rules, executed checks and remaining target/platform boundaries.

Root menu identities can be borrowed by the [DisplayServer status indicators](../components/status-indicators.md). Multiple bindings are allowed; a bound root cannot become a submenu, and FreeMenu clears all bindings before retirement.
