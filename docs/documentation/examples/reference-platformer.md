# Reference platformer

Статус: реализованный reference project для `T-0094`.
Обновлено: 2026-06-23.

## Где находится проект

Reference platformer находится в `examples/reference-platformer/` и является валидным проектом `Electron2D.Editor`, а не standalone demo folder.

Ключевые файлы:

- `Electron2D.ReferencePlatformer.csproj` - локальный .NET project, который собирается против текущего исходного проекта Electron2D;
- `project.e2d.json` - project settings с `mainScene`, display settings и Input Map;
- `export_presets.e2export.json` - presets для Windows, Linux, macOS, Android, iOS и WebAssembly browser;
- `scenes/main.scene.json` - main scene для Editor/project workflow;
- `Scripts/PlatformerGame.cs` - C# gameplay script;
- `resources/reference-platformer.manifest.json` - manifest импортированных ресурсов и их gameplay roles;
- `.electron2d/tasks/board.e2tasks` и `.electron2d/tasks/reference-platformer-acceptance.e2task` - ProjectTaskManager metadata, ожидающие человеческой приёмки.

Проект не содержит `TASKS.md`, `dev-diary/` или `completed-tasks/`: эти файлы относятся только к repository workflow и не должны попадать в пользовательские проекты.

## Что проверяет gameplay

`Scripts/PlatformerGame.cs` строит runtime scene через public Electron2D API:

- `TileSet`, `TileSetAtlasSource` и `TileMapLayer` создают platform tilemap;
- tile collision polygon помечен как one-way platform;
- `CharacterBody2D.MoveAndSlide()` используется для движения player;
- `Camera2D.MakeCurrent()` включает player camera;
- `SpriteFrames` и `AnimatedSprite2D` включают idle/walk animation;
- `AudioStreamPlayer` запускается для jump/checkpoint feedback;
- Input Map содержит keyboard/gamepad actions `move_left`, `move_right`, `jump`, `pause`;
- touch handling реализован через `InputEventScreenTouch` и `InputEventScreenDrag`;
- pause menu представлен обычным `Control`;
- progress save пишет deterministic JSON artifact с checkpoint и coin count.

Headless verification mode запускается обычным `dotnet run` и выводит subsystem markers:

```text
Reference platformer scene loaded: scenes/main.scene.json
Reference platformer subsystems: tilemap=True,oneWay=True,character=True,camera=True,animation=True,audio=True,keyboard=True,gamepad=True,touch=True,pause=True,save=True
Reference platformer progress: checkpoint=checkpoint-01,coins=1,...
```

## Ресурсы

Проект использует реальные локальные ассеты из `data/assets/reference-games/`, скопированные в `examples/reference-platformer/assets/`:

- platformer graphics и source tilemap files;
- jump, footstep и checkpoint OGG-звуки;
- общий TTF font;
- UI sprites для pause/collectible state.

`resources/reference-platformer.manifest.json` запрещает network download step и перечисляет роли ресурсов. Production package checks должны использовать project-local `assets/**`, а не внешние рабочие каталоги.

## Проверка

Основная локальная команда:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ReferencePlatformer.ps1
```

Verifier выполняет:

- проверку обязательных project files;
- проверку отсутствия repository workflow files внутри примера;
- чтение project settings, Input Map, export presets и ProjectTaskManager documents;
- проверку resource manifest, локальных путей и базовых сигнатур PNG/OGG/TTF;
- `dotnet build examples/reference-platformer/Electron2D.ReferencePlatformer.csproj`;
- `dotnet run --project examples/reference-platformer/Electron2D.ReferencePlatformer.csproj --no-build -- --verify`;
- `e2d validate --project examples/reference-platformer --format json`;
- `e2d export build-web --project examples/reference-platformer --skip-publish true --format json`;
- проверку, что WebAssembly package output не содержит `.electron2d/tasks/**`.

Focused automated test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ReferencePlatformerProjectTests"
```

## Текущие границы

`T-0094` закрывает наличие законченного platformer project и локального verifier-а. Она не закрывает:

- сборку обеих reference games на всех целевых платформах (`T-0096`);
- Tier 1 smoke/soak на reference projects (`T-0093`);
- performance gate на reference games (`T-0102`);
- leak verification и полный release candidate gate (`T-0103`, `T-0104`).

Эти задачи должны использовать `examples/reference-platformer/` как настоящий Editor project и не заменять его standalone fixture-ом.
