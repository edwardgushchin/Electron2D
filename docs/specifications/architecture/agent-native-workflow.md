# Agent-native cross-platform 2D game engine workflow Electron2D 0.1

Статус: целевая архитектурная спецификация.
Задача: `T-0114`.
Статус задачи: completed.
Архив: `completed-tasks/2026/06 Июнь.md#T-0114`.
Обновлено: 2026-06-22.

## Позиционирование

Electron2D — C#-first кроссплатформенный 2D-движок с согласованным API, спроектированный для совместной разработки человеком и AI-агентами.

Ключевое обещание `0.1.0`: `Electron2D.Editor` является основным живым рабочим пространством проекта. Разработчик может пользоваться редактором полностью вручную, но AI-агент видит актуальное состояние открытого проекта, выполняет семантически значимые операции редактора, запускает тестовый прогон, наблюдает результат и безопасно объединяет свои изменения с ручными правками.

Семантически значимая операция — это изменение проекта, сцены, ресурса, кода, настроек, импорта, диагностики, тестового запуска или экспорта. Перетаскивание dock-ов, смена темы, размер окна и другие действия, не меняющие проектный смысл, не входят в обязательный AI-паритет.

Это архитектурное требование, а не требование встроенного чата. Agent-native cross-platform 2D game engine означает, что редактор, локальный MCP-сервер, CLI, CI и будущие IDE-интеграции работают с одной моделью проекта через стабильные команды, текстовые файлы, структурированную диагностику и машиночитаемую документацию. Встроенная LLM, генерация игры по одному prompt, облачный аккаунт и привязка к одному AI-провайдеру в `0.1.0` не нужны.

Короткая продуктовая формулировка:

> Electron2D — Agent-native cross-platform 2D game engine.

Английский слоган:

> Agent-native cross-platform 2D game engine.

## Обновлённая цель 0.1

Electron2D 0.1 позволяет человеку и AI-агенту создать небольшую законченную 2D-игру на C#, работая в открытом `Electron2D.Editor` с одной живой моделью проекта. Headless-режим, то есть запуск без окна редактора, остаётся обязательным для CI, тестов, пакетных операций и автономной работы агента, но не определяет основной пользовательский workflow.

Главный вертикальный срез:

```text
Человек + AI
      ↓
открытый Electron2D.Editor
      ↓
Live ProjectWorkspace
      ↓
сцены, ресурсы, код, диагностика и запущенная игра
      ↓
проверка, исправление, тесты и экспорт
```

AI не кликает по экрану, не распознаёт кнопки по пикселям и не управляет редактором через GUI automation. Он семантически управляет открытым Editor через Tooling/MCP-команды и локальный IPC, то есть межпроцессное соединение на той же машине. Если Editor закрыт, те же команды создают headless workspace для CI или автономного сценария.

## Целевая зависимость

```text
                       ┌─ Electron2D.Editor UI
                       │
AI agent ── MCP/IPC ───┤
                       │
CLI / CI ──────────────┤
                       ↓
             Live ProjectWorkspace
             ├── Tooling commands
             ├── revisions
             ├── transactions
             ├── undo/redo
             ├── change events
             ├── import/build state
             └── diagnostics
                       ↓
             scenes / resources / code
                       ↓
             RuntimeController
                       ↓
               running game process
```

`Live ProjectWorkspace` — единая авторитетная модель текущего проекта в памяти. Под ней понимается не новый публичный runtime API, а внутренний слой редактора и tooling, который хранит открытые документы, несохранённые изменения, ревизии, журнал операций, состояние импорта, сборки и диагностики.

Когда Editor открыт, он владеет рабочей сессией. CLI и MCP обнаруживают активную сессию через named pipe на Windows или Unix domain socket на Linux/macOS и направляют изменяющие команды в неё. Когда Editor закрыт, CLI/MCP создают headless `ProjectWorkspace` и работают напрямую с файлами проекта.

Недопустимая зависимость:

```text
Electron2D.Editor
      ↓
OpenAI/Anthropic/Gemini integration, зашитая напрямую в редактор
```

Редактор может запускать разные локальные AI-клиенты, но не должен становиться приватной реализацией API конкретного поставщика моделей.

## Live ProjectWorkspace

`ProjectWorkspace` должен включать:

- `DocumentStore` — хранилище открытых сцен, ресурсов, настроек и текстовых документов с признаком dirty, то есть с несохранёнными изменениями;
- `CommandBus` — единый вход для проектных команд редактора, Tooling, MCP, CLI и тестов;
- `ChangeEventStream` — поток событий об изменениях для Scene Tree, Inspector, FileSystem dock, viewport, diagnostics и Agent Workspace panel;
- `RevisionStore` — версии документов и объектов, которые позволяют понять, к какому состоянию применялась команда;
- `OperationJournal` — журнал происхождения операций и восстановления: человек, AI-сессия, CLI, внешний файл или тест;
- `UndoRedo` — обычная история отмены и повтора, в которую AI-транзакция попадает одной группой;
- `ImportState` — состояние импорта ресурсов: ожидает, импортируется, готово или ошибка;
- `BuildState` — состояние сборки и перезагрузки кода;
- `DiagnosticsStore` — актуальные ошибки, предупреждения и suggested fixes, то есть безопасные предлагаемые исправления.

`ProjectWorkspace` не заменяет систему задач. Его `OperationJournal` отвечает на вопрос «какие технические операции произошли?»: workspace-команда, revision, actor, undo group, conflict, crash recovery. Цель работы, статус задачи, критерии приёмки и смысловые заметки хранятся в отдельном `ProjectTaskManager`.

Открытый Editor не должен держать приватную модель сцены, которую невозможно изменить через Tooling. Операция «добавить `Sprite2D` в сцену» должна быть одной и той же независимо от того, вызвал её пользователь через Scene Tree dock, AI через MCP tool, CLI-команда, тест или будущий IDE-плагин.

Для одного project root допускается только один основной владелец рабочей сессии (`primary workspace owner`), то есть один Editor, который имеет право менять живую модель проекта. Если тот же проект открывается во втором Editor, второй экземпляр должен либо открыться в режиме только для чтения (`read-only`), либо получить понятный отказ с ссылкой на активную сессию. Активная сессия обязана иметь lease/heartbeat, обнаружение stale endpoint и безопасное освобождение lock после crash.

`OperationJournal` должен поддерживать восстановление после падения Editor:

- snapshot dirty-документов, если это безопасно и не раскрывает секреты;
- запись незавершённой транзакции;
- очистку временных файлов после crash;
- сообщение о восстановленной или отброшенной AI-транзакции при следующем запуске;
- запрет молча продолжать частично применённую транзакцию.

## Document model и canonical architecture

`ProjectWorkspace` опирается на базовую модель документа: identity документа, UID объектов, parser/serializer, persisted revision, in-memory revision и structural diff. Эта core-часть должна существовать до полноценного `ProjectWorkspace`, потому что workspace не может корректно отслеживать dirty state и merge без понятной идентичности документов.

Проверяемый контракт этой core-части вынесен в [Canonical document model, revision model и structural diff](../project-system/canonical-document-model.md). Он фиксирует classification документов, стабильный `DocumentId`, object UID, revision transitions и минимальный structural diff до formatter/migrations/schemas.

Старые документы целей, если они описывают компонентную модель `SpriteRenderer`/`Rigidbody`/`AudioSource`, обязательный `Transform` у каждого `Node`, отсутствие script-binding или только четыре платформы без iOS, считаются историческими, пока не синхронизированы с этой спецификацией и `docs/specifications/releases/0.1.0-preview.md`.

Canonical architecture для `0.1.0 Preview`:

- специализированная node/resource модель Electron2D, совместимая с выбранным API-подмножеством Godot;
- `Node2D` и его наследники имеют 2D transform; базовый `Node` не получает обязательный transform;
- `scene_attach_script` не добавляет отдельный Script-компонент. Операция связывает сериализованный узел с пользовательским C#-типом, наследующим подходящий Electron2D node type, и после сборки создаётся единый экземпляр этого типа;
- поддерживаемые платформы релизного контракта включают Windows, Linux, macOS, Android и iOS, но mobile export может оставаться заблокированным до закрытия соответствующих platform tasks и проверок.

## Единое ядро инструментов

`Electron2D.Tooling` — общий слой семантических операций над `ProjectWorkspace`. Его используют редактор, CLI, MCP-сервер, CI и будущие IDE-интеграции.

Минимальный набор сервисов:

```text
Electron2D.Tooling
├── ProjectService
├── TaskService
├── SceneService
├── ResourceService
├── ScriptService
├── ImportService
├── BuildService
├── TestService
├── ExportService
├── RuntimeService
└── DocumentationService
```

Изменяющие команды возвращают структурированный результат с `success`, `operation`, `workspaceRevision`, `documentRevisions`, `persistedRevision`, `dirtyDocuments`, `persistenceState`, `changedFiles`, `changedObjects`, `createdObjects`, `diagnostics` и `undoGroupId`.

Все изменяющие команды должны принимать `expectedRevision`, если они работают с уже открытым документом или объектом. Это защита от потери ручных изменений: команда явно говорит, к какой версии состояния она применялась.

Команды делятся на четыре режима применения:

| Режим | Назначение |
| --- | --- |
| `WorkspaceOnly` | Меняет живую модель Editor, создаёт Undo-запись и помечает документы dirty. На диск автоматически не пишет. |
| `SaveAffectedDocuments` | Сохраняет уже применённые workspace-изменения через временные файлы и атомарную замену. |
| `HeadlessCommit` | Применяет и сохраняет транзакцию атомарно. Используется CLI/CI, когда Editor закрыт или команда явно запущена в headless-режиме. |
| `ExternalImport` | Файл уже изменён на диске. Синхронизатор сравнивает его с `PersistedRevision` и объединяет с текущим dirty-состоянием. |

Editor-команда по умолчанию использует `WorkspaceOnly`. Сохранение на диск — отдельная операция или явный режим. Это запрещает AI-команде автоматически сохранять ручные несохранённые изменения разработчика и не заставляет UI ждать успешной записи на диск перед обновлением живой модели.

## WorkspaceSnapshot для build/test/run

`build`, `test` и `run` не должны читать произвольное состояние с диска, если открытый Editor содержит dirty-документы или открытые code buffers. Перед долгой операцией `ProjectWorkspace` создаёт immutable `WorkspaceSnapshot` — неизменяемый снимок входного состояния:

```text
WorkspaceSnapshot
├── SnapshotId
├── WorkspaceRevision
├── ContentRevision
├── DocumentRevisions
├── DirtyDocuments
├── OpenCodeBuffers
└── CreatedAt
```

Snapshot фиксирует ровно то состояние, которое проверяет build/test/run job. Если отдельный game process или build toolchain не умеет читать workspace из памяти, snapshot материализуется во временную директорию:

```text
.electron2d/workspaces/<session>/<snapshot-id>/
```

Материализация snapshot не считается сохранением проекта и не меняет `PersistedRevision`. Временная директория является internal workspace artifact: её нельзя редактировать вручную, добавлять в source control или использовать как новый источник правды.

Каждый job и каждый artifact, который относится к build/test/run/export/import diagnostics или runtime capture, возвращает:

```text
InputSnapshotId
InputWorkspaceRevision
InputContentRevision
InputDocumentRevisions
InputBuildConfigurationHash
```

`WorkspaceRevision` меняется от любого изменения рабочей сессии: task activity, board rank, selection, editor camera, scene/resource/code edits или project settings. `ContentRevision` меняется только от документов и настроек, влияющих на build/test/run/export/runtime result.

Job, diagnostics, screenshot, runtime tree или visual diff помечаются как `stale` только тогда, когда после старта job изменился один из `InputDocumentRevisions`, `InputContentRevision` или build/run configuration, от которых зависел job. Изменение task status, `TaskActivity`, board rank, selection или другого `EditorMetadata`, не входящего в inputs job, не делает игровой artifact stale.

Политика export отличается от run/test/build:

- по умолчанию экспортируется только clean persisted state;
- export dirty snapshot требует явной команды и показывает, что результат собран из несохранённого снимка;
- export никогда не сохраняет ручные dirty-документы неявно;
- release artifact не может считаться готовым, если он построен из dirty snapshot без явного acceptance решения.

Импорт, сборка, тесты, экспорт и запуск игры являются долгими операциями и используют job-контракт, а не обычный синхронный result:

```json
{
  "operationId": "op_01",
  "kind": "build",
  "inputSnapshotId": "snap_01",
  "inputWorkspaceRevision": 42,
  "inputContentRevision": 17,
  "inputDocumentRevisions": {
    "scenes/main.e2scene": 8,
    "scripts/Player.cs": 3
  },
  "inputBuildConfigurationHash": "sha256:...",
  "state": "running",
  "progress": 0.42,
  "canCancel": true,
  "stale": false,
  "diagnostics": [],
  "artifacts": [],
  "startedAt": "2026-06-22T11:00:00+03:00",
  "completedAt": null
}
```

Состояния job: `queued`, `running`, `succeeded`, `failed`, `cancelled`.

Поток событий долгих операций:

- `operation.started`;
- `operation.progress`;
- `operation.diagnostic`;
- `operation.artifactProduced`;
- `operation.completed`.

Job contract предоставляет общий event stream, cancellation semantics и snapshot identity. CLI JSONL, MCP events, Agent Workspace UI, Editor build/import/test panels, screenshots и runtime snapshots подключают этот поток в своих адаптерных задачах, не превращая core job contract в зависимость от готового UI или протокольного клиента.

## Editor UX baseline

Godot 4 является каноническим UX- и layout-референсом для `Electron2D.Editor 0.1.0`. Это означает знакомую структуру редактора: верхнее меню, переключатель центральных workspaces, scene/document tabs, `Scene` и `FileSystem` docks слева, `Inspector`/`Node` справа, bottom panel для output/debug/diagnostics и run controls в верхней зоне.

Electron2D меняет этот baseline только там, где это связано с продуктовым контрактом:

- `3D` workspace полностью отсутствует;
- 3D nodes, 3D viewport, 3D gizmos, 3D settings, 3D shortcuts и disabled 3D controls не появляются в UI;
- `AssetLib` отсутствует в `0.1.0`;
- `Script` поддерживает только C#;
- вместо `AssetLib` и `3D` в центральном переключателе есть `Tasks`;
- `Agent Workspace` является постоянным dock рядом с любым workspace.

Центральные workspaces:

- `2D` — scene editing, viewport tools, snapping, `Camera2D` preview, collision overlays, `TileMap` и `Control` layout;
- `Script` — встроенная базовая C# IDE с language services и managed debugger;
- `Game` — видимый run/debug workspace с runtime scene tree, screenshots, frame stepping, input injection и performance counters;
- `Tasks` — центральная доска `ProjectTaskManager`, а не dock или bottom panel.

Полный UX-контракт описан в `docs/specifications/editor/godot4-editor-reference.md`.

## Agent Workspace panel

Терминальный dock — только точка запуска локального агента. В редакторе нужна более широкая Agent Workspace panel:

- профили запуска `codex`, `opencode`, `claude code` и других локально установленных клиентов без привязки к одному поставщику;
- терминал в корне проекта без подстановки секретов;
- статус текущей AI-сессии и последнего действия;
- список изменённых сцен, узлов, ресурсов, scripts и настроек;
- переход к изменённому узлу или ресурсу;
- подсветка затронутого объекта во viewport;
- diagnostics и suggested fixes, полученные во время работы агента;
- screenshots и runtime snapshots, созданные агентом;
- текущую задачу `ProjectTaskManager`, её статус, связанные операции, jobs, diagnostics и artifacts;
- остановка, пауза или cancel текущей агентской операции, если command поддерживает отмену;
- grouped undo последней AI-транзакции обычным редакторским Undo.

Default placement: правая dock area под `Inspector`/`Node` либо в одной dock-группе с ними. Панель должна быть dockable, resizable, hideable, movable, maximizable, сохранять layout между запусками и оставаться доступной в `2D`, `Script`, `Game` и `Tasks`.

Панель не должна быть единственным способом AI-интеграции. Если агент подключается снаружи через MCP, Editor обязан показывать его операции в той же панели и в обычных UI-обновлениях.

Агент, запущенный профилем из Editor, должен автоматически получить session-scoped MCP configuration и подключиться к активной Editor-сессии. Без этого Agent Workspace превращается в обычный терминал с coding agent.

Bootstrap запуска:

```text
Editor creates agent session
    ↓
session ID + local endpoint + ephemeral token
    ↓
temporary MCP configuration
    ↓
agent process starts in project root
    ↓
MCP handshake
    ↓
Agent Workspace shows Connected
```

Ephemeral token — временный локальный токен только для этой сессии. Он не должен попадать в project files, logs, screenshots, `AGENTS.md`, shell history или commit.

После handshake агент должен уметь прочитать текущую сцену, selection, revisions, dirty documents и diagnostics. Для агента, запущенного вне Editor, должна быть документированная команда подключения к существующей сессии.

Проверяемый минимум `T-0149` для запуска локального агента:

- Editor-side bootstrapper публикует профили `codex`, `opencode` и `claude-code`; профиль задаёт человекочитаемое имя, команду запуска и безопасный способ передать путь к MCP-конфигурации.
- Для каждого запуска создаются `AgentSessionId`, local endpoint и ephemeral token с временем истечения. Token передаётся только во временный MCP config file и never appears in process arguments, environment variable values intended for logs, status strings, diagnostics, shell history text или project files.
- Temporary MCP configuration создаётся вне project root и вне `.electron2d/`; в project files не появляются `mcp`, `token`, `secret`, `agentSessionId` или путь к временной конфигурации.
- Process start plan использует `WorkingDirectory = projectRoot`, `UseShellExecute = false`, не запускает shell wrapper и передаёт агенту только путь к config file через environment variable `ELECTRON2D_MCP_CONFIG`.
- Тестовый launcher может заменить реальный process start. Acceptance не требует запускать облачный AI-провайдер, но production path должен иметь один route для реального launcher-а.
- Handshake принимает `AgentSessionId` и token, проверяет expiry, подключается к active Editor route через MCP adapter kind и возвращает состояние `Connected` только если агент прочитал project summary, open documents, dirty documents, document revisions, selection resource и diagnostics resource.
- Неверный token или истёкший token переводят Agent Workspace state в `HandshakeError` или `TokenExpired` со structured diagnostic without secret echo.
- `Disconnect` переводит Agent Workspace state в `Disconnected`, не освобождая primary Editor lease.
- Agent Workspace state для этой задачи является model-only contract: `Starting`, `Connected`, `Disconnected`, `HandshakeError`, `TokenExpired`, profile id, session id, route, redacted config path, last diagnostic codes и timestamps. Реальный dock UI подключается отдельной задачей `T-0150`.

## Мгновенная синхронизация Editor и AI

Есть два пути синхронизации.

### Семантическая операция

```text
AI
 ↓
MCP / Tooling command
 ↓
ProjectWorkspace transaction
 ↓
Workspace events
 ↓
Scene Tree + Inspector + FileSystem + Viewport + Diagnostics
 ↓
dirty-документы или явная операция сохранения
```

Например:

```text
scene_set_property(
    sceneUid,
    nodeUid,
    "Position",
    [320, 180],
    expectedRevision
)
```

Scene Tree, Inspector и viewport должны обновиться в том же editor dispatch cycle, то есть в ближайшем цикле обработки UI-событий редактора. Полное перечитывание сцены с диска для такой операции не требуется.

### Внешнее изменение файла

Coding agent всё равно может напрямую создать `.cs`, `.e2scene`, JSON, изображение, audio или shader file. Поэтому нужен `ExternalChangeSynchronizer` — внутренний синхронизатор внешних изменений:

```text
FileSystemWatcher
 ↓
debounce/coalescing
 ↓
create/change/move/delete detection
 ↓
parse + validation
 ↓
structural diff
 ↓
ProjectWorkspace transaction
 ↓
Editor update
```

Обязательные свойства:

- recursive file watching;
- отслеживание create/change/move/delete;
- сохранение UID при переименовании и перемещении;
- подавление событий от собственных записей Editor;
- игнорирование `.git`, `.electron2d/import-cache`, `bin`, `obj`, временных файлов и generated artifacts;
- инкрементальный импорт без полного rescanning проекта;
- обновление уже открытой сцены;
- conflict handling для dirty документов;
- структурированная диагностика parse/import/build ошибок.
- fallback при потере или объединении событий watcher-а.

Критерии задержки:

- semantic Tooling operation обновляет UI в том же editor dispatch cycle;
- external text-file change обнаруживается и отображается не позднее 250 мс после стабилизации записи как P95 на локальной файловой системе Tier 1 desktop-платформ; сетевые диски и перегруженные watcher-и должны деградировать в диагностируемый режим, а не обещать абсолютную задержку;
- новый импортируемый asset сразу появляется в FileSystem dock со статусом `Importing`, `Compiling` или `Error`;
- preview и зависимые объекты обновляются после завершения импорта или сборки;
- пользователь не нажимает `Refresh` и не перезапускает Editor;
- Editor не блокируется полным переимпортом проекта.

Полноценный C# Hot Reload не обязателен для `0.1`. Допустим быстрый rebuild и автоматический перезапуск preview/play session, если файл кода изменился.

Fallback-стратегия watcher-а:

- normal event — incremental update;
- переполнение watcher-а (`watcher overflow`) — сверка затронутого каталога;
- неоднозначное переименование — сверка по UID/hash;
- возврат Editor к работе после паузы — лёгкая проверка согласованности проекта.

Группировка Undo для внешних изменений:

- операции через MCP/Tooling — одна именованная Undo-группа;
- прямые файловые изменения — external transaction на debounce batch;
- для группировки прямых изменений агент использует `workspace_begin_transaction` / `workspace_commit_transaction`;
- build/import/run/export не входят в Undo проекта;
- удаление и перезапись файлов отменимы только при наличии сохранённого snapshot.

## Совместное редактирование человеком и AI

`ProjectWorkspace` должен защищать ручные изменения и AI-изменения от взаимного затирания.

Минимальная модель:

- revision/ETag для сцены, ресурса, настроек и открытого текстового документа;
- `expectedRevision` во всех mutating operations, то есть командах, которые изменяют проект;
- optimistic concurrency, то есть попытка применить команду к ожидаемой версии без глобальной блокировки всего проекта;
- structural merge для непересекающихся изменений;
- conflict panel, если человек и AI изменили одно и то же свойство или один из участников удалил объект, изменённый другим;
- operation provenance, то есть отметка происхождения операции: человек, AI-сессия, CLI, внешний файл;
- grouped undo для всей агентской транзакции.

Политика применения:

| Ситуация | Поведение |
| --- | --- |
| Editor-документ чистый | Изменение AI применяется автоматически |
| Человек и AI изменили разные свойства | Изменения структурно объединяются |
| Изменено одно и то же свойство | Показывается conflict panel |
| AI удаляет изменённый человеком узел | Автоматическое применение запрещено |
| Изменение пришло от Tooling | Попадает в Undo/Redo как одна транзакция |
| Изменение пришло прямой записью файла | Импортируется как external transaction |

Merge policy зависит от типа документа:

| Тип документа | Merge |
| --- | --- |
| Scene/resource/settings | UID/property structural three-way merge. |
| C#/JSON/text | Обычный three-way text merge. |
| Generated files | Не редактируются и не участвуют в автоматическом merge. |
| Изображения/audio/fonts | Автоматическое merge запрещено. |
| Удаление/замена binary | Конфликт, если ресурс используется или был изменён в workspace. |

Открытые C# buffers являются first-class `CodeDocument` в `ProjectWorkspace`, а нормативным владельцем таких buffers в Editor является центральный workspace `Script`. Если AI меняет `.cs` через Tooling/MCP или прямой файл, а разработчик держит несохранённую правку того же файла во встроенном `Script` workspace, синхронизатор обязан применить text merge или показать конфликт, но не перезаписать один вариант молча.

Граница code editing:

```text
Human typing
    → TextBufferEditSession
    → lightweight CodeDocumentChanged events
    → TextBufferUndo
    → CodeDocument.Revision

AI / refactoring / multi-file operation
    → WorkspaceTransactionEngine
    → WorkspaceUndo
    → OperationJournal
    → shared UndoGroupId

Save
    → persistence transaction
    → atomic write
    → PersistedRevision
```

`TextBufferEditSession` публикует grouped/debounced document-change events для dirty state, live Roslyn Workspace и MCP-наблюдения, но не создаёт полноценную `OperationJournal` entry на каждый символ. Агентский `script_save` не может сохранить документ, если после базовой revision агента появились ручные unsaved changes; требуется structured conflict либо интерактивное подтверждение разработчика.

IntelliSense, hover, navigation и live diagnostics работают через live Roslyn Workspace, синхронизированный с `CodeDocument`, и используют `DocumentRevision` + `SemanticVersion`. `WorkspaceSnapshot` используется для воспроизводимых build/test/run/debug и пакетного анализа, а не для каждого completion request или hover.

Code editor использует двухуровневую Undo-модель:

- `TextBufferUndo` — локальный Undo ручного ввода, удаления, paste и локального форматирования внутри одного buffer. Последовательный ввод coalescing-ится в разумные edit groups, меняет `CodeDocument.Revision`, но не создаёт отдельную `OperationJournal` entry на каждый символ.
- `WorkspaceUndo` — глобальный Undo для AI `script_apply_text_edits`, rename symbol, code actions на несколько файлов, attach script, create/rename/delete file и grouped rollback всей AI-транзакции. Такая операция создаёт одну осмысленную workspace transaction, один `UndoGroupId` и одну запись происхождения операции.

AI/refactoring text edits должны отображаться как compound operation в каждом затронутом buffer и одновременно иметь общий `UndoGroupId` для отмены всех файлов.

C# language services строятся в отдельном `Electron2D.CSharpLanguageServices` assembly, работающем внутри Editor process. Для `0.1.0` отдельный language-service process не проектируется; его можно добавить позже отдельной задачей с IPC, restart, crash recovery и синхронизацией процесса. Каждый async request/result содержит `ProjectId`, `DocumentId`, `DocumentRevision`, `SemanticVersion` и `ConfigurationHash`; если buffer изменился до ответа, результат отбрасывается. Live diagnostics debounce-ятся, предыдущие запросы отменяются, а изменения `.csproj`, `Directory.Build.props`, `Directory.Build.targets`, `global.json` и package references перезагружают semantic model. Tooling adapters подключаются позже поверх этих сервисов.

Managed debugger Electron2D `0.1` использует DAP как обязательную протокольную границу:

```text
Electron2D.Editor
      ↓
ManagedDebugClient
      ↓ DAP
packaged .NET debug adapter
      ↓
Electron2D game process
```

Отклонение от DAP допускается только новым архитектурным решением, если `T-0163` докажет отсутствие подходящего распространяемого adapter. Конкретный packaged .NET debug adapter выбирается отдельным technical spike до реализации полноценного debugger. Spike проверяет Windows x64, Linux x64, macOS arm64, Portable PDB, launch/attach, breakpoints, stepping, locals/watches, exceptions, restart, DAP capability matrix, лицензию, право распространения вместе с Electron2D и способ обновления adapter binaries. Editor не должен зависеть от API конкретного debugger напрямую.

Debugging core живёт отдельно от UI и Tooling adapters:

```text
Electron2D.ManagedDebugging
    DAP client, breakpoint model, debug sessions

Electron2D.Editor
    Script/Debugger UI

Electron2D.Tooling
    adapters к language/debug services

Electron2D.Mcp
    MCP presentation
```

Breakpoint model использует stable `ManagedBreakpoint` с `BreakpointId`, `DocumentId`, `SourceAnchor`, `Enabled`, `Verified`, `ResolvedLine`, `ResolvedColumn`, `LastBoundSnapshotId` и `AdapterMessage`. Breakpoint следует за document rename через `DocumentId`, text edits выполняют source-anchor rebase, а неоднозначный rebase помечает breakpoint как unverified вместо молчаливого переноса. Breakpoints являются локальными Editor metadata, физически хранятся в `.electron2d/user/breakpoints.e2debug`, игнорируются Git, переживают перезапуск Editor и не попадают в `WorkspaceSnapshot`, runtime export или production package.

Пример группировки:

```text
Agent transaction: "Add player movement"
├── created scripts/Player.cs
├── changed scenes/player.e2scene
├── changed project.input
└── added tests/player_movement.cs
```

Разработчик должен иметь возможность одним `Undo` отменить всю такую транзакцию.

## Editor capability manifest

Фраза «AI может всё, что может Editor» должна быть проверяемым контрактом для семантически значимых операций. Для этого нужен `Editor Capability Manifest`:

```json
{
  "capability": "scene.node.set_property",
  "kind": "projectMutation",
  "editorCommand": "SceneSetProperty",
  "toolingCommand": "scene.setProperty",
  "mcpTool": "scene_set_property",
  "cliBinding": {
    "kind": "dedicatedCommand",
    "command": "e2d scene set"
  },
  "status": "supported"
}
```

Классы capability:

| Класс | Назначение |
| --- | --- |
| `projectMutation` | Меняет проектные документы: add node, set property, edit resource, Input Map. Имеет revisions, dirty state и Undo. |
| `editorSessionAction` | Меняет только состояние Editor: select node, open document, focus Inspector, move editor camera, highlight object. Не повышает document revision и обычно не входит в Undo проекта. |
| `runtimeAction` | Управляет `RuntimeSession`: run, pause, step, inject input, capture screenshot. |
| `backgroundJob` | Запускает import/build/test/export и использует job-контракт. |
| `readOnlyQuery` | Читает состояние без изменения проекта или Editor-сессии. |

Manifest должен покрывать:

- создание, открытие, сохранение и удаление сцен;
- добавление, удаление, duplicate, rename и reparent узлов;
- изменение всех поддерживаемых Inspector properties;
- создание и редактирование ресурсов;
- назначение текстур, материалов, шрифтов, аудио и scripts;
- подключение сигналов;
- groups;
- Input Map;
- Project Settings;
- SpriteFrames;
- AnimationPlayer tracks;
- TileMap;
- UI themes;
- import settings;
- main scene;
- export presets;
- script create/open/read/rename/delete/search/apply text edits/save/format;
- script diagnostics, completions, signature help, hover, definition, document symbols, references, rename symbol и code actions;
- debugger breakpoints, breakpoint update и watches;
- debug start/attach/pause/continue/stop/restart;
- debug step into/over/out;
- debug threads, stacks всех threads, frame-scoped locals/arguments, watch definitions и explicit watch evaluation;
- запуск текущей сцены и проекта;
- stop/restart/pause;
- step frame и step physics;
- input injection;
- screenshot;
- runtime inspection;
- запуск тестов;
- просмотр diagnostics.

AI не должен редактировать C# через эмуляцию клавиатуры или pixel automation. Для `Script` workspace нужны семантические Tooling/MCP-команды:

```text
script_create
script_open
script_read
script_rename
script_delete
script_search_text
script_apply_text_edits
script_save
script_format
script_get_diagnostics
script_get_completions
script_get_signature_help
script_get_hover
script_get_definition
script_get_document_symbols
script_find_references
script_rename_symbol
script_get_code_actions
script_apply_code_action

debug_set_breakpoint
debug_update_breakpoint
debug_remove_breakpoint
debug_start
debug_attach
debug_restart
debug_pause
debug_continue
debug_step_into
debug_step_over
debug_step_out
debug_get_threads
debug_get_stack
debug_get_locals(frameId)
debug_get_arguments(frameId)
debug_get_watches
debug_evaluate_watches(frameId)
debug_add_watch
debug_update_watch
debug_remove_watch
debug_stop
```

`debug_update_breakpoint` управляет включением, отключением и изменением breakpoint через `BreakpointId`. `debug_get_stack()` возвращает stacks всех threads. `debug_get_locals(frameId)` и `debug_get_arguments(frameId)` явно читают данные выбранного stack frame. `debug_get_watches()` возвращает только определения watches без вычисления expressions; `debug_evaluate_watches(frameId)` вычисляет watches в явно указанном frame. Tooling/MCP не должны зависеть от скрытого UI selection.

`debug_attach` в `0.1.0` не является произвольным `attach(pid)` для агента. Agent MCP/Tooling session может attach только к game process активной Editor play/debug session. Любой attach к другому процессу отсутствует либо требует явного интерактивного подтверждения разработчика.

Editor показывает действия агента в обычном `Script` workspace: изменённые строки, diagnostics, breakpoints, текущий stack frame и состояние debug session. Просмотр locals, arguments, watch definitions и простых values разрешён как baseline; `debug_evaluate_watches(frameId)` должен использовать безопасный режим без side effects там, где adapter это поддерживает. Вычисление выражений с возможными side effects требует отдельного явного подтверждения разработчика.

Не все script/debug команды требуют `WorkspaceSnapshot`:

- `script_get_completions`, `script_get_signature_help`, `script_get_hover`, `script_get_diagnostics`, `script_get_definition`, `script_get_document_symbols`, `script_find_references`, `script_search_text` и `script_get_code_actions` используют `DocumentRevision`, `SemanticVersion` и live Roslyn Workspace;
- `script_apply_text_edits`, `script_format`, `script_rename`, `script_delete`, `script_rename_symbol` и `script_apply_code_action` используют `expectedRevision` и создают workspace transaction только для фактического изменения документов;
- `script_save` использует persistence transaction и проверяет, что нет ручных unsaved changes после базовой revision агента;
- `debug_start`, `debug_restart`, build, run и test используют immutable `WorkspaceSnapshot`.

Tooling и MCP должны иметь полный паритет для семантически значимых возможностей. CLI не обязан иметь отдельную удобную команду для каждого действия Editor, если есть универсальный headless-путь:

```json
{
  "capability": "animation.track.insert_key",
  "kind": "projectMutation",
  "toolingBinding": "animation.track.insertKey",
  "mcpBinding": "animation_insert_key",
  "cliBinding": {
    "kind": "genericTransaction"
  }
}
```

Для действий, которые не применимы к CLI, допускается явное исключение:

```json
{
  "capability": "editor.camera.pan",
  "kind": "editorSessionAction",
  "cliBinding": {
    "kind": "notApplicable",
    "reason": "Editor-session navigation only"
  }
}
```

Release-gate инвариант:

```text
Если Editor capability объявлена Supported в 0.1:
    Tooling binding должен быть Supported.
    MCP binding должен быть Supported.
```

`partial` допустим для Tooling или MCP только тогда, когда сама Editor capability тоже объявлена `partial` и не входит в release-required semantic operations. Нельзя считать AI-паритет выполненным при состоянии `Editor: Supported`, `MCP: Partial` или `Tooling: Partial`.

Для CLI допустимы `genericTransaction` и `notApplicable`, если Tooling/MCP имеют полную поддержку. CI должен падать, если Editor получил новую семантически значимую операцию без полного Tooling binding, полного MCP binding или корректного CLI-исключения.

## CLI как обязательный headless-интерфейс

`e2d` — командный интерфейс Electron2D для CI, headless-режима, пакетных операций и автоматизации без открытого Editor. Он остаётся first-class продуктовым интерфейсом, но основной пользовательский workflow строится вокруг открытого Editor и `ProjectWorkspace`.

Минимальные группы команд:

- `project create`, `project inspect`, `project validate`;
- `scene create`, `scene inspect`, `scene add-node`, `scene set`, `scene attach-script`, `scene connect`;
- `resource inspect`, `resource dependencies`;
- `import`;
- `build`;
- `run`;
- `test`;
- `export`;
- `docs search`, `docs type`, `docs member`, `docs example`;
- `api compare-godot`;
- `mcp serve`;
- `context build`;
- `doctor`.

Каждая команда должна поддерживать:

- `--help`;
- `--format text|json|jsonl`;
- `--quiet`;
- `--verbose`;
- `--dry-run`, если команда меняет проект;
- `--project <path>`.

Если активная Editor-сессия найдена, изменяющие CLI-команды должны по умолчанию работать через неё или явно требовать флаг headless-режима, чтобы не было двух независимых владельцев состояния проекта.

Проверяемый CLI contract вынесен в [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md). Он фиксирует common flags, stable JSON/JSONL envelope, generic workspace transaction path, job stream fields и route mode между active Editor и headless workspace.

## Текстовый формат проекта

Все семантически значимые данные проекта должны храниться в исходных текстовых файлах:

- project settings;
- scenes;
- resources;
- input map;
- translations;
- animation data;
- sprite frame definitions;
- themes;
- export presets без секретов.

Бинарными могут быть изображения, аудио, шрифты, импортированный cache, скомпилированные шейдеры и production asset packs.

Текстовый формат обязан иметь:

- стабильный порядок полей;
- постоянные UID;
- явные имена типов;
- явные ссылки на внешние ресурсы;
- отсутствие абсолютных путей;
- отсутствие editor-specific шума;
- небольшой локальный diff при изменении одного свойства;
- schema version;
- автоматические миграции;
- команду валидации;
- canonical formatter.

Для JSON-представлений должна публиковаться JSON Schema. Целевая версия схемы для `0.1.0` — JSON Schema Draft 2020-12.

Текстовый формат не заменяет live-синхронизацию. Он нужен для diff, code review, автономных агентов и восстановления состояния, но открытый Editor обязан применять изменения через `ProjectWorkspace`, а не только через полное перечитывание файлов.

Проверяемый formatter/schema/migration contract для первой реализации описан в [Stable project text formats, migrations и JSON Schema](../project-system/project-text-formats.md). Этот документ уточняет, какие source JSON files покрыты сейчас и какие typed schemas остаются за доменными задачами.

## Машиночитаемый API manifest

Electron2D должен поставлять версионированный API manifest в JSON. Manifest нужен, чтобы AI-агенты, CLI, Inspector, Wiki, source generators и будущий language server не угадывали границы утверждённого Godot-compatible 2D-профиля.

Эталон для `0.1.0` — Godot `4.7-stable` .NET/C# API. Electron2D реализует 100% публичного C# API только внутри утверждённого 2D-профиля под namespace `Electron2D`; GDScript и API вне профиля не входят в контракт.

Manifest содержит:

- engine version;
- классы и наследование;
- constructors/factories;
- свойства;
- методы;
- сигналы;
- enum и flags;
- значения по умолчанию;
- nullability;
- поддерживаемые Variant-типы;
- доступность по платформам;
- требования к renderer profile;
- статус `supported`, `parity_verified`, `out_of_profile`, `planned`;
- ссылку на Godot `4.7-stable` C# manifest для типов профиля;
- strict parity result для типов профиля;
- примеры использования;
- версию появления или изменения элемента.

## Карта совместимости с Godot

Electron2D API требует формального profile manifest, а не общей фразы в README. Для каждого публичного типа из 2D-профиля документация должна содержать блок:

```text
Godot 4.7 C# profile compatibility
Profile: Electron2D 0.1.0 2D
Status: Supported / Parity verified
Out of profile: no
```

Для типов внутри release profile статусы `partial` и `experimental` недопустимы. Они могут существовать только для API вне обязательного профиля `0.1.0` или для будущих расширений.

Команда `e2d api compare-godot <type> --format json` является строгим verifier-ом для типов профиля. Для включённого типа она должна возвращать:

```text
missing types: 0
missing members: 0
signature mismatches: 0
inheritance mismatches: 0
default mismatches: 0
unexpected changes: 0
```

Для типа вне профиля команда должна явно возвращать `out_of_profile`, а не рекомендовать обходной API.

## MCP и Editor-hosted Agent Gateway

MCP означает Model Context Protocol: локальный протокол, через который AI-клиент получает типизированные tools, resources и prompts. Electron2D должен предоставлять локальный, необлачный MCP-сервер, не привязанный к конкретной модели или поставщику:

```bash
e2d mcp serve
```

MCP остаётся тонким адаптером над `Electron2D.Tooling`, а не второй независимой реализацией.

Проверяемый MCP adapter contract вынесен в [Локальный MCP-сервер поверх active Editor session и Tooling](../mcp/mcp-server.md). Он фиксирует resources/tools manifest, route semantics, task acceptance guard, job event fields и fail-closed security без привязки к облачному AI-провайдеру.

Когда Editor открыт:

```text
Agent
  ↓
MCP
  ↓
active Editor session
  ↓
ProjectWorkspace
```

Когда Editor закрыт:

```text
Agent / CI
  ↓
MCP или CLI
  ↓
headless ProjectWorkspace
```

Editor-hosted Agent Gateway — локальная точка подключения к активной сессии Editor. Она должна отдавать агенту:

- текущую открытую сцену;
- выделенный узел или ресурс;
- dirty documents;
- revision открытых документов;
- положение editor camera;
- состояние Inspector;
- текущие diagnostics;
- import/build state;
- active play session;
- capabilities Editor и runtime.

Gateway должен поддерживать:

- session lease/heartbeat;
- отказ или read-only режим для второго Editor на тот же project root;
- stale endpoint cleanup;
- mismatch project root diagnostics;
- crash-safe release lock;
- запрет работы за пределами project root;
- отключение MCP без деградации ручного Editor workflow.

Проверяемый contract registry/discovery/gateway вынесен в [Editor session discovery и Editor-hosted Agent Gateway](../tooling/editor-session-discovery.md). Он фиксирует безопасный endpoint descriptor, normalized project root verification, stale cleanup, crash-safe release lock и явный headless fallback для CLI/MCP adapter-ов.

Минимальные MCP resources:

- `electron2d://project/summary`;
- `electron2d://project/settings`;
- `electron2d://project/scenes`;
- `electron2d://project/resources`;
- `electron2d://project/diagnostics`;
- `electron2d://workspace/open-documents`;
- `electron2d://workspace/selection`;
- `electron2d://workspace/import-state`;
- `electron2d://workspace/build-state`;
- `electron2d://scene/{uid}`;
- `electron2d://resource/{uid}`;
- `electron2d://api/type/{name}`;
- `electron2d://api/godot-compatibility/{name}`;
- `electron2d://editor/capabilities`;
- `electron2d://runtime/capabilities`;
- `electron2d://runtime/session`;
- `electron2d://docs/topic/{name}`.

Минимальные MCP tools:

- `project_validate`, `project_build`, `project_run`, `project_test`, `project_export`;
- `scene_create`, `scene_inspect`, `scene_add_node`, `scene_remove_node`, `scene_move_node`, `scene_set_property`, `scene_attach_script`, `scene_connect_signal`;
- `resource_inspect`, `resource_import`, `resource_find_references`;
- `workspace_get_state`, `workspace_apply_transaction`, `workspace_resolve_conflict`, `workspace_undo_transaction`;
- `runtime_start`, `runtime_stop`, `runtime_pause`, `runtime_step`, `runtime_inject_input`, `runtime_capture_frame`, `runtime_get_scene_tree`, `runtime_get_diagnostics`.
- `task_list`, `task_get`, `task_create`, `task_update`, `task_claim`, `task_set_status`, `task_add_subtask`, `task_add_dependency`, `task_append_activity`, `task_link_transaction`, `task_link_job`, `task_link_artifact`, `task_submit_for_acceptance`, `task_accept`, `task_request_changes`, `task_cancel`.

`task_accept` и `task_request_changes` являются операциями человеческой приёмки. Для агентской сессии они недоступны без доверенного `OperationContext` с interactive Editor capability и краткоживущим подтверждением Editor UI. `task_cancel` отменяет задачу как больше не нужную и переводит её в `Cancelled`; это отдельное workflow-действие, а не отказ от результата на приёмке.

## Контекстный пакет проекта

`e2d context build` генерирует компактный статический контекст для AI-агента:

```text
.electron2d/context/
├── project-summary.json
├── api-surface.json
├── godot-differences.json
├── scene-index.json
├── resource-graph.json
├── diagnostics.json
└── conventions.md
```

Контекст включает версию движка и .NET, renderer profile, main scene, список сцен, основные узлы, пользовательские классы, Input Map, autoload/services, связи ресурсов, текущие ошибки, платформенные ограничения и команды сборки/тестирования.

Контекстный пакет не заменяет живые MCP resources открытого Editor. Он нужен для CI, автономного анализа и клиентов, которые не держат постоянное соединение с Editor.

Контекст не должен содержать импортированные бинарные данные, весь исходный код движка, секреты подписи, огромные логи, содержимое `.git` и неиспользуемые API.

## ProjectTaskManager и TaskActivity

Пользовательские проекты Electron2D используют встроенный `ProjectTaskManager` как единственный источник состояния задач. Отдельные `TASKS.md`, `completed-tasks/` и `dev-diary/` в пользовательских проектах не создаются шаблоном, не упоминаются в project-local `AGENTS.md`, не имеют отдельного UI и не являются экспортируемым источником правды.

Модель разделяется так:

| Система | Назначение |
| --- | --- |
| `ProjectTaskManager` | Что нужно сделать, кто делает, статус, зависимости и критерии приёмки. |
| `TaskActivity` | Осмысленный ход выполнения: комментарии, решения, исследования, blockers, результаты проверок и итог agent session. |
| `OperationJournal` | Низкоуровневая история workspace-операций, revisions, Undo-групп, конфликтов и crash recovery. |
| Specifications / ADR | Итоговые архитектурные решения и причины. |
| Diagnostics / Job artifacts | Подтверждение результата: тесты, сборки, screenshots, ошибки и другие artifacts. |
| Git | Сохранённые изменения репозитория. |

Task и board documents являются first-class документами `ProjectWorkspace`. Из этого следуют обязательные правила:

- task mutations проходят через `WorkspaceTransactionEngine`;
- поддерживают `expectedRevision`;
- участвуют в dirty state и `SaveAffectedDocuments`;
- поддерживают grouped Undo/Redo;
- внешние изменения проходят через `ExternalChangeSynchronizer`;
- конфликтующие правки не затираются;
- UI обновляется в том же editor dispatch cycle.

Минимальная доменная модель core-слоя:

```text
ProjectTaskManager
├── TaskStore
├── TaskActivityStore
├── TaskDependencyGraph
├── TaskAcceptanceService
└── TaskArtifactLinks
```

Доска первой версии:

```text
Backlog
Ready
In Progress
Blocked
Review
Awaiting Acceptance
Done
Cancelled
```

Модель задачи:

```text
Task
├── TaskId
├── Title
├── Description
├── Status
├── Readiness
├── BlockingReasons
├── Priority
├── Rank / SortKey
├── Labels
├── Assignee
├── CreatedBy
├── ParentTaskId
├── Dependencies
├── AcceptanceCriteria
├── Subtasks
├── Activity
├── LinkedTransactions
├── LinkedJobs
├── LinkedDiagnostics
├── LinkedArtifacts
├── LinkedScenesResourcesAndNodes
├── CreatedAt
├── UpdatedAt
├── SubmittedAt
├── CompletedAt
├── AcceptedAt
├── AcceptedBy
├── AcceptanceState
├── ArchivedAt
├── ArchivedBy
└── CancellationReason
```

`TaskActivityEntry` поддерживает типы:

- `Comment`;
- `Decision`;
- `Investigation`;
- `Blocker`;
- `TestResult`;
- `StatusChange`;
- `AgentSummary`;
- `AcceptanceResult`.

Каждая activity entry имеет стабильный UID:

```text
TaskActivityEntry
├── ActivityEntryId
├── ActorId
├── ActorKind
├── CreatedAt
├── Kind
└── Payload
```

`ActorId`, `ActorKind` и `CreatedAt` являются audit fields. Их заполняет `TaskActivityStore` из доверенного `OperationContext` и системных часов; вызывающий агент, CLI или MCP payload не может передать или переписать эти поля как обычные данные activity.

Acceptance criteria тоже имеют стабильные UID, чтобы одновременные добавления человеком и AI не конфликтовали как правка одного массива:

```text
AcceptanceCriterion
├── CriterionId
├── Description
├── State
└── EvidenceLinks
```

AI может выполнить задачу и перевести её в `Awaiting Acceptance`, но не может самостоятельно установить `Accepted` или `Done`, если проект требует человеческой приёмки. Это контролируется `TaskAcceptanceService`, а не текстовой инструкцией в `AGENTS.md`.

`ActorKind`, записанный в activity, является audit metadata, а не полномочием. Изменяющие операции получают доверенный `OperationContext`, который создаётся Editor, Tooling host или MCP gateway и не принимается из payload задачи. Контракт находится ниже `Electron2D.Tooling` в общем project-system/contracts слое, чтобы `TaskAcceptanceService` из core `ProjectTaskManager` не зависел от будущего `TaskService`:

```text
Electron2D.ProjectSystem/Operations/
├── OperationContext
├── PrincipalKind
└── OperationCapability
```

```text
OperationContext
├── PrincipalId
├── PrincipalKind
├── SessionId
├── Capabilities
└── Origin
```

Agent MCP session получает capability `Task.SubmitForAcceptance`, но не получает `Task.Accept` или `Task.RequestChanges`. Interactive Editor user может получить `Task.Accept`, `Task.RequestChanges` и `Task.Cancel`. Краткоживущее подтверждение приёмки выдаёт только Editor UI; агент не может отправить `ActorKind = Human` или произвольный `PrincipalKind = Human` через Tooling/MCP и обойти guard.

`Review` и `Awaiting Acceptance` означают разное:

- `Review` — техническая проверка: тесты, диагностика, code review или проверка другим агентом;
- `Awaiting Acceptance` — результат подготовлен и ждёт решения разработчика;
- `Done` — разработчик принял результат;
- `Request changes` означает, что результат не принят и задача возвращается в `In Progress`;
- `Cancel` означает, что задача больше не нужна, и переводит её в `Cancelled`.

Разрешённые переходы task state:

| Текущий статус | Разрешённые переходы |
| --- | --- |
| `Backlog` | `Ready`, `Cancelled` |
| `Ready` | `Backlog`, `InProgress`, `Blocked`, `Cancelled` |
| `InProgress` | `Ready`, `Blocked`, `Review`, `Cancelled` |
| `Blocked` | `Ready`, `InProgress`, `Cancelled` |
| `Review` | `InProgress`, `AwaitingAcceptance`, `Blocked` |
| `AwaitingAcceptance` | `InProgress`, `Done` |
| `Done` | человеческое действие `Reopen` переводит в `Ready` |
| `Cancelled` | человеческое действие `Reopen` переводит в `Backlog` |

`Done` недоступен AI на уровне `TaskAcceptanceService`, даже если вызов пришёл не через MCP, а через другой Tooling adapter.

`Reopen` — это действие, а не отдельный статус. Расширенная команда может явно выбрать целевой статус через `task_reopen(targetStatus: Backlog | Ready | InProgress)`, если это разрешено policy. `CompletedAt`, `AcceptedAt`, `AcceptedBy` и прошлый `AcceptanceState` не удаляются: reopen добавляет `TaskActivityEntry` и задаёт новый текущий acceptance state, сохраняя историю принятия.

`Status = Blocked` является ручным workflow-состоянием. Dependency blocking хранится отдельно:

```text
Readiness:
    Ready | BlockedByDependencies

BlockingReasons:
    dependency | environment | decision | external | manual
```

`TaskDependencyGraph` обязан запрещать циклические зависимости, не переводить задачу в `Ready` автоматически, если обязательная dependency ещё не завершена, возвращать диагностируемую причину blocked-состояния, обновлять только dependency-related blocking reason после закрытия dependency и явно описывать поведение при отмене dependency. Завершение dependency не должно снимать ручной blocker, например отсутствие устройства или внешнее решение.

Обычное удаление задачи заменяется на `Cancel` или `Archive`. Hard delete разрешён только для задачи без activity, dependencies и внешних ссылок либо после отдельного destructive confirmation с проверкой всех references. Архивация использует `ArchivedAt` и `ArchivedBy`, не требует отдельной колонки и скрывает карточку из обычных представлений.

Каждая агентская операция связывается с активной задачей:

```text
TaskId
AgentSessionId
OperationId
TransactionId
SnapshotId
JobId
```

Agent Workspace должен показывать текущую задачу, её статус, активного агента, changed objects, transactions, test/job results и diagnostics. Так разработчик видит не только поток действий, но и цель, ради которой они выполнены.

Каноническое хранилище задач — стабильные текстовые metadata-документы проекта:

```text
.electron2d/
└── tasks/
    ├── task-01J....e2task
    ├── task-01K....e2task
    └── board.e2tasks
```

Файлы задач имеют schema version, стабильные UID, canonical formatting, небольшой diff, structural merge и migrations. Editor, AI и пользователь не обязаны редактировать их вручную; Tooling/MCP и headless-команды должны уметь их читать. SQLite или другой индекс допустим только как cache, а не как единственный canonical source.

`.electron2d/tasks/**` является `EditorMetadata`, а не игровыми ресурсами. Task documents:

- хранятся в source control только по решению разработчика;
- доступны Editor, Tooling, CLI и MCP;
- не импортируются как игровые ресурсы;
- не попадают в production asset packs;
- не включаются в APK, AAB, app bundle и desktop distribution;
- не материализуются в runtime snapshot как игровые файлы;
- если экспортируются во внешний отчёт, то только отдельной явной report-командой.

Job может хранить `TaskId`, но не обязан и не должен по умолчанию копировать содержимое задачи в build directory.

Project template не должен добавлять `.electron2d/` целиком в `.gitignore`, иначе вместе с cache будут потеряны задачи. Политика по умолчанию:

```gitignore
.electron2d/import-cache/
.electron2d/workspaces/
.electron2d/context/
.electron2d/session/
.electron2d/user/
```

`.electron2d/tasks/` отслеживается по умолчанию либо включается отдельной настройкой Project Manager; в обоих случаях template обязан сделать выбор явным.

Completed tasks — это представление `Status = Done ORDER BY CompletedAt DESC`, а не отдельная папка. Markdown-отчёт может генерироваться отдельной P1 CLI/report задачей, если она реализована:

```bash
e2d tasks export --status done --format markdown
```

Такой экспорт является отчётом, не обязателен для `0.1.0` и не становится источником истины.

## Project-local `AGENTS.md` и skills

Каждый новый проект должен содержать `AGENTS.md` — предсказуемое место для инструкций coding-агентам. Шаблон должен быть похож по назначению на глобальный пользовательский `AGENTS.md`, но не должен копировать приватные правила пользователя.

Проектный `AGENTS.md` должен описывать:

- версию Electron2D и .NET;
- renderer profile;
- команды validate/build/test/run/export;
- структуру проекта;
- запрет редактировать import cache;
- правило стабильных UID;
- правило проверки через `e2d validate`;
- предупреждение не использовать Godot API вне утверждённого Electron2D 2D-профиля;
- команду `e2d api compare-godot <type>` как strict verifier для спорных API внутри профиля;
- правило подключаться к активной Editor-сессии, если она открыта;
- правило использовать `ProjectTaskManager` через Editor, Tooling или MCP;
- запрет редактировать task storage files напрямую;
- правило связывать изменения, tests, diagnostics, jobs и artifacts с активной задачей;
- правило отправлять завершённую работу на человеческую приёмку через `task_submit_for_acceptance`.

Новый проект также получает стартовые project-local skills для создания сцены, написания gameplay-кода, импорта ресурсов, запуска тестов и подготовки экспорта.

## Видимое управление запущенной игрой

AI-агент должен уметь не только собрать проект, но и проверить поведение в наблюдаемом разработчиком запуске.

Основной путь:

```text
Electron2D.Editor
      ↓ RuntimeController
отдельный game process
      ↕ debug bridge
AI / Editor debugger
```

Игра запускается в embedded viewport редактора или отдельном видимом окне. Отдельный process нужен, чтобы падение игры не роняло Editor.

Минимальные возможности:

- запуск текущей сцены и всего проекта;
- stop/restart;
- pause;
- step frame;
- step physics;
- input injection через Input Map;
- screenshot;
- runtime scene tree;
- inspect node properties;
- metrics;
- runtime diagnostics;
- подсветка изменённого или выбранного runtime-узла в Remote Scene Tree.

Headless-сценарий остаётся обязательным:

```bash
e2d run --scene scenes/main.e2scene --frames 600 --fixed-delta 0.0166667 --input tests/input/start-game.json --capture-frame 300 --output artifacts/run-001
```

Результат headless-запуска:

```text
artifacts/run-001/
├── result.json
├── diagnostics.json
├── runtime.log.jsonl
├── frame-0300.png
├── scene-tree-final.json
└── performance.json
```

Проверяемый контракт CLI flags, input trace, runtime artifacts, JSON schemas и snapshot identity вынесен в [Headless runtime automation](../runtime/headless-runtime-automation.md).

## Тестовый framework для игр

Кроме обычного `dotnet test`, Electron2D 0.1 должен иметь средства для scene tests и visual tests.

Минимальные возможности:

- fixed seed;
- fixed timestep;
- advance one frame;
- advance physics frame;
- имитация Input Map;
- поиск узлов;
- чтение свойств;
- ожидание сигнала;
- screenshot;
- pixel-diff;
- timeout;
- проверка утечек ресурсов после завершения сцены.

Битовая детерминированность физики между всеми платформами не требуется, но один тест должен воспроизводиться на одной платформе при одинаковой конфигурации.

Core test framework возвращает machine-readable diagnostics, screenshots, pixel-diff artifacts и snapshot identity. Отображение хода тестов и visual diff в Editor или Agent Workspace подключается отдельными Editor/UI-задачами поверх этих artifacts.

## Структурированная диагностика

Любая ошибка должна иметь структуру:

- `code`;
- `severity`;
- `category`;
- `message`;
- `file`;
- `line`;
- `column`;
- scene UID;
- node path;
- resource UID;
- related locations;
- suggested fix;
- documentation URI.

Для compiler, shader и validation diagnostics должен быть возможен вывод SARIF 2.1.0:

```bash
e2d validate --format sarif > electron2d.sarif
```

Diagnostics core должен быть пригоден для live stream, то есть для потока актуальных ошибок и предупреждений без ручного перезапуска проверки. Editor и MCP получают этот поток через отдельные adapters, которые не входят в минимальный `Diagnostics.Core`.

## Безопасное изменение проекта

Изменяющие операции AI должны быть транзакционными, но путь транзакции зависит от режима.

`WorkspaceOnly`:

```text
read current workspace revision
    ↓
validate current state
    ↓
apply operation in memory
    ↓
merge or detect conflicts
    ↓
validate resulting state
    ↓
publish workspace events
    ↓
mark affected documents dirty
    ↓
return changed objects, dirty documents and diagnostics
```

`SaveAffectedDocuments`:

```text
read dirty documents
    ↓
validate persisted revision
    ↓
write temporary files
    ↓
atomic replace
    ↓
update persisted revision
    ↓
return changed files and persistence state
```

`HeadlessCommit`:

```text
parse persisted files
    ↓
validate current persisted state
    ↓
apply transaction in memory
    ↓
validate resulting state
    ↓
write temporary files
    ↓
atomic replace
    ↓
return changed files, revisions and diagnostics
```

`ExternalImport`:

```text
read changed file
    ↓
compare with persisted revision
    ↓
parse + structural diff
    ↓
merge with dirty workspace state
    ↓
publish workspace events or conflict
```

Для task documents `ExternalImport`, migration, CLI, crash recovery и любая другая запись проходят через доменный `TaskTransitionValidator` и `TaskAcceptanceService`. Внешний файл получает непривилегированный контекст:

```text
PrincipalKind = ExternalFile
Capabilities  = Task.EditUnprivilegedFields
Origin        = ExternalImport
```

Обычный payload, включая direct file edit, не может управлять privileged fields:

```text
CreatedBy
CreatedAt
UpdatedAt
SubmittedAt
CompletedAt
AcceptedAt
AcceptedBy
AcceptanceState
ArchivedAt
ArchivedBy
TaskActivityEntry.ActorId
TaskActivityEntry.ActorKind
TaskActivityEntry.CreatedAt
```

Также нельзя импортировать privileged transitions без доверенной команды:

```text
AwaitingAcceptance -> Done
Done -> Ready
Cancelled -> Backlog
```

При попытке такого импорта Editor не должен молча отбрасывать поля. Изменение переходит в conflict или pending-import state, а diagnostics возвращают стабильный structured code и объяснение, какую trusted operation нужно выполнить.

Обязательны `--dry-run`, atomic writes для save/headless paths, automatic backup для миграций, защита от записи за пределами project root, запрет изменения import cache через scene API, список затронутых файлов, validation before commit, стабильные UID, отсутствие молчаливого удаления неизвестных свойств, audit log операций MCP и undo group для каждой AI-транзакции.

MCP-сервер не должен автоматически подписывать Android/iOS сборки, читать keystore/certificates, публиковать игру, удалять произвольные файлы или выполнять произвольный shell без отдельного разрешения.

## Воспроизводимость

Проект должен фиксировать Electron2D version, .NET SDK range, NuGet dependencies, native package versions, asset importer versions, renderer profile, physics backend version и serialization schema version.

Минимальные файлы:

- `global.json`;
- `electron2d.lock.json`.

`e2d doctor --format json` проверяет установленный .NET SDK, версию Electron2D, native runtime, Android SDK/NDK, Xcode, export templates, Vulkan/Metal capabilities и доступность signing configuration без раскрытия секретов.

## Документация, пригодная для AI

Wiki остаётся, но одной Wiki недостаточно. Документация должна поставляться вместе с конкретной версией движка в трёх представлениях:

| Представление | Для кого |
| --- | --- |
| Wiki/HTML | Человек |
| XML documentation | C# IDE и compiler tooling |
| JSON API manifest | AI, CLI, Inspector, генераторы |

CLI должен уметь искать локальную документацию:

```bash
e2d docs search "move and slide"
e2d docs type CharacterBody2D --format json
e2d docs member CharacterBody2D.MoveAndSlide
e2d docs example "platformer movement"
```

Документация каждого публичного API должна содержать назначение, сигнатуру, lifecycle restrictions, thread affinity, ownership/disposal, пример, ошибки, платформенные ограничения и renderer restrictions. Отличия от Godot описываются только для API вне обязательного 2D-профиля `0.1.0`; для типов внутри профиля требуется `Parity verified`.

## Что Agent-native cross-platform 2D game engine не означает

Первая версия не обязана иметь:

- встроенное окно ChatGPT;
- собственную LLM;
- генерацию ассетов;
- генерацию игры по одному prompt;
- облачный аккаунт;
- автономную публикацию игры;
- API, привязанный к одному поставщику;
- обучение модели на пользовательском проекте;
- визуального агента, кликающего по Editor;
- возможность произвольно выполнять shell-команды через движок;
- обязательный C# Hot Reload;
- AI-зависимость для ручного использования Editor.

Editor должен оставаться полноценным инструментом разработки без AI. AI дополняет Editor, а не заменяет ручной workflow и не становится единственным владельцем проекта.

## Приоритеты 0.1

Critical для 0.1:

- Live `ProjectWorkspace`;
- immutable `WorkspaceSnapshot` для build/test/run/export artifacts;
- общий Tooling слой семантических операций;
- Editor session discovery и локальный IPC;
- MCP подключение к активной Editor-сессии;
- Agent Workspace panel;
- `ProjectTaskManager` и `TaskActivity`;
- live external-change synchronizer;
- human-AI concurrent editing;
- grouped Undo/Redo для AI-транзакций;
- Editor Capability Manifest и parity verifier;
- текстовые сцены и ресурсы;
- стабильные UID;
- structured diagnostics;
- API manifest;
- карта отличий от Godot;
- visible runtime control;
- debug bridge;
- scene tests и visual tests;
- headless validation/build/test/export;
- локальная документация.

High для 0.1:

- `AGENTS.md` template;
- project-local skills;
- context pack;
- visual regression UX в Editor;
- runtime metrics panel.

Можно отложить: Hot Reload, Edit and Continue, remote debugger для Android/iOS, сложные solution-wide refactorings, visual shader editor, сложный `AnimationTree`, skeletal animation, расширенный particle editor, полноценный profiler UI, plugin marketplace, расширенная dock-система сверх Godot 4 baseline, встроенный AI-chat и автоматическая публикация в магазины.

## Критерии приёмки Agent-native cross-platform 2D game engine

Нужны два benchmark-набора.

### Editor co-development benchmark

1. `Electron2D.Editor` уже открыт.
2. Агент подключается к активной Editor-сессии через MCP/IPC.
3. Агент создаёт `.cs`-файл, и он появляется в FileSystem dock без ручного refresh.
4. Агент добавляет узел в открытую сцену, и он сразу появляется в Scene Tree и viewport.
5. Агент меняет свойство узла, и Inspector обновляется автоматически.
6. Разработчик вручную меняет другое свойство, и оба изменения сохраняются.
7. Разработчик и AI меняют одно и то же свойство, и Editor показывает conflict panel, а не теряет данные.
8. Агент запускает текущую сцену, и разработчик видит запущенную игру.
9. Запуск использует `WorkspaceSnapshot`, поэтому видимая игра отражает dirty-изменения игровых документов; изменение task activity после старта job не делает screenshot или runtime tree `stale`.
10. Агент ставит игру на паузу, делает один frame step, отправляет input и получает screenshot.
11. Агент обнаруживает ошибку через structured diagnostics и исправляет её.
12. Агент открывает `Script` workspace, применяет text edits через `script_apply_text_edits`, получает live diagnostics и completion result без эмуляции клавиатуры.
13. Агент ставит breakpoint, запускает текущую сцену под managed C# debugger, останавливается на breakpoint, читает stacks всех threads, `locals`/`arguments` для явного `frameId`, получает watch definitions, выполняет `debug_evaluate_watches(frameId)` и продолжает выполнение.
14. Agent Workspace показывает current task, linked transactions, linked jobs, diagnostics и artifacts.
15. Агент переводит задачу в `Awaiting Acceptance`, но не может сам установить `Done` или обойти приёмку через подмену `ActorKind`/`PrincipalKind`.
16. Разработчик одним Undo отменяет последнюю агентскую транзакцию.
17. После отключения AI проект остаётся полностью редактируемым вручную.
18. Агент аварийно завершается во время чтения состояния, и session lease освобождается без повреждения проекта.
19. Агент аварийно завершается во время staged-транзакции, и Editor сохраняет целостное состояние с понятной diagnostics.
20. Editor запускается и работает вручную, когда MCP отключён или недоступен.

### Headless benchmark

Тестовый агент получает установленный Electron2D, пустую директорию, документацию Electron2D, CLI/MCP, текстовое описание задачи, без доступа к исходному коду движка, без ручной помощи и без управления Editor мышью.

Агент должен выполнить пять заданий:

1. Создать проект с main scene и экспортируемым свойством.
2. Изменить сцену: добавить `Sprite2D`, `Camera2D`, `CollisionShape2D`, назначить ресурсы и сохранить сцену.
3. Реализовать механику движения персонажа через Input Map.
4. Получить структурированную диагностику отсутствующего ресурса или неверного свойства и исправить её.
5. Запустить scene test, получить screenshot и экспортировать desktop build.

Условия успеха:

- агент не редактировал generated cache;
- агент не использовал недоступный Godot API;
- проект открывается в Editor;
- сцены проходят round-trip;
- task state хранится в `ProjectTaskManager`, а не в `TASKS.md` или `completed-tasks/`;
- тесты проходят;
- сборка запускается;
- все изменения находятся в ожидаемых source-файлах;
- задача выполнена через документированный публичный интерфейс.

Целевой показатель первой версии: Editor co-development benchmark проходит полностью для основного supported агента, а headless benchmark имеет не менее 80% успешных эталонных задач минимум на двух разных AI-агентах без специальных скрытых инструкций.

## Рекомендуемый критический путь

1. Canonical document model, UID и diagnostics core.
2. `ProjectWorkspace` и stable formats.
3. Immutable `WorkspaceSnapshot` и job contract.
4. `WorkspaceTransactionEngine`.
5. `ProjectTaskManager` Core.
6. `Electron2D.Tooling` и `TaskService`.
7. Workspace IPC protocol, CLI и MCP.
8. Editor shell по UX-референсу Godot 4.
9. Script workspace и C# language services.
10. Project Tasks board.
11. Agent Workspace и project templates.
12. Human-AI conflict handling.
13. Headless runtime и debug bridge.
14. Visible Editor-attached runtime.
15. Packaged .NET debug adapter spike.
16. Managed C# debugger.
17. Capability manifest.
18. Script/debug Tooling и MCP parity.
19. AI acceptance benchmarks.
20. Full release candidate gate.
21. Final README и release packaging.

## Источники

- [Первый взгляд на интерфейс Godot](https://docs.godotengine.org/ru/4.x/getting_started/introduction/first_look_at_the_editor.html) - человекочитаемый UX/layout reference для структуры Editor.
- [Godot command line tutorial](https://docs.godotengine.org/en/latest/tutorials/editor/command_line_tutorial.html) - headless запуск, импорт и экспорт как устоявшийся паттерн игровых инструментов.
- [dotnet/roslyn](https://github.com/dotnet/roslyn) - baseline для C# compiler и code-analysis tooling в Editor/tooling layer.
- [Debug Adapter Protocol](https://microsoft.github.io/debug-adapter-protocol/) - протокольная граница между Editor и packaged .NET debugger adapter.
- [JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12) - целевая версия схем для JSON-представлений.
- [Model Context Protocol Tools](https://modelcontextprotocol.io/specification/draft/server/tools) - типизированные tools/resources/prompts для локальной AI-интеграции.
- [AGENTS.md convention](https://agents.md/) - предсказуемый файл инструкций для coding agents.
- [SARIF 2.1.0](https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html) - формат структурированного вывода результатов анализа.
- [global.json overview](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) - фиксация .NET SDK для воспроизводимой сборки.
