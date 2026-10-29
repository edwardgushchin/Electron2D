# Native accessibility

Last updated: 2026-10-27

## Scope and owned types

[ADR 0114](../decisions/accessibility.md#adr-0114) connects a typed semantic tree to the native assistive-technology bus, automatic scene metadata and existing GUI focus/activation. [AccessibilityServer](../classes/AccessibilityServer.md) is the public static owner; its Role/Flags/Action enums carry exact typed selectors. [AccessibilitySupport](../classes/AccessibilitySupport.md) selects application policy. Private [SceneAccessibility](../classes/SceneAccessibility.md), [ATSPIServer](../classes/ATSPIServer.md) and [GIONative](../classes/GIONative.md) own scene reconciliation, native protocols and ABI lifetime respectively. AccessibilityServer.Element retains typed producer state; ATSPIServer's Registration and exception helpers and GIONative's VTable/GError layouts stay private.

## Runtime flow and invariants

Display opening samples the registered accessibility/general/accessibility_support setting. Auto follows the host accessibility-enabled state; Always publishes only this application; Disabled establishes no bridge. None changes desktop preferences. IsSupported distinguishes a live native bridge from missing libraries/bus and unsupported targets. Querying support alone does not activate accessibility-only focus; automatic output or actual manually created elements does.

On Linux the adapter obtains the real accessibility bus through org.a11y.Bus and registers its application with org.a11y.atspi.Registry. It exports Accessible, Application, Component, Action, Value, Text, EditableText, Selection, Table, TableCell and Cache. The display's existing owner pump processes bounded native callback batches; callbacks never execute on a bus worker. Signature/semantic request errors return wire errors without killing the owner. User callback failures return a native Failed reply and aggregate after the native boundary returns.

An automatic scene pass follows ordinary process/deferred work. Cached node membership/order rebuilds only on topology revision; stable frames reuse storage. Prepared Label/Button/LinkButton captions and stored translated Control.AccessibilityName/AccessibilityDescription feed names/descriptions. Actual toggle, disabled, visible, clipping, role and window-relative geometry feed native state. Changed defaults and queued updates notify the producer once; custom producers can refine the borrowed element. Composite focus uses OnGetFocusedAccessibilityElement; the selected RID must be live in the same native window. Native focused state follows that window's active state, which is distinct from retained GUI focus in an inactive window.

Native windows have independent roots and focus targets. Embedded windows and SubViewport descendants remain under their containing native window. Native screen coordinates use actual decorated/client origins when available. Wayland leaves global origin unknown; its window-relative bounds remain valid. Custom producers can supply known outer/client rectangles explicitly. TouchPassthrough and ClipsChildren affect semantic hit testing without changing ordinary GUI hit tests.

Window roots and source-node elements are borrowed. CreateElement is producer-owned; CreateSubElement transfers lifetime to its current semantic parent. Freeing a producer detaches independent children and recursively retires owned ones. Node exit invalidates its transient identity and custom sub-elements; native-window retirement also removes unattached elements. Native signal failures cannot interrupt the rest of retirement. Interface GCHandles are freed by GIO destroy notifications after queued callbacks retire, before their context is released. Scene copies/files retain authored metadata and exclude native RIDs/callbacks.

## Protocol source and dependencies

The first profile uses optional system libgio-2.0.so.0, libglib-2.0.so.0 and libgobject-2.0.so.0 through their public ABI. It adds no managed assembly or bundled native dependency. Protocol declarations and native role/state IDs were checked against [AT-SPI commit 3e62300](https://github.com/GNOME/at-spi2-core/tree/3e62300dae26649dc002f799c8f16bf792316b04). The [architecture](https://gnome.pages.gitlab.gnome.org/at-spi2-core/devel-docs/architecture.html) and [GIO registration lifetime](https://docs.gtk.org/gio/method.DBusConnection.register_object.html) define the bus and callback-context boundary. Small native tuples use stack spans; native builders and allocator totals remain separate from managed allocation checks. Embedded NUL text becomes U+FFFD at the native UTF-8 boundary.

## Verification and limits

The Release runner supports ELECTRON2D_TEST_ACCESSIBILITY=managed, prototype, lifetimes, gpu and compatibility. [Managed checks](../../tests/Electron2D.Tests/AccessibilityFoundationTests.Managed.cs) cover detached guards, stored properties, duplication, authoring warnings and fresh-process archives. [Native checks](../../tests/Electron2D.Tests/AccessibilityFoundationTests.cs) and [lifetime checks](../../tests/Electron2D.Tests/AccessibilityFoundationTests.Lifetime.cs) launch independent [gdbus clients](../../tests/AccessibilityClient/probe.py) to discover registry/application/window/control trees, read roles/states/bounds/cache, observe native events and invoke ordinary focus/Pressed. They cover reparenting inside the currently executing native action, defunct references, hidden/disabled guards, composite focus selection, independent native-window retirement, bad requests, callback failures and fresh reopening. Prepared idle/active full-frame managed allocations and actual rendered PNGs are checked on both current software-native canvas backends.

These checks establish the exercised Linux protocol and GUI behavior. A real human screen-reader session, native heap totals, physical GPUs/input, performance throughput, X11 screen placement and foreign-platform adapters remain separate acceptance gates. Remaining color/custom payload actions retain explicit follow-up slices in coverage; assigning a role does not implement those protocols. Driver selection and automatic scene scheduling now execute as described below.

Container.AccessibilityRegion is stored and inherited across the complete container family. It toggles the real landmark/container native role using the ordinary accessible name; unnamed landmarks and actions expose authoring warnings. The typed project policy has generated JSON enum metadata so static startup and persistence work without reflection.

The standalone NativeAOT consumer verifies compiled policy persistence, native callback failure/reopening, manual semantic geometry/hit/action output and automatic Compatibility GUI landmarks/roles/states/focus/actions with local native prerequisites in the documented runtimes layout. This is an executable public-project check; release-package qualification remains the separate development/release pipeline.

Edited-scene warning notifications refresh after name, landmark and displayed-caption changes. SceneTree finalization releases the retained semantic traversal. Native topology changes publish descendant visibility updates; native integer projection saturates large composed coordinates while preserving rectangle extents.

Numeric output adds four real producer setters, Action<double> SetValue, no-argument decrement/increment, native Value property reads/writes and page actions. Automatic sources cover all seven concrete Slider/ScrollBar/SpinBox/progress widgets. The numeric boundary, read-only policy, ordinary shared Range mutation and cold text formatting are specified in [ADR 0114](../decisions/accessibility.md#adr-0114). The protocol declaration is pinned to the same GNOME commit: [Value.xml](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/xml/Value.xml).

ELECTRON2D_TEST_ACCESSIBILITY_VALUES=gpu or compatibility launches [the numeric client](../../tests/AccessibilityClient/values.py) from [a public scene](../../tests/Electron2D.Tests/AccessibilityValueTests.cs). It exercises native values/actions/events, snapping/page caps, shared peers, reentry, silent writes, read-only/hidden/retired guards, affixed value text, IEEE publication and finite request validation. Numeric callback failure must reach the owner with the original exception; a fresh display must reopen. Prepared frame measurements distinguish idle and actively changing values. Release-package and human-reader acceptance remain separate.

## Semantic relationships

Control's four stored copied path arrays, current ButtonGroup toggle peers and the complete related-element service family now feed native direct/reciprocal relations. Explicit group/error/active-descendant/internal-link targets have real native consumers. [The service reference](../classes/AccessibilityServer.md#semantic-relationships) owns per-method semantics; [ADR 0114](../decisions/accessibility.md#adr-0114) owns provenance, ownership, focus and native projection. [Native hyperlinks](../classes/AccessibilityServer.md#native-hyperlinks) also publish actual LinkButton and custom-producer URLs; the owning contract records endpoint coexistence, the corrected Int32 NAnchors ABI and independent-client verification. No originating Hypertext range is claimed.

ELECTRON2D_TEST_ACCESSIBILITY_RELATIONS=managed verifies copied metadata, warnings, duplication and fresh-process scenes. gpu/compatibility launch [an independent native graph client](../../tests/AccessibilityClient/relations.py) against [a rendered form](../../tests/Electron2D.Tests/AccessibilityRelationTests.cs): real value validation, label repair, direct/reciprocal links, manual/source overlap, radio membership, virtual descendant focus and hyperlink target retirement. The focus-state fixture publishes the window's active semantic flag after drawing; it verifies protocol focus routing, not physical desktop focus acquisition. Prepared idle and active graph/focus changes retain the managed-owner allocation boundary. NativeAOT uses a standalone public source consumer; package delivery and human reader acceptance remain separate.

## Native text

[The service reference](../classes/AccessibilityServer.md#native-text) owns Unicode offsets, copied shaped lines, typed callbacks, source editing, font/styling attributes, password and scrolling contracts. [Text.xml](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/xml/Text.xml) and [EditableText.xml](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/xml/EditableText.xml) pin the native ABI. Private TextDocument composes query snapshots; SourceTextLines retain source identities independently of stored scene state. TextLayout supplies actual scalar geometry and fallback-font metrics, while the existing ICU wrapper supplies word/sentence boundaries.

ELECTRON2D_TEST_ACCESSIBILITY_TEXT=gpu or compatibility launches [an independent native client](../../tests/AccessibilityClient/text.py) against [a rendered public scene](../../tests/Electron2D.Tests/AccessibilityTextTests.cs). It exercises text reads/edits, scalar selections and multiple carets, real geometry, source/producer lifetime, masks, read-only/length policies and prepared active/idle frames. Native query allocation and physical/human acceptance remain separate from the frame measurement.

The clipboard profile uses an explicitly selected SDL dummy/software process and verifies the actual headless driver before any copy/cut/paste. It exercises those native requests without replacing the desktop clipboard. Headless output keeps unknown screen origin, as Wayland does, rather than querying unsupported window decorations. The independent client also uses libatspi to check cached Text/EditableText discovery and editability transitions.

## Native scrolling

[The service reference](../classes/AccessibilityServer.md#native-scrolling) owns the finite state, typed payload, source-unit and placement contracts. [Component.xml](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/xml/Component.xml) pins ScrollTo/ScrollToPoint. The bridge emits offset attributes and visible-data changes, preserves read-only scrolling, distinguishes visible from showing and uses the renderer's actual clip geometry for exposure/hits.

ELECTRON2D_TEST_ACCESSIBILITY_SCROLL=gpu or compatibility launches [an independent wire/libatspi client](../../tests/AccessibilityClient/scroll.py) against [a public rendered scene](../../tests/Electron2D.Tests/AccessibilityScrollTests.cs). It exercises eight source families, nested/affine placement, unit/hint/point dispatch, RTL, oversized/extreme/singular cases, stable focus/selection, reentrant retirement and callback failure/reopening. Prepared idle/active owner frames are measured separately from cold requests, layout work and native allocations. Live-announcement producers retain their separate dependency.

## Native collections

[The service reference](../classes/AccessibilityServer.md#native-collections) owns collection publication, native index/span policy, selection callbacks and source identity lifetime. Native Selection/Table/TableCell declarations are pinned to the same [GNOME protocol revision](https://github.com/GNOME/at-spi2-core/tree/3e62300dae26649dc002f799c8f16bf792316b04/xml). ItemList, Tree/TreeItem and TabBar/owned TabContainer strips supply actual selectable virtual children, expansion, headers and cell navigation through ordinary GUI behavior.

ELECTRON2D_TEST_ACCESSIBILITY_COLLECTION=gpu or compatibility runs [a rendered public scene](../../tests/Electron2D.Tests/AccessibilityCollectionTests.cs) with [an independent wire/libatspi client](../../tests/AccessibilityClient/collection.py). It exercises source selection/events, merged cells/headers, folding, offscreen reveal, identity-preserving reorder/transfer, duplication, removal, invalid/extreme requests, reentrant retirement and callback failure/reopening. Prepared idle and actively changing frames retain the managed-owner allocation boundary. NativeAOT uses a public source consumer with local prerequisites; human reader, physical devices, foreign adapters, native heap and release-package qualification remain separate.

## Live regions and popup presentation

[The service contract](../classes/AccessibilityServer.md#native-live-and-popups) owns typed priority, coalescing, inheritance/Off, privacy, popup capability, tooltip actions and real menu-source semantics. [Event.xml](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/xml/Event.xml) pins the native Announcement signature. Control stores AccessibilityLive; LineEdit stores ContextMenuEnabled and supplies its lazy borrowed menu. PopupMenu/OptionButton add real selectable menu sources, including separate native window roots.

ELECTRON2D_TEST_ACCESSIBILITY_LIVE=gpu or compatibility launches [an independent client](../../tests/AccessibilityClient/live.py) against [the public scene](../../tests/Electron2D.Tests/AccessibilityLiveTests.cs), including libatspi Announcement reception, real tooltip/context presentation, custom commands, disabled/read-only policies, stable embedded item identity and callback failure/reopening. The corresponding gpu-native/compatibility-native profiles exercise [distinct native popup surfaces](../../tests/Electron2D.Tests/AccessibilityLiveTests.Popups.cs), rendered item geometry, source mutations and root/item retirement. Popup tests may repeat a bounded presentation request when ordinary application focus loss cancels a transient; they do not require desktop focus ownership. Prepared idle and active full frames measure managed allocations separately from cold menus/native queries and native heaps. A standalone strict NativeAOT consumer exercises the same public flow; release packages, physical devices, foreign adapters and human speech acceptance retain their own gates.

## Native custom actions and drag transactions

[The canonical contract](../classes/AccessibilityServer.md#custom-actions-and-accessible-drag) connects integer custom IDs, native names with context, assertive grab announcements and actual Control/text transactions. [Action.xml](https://github.com/GNOME/at-spi2-core/blob/3e62300dae26649dc002f799c8f16bf792316b04/xml/Action.xml) pins the native action ordinals/descriptions. Element custom state stays transient; the typed project input definition uses existing persistence.

ELECTRON2D_TEST_ACCESSIBILITY_DRAG=managed checks [ownership, forwarding, keyboard remapping/project files and text/capacity](../../tests/Electron2D.Tests/AccessibilityDragTests.Managed.cs). gpu/compatibility run [a real public scene](../../tests/Electron2D.Tests/AccessibilityDragTests.cs) and [an independent libatspi client](../../tests/AccessibilityClient/drag.py): exact signed IDs, replacement ordinals, empty descriptions, disabled/unbound/retired actions, NUL/Unicode/large name context, actual announcements, preview output, acceptance changes, transfers and callback failure/reopening. Prepared idle/active full frames are measured separately from cold source preparation and native requests. Strict NativeAOT references the same public fixtures; source-consumer qualification does not establish release-package, native-heap, physical-device, foreign-adapter or human reader acceptance.

[Contextual native navigation](../classes/AccessibilityServer.md#contextual-navigation) connects custom container position/help, direct keybinding metadata from real button/menu sources, class names and role descriptions. The owning contract defines override, lifecycle and verification boundaries.


[Producer models](../classes/AccessibilityServer.md#producer-models) connect typed, process-local element metadata to existing compiled native actions. The service retains the slot without owning disposal; replacement, clearing and all element retirement paths release it. Automatic scene identities and copying remain separate. AccessibilityModelTests and its independent native client verify current-model lookup, ownership, failures and prepared reference-model frame budgets.

The foundation retirement/reopening regression gives its deferred child finite bounds before attachment: Visible includes connected semantic groups, while Showing also requires nonempty geometry. Attaching that subtree still has to emit the real showing transition.


## Native driver and scene scheduling

`ProjectSettings.AccessibilityDriver` is a permanent, nonbasic string setting at
`accessibility/general/accessibility_driver`. `atspi` selects the implemented
GIO/AT-SPI bridge and is the default; `dummy` disables both automatic output and
explicit element operations. Names are case-sensitive and unsupported names
reject during settings validation. The display samples the effective feature
override before attempting native initialization. Changing it affects the next
display, including after failure. A selected native driver still reports
unavailable when the host libraries or accessibility bus are absent. Selection
never changes desktop settings or reports a simulated service as supported.

`ProjectSettings.AccessibilityUpdatesPerSecond` is a permanent, nonbasic positive
integer setting at `accessibility/general/updates_per_second`, default 60. A new
SceneTree captures its effective feature override. Its first automatic semantic
pass runs immediately; later passes run at most once per monotonic wall-clock
interval and at most once per idle frame. A slow frame skips missed intervals
without replaying stale snapshots. Pausing gameplay, time scaling and a frame's
simulated delta do not stop or accelerate this clock. Repeated queued requests
coalesce; each pass reads the latest source metadata, topology, locale and focus.
Both preferences carry restart-required registry metadata. Existing displays and
trees retain their captured selections.

Native requests continue through the ordinary display event pump between scene
passes. Explicit service setters publish immediately, and source/window retirement
still invalidates its RID and callbacks immediately. Native activation checks the
current source visibility, enabled state and lifetime even before a scheduled
metadata pass. Pending manual live announcements flush on skipped scene passes;
source-generated announcements follow their scheduled source publication. This
rate is a ceiling for automatic scene publication, not a bus request limit or a
fixed simulation frequency.

`ELECTRON2D_TEST_ACCESSIBILITY_SCHEDULING=managed`, `gpu` and `compatibility`
select [settings and host checks](../../tests/Electron2D.Tests/AccessibilitySchedulingTests.cs).
The [independent libatspi client](../../tests/AccessibilityClient/scheduling.py)
reads the actual native scene and invokes actual buttons while metadata is
coalesced. On Linux/Wayland, GPU and compatibility runs verify native execution, disabled
and reopened displays, scheduled callback failure and 64 idle/64 active prepared
owner-frame samples with zero managed bytes. NativeAOT, foreign targets, native
heap totals and human-reader acceptance remain separate gates.

Mutating native requests force current scene passes before dispatch and after successful mutation, including numeric writes, actions, focus, scrolling, text edits/selections and collection selection. This preserves current geometry and selection between rate-limited publications. Read-only native queries do not force a pass. If source preparation retires the requested identity, the request returns an expired-object error; preparation failures keep the existing owner-pump aggregation policy.
