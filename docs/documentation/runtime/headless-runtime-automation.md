# Headless runtime automation

Статус: реализованная внутренняя основа.
Задача: `T-0121`.
Обновлено: 2026-06-22.

## Назначение

`e2d run` теперь имеет headless runtime mode, то есть запуск сцены без ручного взаимодействия с `Electron2D.Editor`. Этот режим нужен для CI, автономных агентов и будущих scene/visual tests. Он не является видимым Editor RuntimeController: отдельный видимый запуск игры остаётся задачей Editor workflow.

Headless runtime mode включается, когда `e2d run` получает любой runtime-флаг:

- `--scene`;
- `--frames`;
- `--fixed-delta`;
- `--input`;
- `--capture-frame`;
- `--output`.

Без этих флагов `e2d run --format jsonl` сохраняет прежний generic job mode и возвращает queued job event с snapshot identity.

## Команда

Минимальный запуск:

```powershell
e2d run `
  --project <project-root> `
  --scene scenes/main.scene.json `
  --frames 600 `
  --fixed-delta 0.0166667 `
  --input tests/input/start-game.json `
  --capture-frame 300 `
  --output artifacts/run-001 `
  --format json
```

Обязательные параметры headless mode:

- `--scene <path>` — project-relative путь к scene JSON.
- `--frames <count>` — число кадров больше `0`.
- `--fixed-delta <seconds>` — фиксированный шаг кадра больше `0`.
- `--output <path>` — каталог артефактов. Относительный путь считается от project root.

Необязательные параметры:

- `--input <path>` — project-relative input trace.
- `--capture-frame <frame>` — номер кадра `1..frames`, который создаёт PNG `frame-XXXX.png`.
- `--input-build-configuration-hash <hash>` — hash build/run configuration для snapshot identity.

## Input trace

Input trace — JSON-файл с событиями actions по кадрам:

```json
{
  "format": "Electron2D.InputTrace",
  "version": 1,
  "events": [
    { "frame": 1, "action": "move_right", "state": "pressed" },
    { "frame": 3, "action": "move_right", "state": "released" }
  ]
}
```

Сейчас поддержаны только события `pressed` и `released`. События применяются перед обработкой своего кадра. Итоговое состояние действий записывается в `result.json` и `scene-tree-final.json`.

## Артефакты

Output directory получает:

- `result.json`;
- `diagnostics.json`;
- `runtime.log.jsonl`;
- `scene-tree-final.json`;
- `performance.json`;
- `frame-XXXX.png`, если указан `--capture-frame`.

Все JSON artifacts содержат:

- `schemaVersion = 1`;
- `$schema` со ссылкой на файл из `schemas/runtime/`;
- `inputSnapshotId`;
- `inputWorkspaceRevision`;
- `inputContentRevision`;
- `inputDocumentRevisions`;
- `inputBuildConfigurationHash`.

Published schemas:

- `schemas/runtime/headless-input-trace.schema.json`;
- `schemas/runtime/headless-run-result.schema.json`;
- `schemas/runtime/headless-run-diagnostics.schema.json`;
- `schemas/runtime/headless-run-scene-tree.schema.json`;
- `schemas/runtime/headless-run-performance.schema.json`.

`runtime.log.jsonl` пишет события `runtime.started`, `input.action`, `frame.captured` и `runtime.completed`.

## Текущий scope

Первая реализация детерминирована и deliberately narrow:

- создаёт `WorkspaceSnapshot` через общий job contract;
- открывает scene и input trace в headless `ProjectWorkspace`, чтобы snapshot identity включал revisions входных документов;
- строит final scene tree из стабильного scene JSON;
- применяет input trace к action state;
- создаёт PNG frame capture как стабильный минимальный frame artifact;
- вычисляет `performance.json` из `frames` и `fixedDelta`, не из wall-clock времени.

Она пока не выполняет пользовательский C# runtime process, physics, rendering backend или audio. Эти части подключаются последующими runtime/debug/visual-test задачами поверх того же artifact contract.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~Electron2DHeadlessRuntimeAutomationTests
```

Регрессия CLI contract:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~Electron2DHeadlessRuntimeAutomationTests|FullyQualifiedName~Electron2DCliWorkflowTests"
```
