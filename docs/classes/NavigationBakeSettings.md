# NavigationBakeSettings

Last updated: 2026-10-10

Internal production type in [NavigationPolygon.Baking.cs](../../src/Navigation/2D/NavigationPolygon.Baking.cs), compiled into Electron2D.dll.

Internal immutable record of all target bake/source selection settings. Resource captures it with its copied outlines under one gate; worker input remains coherent while later edits replace records. Generated CellSize accompanies the output for map publication checks. No renderer/native ownership is stored.

The [baking component](../components/navigation-baking.md) documents actual integration, failure/lifetime/ordering and NavigationBakingTests/BakedRoutes verification. No public constructor/backend API is exported.
