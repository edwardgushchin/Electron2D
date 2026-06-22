# WorkspaceJob contract и event stream

Статус: целевая спецификация для `T-0147`.
Обновлено: 2026-06-22.
Связанные документы: [AI-friendly workflow Electron2D 0.1](../architecture/ai-friendly-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Live ProjectWorkspace](live-project-workspace.md); [WorkspaceSnapshot, job input identity и dirty export policy](workspace-snapshot.md); [Diagnostics.Core](../diagnostics/diagnostics-core.md).

## Назначение

`WorkspaceJob` задаёт общий внутренний контракт для долгих операций project-system слоя: import, build, test, export и run. Под долгой операцией здесь понимается действие, которое имеет lifecycle, progress, diagnostics, artifacts и может завершиться после того, как открытый `ProjectWorkspace` уже изменился.

Этот слой не запускает реальные toolchains и не является UI, CLI или MCP adapter. Он должен предоставить in-memory модель job и поток событий, который будущие адаптеры смогут отдать как CLI JSONL, MCP events, Editor panels или Agent Workspace без повторного определения состояний и stale rules.

## Обязательная модель job

Каждый job должен хранить:

- `OperationId` — стабильный id операции, общий для job record, events, diagnostics и artifacts;
- `InputSnapshotId`;
- `InputWorkspaceRevision`;
- `InputContentRevision`;
- `InputDocumentRevisions`;
- `InputBuildConfigurationHash`;
- `Kind`;
- `State`;
- `Progress`;
- `CanCancel`;
- `Diagnostics`;
- `Artifacts`;
- `StartedAt`;
- `CompletedAt`;
- `Stale`.

Input-поля берутся из `WorkspaceJobInputIdentity`, созданного из `WorkspaceSnapshot`. `InputBuildConfigurationHash` должен быть непустой deterministic строкой, созданной вызывающей подсистемой из build/run/export configuration.

`Kind` поддерживает значения:

- `Import`;
- `Build`;
- `Test`;
- `Export`;
- `Run`.

`State` поддерживает значения:

- `Queued`;
- `Running`;
- `Succeeded`;
- `Failed`;
- `Cancelled`.

Новый job создаётся в `Queued` с progress `0`. `StartedAt` заполняется при переходе в `Running`. `CompletedAt` заполняется только в terminal states: `Succeeded`, `Failed` или `Cancelled`. Terminal job не принимает новые progress updates, diagnostics, artifacts или повторный cancel.

`Progress` должен быть в диапазоне `0..1`. Пока job не завершён, progress не должен уменьшаться. Успешное завершение устанавливает progress `1`. Failed и cancelled job сохраняют последний известный progress, чтобы consumer не видел ложное стопроцентное выполнение.

## Event stream

`WorkspaceJobEventStream` первой реализации является in-memory publisher/subscriber. Он должен:

- сохранять порядок публикации;
- доставлять события fake consumers в tests;
- не зависеть от UI, CLI, MCP или внешнего процесса;
- публиковать immutable event payload, достаточный для будущего JSONL/MCP adapter без повторного чтения job store.

Обязательные event kinds:

- `operation.started` — job перешёл из `Queued` в `Running`;
- `operation.progress` — progress изменился;
- `operation.diagnostic` — добавлена structured diagnostic;
- `operation.artifactProduced` — добавлен artifact;
- `operation.completed` — job перешёл в `Succeeded`, `Failed` или `Cancelled`.

Событие должно содержать `OperationId`, `Kind`, `State`, `Progress`, `CanCancel`, input identity, `StartedAt`, `CompletedAt`, `Stale`, а также diagnostic или artifact, если событие относится к ним.

## Cancellation

Cancel/stop должен проходить через job model, а не через отдельный boolean flag.

Если job cancellable и находится в `Queued` или `Running`, cancel:

1. добавляет structured diagnostic с кодом `E2D-TOOLING-0001`;
2. публикует `operation.diagnostic`;
3. переводит job в `Cancelled`;
4. сбрасывает `CanCancel` в `false`;
5. заполняет `CompletedAt`;
6. публикует `operation.completed`.

Если job уже terminal или `CanCancel = false`, cancel должен fail-closed: state не меняется, artifacts не добавляются, а caller получает structured diagnostic с объяснением отказа. Такой отказ не обязан публиковаться в event stream как часть job, потому что сама job не изменилась.

## Diagnostics и artifacts

Diagnostics используют `StructuredDiagnostic` из `Diagnostics.Core`. Job contract не должен принимать plain string errors как результат операции.

Artifact должен хранить:

- `ArtifactKind` — например `build-log`, `screenshot`, `runtime-tree`, `test-results`;
- `InputIdentity`;
- `Stale`.

`Artifact.Stale` вычисляется по тем же правилам, что и `WorkspaceJob.Stale`: artifact stale, если изменился входной документ, `ContentRevision` или build/run/export configuration hash. Изменение только workspace metadata, task activity, selection или diagnostics panel state не делает artifact stale.

## Stale rules

Job stale flag вычисляется через `WorkspaceSnapshotStalenessEvaluator`:

- stale, если `InputBuildConfigurationHash` отличается от текущего configuration hash;
- stale, если `InputContentRevision` отличается от текущей `ContentRevision`;
- stale, если revision любого документа из `InputDocumentRevisions` отличается от текущего значения или документ больше не открыт в workspace;
- не stale, если изменился только `WorkspaceRevision`, а content revision, input document revisions и configuration hash прежние.

Consumer должен иметь возможность запросить актуализацию stale flag после workspace changes. Эта операция не меняет state job и не публикует progress/completed events; она только обновляет stale marker в job/artifacts для последующего чтения.

## Критерии приёмки

- Есть focused tests на lifecycle `Queued -> Running -> Succeeded`, progress events и immutable input identity.
- Есть focused tests на failed completion с structured diagnostic.
- Есть focused tests на cancellation: cancellable job публикует diagnostic и completed event со state `Cancelled`; non-cancellable или terminal job возвращает structured diagnostic без изменения state.
- Есть fake consumer tests, подтверждающие ordered events `operation.started`, `operation.progress`, `operation.diagnostic`, `operation.artifactProduced`, `operation.completed`.
- Есть tests, подтверждающие, что diagnostics и artifacts хранят `OperationId`, input identity и stale flag.
- Есть tests на stale marking: изменение content/input document/build configuration делает job и artifacts stale, а metadata-only workspace change не делает их stale.
- Implementation documentation описывает фактическое поведение, ограничения первой реализации и focused test command.
