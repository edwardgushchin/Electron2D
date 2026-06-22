# AI-friendly workflow Electron2D 0.1

Статус: целевая архитектурная спецификация.
Задача: `T-0114`.
Обновлено: 2026-06-22.

## Позиционирование

Electron2D — C#-first кроссплатформенный 2D-движок с согласованным API, спроектированный для совместной разработки человеком и AI-агентами.

Ключевое обещание `0.1.0`: `Electron2D.Editor` является основным живым рабочим пространством проекта. Разработчик может пользоваться редактором полностью вручную, но AI-агент видит актуальное состояние открытого проекта, выполняет семантически значимые операции редактора, запускает тестовый прогон, наблюдает результат и безопасно объединяет свои изменения с ручными правками.

Семантически значимая операция — это изменение проекта, сцены, ресурса, кода, настроек, импорта, диагностики, тестового запуска или экспорта. Перетаскивание dock-ов, смена темы, размер окна и другие действия, не меняющие проектный смысл, не входят в обязательный AI-паритет.

Это архитектурное требование, а не требование встроенного чата. AI-friendly означает, что редактор, локальный MCP-сервер, CLI, CI и будущие IDE-интеграции работают с одной моделью проекта через стабильные команды, текстовые файлы, структурированную диагностику и машиночитаемую документацию. Встроенная LLM, генерация игры по одному prompt, облачный аккаунт и привязка к одному AI-провайдеру в `0.1.0` не нужны.

Короткая продуктовая формулировка:

> Electron2D — 2D-движок на C# для совместной разработки человеком и AI.

Английский слоган:

> Electron2D — a C# 2D engine built for humans and AI agents.

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
- `OperationJournal` — журнал происхождения операций: человек, AI-сессия, CLI, внешний файл или тест;
- `UndoRedo` — обычная история отмены и повтора, в которую AI-транзакция попадает одной группой;
- `ImportState` — состояние импорта ресурсов: ожидает, импортируется, готово или ошибка;
- `BuildState` — состояние сборки и перезагрузки кода;
- `DiagnosticsStore` — актуальные ошибки, предупреждения и suggested fixes, то есть безопасные предлагаемые исправления.

Открытый Editor не должен держать приватную модель сцены, которую невозможно изменить через Tooling. Операция «добавить `Sprite2D` в сцену» должна быть одной и той же независимо от того, вызвал её пользователь через Scene Tree dock, AI через MCP tool, CLI-команда, тест или будущий IDE-плагин.

## Единое ядро инструментов

`Electron2D.Tooling` — общий слой семантических операций над `ProjectWorkspace`. Его используют редактор, CLI, MCP-сервер, CI и будущие IDE-интеграции.

Минимальный набор сервисов:

```text
Electron2D.Tooling
├── ProjectService
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

Изменяющие команды возвращают структурированный результат с `success`, `operation`, `workspaceRevision`, `changedFiles`, `changedObjects`, `createdObjects`, `diagnostics` и `undoGroupId`.

Все изменяющие команды должны принимать `expectedRevision`, если они работают с уже открытым документом или объектом. Это защита от потери ручных изменений: команда явно говорит, к какой версии состояния она применялась.

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
- остановка, пауза или cancel текущей агентской операции, если command поддерживает отмену;
- grouped undo последней AI-транзакции обычным редакторским Undo.

Панель не должна быть единственным способом AI-интеграции. Если агент подключается снаружи через MCP, Editor обязан показывать его операции в той же панели и в обычных UI-обновлениях.

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
atomic persistence
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

Критерии задержки:

- semantic Tooling operation обновляет UI в том же editor dispatch cycle;
- external text-file change обнаруживается и отображается не позднее 250 мс после стабилизации записи;
- новый импортируемый asset сразу появляется в FileSystem dock со статусом `Importing`, `Compiling` или `Error`;
- preview и зависимые объекты обновляются после завершения импорта или сборки;
- пользователь не нажимает `Refresh` и не перезапускает Editor;
- Editor не блокируется полным переимпортом проекта.

Полноценный C# Hot Reload не обязателен для `0.1`. Допустим быстрый rebuild и автоматический перезапуск preview/play session, если файл кода изменился.

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
  "editorCommand": "SceneSetProperty",
  "toolingCommand": "scene.setProperty",
  "mcpTool": "scene_set_property",
  "cliCommand": "e2d scene set",
  "status": "supported"
}
```

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
- запуск текущей сцены и проекта;
- stop/restart/pause;
- step frame и step physics;
- input injection;
- screenshot;
- runtime inspection;
- запуск тестов;
- просмотр diagnostics.

CI должен падать, если Editor получил новую проектную операцию, но для неё нет Tooling-команды, MCP tool/resource или явного `not_applicable` с объяснением.

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

## Машиночитаемый API manifest

Electron2D должен поставлять версионированный API manifest в JSON. Manifest нужен, чтобы AI-агенты, CLI, Inspector, Wiki, source generators и будущий language server не угадывали отличия Electron2D от Godot.

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
- статус `supported`, `partial`, `experimental`;
- отличия от Godot;
- примеры использования;
- версию появления или изменения элемента.

## Карта совместимости с Godot

Electron2D API требует формальной карты отличий, а не только общей фразы в README. Для каждого публичного типа документация должна содержать блок:

```text
Compatibility with Godot
Supported:
Partial:
Not supported:
Behavioral differences:
```

Команда `e2d api compare-godot <type> --format json` должна возвращать совместимые члены, неподдерживаемые члены и поведенческие отличия. Без этого AI-агенты будут создавать убедительно выглядящий, но некомпилируемый Electron2D-код.

## MCP и Editor-hosted Agent Gateway

MCP означает Model Context Protocol: локальный протокол, через который AI-клиент получает типизированные tools, resources и prompts. Electron2D должен предоставлять локальный, необлачный MCP-сервер, не привязанный к конкретной модели или поставщику:

```bash
e2d mcp serve
```

MCP остаётся тонким адаптером над `Electron2D.Tooling`, а не второй независимой реализацией.

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
- предупреждение не предполагать полную совместимость с Godot;
- команду `e2d api compare-godot <type>` для спорных API;
- правило подключаться к активной Editor-сессии, если она открыта.
- правила ведения рабочих Markdown-записей проекта: `TASKS.md`, `dev-diary/`, `completed-tasks/`, `docs/specifications/` и `docs/documentation/`;
- запрет закрывать задачи без явной приёмки пользователя и правило переносить принятые задачи в monthly archive `completed-tasks/YYYY/MM Месяц.md`.

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

Editor должен показывать ход тестов, diagnostics и visual diff в рабочей области, если тест запущен из Editor или подключённой AI-сессии.

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

Editor должен получать live diagnostics stream, то есть поток актуальных ошибок и предупреждений без ручного перезапуска проверки. Агент должен видеть те же diagnostics через MCP.

## Безопасное изменение проекта

Изменяющие операции AI должны быть транзакционными:

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
write temporary files
    ↓
atomic replace
    ↓
return changed files, changed objects and diagnostics
```

Обязательны `--dry-run`, atomic writes, automatic backup для миграций, защита от записи за пределами project root, запрет изменения import cache через scene API, список затронутых файлов, validation before commit, стабильные UID, отсутствие молчаливого удаления неизвестных свойств, audit log операций MCP и undo group для каждой AI-транзакции.

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

Документация каждого публичного API должна содержать назначение, сигнатуру, lifecycle restrictions, thread affinity, ownership/disposal, пример, ошибки, платформенные ограничения, renderer restrictions и отличия от Godot.

## Что AI-friendly не означает

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
- общий Tooling слой семантических операций;
- Editor session discovery и локальный IPC;
- MCP подключение к активной Editor-сессии;
- Agent Workspace panel;
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

Можно отложить: встроенный редактор C#, visual shader editor, сложный `AnimationTree`, skeletal animation, расширенный particle editor, полноценный profiler UI, plugin marketplace, сложная dock-система, встроенный AI-chat и автоматическая публикация в магазины.

## Критерии приёмки AI-friendly

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
9. Агент ставит игру на паузу, делает один frame step, отправляет input и получает screenshot.
10. Агент обнаруживает ошибку через structured diagnostics и исправляет её.
11. Разработчик одним Undo отменяет последнюю агентскую транзакцию.
12. После отключения AI проект остаётся полностью редактируемым вручную.

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
- тесты проходят;
- сборка запускается;
- все изменения находятся в ожидаемых source-файлах;
- задача выполнена через документированный публичный интерфейс.

Целевой показатель первой версии: Editor co-development benchmark проходит полностью для основного supported агента, а headless benchmark имеет не менее 80% успешных эталонных задач минимум на двух разных AI-агентах без специальных скрытых инструкций.

## Источники

- [Godot command line tutorial](https://docs.godotengine.org/en/latest/tutorials/editor/command_line_tutorial.html) - headless запуск, импорт и экспорт как устоявшийся паттерн игровых инструментов.
- [JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12) - целевая версия схем для JSON-представлений.
- [Model Context Protocol Tools](https://modelcontextprotocol.io/specification/draft/server/tools) - типизированные tools/resources/prompts для локальной AI-интеграции.
- [AGENTS.md convention](https://agents.md/) - предсказуемый файл инструкций для coding agents.
- [SARIF 2.1.0](https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html) - формат структурированного вывода результатов анализа.
- [global.json overview](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) - фиксация .NET SDK для воспроизводимой сборки.
