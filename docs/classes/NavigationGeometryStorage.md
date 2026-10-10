# NavigationGeometryStorage

Last updated: 2026-10-10

Internal production type in [NavigationMeshSourceGeometryData.cs](../../src/Navigation/2D/NavigationMeshSourceGeometryData.cs), compiled into Electron2D.dll.

Internal bounded planar contour copy/read/write helpers shared by target outlines and source Resource payload. Limit is 1048576 points/contours; file reads validate finite points, count and remaining bytes before allocation. No reflection, native identities or delegates are persisted. Helpers allocate only explicit setup/storage buffers.

The [baking component](../components/navigation-baking.md) documents actual integration, failure/lifetime/ordering and NavigationBakingTests/BakedRoutes verification. No public constructor/backend API is exported.
