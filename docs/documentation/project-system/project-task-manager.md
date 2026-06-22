# ProjectTaskManager

Статус: реализованная внутренняя основа.
Задача: `T-0154`.
Обновлено: 2026-06-23.

## Назначение

`ProjectTaskManager` реализован в `Electron2D.ProjectSystem` как внутренний слой задач пользовательского проекта. Внутренний слой означает код, доступный тестам и будущим Editor/Tooling/MCP-адаптерам, но ещё не публичный runtime API для игр.

Этот слой хранит задачи как first-class документы `ProjectWorkspace`, а не как Markdown-backlog. Пользовательские проекты должны использовать `.electron2d/tasks/*.e2task` и `.electron2d/tasks/board.e2tasks` как canonical task storage, а `TASKS.md`, `completed-tasks/` и `dev-diary/` остаются только рабочими файлами репозитория Electron2D.

## OperationContext

Для изменяющих task operations добавлен общий `OperationContext` в `Electron2D.ProjectSystem/Operations/`. Он содержит:

- `PrincipalId`;
- `PrincipalKind`;
- `SessionId`;
- `Capabilities`;
- `Origin`.

`PrincipalKind` описывает источник операции: `Human`, `Agent`, `Cli`, `ExternalFile`, `System` или `Test`. Полномочия задаются через `OperationCapability`: `TaskWrite`, `TaskSubmitForAcceptance`, `TaskAccept`, `TaskRequestChanges`, `TaskCancel` и `TaskReopen`.

`TaskActivityEntry.ActorId`, `TaskActivityEntry.ActorKind` и `TaskActivityEntry.CreatedAt` заполняются из доверенного `OperationContext` и текущих часов. Payload activity не может подменить эти audit-поля через текст вида `ActorId=...`, `ActorKind=...` или `CreatedAt=...`.

## Модель задач

Реализованы статусы:

- `Backlog`;
- `Ready`;
- `InProgress`;
- `Blocked`;
- `Review`;
- `AwaitingAcceptance`;
- `Done`;
- `Cancelled`.

`ProjectTask` хранит id, title, description, status, readiness, blocking reasons, priority, rank, labels, assignee, creator, parent, dependencies, acceptance criteria, subtasks, activity, links на transactions/jobs/diagnostics/artifacts/scenes/resources/nodes и audit timestamps.

`AcceptanceCriterion` хранит stable `CriterionId`, description, state и evidence links. `TaskActivityEntry` хранит stable `ActivityEntryId`, audit-поля, kind и payload. Поддержаны activity kinds `Comment`, `Decision`, `Investigation`, `Blocker`, `TestResult`, `StatusChange`, `AgentSummary` и `AcceptanceResult`.

## Приёмка и переходы статусов

`ProjectTaskManager.ChangeStatus(...)` применяет переходы через `WorkspaceTransactionEngine`. Текущий contract:

- `Review -> AwaitingAcceptance` доступен агенту с capability `TaskSubmitForAcceptance`;
- `AwaitingAcceptance -> Done` требует `PrincipalKind.Human` и capability `TaskAccept`;
- `AwaitingAcceptance -> InProgress` как request changes требует `PrincipalKind.Human` и capability `TaskRequestChanges`;
- `Done` и `Cancelled` можно открыть заново через `Reopen` только с `PrincipalKind.Human` и capability `TaskReopen`.

AI-агент не может поставить `Done` или принять задачу, даже если payload пытается объявить себя человеком. `Reopen` сохраняет прошлые `CompletedAt`, `AcceptedAt`, `AcceptedBy` и историческое acceptance state в activity, а текущий `AcceptanceState` становится `Reopened`.

## Storage и transaction integration

`ProjectTaskStorage.GetTaskDocumentPath(taskId)` возвращает `.electron2d/tasks/<taskId>.e2task`, а board document хранится в `.electron2d/tasks/board.e2tasks`.

`ProjectTaskSerializer` пишет deterministic JSON с `format = "Electron2D.TaskFile"` или `format = "Electron2D.TaskBoard"` и `version = 1`. `.e2task` и `.e2tasks` классифицируются как JSON `EditorMetadata`.

Новый проект из шаблона получает стартовую доску `.electron2d/tasks/board.e2tasks` и задачу `.electron2d/tasks/welcome.e2task`. Эти файлы создаёт `ProjectTemplateCreator`; дальнейшие изменения должны идти через Editor, Tooling или MCP, а не через прямую ручную правку JSON.

Task mutations:

- используют `WorkspaceTransactionEngine` в режиме `WorkspaceOnly`;
- проверяют `expectedRevision`;
- создают Undo group через `UndoGroupId`;
- помечают task document dirty;
- сохраняются через существующий режим `SaveAffectedDocuments`;
- возвращают structured diagnostics вместо тихой перезаписи.

External import task document проверяется task guard до применения transaction. Direct file edit, который пытается поставить `Done`, изменить accepted/audit поля или добавить activity с привилегированными audit-полями, отклоняется diagnostic `E2D-TASK-0002`; import state документа помечается как `pending-conflict`.

## Dependency graph

`TaskDependencyGraph` сейчас проверяет:

- добавление dependency, создающее цикл;
- readiness для незавершённых dependencies;
- readiness для отменённой dependency;
- сохранение ручного `Status = Blocked` и manual blocking reason после закрытия dependency.

Dependency-related блокировка обновляет только `Readiness` и `BlockingReasons`. Она не переводит задачу в `Ready` автоматически, если workflow-статус вручную оставлен `Blocked`.

## Diagnostics

Добавлены task diagnostics:

- `E2D-TASK-0002` — операция задач отклонена acceptance guard, privileged field guard или transition validator;
- `E2D-TASK-0003` — dependency graph требует внимания: цикл, незавершённая dependency или отменённая dependency.

## Текущие ограничения

Реализация закрывает core/domain/storage слой. Она не добавляет:

- визуальную доску `Tasks`;
- Tooling-команды;
- MCP tools/resources;
- Agent Workspace current task UI;
- Markdown-report export.

Эти возможности остаются в отдельных задачах и должны использовать текущий core contract.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ProjectTaskManagerTests
```

Проверка покрывает статусы и приёмку, request changes, reopen, audit-поля activity, storage round-trip, document classification, dependency graph, external import guard, workspace transaction semantics и сохранение dirty task document через `SaveAffectedDocuments`.
