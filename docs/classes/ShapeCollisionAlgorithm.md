# ShapeCollisionAlgorithm

Last updated: 2026-10-11

**Source:** [ShapeGeometry.Custom.cs](../../src/Scene/Resources/ShapeGeometry.Custom.cs)

Typed synchronous standalone pair algorithm. The first argument owns the custom implementation, all values are in engine units, and caller-owned contacts alternate first/second boundary points. Counts and finite points validate atomically. Nested distinct pairs execute; identical/reversed recursion rejects.

See [custom collision geometry](../components/physics-custom-geometry.md) for data ownership, mass, queries, drawing, mutation, replay and verified limits. File-backed custom format codecs and generic production solver acceptance remain open.
