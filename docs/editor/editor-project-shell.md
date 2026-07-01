# Electron2D.Editor project shell

Обновлено: 2026-07-01.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0078`, расширена проверкой реального окна `T-0165`, проверкой использования общего runtime UI stack `T-0171` и рабочим путём репозитория после `T-0210`.
Дата: 2026-07-01.

## Цель

`Electron2D.Editor` должен появиться как отдельный desktop executable в `src/Electron2D.Editor/`. Базовый shell редактора должен собираться вместе с решением, ссылаться на runtime `Electron2D`, стартовать в проверяемом smoke-режиме и строить первый UI root на общем runtime UI stack Electron2D.

Начиная с `T-0165`, visible UI acceptance нельзя закрывать bootstrap model, PNG из synthetic harness или headless smoke-командой. Обычный запуск редактора не должен показывать synthetic visual harness/debug layer, то есть заранее нарисованную пиксельную картинку вместо настоящего интерфейса. Редактор должен иметь проверяемый desktop window host: обычный запуск создаёт пользовательское окно, строит shell из runtime `Control`/`Button` узлов, отправляет pointer/keyboard input в этот `SceneTree` и остаётся в event loop до закрытия окна.

Project Manager, scene tree dock, 2D viewport, Inspector, FileSystem dock, run/stop workflow, встроенный редактор кода и Agent Workspace panel остаются отдельными задачами. Эта задача не должна подменять их placeholder UI или публичными editor-only API.

## Runtime UI dogfooding gate

Интерфейс `Electron2D.Editor` должен быть построен и запущен через общий runtime UI stack Electron2D. Здесь dogfooding означает не запрет на внутренний код редактора, а использование тех же фундаментальных `SceneTree`, `Viewport`, `Control`, `Container`, theme, input, focus и rendering paths, с которыми столкнётся обычное приложение. Editor может быть дружественной сборкой и пользоваться ограниченным internal host API для frame lifecycle, platform events, input dispatch, render submission и presentation.

Для shell это означает:

- `SceneTree` и root `Viewport` являются владельцами UI.
- Видимые элементы создаются настоящими runtime `Control`-типами: `Panel`, `Label`, `Button`, `Container` и последующими стандартными или editor-owned control subclasses.
- Кликабельные действия shell выполняются через button signal path: input доставляется в `SceneTree`, runtime вызывает `_GuiInput` соответствующего `Button`, затем `Button` испускает сигнал.
- Рендер кадра берёт команды из `CanvasItem`/`Control` draw callbacks общего runtime stack. Внутренние части renderer могут оставаться `internal`, но редактор не должен рисовать видимые widgets вторым shell renderer.
- Пиксельный буфер окна допустим только как конечный presentation buffer; он не является моделью интерфейса Editor.
- `ShellRegion` может остаться snapshot, тестовой или persistence-моделью. Нарушением он становится только если видимый shell рисуется напрямую из этих регионов или input обрабатывается самостоятельным hit-test по ним вместо доставки событий в `Control`.

Запрещённые обходы runtime UI dogfooding:

- рисовать рабочий shell напрямую прямоугольниками и текстом из `ShellRegion`;
- считать `Control` tree доказательством UI, если видимый кадр создан отдельным ручным renderer;
- обрабатывать pointer selection через поиск координат в `ShellRegion` вместо доставки input в `SceneTree` и дальнейшего runtime GUI routing;
- реализовывать отдельные от `Control` hover, focus, pressed, disabled, clipping или input routing models для shell widgets;
- добавлять editor-only UI API в runtime только для обхода отсутствующей общей runtime UI возможности.

Если общий runtime UI stack не позволяет построить нужный shell без такого обхода, это считается незакрытым расхождением и T-0171 не может быть принята. Отсутствие default theme/font, render clipping, text wrapping/ellipsis, hover/focus/pressed/disabled visuals, scrolling, popup/modal focus, keyboard navigation, DPI scaling, reusable split containers или allocation-free layout остаётся отдельными UI API gaps, потому что эти возможности нужны и редактору, и обычным приложениям.

## Контракт проекта

- `src/Electron2D.Editor/Electron2D.Editor.csproj` является executable project для `net10.0`.
- На Windows editor executable собирается как GUI application, а не console application: обычный запуск и double-click по связанному `.e2d` не должны создавать отдельное console window. Smoke-команды по-прежнему обязаны писать machine-readable output, когда stdout/stderr перенаправлены тестовым процессом.
- Проект добавлен в `src/Electron2D.sln`.
- Editor project использует `ProjectReference` на `src/Electron2D/Electron2D.csproj`.
- Editor project не использует WPF, WinForms, Avalonia или другой внешний desktop UI framework.
- Editor project не добавляет публичные runtime types в assembly `Electron2D`.
- Исходные файлы editor project имеют MIT header проекта.
- Editor project использует `data/assets/branding/icon/electron2d.ico` как `ApplicationIcon` для desktop executable.

## Smoke-режим

Executable должен поддерживать аргумент:

```text
--smoke
```

Smoke-режим должен:

- создать `SceneTree`;
- настроить root `Viewport.Size` для desktop editor shell;
- создать UI root на базе `Control`/`Panel`/`Label` или других runtime UI controls;
- подготовить bootstrap state через общий runtime API, без обращения к test-only frame stepping;
- вернуть exit code `0`;
- вывести короткий machine-readable текст, по которому test host может подтвердить, что editor shell использует Electron2D runtime.

`--smoke` остаётся быстрым bootstrap-путём для проверки зависимостей и runtime UI root. Он не является доказательством visible UI.

## Real-window smoke

Executable должен поддерживать аргумент:

```text
--window-smoke <work-root>
```

Real-window smoke должен:

- создать desktop window с заголовком `Electron2D.Editor`;
- использовать тот же shell layout model, что и обычный Editor startup;
- построить shell UI через runtime `Control`, `Panel`, `Label`, `Button` и `Container`, а не через synthetic visual harness/debug layer или ручной renderer по координатным областям;
- показать окно, войти в управляемый event loop и отрисовать минимум один frame shell UI через общий runtime host;
- выполнить проверяемый pointer input по видимой кнопке через runtime input path и keyboard command dispatch для shortcut из Editor baseline;
- сохранить screenshot frame, который соответствует кадру, отправленному в созданное окно;
- сохранить JSON analysis с размером окна, размером screenshot, selected workspace, rendered frame count, draw command count, runtime UI source, input dispatch source, pointer/keyboard result, text overflow count, clickable control count, forbidden UI matches и долей красных доминирующих пикселей;
- вывести machine-readable строки `WindowCreated=True`, `WindowShown=True`, `FramePresented=True`, `EventPumpObserved=True`, `PointerInteractionObserved=True`, `KeyboardInteractionObserved=True`, `RuntimeControlTree=True`, `VisualHarnessRemoved=True`, `ScreenshotReviewed=True`, `ScreenshotPath=...` и `AnalysisPath=...`;
- завершиться без зависания и уничтожить созданное окно.

Если host не может создать desktop window в текущей среде, smoke должен завершиться ошибкой с диагностикой, а visible UI-задачи остаются неприемлемыми до проверки на машине с окном. Headless bootstrap, compile-only check или synthetic harness без real-window smoke не закрывают этот критерий.

Обычный запуск без smoke-флагов должен создавать `Electron2D.Editor` desktop window, запускаться в maximized resizable window mode, занимать всю доступную клиентскую область кадром редактора без чёрных незаполненных зон и оставаться в event loop до закрытия пользователем или системного запроса на завершение. Этот path обязан использовать runtime-control-tree shell и runtime event loop: synthetic `ShellVisualHarness`, заранее нарисованный canvas, red/fallback debug frame и одноразовый static screenshot запрещены для обычного окна.

Если обычный запуск получает единственный аргумент `<ProjectName>.e2d`, startup сначала открывает проект через Project Manager, затем строит runtime-control-tree shell из project-bound layout. Открытие существующего проекта не должно требовать repository root или template directory рядом с executable: эти данные нужны для создания новых проектов, но не для чтения уже существующего `.e2d`.

## Приемочные критерии

- Есть integration tests, которые подтверждают наличие editor project в solution, отсутствие внешнего UI framework package references и успешный `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --smoke`.
- Есть integration test для `--window-smoke <work-root>`, который проверяет создание окна, event loop, rendered frame, runtime control tree, pointer/keyboard result, screenshot, JSON analysis artifact и отсутствие red debug frame.
- Есть integration test, который подтверждает, что `Electron2D.Editor.csproj` использует `WinExe`, чтобы Windows double-click не создавал отдельное console window.
- `ShellVisualHarness` не используется в runtime/window path редактора; shell layout smoke может проверять только модель layout и JSON analysis без fake screenshot.
- Release metadata verifier подтверждает, что `Electron2D.Editor` подключает брендовую `.ico`-иконку.
- `dotnet build src/Electron2D.sln -c Release` проходит.
- `dotnet run --project eng\Electron2D.Build -- test --timeout-seconds 3600` проходит.
- `dotnet run --project eng\Electron2D.Build -- verify licenses` проходит.

## Фактическое состояние, ограничения и проверки

Статус: документация реализации для `T-0078`, обновлено для `T-0171` и текущего C#-инструмента репозитория.
Дата: 2026-07-01.

## Назначение

`Electron2D.Editor` является отдельным executable project для desktop-редактора. Базовый shell проверяет, что editor build path существует, использует runtime `Electron2D`, может стартовать без внешнего desktop UI framework, строит стартовый UI root через общий shell layout model и создаёт пользовательское desktop-окно.

Project Manager, docks, viewport interactions, Inspector, Project Settings UI, run/stop workflow, встроенный редактор кода, C# language services и Agent Workspace panel реализуются отдельными задачами поверх этого проекта. Общий layout shell, persistence и visual harness описаны отдельно: [Editor shell layout и visual harness](editor-shell-layout.md). Экран настроек проекта описан отдельно: [Project Settings UI редактора](project-settings-ui.md). Центральное рабочее пространство встроенного редактора кода описано отдельно: [Script workspace редактора](script-workspace.md), semantic C# подсказки описаны в [C# language services в Script workspace](../scripting/editor-language-services.md).
Specialized editors для `SpriteFrames`, `TileMap` и `AnimationPlayer` описаны отдельно: [Specialized editors в `Electron2D.Editor`](specialized-editors.md).

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

Команда создаёт desktop window `Electron2D.Editor`, показывает окно, прокачивает event loop, отрисовывает shell frame из runtime `Control` draw commands, проверяет pointer input по workspace switcher через runtime input dispatch и keyboard command dispatch для baseline shortcut map, затем завершает smoke без зависания.

Основной результат сохраняется в:

- `.temp/editor-window-smoke/visual/editor-window-smoke.png`;
- `.temp/editor-window-smoke/visual/editor-window-smoke.analysis.json`.

PNG является screenshot frame, отправленным в созданное окно, а JSON analysis фиксирует `WindowCreated`, `WindowShown`, `FramePresented`, `EventPumpObserved`, selected workspace, размер окна, размер screenshot, pointer/keyboard result, text overflow count, clickable controls и forbidden UI matches.

Начиная с правки `T-0171`, real-window smoke фиксирует `rendering.source=runtime-control-tree`, `runtimeUiRendering=True`, `input.dispatchSource=RuntimeHost`, `runtimeUiDispatch=True`, `visualHarnessRemoved=True`, `drawCommands` и `redDominantPixelRatio`. Он больше не создаёт `reattestedVisibleLayers`, потому что обычное окно должно проверяться через текущий runtime UI tree, то есть через реальные runtime `Control` nodes и их draw commands, а не через повторный показ заранее сохранённых harness screenshots.

`Electron2D.Editor` подключает `data/assets/branding/icon/electron2d.ico` как `ApplicationIcon`, поэтому собираемый desktop executable получает брендовую иконку из поставляемого asset pack.

Дополнительная smoke-команда shell layout:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --shell-layout-smoke .temp\editor-shell-visual
```

Она сохраняет layout state и JSON analysis artifact для проверки default layout, workspace switcher, docks, bottom panel, persistence и отсутствия 3D/GDScript/AssetLib UI. Эта команда не создаёт PNG и не заменяет real-window visual acceptance; screenshot для видимого shell должен идти через `--window-smoke` или отдельную visual smoke-команду.

Smoke-команда Agent Workspace panel:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --agent-workspace-panel-smoke .temp\agent-workspace-panel
```

Она сохраняет dock state, PNG screenshot и JSON analysis artifact для проверки размещения Agent Workspace во вкладке `Agent` нижней панели, session/task/job/actions model, полного diagnostics payload, artifacts с snapshot identity, grouped Undo и отсутствия AI action для human acceptance.

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

Smoke-команда specialized editors:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --specialized-editors-smoke .temp\specialized-editors-smoke
```

Она создаёт валидный проект из canonical template, сохраняет `SpriteFrames`, `TileSet`, `Animation` и scene file в runtime text formats, перечитывает их через runtime serializers, показывает specialized editors frame в настоящем окне и сохраняет PNG/JSON visual analysis artifact. Проверка фиксирует `SpriteFrames`, `TileMap` и `AnimationPlayer` panels, pointer interaction по palette tile, keyboard save command, отсутствие text overflow и отсутствие запрещённых 3D/GDScript/AssetLib UI.

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
- Публичные `Label` и `Button` требуют явный `Font` в theme. Готового публичного default-font factory пока нет, поэтому shell редактора задаёт локальный простой `ShellFont`, наследующий публичный `Font` contract с базовыми метриками. Это не блокирует текущий shell, но остаётся пробелом публичного API для приложений, которым нужен готовый шрифт без собственного ресурса.
- Нет полноценного Project Manager selection screen для ручного выбора проектов; текущий Project Manager и Project Settings UI доступны как проверяемые Editor smoke workflows.
- Общий shell layout уже содержит зоны docks и bottom panel. Scene editing, Inspector UI, 2D viewport tools, run workflow, Script workspace, model-first Agent Workspace content, model-first `Tasks` workspace и Project Settings UI реализованы отдельными задачами.
- Editor project не должен добавлять WPF, WinForms, Avalonia или другой внешний UI framework.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectShellTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorShellLayoutTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorAgentWorkspacePanelTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectTasksBoardTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectSettingsUiTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorSpecializedEditorsTests"
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
dotnet run --project eng\Electron2D.Build -- verify licenses
dotnet run --project eng\Electron2D.Build -- verify release-metadata
dotnet run --project eng\Electron2D.Build -- test --timeout-seconds 3600
dotnet build src\Electron2D.sln -c Release
```
