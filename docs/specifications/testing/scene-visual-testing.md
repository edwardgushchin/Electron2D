# Scene tests и visual regression tests

Статус: целевая спецификация для `T-0122`.
Обновлено: 2026-06-22.
Связанные документы: [AI-friendly workflow Electron2D 0.1](../architecture/ai-friendly-workflow.md), [Headless runtime automation](../runtime/headless-runtime-automation.md), [WorkspaceSnapshot, job input identity и dirty export policy](../project-system/workspace-snapshot.md), [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md).

## Назначение

Scene/visual testing framework — это проверяемый слой для автоматической проверки поведения игры без ручного управления Editor. Он нужен CI, автономным агентам, будущему Editor test panel и Agent Workspace. Отображение progress, diagnostics и visual diff в Editor подключается отдельными UI-задачами поверх тех же machine-readable artifacts.

Первая реализация должна быть deterministic и same-platform reproducible. Она может использовать текстовое scene document и стабильный frame artifact из headless runtime foundation, пока полноценный runtime process, rendering backend, physics и script execution подключаются последующими задачами.

## Test suite manifest

Проект может содержать manifest:

```text
tests/electron2d.scene-tests.json
```

Минимальный формат:

```json
{
  "format": "Electron2D.SceneTestSuite",
  "version": 1,
  "tests": [
    {
      "name": "player_exists",
      "scene": "scenes/main.scene.json",
      "frames": 2,
      "fixedDelta": 0.5,
      "assertNodes": [
        { "path": "/Player", "type": "Electron2D.Node2D" }
      ],
      "assertProperties": [
        { "node": "/Player", "property": "speed", "equals": 10 }
      ],
      "visual": {
        "captureFrame": 2,
        "reference": "tests/references/player-frame.png",
        "tolerance": 0
      }
    }
  ]
}
```

Правила:

- `name` должен быть стабильным и уникальным внутри suite;
- `scene` — project-relative путь к scene JSON;
- `frames` больше `0`;
- `fixedDelta` больше `0`;
- `assertNodes.path` использует простой scene path вида `/Root/Child`;
- `assertProperties.equals` поддерживает bool, number, string и null в первой реализации;
- `visual.reference` — project-relative путь к reference frame;
- `visual.tolerance` задаёт допустимую долю отличий `0..1`.

## CLI

`e2d test --format json` запускает scene test suite, когда в project root найден `tests/electron2d.scene-tests.json` или явно указан `--manifest <path>`.

Пример:

```powershell
e2d test --project . --format json --output artifacts/tests
```

Параметры:

- `--manifest <path>` — project-relative путь к suite manifest; по умолчанию `tests/electron2d.scene-tests.json`.
- `--output <path>` — каталог test artifacts; по умолчанию `.electron2d/test-artifacts/latest`.
- `--input-build-configuration-hash <hash>` — hash build/test configuration для snapshot identity.

`e2d test --format jsonl` без scene-test manifest сохраняет generic queued job mode из CLI contract.

## Artifacts

Output directory получает:

```text
result.json
diagnostics.json
events.jsonl
screenshots/<test-name>-frame-XXXX.png
pixel-diff/<test-name>-diff.png
```

`result.json` содержит:

- `schemaVersion`;
- `$schema`;
- `succeeded`;
- `suite`;
- test summaries;
- artifact paths;
- snapshot identity fields.

`diagnostics.json` содержит structured diagnostics для failed assertions, missing references, invalid manifest и timeout.

`events.jsonl` содержит ordered progress events:

- `test.suiteStarted`;
- `test.started`;
- `test.frameAdvanced`;
- `test.screenshotCaptured`;
- `test.visualCompared`;
- `test.completed`;
- `test.suiteCompleted`.

Каждый event должен содержать `schemaVersion`, `event`, `testName` где применимо, progress fields и snapshot identity или ссылку на suite result identity.

## Scene assertions

Первая реализация должна:

- загрузить scene JSON;
- построить простой scene tree по `nodes`;
- найти узел по `/Name` или `/Parent/Child`;
- проверить type узла;
- прочитать serializable property value из `properties.<name>.value`;
- advance frame count детерминированно без wall-clock ожидания.

Если assertion не проходит, test suite завершается `succeeded = false`, а diagnostic содержит stable code, message, scene path и test name.

## Visual comparison

Первая реализация создаёт deterministic frame capture и сравнивает его с reference image. Пока настоящий renderer не подключён, capture может быть минимальным PNG artifact, но comparison result должен уже иметь будущий формат:

- `reference`;
- `actual`;
- `diff`;
- `differenceRatio`;
- `tolerance`;
- `passed`.

Если reference отсутствует, test fails fail-closed с structured diagnostic. Если reference есть и difference ratio не превышает tolerance, test passes. Pixel-diff artifact создаётся всегда для visual test.

## JSON schemas

Published schemas:

- `schemas/testing/scene-test-suite.schema.json`;
- `schemas/testing/scene-test-result.schema.json`;
- `schemas/testing/scene-test-diagnostics.schema.json`;
- `schemas/testing/scene-test-events.schema.json`.

## Критерии приёмки

- Создан project/package `Electron2D.Testing`.
- Scene tests загружают scene JSON, находят node, читают property и детерминированно advance frames.
- Visual tests сравнивают captured frame с reference image по tolerance и создают diff artifact.
- `e2d test --format json` запускает project scene tests при найденном или указанном manifest.
- Test artifacts содержат progress events, diagnostics, screenshots, pixel-diff output и snapshot identity.
- Добавлены sample integration tests для минимального проекта.
- Implementation documentation в `docs/documentation/testing/` описывает фактический scope, manifest, artifacts и команды проверки.
