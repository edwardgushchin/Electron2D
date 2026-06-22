# Headless runtime automation

Статус: целевая спецификация для `T-0121`.
Обновлено: 2026-06-22.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md), [WorkspaceSnapshot, job input identity и dirty export policy](../project-system/workspace-snapshot.md), [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md).

## Назначение

Headless runtime automation — это запуск сцены без ручного взаимодействия с `Electron2D.Editor`. Этот режим нужен для CI, автономных агентов, scene tests и пакетных проверок. Он не заменяет видимый запуск в Editor: задача видимого управления запущенной игрой остаётся отдельным Editor workflow.

Команда `e2d run` должна иметь два совместимых режима:

- generic job mode без runtime-флагов продолжает возвращать job event и snapshot identity для существующего CLI/MCP contract;
- headless runtime mode включается, когда команда получает runtime-флаги `--frames`, `--fixed-delta`, `--input`, `--capture-frame`, `--output` или `--scene`.

## Команда

Минимальная команда:

```powershell
e2d run --project . --scene scenes/main.scene.json --frames 600 --fixed-delta 0.0166667 --input tests/input/start-game.json --capture-frame 300 --output artifacts/run-001 --format json
```

Обязательные параметры headless runtime mode:

- `--scene <path>` — project-relative путь к сцене.
- `--frames <count>` — количество симулируемых кадров, целое число больше `0`.
- `--fixed-delta <seconds>` — фиксированный шаг кадра в секундах, число больше `0`.
- `--output <path>` — каталог артефактов. Относительный путь считается относительно project root.

Необязательные параметры:

- `--input <path>` — project-relative путь к input trace.
- `--capture-frame <frame>` — номер кадра для сохранения PNG frame capture. Номер должен быть в диапазоне `1..frames`.
- `--input-build-configuration-hash <hash>` — hash конфигурации build/run, который сохраняется в job input identity.

## Input trace

Input trace — это JSON-документ с событиями действий по кадрам. Минимальная схема:

```json
{
  "format": "Electron2D.InputTrace",
  "version": 1,
  "events": [
    { "frame": 1, "action": "move_right", "state": "pressed" },
    { "frame": 4, "action": "move_right", "state": "released" }
  ]
}
```

Правила:

- `frame` начинается с `1`;
- `action` не может быть пустым;
- `state` принимает только `pressed` или `released`;
- события применяются перед симуляцией своего кадра;
- итоговое состояние действий попадает в `result.json`, `scene-tree-final.json` и `runtime.log.jsonl`.

## Артефакты

`--output` должен получить стабильный набор файлов:

```text
result.json
diagnostics.json
runtime.log.jsonl
scene-tree-final.json
performance.json
frame-0300.png
```

`frame-XXXX.png` создаётся только когда указан `--capture-frame`; имя использует номер кадра с минимум четырьмя цифрами. Например, `--capture-frame 7` создаёт `frame-0007.png`.

Все JSON-файлы используют `schemaVersion = 1`, стабильный порядок полей и перенос строки `\n` в конце файла. `result.json`, `diagnostics.json`, `scene-tree-final.json`, `performance.json` и input trace должны иметь опубликованные JSON Schema Draft 2020-12 files в `schemas/runtime/`.

## Snapshot identity

Headless run создаёт `WorkspaceSnapshot` через общий job contract. Каждый runtime artifact должен быть связан со входным состоянием:

- `inputSnapshotId`;
- `inputWorkspaceRevision`;
- `inputContentRevision`;
- `inputDocumentRevisions`;
- `inputBuildConfigurationHash`.

Эти поля нужны Agent Workspace, Editor RuntimeController и будущим visual tests, чтобы понять, устарел ли screenshot, runtime tree или diagnostics после изменения проекта.

## JSON shape

`result.json` содержит:

- `schemaVersion`;
- `command = "run"`;
- `succeeded`;
- `scene`;
- `frames`;
- `fixedDelta`;
- `capturedFrame`;
- `outputDirectory`;
- `artifacts`;
- snapshot identity;
- итоговое состояние actions.

`diagnostics.json` содержит `schemaVersion`, snapshot identity и массив structured diagnostics. Для успешного минимального запуска массив пустой.

`scene-tree-final.json` содержит `schemaVersion`, `scene`, `finalFrame`, snapshot identity, список узлов сцены и итоговое состояние actions. Первая реализация может строить дерево из стабильного текстового scene document без выполнения пользовательского C# кода, но формат должен оставаться пригодным для будущего настоящего runtime process.

`performance.json` содержит `schemaVersion`, `frames`, `fixedDelta`, `simulatedSeconds`, `averageFrameTimeMs`, `fps` и snapshot identity. В первой реализации эти metrics детерминированы из входных параметров, без попытки измерять wall-clock время как runtime performance.

`runtime.log.jsonl` содержит по одной JSON-строке на событие. Минимальные события:

- `runtime.started`;
- `input.action`;
- `frame.captured`;
- `runtime.completed`.

## Диагностика и безопасность

Команда должна fail-closed:

- отсутствующий `--scene`, `--frames`, `--fixed-delta` или `--output` в headless mode возвращает structured CLI diagnostic;
- путь `--output` не может выходить за пределы project root, если он относительный;
- output directory пересоздаётся только как дочерний путь project root или абсолютный путь, явно переданный пользователем;
- input trace с неизвестным state или frame вне диапазона возвращает structured CLI diagnostic;
- команда не выполняет произвольный shell и не читает signing secrets.

## Критерии приёмки

- `e2d run` поддерживает `--scene`, `--frames`, `--fixed-delta`, `--input`, `--capture-frame`, `--output` в headless runtime mode.
- Generic `e2d run --format jsonl` без runtime-флагов сохраняет существующий job event contract.
- Input trace применяет `pressed` и `released` action events по кадрам.
- Выходной каталог содержит стабильные `result.json`, `diagnostics.json`, `runtime.log.jsonl`, `scene-tree-final.json`, `performance.json` и PNG frame capture при `--capture-frame`.
- JSON artifacts ссылаются на published schemas в `schemas/runtime/`.
- Все artifacts содержат snapshot identity fields.
- Повторный same-platform run с одинаковыми inputs создаёт одинаковые JSON artifacts и PNG bytes, кроме абсолютного `outputDirectory`, если путь запуска изменился.
- Headless run не требует открытого Editor и не выполняет GUI automation.
- Implementation documentation в `docs/documentation/runtime/` описывает фактический scope, формат input trace, artifacts и команды проверки.
