# CustomShape

Last updated: 2026-10-13

[Curved custom verification](../components/physics-registered-adapter.md#curved-custom-contacts)
now exercises the independent annulus's inner/outer CCD arcs, motor/counterpart
motion, valid boundary contact points, momentum and private replay in serial and
separate numerical modes. This does not make the format a built-in algorithm.

**Source:** [CustomShape.cs](../../src/Scene/Resources/CustomShape.cs)

A concrete resource holding initialized immutable custom geometry. Geometry replacement emits a revision change and preserves RID identity. Duplicate and server data copies share immutable geometry in independent resource wrappers. CustomShapeCreate accepts this complete data directly.

See [custom collision geometry](../components/physics-custom-geometry.md) for data ownership, mass, queries, drawing, mutation, replay and verified limits. File-backed custom format codecs and generic production solver acceptance remain open.

Geometric writes use [Shape.EnsureGeometryChangeAccess](Shape.md) before private
authoring, including equal assignments. Scene/raw owner, phase and borrowed-hook
rejection preserves data, RID and immutable geometry cache. Publication rechecks
owner/phase without repeating selected status hooks; ordinary post-solver edits
retain their existing behavior.

The public-only --custom consumer verifies independent annulus/circle resource
sweeps, retained inner holes and true boundary contacts in both argument orders
through registered, CPU and explicitly enabled GPU spaces.
Twelve exact cases, 512 seeded boundary cases and 128 warmed standalone/selected
cycles retain the .0001 scene-unit boundary tolerance and 0/0 managed-byte gate.
See [custom collision geometry](../components/physics-custom-geometry.md) for the
analytic model and remaining numerical-extension limits.
