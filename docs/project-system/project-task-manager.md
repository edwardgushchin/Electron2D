# ProjectTaskManager, TaskActivity и task storage

Обновлено: 2026-07-13.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: рабочий CLI-only TaskBoard v3 для актуализированной `T-1147`; v2 сохранён только как вход детерминированной миграции. Нижний раздел «Контракт TaskBoard v3» нормативен; ранние v2-разделы сохраняют исторический migration context.
Обновлено: 2026-07-13.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1-preview](../releases/0.1-preview.md); [Live ProjectWorkspace](live-project-workspace.md); [WorkspaceTransactionEngine и безопасные project operations](workspace-transactions.md); [WorkspaceJob contract и event stream](workspace-jobs.md).

## Назначение

`ProjectTaskManager` задаёт единое состояние задач для самого репозитория Electron2D, шаблонов и пользовательских проектов. Каноническое состояние хранится в корневой `.taskboard`, а legacy `TASKS.md`, `data/completed-tasks/**` и `.electron2d/tasks/**` больше не являются источниками истины.

Публичная граница изменения задач — `e2d tasks`. VS Code, агенты и другие внешние клиенты не записывают `.e2task`, `.e2tasks` или attachment metadata напрямую. `ProjectTaskManager` и общий command application остаются внутренней реализацией, которую используют CLI и адаптеры Electron2D без появления второго публичного writer.

`data/dev-diary/**` остаётся отдельным append-only журналом сессий репозитория. Он не хранит статус задачи и не заменяет `TaskActivity`.

Эта спецификация описывает core/domain/storage, CLI mutation boundary, миграцию и правила безопасности. UX VS Code закреплён в [Project Tasks board редактора](../editor/project-tasks-board.md), а команды — в [`e2d` CLI](../cli/e2d-cli.md).

## Состав слоя

Обязательные компоненты:

- `ProjectTaskManager` — точка входа для создания и изменения задач в открытом `ProjectWorkspace`;
- `TaskStore` — загрузка, поиск и сохранение task documents;
- `TaskActivityStore` — добавление смысловых записей выполнения;
- `TaskDependencyGraph` — проверка зависимостей и readiness;
- `TaskTransitionValidator` — проверка допустимых переходов статусов;
- `TaskAcceptanceService` — приёмка, возврат на доработку, cancel и reopen;
- `TaskBoard` — placements, stable rank и группировка задач без дублирования task status;
- `TaskBoardStore` — canonical disk layout, cross-process lock, revisions, atomic transaction journal и recovery;
- `TaskMigrationService` — fail-closed миграция v1 и legacy Markdown с source-digest/parity report;
- `TaskAttachmentStore` — безопасное копирование, хеширование, квоты и удаление attachment blobs.

`OperationContext` — доверенный контекст операции, созданный Editor, Tooling host или MCP gateway. Он хранит `PrincipalId`, `PrincipalKind`, `SessionId`, `Capabilities` и `Origin`. Payload задачи не может сам объявить эти значения как полномочия.

`e2d tasks` является единственным публичным writer. Внутренний command application обязан использовать те же validators и transaction store для CLI, Tooling/MCP и IDE adapters; direct JSON writer в каждом adapter запрещён.

## Статусы и переходы

Поддерживаются статусы:

- `Ready`;
- `InProgress`;
- `Blocked`;
- `Review`;
- `Done`;
- `Cancelled`.

Допустимые переходы:

| Текущий статус | Разрешённые переходы |
| --- | --- |
| `Ready` | `InProgress`, `Blocked`, `Cancelled` |
| `InProgress` | `Ready`, `Blocked`, `Review`, `Cancelled` |
| `Blocked` | `Ready`, `InProgress`, `Cancelled` |
| `Review` | `InProgress`, `Blocked`, `Done` только через human `accept` |
| `Done` | `Reopen` в `Ready` |
| `Cancelled` | `Reopen` в `Ready` |

`Review` объединяет техническую проверку результата и ожидание решения разработчика. Эти фазы различает `AcceptanceState`: `Open` означает внутреннюю проверку, `Submitted` — ожидание решения человека. `Done` означает, что разработчик принял результат. `Request changes` возвращает задачу в `InProgress`. `Cancel` переводит задачу в `Cancelled`, когда работа больше не нужна. `Reopen` является действием человека, а не отдельным статусом.

AI-агент может выполнить `submit` для задачи в `Review`, если его `OperationContext` содержит capability `Task.SubmitForAcceptance`: status остаётся `Review`, а `AcceptanceState` становится `Submitted`. AI-агент не может установить `Done`, даже если payload пытается объявить `ActorKind = Human` или `PrincipalKind = Human`. `Task.Accept` и `Task.RequestChanges` разрешены только для `Review` + `Submitted` и требуют capability `Task.Accept` или `Task.RequestChanges`, выданной interactive Editor user context или краткоживущим подтверждением Editor UI.

## Модель задачи

`ProjectTask` schema v2 должен хранить:

- `TaskUid` — immutable UID, не меняющийся при переименовании человекочитаемого ID;
- `TaskId`;
- `LegacyAliases`;
- `Title`;
- `Description`;
- `Status`;
- `ManualBlockingReasons`;
- `Priority`;
- `Labels` — устойчивые ссылки на теги из общего каталога доски;
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
- `ExecutionContract`;
- `Attachments`;
- `LegacySourceFragments`;
- `Revision`;
- `CreatedAt`;
- `UpdatedAt`;
- `Deadline` — необязательная календарная дата выполнения;
- `SubmittedAt`;
- `CompletedAt`;
- `AcceptedAt`;
- `AcceptedBy`;
- `AcceptanceState`;
- `ArchivedAt`;
- `ArchivedBy`;
- `CancellationReason`.

`Readiness` не дублируется как изменяемое поле schema v2: command application вычисляет `Ready`, `BlockedByDependencies` или `DependencyCancelled` из текущего набора active/completed задач. В task file хранятся только зависимости и явные manual/environment/decision/external blockers. Новая задача и `reopen` по умолчанию получают `Status = Ready`. Status и acceptance history принадлежат task file; rank и group placement принадлежат board file.

`TaskExecutionContract` структурирует `TaskType`, `ReadyToStart`, `StopConditions`, `AllowedChanges`, `ForbiddenChanges`, `RequiredOutputs`, `RequiredCommands` и `ExternalAudit`. Поля допускают Markdown-текст, но имеют стабильные ключи и массивы, чтобы verifiers не разбирали произвольные заголовки. `LinkedArtifacts` хранит только project-relative file/directory references и URI артефактов; shell command не является artifact. Команды принадлежат только `ExecutionContract.RequiredCommands`.

`TaskAttachment` хранит `AttachmentId`, `DisplayName`, project-relative `RelativePath`, `MediaType`, `ByteLength`, `Sha256`, `AddedAt` и `AddedBy`. Blob не кодируется в JSON. По умолчанию один файл ограничен 25 MiB, а суммарный объём `.taskboard/attachments` — 250 MiB.

Задача может хранить необязательный `PreviewAttachmentId` — ссылку на растровое вложение, выбранное пользователем для обложки карточки. Если ссылка не задана, эффективной обложкой становится первое растровое вложение по устойчивому порядку `AttachmentId`. Если изображений нет, обложки нет. Разрешены только `image/png`, `image/jpeg`, `image/gif`, `image/webp` и `image/bmp`; связанный файл из `LinkedArtifacts`, SVG, HTML, архив или произвольный путь обложкой быть не может.

CLI назначает и сбрасывает выбранную обложку revision-aware командами `attachment set-preview` и `attachment clear-preview`. Назначение отсутствующего или неподдерживаемого вложения завершается без записи. При удалении выбранного вложения `PreviewAttachmentId` очищается в той же транзакции, после чего действует обычный выбор первого оставшегося изображения. Проверка taskboard отклоняет dangling и нерастровую ссылку.

Теги принадлежат всей доске. Доска хранит общий каталог: у каждого тега есть устойчивый `TagId`, отображаемое название и цвет из разрешённой палитры. Значения `ProjectTask.Labels` являются только ссылками на `TagId`; свободные строки в этом массиве запрещены. Имя поля сохраняется в schema v2 ради совместимости существующих task files, а более точное wire-имя откладывается до следующей версии формата.

При назначении можно выбрать существующий тег или создать новый. Во втором случае одна транзакция сначала добавляет тег в каталог доски, затем назначает его задаче. Новый тег сразу становится доступен всем остальным задачам и не существует как локальное значение одной карточки. Те же теги можно заранее создавать, переименовывать, перекрашивать и удалять через настройки доски и команды `e2d tasks tag`.

Удаление тега, который назначен хотя бы одной активной или архивной задаче, отклоняется. Имя не может повторять другое имя без учёта регистра и окружающих пробелов. Любое изменение каталога повышает revision доски, назначение тега повышает revision задачи, а `tasks verify` отклоняет ссылку на отсутствующий тег. При первом `normalize` существующие значения `Labels` получают определения в каталоге без изменения task files.

`Deadline` хранится как календарная дата ISO 8601 `YYYY-MM-DD`, без времени и часового пояса. Поле отсутствует, если срок не задан. CLI умеет задать и очистить срок, а карточка показывает локализованную дату без риска сдвига между часовыми поясами.

`LegacySourceFragment` хранит source path, byte offset/length, encoding/BOM/line-ending metadata, SHA-256 и точный Markdown fragment. Набор task fragments вместе с board-level unassigned fragments должен позволять побайтово восстановить каждый удаляемый legacy source.

`AcceptanceCriterion` имеет `CriterionId`, `Description`, `State` (`Open`, `Passed`, `Failed`) и `EvidenceLinks`. Criteria имеют stable UID, чтобы независимые добавления не конфликтовали как правка одного массива. Состояние критерия изменяется только явной criterion mutation: смена task status, submit, human accept, archive или reopen не переводит criteria автоматически.

`TaskActivityEntry` имеет `ActivityEntryId`, `ActorId`, `ActorKind`, `CreatedAt`, `Kind` и `Payload`. Поддерживаются виды `Comment`, `Decision`, `Investigation`, `Blocker`, `TestResult`, `StatusChange`, `AgentSummary` и `AcceptanceResult`.

`ActorId`, `ActorKind` и `CreatedAt` являются audit-полями, то есть полями происхождения записи. Их заполняет `TaskActivityStore` из доверенного `OperationContext` и системных часов. Вызывающий агент, CLI или MCP payload может передать только смысловой `Kind` и `Payload`; он не может передать или перезаписать audit-поля как обычные данные.

Запуск `e2d tasks` из Codex Desktop распознаётся только по доверенной переменной окружения процесса `CODEX_INTERNAL_ORIGINATOR_OVERRIDE=Codex Desktop`, которую не принимает ни один task payload или аргумент команды. Такой контекст записывает новые status activity и conversation comments как `ActorKind = Agent`, `ActorId = Codex`. Обычный терминальный запуск без точного marker сохраняет `ActorKind = Cli`, `ActorId = cli`; неизвестное значение fail-closed использует тот же CLI fallback. Это правило применяется только к новым append-only записям и не переписывает прежнюю activity, conversation history или audit-поля.

## Зависимости и readiness

`Readiness` вычисляется отдельно от workflow-статуса:

- `Ready`;
- `BlockedByDependencies`;
- `DependencyCancelled`.

`BlockingReasons` хранит причины:

- `dependency`;
- `environment`;
- `decision`;
- `external`;
- `manual`.

`TaskDependencyGraph` обязан загружать active и completed task documents и:

- запрещать циклы;
- сохранять канонический workflow-статус `Ready`, но вычислять effective board status `Blocked`, если dependency не завершена, отменена или присутствует явный manual/environment/decision/external blocker;
- возвращать structured diagnostic с диагностируемой причиной blocked-состояния;
- после завершения dependency обновлять только dependency-related `BlockingReasons` и `Readiness`;
- после снятия всех вычисляемых и ручных причин автоматически возвращать effective board status в `Ready` без скрытой записи task file;
- не снимать явно назначенный workflow `Status = Blocked`;
- при отмене dependency переводить readiness зависимой задачи в `DependencyCancelled`, оставляя канонический workflow-статус неизменным, и возвращать diagnostic.

`ParentTaskId` образует отдельное дерево containment и не заменяет `Dependencies`. Parent graph и dependency graph проверяются независимо; оба запрещают self-reference и cycles. Задача с незавершённой dependency не может перейти в `InProgress`, `Review` или быть отправлена через `submit`.

## Эпохи, вехи и порядок доски

`TaskBoard` schema v2 содержит:

- `BoardId`, `Revision` и политику генерации ID;
- `Groups` двух видов `Epoch` и `Milestone`;
- optional `ParentGroupId` только для связи `Milestone -> Epoch`;
- `Placements` с `TaskId`, optional `GroupId` и stable `Rank`;
- attachment policy и migration metadata;
- общий каталог тегов;
- board-level legacy fragments, которые нельзя отнести к одной задаче.

Задача может находиться в одной вехе, непосредственно в эпохе или в `Ungrouped`. Более глубокая вложенность и membership во многих группах в schema v2 запрещены. Колонки UI выводятся из effective board status: для `Ready` он становится `Blocked` при dependency/manual blockers, в остальных случаях совпадает с `ProjectTask.Status`. Board не хранит собственные status arrays и не может расходиться с task file.

## Хранилище

Каноническое хранилище задач — стабильные текстовые metadata-документы в корне project/repository root:

```text
.taskboard/
├── board.e2tasks
├── tasks/
│   ├── T-0001.e2task
│   └── T-0002.e2task
├── completed/
│   └── T-0000.e2task
├── attachments/
│   └── T-0001/<attachment-id>/<safe-name>
└── .gitignore
```

Task document использует `format = "Electron2D.TaskFile"`, `version = 2`; board document использует `format = "Electron2D.TaskBoard"`, `version = 2`. Оба формата используют deterministic UTF-8 JSON без BOM, LF, stable ordering, explicit revisions и migrations. Единственное поле описания задачи и board group называется `description`; прежнее имя удалено из serializer, reader, CLI DTO и JSON Schema, поэтому документ без обязательного `description` отклоняется fail closed. Кириллица, обратные апострофы и другие печатные Unicode-символы записываются непосредственно в UTF-8, а не как нечитаемые `\uXXXX`; JSON escaping сохраняется только там, где его требует сам JSON-синтаксис. `.taskboard/tasks/<TaskId>.e2task` содержит active карточки, включая `Done` до явного archive.

`.taskboard/**` является `EditorMetadata`: документы доступны CLI, Editor, Tooling/MCP и IDE adapters, но не импортируются как игровые ресурсы, не попадают в production asset packs, APK, AAB, app bundle, desktop distribution или runtime snapshot. Product/release packages исключают всю `.taskboard`, а audit package включает только явно разрешённые metadata/evidence без произвольных attachments.

`Done` означает принятую задачу, но не переносит файл автоматически. `archive` доступен только для `Done` или `Cancelled`, переносит task file в `.taskboard/completed/`, удаляет active placement и сохраняет attachments/history. `unarchive` возвращает task file и placement; `reopen` является отдельным status action. Hard delete требует exact task-ID confirmation, reference scan и отдельного разрешения на удаление attachments.

`.taskboard/.gitignore` исключает только lock, transaction staging/journal и cache. Task/board documents и attachments отслеживаются source control по умолчанию.

## Disk transactions и concurrency

Любая mutation получает expected board/task revisions, затем ограниченно ожидает exclusive cross-process lock и только после его получения повторно загружает store, проверяет revisions и все invariants. По умолчанию writer ждёт не более 10 секунд с паузой между попытками от 25 до 250 миллисекунд; CLI позволяет уменьшить эти границы через `--lock-timeout-ms` и `--lock-backoff-ms`, а внутренний API принимает `CancellationToken`. Истечение срока и отмена являются retryable writer-coordination failures с отдельными стабильными кодами; task/board revision mismatch остаётся semantic CAS conflict и автоматически не повторяется. Изменения записываются в staging, после чего durable transaction manifest описывает before/after hashes и create/replace/move/delete operations. Commit использует atomic replace/move в пределах одного filesystem; незавершённый manifest восстанавливается или откатывается следующей CLI-командой до чтения нового snapshot.

Manifest `Electron2D.TaskTransaction` хранится в `.taskboard/.transactions/<transaction-id>.json`, а immutable staged payloads — в `.taskboard/.staging/<transaction-id>/`. Каждая operation содержит project-relative target path, staged path для replace, `beforeSha256` и `afterSha256`. Replay идемпотентен: target может совпадать либо с before hash, либо уже с after hash; любое третье содержимое означает conflict и останавливает recovery без удаления manifest. После применения всех replace/delete operations manifest и его staging удаляются.

Операция может передать `--operation-id <stable-id>`. Writer связывает ID с каноническим fingerprint команды и одной транзакцией сохраняет ignored local receipt вместе с результатом. Повтор того же ID и fingerprint возвращает уже записанный результат без новой задачи, реплики, activity, связи, вложения или status transition; повтор ID с другим fingerprint отклоняется как idempotency conflict. Receipt предназначен для безопасного повтора после неоднозначного transient-сбоя и не заменяет tracked lossless task history.

Создание задачи является коммутативной board-операцией: после получения lock оно выделяет `taskId`, `taskUid`, placement и следующую board revision из заново прочитанного состояния. Поэтому `tasks create` не требует заранее угаданной board revision; явно переданный `--expected-board-revision` остаётся дополнительным CAS guard. Остальные board/task mutations требуют expected revisions. Две независимые mutation разных задач последовательно фиксируются под lock. Две mutation одной task revision не используют last-write-wins: явно коммутативный append может быть повторён вызывающей стороной с новой revision, а изменение полей получает стабильный CAS conflict с actual revision.

Conversation, activity и context checkpoints получают ID и sequence только внутри той же locked transaction. Append всегда читает последний committed task после получения lock; stale context не повторяется автоматически. Успешный append неизменяем, а проигравший CAS не считается подтверждённой записью и может быть безопасно повторён с новым snapshot. Writer lock освобождается через `Dispose` даже при исключении или отмене; оставшийся `.lock` является обычным lock-файлом, а не признаком владения, поэтому аварийно завершившийся процесс не создаёт вечную блокировку.

Semantic/CAS conflict не повторяет mutation автоматически. CLI возвращает stable diagnostic (`E2D-TASK-0004` timeout, `E2D-TASK-0005` cancellation, `E2D-TASK-0006` revision conflict, `E2D-TASK-0007` idempotency conflict), retryability и actual revisions; UI перезагружает snapshot и просит пользователя повторить осмысленное действие. Повторяется только получение writer lock и явно идентифицированная операция после неоднозначного transient-сбоя.

## CLI-only mutation contract

`e2d tasks` поддерживает `init`, `board`, `list`, `get`, `create`, `update`, `move`, `set-status`, `submit`, `accept`, `request-changes`, `cancel`, `reopen`, `archive`, `unarchive`, `delete`, `comment`, `criterion`, `parent`, `dependency`, `group`, `tag`, `attachment`, `verify`, `normalize`, `migrate` и `export`.

Срок задаётся через `--deadline <YYYY-MM-DD>` в `create` или `update`, а `update --clear-deadline true` удаляет его. Одновременная передача нового срока и флага очистки отклоняется.

Каталог тегов и назначения меняются только revision-aware командами:

```text
e2d tasks tag create --name <name> --color <color> [--assign-to <task-id> --expected-task-revision <n>] --expected-board-revision <n>
e2d tasks tag update <tag-id> [--name <name>] [--color <color>] --expected-board-revision <n>
e2d tasks tag delete <tag-id> --expected-board-revision <n>
e2d tasks tag assign <task-id> --tag <tag-id> --expected-task-revision <n> --expected-board-revision <n>
e2d tasks tag unassign <task-id> --tag <tag-id> --expected-task-revision <n> --expected-board-revision <n>
```

В TaskBoard v3 поле `tags[].color` принимает либо одно из семи legacy-значений `Gray`, `Blue`, `Green`, `Yellow`, `Orange`, `Red`, `Purple`, либо canonical custom color в точном формате `#RRGGBB`. Hex-цифры при mutation нормализуются к верхнему регистру. Сокращённая форма `#RGB`, alpha-канал, CSS function/name вне legacy-набора, пробелы и произвольные строки запрещены одинаково JSON Schema, semantic validator и CLI mutation. Расширение обратно совместимо с существующими board documents и не меняет TaskBoard v2 snapshot.

Каталог может использовать custom colors для однозначного визуального различения тегов. Уникальность является проверяемым свойством конкретной доски: два определения не должны иметь одинаковый canonical color value без учёта регистра. `tag update --color` остаётся revision-aware board mutation; массовая смена палитры выполняется последовательными CLI-командами с актуальной board revision и никогда не означает ручную правку `board.e2tasks`.

В `T-1210` рабочий каталог из 33 тегов переведён на 33 разных canonical `#RRGGBB` через последовательные `e2d tasks tag update`. Board revision изменился `121 → 154`; итоговый аудит подтвердил `33` определения, `33` уникальных значения, отсутствие повторов и неканонических цветов. Контракт проверен schema/semantic и CLI-тестами: lowercase hex нормализуется, legacy-имена сохраняют обратную совместимость, а сокращённые, alpha- и произвольные CSS-значения отклоняются без записи.

`tag create --assign-to` выполняется одной дисковой транзакцией: при конфликте revision не остаётся ни нового тега, ни частичного назначения. `tag assign` принимает только существующий `TagId`. Повторное назначение, отсутствующий тег, занятое имя, неизвестный цвет и удаление используемого тега завершаются без изменения файлов.

Компактный снимок доски содержит каталог тегов и только данные, необходимые карточкам. Для каждой задачи в него входят ссылки на теги, `Deadline`, общее число критериев, число критериев в состоянии `Passed`, количество настоящих attachments и минимальные metadata эффективной обложки: `AttachmentId`, display name, project-relative path и raster media type. `LinkedArtifacts` в счётчик вложений и выбор обложки не входят. Остальные тексты критериев и attachment metadata по-прежнему загружаются только через `tasks get`.

Criteria управляются только revision-aware командами:

```text
e2d tasks criterion add <task-id> --criterion <criterion-id> --description <text> [--state Open] --expected-revision <n>
e2d tasks criterion update <task-id> --criterion <criterion-id> --description <text> --expected-revision <n>
e2d tasks criterion set-state <task-id> --criterion <criterion-id> --state Open|Passed|Failed --expected-revision <n>
e2d tasks criterion remove <task-id> --criterion <criterion-id> --expected-revision <n>
```

`add` отклоняет duplicate `criterionId`, `update` меняет только description, `set-state` меняет только state, `remove` удаляет ровно один criterion. Missing criterion, пустые ID/description, неподдерживаемое state и stale revision fail closed. Успешная mutation увеличивает task revision и `updatedAt`, сохраняет task через transaction store и возвращает полный snapshot; `--dry-run` возвращает рассчитанный snapshot без записи файла.

`tasks normalize --expected-board-revision <n>` под exclusive lock перечитывает board и все active/completed task documents, проверяет board revision и транзакционно переписывает только те JSON-файлы, чьи canonical serializer bytes отличаются. Форматирование не меняет task/board revisions, статусы, activity или audit fields; повторный запуск идемпотентен. Для legacy task команда консервативно распознаёт clear command strings в `LinkedArtifacts`, переносит команды в unique ordered `ExecutionContract.RequiredCommands` и сохраняет file/directory references на месте без потери данных. Существующие `RequiredCommands` сохраняются без текстовой перезаписи; форма без surrounding Markdown backticks используется только как ключ дедупликации. Неизвестная произвольная строка не угадывается как файл или команда и не должна отображаться UI как файл. Schema migrations должны быть завершены до удаления compatibility reader; после cutover команда работает только с canonical schema.

Все команды принимают `--project` и `--format json`. Изменяющие команды поддерживают `--dry-run`, expected revisions и `--input <file|->` для сложного payload. Output использует общий CLI envelope и возвращает changed files, revisions, structured diagnostics и актуальный result snapshot.

Noninteractive CLI, MCP и agent context не получают `TaskAccept`/`TaskRequestChanges` только из payload. Trusted Editor/VS Code bridge создаёт краткоживущий human capability после interactive confirmation; audit actor/time заполняются host context и системными часами.

## Вложения и безопасность путей

`attachment add` копирует только обычный файл после canonical `GetFullPath`/containment и reparse-point проверки. Запрещены absolute target paths, `..`, control characters, directories, device/special files и symlink/reparse escape. Имя blob формируется CLI и не доверяет исходному filename.

Архивы хранятся как opaque blobs: TaskManager их не распаковывает, не индексирует и не исполняет. Inline preview разрешён только клиентам для allowlisted raster formats после повторной проверки attachment ID, metadata, containment и hash; SVG/HTML/archives не исполняются внутри webview. Compact response не публикует готовый URI: trusted Extension Host повторно проверяет тип и принадлежность пути задаче, затем преобразует его в ограниченный webview resource URI.

## Миграция и cutover

`migrate --dry-run` поддерживает legacy `.electron2d/tasks` v1, root `TASKS.md` и `data/completed-tasks/**`. Markdown parser учитывает fenced blocks, aliases полей, ROADMAP hierarchy, повторные source copies и logical ID collisions. Structured mapping дополняется точными legacy fragments.

`--apply` разрешён только при совпадении source digests с dry-run report. Известные legacy anomalies исправляются явно в report: обратные рёбра `T-1015/T-1016 -> T-0980` удаляются; различные historical records с одинаковым ID получают unique canonical suffix IDs и сохраняют `LegacyAliases`; повторные fragments одной logical record объединяются.

Текущий parser slice распознаёт task headings `H1`/`H2` только вне CommonMark fences, сегментирует каждый source в contiguous task/board fragments и сохраняет UTF-8 BOM, CRLF/LF, byte offsets/lengths и SHA-256. На реальном corpus подтверждены 941 active + 202 completed records, 1143 unique canonical IDs после collision mapping, 1146 fragments, 8036 dependency edges после удаления только двух allowlisted обратных рёбер, 6 Epoch + 40 Milestone groups, 941 active placements, три stale ROADMAP diagnostics и пять `Ungrouped` placements. Исходные три файла восстанавливаются побайтово из fragments.

Parsed records конвертируются в serializer-valid TaskFile v2: deterministic UID, collision aliases, 16 parent links, fixed statuses, timestamps, dependencies, execution contracts, acceptance criteria, dated activity, path links и raw fragments. `tasks migrate --dry-run` формирует deterministic report SHA-256; `--apply true --report-sha` требует точного текущего digest, пишет canonical snapshot транзакционно и идемпотентен. Отдельный `--finalize true` повторно сравнивает каждый canonical task/board byte с reviewed plan, запускает store verification и лишь затем транзакционно удаляет перечисленные legacy sources и ставит `migration.finalized = true`.

Repository cutover выполнен 2026-07-11 по reviewed report SHA-256 `d5e7d2178eb7ca6423850c1eed0d2e7c49840c1f6cca1be3b7228127e3d24a71`: dry-run подтвердил 1143 задачи, apply создал 941 active и 202 completed документов, повторный apply не изменил ни одного файла, а finalize удалил `TASKS.md` и `data/completed-tasks/**`. Post-finalize `tasks verify` прошёл. После включения readable-Unicode serializer команда `tasks normalize` транзакционно переписала 1144 board/task-файла без изменения revisions; дополнительный проход перевёл два emoji surrogate pair в прямой UTF-8. Переименование канонического description key затем переписало 1147 файлов root taskboard, один template task и шесть Platformer tasks; повторные запуски изменили 0 файлов, raw scan трёх taskboards подтвердил отсутствие legacy key, а `tasks verify` прошёл для `944/202`, `1/0` и `6/0`. Embedded snapshot merge сохраняет полный outer fragment и ищет missing structured fields по всему task-owned fragment; отдельный provenance mapping для каждого embedded field и typed remap произвольных collision references остаются migration-hardening возможностями, но не источником dual-write.

Post-finalize regression для живого репозитория читает его текущим version-specific store. После перехода корневой доски на v3 тест использует `TaskBoardV3DiskStore.Verify`, проверяет 1143 перенесённые task-owned legacy fragments, aliases, parent/dependency graph, execution contracts и v2→v3 migration provenance. V3 JSON не передаётся в v2-only `TaskBoardDiskStore`, а текущий active count не сравнивается с историческими 941 карточками: после cutover новые задачи законно добавляются, тогда как исходный migration inventory остаётся выделяемым по сохранённым fragments и provenance.

Первый consumer cutover перевёл `ProjectTaskManager` workspace fixtures, `TaskService.List`, MCP task tools, project template creator, template verifier, reference Platformer и Windows/release exclusions на `.taskboard`. Новый template содержит TaskBoard/TaskFile v3 и transient-only `.taskboard/.gitignore`; Platformer имеет шесть self-contained active task files, а внешняя repository dependency `T-0215` хранится как typed link вместо missing DAG target. Legacy `.electron2d/tasks/**` остаётся только deliberate read-only classification и package exclusion для миграционной совместимости.

Finalize с удалением источников был разрешён только после чистого `e2d tasks verify`, побайтового reconstruction check, проверки counts/references/DAG, повторного idempotent migration run и exact confirmation report digest. После cutover mutations работают только с `.taskboard`; dual-write отсутствует.

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

Попытка direct file edit поставить `Done`, выполнить `Done -> Ready` или `Cancelled -> Ready` без trusted command должна вернуть structured diagnostic и оставить import в conflict/pending state. Такая проверка нужна даже для headless import, чтобы прямое редактирование файла не обходило приёмку.

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

- Есть focused tests на статусы, допустимые переходы, `Review` + `AcceptanceState`, `Request changes`, `Cancel` и `Reopen`.
- Есть focused tests на acceptance guard: AI может выполнить `submit` и оставить задачу в `Review` с `Submitted`, но не может установить `Done`; human context с нужной capability может принять или вернуть задачу.
- Есть focused tests на сохранение истории при `Reopen`: `CompletedAt`, `AcceptedAt`, `AcceptedBy` и прошлое `AcceptanceState` не удаляются, а activity получает запись о reopen.
- Есть focused tests на task model, `AcceptanceCriterion` stable UID и `TaskActivityEntry` audit fields, которые заполняются из `OperationContext`.
- Есть focused tests на storage round-trip для `.taskboard/tasks/*.e2task`, `.taskboard/completed/*.e2task` и `.taskboard/board.e2tasks`, публикуемые schema v3, canonical formatting, UID/revisions/groups/placements/attachments/legacy fragments и classification как `EditorMetadata`.
- Есть focused tests на transaction semantics: `expectedRevision`, dirty state, grouped Undo/Redo, `SaveAffectedDocuments` и conflict diagnostics.
- Есть focused tests на external import guard для привилегированных полей и direct file edit попыток поставить `Done` или сделать privileged reopen.
- Есть focused tests на dependency graph: cycle rejection, blocked readiness, завершение dependency без снятия manual blocker и cancelled dependency diagnostic.
- Есть focused tests на parent cycles, dependency-gated transitions, active/completed lookup, archive/unarchive и guarded hard delete.
- Есть focused tests на cross-process conflict, transaction recovery, attachment containment/hash/quotas и отсутствие automatic mutation retry.
- Есть migration golden tests на exact source reconstruction, field aliases/fences, ROADMAP groups, collisions, approved graph repair, idempotence и finalize guard.
- CLI integration tests подтверждают полный command surface, stable JSON envelope, `--dry-run`, expected revisions и stdin payload.
- Implementation documentation описывает фактическое поведение, текущие ограничения и focused test command.

## Фактическое состояние, ограничения и проверки

Статус: v3 является единственным рабочим и публикуемым контрактом; v2 сохранён только как вход migration reader.
Задача: `T-0154`, `T-1147`, `T-1200`.
Обновлено: 2026-07-13.

## Контракт TaskBoard v3

### Граница версии

TaskFile/TaskBoard v2 был рабочим форматом репозитория, шаблона проекта и примера Platformer до v3 cutover. Перевод ссылок с `taskId` на `taskUid`, удаление `subtasks`, изменение lifecycle и типизация payload несовместимы с опубликованной формой v2. Поэтому v2 не исправляется скрыто: новый надёжный контракт получает `version = 3`, отдельные schema и детерминированную миграцию v2→v3. После cutover v2 разрешён только как вход миграции; обычные create/read/update выполняются над v3.

После repository cutover публикуемый schema inventory содержит только `task-file-v3.schema.json` и `task-board-v3.schema.json`. Файлы `task-file-v2.schema.json` и `task-board-v2.schema.json` удалены как неактуальный публичный contract. Детерминированная v2→v3 миграция опирается на версионный code-owned reader и migration fixtures, а не на публикуемую v2 schema.

TaskBoard v3 на этой ветке остаётся pre-release: schema v3 и её C#-контракт ещё не входили в tracked commit, registry или опубликованный package. Поэтому замечания до первой публикации заменяют v3 целиком с теми же `$id`/`version`, а repository/cache не должны сохранять более раннюю pre-release форму. После первой внешней публикации любое несовместимое изменение получает новый major format и новый schema `$id`; повторное использование `version = 3` запрещено.

### Идентичность и связи

- `taskUid` — неизменяемый внешний ключ. Board placements, parent и все межзадачные relations используют только его.
- `taskId` — изменяемый отображаемый номер. При переименовании прежнее значение добавляется в `legacyAliases`, но ссылки и attachment paths не меняются.
- Каждый TaskFile содержит `boardId`; semantic validator требует точного совпадения с открытой доской.
- `assignee` — nullable identity текущего исполнителя. Поле участвует в проверке независимой приёмки и не заменяется `createdBy` или автором последней activity.
- Иерархия хранит только `parentTaskUid`. Список детей вычисляется; массива `subtasks` в v3 нет.
- `relations` содержит объекты с устойчивым `relationId`, `kind` и `targetTaskUid`. `DependsOn` участвует в readiness DAG; прочие виды связи не блокируют выполнение.
- Теги хранятся в `tagIds`, а artifacts/scenes/resources/nodes — в типизированных объектах с явным kind и value. Произвольная строка больше не угадывается как путь, URI или команда.

Active TaskFile может иметь ровно один placement по `taskUid`; archived TaskFile placement не имеет. `taskUid` и `taskId` уникальны среди active и completed файлов. Parent и relation target обязаны принадлежать той же доске, не могут указывать на саму задачу; parent graph и `DependsOn` graph ацикличны.

### Жизненный цикл

v3 хранит текущий lifecycle отдельно от типизированной append-only activity. Reopen сбрасывает текущую попытку приёмки, а предыдущая принятая или отменённая попытка остаётся в activity; поэтому активная задача не носит timestamps прошлого terminal state.

| Status | Допустимый acceptance state | Обязательные условия |
| --- | --- | --- |
| `Ready` | `NotSubmitted` | Нет terminal/archive timestamps и активных blockers. Незавершённая dependency влияет на вычисленный board status, но не записывает новый status. |
| `InProgress` | `NotSubmitted`, `ChangesRequested` | Нет terminal/archive timestamps и активных blockers. `ChangesRequested` следует только после typed acceptance result доверенного аудитора или владельца. |
| `Blocked` | `NotSubmitted`, `ChangesRequested` | Есть хотя бы один явный manual/environment/decision/external blocker. Dependency-only блокировка не записывает `Blocked`. |
| `Review` | `InternalReview`, `Submitted` | Нет активных blockers. Для `Submitted` обязательна текущая `submittedAt`; для `InternalReview` она отсутствует. |
| `Done` | `Accepted` | Нет активных blockers; список criteria непуст, все criteria имеют `Passed` и хотя бы одну evidence link; обязательны `submittedAt`, `completedAt`, `acceptedAt`, `acceptedBy`, актуальный authoritative `workspaceChanges` и typed acceptance activity независимого аудитора или владельца. Для audit mode `Single|PrimaryControl` acceptance ссылается на последний успешный audit run требуемой цепочки. |
| `Cancelled` | `Cancelled` | Обязательны `cancelledAt`, непустая причина и typed cancellation activity. |

Archive metadata допускается только у `Done` или `Cancelled` и требует одновременно `archivedAt` и `archivedBy`. Для остальных status эти поля отсутствуют. У blocker в состоянии `Active` отсутствуют `resolvedAt`/`resolvedBy`, у `Resolved` оба поля обязательны. Все audit timestamps упорядочены относительно `createdAt` и друг друга. Decision `Rejected` не является отдельным скрытым acceptance state: отказ с возможностью исправления записывается как `ChangesRequested`, а окончательная отмена — отдельным переходом в `Cancelled`. JSON Schema применяет локальные `if/then`; semantic validator проверяет критерии и связанные данные; transition validator проверяет допустимый переход и полномочия.

Разрешённые переходы: `Ready → InProgress|Blocked|Cancelled`, `InProgress → Review|Blocked|Cancelled`, `Blocked → Ready|InProgress|Cancelled`, `Review → Review(Submitted)|InProgress(ChangesRequested)|Done|Cancelled`, `Done|Cancelled → Ready` только через trusted reopen. Повторное сохранение того же lifecycle state не является переходом.

### Три слоя проверки

1. JSON Schema с включённой проверкой стандартных `format` проверяет форму одного документа, enum, размеры, канонические ID/rank/path patterns и локальные lifecycle conditions. `contains` и другие applicator keywords всегда имеют локально типизированные subschema, поэтому strict AJV не выдаёт неоднозначных предупреждений.
2. Обязательный versioned `TaskBoardSemanticValidatorV3` загружает active и completed tasks вместе с board и проверяет уникальность, существование ссылок, placements, группы, теги, графы, лимиты и blobs.
3. Обязательный versioned `TaskTransitionValidatorV3` сравнивает предыдущий и новый snapshots: task revision обязан увеличиться ровно на один, board revision — ровно на один для board mutation; `taskUid`, `boardId`, `createdAt` и `createdBy` неизменяемы; старые conversation/activity/checkpoint/audit entries и original attachment metadata не удаляются и не переписываются; audit/acceptance/archive поля меняются только доверенной командой с нужной capability и ролью. Definition-поля (`title`, `description`, criteria, execution contract и связи) меняются только вместе с append-only `TaskPatched`. Событие хранит `fromRevision`, `toRevision`, собственный `activitySequence`, hash profile, RFC 6902 operation с типизированными по `path` `oldValue`/`value` и вычисленные валидатором `beforeTaskCoreDigest`/`afterTaskCoreDigest`, поэтому task core можно воспроизвести вперёд и назад от genesis TaskFile.

TaskBoard v3 хранит обязательный `validationContract` с точными именами `TaskBoardSemanticValidatorV3`, `TaskTransitionValidatorV3`, `AgentContextBuilderV3` и `TaskExecutionPolicyV3`, а также `formatAssertions = true`. Это исполняемая привязка версии, а не текстовое примечание schema. Негативный contract suite обязателен для duplicate identity, dangling references, parent/dependency cycles, duplicate ranks, invalid placements и каждого запрещённого lifecycle/transition counterexample.

Обычная CLI mutation проходит все три слоя до подготовки transaction manifest. Ошибка любого слоя не оставляет task, board, attachment или allocation частично изменёнными.

### Уникальность и ранжирование

Semantic validator отклоняет повторяющиеся `taskUid`, `taskId`, `tagId`, `groupId`, `relationId`, `criterionId`, `attachmentId` и `activityEntryId`. Group rank уникален среди sibling groups; placement rank уникален внутри одного `groupId` (включая `null`). Rank v3 — строка ровно из 12 десятичных цифр и сравнивается лексикографически; формы `2`, `02` и одинаковый rank недопустимы. Rebalance является отдельной board transaction.

ID allocator работает только под существующим cross-process write lock. Create требует актуальную board revision, выделяет `taskId`, создаёт `taskUid`, placement и увеличивает `nextNumber`/board revision одной транзакцией. Одной проверки JSON или локального `nextNumber` без CAS недостаточно.

### Структурированное выполнение

`executionContract.commands` не содержит произвольных shell-строк. Native command spec хранит `commandId`, `kind = Process`, `executable`, массив `arguments`, project-relative `workingDirectory` (корень проекта записывается как `.`), `platforms`, `timeoutSeconds`, `expectedExitCodes`, массив запрошенных `requestedCapabilities` и признак human confirmation. Capability выбирается из `WorkspaceRead`, `WorkspaceWrite`, `Network`, `ExternalEffect`; одновременно можно запросить несколько. `platforms = ["Any"]` не смешивается с конкретными платформами. Executable и arguments не склеиваются в shell command.

Валидность command spec и заявленные capabilities не дают разрешения на запуск. Обязательный `TaskExecutionPolicyV3` независимо вычисляет разрешённые capabilities из доверенного `OperationContext`, не управляется task payload и по умолчанию запрещает выполнение. `WorkspaceWrite`, `Network`, `ExternalEffect` и неизвестный executable требуют более строгого policy decision или human confirmation. `bash`, `sh`, `zsh`, `cmd`, `powershell`, `pwsh` и их platform aliases распознаются как shell interpreters даже при `kind = Process`; их нельзя использовать для обхода `LegacyShell`, а запуск требует отдельного shell-interpreter grant и human confirmation.

`executionContract.externalAudit` — закрытый typed object с mode `None|Single|PrimaryControl`, требованием независимости `NotRequired|DifferentActor|CleanControlContext`, инструкцией и точным списком обязательных verdicts. Свободная строка не является полномочием и не управляет audit tooling.

Миграция не пытается безопасно угадать структуру старой shell-строки. Такой текст становится `kind = LegacyShell`, сохраняется дословно и получает неизменяемое `execution = ForbiddenUntilReviewed`. Автоматический executor всегда отклоняет этот kind.

### Типизированная activity и evidence

Activity entry сохраняет общие audit-поля и обязательный непрерывный `sequence`, но payload зависит от kind. Корневой `lastActivitySequence` равен последнему sequence или нулю; индекс массива не является контрактом. Новые `Comment` и `AgentSummary` запрещены: legacy-реплики мигрируются в `conversation`, а не остаются вторым источником истины. Старый непрозрачный activity-текст разрешён только как `Legacy` с исходным kind:

- `StatusChange` — previous/next status и reason;
- `TestResult` — commandId, outcome, exit code, summary и typed evidence;
- `Blocker` — blocker kind, reason и active/resolved state;
- `Decision` — decision, rationale и authority;
- `AuditRun` — stage `Primary|Control`, immutable run ID, trusted auditor identity, проверенные task/context/package digests, report attachment, decision и previous-verdict chain;
- `AcceptanceResult` — decision `Accepted|ChangesRequested`, reason, trusted authority actor/role и ссылка `auditRunId` на существующий подходящий run;
- `TaskPatched` — canonical RFC 6902 patch и SHA-256 task core до/после изменения;
- `WorkspaceChangesUpdated` — authoritative полный snapshot изменённых workspace files, base/current revisions и canonical digest до/после пересчёта;
- `Investigation` — summary и typed evidence;
- `Legacy` — исходный kind и непрозрачный текст только для миграции.

Evidence и обычные typed links используют kind-dependent validation: `File`/`Directory` содержат project-relative path, `Uri` — URI, `Attachment` — существующий attachment ID. Arbitrary string не интерпретируется. Старые activity entries являются immutable prefix: transition может только дописать новые entries с уникальными ID, непрерывными sequence и неубывающим временем. Atomic append проверяет `expectedRevision`, `expectedLastMessageSequence` и `expectedLastActivitySequence` под одним writer lock; несовпадение любой границы не оставляет записи.

### Доверенные роли и независимая приёмка

Task payload никогда не объявляет полномочия своего автора. Writer получает из доверенного `OperationContext` identity, session/origin, capabilities и одну роль `Worker|Auditor|Owner`. `Worker` может менять разрешённое содержимое и отправлять задачу на проверку, но не принимает её даже при ошибочно выданной capability. `Auditor` может выдать typed acceptance result только при наличии отдельного доверенного grant, а `Owner` использует human-owned route. Агент-аудитор представляется как `actorKind = Agent`, а не поддельный `Human`.

`TaskTransitionValidatorV3` требует, чтобы новая acceptance activity, `acceptedBy` и trusted context совпадали по identity/role. `AcceptanceResult` обязан ссылаться на существующий `AuditRun` для audit mode `Single|PrimaryControl`: для `Single` нужен последний успешный `Primary`, для `PrimaryControl` — следующие друг за другом успешные `Primary` и `Control` с требуемой независимостью и цепочкой предыдущего verdict. Run хранит проверенную task revision, revision своей записи, context/workspace/package digests, immutable report attachment и закрытый package manifest с exact input/excluded attachment IDs. `Control` дополнительно хранит clean-context manifest: primary run/report/verdict artifacts перечислены как исключённые и не входят во входной package. `packageDigest` вычисляется валидатором по этим manifest, а не принимается как заявление. Более поздний `NeedsFixes`, несоответствие revision/context или изменение task core/workspace после финального run запрещают `Done`. Автор/assignee/worker текущей попытки не может быть аудитором или принимающим собственного результата. Самоприёмка запрещена независимо от текста payload, а смена role или actor kind внутри TaskFile не создаёт полномочий.

### Conversation и воспроизводимый контекст агента

TaskFile v3 является полным context capsule и раздельно хранит definition/lifecycle, append-only `conversation.messages`, append-only системную `activity`, originals/derived attachment metadata и nullable derived `contextSnapshot`. Каждая реплика имеет устойчивый уникальный ID, уникальный непрерывный `sequence` от единицы, автора с identity/kind/role, timestamp, optional reply/reference и массив typed content blocks (`Markdown` или ссылка на attachment). `lastMessageSequence` равен maximum sequence или нулю для пустой истории; reply указывает только на уже существующую более раннюю реплику, attachment block — только на original той же задачи. Для `actorKind = Agent` обязателен `agentRunId`; Human/Cli/System/Test не могут заявить чужой agent run. `System` никогда не получает роль `Owner`. Author identity/role назначаются writer из trusted operation context и сверяются transition validator, а не принимаются из JSON payload. Исправление — новая реплика; summary не удаляет и не переписывает историю. Встроенный массив не имеет лимита количества сообщений; полный `tasks get` всегда позволяет прочитать старые реплики и файлы. При росте истории storage может перейти на эквивалентный physical aggregate `task.json + conversation.jsonl + activity.jsonl + attachments + derived` только отдельной версией/миграцией с сохранением тех же инвариантов.

Каждый agent run фиксирует immutable context checkpoint: `agentRunId`, `taskRevision`, `lastMessageSequence`, `lastActivitySequence`, `contextDigest`, actor/role и optional `rebaseOfCheckpointId`. Нормативный `AgentContextBuilderV3` использует RFC 8785 JCS: object names сортируются по UTF-16 code units, JSON strings сериализуются без необязательного escaping, числа canonical; затем берутся exact UTF-8 bytes без BOM и SHA-256. Профиль фиксирован как `sha256-jcs-rfc8785-v1`; смена правил требует нового profile. `taskCoreDigest` вычисляется по явно перечисленным task definition/lifecycle полям без `contextSnapshot`, checkpoints, conversation, activity и attachments, поэтому не самоссылочен. `checkpointDigest` вычисляется по checkpoint projection без собственного поля `contextDigest`; остальные identity/watermark fields входят в projection, поэтому checkpoint связывается с manifest без рекурсивного hash. Отдельный manifest содержит `throughTaskRevision`, `throughMessageSequence`, `throughActivitySequence`, digest точных prefixes conversation/activity, `workspaceChangesDigest`, упорядоченный attachment manifest и digest каждого original/ready derivative blob. Для `Original` запрещён `derivativeId`, для derivative он обязателен. Итоговый `contextDigest = SHA-256(JCS(contextManifest))`. Один golden corpus обязан давать одинаковые canonical bytes/digests в .NET и JS.

`contextSnapshot` хранит тот же builder/hash profile, точный manifest покрытых источников и summary. Контекст собирается только как `summary through K + raw messages/activity with sequence > K`; если snapshot отсутствует, builder передаёт всю raw history и не имеет права молча обрезать её параметром окна. Snapshot является derived cache и не заменяет исходные messages/activity/files. Runtime перед записью сравнивает task/message/activity watermarks; stale append не повторяется автоматически.

### Authoritative workspace changes

TaskFile обязательно хранит корневой `workspaceChanges` с `baseRevision`, `currentRevision` и итоговым уникальным списком `files`. Запись файла содержит canonical project-relative `path`, `changeKind = Added|Modified|Deleted|Renamed|Copied|TypeChanged`, nullable `previousPath`, nullable `baseSha256`/`currentSha256`, `firstChangedAt`, `lastChangedAt` и уникальные `agentRunIds`. `Renamed` требует `previousPath`; `Added` не имеет base hash; `Deleted` не имеет current hash; остальные состояния требуют оба применимых hash. Один итоговый path встречается ровно один раз. `.taskboard/**`, `.git/**`, attachments/derivatives и transient build cache не входят в этот список.

Список не принимается из agent/user JSON. `WorkspaceChangesBuilderV3` под trusted host boundary снимает baseline реального workspace, а перед `Review`, audit package и `Done` повторно обходит обычные файлы через canonical path/realpath/lstat, отклоняет symlink/reparse escape, вычисляет SHA-256 и строит diff. `TaskTransitionValidatorV3` допускает изменение корневого snapshot только вместе с `WorkspaceChangesUpdated`, чей before/after digest и полный snapshot совпадают с вычисленным trusted result. Semantic verify повторно проверяет текущие paths/types/hashes; unavailable baseline или drift fail closed. Machine-readable элементы `executionContract.allowedChanges`/`forbiddenChanges` имеют форму `path:<glob>`: каждый changed path должен совпасть хотя бы с одним allowed glob и ни с одним forbidden glob; narrative entries не создают разрешения. `AgentContextBuilderV3` всегда передаёт workspaceChanges и отдельный `workspaceChangesDigest` следующему агенту и аудитору.

Интерактивный чат редактора не получает исключений из этого контракта. Human message записывается до запуска модели через доверенный host-only stdio bridge с одноразовой capability, exact task revision и actor identity из host context; stdin/stdout bridge использует явный UTF-8 без BOM независимо от Windows console/OEM code page, поэтому canonical Markdown совпадает с введённым Unicode-текстом. Произвольный CLI-процесс и webview payload не могут объявить себя человеком. Final agent message записывается отдельным trusted agent append с тем же `agentRunId`, который использован в checkpoint. Для ещё не подтверждённого human append точный task revision conflict допускает один bounded retry после свежего `tasks get`, проверки неизменного `taskUid` и отсутствия выигравшей реплики; прочий conflict и неоднозначный результат fail closed. Final append продолжает требовать проверки message/run identity и не повторяется вслепую. Новый `connecting` с другим `agentRunId` сбрасывает terminal presentation завершённого/ошибочного run; поздние события прежнего run игнорируются. Transient reasoning, tool, permission и transport events являются presentation state и не создают второй журнал переписки.

OpenCode integration располагается за Extension Host boundary. Один backend server принадлежит одной workspace folder, а OpenCode session принадлежит ровно одной паре workspace/task UID; session ID не является полномочием и не заменяет canonical context digest. Новый run сначала собирает и фиксирует `AgentContextBuilderV3` checkpoint, затем инъецирует canonical context без генерации ответа и только после этого отправляет пользовательский prompt. Backend stream нормализуется в безопасные summary/commentary/tool/permission/status/final events. Отмена обязана прервать session run; disconnect или reload не превращает partial output в final conversation message. Потерянный owned transport инвалидирует runtime и его session cache; только однозначный connection-refused до подтверждённой доставки prompt допускает один новый runtime и один повтор, тогда как ambiguous transport failure запрещает replay. Низкоуровневый `fetch failed` не показывается пользователю без безопасного пояснения и действия.

OpenCode запускается с отключённым sharing и явной fail-closed permission configuration, потому что upstream defaults не являются политикой TaskBoard. Security overlay сохраняет обычные пользовательские provider registry и authentication, не задаёт `enabled_providers`, provider allowlist, endpoint или credential. Каждый context/prompt request явно выбирает требуемую для этого чата модель `openai/gpt-5.6-sol` (`GPT-5.6 Sol`): request-level выбор перекрывает как устаревший глобальный default на LM Studio, так и последнюю модель восстановленной task-session; fallback на них запрещён. Read-only project inspection может быть разрешён profile, но edit, shell/task execution, network и external-directory access требуют trusted human confirmation или отклоняются. Отсутствующий executable, недоступный transport, требуемая модель или authentication возвращают структурированную диагностику без secrets. TaskBoard сохраняет только canonical user/final messages, checkpoints и минимальные run/session references. Локальное disk retention session/message storage самой OpenCode является документированным ограничением первой версии; строгая no-disk гарантия не объявляется без отдельного доказуемого ephemeral backend profile.

Живой follow-up `T-1222` в установленном VSIX подтвердил этот контракт: canonical conversation сохранила точную кириллическую Human-реплику `Ты тут? Ответь одним словом: готово`, checkpoint привязан к тому же run, а финальная Agent-реплика равна `готово`. Экспорт OpenCode-session для этого run содержит `providerID = openai` и `modelID = gpt-5.6-sol`; LM Studio transport не участвовал. Видимое и canonical состояния совпадают на FFmpeg-доказательстве `.temp/taskboard-t1222-opencode-gpt56-final.png`.

### Вложения и лимиты

v3 attachment original хранится под `.taskboard/attachments/<taskUid>/<attachmentId>/<safe-name>`, а производный текст/OCR/preview — под `.taskboard/derived/<taskUid>/<attachmentId>/<derivativeId>/<safe-name>`. Path segments metadata обязаны в точности совпадать с owning `taskUid`, `attachmentId` и `derivativeId`. Общий project path resolver отклоняет absolute/UNC/drive paths, backslash, colon/ADS, control characters, `< > " | ? *`, пустые/`.`/`..` segments, trailing dot/space и Windows reserved names без учёта регистра (`CON`, `NUL`, `COM1` и т. п.). Эти правила одинаковы для links/evidence/commands/workspaceChanges и attachment paths. Task schema не дублирует числовой максимум доски. Semantic validator для каждого original/derived blob выполняет canonical path/realpath containment, отклоняет reparse/symlink, требует обычный файл, проверяет byte length и SHA-256, затем применяет актуальные `perFile <= perTask <= board` limits из TaskBoard.

Original metadata сохраняет display name, media type, hash и size и после публикации append-only не может быть удалена, переименована или подменена под тем же `attachmentId`. Derived lifecycle хранит status `Pending|Ready|Failed|Unsupported|NotRequired`: `Ready` требует blob metadata, source hash, extractor name/version и timestamp; `Failed` требует непустую reason; остальные terminal/no-blob состояния не выдают фиктивный путь. Извлечённый текст или OCR не заменяет original. Preview может ссылаться только на attachment той же задачи с allowlisted raster MIME type. Поддерживаемый read-only retrieval API по task/attachment/optional derivative ID возвращает проверенные metadata и bytes через CLI boundary, поэтому агенту не нужен прямой filesystem access; перед выдачей повторно проверяются ownership, regular-file realpath containment, size и SHA-256.

### Ограничения размера

Schema v3 задаёт конечные пределы на отдельные значения и рабочие наборы: title — 512 символов, description — 262144, criterion description и один human-readable conversation/activity payload — 16384, до 256 criteria, до 1024 relations/artifacts и до 32 legacy fragments; один legacy Markdown fragment — не больше 1 MiB. Conversation и activity не имеют лимита количества, потому что являются lossless append-only history; storage защищается byte quotas и может отдавать summary + recent window, сохраняя отдельный доступ ко всему диапазону. Nullable string означает только `null` или непустую trimmed строку: `""` и строка из пробелов не являются третьим состоянием.

### Миграция v2→v3

`e2d tasks migrate --to-version 3` сначала строит dry-run report с digest каждого v2 board/task/blob и детерминированным UID mapping. Apply требует точный report SHA-256, исходную board revision и неизменившиеся digests. Миграция:

- сохраняет существующий `taskUid`, добавляет `boardId` и переводит placements/parent/dependencies на UID;
- удаляет stored `subtasks`, потому что дети вычисляются по parent;
- переводит labels и links в `tagIds` и typed objects;
- преобразует распознаваемые activity entries в typed payload, а остальные — в `Legacy` без потери исходного текста;
- переводит string commands в запрещённые `LegacyShell` specs;
- переносит attachment blobs из taskId-path в taskUid-path и повторно сверяет hash/size;
- создаёт optional migration provenance только для migrated board; provenance содержит `reportPath`, `reportSha256` и `sourceDigests`, ключи которых являются каноническими project-relative source paths; новая native v3 board не обязана иметь migration block;
- переводит scalar `requiredAccess`, строковый `externalAudit`, legacy acceptance actor reference и отсутствующие context fields в канонические v3 объекты без потери исходного текста.

Apply записывает board, все tasks и перемещаемые blobs одной recoverable transaction. Повторный apply идемпотентен. До finalize v2 остаётся исходным snapshot; finalize разрешён только после v3 schema validation, full semantic verify, consumer checks и точного reconstruction/provenance отчёта. Root, template и Platformer cutover выполняются одной и той же публичной CLI-командой без ручной правки `.e2task`/`.e2tasks`.

Для уже созданных pre-release v3 documents reader применяет узкий fail-closed compatibility lens до проверки: добавляет отсутствующие пустые conversation/context fields, переводит scalar access и строковый audit в typed форму, а legacy Comment — в message без потери текста. Для уже принятой terminal-задачи scalar audit нельзя задним числом превратить в доказанный `AuditRun`: исходная строка сохраняется как `Legacy/ExternalAuditContract`, а текущий typed contract становится `None`; новые и ещё не принятые задачи сохраняют требование `Single` и проходят обычный audit-run gate. Нераспознаваемый legacy path перестаёт считаться `File`/`Directory` и сохраняется как неисполняемый `Resource`. `Passed` без evidence у незавершённой задачи интерпретируется как `Open`; у ранее принятой terminal-задачи evidence ссылается на её неизменяемый `AcceptanceResult`. Если принятый draft был создан до появления criteria и определяется отсутствием новых root watermarks/workspace contract, lens добавляет ровно один `legacy-accepted-result` со ссылкой на то же acceptance event; у уже канонического TaskFile пустой набор criteria остаётся ошибкой. Lens не создаёт capability и не ослабляет validators для нативной v3-приёмки; следующая revision-aware mutation записывает каноническую форму. После cutover новые документы обязаны сразу соответствовать полной schema.

Для context checkpoint compatibility lens добавляет `lastActivitySequence` только когда поле действительно отсутствует в старом draft. Если checkpoint уже содержит этот watermark, его значение считается историческим immutable prefix и не синхронизируется с текущим корневым `lastActivitySequence`, даже если после checkpoint в задачу добавлены новые activity entries. Повторный upgrade канонического документа идемпотентен.

Structured update может заменить только явно переданные definition-поля и добавить соответствующий `TaskPatched`. Уже сохранённые `conversation.messages`, `contextCheckpoints`, activity и audit prefixes остаются неизменными, даже если корневой activity watermark уже ушёл вперёд после исторического checkpoint. Попытка подменить любое существующее поле checkpoint по-прежнему отклоняется transition validator-ом.

### CLI и граница записи

`e2d tasks init` создаёт сразу нативную v3-доску с `migration = null`. После появления board v2 обычные mutations отклоняются: v2 можно прочитать, проверить, экспортировать и перевести в v3, но нельзя продолжать изменять как рабочий формат. Единственным writer для v3 является `e2d tasks`; Tooling/MCP и редакторские представления читают проверенный snapshot или вызывают тот же CLI-контракт и не сериализуют собственную форму TaskFile.

`create` требует CAS по board revision, а task mutation — одновременный CAS по task revision, `lastMessageSequence` и `lastActivitySequence`; writer может получать watermarks из одного ранее прочитанного context capsule. Сложный payload передаётся JSON-объектом из файла или stdin: кроме коротких scalar-полей разрешены только `executionContract`, `acceptanceCriteria`, `links`, `tagIds`, `parentTaskUid`, `relations` и `assignee`. `workspaceChanges` и служебные audit/identity/lifecycle/context-поля запрещены, неполная command spec отклоняется schema validator, а повтор одного поля в JSON и CLI-флаге считается конфликтом. Conversation/activity/workspace mutation имеет отдельный trusted append route; после записи CLI возвращает проверенный task/board snapshot. Прямое редактирование `.e2task` не является поддерживаемой операцией.

Полный ответ чтения сохраняет тип каждой связи в `links`. Совместимая плоская проекция `linkedArtifacts` передаёт значения `File` без изменений, а к значению `Directory` добавляет ровно один завершающий `/`; само каноническое `links[].value` при этом остаётся нормализованным без завершающего разделителя. Благодаря этому старые клиенты отличают каталог от файла по явному признаку и не угадывают тип по точке в имени, например в `src/Electron2D.Cli`.

Разрешённые метаданные можно менять с проверкой revision и после доверенной приёмки задачи, в том числе когда задача уже перенесена в `completed`. Сохранённое состояние `Done` само по себе не запускает приёмку повторно: полномочие trusted acceptance требуется только для фактического перехода в `Done` или изменения защищённых acceptance audit fields. Поэтому `update` и `tag assign` могут менять `tagIds`, описание и другие обычные поля принятой задачи, но не могут подменить `acceptedAt`, `acceptedBy`, `acceptanceState` или существующую запись `AcceptanceResult`.

Для большой заранее проверенной классификации используется `e2d tasks tag apply --input <file|->`. Корневой JSON-объект содержит `expectedBoardRevision` и массив `tagUpdates`; каждый элемент задаёт `taskId`, `expectedRevision` и полный новый массив `tagIds`. Команда принимает активные и архивные задачи, отклоняет повтор task/tag ID, неизвестную ссылку и любую stale revision, затем один раз проверяет итоговый snapshot и записывает все изменённые TaskFile одной recoverable transaction. Ошибка одного элемента отменяет весь план. Команда не вычисляет теги и не меняет каталог, статус, placement или acceptance: смысловая классификация формируется вызывающей стороной и передаётся явно.

`tasks normalize` для v3 не исправляет смысловые ошибки и не выполняет скрытую миграцию. Он требует полностью валидный snapshot и меняет только неканонические JSON bytes, включая лишнее Unicode escaping, без изменения revision, lifecycle или activity. Повторный проход идемпотентен.

### Матрица источников истины

| Данные | Источник истины | Основная проверка |
| --- | --- | --- |
| UID, board membership, definition и lifecycle задачи | TaskFile v3 | Schema + transition validator |
| Display ID allocation, tags, groups, placement/rank, attachment policy | TaskBoard v3 | Schema + semantic validator + board CAS |
| Parent и relations | TaskFile v3 по UID | Semantic graph validator |
| Список детей и readiness | Вычисляется | Semantic graph evaluator |
| Conversation | Append-only TaskFile messages | Transition validator + `AgentContextBuilderV3` |
| Activity/audit history | Append-only TaskFile activity | `TaskTransitionValidatorV3` |
| Изменённые в задаче файлы | Trusted `workspaceChanges` + append-only `WorkspaceChangesUpdated` | Реальный workspace diff/hash + transition/semantic validators |
| Возможность выполнить command spec | Внешняя trusted execution policy | Executor authorization, не JSON Schema |
| Blob contents | `.taskboard/attachments/<taskUid>/...` и `.taskboard/derived/<taskUid>/...` | realpath + hash + dynamic board limits |
| Legacy provenance | Optional migration block/fragments | Migration report/finalize guard |

## Исторический срез `T-1147`

Первый подтверждённый RED/GREEN-срез перевёл canonical serializer на `.taskboard/tasks/<TaskId>.e2task`, `.taskboard/board.e2tasks` и `version = 2`. Task payload уже содержит UID/revision, execution contract, attachments и legacy fragments; board payload содержит revision, groups и placements без status columns. `.taskboard/**` классифицируется как `EditorMetadata`.

`e2d tasks init --format json` создаёт empty board, `tasks/`, `completed/`, `attachments/` и `.taskboard/.gitignore`, который исключает только lock/staging/transaction/cache state. Команда не перезаписывает существующую доску и поддерживает `--dry-run` через общий CLI contract.

Dependency validation теперь fail-closed отклоняет ссылку на отсутствующую задачу. Через cross-process lock реализованы первые disk operations: `create`, `board`, `list`, `get`, `update`, `move`, `set-status`, `dependency add/remove`, `parent set`, `group add`, `comment add`, `attachment add/remove`, `cancel`, `archive`, `unarchive`, `reopen`, guarded `delete` и `verify`. Mutations требуют expected task/board revisions; переход в `InProgress` отклоняется при незавершённых dependencies. `archive` переносит только `Done`/`Cancelled` между `.taskboard/tasks` и `.taskboard/completed`, а `reopen` сохраняет исторические acceptance/audit fields.

Store записывает text и binary payloads через `.taskboard/.staging` и durable manifests в `.taskboard/.transactions`: replay сверяет before/after SHA-256, допускает частично применённое after-state и fail-closed останавливается при третьем содержимом. Опубликованы TaskFile/TaskBoard JSON Schema draft 2020-12. Attachment slice применяет лимиты 25 MiB/250 MiB, запрещает reparse source и хранит blob вне JSON. `submit` доступен обычному CLI, но публичные `accept` и `request-changes` всегда требуют trusted human: VS Code после modal confirmation создаёт случайный 256-bit capability, передаёт его только через environment и private stdio payload, а CLI сам фиксирует actor/time. Миграция Markdown-корпуса, real repository finalize, consumer/template/reference cutover, audit-followup reader, safe scalar `--input <file|->`, group/parent mutations и VS Code extension реализованы. Для крупной repository-доски VS Code использует compact board snapshot и загружает полные details отдельным `tasks get`.

## Назначение

`ProjectTaskManager` реализован в `Electron2D.ProjectSystem` как внутренний слой задач пользовательского проекта. Внутренний слой означает код, доступный тестам и будущим Editor/Tooling/MCP-адаптерам, но ещё не публичный runtime API для игр.

Этот слой хранит задачи как first-class документы, а не как Markdown-backlog. Новая canonical storage — `.taskboard`; `.electron2d/tasks/**` остаётся только legacy v1 input для migration/export compatibility.

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

- `Ready`;
- `InProgress`;
- `Blocked`;
- `Review`;
- `Done`;
- `Cancelled`.

`ProjectTask` хранит id, title, description, status, readiness, blocking reasons, priority, rank, labels, assignee, creator, parent, dependencies, acceptance criteria, subtasks, activity, links на transactions/jobs/diagnostics/artifacts/scenes/resources/nodes и audit timestamps.

`AcceptanceCriterion` хранит stable `CriterionId`, description, state и evidence links. `TaskActivityEntry` хранит stable `ActivityEntryId`, audit-поля, kind и payload. Поддержаны activity kinds `Comment`, `Decision`, `Investigation`, `Blocker`, `TestResult`, `StatusChange`, `AgentSummary` и `AcceptanceResult`.

CLI criterion slice реализован в `TaskBoardDiskStore` и `e2d tasks criterion`: `add` создаёт уникальный criterion с default `Open`, `update` заменяет description с сохранением state/evidence links, `set-state` явно выбирает `Open`, `Passed` или `Failed`, `remove` удаляет criterion по stable ID. Все операции используют общий `MutateTask`, поэтому optimistic revision, `updatedAt`, transaction write, dry-run snapshot и diagnostics совпадают с другими task mutations. Lifecycle acceptance остаётся независимой: даже trusted переход задачи в `Done` сохраняет текущие criterion states.

## Приёмка и переходы статусов

`ProjectTaskManager.ChangeStatus(...)` применяет переходы через `WorkspaceTransactionEngine`. Целевой contract `T-1166`:

- `submit` для `Review` + `Open` сохраняет status `Review`, устанавливает `AcceptanceState=Submitted` и `SubmittedAt` и требует capability `TaskSubmitForAcceptance`;
- `Review` + `Submitted -> Done` через `accept` требует `PrincipalKind.Human` и capability `TaskAccept`;
- `Review` + `Submitted -> InProgress` через `request changes` требует `PrincipalKind.Human` и capability `TaskRequestChanges`;
- обычный переход `InProgress -> Review` устанавливает `AcceptanceState=Open`, чтобы повторная техническая проверка не выглядела уже отправленной;
- `Done` и `Cancelled` можно открыть заново через `Reopen` только с `PrincipalKind.Human` и capability `TaskReopen`.

AI-агент не может поставить `Done` или принять задачу, даже если payload пытается объявить себя человеком. `Reopen` сохраняет прошлые `CompletedAt`, `AcceptedAt`, `AcceptedBy` и историческое acceptance state в activity, а текущий `AcceptanceState` становится `Reopened`.

Legacy task file со `status=AwaitingAcceptance` читается compatibility reader-ом как `status=Review` и `AcceptanceState=Submitted`; `tasks normalize` сохраняет каноническое представление без изменения revision/activity. Новые schema, CLI mutations и UI значение `AwaitingAcceptance` не принимают.

В `T-1166` модель, disk store, Tooling/MCP, CLI, desktop Editor и VS Code переведены на единый `Review`. Вход `InProgress -> Review` устанавливает `AcceptanceState=Open`; `submit` сохраняет status и устанавливает `Submitted`; после submission обычный CLI `set-status` не может обойти human decision. `accept` переводит в `Done`, `request changes` — в `InProgress` + `ChangesRequested`. RED/GREEN зафиксирован для status/schema, core acceptance, CLI submit и legacy normalization; focused ProjectTaskManager/CLI прошёл 61 test, Editor 2, Tooling/MCP 12, external synchronizer 7. Три tracked board roots прошли idempotent normalize dry-run с 0 changes.

## Storage и transaction integration

`ProjectTaskStorage.GetTaskDocumentPath(taskId)` возвращает `.taskboard/tasks/<taskId>.e2task`, а board document хранится в `.taskboard/board.e2tasks`.

`ProjectTaskSerializer` пишет deterministic JSON с `format = "Electron2D.TaskFile"` или `format = "Electron2D.TaskBoard"` и `version = 2`. `.e2task` и `.e2tasks` классифицируются как JSON `EditorMetadata`.

Новый проект из шаблона получает стартовую доску `.taskboard/board.e2tasks` и задачу `.taskboard/tasks/welcome.e2task`. Эти файлы создаёт `ProjectTemplateCreator`; дальнейшие изменения идут только через `e2d tasks` или trusted adapters поверх того же command application, а не через прямую правку JSON.

## Markdown report export

`e2d tasks export` реализует внешний Markdown-отчёт поверх текущего task storage. Команда читает `.taskboard/tasks/*.e2task`, фильтрует задачи по status, labels/conventions для milestone, version, epic и agent session, а также по assignee, и пишет deterministic Markdown в stdout.

Отчёт не является хранилищем задач. `.taskboard/tasks/*.e2task` и `.taskboard/board.e2tasks` остаются canonical storage, то есть единственным проектным источником истины; экспорт никогда не создаёт второй task ledger.

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

В `T-1165` `Backlog` удалён из `ProjectTaskStatus`, TaskFile schema и таблицы CLI-переходов. `create` и `reopen` по умолчанию устанавливают canonical `Ready`. Для canonical `Ready` compact/full CLI snapshot вычисляет отдельный `boardStatus`: незавершённая dependency даёт `BlockedByDependencies`, отменённая dependency — `DependencyCancelled`, а любой manual/environment/decision/external blocker также размещает карточку в `Blocked`. После устранения причин следующий read снова возвращает `boardStatus=Ready` без скрытой записи dependent task; явно установленный workflow-статус `Blocked` остаётся явным до разрешённого status transition. Одноразовый compatibility reader принимает только legacy-файл со строкой `Backlog` и при `tasks normalize` записывает `Ready`; новые mutations и schema значение `Backlog` не принимают. В рабочей доске нормализованы 908 active task files, ещё один legacy welcome task нормализован в project template; повторные normalize изменили 0 файлов. `tasks verify` подтвердил 959 active / 202 completed задачи в repository, 1/0 в template и 6/0 в platformer example; raw search не находит `status=Backlog` ни в одной из трёх досок.

В `T-1180` исправлена системная ошибка legacy migration: `PopulateLinks` захватывал backtick-значения с `/`, включая команды с `--project`, поэтому clear shell commands попадали одновременно в `LinkedArtifacts` и execution contract. `tasks normalize` теперь распознаёт консервативный allowlist executable prefixes, переносит command payload в `RequiredCommands`, дедуплицирует по форме без surrounding backticks и сохраняет существующий command text. File/directory/unknown artifacts остаются на месте; revision, status, activity, audit и board revision не меняются. На repository board транзакционно нормализован 921 task-файл; повторный run изменил 0. Реальный `T-0244` сохранил revision 1, получил 11 path artifacts без команд и 7 required commands без потери четырёх behavioral command variants.

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

Focused проверка provenance Codex Desktop:

```powershell
dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~TasksCliPreservesCodexAgentIdentity" --no-restore -v:minimal
```

В `T-1215` тест сначала воспроизвёл сохранение новой Codex status activity как `Cli / cli`, затем прошёл после переноса trusted actor identity через `CliExecutionContext` в TaskBoard v3 status/comment mutations. Один сценарий проверяет terminal fallback, `Agent / Codex` для status и comment, а также неизменность более ранней `Cli / cli` activity. Payload и аргументы CLI не получили поля для подмены audit identity; human acceptance context не менялся.

Для обложек карточек focused RED/GREEN-срез проверяет round-trip `PreviewAttachmentId`, отказ для отсутствующего и нерастрового вложения, CLI `set-preview`/`clear-preview`, автоматический выбор первого изображения, compact metadata и fallback после удаления выбранного файла. Проверка `ProjectTaskManagerTests` завершилась `16/16`, а отдельный CLI-сценарий — успешно.
