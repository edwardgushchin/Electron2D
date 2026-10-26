# SceneAccessibility

Last updated: 2026-10-26

- Declaration: `internal sealed class SceneAccessibility`
- Source: [SceneAccessibility.cs](../../src/Scene/Main/SceneAccessibility.cs)
- Component: [Native accessibility](../components/accessibility.md)

SceneTree retains one controller for automatic native semantic output. A cached traversal follows PathRevision; membership changes reconcile native-window ownership and source-child order while preserving custom sub-elements. Each ordinary process completion synchronizes prepared captions, authored translated metadata, roles, bounds, visibility, disabled/check states and GUI focus. Queued updates coalesce. NotificationAccessibilityUpdate runs after defaults so custom producers can refine them; the focused-element extension selects one live same-window element.

Native windows have separate roots and focus targets. Embedded windows and SubViewport controls belong to their containing native window; focus follows the active viewport/embedded chain. Source state and numeric snapshots avoid re-emitting defaults over custom metadata every frame. Topology changes, callback dispatch and native queries are cold allocation-capable work; unchanged warmed traversal reuses node/focus/order storage. Nodes retire their own borrowed identity and owned sub-elements before leaving a tree, including exceptional exits. Scene serialization stores metadata, never the native identity or callbacks. SceneTree finalization releases the retained traversal/controller.

This private controller cannot add text editing or selection/table protocols by assigning a role. See the service's row-level coverage and ADR 0114 for follow-up boundaries. AccessibilityFoundationTests exercises actual native output and independently launched protocol clients.

Range sources publish numeric state and editable/read-only actions through the Value interface. Value, bounds, step and page snapshots suppress redundant native output and preserve custom overrides until source changes; delegates are created only when the source binds. AccessibilityValueTests covers all seven concrete widgets through an external native client.

Stored Control relation paths and ButtonGroup toggle membership reconcile after source updates. Tree paths, element identities, authored path revisions and current membership invalidate prepared relations; ordinary value/checked changes do not rebuild them. Incoming references retire immediately, and a later topology pass can resolve a restored path to its new identity. Manual and automatic edges remain independent. See [ADR 0114](../decisions/accessibility.md#adr-0114).

The [native text slice](AccessibilityServer.md#native-text) adds source line reconciliation, Text/EditableText request handling and scalar geometry/ICU boundaries. Private query snapshots and copied TextStyle metadata retain no borrowed native font pointers; active source publication reuses cached lines.
