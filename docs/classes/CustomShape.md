# CustomShape

Last updated: 2026-10-11

**Source:** [CustomShape.cs](../../src/Scene/Resources/CustomShape.cs)

A concrete resource holding initialized immutable custom geometry. Geometry replacement emits a revision change and preserves RID identity. Duplicate and server data copies share immutable geometry in independent resource wrappers. CustomShapeCreate accepts this complete data directly.

See [custom collision geometry](../components/physics-custom-geometry.md) for data ownership, mass, queries, drawing, mutation, replay and verified limits. File-backed custom format codecs and generic production solver acceptance remain open.
