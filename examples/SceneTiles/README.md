# Scene tiles

Create a portable scene, then open it in a separate run:

```bash
dotnet run --project examples/SceneTiles/SceneTiles.csproj -- write scene-assets
dotnet run --project examples/SceneTiles/SceneTiles.csproj -- scene-assets
```

The upper row shows eight transforms of one visual prefab. The lower row instantiates ordinary static bodies and navigation regions from another prefab. The source borrows templates; the tile layer creates independent scene children after entering the tree. Authoring uses only the public runtime API.

Body nodes currently require unit global scale and zero skew; keep their prefab placements unreflected. Editor scene placeholders and a tile-authoring workspace remain separate capabilities.
