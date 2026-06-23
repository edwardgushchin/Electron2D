# Editor shell layout и visual harness

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт, состояние и проверки

Статус: реализовано для `T-0157`, переаттестация через окно добавлена в `T-0165`.
Обновлено: 2026-06-23.

## Назначение

`Electron2D.Editor` имеет общий shell layout для стартового окна редактора: верхнее меню, переключатель центральных рабочих пространств, вкладки документов, левые и правые docks, нижнюю панель и зарезервированную область `Agent Workspace`.

Текущая реализация использует один внутренний `ShellLayout` для стартового UI root редактора, сохраняемого состояния layout, automated visual harness и real-window smoke. Обычный запуск создаёт окно `Electron2D.Editor` как GUI application без отдельной консоли Windows и строит shell через runtime-control-tree, то есть через реальные runtime `Control` nodes, а не через заранее нарисованный PNG/harness.

## Default layout

Viewport shell фиксируется как `1280x720` для smoke-проверки и visual acceptance harness.

Верхнее меню:

```text
Scene | Project | Debug | Editor | Help
```

Workspace switcher содержит только:

```text
2D | Script | Game | Tasks
```

Левая область:

```text
Scene
FileSystem
```

Правая область:

```text
Inspector
Node
Agent Workspace
```

Bottom panel:

```text
Output | Debugger | Diagnostics | Search | Animation | Audio | Tests
```

В shell отсутствуют workspace `3D`, `AssetLib`, GDScript UI, `.gd` templates, `Node3D` entries и disabled 3D controls.

## Layout state

Shell сохраняет JSON state с такими данными:

- выбранный central workspace;
- collapsed/expanded состояние bottom panel;
- размеры left/right docks и bottom panel;
- открытые document tabs;
- per-workspace selection, scroll, zoom и open documents.

Smoke-сценарий переключает workspaces `Script`, `Game`, `Tasks`, сворачивает и разворачивает bottom panel, сохраняет state, заново читает его и проверяет stable round-trip.

## Shortcut map

Machine-readable shortcut map находится в `ShellLayout.Shortcuts`.

Поддержаны базовые действия:

- `F5` - run project;
- `F6` - run current scene;
- `F7` - switch to Script workspace или build для code context;
- `F8` - stop/pause active play/debug session;
- `Ctrl+S` - save current document;
- `Ctrl+Shift+S` - save all;
- `Ctrl+F` - search current document или active panel;
- `Ctrl+Shift+F` - project search;
- `Ctrl+Z` / `Ctrl+Y` - undo/redo;
- `Ctrl+P` - quick open project file;
- `Ctrl+G` - go to line.

Smoke-проверка подтверждает, что shortcut map не содержит 3D или GDScript actions.

## Visual harness

Команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --shell-layout-smoke .temp\editor-shell-visual
```

Создаёт:

- `.temp/editor-shell-visual/editor-shell-layout.state.json`;
- `.temp/editor-shell-visual/visual/editor-shell-default.png`;
- `.temp/editor-shell-visual/visual/editor-shell-default.analysis.json`.

PNG является deterministic screenshot artifact для ручного просмотра агентом до закрытия UI-задачи. JSON analysis содержит координаты workspace switcher, left docks, right docks, bottom panel, число кликабельных controls, результат проверки text overflow и список запрещённых UI matches.

Начиная с `T-0165`, этот harness является подготовительной проверкой layout model, а не финальной приёмкой visible UI. Он не используется в обычном интерактивном запуске редактора. Финальная проверка видимого shell должна использовать real-window smoke:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --window-smoke .temp\editor-window-smoke
```

Она создаёт окно `Electron2D.Editor`, строит кадр через runtime-control-tree, то есть через реальные runtime `Control` nodes, сохраняет `.temp/editor-window-smoke/visual/editor-window-smoke.png` и `.temp/editor-window-smoke/visual/editor-window-smoke.analysis.json`, фиксирует размер окна `1280x720`, selected workspace `Tasks`, event loop, rendered frame, pointer/keyboard result, отсутствие text overflow и отсутствие запрещённых UI entries.

JSON analysis дополнительно фиксирует `rendering.source=runtime-control-tree`, `visualHarnessRemoved=True`, `drawCommands` и `redDominantPixelRatio`. Обычный real-window smoke больше не загружает старые screenshots отдельных harness-слоёв и не создаёт `reattestedVisibleLayers`, потому что этот путь должен проверять текущее окно редактора, а не заранее подготовленные изображения.

В текущей проверке агент открыл `editor-shell-default.png` и подтвердил:

- workspace switcher содержит только `2D`, `Script`, `Game`, `Tasks`;
- `Scene` и `FileSystem` расположены слева;
- `Inspector`, `Node` и `Agent Workspace` расположены справа;
- bottom panel видна, читаема и имеет кнопку `Collapse`;
- текст не выходит за границы контейнеров и не перекрывает соседние элементы;
- `3D`, `AssetLib`, GDScript UI и disabled 3D controls визуально отсутствуют.

Для real-window smoke агент открыл `.temp/editor-window-smoke/visual/editor-window-smoke.png` и подтвердил:

- окно 1280x720 показывает верхнее меню `Scene`, `Project`, `Debug`, `Editor`, `Help`;
- workspace switcher содержит только `2D`, `Script`, `Game`, `Tasks`, выбран `Tasks`;
- `Scene` и `FileSystem` расположены слева;
- `Inspector`, `Node` и `Agent Workspace` расположены справа;
- bottom panel видна внизу и содержит tabs `Output`, `Debugger`, `Diagnostics`, `Search`, `Animation`, `Audio`, `Tests`;
- текст читаем и не выходит за контейнеры;
- `3D`, `AssetLib`, GDScript UI и disabled 3D controls визуально отсутствуют.

## Проверки

Focused test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorShellLayoutTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorWindowSmokeRunCreatesRealWindowAndWritesVisualArtifacts"
```

Smoke-команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --shell-layout-smoke .temp\editor-shell-visual
```

Документационный verifier после изменения справки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LocalDocumentation.ps1
```
