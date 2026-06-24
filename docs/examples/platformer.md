# Platformer 0.1.0 Preview

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0094`.

## Цель

`examples/platformer/` должен быть законченной приёмочной игрой `0.1.0 Preview`, а не standalone demo, fixture или набором runtime files. Проект должен открываться и проверяться тем же форматом, который использует `Electron2D.Editor`: `Platformer.e2d`, `.csproj`, main scene, project settings, Input Map, export presets, ресурсы, C# scripts и `.electron2d/tasks/**`.

Игра проверяет небольшой 2D platformer loop: игрок двигается по tilemap-уровню, прыгает на one-way platform, камера следует за игроком, анимация меняет кадры, звук запускается на игровых событиях, gamepad/keyboard/touch input приводят к одному action flow, pause menu останавливает gameplay, а progress save фиксирует checkpoint и собранные предметы.

Проект должен быть playable через API движка и project runtime host. Обычный запуск `e2d run --project examples/platformer` открывает графическое окно Electron2D с видимым platformer-интерфейсом и управлением через Input Map; консольный псевдо-рендер не считается игрой. Игровые скрипты используют только публичные `Node`/`Node2D`/rendering/input API. Код проекта не должен напрямую вызывать платформенные или оконные библиотеки: создание окна, event loop, dispatch input events, рендер кадра и screenshot выполняет runner движка, который не добавляет out-of-profile public bootstrap type вроде `Electron2DApplication`.

Координатный контракт примера совпадает с 2D API движка: положительный `Y` направлен вниз, `Vector2.Up` равен `(0, -1)`, а `CharacterBody2D.Velocity` задаёт скорость в единицах сцены за секунду. Поэтому platformer использует положительную gravity и отрицательную jump velocity; это не считается обходом или несовместимостью.

Для автоматической проверки без ручного ввода проект поддерживает `--play-script <commands>`, где команды проходят тот же gameplay state path и выводят machine-readable строки `Mode=playable`, `Playable=True`, `FramesAdvanced`, `CommandsApplied`, `PlayerPosition`, `Checkpoint`, `Coins`, `Paused`, `SavePath`, `WindowCreated`, `WindowShown`, `FramePresented`, `InputEventsDispatched`, `DrawCommands`, `ScreenshotPath`. Screenshot должен быть PNG-артефактом кадра игры, построенного через scene tree и canvas/UI API Electron2D, а не текстовым логом.

## Project Layout

Обязательные файлы:

- `examples/platformer/Platformer.csproj`;
- `examples/platformer/scripts/PlatformerGame.cs`;
- `examples/platformer/Platformer.e2d`;
- `examples/platformer/global.json`;
- `examples/platformer/scenes/main.scene.json`;
- `examples/platformer/resources/platformer.manifest.json`;
- `examples/platformer/.electron2d/tasks/board.e2tasks`;
- `examples/platformer/.electron2d/tasks/platformer-acceptance.e2task`.

В корне проекта не должно быть `bin/`, `obj/`, `project.e2d.json`, `electron2d.lock.json`, `export_presets.e2export.json` или `Scripts/`. `bin/` и `obj/` являются сборочными артефактами; отдельные lock/export JSON заменены встроенными разделами главного файла проекта.

`Platformer.e2d` должен:

- использовать `format = "Electron2D.ProjectSettings"` и `formatVersion = 1`;
- иметь `name = "Platformer"`;
- ссылаться на `scenes/main.scene.json` как `mainScene`;
- задавать actions `move_left`, `move_right`, `jump`, `pause` с keyboard и gamepad bindings;
- задавать landscape display settings.
- содержать встроенный раздел `exportPresets` с presets для `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64` и `WebAssemblyBrowser`;
- содержать встроенный раздел `reproducibilityLock` с engine, .NET SDK, package/importer и export template версиями.

Встроенный `exportPresets` является проверяемым input обычного export workflow; реальные platform smoke, soak и production package checks закрываются задачами `T-0096`, `T-0093` и release gate.

`scenes/main.scene.json` должен хранить дерево нод игры: world nodes, player/camera nodes, HUD, pause menu и interactive UI controls. HUD и pause menu должны находиться в `CanvasLayer`, чтобы оставаться в координатах viewport при движении `Camera2D`. Сцена также хранит ресурсы и настройки этих нод: texture paths, font resources, audio stream metadata, `TileSet`/tile cells, `CollisionShape2D.Shape`, `AnimatedSprite2D.SpriteFrames`, theme colors, labels и button state. Скрипт `scripts/PlatformerGame.cs` не должен собирать это дерево через `new Label`, `new Button`, `new TextureRect`, `new Sprite2D`, `new Panel` или `AddChild(...)`; не должен грузить project assets через `ImageTexture.LoadFromFile(...)`; не должен хранить hardcoded scene paths вроде `World/Player` или `Hud/HudPanel/StatusLabel`. Сцена должна задавать exported typed references на нужные ноды, а скрипт может подключать сигналы и выполнять gameplay behavior.

## Gameplay Contract

C# script должен использовать scene-owned node tree через публичный API Electron2D. Main scene должна сериализовать:

- `TileSet`, `TileSetAtlasSource` и `TileMapLayer` с collision polygons;
- обычный collision floor для стартового положения player;
- at least one one-way tile collision polygon;
- `CollisionShape2D.Shape` для player collider;
- texture resources для world/UI sprites;
- `SpriteFrames` для player animation;
- `AudioStream` resources для jump/checkpoint feedback;
- font resources и theme overrides для HUD/pause controls.

C# script должен явно использовать:

- `CharacterBody2D.MoveAndSlide()` для player movement;
- `Camera2D.MakeCurrent()` и player-follow state;
- `AnimatedSprite2D` для idle/walk animation;
- `AudioStreamPlayer` или `AudioStreamPlayer2D` для jump/checkpoint feedback;
- `Input.IsActionJustPressed(...)`, `InputMap.HasAction(...)`, horizontal axis через action strength или специализированный axis helper без включения `jump`/`pause` в вертикальную ось movement vector, gamepad bindings из Input Map и touch events через `InputEventScreenTouch`/`InputEventScreenDrag`;
- pause state через `GetTree().Paused`, `PauseMenu.Visible` и `ProcessMode.WhenPaused` для menu subtree;
- deterministic save progress file under user data или verifier-supplied output path.

Script-class форма должна быть `partial`, чтобы файл оставался совместимым с выбранным C# script-class contract. Пример может быть обычным .NET project в Electron2D, но сам gameplay class не должен полагаться на форму, которая заведомо не переносится в source-generator based script pipeline.

Touch handling должен трактовать `InputEventScreenTouch.Position` как viewport coordinates. Если backend в текущем preview отдаёт нормализованные значения `0..1`, script обязан нормализовать это явно и тем самым сохранить одинаковую логику для pixel coordinates и preview backend coordinates. Для drag-направления используется `InputEventScreenDrag.ScreenRelative`, потому что это unscaled delta.

Прыжок должен применяться только при floor state из `CharacterBody2D.IsOnFloor()` или при эквивалентном состоянии, полученном предыдущим `MoveAndSlide()` в physics frame. Script не должен выдавать бесконечные air jumps за счёт безусловного сброса `Velocity.Y`.

Gameplay script не должен имитировать пол прямой записью `Player.Position` после `MoveAndSlide()`. Вертикальная остановка, `IsOnFloor()` и one-way behavior должны быть следствием collision state, а не ручного прижатия к `Y = 0`.
Стартовая опора игрока должна быть обычным collision tile из `main.scene.json`; one-way tile остаётся отдельным проверяемым элементом уровня. Если collision tilemap используется только для физики, он должен быть скрыт от отрисовки, чтобы acceptance screenshot показывал игровую сцену без служебных обрезанных тайлов.

Playable entrypoint должен использовать только публичный API Electron2D:

- `SceneTree`, `Node`, `Node2D`, `CanvasItem`, `InputMap`, `Input`, `InputEvent*` и игровые node/script-типы;
- публичный runtime window host Electron2D для открытия окна, обработки событий, продвижения кадров, построения draw commands, показа кадра и сохранения screenshot;
- никакого прямого использования backend/window/input библиотек из проекта игры, `Console.ReadKey`, ASCII frame output или custom window loop в `examples/platformer/`.

Проект может использовать curated assets из `data/assets/reference-games/`, но не должен ссылаться на сетевые download steps, placeholder files, ignored local workflow files, `TASKS.md`, `dev-diary/` или `completed-tasks/`.

## Verification

Нужен локальный verifier `tools/Verify-Platformer.ps1`, который:

- проверяет обязательные файлы и отсутствие запрещённых workflow files внутри примера;
- читает `Platformer.e2d` через project settings loader;
- читает встроенный `exportPresets` через export preset store;
- читает встроенный `reproducibilityLock` через reproducibility verifier;
- проверяет `.electron2d/tasks/**` как first-class Editor metadata;
- проверяет, что `resources/platformer.manifest.json` ссылается только на существующие локальные assets;
- собирает `Platformer.csproj`;
- запускает headless verification mode и проверяет output-маркеры gameplay subsystems;
- запускает playable script mode через `e2d run --project examples/platformer --play-script "right,jump,right,pause,save,quit" --screenshot <artifact.png>` и проверяет, что пользовательский игровой loop реально принимает команды, меняет состояние, открывает окно через project runtime host Electron2D, рендерит кадры, сохраняет PNG screenshot и пишет progress save;
- запускает `e2d validate --project examples/platformer --format json` как preview validation route;
- проверяет, что package-planning/verifier steps не копируют `.electron2d/tasks/**` в runtime asset list.

Headless verification не должен считать подсистему готовой только по наличию object references:

- `OneWayPlatformReady` подтверждается через `TileMapLayer.GetUsedCells()`, `GetCellTileData(...)`, `TileData.GetCollisionPolygonsCount(...)` и `TileData.IsCollisionPolygonOneWay(...)`;
- `InputReady` проверяется через `InputMap.HasAction(...)`, а не через строковый поиск в списке actions;
- keyboard bindings проверяются отдельно от наличия action names;
- gamepad bindings принимают `InputEventJoypadButton` и `InputEventJoypadMotion`, чтобы analog stick не считался ошибкой;
- save marker становится `true` только после записи и обратного чтения JSON payload с ожидаемыми `checkpointId` и `coins`;
- animation marker должен доказывать активный `AnimatedSprite2D` и запуск `AnimationPlayer`, если timeline node присутствует;
- touch marker должен проходить через координаты, похожие на реальные viewport pixels, а не только через нормализованные тестовые значения.
- pause marker должен подтверждать `SceneTree.Paused`, а не только `PauseMenu.Visible`;
- screenshot-gate должен проверять, что HUD/pause overlay находятся в screen-space, а acceptance screenshots не содержат случайно обрезанных декоративных объектов на границах viewport.

Focused automated test должен падать до появления проекта/verifier-а и проходить после реализации. Документация реализации должна описать текущие ограничения: настоящий Tier 1 smoke/soak, performance gate и сборка обеих reference games на пяти платформах остаются в `T-0093`, `T-0096` и `T-0102`.

## Фактическое состояние, ограничения и проверки

Статус: реализованный reference project для `T-0094`.
Обновлено: 2026-06-23.

## Где находится проект

Platformer находится в `examples/platformer/` и является валидным проектом `Electron2D.Editor`, а не standalone demo folder.

Ключевые файлы:

- `Platformer.csproj` - локальный .NET project, который собирается против текущего исходного проекта Electron2D;
- `Platformer.e2d` - project settings с `mainScene`, display settings, Input Map, встроенными export presets и встроенным reproducibility lock; воспроизводимый lock означает список версий SDK, engine package и import/export metadata, который нужен для повторяемой сборки;
- `scenes/main.scene.json` - main scene для Editor/project workflow;
- `scripts/PlatformerGame.cs` - C# gameplay script;
- `resources/platformer.manifest.json` - manifest импортированных ресурсов и их gameplay roles;
- `.electron2d/tasks/board.e2tasks` и `.electron2d/tasks/platformer-acceptance.e2task` - ProjectTaskManager metadata, ожидающие человеческой приёмки.

Проект не содержит `TASKS.md`, `dev-diary/` или `completed-tasks/`: эти файлы относятся только к repository workflow и не должны попадать в пользовательские проекты.

## Что проверяет gameplay

`scenes/main.scene.json` хранит runtime scene как данные проекта:

- tree нод world/player/camera/HUD/pause menu;
- texture, font и audio resource references;
- `TileSet`, tile cells, обычный collision floor для стартового положения player и отдельный one-way collision polygon;
- `CollisionShape2D.Shape` для player collider;
- `SpriteFrames` для idle/walk animation;
- theme colors, labels и button state для HUD/pause controls;
- exported typed script references, которые связывают C# gameplay code с конкретными нодами сцены.

`scripts/PlatformerGame.cs` не строит visual tree и не грузит ассеты вручную. Он получает готовые typed references из scene data, затем выполняет gameplay:

- `CharacterBody2D.MoveAndSlide()` используется для движения player;
- `Camera2D.MakeCurrent()` включает player camera;
- `AnimatedSprite2D` включает idle/walk animation;
- `AudioStreamPlayer` запускается для jump/checkpoint feedback;
- Input Map содержит keyboard/gamepad actions `move_left`, `move_right`, `jump`, `pause`;
- horizontal movement читает action strength для `move_left`/`move_right`, а `jump` и `pause` не используются как вертикальная ось movement vector;
- touch handling реализован через `InputEventScreenTouch` и `InputEventScreenDrag`: pixel coordinates нормализуются через viewport size, а drag-направление берётся из `ScreenRelative`;
- HUD и pause menu находятся в `CanvasLayer`, поэтому остаются в координатах viewport при движении `Camera2D`;
- pause menu включает `ProcessMode.WhenPaused`, а script переключает `SceneTree.Paused`, чтобы gameplay callbacks останавливались через дерево сцены;
- player удерживается на полу через tile collision и `CharacterBody2D.MoveAndSlide()`, без ручной записи `Player.Position` после физического шага;
- служебный `PlatformTileMap` скрыт от отрисовки, но его collision остаётся активным; видимую землю и платформы задают отдельные scene nodes;
- progress save пишет deterministic JSON artifact с checkpoint и coin count и подтверждает результат обратным чтением файла.

Координатная система примера совпадает с runtime 2D API: положительный `Y` направлен вниз, `Vector2.Up` равен `(0, -1)`, gravity положительная, а jump velocity отрицательная. Это ожидаемое поведение для platformer script в текущем контракте движка.

Headless subsystem markers теперь строже, чем простая проверка object references. `OneWayPlatformReady` читает used cells из `TileMapLayer`, получает `TileData` каждой клетки и ищет one-way collision polygon. `InputReady` использует `InputMap.HasAction()` и наличие keyboard bindings, а `GamepadBindingsReady` принимает и button bindings, и analog stick bindings через `InputEventJoypadMotion`.

Headless verification mode запускается обычным `dotnet run` и выводит subsystem markers:

```text
Platformer scene loaded: scenes/main.scene.json
Platformer subsystems: tilemap=True,oneWay=True,character=True,camera=True,animation=True,audio=True,keyboard=True,gamepad=True,touch=True,pause=True,save=True
Platformer progress: checkpoint=checkpoint-01,coins=1,...
```

Playable mode запускается без `--verify`:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- run --project examples\platformer
```

Интерактивный режим показывает видимое текстовое состояние platformer loop и принимает команды `A`/Left, `D`/Right, Space, `P`, `S`, `Q`. Для автоматической проверки того же loop используется:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- run --project examples\platformer --play-script "right,jump,right,pause,save,quit"
```

`--play-script` выводит `Mode=playable`, `Playable=True`, количество обработанных команд, позицию игрока, pause state, checkpoint, coins, save path и screenshot path. Это не заменяет будущий platform smoke/soak из `T-0093`: режим остаётся коротким acceptance hook для reference project, но он больше не должен засчитывать one-way platform, input map или save progress только по наличию объектов.

## Ресурсы

Проект использует реальные локальные ассеты из `data/assets/reference-games/`, скопированные в `examples/platformer/assets/`:

- platformer graphics и source tilemap files;
- jump, footstep и checkpoint OGG-звуки;
- общий TTF font;
- UI sprites для pause/collectible state.

`resources/platformer.manifest.json` запрещает network download step и перечисляет роли ресурсов. Production package checks должны использовать project-local `assets/**`, а не внешние рабочие каталоги.

## Проверка

Основная локальная команда:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-Platformer.ps1
```

Verifier выполняет:

- проверку обязательных project files;
- проверку отсутствия repository workflow files внутри примера;
- чтение `Platformer.e2d`, Input Map, embedded export presets и ProjectTaskManager documents;
- проверку resource manifest, локальных путей и базовых сигнатур PNG/OGG/TTF;
- `dotnet build examples/platformer/Platformer.csproj`;
- `dotnet run --project src/Electron2D.Cli/Electron2D.Cli.csproj -- run --project examples/platformer --play-script "save,quit"`;
- `dotnet run --project src/Electron2D.Cli/Electron2D.Cli.csproj -- run --project examples/platformer --play-script "right,jump,right,pause,save,quit"`;
- `e2d validate --project examples/platformer --format json`;
- `e2d export build-web --project examples/platformer --skip-publish true --format json`;
- проверку, что WebAssembly package output не содержит `.electron2d/tasks/**`.

Focused automated test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~PlatformerProjectTests"
```

## Текущие границы

`T-0094` закрывает наличие законченного platformer project и локального verifier-а. Общая матрица `T-0096` проверяет, что этот проект использует полный набор export targets из обычной структуры проекта. Эти задачи не закрывают:

- Tier 1 smoke/soak на reference projects (`T-0093`);
- performance gate на reference games (`T-0102`);
- leak verification и полный release candidate gate (`T-0103`, `T-0104`).

Эти задачи должны использовать `examples/platformer/` как настоящий Editor project и не заменять его standalone fixture-ом.
