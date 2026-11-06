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

PopupMenu's native preference uses this same owner and typed item callbacks; canvas fallback remains explicit for unsupported item/presentation roles. Native OS text/decoration determine its measured size independently of the canvas window geometry. Native focus follows the existing keyboard-only IDFocused policy; mouse hover updates the focused record without that event. Hide/Visible=false during tracking cancels the whole OS popup tree. Authored preference persists in ordinary scene/resource storage, while native identities and callbacks do not. MenuBar global-menu integration retains its actual producer. The Windows status-indicator producer below consumes this same menu owner.

### Status indicators

The DisplayServer status-indicator family uses the existing static service and seven typed operations plus InvalidIndicatorId. CreateStatusIndicator borrows a nullable Texture and accepts an optional Action receiving MouseButton and desktop Vector2i. IDs are positive process-monotonic signed integers and never bind another display session; null icons are actual Shell image removal, not inert model fields. Tooltip text rejects NUL and truncates to the Shell 127 UTF16-unit limit without splitting a surrogate pair. GetRect returns native desktop bounds or an empty rectangle when the Shell cannot locate the icon. For hidden icons the Shell may report its overflow activation rectangle; once revealed it reports the icon rectangle.

Windows uses Shell_NotifyIconW/NotifyIconGetRect with NOTIFYICON_VERSION (3), preserving full-width IDs and pressed-button notifications. Native callbacks only enqueue accepted button/position/native-anchor/delegate/menu snapshots. ProcessEvents delivers them on the owner thread after the native poll, catches failures, continues required work and reports failures together. ForceProcessAndDropEvents discards queued activation input. Delete/retirement invalidate queued records, unsubscribe borrowed icons, detach menu bindings and release owned HICON snapshots. Icon replacement commits only after successful native modification; failed preparation/modification preserves the old source and HICON. Resource change/disposal is refreshed by the owner pump. Icon virtual reads reserve the display against disposal and reject status-family reentrant mutation.

A bound NativeMenu RID is borrowed from the exact display-owned menu service. Multiple indicators may share one root; an attached submenu cannot be bound, and a status-bound root cannot become a submenu until unbound. FreeMenu detaches its status bindings without disposing the indicators. Right-button presses with a live binding open its real menu at the native icon center; other presses, or an absent/retired binding, deliver the typed callback. No raw Shell/GDI handles enter public APIs.

The backend probes the active Windows driver and actual Shell notification host for StatusIndicator capability. TaskbarCreated marks retained icons for re-registration by the owner pump, with the same IDs, current images/tooltips and callbacks; no Explorer restart is performed by engine or tests. Failed native deletion retires managed ownership and reports uncertain OS removal. Display shutdown attempts all removals before menu/window retirement. Unsupported drivers fail explicitly. Other platforms retain their adapter/qualification dependencies; creation on Windows does not establish equivalent platform behavior.

### Verification

Use a public consumer and an independent Win32 inspector/driver for real popup geometry, native flags, Unicode, icons, selection, cancellation, submenus and close/open/hover delivery. Cover malformed/stale/foreign IDs, callback failure/reentry, detach/free/retirement and rollback. Verify source, fresh package and strict NativeAOT; distinguish real native UI from model tests, physical human acceptance and foreign-platform parity. No vendor change is required.

Status-indicator verification uses an independent Shell inspector/driver for actual registration, native rects/tooltips and callback/menu roundtrips, plus source/fresh-package/strict NativeAOT failure/lifetime/retry controls. Actual on-screen pixels and physical activation need separate evidence when the desktop obscures the icon or denies foreground ownership; injected messages must remain explicitly identified. A scoped TaskbarCreated notification is a recovery-path test, not proof of actual Explorer restart recovery.
