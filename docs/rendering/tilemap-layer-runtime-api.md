# TileMapLayer runtime API

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0135`.
Обновлено: 2026-06-22.

## Цель

`0.1.0 Preview` должен предоставить минимальный runtime API для tilemap-слоя, достаточный для Platformer и других 2D-примеров без временных обходных решений в `examples/`.

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
- `docs/rendering/tilemap-layer-runtime-api.md` описывает фактическую реализацию.
- Public API baseline, API Wiki и compatibility verifier обновлены после реализации.

## Фактическое состояние, ограничения и проверки

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
