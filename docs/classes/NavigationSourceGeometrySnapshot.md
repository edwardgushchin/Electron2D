# NavigationSourceGeometrySnapshot

Last updated: 2026-10-10

Internal production type in [NavigationMeshSourceGeometryData.cs](../../src/Navigation/2D/NavigationMeshSourceGeometryData.cs), compiled into Electron2D.dll.

Internal immutable record retaining three copied contour families. Resource getters expose independent arrays/immutable projected entries. Setters replace complete snapshots, and background jobs retain the captured version independently of later source edits or disposal. Storage and preparation point/count limits are shared.

The [baking component](../components/navigation-baking.md) documents actual integration, failure/lifetime/ordering and NavigationBakingTests/BakedRoutes verification. No public constructor/backend API is exported.
