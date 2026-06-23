# UI-heavy reference game

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
- `e2d validate --project examples/ui-heavy-reference --format json`;
- `e2d export build-web --project examples/ui-heavy-reference --skip-publish true --format json`;
- проверку, что WebAssembly package output не содержит `.electron2d/tasks/**`.

Focused automated test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~UiHeavyReferenceProjectTests"
```

## Текущие границы

`T-0095` закрывает наличие законченного UI-heavy project и локального verifier-а. Она не закрывает:

- сборку обеих reference games на всех целевых платформах (`T-0096`);
- Tier 1 smoke/soak на reference projects (`T-0093`);
- performance gate на reference games (`T-0102`);
- leak verification и полный release candidate gate (`T-0103`, `T-0104`).

Эти задачи должны использовать `examples/reference-platformer/` и `examples/ui-heavy-reference/` как настоящие Editor projects и не заменять их standalone fixtures.
