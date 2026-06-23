# Specialized editors в `Electron2D.Editor`

Статус: документация реализации для `T-0086`.
Дата: 2026-06-23.
Связанные документы: [Specialized editors: SpriteFrames, TileMap и AnimationPlayer](../../specifications/editor/specialized-editors.md); [Electron2D.Editor project shell](editor-project-shell.md); [Референс интерфейса редактора Godot 4](../../specifications/editor/godot4-editor-reference.md).

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
- справа видны `Inspector`, `Node` и `Agent Workspace`;
- снизу видна bottom panel с вкладкой `Animation`;
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
