# Native menu decisions

Last updated: 2026-11-06

<a id="adr-0117"></a>
## ADR 0117: Typed native popup menus owned by the display session

### Status

Accepted under the native-menu prerequisite in ADR 0041 and the complete applicable API objective.

### Decision

NativeMenu is a sealed ElectronObject service with static public operations over a retained display-owned object (ADR 0095). A live display owns its opaque menu RIDs, native handles and callbacks. Only its owner thread may query or mutate menus. Explicit caller FreeMenu retires an unattached menu; display retirement frees all remaining menus before its native window. RIDs are never reused and a later display rejects old identities. Raw HMENU/HBITMAP/HWND values remain private.

Implement the Windows desktop popup producer with system User32/GDI/Comctl32 and the existing SDL window. The direct service uses the main window; the PopupMenu bridge supplies its containing native owner and reserves both that window and the root display. Ordinary, check/radio, icon, separator and submenu items execute with exact logical indices, including hidden items. Submenus have one parent; reject cycles, duplicate attachment and freeing an attached child. Detach before native parent destruction so children retain independent ownership. Mutations commit after native success; active native presentation reserves its display/menu graph. Open callbacks may populate items during native initialization. Other live native-menu mutation, recursive presentation and host retirement reject during tracking. Native callbacks catch managed failures before returning across the ABI; after closure report required callback failures together.

Action and Action<T> replace dynamic Callable payloads. Exact generic tag values use the established PopupMenu metadata policy; tags and callbacks are runtime-only and borrowed values are never disposed. Typed callback assignment/tag replacement validates the same exact type before mutation. Selection dispatch follows native closure and close notifications, only while the captured item remains live. Check and multistate values remain explicit application state, with no automatic toggling. Hover callbacks follow real mouse selection. Open/close state and last native geometry use actual menu-loop messages rather than constant query results.

Icons borrow Texture resources but own copied native bitmap snapshots. Resolve changed/disposed inputs on the owner before presentation, preserve old bitmap ownership on failed replacement and unsubscribe at item retirement. Native menus use system text and decoration; no engine glyph renderer or second UI toolkit is introduced. Interface RTL changes the actual native layout. Preparation is cold; ordinary closed queries reuse retained records.

Only expose a reference operation when its complete current native consumer executes. The first Windows slice does not add inert key-callback/accelerator, tooltip, indentation or minimum-width fields. Their rows retain exact native keyboard/presentation dependencies. System menu IDs and global menu registration require the owning desktop global-menu producer; they are not constant-result public placeholders. Unsupported target creation fails explicitly, while Feature reports actual native capabilities. Desktop Linux, Apple, mobile/TV/Web and the full ADR 0021 matrix retain separate adapters and receipts.

PopupMenu's native preference uses this same owner and typed item callbacks; canvas fallback remains explicit for unsupported item/presentation roles. Native OS text/decoration determine its measured size independently of the canvas window geometry. Native focus follows the existing keyboard-only IDFocused policy; mouse hover updates the focused record without that event. Hide/Visible=false during tracking cancels the whole OS popup tree. Authored preference persists in ordinary scene/resource storage, while native identities and callbacks do not. MenuBar global-menu integration and tray/status presentation retain their actual producers.

### Verification

Use a public consumer and an independent Win32 inspector/driver for real popup geometry, native flags, Unicode, icons, selection, cancellation, submenus and close/open/hover delivery. Cover malformed/stale/foreign IDs, callback failure/reentry, detach/free/retirement and rollback. Verify source, fresh package and strict NativeAOT; distinguish real native UI from model tests, physical human acceptance and foreign-platform parity. No vendor change is required.
