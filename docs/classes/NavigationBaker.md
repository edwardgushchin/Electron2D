# NavigationBaker

Last updated: 2026-10-10

Internal production type in [NavigationBaker.cs](../../src/Servers/Navigation/NavigationBaker.cs), compiled into Electron2D.dll.

Internal bounded planar clipping/clearance/partition kernel. Converts immutable source/settings snapshots into complete NavigationPolygonData. Uses Clipper2 precision two and a finite coordinate limit of 1e15; preserves NonZero winding, Miter erosion and carve/border order. Convex partition has a convex-input fast path and adjacency-driven triangle merging. Failed preparation throws before target publication. Cold work may allocate; worker/native/foreign boundaries remain explicit.

The [baking component](../components/navigation-baking.md) documents actual integration, failure/lifetime/ordering and NavigationBakingTests/BakedRoutes verification. No public constructor/backend API is exported.
