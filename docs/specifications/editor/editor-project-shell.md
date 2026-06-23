# Electron2D.Editor project shell

Статус: целевая спецификация для `T-0078`, расширена real-window gate `T-0165`.
Дата: 2026-06-23.

## Цель

`Electron2D.Editor` должен появиться как отдельный desktop executable в `src/Electron2D.Editor/`. Базовый shell редактора должен собираться вместе с решением, ссылаться на runtime `Electron2D`, стартовать в проверяемом smoke-режиме и строить первый UI root на публичном runtime UI API.

Начиная с `T-0165`, visible UI acceptance нельзя закрывать только bootstrap model, PNG из synthetic harness или headless smoke-командой. Редактор должен иметь проверяемый desktop window host: обычный запуск создаёт пользовательское окно, а documented automated smoke создает такое же окно, отрисовывает shell frame, проверяет ввод и сохраняет screenshot/analysis artifact.

Project Manager, scene tree dock, 2D viewport, Inspector, FileSystem dock, run/stop workflow, встроенный редактор кода и Agent Workspace panel остаются отдельными задачами. Эта задача не должна подменять их placeholder UI или публичными editor-only API.

## Контракт проекта

- `src/Electron2D.Editor/Electron2D.Editor.csproj` является executable project для `net10.0`.
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
- создать UI root на базе `Control`/`Panel`/`Label` или других public UI controls runtime;
- подготовить bootstrap state только через публичный runtime API, без обращения к test-only frame stepping;
- вернуть exit code `0`;
- вывести короткий machine-readable текст, по которому test host может подтвердить, что editor shell использует Electron2D runtime.

`--smoke` остаётся быстрым bootstrap-путём для проверки зависимостей и public UI root. Он не является доказательством visible UI.

## Real-window smoke

Executable должен поддерживать аргумент:

```text
--window-smoke <work-root>
```

Real-window smoke должен:

- создать desktop window с заголовком `Electron2D.Editor`;
- использовать тот же shell layout model, что и обычный Editor startup;
- показать окно, войти в управляемый event loop и отрисовать минимум один frame shell UI;
- выполнить проверяемый pointer hit-test по видимому control и keyboard command dispatch для shortcut из Editor baseline;
- сохранить screenshot frame, который соответствует видимому содержимому созданного окна;
- сохранить JSON analysis с размером окна, размером screenshot, selected workspace, rendered frame count, pointer/keyboard result, text overflow count, clickable control count и forbidden UI matches;
- переаттестовать visible UI layers `T-0157`, `T-0150`, `T-0155`, `T-0158`, `T-0159`, `T-0160` и `T-0161`, показав каждый layer frame в real-window presenter и записав `presentedInWindow=True` в общий analysis;
- вывести machine-readable строки `WindowCreated=True`, `WindowShown=True`, `FramePresented=True`, `EventPumpObserved=True`, `PointerInteractionObserved=True`, `KeyboardInteractionObserved=True`, `ReattestedVisibleLayers=T-0157|T-0150|T-0155|T-0158|T-0159|T-0160|T-0161`, `ScreenshotReviewed=True`, `ScreenshotPath=...` и `AnalysisPath=...`;
- завершиться без зависания и уничтожить созданное окно.

Если host не может создать desktop window в текущей среде, smoke должен завершиться ошибкой с диагностикой, а visible UI-задачи остаются неприемлемыми до проверки на машине с окном. Headless bootstrap, compile-only check или synthetic harness без real-window smoke не закрывают этот критерий.

Обычный запуск без smoke-флагов должен создавать `Electron2D.Editor` desktop window и оставаться в event loop до закрытия пользователем или системного запроса на завершение.

## Приемочные критерии

- Есть integration tests, которые подтверждают наличие editor project в solution, отсутствие внешнего UI framework package references и успешный `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --smoke`.
- Есть integration test для `--window-smoke <work-root>`, который проверяет создание окна, event loop, rendered frame, pointer/keyboard result, screenshot и JSON analysis artifact.
- Visible UI-задачи `T-0157`, `T-0150`, `T-0155`, `T-0158`, `T-0159`, `T-0160` и `T-0161` переаттестованы real-window smoke artifact или отдельными real-window сценариями.
- Release metadata verifier подтверждает, что `Electron2D.Editor` подключает брендовую `.ico`-иконку.
- `dotnet build src/Electron2D.sln -c Release` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
