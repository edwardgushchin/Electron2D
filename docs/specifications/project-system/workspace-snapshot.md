# WorkspaceSnapshot, job input identity и dirty export policy

Статус: целевая спецификация для `T-0153`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Live ProjectWorkspace](live-project-workspace.md); [Canonical document model, revision model и structural diff](canonical-document-model.md).

## Назначение

`WorkspaceSnapshot` фиксирует входное состояние открытого `ProjectWorkspace` для долгих операций: `build`, `test`, `run` и export dirty snapshot. Под долгой операцией здесь понимается действие, которое может завершиться позже, чем изменится открытый проект, и поэтому должно явно помнить, какие документы, ревизии и настройки были входом.

Snapshot нужен, чтобы dirty workspace не заставлял инструменты читать случайное старое состояние с диска. Если build toolchain или game process умеет читать только файлы, snapshot материализуется во временную директорию внутри project root, но это не является сохранением проекта.

## Snapshot identity

Каждый snapshot должен содержать:

- `SnapshotId` — stable id для links между job, diagnostics, screenshot, runtime tree и visual diff;
- `WorkspaceRevision` — ревизия всей workspace-сессии на момент snapshot;
- `ContentRevision` — ревизия игровых/project documents, влияющих на build/test/run/export;
- `DocumentRevisions` — path -> in-memory revision для всех открытых документов;
- `DirtyDocuments` — список dirty documents на момент snapshot;
- `OpenCodeBuffers` — открытые code buffers, которые могут ещё не совпадать с файлами на диске;
- `CreatedAt` — timestamp snapshot creation;
- immutable копию document text для materialization.

После создания snapshot не должен меняться, даже если `ProjectWorkspace` изменился.

## Open code buffers

`OpenCodeBuffers` — это snapshot-представление открытых `.cs` buffers. В `0.1.0 Preview` они считаются частью input snapshot для build/test/run, потому что compiler должен видеть то состояние кода, которое видел пользователь или AI-агент при запуске job.

Минимальное правило: любой открытый document с расширением `.cs` попадает в `OpenCodeBuffers` с path, text и revision. Будущая Script workspace может расширить это отдельной моделью code documents, но не должна ломать snapshot identity.

## Materialization

Если consumer требует файловую директорию, snapshot materializer пишет копию snapshot в:

```text
.electron2d/workspaces/<session-id>/<snapshot-id>/
```

Требования:

- `session-id` и `snapshot-id` нормализуются как безопасные имена сегментов path;
- все materialized files остаются внутри `.electron2d/workspaces/`;
- source files проекта не изменяются;
- `PersistedRevision` открытых документов не меняется;
- dirty state не очищается;
- рядом с файлами пишется `workspace-snapshot.json` с input identity;
- cleanup удаляет только materialized snapshot directory внутри `.electron2d/workspaces/`.

Materialization не должна silently drop unknown opened documents. Если path небезопасен или выходит за project root, операция должна fail-closed.

## Job input identity

Каждый job и каждый artifact, который относится к build/test/run/export diagnostics, screenshot, runtime tree или visual diff, должен хранить:

- `InputSnapshotId`;
- `InputWorkspaceRevision`;
- `InputContentRevision`;
- `InputDocumentRevisions`;
- `InputBuildConfigurationHash`.

`InputBuildConfigurationHash` — deterministic string, созданная из build/run/export configuration. Если конфигурация меняется после старта job, artifact становится stale даже при неизменных документах.

## Stale rules

Artifact считается `stale`, если после старта job изменилось хотя бы одно:

- `ContentRevision`;
- revision любого документа, который был в `InputDocumentRevisions`;
- `InputBuildConfigurationHash`.

Artifact не становится `stale` только из-за изменения `WorkspaceRevision`, если `ContentRevision`, входные document revisions и build/run configuration не изменились. Это важно для task status, `TaskActivity`, board rank, selection, diagnostics panel state и другой `EditorMetadata`, то есть редакторских данных, не влияющих на игровой build/test/run result.

## Export policy

Export default policy:

- по умолчанию export использует clean persisted state;
- если workspace dirty, default export должен вернуть отказ с объяснением и не сохранять документы;
- export dirty snapshot разрешён только при явном режиме `DirtySnapshot`;
- dirty snapshot export использует `WorkspaceSnapshot` и materialization, но не меняет source files, `PersistedRevision` и dirty state;
- release artifact, собранный из dirty snapshot, должен быть явно помечен как dirty snapshot input и не считаться clean release artifact без решения пользователя.

## Критерии приёмки

- Есть focused tests на создание immutable snapshot с `SnapshotId`, `WorkspaceRevision`, `ContentRevision`, `DocumentRevisions`, `DirtyDocuments`, `OpenCodeBuffers` и `CreatedAt`.
- Snapshot materialization пишет только в `.electron2d/workspaces/<session-id>/<snapshot-id>/`, создаёт `workspace-snapshot.json`, не меняет source files, persisted revisions или dirty state, и умеет безопасно cleanup.
- Job input identity содержит `InputSnapshotId`, `InputWorkspaceRevision`, `InputContentRevision`, `InputDocumentRevisions` и `InputBuildConfigurationHash`.
- Stale evaluator помечает artifact stale при изменении входного документа, `ContentRevision` или build configuration hash.
- Stale evaluator не помечает artifact stale при изменении только workspace metadata, которое не меняет `ContentRevision`.
- Default export policy отказывается экспортировать dirty workspace как clean persisted state.
- Dirty snapshot export требует явного режима и не сохраняет ручные dirty-документы.
- Implementation documentation описывает фактическое поведение и focused test command.
