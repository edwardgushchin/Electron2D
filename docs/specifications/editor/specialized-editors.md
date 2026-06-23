# Specialized editors: SpriteFrames, TileMap и AnimationPlayer

Статус: целевая спецификация для `T-0086` и `0.1.0 Preview`.
Дата: 2026-06-23.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Референс интерфейса редактора Godot 4](godot4-editor-reference.md).

## Цель

`Electron2D.Editor` должен позволять настраивать три часто используемых 2D-ресурса без выхода из редактора:

- `SpriteFrames` для `AnimatedSprite2D`;
- `TileSet` и `TileMapLayer` для палитры тайлов и раскладки клеток;
- базовую timeline-модель `AnimationPlayer` для value tracks, то есть дорожек значений свойств, и method tracks, то есть дорожек вызова методов.

Эта задача не вводит новый runtime API. Редактор должен работать с уже существующими текстовыми форматами runtime resources и scene files, чтобы данные можно было сохранить, перечитать и использовать в проекте без отдельного editor-only формата.

## Обязательный workflow

Smoke-сценарий `--specialized-editors-smoke <work-root>` должен:

1. Создать валидный проект из canonical template через Project Manager.
2. Создать project-relative файлы:
   - `resources/player_frames.e2res` с документом `Electron2D.SerializedResource` для `Electron2D.SpriteFrames`;
   - `resources/terrain_tileset.e2res` с документом `Electron2D.SerializedResource` для `Electron2D.TileSet`;
   - `resources/player_motion.e2res` с документом `Electron2D.SerializedResource` для `Electron2D.Animation`;
   - `scenes/specialized-editors.scene.json` с документом `Electron2D.SceneFile`, где используются `AnimatedSprite2D`, `TileMapLayer` и `AnimationPlayer`.
3. Перечитать все созданные resource/scene documents теми же runtime text serializers, которые используются для игровых файлов.
4. Проверить round-trip: повторная сериализация перечитанных документов не теряет данные и сохраняет стабильный порядок.
5. Показать в настоящем окне `Electron2D.Editor` центральный specialized editors workspace со следующими областями:
   - `SpriteFrames`: список animations, frame list, fps, loop mode, выбранный texture reference, действия add/remove/reorder;
   - `TileMap`: tileset source, palette grid, selected tile, brush/erase/paint, grid координаты и used rect;
   - `AnimationPlayer`: список animations, timeline, tracks, keyframes, length, loop mode, playhead и track target path.
6. Сохранить PNG screenshot и JSON analysis artifact.

## Acceptance Criteria

- Smoke-команда завершается с exit code `0` и строкой `Electron2D.Editor specialized editors smoke passed`.
- Созданный каталог проекта содержит `project.e2d.json`, `resources/player_frames.e2res`, `resources/terrain_tileset.e2res`, `resources/player_motion.e2res` и `scenes/specialized-editors.scene.json`.
- `SpriteFrames` resource document содержит animations `idle` и `run`, frame durations, fps и loop mode; после reload значения сохраняются.
- `TileSet` resource document содержит atlas source, tile size и palette tiles; scene file содержит `TileMapLayer` cells, которые после reload сохраняют `sourceId`, `atlasCoords` и `usedRect`.
- `Animation` resource document содержит length, loop mode, value track и method track; после reload сохраняются track target paths, key times и key values.
- Scene file использует runtime node type names `Electron2D.AnimatedSprite2D`, `Electron2D.TileMapLayer` и `Electron2D.AnimationPlayer`.
- Screenshot настоящего окна фиксирует три specialized editor panels, shell layout, docks и bottom panel.
- Analysis artifact фиксирует `WindowCreated=True`, `WindowShown=True`, `FramePresented=True`, pointer interaction по palette tile, keyboard save command, `TextOverflowCount=0`, отсутствие `3D`, `AssetLib`, GDScript и `.gd` UI.
- UI соответствует layout-референсу: `2D` workspace остаётся выбранным, specialized editors находятся в центральной области, `Inspector`, `FileSystem`, `Scene` и bottom `Animation` tab видимы как части общего редактора.

## Не входит в scope

- Visual graph state machine, blend tree, skeletal animation и секции timeline.
- Полный tile collision editor; smoke проверяет только видимость collision summary, если данные есть в resource document.
- Импорт реальных PNG atlas files через FileSystem dock. Smoke может использовать runtime resource references и synthetic texture metadata, потому что задача проверяет editor workflow и сохранение текстовых runtime documents.
- Запуск игры и визуальная проверка playback. Runtime playback покрыт отдельными задачами `SpriteFrames`, `TileMapLayer` и `AnimationPlayer`.
