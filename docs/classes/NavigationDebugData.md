# NavigationDebugData

Last updated: 2026-10-27

Immutable internal value snapshot of one scene navigation producer. It contains borrowed immutable polygon/contour/pathway arrays and copied scalar/transform state. Typed equality controls redraw invalidation; no public mutable array or backend identity is exposed.

Part of [navigation controls and diagnostics](../components/navigation-diagnostics.md), under [ADR 0097](../decisions/navigation.md#global-simulation-and-scene-diagnostics).
