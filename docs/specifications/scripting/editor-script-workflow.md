# Script workspace и встроенная C# IDE

Статус: целевая спецификация для `0.1.0 Preview`.
Задачи: `T-0158`, `T-0159`, `T-0160`, `T-0161`, `T-0163`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor 0.1.0` должен позволять написать, исследовать, собрать и пошагово отладить игровую C#-логику без выхода из редактора и без установки внешней IDE. Внешний IDE не является обязательной частью workflow и не может использоваться как замена отсутствующим возможностям `Script` workspace.

`Script` является центральным workspace редактора, а не вспомогательным dock. Он работает с тем же `ProjectWorkspace`, что Scene Tree, Inspector, FileSystem, Agent Workspace, Tooling и MCP.

## Scope

Входит:

- создание, открытие, редактирование, переименование и удаление `.cs` files;
- встроенный C# text editor;
- project-aware language services;
- live compiler diagnostics;
- attach script к node;
- build/rebuild workflow;
- managed C# debugger для local desktop runs;
- Tooling/MCP parity для script и debugger operations.

Не входит в `0.1.0`:

- GDScript;
- visual scripting;
- Hot Reload;
- Edit and Continue;
- complex solution-wide refactorings;
- Roslyn analyzer marketplace;
- встроенное NuGet management UI;
- decompiler;
- memory profiler;
- remote debugger для Android/iOS;
- другие языки программирования.

## UI prerequisites

Поскольку `Electron2D.Editor` написан на Electron2D, `Script` workspace не должен строиться на приватных обходных controls, если нужные публичные UI types входят в утверждённый 2D-профиль Godot C# API. Перед реализацией `T-0158` нужен точный prerequisite manifest с полностью рабочими:

- `TextEdit`;
- `CodeEdit`;
- `SyntaxHighlighter`;
- `CodeHighlighter`;
- `PopupMenu`;
- `TabContainer`;
- `Tree`;
- `ItemList`;
- `SplitContainer`;
- `ScrollBar`;
- `LineEdit`;
- `Label`;
- `Button`.

Также должны быть проверены:

- IME;
- clipboard;
- selection;
- caret navigation;
- Unicode;
- monospace font rendering;
- большие текстовые документы;
- horizontal/vertical scrolling;
- gutter drawing;
- mouse hit testing по строке и колонке.

Если эти типы входят в обязательный 2D-профиль `0.1.0`, они должны пройти обычный parity gate как `Supported / Parity verified`, а не реализовываться только для Editor приватным путём.

## CodeDocument и ProjectWorkspace

Открытый C# buffer является first-class документом `ProjectWorkspace`:

```text
CodeDocument
├── DocumentId
├── Path
├── Text
├── Revision
├── PersistedRevision
├── IsDirty
├── Diagnostics
└── SemanticVersion
```

Следствия:

- ручной ввод в активном editor buffer идёт через `TextBufferEditSession`, а не через полноценную workspace transaction на каждое нажатие клавиши;
- AI-правки, refactoring, multi-file code actions, create/rename/delete files и attach script проходят через `WorkspaceTransactionEngine`;
- save выполняется отдельной persistence transaction: сохраняет уже принятый in-memory buffer через atomic write и повышает `PersistedRevision`;
- семантические операции принимают `expectedRevision`;
- dirty state виден Editor, Tooling, MCP, build/test/run и Agent Workspace;
- внешнее изменение `.cs` импортируется через `ExternalChangeSynchronizer`;
- непересекающиеся изменения могут объединяться three-way text merge;
- конфликтующие изменения показываются в conflict panel;
- build/run/test/debug используют `WorkspaceSnapshot`, включающий unsaved C# buffers;
- никакая операция AI не должна молча сохранять ручные несохранённые изменения разработчика.

`TextBufferEditSession` публикует lightweight `CodeDocumentChanged` events для dirty state, Roslyn synchronization и MCP-наблюдения. Эти события grouped/debounced и не являются полноценными `OperationJournal` entries. `script_save`, вызванный агентом, должен проверять базовую revision агента: если после неё появились ручные unsaved changes, save возвращает structured conflict либо требует интерактивное подтверждение разработчика.

## Undo model

Для code buffers есть два уровня Undo.

`TextBufferUndo` используется для обычного ручного ввода внутри активного editor buffer:

- символы и удаления;
- вставка;
- локальные действия форматирования;
- обычный `Ctrl+Z` внутри code editor;
- coalescing последовательного ввода в разумные edit groups.

Такие действия меняют `CodeDocument.Revision`, но не создают отдельную `OperationJournal` entry на каждый символ и не публикуют лишний поток глобальных workspace-событий для Agent Workspace, Inspector или других panels.

`WorkspaceUndo` используется для семантических и межфайловых операций:

- AI `script_apply_text_edits`;
- `rename symbol`;
- code action, затрагивающий несколько файлов;
- attach script;
- создание, переименование и удаление файла;
- grouped rollback всей AI-транзакции.

Нормативная модель:

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

AI-изменение должно одновременно отображаться как одна compound operation в каждом затронутом buffer и иметь один глобальный `UndoGroupId` для отмены всех файлов.

## Встроенный C# editor

Обязательный минимум:

- вкладки открытых документов;
- line numbers;
- C# syntax highlighting;
- автоматические отступы;
- настройка tabs/spaces;
- matching скобок и кавычек;
- code folding;
- выделение текущей строки;
- поиск и замена в файле;
- поиск по проекту;
- переход к строке;
- clipboard;
- Undo/Redo;
- сохранение одного файла;
- `Save All`;
- отображение dirty state;
- восстановление открытых вкладок после перезапуска Editor.

## Language services

Это project-aware language services, а не словарное дополнение текста.

Архитектурный baseline — Roslyn. Roslyn используется как официальный C# compiler/code-analysis host, но не попадает в игровой runtime и не зависит от Editor UI. Граница реализации:

```text
Electron2D.CSharpLanguageServices
├── ProjectModelHost
├── DocumentSynchronization
├── CompletionService
├── SignatureHelpService
├── HoverService
├── DiagnosticsService
├── NavigationService
├── RenameService
└── FormattingService
```

`Electron2D.CSharpLanguageServices` синхронизирует live Roslyn Workspace с `CodeDocument`. Completion, hover, live diagnostics, navigation, references и rename используют актуальные unsaved buffers через `DocumentRevision` и `SemanticVersion`. Полный immutable `WorkspaceSnapshot` не создаётся для каждого символа, completion request или hover.

`WorkspaceSnapshot` используется для воспроизводимых build/test/run/debug workflows и пакетного анализа, где нужен стабильный input state.

Обязательный минимум:

- autocomplete типов, методов, свойств, events и локальных переменных;
- completion для API Electron2D;
- signature help с активным параметром;
- hover/Quick Info;
- XML documentation;
- live compiler diagnostics;
- подчёркивание ошибок и предупреждений;
- go to definition;
- find references;
- rename symbol;
- document formatting;
- basic code actions;
- добавление недостающего `using`;
- навигация по классам, методам и свойствам текущего файла;
- diagnostics panel с переходом к строке ошибки.

Semantic model учитывает:

- текущий `.csproj`;
- project references;
- Electron2D assemblies;
- NuGet dependencies;
- conditional compilation symbols;
- unsaved C# buffers;
- source-generated API;
- фактическую build configuration.

Каждый request и result language-service слоя содержит:

```text
ProjectId
DocumentId
DocumentRevision
SemanticVersion
ConfigurationHash
```

Если buffer изменился до получения ответа, старый completion, hover, signature help или diagnostic отбрасывается. Обязательны cancellation предыдущих запросов, debounce live diagnostics, reload после изменения `.csproj`, `Directory.Build.props`, `Directory.Build.targets`, `global.json` и package references, а также structured diagnostic, если semantic model не удалось построить.

## Attach к узлу

Attach script к node работает с текущей scene serialization:

- editor получает `SceneFileDocument`;
- находит node по stable id;
- связывает serialized node с полным именем script class;
- сохраняет scene через workspace transaction;
- после save/load round-trip scene остаётся стабильной.

`scene_attach_script` не добавляет отдельный Script-компонент. Операция связывает serialized node с пользовательским C#-типом, наследующим подходящий Electron2D node type, и после сборки создаётся единый экземпляр этого типа.

Если node не найден, script class не подходит по наследованию или scene file повреждён, операция завершается structured diagnostic.

## Build и compiler diagnostics

Editor build workflow запускает project build через .NET toolchain на `WorkspaceSnapshot`.

Результат содержит:

- operation id;
- input snapshot id;
- input document revisions;
- input build configuration hash;
- exit code;
- stdout/stderr;
- diagnostics с severity, code, file, line, column и message;
- success/failure;
- stale flag.

Compiler errors должны быть actionable: Editor знает файл, строку, колонку, code и message, может открыть файл в `Script` workspace и перейти к нужной строке.

## Managed C# debugger

Managed C# debugger не заменяет runtime debug bridge. Это отдельная подсистема.

Editor не зависит от API конкретного debugger напрямую. Между Editor и debugger adapter фиксируется протокольная граница, предпочтительно Debug Adapter Protocol:

```text
Electron2D.Editor
      ↓
ManagedDebugClient
      ↓ DAP
packaged .NET debug adapter
      ↓
Electron2D game process
```

Конкретный packaged .NET debug adapter выбирается отдельным техническим spike до реализации полноценного debugger. Spike обязан проверить:

- Windows x64;
- Linux x64;
- macOS arm64;
- Portable PDB;
- launch и attach;
- breakpoints;
- stepping;
- locals/watches;
- exceptions;
- process restart;
- DAP capability matrix;
- способ обновления adapter binaries;
- распространение по совместимой лицензии.

Runtime debug bridge:

- scene tree;
- node properties;
- physics state;
- input;
- screenshots;
- frame step;
- performance metrics.

Managed C# debugger:

- source breakpoints;
- call stack;
- threads;
- locals;
- arguments;
- watches;
- exceptions;
- step into/over/out.

Debugger core живёт отдельно от UI и Tooling adapters:

```text
Electron2D.ManagedDebugging
├── ManagedDebugClient
├── DebugSession
├── BreakpointStore
├── ManagedBreakpoint
├── SourceAnchorRebaser
└── DapAdapterHost
```

`Electron2D.Editor` использует эти сервисы для UI. `Electron2D.Tooling` и `Electron2D.Mcp` подключают adapters позже в `T-0161`; language/debug core не должны зависеть от `Electron2D.Tooling`.

Обязательный минимум debugger для local desktop runs на Windows, Linux и macOS:

- установка breakpoint кликом в gutter;
- включение, отключение и удаление breakpoint;
- сохранение breakpoints между Editor sessions;
- запуск текущей сцены или проекта под debugger;
- attach к игровому процессу, запущенному Editor;
- pause;
- continue;
- stop;
- restart;
- step into;
- step over;
- step out;
- подсветка текущей исполняемой строки;
- call stack;
- выбор stack frame;
- список threads;
- locals;
- arguments;
- watches;
- просмотр exception и stack trace;
- остановка на необработанном exception;
- переход из stack trace к исходному коду;
- Debug Output.

## Breakpoint model

Breakpoints хранятся как локальные Editor metadata и не входят в игру, asset pack, runtime snapshot или production export.

```text
ManagedBreakpoint
├── BreakpointId
├── DocumentId
├── SourceAnchor
├── Enabled
├── Verified
├── ResolvedLine
├── ResolvedColumn
├── LastBoundSnapshotId
└── AdapterMessage
```

Требования:

- breakpoint следует за документом при rename через `DocumentId`;
- text edits выполняют rebase `SourceAnchor`;
- при неоднозначном rebase breakpoint получает `Verified = false`, а не переезжает молча;
- line/column, возвращённые DAP adapter, обновляют `ResolvedLine` и `ResolvedColumn`;
- `AdapterMessage` хранит объяснение debugger adapter, если breakpoint не bound;
- агентские изменения breakpoints отображаются в Agent Workspace.

Debugger запускается на конкретном immutable `WorkspaceSnapshot`:

```text
WorkspaceSnapshot
    ↓
build с Portable PDB
    ↓
game process
    ↓
DebugSession
    ↓
mapping snapshot source → open CodeDocument
```

Debug session хранит:

- `SnapshotId`;
- source file revisions;
- build configuration;
- symbols path;
- mapping временных snapshot-файлов к canonical project documents.

Если пользователь или AI меняет код после запуска debug session:

- session помечается `stale`;
- текущая строка и breakpoints не должны молча отображаться для другой версии исходника;
- Editor предлагает rebuild/restart;
- несохранённые изменения не сохраняются автоматически.

## AI и Tooling/MCP parity

AI не редактирует код через keyboard emulation или pixel automation. Для него нужны семантические Tooling/MCP-команды:

```text
script_create
script_open
script_read
script_apply_text_edits
script_save
script_format
script_get_diagnostics
script_get_completions
script_get_signature_help
script_get_hover
script_get_definition
script_find_references
script_rename_symbol

debug_set_breakpoint
debug_remove_breakpoint
debug_start
debug_attach
debug_pause
debug_continue
debug_step_into
debug_step_over
debug_step_out
debug_get_threads
debug_get_stack
debug_get_locals
debug_get_watches
debug_stop
```

Для `0.1.0` `debug_attach` не является произвольным `attach(pid)` для агента:

- agent session может attach только к game process активной Editor play/debug session;
- developer может подтвердить дополнительный attach интерактивно;
- arbitrary process attach отсутствует либо требует явного человеческого разрешения.

Editor показывает действия агента в `Script` workspace: изменённые строки, diagnostics, breakpoints, current stack frame и debug session state.

Вычисление expressions debugger может иметь side effects. Для AI baseline — просмотр locals и простых values. Любое вычисление выражения, которое может изменить состояние процесса, требует явного подтверждения разработчика.

## Smoke и acceptance

Acceptance для `T-0158`:

- Editor создаёт, открывает, редактирует, сохраняет и переоткрывает `.cs` file через `Script` workspace model.
- Editor показывает dirty state, line numbers, syntax highlighting и search/replace.
- Save All сохраняет несколько dirty buffers.
- Открытые tabs восстанавливаются после restart.

Acceptance для `T-0159`:

- Completion учитывает Electron2D API, project references и unsaved buffer через live Roslyn Workspace.
- Signature help показывает активный параметр.
- Hover показывает XML documentation.
- Diagnostics появляются live и ведут к строке ошибки.
- Go to definition, find references и rename symbol работают в пределах тестового проекта.
- IDE-запросы используют `DocumentRevision` и `SemanticVersion`; `WorkspaceSnapshot` используется только для build/test/run/debug и пакетного анализа.

Acceptance для `T-0160`:

- Breakpoint устанавливается в gutter и сохраняется между sessions.
- Breakpoint хранит `BreakpointId`, `DocumentId`, source anchor, verified state, resolved location и last bound snapshot.
- Breakpoint rebase после text edits корректно переносит anchor либо помечает breakpoint unverified.
- Current scene запускается под debugger.
- Debugger останавливается на breakpoint.
- Editor показывает current line, call stack, threads, locals, arguments и watches.
- Step into/over/out, continue, pause, stop и restart работают.
- Unhandled exception открывает stack trace и source location.
- Изменение кода после запуска помечает session как `stale`.

Acceptance для `T-0161`:

- Tooling/MCP script commands применяют text edits без keyboard emulation.
- AI получает diagnostics/completion/definition/references через Tooling/MCP.
- AI ставит breakpoint, запускает debug session, читает stack/locals/watches и продолжает выполнение.
- `debug_attach` для агента ограничен active Editor game process; произвольный attach требует человеческого подтверждения.
- Agent Workspace показывает script/debug operations и links к active task.

Общие проверки:

- `powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki` проходит перед editor work.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.

## Источники

- [dotnet/roslyn](https://github.com/dotnet/roslyn) - официальный open-source C# compiler и code-analysis API baseline.
- [Debug Adapter Protocol](https://microsoft.github.io/debug-adapter-protocol/) - протокольная граница между Editor и packaged debugger adapter.
