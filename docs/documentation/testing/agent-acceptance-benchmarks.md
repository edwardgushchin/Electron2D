# Agent acceptance benchmarks

Статус: текущая реализация для `T-0128`.
Обновлено: 2026-06-23.
Связанные спецификации: [Agent acceptance benchmarks для Electron2D 0.1](../../specifications/testing/agent-acceptance-benchmarks.md), [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../../specifications/architecture/agent-native-workflow.md), [Electron2D 0.1.0 Preview](../../specifications/releases/0.1.0-preview.md).

## Что реализовано

Release gate для Agent-native workflow хранится в двух tracked файлах:

- `data/quality/agent-acceptance-benchmarks.json` - machine-readable manifest benchmark suites, scenarios и evidence.
- `tools/Run-AgentAcceptanceBenchmarks.ps1` - локальный runner, который читает manifest, проверяет referenced files и запускает evidence steps.

Runner не создаёт отдельную тестовую модель поверх продукта. Он связывает уже существующие проверки Editor, Tooling, MCP, Project Tasks, WorkspaceJob, WorkspaceTransaction, script/debug tooling и headless runtime automation в один release artifact.

## Команды

Показать список suite и сценариев:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-AgentAcceptanceBenchmarks.ps1 -List
```

Проверить manifest и создать plan без запуска тяжёлых smoke-команд:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-AgentAcceptanceBenchmarks.ps1 -DryRun -OutputDirectory .temp\agent-acceptance-benchmarks
```

Запустить полный benchmark:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-AgentAcceptanceBenchmarks.ps1 -OutputDirectory .temp\agent-acceptance-benchmarks
```

Запустить один suite:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-AgentAcceptanceBenchmarks.ps1 -Suite editor-co-development -OutputDirectory .temp\agent-acceptance-editor
powershell -ExecutionPolicy Bypass -File tools\Run-AgentAcceptanceBenchmarks.ps1 -Suite headless-ai -OutputDirectory .temp\agent-acceptance-headless
```

## Artifacts

`-DryRun` создаёт:

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
