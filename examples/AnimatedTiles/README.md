# Animated tiles

Author a portable scene, then reload it in a separate process:

```sh
dotnet run --project examples/AnimatedTiles/AnimatedTiles.csproj -c Release -- write /tmp/animated-tiles
dotnet run --project examples/AnimatedTiles/AnimatedTiles.csproj -c Release -- /tmp/animated-tiles
```

The upper row shares synchronized red/green frame timing. The lower row uses stable random starting phases. Red lasts one quarter of each cycle; blue end cells remain static. The source supplies a padded runtime atlas and the layer replays retained frame commands.
