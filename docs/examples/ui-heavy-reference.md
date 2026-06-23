# UI-heavy reference game 0.1.0 Preview

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0095`.

## Цель

`examples/ui-heavy-reference/` должен быть второй законченной приёмочной игрой `0.1.0 Preview`, а не standalone demo, fixture или набором runtime files. Проект должен открываться и проверяться тем же форматом, который использует `Electron2D.Editor`: `project.e2d.json`, `.csproj`, main scene, project settings, Input Map, export presets, ресурсы, C# scripts и `.electron2d/tasks/**`.

Игра должна быть небольшой карточной головоломкой с полным UI-loop: главное меню, игровое поле с карточками, список целей, переключение локали, touch/mouse/keyboard actions, текст, сохранение прогресса, переход между menu/game/result scenes и renderer profile `Compatibility` для Android preset.

Проект должен быть playable через API движка и project runtime host. Обычный запуск `dotnet run --project examples/ui-heavy-reference/Electron2D.UiHeavyReference.csproj` открывает графическое окно Electron2D с видимым меню, игровым полем и result screen; консольный псевдо-рендер не считается игрой. Игровые скрипты используют только публичные `Control`/resource/input API. Код проекта не должен напрямую вызывать платформенные или оконные библиотеки: создание окна, event loop, dispatch input events, рендер кадра и screenshot выполняет runner движка, который не добавляет out-of-profile public bootstrap type вроде `Electron2DApplication`.

Для автоматической проверки без ручного ввода проект поддерживает `--play-script <commands>`, где команды проходят тот же UI/gameplay state path и выводят machine-readable строки `Mode=playable`, `Playable=True`, `FramesAdvanced`, `CommandsApplied`, `Scene`, `Locale`, `Score`, `Moves`, `SelectedCard`, `SavePath`, `WindowCreated`, `WindowShown`, `FramePresented`, `InputEventsDispatched`, `DrawCommands`, `ScreenshotPath`. Screenshot должен быть PNG-артефактом кадра игры, построенного через scene tree и canvas/UI API Electron2D, а не текстовым логом.

## Project Layout

Обязательные файлы:

- `examples/ui-heavy-reference/Electron2D.UiHeavyReference.csproj`;
- `examples/ui-heavy-reference/Program.cs`;
- `examples/ui-heavy-reference/Scripts/CardPuzzleGame.cs`;
- `examples/ui-heavy-reference/project.e2d.json`;
- `examples/ui-heavy-reference/electron2d.lock.json`;
- `examples/ui-heavy-reference/global.json`;
- `examples/ui-heavy-reference/export_presets.e2export.json`;
- `examples/ui-heavy-reference/scenes/menu.scene.json`;
- `examples/ui-heavy-reference/scenes/game.scene.json`;
- `examples/ui-heavy-reference/scenes/result.scene.json`;
- `examples/ui-heavy-reference/resources/ui-heavy-reference.manifest.json`;
- `examples/ui-heavy-reference/.electron2d/tasks/board.e2tasks`;
- `examples/ui-heavy-reference/.electron2d/tasks/ui-heavy-acceptance.e2task`.

`project.e2d.json` должен:

- использовать `format = "Electron2D.ProjectSettings"` и `formatVersion = 1`;
- иметь `name = "Electron2D.UiHeavyReference"`;
- ссылаться на `scenes/menu.scene.json` как `mainScene`;
- задавать actions `accept`, `cancel`, `next_card`, `previous_card`, `switch_locale` с keyboard, pointer/touch-compatible и gamepad bindings там, где формат Input Map это поддерживает;
- задавать portrait-capable display settings с безопасной областью для mobile runtime.

`export_presets.e2export.json` должен содержать presets для `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64` и `WebAssemblyBrowser`. Android preset обязан использовать renderer profile `Compatibility`, потому что эта игра проверяет мобильный fallback-путь. Эти presets являются проверяемыми input-файлами обычного export workflow; реальные platform smoke, soak и production package checks закрываются задачами `T-0096`, `T-0093` и release gate.

## Gameplay Contract

C# script должен создавать runtime UI scene через публичный API Electron2D и явно использовать:

- `Control` root с container-based layout;
- минимум два container-типа, включая линейный container и grid/table-style container;
- `Panel`, `Label`, `Button`, `TextureButton`, `CheckBox`, `Slider`, `ProgressBar`, `TextureRect` или `NinePatchRect`;
- structured UI control для списка или вкладок, если соответствующий публичный API доступен; иначе script должен явно описать fallback через обычные `Button`/`Label` элементы и verifier должен проверять этот fallback;
- локализацию через `Translation`/`TranslationServer.Tr(...)` или public equivalent `Tr(...)`, переключение `en`/`ru` и fallback key;
- разные target resolutions: desktop landscape, mobile portrait и tablet layout checkpoints;
- touch input через `InputEventScreenTouch` или pointer-equivalent UI action;
- текстовые элементы с локализованными строками, которые не зависят от сетевых ресурсов;
- progress save под user data или verifier-supplied output path;
- scene transitions `menu -> game -> result -> menu`;
- Android compatibility renderer marker, полученный из export preset или project settings.

Playable entrypoint должен использовать только публичный API Electron2D:

- `SceneTree`, `Control`, containers, controls, `CanvasItem`, `InputMap`, `Input`, `InputEvent*` и игровые UI/script-типы;
- публичный runtime window host Electron2D для открытия окна, обработки событий, продвижения кадров, построения draw commands, показа кадра и сохранения screenshot;
- никакого прямого использования backend/window/input библиотек из проекта игры, `Console.ReadKey`, ASCII frame output или custom window loop в `examples/ui-heavy-reference/`.

Проект может использовать curated assets из `data/assets/reference-games/`, но не должен ссылаться на сетевые download steps, placeholder files, ignored local workflow files, `TASKS.md`, `dev-diary/` или `completed-tasks/`.

## Verification

Нужен локальный verifier `tools/Verify-UiHeavyReference.ps1`, который:

- проверяет обязательные файлы и отсутствие запрещённых workflow files внутри примера;
- читает `project.e2d.json` через project settings loader;
- читает `export_presets.e2export.json` через export preset store;
- проверяет, что Android preset использует renderer profile `Compatibility`;
- проверяет `.electron2d/tasks/**` как first-class Editor metadata;
- проверяет, что `resources/ui-heavy-reference.manifest.json` ссылается только на существующие локальные assets;
- проверяет локализацию `en`/`ru`, game data и базовые сигнатуры PNG/OGG/TTF;
- собирает `Electron2D.UiHeavyReference.csproj`;
- запускает headless verification mode и проверяет output-маркеры UI/gameplay subsystems;
- запускает playable script mode через `dotnet run --project examples/ui-heavy-reference/Electron2D.UiHeavyReference.csproj --no-build -- --play-script "play,next,accept,locale,next,accept,result,save,quit" --screenshot <artifact.png>` и проверяет, что пользовательский UI/gameplay loop реально принимает команды, меняет scene state, selection, locale, score, открывает окно через project runtime host Electron2D, рендерит UI frame, сохраняет PNG screenshot и пишет progress save;
- запускает `e2d validate --project examples/ui-heavy-reference --format json` как preview validation route;
- проверяет, что package-planning/verifier steps не копируют `.electron2d/tasks/**` в WebAssembly package output.

Focused automated test должен падать до появления проекта/verifier-а и проходить после реализации. Документация реализации должна описать текущие ограничения: настоящий Tier 1 smoke/soak, performance gate и сборка обеих reference games на пяти платформах остаются в `T-0093`, `T-0096` и `T-0102`.

## Фактическое состояние, ограничения и проверки

Статус: реализованный reference project для `T-0095`.
Обновлено: 2026-06-23.

## Где находится проект

UI-heavy reference game находится в `examples/ui-heavy-reference/` и является валидным проектом `Electron2D.Editor`, а не standalone demo folder.

Ключевые файлы:

- `Electron2D.UiHeavyReference.csproj` - локальный .NET project, который собирается против текущего исходного проекта Electron2D;
- `project.e2d.json` - project settings с `mainScene`, display settings и Input Map;
- `export_presets.e2export.json` - presets для Windows, Linux, macOS, Android, iOS и WebAssembly browser;
- `scenes/menu.scene.json`, `scenes/game.scene.json`, `scenes/result.scene.json` - scene files для Editor/project workflow;
- `Scripts/CardPuzzleGame.cs` - C# gameplay script;
- `resources/ui-heavy-reference.manifest.json` - manifest импортированных ресурсов и их gameplay roles;
- `.electron2d/tasks/board.e2tasks` и `.electron2d/tasks/ui-heavy-acceptance.e2task` - ProjectTaskManager metadata, ожидающие человеческой приёмки.

Проект не содержит `TASKS.md`, `dev-diary/` или `completed-tasks/`: эти файлы относятся только к repository workflow и не должны попадать в пользовательские проекты.

## Что проверяет gameplay

`Scripts/CardPuzzleGame.cs` строит runtime UI scene через public Electron2D API:

- `Control` root и `Panel` создают базовый UI shell;
- `VBoxContainer` и `GridContainer` проверяют container-based layout;
- `Label`, `Button`, `TextureButton`, `CheckBox`, `Slider`, `ProgressBar`, `TextureRect` и `NinePatchRect` проверяют базовые controls;
- `ItemList` проверяет structured UI list, selection и objectives panel;
- `Translation` и `TranslationServer` регистрируют локали `en`/`ru`, переключают locale и проверяют fallback key;
- responsive layout checkpoints применяются для desktop landscape, mobile portrait и tablet layout;
- touch handling реализован через `InputEventScreenTouch`;
- scene transitions проходят путь `menu -> game -> result -> menu`;
- progress save пишет deterministic JSON artifact со сценой, locale, score, moves и выбранной карточкой;
- Android export preset должен использовать renderer profile `Compatibility`.

Headless verification mode запускается обычным `dotnet run` и выводит subsystem markers:

```text
UI-heavy reference scene loaded: scenes/menu.scene.json
UI-heavy reference subsystems: control=True,containers=True,basicControls=True,structuredList=True,localization=True,resolutions=True,touch=True,text=True,save=True,sceneTransition=True,androidCompatibility=True,audio=True
UI-heavy reference progress: scene=menu,locale=en,score=...,moves=...,save=...
```

Playable mode запускается без `--verify`:

```powershell
dotnet run --project examples\ui-heavy-reference\Electron2D.UiHeavyReference.csproj
```

Интерактивный режим показывает видимое текстовое состояние menu/game/result loop и принимает Enter/Space, Left/Right, `L`, `R`, `S`, `Q`. Для автоматической проверки того же loop используется:

```powershell
dotnet run --project examples\ui-heavy-reference\Electron2D.UiHeavyReference.csproj -- --play-script "play,next,accept,locale,next,accept,result,save,quit"
```

`--play-script` выводит `Mode=playable`, `Playable=True`, количество обработанных команд, scene, locale, score, moves, selected card, save path и frame-представление. Это не заменяет будущий platform smoke/soak, но больше не является только subsystem smoke: команды меняют UI/gameplay state.

## Ресурсы

Проект использует реальные локальные ассеты из `data/assets/reference-games/`, скопированные в `examples/ui-heavy-reference/assets/`:

- UI-heavy graphics для карточек, кнопок и status icons;
- card flip и reward OGG-звуки;
- `card-set.json` с проверяемыми данными карточек;
- localization JSON для `en` и `ru`;
- общий TTF font и shared UI sprites.

`resources/ui-heavy-reference.manifest.json` запрещает network download step и перечисляет роли ресурсов. Production package checks должны использовать project-local `assets/**`, а не внешние рабочие каталоги.

## Проверка

Основная локальная команда:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-UiHeavyReference.ps1
```

Verifier выполняет:

- проверку обязательных project files;
- проверку отсутствия repository workflow files внутри примера;
- чтение project settings, Input Map, export presets и ProjectTaskManager documents;
- проверку, что Android preset использует renderer profile `Compatibility`;
- проверку resource manifest, локальных путей, локализации, game data и базовых сигнатур PNG/OGG/TTF;
- `dotnet build examples/ui-heavy-reference/Electron2D.UiHeavyReference.csproj`;
- `dotnet run --project examples/ui-heavy-reference/Electron2D.UiHeavyReference.csproj --no-build -- --verify`;
- `dotnet run --project examples/ui-heavy-reference/Electron2D.UiHeavyReference.csproj --no-build -- --play-script "play,next,accept,locale,next,accept,result,save,quit"`;
- `e2d validate --project examples/ui-heavy-reference --format json`;
- `e2d export build-web --project examples/ui-heavy-reference --skip-publish true --format json`;
- проверку, что WebAssembly package output не содержит `.electron2d/tasks/**`.

Focused automated test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~UiHeavyReferenceProjectTests"
```

## Текущие границы

`T-0095` закрывает наличие законченного UI-heavy project и локального verifier-а. Общая матрица `T-0096` проверяет, что этот проект вместе с reference platformer использует один набор export targets из обычной структуры проекта. Эти задачи не закрывают:

- Tier 1 smoke/soak на reference projects (`T-0093`);
- performance gate на reference games (`T-0102`);
- leak verification и полный release candidate gate (`T-0103`, `T-0104`).

Эти задачи должны использовать `examples/reference-platformer/` и `examples/ui-heavy-reference/` как настоящие Editor projects и не заменять их standalone fixtures.
