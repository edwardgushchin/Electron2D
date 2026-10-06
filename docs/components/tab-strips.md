# Tab strips

Last updated: 2026-10-06

## Scope and owned types

TabBar : Control owns ordered tab data, prepared intrinsic/clipped TextLayout caches and one internal Timer for foreign-drag hover selection. Its AlignmentMode and CloseButtonDisplayPolicy retain logical alignment and close-button policy. Icons, fonts/theme resources and exact typed metadata are borrowed; native glyph resources stay with existing fonts. ThemeDB.Tabs.cs supplies every declared style/icon/color/font/constant key through ordinary theme ownership. No separate renderer, native popup window or untyped value system is introduced.

## Runtime flow

Model edits invalidate the strip; layout shapes translated titles with per-tab language/direction, measures style/icon/text/button widths, preserves non-text content under a cap, chooses a clipped page and mirrors it in RTL. Hidden records retain identity without consuming layout space. Alignment shifts the current page inside the arrow-reduced available width. Text clipping uses a second prepared cache so measuring intrinsic widths does not invalidate a clipped layout on every selection edit. Drawing places nonselected tabs first, the selected tab last, then arrows and an active drop marker through retained CanvasItem commands. ClipTabs also bounds own drawing for an oversized first tab.

CurrentTab commits current/previous indices and layout before Selected/Changed callbacks. Equal selection emits Selected only; nested edits suppress an obsolete outer Changed. Count and move operations preserve their separate notification rules; attached removal of the selected tab reports its replacement without Selected. Available navigation skips hidden/disabled tabs without wrapping. Keyboard and controller directions mirror in RTL, with internal scaled-time controller repeat and focus-loss cancellation. Close and auxiliary button actions remain requests; user code decides whether to remove data.

The existing root-viewport GUI drag system borrows a typed internal payload containing source/tab identities, creates a translated Label preview and routes target checks/drops. Same-strip drop reorders an available tab and reports ActiveTabRearranged before selection. Cross-strip transfer requires matching groups other than -1, the same tree and target capacity. It prepares target icon subscriptions, commits both lists before callbacks and moves all metadata/layout/icon configuration. Removed/disposed/stale payloads reject. A non-tab drag can start the themed one-shot hover timer without being accepted as a tab drop; disabled/hidden tabs and explicit disable do not activate. The timer is a real internal child, recreated per instance and disposed through Node ownership.

## Invariants and failures

Source indices are zero-based, count is bounded to 65,536 and enum Max values are sentinels. Typed metadata reads require the same T, including explicit typed null; wrong/missing type throws KeyNotFoundException. Resources remain borrowed and shared icon disposal clears all matching roles. Attached edits and scene capture follow the shared Node guards; shaping callbacks cannot edit/dispose their strip. Finite hit coordinates are required. Data/layout commitment precedes events; collected observer failures do not prevent remaining mandatory delivery/cleanup. Structural callbacks may invalidate a former index, so later actions follow tab identity rather than saved indices.

The slice corrects concrete interaction failures: release over another button does not close that tab, stale drag indices cannot transfer a replacement record, disabled foreign-hover activation is suppressed, whole transfers commit before observer callbacks, nested selection does not publish obsolete Changed, trailing hidden records do not create useless navigation and oversized first-tab own pixels are clipped. Ordinary declared selection/lifecycle/default behavior is retained; these adaptations remain explicit in coverage.

## Persistence and integration

PackedScene captures count/configuration/current and the declared indexed title/tooltip/icon/disabled fields. Current restores after indexed data. Other per-tab runtime fields and metadata are absent from that stored schema; public setters still execute them. The built-in TabBar file factory supports actual e2dscene load/instantiate. Internal timer/processing state does not enter the file. TabBarTests saves a file and loads/runs it in a fresh process with ordinary public factories and navigation. [TabContainer](tab-panels.md) now supplies the independent executable content-panel consumer; popup menus/windows and editor authoring retain separate dependencies.

## Verification

`ELECTRON2D_TEST_TABS=1 dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` exercises defaults, queued/equal/nested/failed selection, unavailable navigation, move/remove/count state, exact metadata, icons/disposal, cap/tooltips, translation direction/language, RTL geometry/hit testing, page reveal, pointer/RMB/middle/close ordering, canceled releases, group identity transfer, hover-delay/disable, owner/lifetime and packed/fresh-process file state.

`ELECTRON2D_TEST_TABS_NATIVE=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` executes current GPU and compatibility Window/Engine.Run hosts, verifies pixels for style/title/icon/close, mirrored overflow/reveal and clipped own output, then measures 64 warmed active selection/layout/record/render frames. Both backends passed with zero measured managed bytes. The native PNGs were inspected visually. Readback itself, cold shaping/structural edits and source changes allocate; native/external allocations, real controller hardware, other platforms and owner acceptance remain unverified. Native accessibility roles/actions require the absent accessibility service.

Dependencies and decisions: shared Control/Node scene/input/focus/drag, ThemeOwner/ThemeDB, Font/TextLayout and retained CanvasItem under ADRs 0008, 0038, 0046 and 0083. No architecture decision or vendored algorithm changes were needed.

Tab panels share strip theme resources and private tab-record identity to transfer complete state with actual Control page ownership. These helpers remain internal; the TabBar public surface is unchanged. Container drags reuse existing insertion, preview and acceptance behavior, while the container commits scene hierarchy changes.
