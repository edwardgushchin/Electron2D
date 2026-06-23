# Electron2D.Editor project shell

Статус: документация реализации для `T-0078`, обновлено для `T-0165`.
Дата: 2026-06-23.

## Назначение

`Electron2D.Editor` является отдельным executable project для desktop-редактора. Базовый shell проверяет, что editor build path существует, использует runtime `Electron2D`, может стартовать без внешнего desktop UI framework, строит стартовый UI root через общий shell layout model и создаёт пользовательское desktop-окно.

Project Manager, docks, viewport interactions, Inspector, Project Settings UI, run/stop workflow, встроенный редактор кода, C# language services и Agent Workspace panel реализуются отдельными задачами поверх этого проекта. Общий layout shell, persistence и visual harness описаны отдельно: [Editor shell layout и visual harness](editor-shell-layout.md). Экран настроек проекта описан отдельно: [Project Settings UI редактора](project-settings-ui.md). Центральное рабочее пространство встроенного редактора кода описано отдельно: [Script workspace редактора](script-workspace.md), semantic C# подсказки описаны в [C# language services в Script workspace](../scripting/editor-language-services.md).

## Текущее поведение

Editor executable поддерживает быстрый bootstrap smoke-режим:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --smoke
```

Smoke-режим создаёт `SceneTree`, настраивает root `Viewport`, строит первый UI root через runtime controls и выводит machine-readable строки с результатом. Проверка используется тестами и CI, чтобы подтвердить, что editor shell запускается на Electron2D runtime.

Обычный запуск без smoke-флагов теперь создаёт desktop window `Electron2D.Editor`, показывает стартовый shell frame и остаётся в event loop до закрытия окна пользователем. Этот режим предназначен для ручного запуска редактора.

Автоматическая проверка реального окна:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --window-smoke .temp\editor-window-smoke
```

Команда создаёт desktop window `Electron2D.Editor`, показывает окно, прокачивает event loop, отрисовывает shell frame, проверяет pointer hit-test по workspace switcher и keyboard command dispatch для baseline shortcut map, затем завершает smoke без зависания. Дополнительно команда запускает существующие visible UI harnesses и последовательно показывает их frames в настоящем окне, чтобы переаттестовать ранее закрытые model-first слои через real-window presenter.

Основной результат сохраняется в:

- `.temp/editor-window-smoke/visual/editor-window-smoke.png`;
- `.temp/editor-window-smoke/visual/editor-window-smoke.analysis.json`.

Layer reattestation artifacts сохраняются под `.temp/editor-window-smoke/visible-layer-reattestation/`:

| Task | Layer |
| --- | --- |
| `T-0157` | default shell layout |
| `T-0150` | Agent Workspace panel |
| `T-0155` | Project Tasks board |
| `T-0158` | Script workspace |
| `T-0159` | Script language services |
| `T-0160` | Managed debugger |
| `T-0161` | Script/debugger tooling |

PNG является screenshot frame, отправленным в созданное окно, а JSON analysis фиксирует `WindowCreated`, `WindowShown`, `FramePresented`, `EventPumpObserved`, selected workspace, размер окна, размер screenshot, pointer/keyboard result, text overflow count, clickable controls, forbidden UI matches и `reattestedVisibleLayers` со статусом `presentedInWindow=True` для каждого visible UI layer.

`Electron2D.Editor` подключает `data/assets/branding/icon/electron2d.ico` как `ApplicationIcon`, поэтому собираемый desktop executable получает брендовую иконку из поставляемого asset pack.

Дополнительная smoke-команда shell layout:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --shell-layout-smoke .temp\editor-shell-visual
```

Она сохраняет layout state, PNG screenshot и JSON analysis artifact для проверки default layout, workspace switcher, docks, bottom panel, persistence и отсутствия 3D/GDScript/AssetLib UI.

Smoke-команда Agent Workspace panel:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --agent-workspace-panel-smoke .temp\agent-workspace-panel
```

Она сохраняет dock state, PNG screenshot и JSON analysis artifact для проверки правого размещения Agent Workspace, session/task/job/actions model, полного diagnostics payload, artifacts с snapshot identity, grouped Undo и отсутствия AI action для human acceptance.

Smoke-команда Project Tasks board:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --tasks-board-smoke .temp\project-tasks-board
```

Она сохраняет board state, PNG screenshot и JSON analysis artifact для проверки центрального `Tasks` workspace, колонок `ProjectTaskManager`, правого `Task Details`, filters, drag-and-drop intent, trusted human actions и отсутствия запрещённых 3D/GDScript/AssetLib UI.

Smoke-команда Project Settings UI:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --project-settings-smoke .temp\project-settings-ui
```

Она создаёт валидный проект из canonical template, открывает его через Project Manager, сохраняет `project.e2d.json` и `export_presets.e2export.json`, заново загружает оба файла, показывает Project Settings frame в настоящем окне и сохраняет PNG/JSON visual analysis artifact. Проверка фиксирует `mainScene`, display settings, renderer profile, physics tick rate, Input Map, export presets, pointer/keyboard result, отсутствие text overflow и отсутствие запрещённых 3D/GDScript/AssetLib UI.

Smoke-команда Script workspace:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-workspace-smoke .temp\script-workspace
```

Она сохраняет script workspace state, PNG screenshot и JSON analysis artifact для проверки центрального `Script` workspace, вкладок, line gutter, editor surface, search/replace, caret/selection, правого `Code Document`, conflict marker, snapshot identity и отсутствия запрещённых 3D/GDScript/AssetLib UI.

Smoke-команда C# language services:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-language-services-smoke .temp\script-language-services
```

Она сохраняет language-services state, PNG screenshot и JSON analysis artifact для проверки completion popup, hover/Quick Info, signature help, live diagnostics, source navigation metadata, rename/format/code-action preview, stale response marker и отсутствия запрещённых 3D/GDScript/AssetLib UI.

## Ограничения

- `--window-smoke` создаёт управляемый короткий event loop для автоматической проверки, а обычный запуск остаётся в event loop до закрытия окна.
- Нет полноценного Project Manager selection screen для ручного выбора проектов; текущий Project Manager и Project Settings UI доступны как проверяемые Editor smoke workflows.
- Общий shell layout уже содержит зоны docks. Scene editing, Inspector UI, 2D viewport tools, run workflow, Script workspace, model-first Agent Workspace content, model-first `Tasks` workspace и Project Settings UI реализованы отдельными задачами.
- Editor project не должен добавлять WPF, WinForms, Avalonia или другой внешний UI framework.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectShellTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorShellLayoutTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorAgentWorkspacePanelTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectTasksBoardTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectSettingsUiTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptWorkspaceTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptLanguageServicesTests"
```

Ручная visual acceptance проверка после `T-0165`:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --window-smoke .temp\editor-window-smoke
```

После запуска агент должен открыть `.temp/editor-window-smoke/visual/editor-window-smoke.png` и проверить, что layout читаем, `Tasks` workspace выбран, docks и bottom panel размещены ожидаемо, текст не выходит за контейнеры и запрещённые `3D`/`AssetLib`/GDScript элементы отсутствуют.

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-ReleaseMetadata.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
