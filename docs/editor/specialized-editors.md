# Specialized editors: SpriteFrames, TileMap и AnimationPlayer

Обновлено: 2026-06-24.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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
- UI соответствует layout-референсу: `2D` workspace остаётся выбранным, specialized editors находятся в центральной области, `Inspector`, `FileSystem`, `Scene`, bottom `Animation` tab и bottom `Agent` tab видимы как части общего редактора.

## Не входит в scope

- Visual graph state machine, blend tree, skeletal animation и секции timeline.
- Полный tile collision editor; smoke проверяет только видимость collision summary, если данные есть в resource document.
- Импорт реальных PNG atlas files через FileSystem dock. Smoke может использовать runtime resource references и synthetic texture metadata, потому что задача проверяет editor workflow и сохранение текстовых runtime documents.
- Запуск игры и визуальная проверка playback. Runtime playback покрыт отдельными задачами `SpriteFrames`, `TileMapLayer` и `AnimationPlayer`.

## Фактическое состояние, ограничения и проверки

Статус: документация реализации для `T-0086`.
Дата: 2026-06-23.
Связанные документы: [Specialized editors: SpriteFrames, TileMap и AnimationPlayer](specialized-editors.md); [Electron2D.Editor project shell](editor-project-shell.md); [Референс интерфейса редактора Godot 4](godot4-editor-reference.md).

## Назначение

Specialized editors закрывают текущий preview-level workflow для трёх 2D-ресурсов, которые часто нужны при сборке небольшой игры:

- `SpriteFrames` для `AnimatedSprite2D`;
- `TileSet` и `TileMapLayer` для палитры и раскладки клеток;
- базовая timeline-модель `AnimationPlayer`.

Текущая реализация проверяется через executable smoke-команду. Она не добавляет новый публичный runtime API и не вводит editor-only формат хранения: файлы проекта записываются как runtime text documents, то есть как обычные текстовые resource/scene документы движка.

## Smoke-команда

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --specialized-editors-smoke .temp\specialized-editors-smoke
```

Команда создаёт валидный проект из canonical template через Project Manager, открывает его, создаёт ресурсы и сцену, затем показывает specialized editors frame в настоящем окне `Electron2D.Editor`.

Создаваемые project-relative файлы:

| Файл | Формат | Назначение |
| --- | --- | --- |
| `resources/player_frames.e2res` | `Electron2D.SerializedResource` | `SpriteFrames` animations `idle` и `run`, fps, loop mode, frame durations и texture references. |
| `resources/terrain_tileset.e2res` | `Electron2D.SerializedResource` | `TileSet` с atlas source, tile size, texture region size и palette tiles. |
| `resources/player_motion.e2res` | `Electron2D.SerializedResource` | `Animation` с length, loop mode, value track `Player:position:x` и method track `OnStep`. |
| `scenes/specialized-editors.scene.json` | `Electron2D.SceneFile` | Scene nodes `AnimatedSprite2D`, `TileMapLayer` и `AnimationPlayer` со ссылками на созданные ресурсы. |

Перед показом UI smoke перечитывает созданные файлы через `SerializedResourceTextSerializer` и `SceneFileTextSerializer`, затем повторно сериализует документы. Это подтверждает, что редактор сохраняет данные в стабильных runtime formats и может открыть их снова без потери порядка или значений.

## Видимый UI

Specialized editors отображаются внутри общего shell layout:

- выбран `2D` workspace;
- слева видны `Scene` и `FileSystem`;
- справа видны `Inspector` и `Node`;
- снизу видна bottom panel с вкладками `Animation` и `Agent`;
- в центральной области одновременно показаны panels `SpriteFrames`, `TileMap` и `AnimationPlayer`.

`SpriteFrames` panel показывает animations, frame list, fps, loop mode, selected texture и действия `Add`, `Remove`, `Reorder`.

`TileMap` panel показывает tileset source, used rect, selected tile, palette grid и действия `Brush`, `Erase`, `Paint`.

`AnimationPlayer` panel показывает animation name, length, loop mode, playhead, tracks, keyframes и действия `Add Key`, `Play`, `Loop`.

Smoke сохраняет:

- `.temp/specialized-editors-smoke/visual/specialized-editors-ui.png`;
- `.temp/specialized-editors-smoke/visual/specialized-editors-ui.analysis.json`.

PNG является frame, отправленным в созданное desktop-окно. JSON analysis фиксирует `WindowCreated`, `WindowShown`, `FramePresented`, pointer interaction по palette tile, keyboard save command, количество clickable controls, отсутствие text overflow и отсутствие запрещённых `3D`, `AssetLib`, GDScript и `.gd` элементов.

## Ограничения

- Текущий workflow является preview-level smoke surface: он проверяет сохранение и видимую структуру specialized editors, но не является полной ручной панелью редактирования всех свойств.
- Sprite texture references сохраняются как external resource references в `Electron2D.SerializedResource`; полноценный resolver импортированных texture assets не расширяется этой задачей.
- Timeline покрывает только value track и method track. Blend tree, state machine, audio tracks, skeletal animation, секции timeline и capture mode не входят в `0.1.0 Preview`.
- TileMap editor показывает palette/cell workflow и сохраняет cells; полный collision polygon editor не входит в scope этой задачи.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorSpecializedEditorsTests"
```

Ручная visual acceptance после smoke:

1. Открыть `.temp\specialized-editors-smoke\visual\specialized-editors-ui.png` или PNG из выбранного work root.
2. Проверить, что три panels размещены в центральной области, текст не выходит за bounds, selected `2D` workspace, docks и bottom panel видимы.
3. Проверить отсутствие `3D`, `AssetLib`, GDScript и `.gd` UI.

Regression slice для связанных runtime formats:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorSpecializedEditorsTests|FullyQualifiedName~SpriteFramesAnimatedSprite2DTests|FullyQualifiedName~TileMapLayerRuntimeTests|FullyQualifiedName~AnimationPlayerTracksTests|FullyQualifiedName~ResourceFileSerializationTests|FullyQualifiedName~SceneResourceSerializationTests"
```
