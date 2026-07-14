# Markdown report export для Project Tasks

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт, состояние и проверки

Статус: целевая спецификация для `T-0156`.
Обновлено: 2026-06-23.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [ProjectTaskManager, TaskActivity и task storage](project-task-manager.md); [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md).

## Назначение

`e2d tasks export` создаёт человекочитаемый Markdown-отчёт по встроенным задачам пользовательского проекта. Отчёт нужен для публикации статуса, ревью и внешних заметок, но не является источником истины.

Каноническое хранилище остаётся прежним:

```text
.taskboard/*.e2task
.taskboard/board.e2tasks
```

Команда не должна создавать или обновлять `TASKS.md`, `completed-tasks/` или `dev-diary/` в пользовательском проекте. Эти файлы относятся только к локальному workflow разработки самого Electron2D.

## Команда

Минимальный supported вызов:

```powershell
e2d tasks export --project <path> --format markdown
```

Если `--format` не указан, команда пишет такой же Markdown-отчёт в stdout. Отчёт пишется только в stdout; shell redirection остаётся ответственностью вызывающего пользователя или CI.

Поддержанные фильтры:

- `--status <status>` — фильтрует по `ProjectTask.Status`, сравнение нечувствительно к регистру и принимает значения вроде `done`, `Ready`, `AwaitingAcceptance`;
- `--assignee <id>` — фильтрует по `ProjectTask.Assignee`;
- `--milestone <name>` — фильтрует по label `milestone:<name>`;
- `--version <value>` — фильтрует по label `version:<value>`;
- `--epic <name>` — фильтрует по label `epic:<name>`;
- `--agent-session <id>` — фильтрует по label `agent-session:<id>` или activity payload с `AgentSessionId=<id>`/`agentSession=<id>`.

`milestone`, `version`, `epic` и `agent-session` не вводят новую schema миграцию task storage. До появления отдельных полей они являются отчётными conventions поверх уже существующих `Labels` и `TaskActivityEntry.Payload`.

## Загрузка и безопасность

Команда читает только `.taskboard/*.e2task` через существующий `ProjectTaskSerializer`. `board.e2tasks` может использоваться только для предсказуемого порядка, но не изменяется.

При malformed task document команда должна завершиться fail-closed с `E2D-CLI-0002` и не писать partial report как успешный результат. Empty project без `.e2task` документов возвращает валидный Markdown с количеством задач `0`.

Команда не открывает `ProjectWorkspace` на запись, не выбирает active Editor route, не вызывает `WorkspaceTransactionEngine`, не создаёт Undo group и не меняет `.taskboard/**`.

## Markdown output

Отчёт обязан быть deterministic:

- не содержит текущую дату или локальный absolute path;
- всегда содержит заголовок `# Project Tasks Report`;
- содержит строку, что Markdown является report only и не заменяет canonical task storage;
- содержит `Source`, `Filters` и `Task count`;
- группирует задачи по статусам;
- внутри группы `Done` сортирует задачи по `CompletedAt DESC`, затем `TaskId`;
- внутри остальных групп сортирует задачи по board order, затем `Rank`, `CreatedAt`, `TaskId`;
- перечисляет task id, title, status, priority, rank, assignee, labels, created/completed/accepted timestamps, criteria states и последние activity entries в стабильном порядке.

Labels в отчёте сортируются ordinal. Activity entries сортируются по `CreatedAt`, затем `ActivityEntryId`; в отчёт попадают только последние три записи задачи, чтобы отчёт оставался компактным.

## Критерии приёмки

- Есть CLI-команда `e2d tasks export --format markdown`.
- Команда фильтрует задачи по status, milestone, version, epic, assignee и agent session, если эти сведения представлены в task data.
- Markdown output стабилен, сортируется предсказуемо и покрыт exact golden test.
- Команда не создаёт и не обновляет `TASKS.md`, `completed-tasks/` или `dev-diary/` в пользовательском проекте.
- Implementation documentation описывает, что Markdown export является отчётом, а не canonical task storage.
- Focused проверка документирована.
