# TileMapLayer runtime API

Статус: целевая спецификация.
Задача: `T-0135`.
Обновлено: 2026-06-22.

## Цель

`0.1.0 Preview` должен предоставить минимальный runtime API для tilemap-слоя, достаточный для reference platformer и других 2D-примеров без временных обходных решений в `examples/`.

Контракт этой задачи закрывает только runtime-поверхность:

- хранение tile definitions в `TileSet`;
- atlas source для texture regions;
- tile data с collision polygon metadata;
- `TileMapLayer` как `Node2D`-узел;
- canvas submission клеток в существующую render queue;
- участие tilemap collision в `World2D`, direct queries и движении `CharacterBody2D`/`RigidBody2D`;
- one-way collision для платформ;
- переносимый `.e2res` round-trip для `TileSet` и atlas tile metadata через AOT-safe resource metadata.

Редактор палитры, terrain painting, navigation, occlusion, scene tiles, pattern API, чанкинг и оптимизация merge-полигонов остаются отдельными задачами.

## Публичная поверхность

Новые типы:

- `TileSet : Resource`;
- `TileSetSource : Resource`;
- `TileSetAtlasSource : TileSetSource`;
- `TileData : Object`;
- `TileMapLayer : Node2D`.

`TileSet` хранит `TileSize` и source registry. `AddSource()` возвращает stable source ID; `GetSource()`, `HasSource()`, `RemoveSource()`, `GetSourceCount()`, `GetSourceId()` и `Clear()` работают без reflection и без внешнего asset pipeline.

`TileSetAtlasSource` хранит `Texture`, `TextureRegionSize` и tile entries. `CreateTile()` создаёт tile data для atlas coordinates; `GetTileData()` возвращает `TileData` или `null`; `GetTileTextureRegion()` возвращает source rectangle в texture pixels.

`TileData` хранит per-tile metadata, нужную runtime-слою:

- `Modulate`;
- `TextureOrigin`;
- `ZIndex`;
- collision polygons по layer id;
- one-way флаг и margin на polygon.

`TileMapLayer` хранит одну карту клеток:

- `TileSet`;
- `Enabled`;
- `CollisionEnabled`;
- `RenderingQuadrantSize`;
- `PhysicsQuadrantSize`;
- `XDrawOrderReversed`;
- `YSortOrigin`;
- `SetCell()`;
- `EraseCell()`;
- `Clear()`;
- `GetCellSourceId()`;
- `GetCellAtlasCoords()`;
- `GetCellAlternativeTile()`;
- `GetCellTileData()`;
- `GetUsedCells()`;
- `GetUsedCellsById()`;
- `GetUsedRect()`;
- `LocalToMap()`;
- `MapToLocal()`;
- `UpdateInternals()`;
- `NotifyRuntimeTileDataUpdate()`.

`TileMapLayer` не должен наследоваться от physics body. Участие в физике является внутренним runtime-поведением слоя.

## Serialization behavior

`TileSet` и `TileSetAtlasSource` должны регистрироваться в internal AOT-safe resource metadata registry. Сериализация не должна искать публичные properties через reflection и не должна добавлять новый публичный serializer API.

Минимальный переносимый snapshot хранит:

- `TileSet.TileSize`;
- source id и source type;
- `TileSetAtlasSource.TextureRegionSize`;
- atlas coordinates, alternative tile id и size in atlas;
- `TileData.Modulate`, `TextureOrigin`, `ZIndex`;
- collision polygon layer id, polygon index, points, one-way flag и one-way margin.

Texture loading/import остаётся отдельным resource pipeline: runtime API хранит ссылку на `Texture2D`, а этот task фиксирует переносимость tile definitions и collision/render metadata.

## Rendering behavior

`CanvasSubmissionContext` должен превращать видимые клетки `TileMapLayer` в обычные texture render commands:

- source texture берётся из `TileSetAtlasSource.Texture`;
- source rectangle вычисляется через `TextureRegionSize` и atlas coordinates;
- destination rectangle вычисляется через map coordinates и `TileSet.TileSize`;
- `TileData.Modulate` умножается как command modulate;
- layer visibility, `CanvasItem.Modulate`, `SelfModulate`, `ZIndex`, canvas layer transform, camera transform и pixel snapping применяются тем же путём, что и для других canvas commands;
- `Enabled == false` или `Visible == false` исключает rendering commands.

## Physics behavior

`TileMapLayer` получает внутренний body RID при входе в `SceneTree` и освобождает его при выходе или `Free()`. RID используется только для query/collision результатов.

Активная collision cell существует, если:

- `TileMapLayer.Enabled == true`;
- `TileMapLayer.CollisionEnabled == true`;
- cell ссылается на существующий `TileSetAtlasSource`;
- у tile data есть collision polygon с минимум тремя точками.

На `0.1.0 Preview` polygon участвует как AABB bounds. Это должно быть явно отражено в документации реализации. One-way collision работает по существующим правилам managed AABB movement: движение сверху вниз блокируется, движение снизу вверх пропускается.

## Acceptance criteria

- Добавлены tests для `TileSet`, `TileSetAtlasSource`, `TileData` и `TileMapLayer`.
- `TileMapLayer` отдаёт render commands с правильными texture, source rect, destination rect и transform.
- `TileMapLayer` участвует в `CharacterBody2D.MoveAndSlide()` и сообщает себя как collider.
- One-way tile collision блокирует падение сверху и пропускает движение снизу.
- `TileSet` с atlas tiles и collision metadata проходит `.e2res` serialize/deserialize/instantiate round-trip.
- Все новые публичные типы и члены имеют полноценную XML-документацию.
- `docs/documentation/rendering/tilemap-layer-runtime-api.md` описывает фактическую реализацию.
- Public API baseline, API Wiki и compatibility verifier обновлены после реализации.
