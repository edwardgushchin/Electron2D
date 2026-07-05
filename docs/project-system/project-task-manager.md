# ProjectTaskManager, TaskActivity и task storage

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0154`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1-preview](../releases/0.1-preview.md); [Live ProjectWorkspace](live-project-workspace.md); [WorkspaceTransactionEngine и безопасные project operations](workspace-transactions.md); [WorkspaceJob contract и event stream](workspace-jobs.md).

## Назначение

`ProjectTaskManager` задаёт встроенное состояние задач пользовательского проекта. Он заменяет Markdown-backlog файлы внутри проектов пользователя: шаблоны Electron2D не должны создавать `TASKS.md`, `completed-tasks/` или `dev-diary/` как источник истины для игры или приложения.

Эта спецификация описывает только core/domain/storage слой, то есть модель данных, правила переходов, проверку полномочий и текстовое хранилище задач. Полноценная доска `Tasks`, Tooling-команды, MCP tools/resources, project template, Agent Workspace и Markdown-report реализуются отдельными задачами поверх этого слоя.

## Состав слоя

Обязательные компоненты:

- `ProjectTaskManager` — точка входа для создания и изменения задач в открытом `ProjectWorkspace`;
- `TaskStore` — загрузка, поиск и сохранение task documents;
- `TaskActivityStore` — добавление смысловых записей выполнения;
- `TaskDependencyGraph` — проверка зависимостей и readiness;
- `TaskTransitionValidator` — проверка допустимых переходов статусов;
- `TaskAcceptanceService` — приёмка, возврат на доработку, cancel и reopen;
- `TaskBoard` — порядок задач по колонкам через stable rank или sort key.

`OperationContext` — доверенный контекст операции, созданный Editor, Tooling host или MCP gateway. Он хранит `PrincipalId`, `PrincipalKind`, `SessionId`, `Capabilities` и `Origin`. Payload задачи не может сам объявить эти значения как полномочия.

## Статусы и переходы

Поддерживаются статусы:

- `Backlog`;
- `Ready`;
- `InProgress`;
- `Blocked`;
- `Review`;
- `AwaitingAcceptance`;
- `Done`;
- `Cancelled`.

Допустимые переходы:

| Текущий статус | Разрешённые переходы |
| --- | --- |
| `Backlog` | `Ready`, `Cancelled` |
| `Ready` | `Backlog`, `InProgress`, `Blocked`, `Cancelled` |
| `InProgress` | `Ready`, `Blocked`, `Review`, `Cancelled` |
| `Blocked` | `Ready`, `InProgress`, `Cancelled` |
| `Review` | `InProgress`, `AwaitingAcceptance`, `Blocked` |
| `AwaitingAcceptance` | `InProgress`, `Done` |
| `Done` | `Reopen` в `Ready` |
| `Cancelled` | `Reopen` в `Backlog` |

`Review` означает техническую проверку результата. `AwaitingAcceptance` означает ожидание решения разработчика. `Done` означает, что разработчик принял результат. `Request changes` возвращает задачу в `InProgress`. `Cancel` переводит задачу в `Cancelled`, когда работа больше не нужна. `Reopen` является действием человека, а не отдельным статусом.

AI-агент может перевести задачу в `AwaitingAcceptance`, если его `OperationContext` содержит capability `Task.SubmitForAcceptance`. AI-агент не может установить `Done`, даже если payload пытается объявить `ActorKind = Human` или `PrincipalKind = Human`. `Task.Accept` и `Task.RequestChanges` требуют capability `Task.Accept` или `Task.RequestChanges`, выданной interactive Editor user context или краткоживущим подтверждением Editor UI.

## Модель задачи

`ProjectTask` должен хранить:

- `TaskId`;
- `Title`;
- `Description`;
- `Status`;
- `Readiness`;
- `BlockingReasons`;
- `Priority`;
- `Rank` или `SortKey`;
- `Labels`;
- `Assignee`;
- `CreatedBy`;
- `ParentTaskId`;
- `Dependencies`;
- `AcceptanceCriteria`;
- `Subtasks`;
- `Activity`;
- `LinkedTransactions`;
- `LinkedJobs`;
- `LinkedDiagnostics`;
- `LinkedArtifacts`;
- `LinkedScenesResourcesAndNodes`;
- `CreatedAt`;
- `UpdatedAt`;
- `SubmittedAt`;
- `CompletedAt`;
- `AcceptedAt`;
- `AcceptedBy`;
- `AcceptanceState`;
- `ArchivedAt`;
- `ArchivedBy`;
- `CancellationReason`.

`AcceptanceCriterion` имеет `CriterionId`, `Description`, `State` и `EvidenceLinks`. Criteria имеют stable UID, чтобы независимые добавления не конфликтовали как правка одного массива.

`TaskActivityEntry` имеет `ActivityEntryId`, `ActorId`, `ActorKind`, `CreatedAt`, `Kind` и `Payload`. Поддерживаются виды `Comment`, `Decision`, `Investigation`, `Blocker`, `TestResult`, `StatusChange`, `AgentSummary` и `AcceptanceResult`.

`ActorId`, `ActorKind` и `CreatedAt` являются audit-полями, то есть полями происхождения записи. Их заполняет `TaskActivityStore` из доверенного `OperationContext` и системных часов. Вызывающий агент, CLI или MCP payload может передать только смысловой `Kind` и `Payload`; он не может передать или перезаписать audit-поля как обычные данные.

## Зависимости и readiness

`Readiness` хранит состояние готовности отдельно от workflow-статуса:

- `Ready`;
- `BlockedByDependencies`;
- `DependencyCancelled`.

`BlockingReasons` хранит причины:

- `dependency`;
- `environment`;
- `decision`;
- `external`;
- `manual`.

`TaskDependencyGraph` обязан:

- запрещать циклы;
- не переводить задачу в `Ready` автоматически, если обязательная dependency ещё не завершена;
- возвращать structured diagnostic с диагностируемой причиной blocked-состояния;
- после завершения dependency обновлять только dependency-related `BlockingReasons` и `Readiness`;
- не снимать ручной `Status = Blocked`;
- при отмене dependency переводить readiness зависимой задачи в `DependencyCancelled`, оставляя workflow-статус неизменным, и возвращать diagnostic.

## Хранилище

Каноническое хранилище задач — стабильные текстовые metadata-документы проекта:

```text
.electron2d/
└── tasks/
    ├── task-01J....e2task
    ├── task-01K....e2task
    └── board.e2tasks
```

Task document использует `format = "Electron2D.TaskFile"` и schema version. Board document использует `format = "Electron2D.TaskBoard"` и schema version. Оба формата должны иметь canonical formatting, stable UID, migrations и небольшой diff.

`.electron2d/tasks/**` является `EditorMetadata`: эти документы доступны Editor, Tooling, CLI и MCP, но не импортируются как игровые ресурсы, не попадают в production asset packs, APK, AAB, app bundle или desktop distribution и не материализуются в runtime snapshot как игровые файлы.

Completed tasks являются представлением `Status = Done ORDER BY CompletedAt DESC`, а не отдельной папкой или архивом. Экспорт task data во внешний отчёт возможен только отдельной явной report-командой, описанной в [Markdown report export для Project Tasks](project-tasks-markdown-report-export.md); такой отчёт не обязателен для `0.1-preview` и не становится canonical storage.

## Transaction integration и external import

Task и board documents являются first-class документами `ProjectWorkspace`. Изменяющие операции должны:

- проходить через `WorkspaceTransactionEngine`;
- требовать `expectedRevision`;
- участвовать в dirty state;
- поддерживать `SaveAffectedDocuments`;
- создавать grouped Undo/Redo через `UndoGroupId`;
- возвращать conflicts и diagnostics вместо перезаписи данных.

Любое изменение task document, включая Tooling, CLI, MCP, `ExternalImport`, migration и crash recovery, проходит через `TaskTransitionValidator` и `TaskAcceptanceService`.

Непривилегированный payload не может изменить `CreatedBy`, `CreatedAt`, `UpdatedAt`, `SubmittedAt`, `CompletedAt`, `AcceptedAt`, `AcceptedBy`, `AcceptanceState`, `ArchivedAt`, `ArchivedBy`, `TaskActivityEntry.ActorId`, `TaskActivityEntry.ActorKind` или `TaskActivityEntry.CreatedAt`.

Попытка direct file edit поставить `Done`, выполнить `Done -> Ready` или `Cancelled -> Backlog` без trusted command должна вернуть structured diagnostic и оставить import в conflict/pending state. Такая проверка нужна даже для headless import, чтобы прямое редактирование файла не обходило приёмку.

## Связи с операциями и artifacts

Агентские операции могут связываться с:

- `TaskId`;
- `AgentSessionId`;
- `OperationId`;
- `TransactionId`;
- `SnapshotId`;
- `JobId`.

Task storage не копирует activity в build directory. Job может хранить `TaskId`, но по умолчанию не дублирует содержимое задачи в build/test/run artifacts.

## Критерии приёмки

- Есть focused tests на статусы, допустимые переходы, `Review`/`AwaitingAcceptance`, `Request changes`, `Cancel` и `Reopen`.
- Есть focused tests на acceptance guard: AI может отправить задачу в `AwaitingAcceptance`, но не может установить `Done`; human context с нужной capability может принять или вернуть задачу.
- Есть focused tests на сохранение истории при `Reopen`: `CompletedAt`, `AcceptedAt`, `AcceptedBy` и прошлое `AcceptanceState` не удаляются, а activity получает запись о reopen.
- Есть focused tests на task model, `AcceptanceCriterion` stable UID и `TaskActivityEntry` audit fields, которые заполняются из `OperationContext`.
- Есть focused tests на storage round-trip для `.electron2d/tasks/*.e2task` и `.electron2d/tasks/board.e2tasks`, canonical formatting, schema version и document classification как `EditorMetadata`.
- Есть focused tests на transaction semantics: `expectedRevision`, dirty state, grouped Undo/Redo, `SaveAffectedDocuments` и conflict diagnostics.
- Есть focused tests на external import guard для привилегированных полей и direct file edit попыток поставить `Done` или сделать privileged reopen.
- Есть focused tests на dependency graph: cycle rejection, blocked readiness, завершение dependency без снятия manual blocker и cancelled dependency diagnostic.
- Implementation documentation описывает фактическое поведение, текущие ограничения и focused test command.

## Фактическое состояние, ограничения и проверки

Статус: реализованная внутренняя основа.
Задача: `T-0154`.
Обновлено: 2026-06-23.

## Назначение

`ProjectTaskManager` реализован в `Electron2D.ProjectSystem` как внутренний слой задач пользовательского проекта. Внутренний слой означает код, доступный тестам и будущим Editor/Tooling/MCP-адаптерам, но ещё не публичный runtime API для игр.

Этот слой хранит задачи как first-class документы `ProjectWorkspace`, а не как Markdown-backlog. Пользовательские проекты должны использовать `.electron2d/tasks/*.e2task` и `.electron2d/tasks/board.e2tasks` как canonical task storage, а `TASKS.md`, `data/completed-tasks/` и `data/dev-diary/` остаются только рабочими файлами репозитория Electron2D.

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

## Markdown report export

`e2d tasks export` реализует внешний Markdown-отчёт поверх текущего task storage. Команда читает `.electron2d/tasks/*.e2task`, фильтрует задачи по status, labels/conventions для milestone, version, epic и agent session, а также по assignee, и пишет deterministic Markdown в stdout.

Отчёт не является хранилищем задач. `.electron2d/tasks/*.e2task` и `.electron2d/tasks/board.e2tasks` остаются canonical storage, то есть единственным проектным источником истины. `tasks export` не создаёт и не обновляет `TASKS.md`, `completed-tasks/` или `dev-diary/` в пользовательском проекте.

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

- Tooling-команды;
- MCP tools/resources.

Визуальная доска `Tasks` и Agent Workspace current task UI уже существуют как model-first UI snapshot и visual harness в `Electron2D.Editor`; постоянная live-привязка к desktop event loop и MCP-проверка остаются следующими слоями поверх текущего core contract.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ProjectTaskManagerTests
```

Проверка покрывает статусы и приёмку, request changes, reopen, audit-поля activity, storage round-trip, document classification, dependency graph, external import guard, workspace transaction semantics и сохранение dirty task document через `SaveAffectedDocuments`.

Focused проверка Markdown report export:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~TasksExportWritesStableMarkdownReportWithoutCreatingWorkflowFiles
```

Проверка покрывает exact Markdown output, фильтры `status`/`milestone`/`version`/`epic`/`assignee`/`agent-session` и отсутствие `TASKS.md`, `completed-tasks/`, `dev-diary/` в пользовательском проекте.
