# ATSPIServer

Last updated: 2026-10-26

- Declaration: `internal sealed unsafe partial class ATSPIServer : IDisposable`
- Source: [ATSPIServer.cs](../../src/Servers/Accessibility/ATSPIServer.cs)
- Component: [Native accessibility](../components/accessibility.md)

Owns a private GMainContext and authenticated accessibility-bus connection. It exports Accessible, Application, Component, Action and Cache, registers its application with the real registry, and pumps bounded batches on the display owner. Native callbacks validate requests and return protocol errors. Application failures return a native Failed reply and aggregate after callbacks have returned to managed owner code. Unsupported platforms and missing optional native dependencies do not publish a fake bridge.

Private Registration retains the element/bridge and native registration IDs. Each registered interface has its own GCHandle; GIO's destroy notification releases that handle only after queued callbacks retire. Unregister emits defunct/cache removal where possible, then unregisters every interface in a finally block. Dispose unregisters all objects, closes the connection, drains destroy notifications and releases context/introspection/vtable ownership. InvalidRequestException represents wire errors without poisoning the owner; CallbackException preserves the original application error for pump aggregation.

Component queries project window/parent/screen geometry and semantic clipping/passthrough. Unknown Wayland screen origin uses the native unknown-coordinate sentinel; client-window bounds remain usable. Role/state tables follow the pinned protocol enums. Placement/scroll requests use source or producer callbacks; numeric Value reads/writes use compiled callbacks. The Value interface registers on first actual numeric publication, advertises only then and retires with its element. Positive jump creates native page actions; read-only sources reject numeric requests. Text/EditableText and Selection/Table/TableCell register on actual publication. The protocol XML contains ABI declarations only, with the source identity documented in the component. Independent client tests exercise discovery, cache, events, actions and retirement; raw protocol checks are distinct from human screen-reader acceptance.

Accessible.GetRelationSet projects retained direct links and native reciprocals, grouped and deduplicated at query time. Relation queries are cold allocation-capable work. The Hyperlink interface registers on the first internal anchor and retires with its element; GetObject returns the actual target. Active-descendant/error events use the standard Object event signature and native state bits. See [the service](AccessibilityServer.md#semantic-relationships).

The [native text slice](AccessibilityServer.md#native-text) adds source line reconciliation, Text/EditableText request handling and scalar geometry/ICU boundaries. Private query snapshots and copied TextStyle metadata retain no borrowed native font pointers; active source publication reuses cached lines.

The Scroll partial adapts native Component placement and Action requests, exposes offset/range attributes and events and distinguishes visible from showing. Clip projection reuses the existing rendered clip; grouping and internal bar hits survive. Visibility traversal uses retained reentrant segments. Bounds queries tolerate source lines removed before semantic retirement. See [native scrolling](AccessibilityServer.md#native-scrolling) for payload, units, ownership and verification limits.

CollectionProtocol/Collections export native selection and table/cell queries and dispatch, including span-aware axis coverage, source policy and bounded selected-index responses. Source Tree cells remain direct children with separate non-owning hierarchy relations. See [native collections](AccessibilityServer.md#native-collections).

ATSPIServer.Live coalesces pending live content on the owner and emits the pinned Object.Announcement signature. Hidden/protected/retired and Off regions suppress output; numeric formatting uses a stack UTF-8 buffer. Popup/tooltip metadata and action names use the same native states/attributes/actions as ordinary queries. See [the service contract](AccessibilityServer.md#native-live-and-popups).

The Custom partial publishes integer-ID actions and composes native Name/cache/events from base name and extra context through stack/native UTF-8 storage. The existing native callback boundary handles custom errors and retirement. [Custom actions](AccessibilityServer.md#custom-actions-and-accessible-drag) owns the public contract.
