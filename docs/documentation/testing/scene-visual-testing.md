# Scene tests и visual regression tests

Статус: реализованная внутренняя основа.
Задача: `T-0122`.
Обновлено: 2026-06-22.

## Назначение

`Electron2D.Testing` добавляет первый проверяемый слой scene/visual tests для проектов Electron2D. Это internal test framework, то есть внутренняя библиотека для CLI, CI и будущих панелей Editor/Agent Workspace, а не публичный runtime API для пользовательских игр.

Текущая реализация запускается без ручного Editor UI. Она читает scene-test manifest, загружает scene JSON, проверяет узлы и свойства, детерминированно продвигает кадры и пишет machine-readable artifacts для дальнейшего отображения человеком или агентом.

## Manifest

По умолчанию CLI ищет manifest:

```text
tests/electron2d.scene-tests.json
```

Минимальный пример:

```json
{
  "format": "Electron2D.SceneTestSuite",
  "version": 1,
  "tests": [
    {
      "name": "player_exists",
      "scene": "scenes/main.scene.json",
      "frames": 1,
      "fixedDelta": 0.5,
      "assertNodes": [
        {
          "path": "/Player",
          "type": "Electron2D.Node2D"
        }
      ],
      "assertProperties": [
        {
          "node": "/Player",
          "property": "speed",
          "equals": 10
        }
      ],
      "visual": {
        "captureFrame": 1,
        "reference": "tests/references/player-frame.png",
        "tolerance": 0
      }
    }
  ]
}
```

`scene` и `visual.reference` являются project-relative путями. `assertNodes.path` и `assertProperties.node` используют простой путь вида `/Root/Child`.

## CLI

Scene tests запускаются командой:

```powershell
e2d test --project . --format json
```

Команда переходит в scene-test режим, когда:

- выбран `--format json`;
- найден `tests/electron2d.scene-tests.json` или указан `--manifest <path>`.

Дополнительные параметры:

- `--manifest <path>` - project-relative путь к manifest;
- `--output <path>` - каталог artifacts, по умолчанию `.electron2d/test-artifacts/latest`;
- `--input-build-configuration-hash <hash>` - stable hash конфигурации тестового запуска.

`e2d test --format jsonl` без scene-test manifest сохраняет общий queued job mode, введённый CLI-задачами. Это важно для обратной совместимости текущего job contract.

## Artifacts

Output directory содержит:

```text
result.json
diagnostics.json
events.jsonl
screenshots/<test-name>-frame-XXXX.png
pixel-diff/<test-name>-diff.png
```

`result.json` использует schema `https://electron2d.dev/schemas/testing/scene-test-result.schema.json` и содержит:

- `succeeded`;
- `suite`;
- результаты tests;
- paths artifacts;
- `inputSnapshotId`, `inputWorkspaceRevision`, `inputContentRevision`, `inputDocumentRevisions`;
- `inputBuildConfigurationHash`.

`diagnostics.json` содержит structured diagnostics. Сейчас runner создаёт:

- `E2D-TEST-0001` для node/property assertion failure;
- `E2D-TEST-0002` для отсутствующего reference image или превышения visual tolerance.

`events.jsonl` пишет ordered progress events:

- `test.suiteStarted`;
- `test.started`;
- `test.frameAdvanced`;
- `test.screenshotCaptured`;
- `test.visualCompared`;
- `test.completed`;
- `test.suiteCompleted`.

## Текущий scope

Реализовано:

- project `src/Electron2D.Testing`;
- загрузка scene JSON из `nodes`;
- построение simple scene path по `name` и `parent`;
- поиск узла по `/Name` или `/Parent/Child`;
- проверка node type;
- чтение `properties.<name>.value`;
- deterministic frame advance без ожидания wall-clock;
- deterministic PNG capture для visual tests;
- byte-level comparison с reference PNG как первая замена будущего pixel diff;
- structured artifacts и published JSON schemas в `schemas/testing/`.

Ограничения текущего слоя:

- пользовательский C# runtime process ещё не запускается;
- physics, script lifecycle и настоящий rendering backend не исполняются;
- visual diff пока сравнивает стабильный PNG artifact байт-в-байт и пишет такой же PNG как diff output;
- Editor-панель прогресса и visual diff подключаются отдельными UI-задачами поверх этих artifacts.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~Electron2DSceneVisualTestingTests
```

Она покрывает runner, CLI `e2d test --format json`, published schemas и минимальный проект со сценой, property assertion и visual reference.
