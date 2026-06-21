# TileMapLayer runtime API

Статус: реализация T-0135.
Обновлено: 2026-06-22.

`TileMapLayer` добавляет runtime-путь для tile-based уровней без генерации `Sprite2D` или `StaticBody2D` дочерних узлов. Слой остаётся обычным `Node2D`, а rendering и physics используют внутренние представления.

## Tileset

`TileSet` хранит размер клетки и набор source objects. Для `0.1.0 Preview` реализован `TileSetAtlasSource`:

- `Texture` задаёт atlas texture;
- `TextureRegionSize` задаёт размер одной atlas region;
- `CreateTile()` создаёт tile entry по atlas coordinates;
- `GetTileData()` возвращает per-tile metadata;
- `GetTileTextureRegion()` вычисляет source rectangle в texture pixels.

`TileData` хранит визуальные данные (`Modulate`, `TextureOrigin`, `ZIndex`) и collision polygons. Collision polygon points задаются в local cell space от верхнего левого угла клетки. Runtime baseline использует AABB этих точек.

## Layer cells

`TileMapLayer.SetCell()` записывает `sourceId`, `atlasCoords` и `alternativeTile` для map coordinates. `EraseCell()` удаляет клетку, `Clear()` очищает слой.

Запросы `GetCellSourceId()`, `GetCellAtlasCoords()` и `GetCellAlternativeTile()` возвращают `-1`, `Vector2I(-1, -1)` и `-1`, если клетки нет. `GetCellTileData()` возвращает `null`, если клетка не указывает на существующий atlas tile.

`MapToLocal()` возвращает центр клетки в local space. `LocalToMap()` использует floor-деление по `TileSet.TileSize`, поэтому отрицательные координаты корректно переходят в отрицательные map cells.

## Rendering

`CanvasSubmissionContext` разворачивает каждую видимую клетку в texture command:

- source rect берётся из atlas coordinates и `TextureRegionSize`;
- destination rect строится из map coordinates и `TileSet.TileSize`;
- command modulate берётся из `TileData.Modulate`;
- transform, inherited visibility, modulation, layer, z-index, y-sort и pixel snapping проходят через общий canvas pipeline.

`Enabled == false`, `Visible == false`, отсутствующий `TileSet`, отсутствующий source, отсутствующий texture или отсутствующий tile data исключают клетку из rendering.

## Serialization

`TileSet` и `TileSetAtlasSource` зарегистрированы в internal AOT-safe resource metadata registry. `ResourceObjectSerializer` сохраняет tile definitions в существующий `.e2res` документ без reflection-discovery публичных properties.

Round-trip сохраняет:

- размер клетки `TileSet.TileSize`;
- source id и atlas source type;
- `TileSetAtlasSource.TextureRegionSize`;
- atlas coordinates, alternative tile id и size in atlas;
- `TileData.Modulate`, `TextureOrigin`, `ZIndex`;
- collision polygon layer id, polygon index, points, one-way flag и one-way margin.

Ссылка на `Texture2D` остаётся runtime resource reference. Полная загрузка texture assets через import pipeline не входит в `T-0135` и должна закрываться отдельной задачей resource pipeline, если reference game потребует автоматический restore texture.

## Physics

При входе в `SceneTree` слой создаёт внутренний body RID. Этот RID возвращается в `KinematicCollision2D.GetColliderRid()` и direct query dictionaries, но не раскрывает backend-specific объект.

`CollisionEnabled == false` выключает tile collisions. Активные polygons из `TileData` участвуют в managed AABB queries и movement как статические shapes слоя. One-way flag и margin на polygon используются тем же solver path, что и `CollisionShape2D.OneWayCollision`.

Ограничения preview:

- polygon narrow phase пока не реализован, используется AABB;
- physics quadrant merge не оптимизирует shapes;
- navigation, occlusion, terrain connect и scene tiles не входят в текущий runtime baseline.
