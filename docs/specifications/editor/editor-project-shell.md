# Electron2D.Editor project shell

Статус: целевая спецификация для `T-0078`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor` должен появиться как отдельный desktop executable в `src/Electron2D.Editor/`. Эта задача создаёт только базовый shell редактора: проект должен собираться вместе с решением, ссылаться на runtime `Electron2D`, стартовать в проверяемом smoke-режиме и строить первый UI root на публичном runtime UI API.

Project Manager, scene tree dock, 2D viewport, Inspector, FileSystem dock, run/stop workflow, встроенный редактор кода и Agent Workspace panel остаются отдельными задачами. Эта задача не должна подменять их placeholder UI или публичными editor-only API.

## Контракт проекта

- `src/Electron2D.Editor/Electron2D.Editor.csproj` является executable project для `net10.0`.
- Проект добавлен в `src/Electron2D.sln`.
- Editor project использует `ProjectReference` на `src/Electron2D/Electron2D.csproj`.
- Editor project не использует WPF, WinForms, Avalonia или другой внешний desktop UI framework.
- Editor project не добавляет публичные runtime types в assembly `Electron2D`.
- Исходные файлы editor project имеют MIT header проекта.

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

Обычный запуск без `--smoke` в этой задаче может выполнять тот же безопасный startup path и завершаться после bootstrap. Бесконечный event loop и real desktop window относятся к следующим editor задачам.

## Приемочные критерии

- Есть integration tests, которые подтверждают наличие editor project в solution, отсутствие внешнего UI framework package references и успешный `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --smoke`.
- `dotnet build src/Electron2D.sln -c Release` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
