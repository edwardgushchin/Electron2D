# Tile routes

Write a portable scene in one process, then run it in another:

```sh
dotnet run --project examples/TileRoutes/TileRoutes.csproj -c Release -- write /tmp/tile-routes
dotnet run --project examples/TileRoutes/TileRoutes.csproj -c Release -- /tmp/tile-routes
```

The scene stores shared navigation polygons in a TileSet, generates navigation regions for a square ring of cells, and draws the queried path and navigation overlay. The dark center is not walkable.
