# Agent acceptance benchmarks для Electron2D 0.1

Обновлено: 2026-06-30.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0128`.
Обновлено: 2026-06-30.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md), [Референс интерфейса редактора Godot 4](../editor/godot4-editor-reference.md), [Локальный MCP-сервер поверх active Editor session и Tooling](../mcp/mcp-server.md), [Editor session discovery и Editor-hosted Agent Gateway](../tooling/editor-session-discovery.md), [ProjectTaskManager, TaskActivity и task storage](../project-system/project-task-manager.md), [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md), [Human-AI concurrent editing, conflicts и grouped Undo](../project-system/concurrent-editing-and-undo.md), [Headless runtime automation](../runtime/headless-runtime-automation.md), [Script workspace и встроенная C# IDE](../scripting/editor-script-workflow.md).

## Назначение

Agent acceptance benchmarks являются release gate для обещания **Agent-native cross-platform 2D game engine**. Они не заменяют отдельные unit, integration, smoke и visual harness проверки. Их задача - собрать эти проверки в один воспроизводимый набор, показать coverage по критериям совместной разработки человека и AI, сохранить evidence artifacts и дать понятный итог для релиза.

Benchmark состоит из двух suite:

- `editor-co-development` - основной suite для открытого `Electron2D.Editor`, активной Editor-сессии, живого `ProjectWorkspace`, видимого окна, Project Tasks, runtime control, script/debug tooling и trusted human acceptance.
- `headless-ai` - вторичный suite для автономной работы агента через установленный Electron2D, CLI/MCP, локальную документацию и пустой проект без исходного кода движка и без управления Editor мышью.

## Manifest

Canonical manifest хранится в:

```text
data/quality/agent-acceptance-benchmarks.json
```

Формат верхнего уровня:

```json
{
  "format": "Electron2D.AgentAcceptanceBenchmarkManifest",
  "version": 1,
  "release": "0.1.0-preview",
  "suites": []
}
```

Каждый suite содержит:

- `id`;
- `title`;
- `mode`: `activeEditor` или `headless`;
- `targetSuccessRatio`;
- `releaseRequired`;
- `scenarios`;
- `evidence`;
- `successConditions`.

`scenarios` описывают проверяемое поведение простыми stable identifiers. `evidence` связывает сценарии с существующими focused tests, Editor smoke commands, visual screenshot analysis, manual harness или runner output. `successConditions` задают числовые и policy-гейты.

## Runner

Release gate runner доступен через:

```text
dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks
```

Runner обязан поддерживать:

- `--list` - вывести suite/scenario/evidence plan без запуска тяжёлых проверок;
- `--dry-run` - проверить manifest, существование referenced specs/docs/tests/scripts и сгенерировать plan artifact;
- обычный запуск - выполнить все автоматизированные evidence steps, сохранить результат и вернуть non-zero exit code при failure;
- `--suite editor-co-development` и `--suite headless-ai` для focused запуска;
- `--output <path>` для release artifact output.

Output directory содержит:

```text
agent-acceptance-benchmarks/
├── benchmark-result.json
├── benchmark-plan.json
├── logs/
└── artifacts/
```

`benchmark-result.json` содержит:

- `format = Electron2D.AgentAcceptanceBenchmarkResult`;
- `manifestVersion`;
- `release`;
- suite summaries;
- scenario summaries;
- evidence status;
- paths to screenshot/analysis artifacts for visible Editor checks;
- target success ratio result;
- failed diagnostics.

## Editor co-development suite

`editor-co-development` является release-required suite. Для `0.1.0 Preview` он считается успешным только при `targetSuccessRatio = 1.0`.

Обязательные сценарии:

- `active-editor-route`: CLI/MCP adapter находит активную Editor-сессию по project root и направляет changing commands в тот же `ProjectWorkspace`, а при закрытом Editor использует headless fallback.
- `created-script-visible`: созданный `.cs` файл появляется в FileSystem dock или `Script` workspace без ручного refresh.
- `scene-inspector-viewport-update`: изменение сцены добавляет node, обновляет Scene Tree, Inspector и viewport.
- `concurrent-editing-conflict-panel`: непересекающиеся изменения merge-ятся, конфликт одного свойства не теряет данные и создаёт conflict evidence.
- `visible-runtime-control`: агент запускает текущую сцену в видимом runtime, ставит pause, делает frame step, inject input, получает screenshot, runtime tree и diagnostics.
- `snapshot-artifact-stale-policy`: run/build/test artifacts имеют `InputSnapshotId`, `InputWorkspaceRevision`, `InputContentRevision`, `InputDocumentRevisions`, build/run configuration hash и становятся stale только при изменении игровых input-документов, content revision или build/run configuration.
- `task-metadata-not-stale`: изменение task status, `TaskActivity` или board rank после старта job не делает game artifact stale.
- `project-task-manager-links`: active task связывает transactions, jobs, diagnostics и artifacts.
- `agent-awaiting-acceptance-only`: агент может перевести task в `Awaiting Acceptance`, но не может поставить `Done`.
- `trusted-human-acceptance`: agent-originated payload не может подменить `ActorKind`/`PrincipalKind`, а interactive Editor user может accept/request changes.
- `project-tasks-board-manual-flow`: developer может создать задачу, выбрать active task, принять результат или запросить изменения вручную.
- `script-workspace-tooling`: агент применяет text edits через Tooling/MCP, получает diagnostics/completion без keyboard emulation, а developer видит изменения в центральном `Script` workspace.
- `editor-window-screenshot-analysis`: benchmark сохраняет screenshot окна `Electron2D.Editor` после видимых изменений и анализирует layout, доступность controls, отсутствие text overflow и соответствие reference layout.
- `managed-debugger-tooling`: агент ставит breakpoint, запускает scene под debugger, читает stacks всех threads, `locals`/`arguments` для явного `frameId`, получает watch definitions, вызывает `debug_evaluate_watches(frameId)` и продолжает выполнение.
- `structured-diagnostics-ai-fix`: structured diagnostics доступны агенту и связаны с исправлением ошибки.
- `grouped-ai-undo`: последняя AI transaction отменяется одной Undo-группой.
- `agent-crash-read-and-staged-transaction`: crash агента во время чтения и во время staged transaction освобождает session lease, сохраняет целостное состояние и оставляет проект доступным вручную.
- `editor-without-mcp`: Editor запускается и ручной workflow работает при отключённом MCP.

Обязательное visual evidence:

- хотя бы один evidence step должен создать real-window screenshot `Electron2D.Editor`;
- analysis JSON должен подтверждать `actualWindow = true`, `framePresented = true`, `pointerInteractionObserved = true`, `keyboardInteractionObserved = true`, `textOverflowCount = 0`, `forbiddenUiMatches = 0` и `screenshotReviewed = true`;
- visual step должен ссылаться на [Референс интерфейса редактора Godot 4](../editor/godot4-editor-reference.md).

## Headless AI suite

`headless-ai` является release-required suite, но до полной автоматизации допускается `documentedManualHarness`, если он хранит инструкции, входные задания, expected evidence и журнал результата. Целевой показатель для `0.1.0 Preview` - минимум `0.8`.

Suite содержит ровно пять эталонных заданий:

1. `create-project` - создать проект из установленного Electron2D.
2. `change-scene` - изменить сцену через CLI/MCP и сохранить валидный project format.
3. `implement-mechanic` - реализовать небольшую C# механику в пользовательском проекте.
4. `fix-diagnostic` - исправить ошибку по structured diagnostic.
5. `verify-and-build` - запустить проверки и собрать проект.

Условия успеха:

- агент не получает исходный код движка как input benchmark;
- агент не редактирует generated cache: `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/`, `.electron2d/user/`;
- агент не использует unsupported API из карты совместимости;
- успешной считается задача, где expected artifacts созданы, проверки зелёные, а forbidden paths/API violations отсутствуют;
- suite запускается минимум для двух разных AI-agent profiles либо имеет documented manual harness до автоматизации.

## Критерии приёмки

- Specification фиксирует два suite, target success ratio, manifest location, runner contract, visual evidence policy и пять headless заданий.
- Добавлен tracked manifest `data/quality/agent-acceptance-benchmarks.json`, где каждый scenario связан с evidence step.
- Добавлен C# runner `dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks` с `--list`, `--dry-run`, focused suite запуском, output artifacts и non-zero failure behavior.
- Automated test проверяет manifest schema, обязательные scenarios, headless tasks, referenced files, visual evidence requirements, stale policy coverage и runner contract.
- Focused red test падает до добавления manifest/runner и проходит после реализации.
- Runner dry run создаёт `benchmark-plan.json` и не запускает тяжёлые проверки.
- Минимальный automated run выполняет существующие focused integration/smoke checks для Editor co-development и headless runtime coverage.
- Implementation documentation в `docs/testing/` описывает текущий runner, manifest, evidence mapping, команды и ограничения manual harness.
- Release documentation или release gate documentation ссылается на benchmark как обязательную проверку `0.1.0 Preview`.

## Фактическое состояние, ограничения и проверки

Статус: текущая реализация для `T-0128`.
Обновлено: 2026-06-30.
Связанные спецификации: [Agent acceptance benchmarks для Electron2D 0.1](agent-acceptance-benchmarks.md), [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Что реализовано

Release gate для Agent-native workflow хранится в двух tracked файлах:

- `data/quality/agent-acceptance-benchmarks.json` - machine-readable manifest benchmark suites, scenarios и evidence.
- `dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks` - локальный C# runner, который читает manifest, проверяет referenced files и запускает evidence steps.

Runner не создаёт отдельную тестовую модель поверх продукта. Он связывает уже существующие проверки Editor, Tooling, MCP, Project Tasks, WorkspaceJob, WorkspaceTransaction, script/debug tooling и headless runtime automation в один release artifact.

## Команды

Показать список suite и сценариев:

```bash
dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks --list
```

Проверить manifest и создать plan без запуска тяжёлых smoke-команд:

```bash
dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks --dry-run --output .temp/agent-acceptance-benchmarks
```

Запустить полный benchmark:

```bash
dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks --output .temp/agent-acceptance-benchmarks
```

Запустить один suite:

```bash
dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks --suite editor-co-development --output .temp/agent-acceptance-editor
dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks --suite headless-ai --output .temp/agent-acceptance-headless
```

## Artifacts

`--dry-run` создаёт:

```text
benchmark-plan.json
```

Обычный запуск создаёт:

```text
benchmark-plan.json
benchmark-result.json
logs/<evidence-id>.log
artifacts/
```

`benchmark-result.json` использует формат `Electron2D.AgentAcceptanceBenchmarkResult`, содержит `release`, `manifestVersion`, итог `succeeded`, suite summaries, success ratio и evidence results. Если release-required suite не набрал свой `targetSuccessRatio`, runner возвращает non-zero exit code.

## Editor co-development suite

`editor-co-development` является обязательным release gate с целевым результатом `1.0`. Manifest связывает его сценарии с такими проверками:

- real-window smoke `EditorWindowSmokeRunCreatesRealWindowAndWritesVisualArtifacts`;
- active Editor discovery и MCP routing;
- FileSystem, Scene Tree, Inspector и Viewport smoke;
- Agent Workspace panel, Project Tasks board и shell layout smoke;
- Tooling service boundary, runtime control и ProjectTaskManager guards;
- WorkspaceJob stale markers, WorkspaceTransaction conflicts и grouped Undo;
- script/debug Tooling parity, managed debugger commands и visible script/debug smoke;
- agent bootstrap handshake, token expiry и disconnect state;
- Editor run workflow diagnostics.

Visual gate требует screenshot реального окна `Electron2D.Editor`, подтверждённый frame, pointer/keyboard interaction, `textOverflowCount = 0`, отсутствие forbidden UI matches и `screenshotReviewed = true`.

## Headless Manual Harness

`headless-ai` уже имеет automated evidence для headless runtime automation, context pack, task storage, generated cache guard и API manifest. До отдельной автоматизации двух разных AI-agent profiles допускается documented manual harness:

1. Подготовить чистый installed Electron2D build и пустую директорию проекта.
2. Дать агенту только installed CLI/MCP, локальную документацию, generated context pack и текст одного задания.
3. Выполнить пять заданий manifest: `create-project`, `change-scene`, `implement-mechanic`, `fix-diagnostic`, `verify-and-build`.
4. Зафиксировать для каждого задания входной prompt, agent profile, команды проверки, созданные artifacts и итог pass/fail.
5. Проверить, что агент не редактировал `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/`, `.electron2d/user/`.
6. Проверить, что агент не использовал unsupported API по API manifest и compatibility data.
7. Повторить для второго AI-agent profile либо явно отметить, что release использует documented manual harness до автоматизации.

Manual результат не делает задачу принятой за разработчика. Он только является evidence для release gate, пока двух-agent automation не закрыта отдельной задачей.

## Ограничения

Runner выполняет evidence steps последовательно, чтобы не перегружать рабочую станцию real-window smoke и сборками. Он не скрывает failures: non-zero exit code любого required automated evidence снижает success ratio suite.

`documentedManualHarness` считается документированным evidence только если эта страница существует и manifest ссылается на неё. Он не заменяет automated Editor suite и не может поднять `editor-co-development` до pass.
