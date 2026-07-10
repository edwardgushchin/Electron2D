# Script workspace и встроенная C# IDE

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `0.1-preview`.
Задачи: `T-0158`, `T-0159`, `T-0160`, `T-0161`, `T-0163`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor 0.1-preview` должен позволять написать, исследовать, собрать и пошагово отладить игровую C#-логику без выхода из редактора и без установки внешней IDE. Внешний IDE не является обязательной частью workflow и не может использоваться как замена отсутствующим возможностям `Script` workspace.

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

Не входит в `0.1-preview`:

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

Поскольку `Electron2D.Editor` написан на Electron2D, `Script` workspace не должен строиться на приватных обходных controls, если нужные публичные UI capabilities входят в утверждённую публичную поверхность Godot 4.7 C# API. Перед реализацией `T-0158` нужен generated prerequisite manifest, который ссылается на API manifest и UI compatibility table, а не переписывает public type list вручную. Manifest должен подтвердить готовность text-editing surface, syntax highlighting, popup/menu behavior, tabbed documents, hierarchical/list navigation, split/scroll layout и basic labeled/action controls.

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

Если эти типы входят в обязательную публичную поверхность `0.1-preview`, они должны иметь `Supported`/`profile_approved` в manual profile либо получить утверждённую строку `Deferred`/`Unsupported` в `data/api/electron2d-public-api-profile.json`, а не реализовываться только для Editor приватным путём. Полная strict parity остаётся отдельным evidence-контрактом.

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

Проверяемый минимум `T-0158` для базового `Script` workspace:

- `Script` выбран через общий workspace switcher `2D`, `Script`, `Game`, `Tasks` и занимает центральную область редактора;
- prerequisite manifest перед запуском workspace фиксирует через generated UI gate, что text editing, syntax highlighting, popup/menu behavior, tabbed documents, hierarchical/list navigation, split/scroll layout, basic labeled/action controls, IME, clipboard, selection, caret navigation, Unicode, monospace font rendering, large documents, scrolling, gutter drawing и mouse hit testing по строке/колонке закрыты публичной поверхностью;
- `CodeDocument` хранит `DocumentId`, path, text, revision, persisted revision, dirty state, diagnostics и semantic version;
- создание, открытие, редактирование, rename и delete `.cs` files проходят через `Script` workspace model и возвращают structured result;
- document tabs показывают открытые `.cs` buffers, dirty marker, active tab и восстанавливаются после restart через persistence state;
- editor surface показывает line numbers, C# syntax highlighting, auto indentation mode, tabs/spaces settings, bracket/quote matching, code folding markers, current line highlight, caret position и selection range;
- search/replace in file, project search, go to line, clipboard, Undo/Redo, save file и `Save All` представлены как действия workspace model;
- ручной ввод использует `TextBufferEditSession`, публикует lightweight `CodeDocumentChanged` event, повышает `CodeDocument.Revision` и не создаёт `OperationJournal` entry на каждый символ;
- AI/refactoring/multi-file operation создаёт один `UndoGroupId` и compound edits в затронутых buffers;
- `script_save` или другой agent save проверяет базовую revision агента и возвращает structured conflict, если после неё появились ручные unsaved changes;
- внешнее изменение `.cs` проходит через synchronizer result: непересекающиеся правки merge, конфликтующие правки не затирают dirty editor buffer и дают conflict marker;
- build/run/test используют `WorkspaceSnapshot` с unsaved C# buffers, включая `InputSnapshotId`, `InputWorkspaceRevision`, `InputContentRevision`, `InputDocumentRevisions` и `InputBuildConfigurationHash`;
- UI smoke harness создаёт PNG screenshot и JSON analysis. Acceptance требует открыть screenshot, проверить вкладки, gutter, text editor surface, search/replace controls, caret/selection states, отсутствие text overflow, отсутствие GDScript/3D/AssetLib UI и соответствие `docs/editor/godot4-editor-reference.md`.

## Language services

Это project-aware language services, а не словарное дополнение текста.

Архитектурный baseline — Roslyn. Для `0.1-preview` Roslyn host реализуется как отдельный assembly `Electron2D.CSharpLanguageServices`, работающий внутри Editor process. Он не попадает в игровой runtime и не зависит от Editor UI. Отдельный language-service process не входит в `0.1-preview`; его можно добавить позже отдельной задачей, где будут описаны IPC, restart, crash recovery и синхронизация процесса. Граница реализации:

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

Проверяемый минимум `T-0159` для language services:

- создан отдельный project/assembly `Electron2D.CSharpLanguageServices`, который не входит в runtime assembly, не зависит от Editor UI и использует Roslyn semantic model;
- `Electron2D.Editor` подключает language-services assembly как внутренний сервис `Script` workspace; отдельный language-service process не создаётся;
- smoke model принимает in-memory `CodeDocument` с unsaved text и request identity: `ProjectId`, `DocumentId`, `DocumentRevision`, `SemanticVersion`, `ConfigurationHash`;
- IDE-операции completion, signature help, hover/Quick Info, live diagnostics, definition, references, rename, formatting и code action работают по live document state, а не через `WorkspaceSnapshot`;
- completion возвращает Electron2D API symbols из текущей referenced `Electron2D` assembly, локальные symbols текущего document и properties/methods resolved через Roslyn semantic model;
- signature help возвращает overload display, active parameter index, parameter names и documentation/source summary для вызова в unsaved buffer;
- hover/Quick Info возвращает symbol display string и XML documentation для documented symbol в текущем C# document;
- live diagnostics возвращает compiler diagnostics с file, 1-based line/column, severity, code и message без ручного build;
- go to definition и find references возвращают stable source spans с `DocumentId`, file path, line/column и `DocumentRevision`;
- rename symbol возвращает deterministic text edits с expected revision и не применяет их напрямую;
- document formatting возвращает text edit или formatted text, полученный через Roslyn syntax formatting;
- basic code action добавляет недостающий `using` для известного framework type и возвращает проверяемый edit;
- stale result проверяется явно: ответ с устаревшим `DocumentRevision` помечается как discarded/stale и не применяется к новому buffer state;
- cancellation/debounce/reload представлены в model как machine-readable flags: предыдущий request cancellation, diagnostics debounce interval и reload trigger для `.csproj`/package reference changes;
- semantic model failure возвращает structured diagnostic `E2D-SCRIPT-0003`, а не exception наружу;
- UI smoke harness создаёт PNG screenshot и JSON analysis с видимыми completion popup, hover/Quick Info panel и diagnostics panel. Acceptance требует открыть screenshot, проверить placement, читаемость, отсутствие text overflow, keyboard/current-selection state и отсутствие GDScript/3D/AssetLib UI.

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

Editor не зависит от API конкретного debugger напрямую. Managed debugger Electron2D `0.1` использует Debug Adapter Protocol (DAP) как обязательную протокольную границу:

```text
Electron2D.Editor
      ↓
ManagedDebugClient
      ↓ DAP
packaged .NET debug adapter
      ↓
Electron2D game process
```

Отклонение от DAP допускается только новым архитектурным решением, если `T-0163` докажет отсутствие подходящего распространяемого adapter.

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

Проверяемый минимум `T-0163` для выбора packaged .NET debug adapter:

- результат spike фиксируется в tracked manifest `data/debugging/dotnet-debug-adapter-selection.json`, чтобы `T-0160` мог использовать выбранный adapter без повторного выбора;
- manifest выбирает один adapter, содержит source repository, pinned release tag или commit, SPDX license id, redistribution policy, DAP transport, command-line arguments, update policy и links на primary sources;
- manifest содержит candidate review минимум для `netcoredbg`, Microsoft debugger adapters и молодых open-source alternatives, с machine-readable причиной выбора или отказа;
- selected adapter должен использовать DAP boundary `Electron2D.Editor -> Electron2D.ManagedDebugging -> DAP stdio -> adapter -> Electron2D game process`;
- Windows x64 validation выполняет реальный local smoke выбранного adapter: download/extract, `--version`, DAP `initialize`, `launch`, `setBreakpoints`, `configurationDone`, остановка на breakpoint, `threads`, `stackTrace`, `scopes`, `variables`, `next`, `continue`, `disconnect`;
- Windows x64 attach validation выполняет `attach` к уже запущенному .NET process, `configurationDone`, `threads`, `pause`, `stackTrace`, `continue` и `disconnect`;
- validation sample собирается в Debug configuration с Portable PDB, чтобы проверка не подменялась launch без debug symbols;
- Linux x64 и macOS arm64 фиксируются как release packaging targets. Если upstream release не содержит нужный binary artifact, manifest должен явно указать source-build strategy, required host, required release-gate smoke и причину, почему это не меняет выбранный adapter;
- DAP capability matrix включает launch, attach, breakpoints, stepping, threads, stackTrace, scopes, variables, expression evaluation, exception filters, terminate/restart-related support и ограничения;
- license validation подтверждает MIT-compatible redistribution для selected adapter и исключает adapters, чья лицензия не разрешает поставку вместе с Electron2D;
- documentation `docs/scripting/managed-debug-adapter-selection.md` описывает выбранный adapter, проверенные команды, capability matrix, platform/package plan, limitations и update procedure;
- integration test проверяет manifest и documentation, чтобы отсутствие platform strategy, license, capability или T-0160 handoff считалось regression.

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

Проверяемый минимум `T-0160` для managed debugger:

- создан отдельный project/assembly `Electron2D.ManagedDebugging`, который не входит в игровой runtime assembly, не зависит от `Electron2D.Tooling`/`Electron2D.Mcp` и использует adapter manifest `data/debugging/dotnet-debug-adapter-selection.json` из `T-0163`;
- Editor подключает debugger core как внутренний сервис `Script` workspace и не выбирает debugger adapter заново;
- `ManagedDebugClient` строит DAP boundary `Electron2D.Editor -> Electron2D.ManagedDebugging -> DAP stdio -> netcoredbg -> Electron2D game process`, читает выбранный adapter id/release/arguments из manifest и сохраняет machine-readable transcript команд `initialize`, `launch`, `attach`, `setBreakpoints`, `configurationDone`, `stopped:breakpoint`, `threads`, `stackTrace`, `scopes`, `variables`, `pause`, `continue`, `next`, `stepIn`, `stepOut`, `disconnect`;
- запуск проекта под debugger использует immutable `WorkspaceSnapshot`, Debug build с Portable PDB и mapping snapshot source file к canonical `CodeDocument`;
- attach ограничен game process, запущенным Editor; `RunSession` публикует `ProcessId`, чтобы debugger не требовал произвольный attach к чужому процессу;
- restart реализован как Editor-managed `disconnect` текущей сессии и новый `launch` на свежем `WorkspaceSnapshot`, потому что выбранный adapter не объявляет native DAP restart request;
- breakpoint store пишет локальные metadata в `.electron2d/user/breakpoints.e2debug`, сохраняет `BreakpointId`, `DocumentId`, `SourceAnchor`, `Enabled`, `Verified`, `ResolvedLine`, `ResolvedColumn`, `LastBoundSnapshotId` и `AdapterMessage`, переживает перезапуск Editor и не попадает в `WorkspaceSnapshot`/export;
- breakpoint следует за document rename через `DocumentId`, rebases `SourceAnchor` после text edits и получает `Verified = false` при неоднозначном переносе;
- debug state содержит current execution line, выбранный thread/frame, call stack, threads, locals, arguments, watch definitions, watch evaluation result, exception info и debug output;
- изменение C# document после старта debug session помечает session как `stale` и предлагает rebuild/restart без автоматического сохранения dirty buffer;
- remote Android/iOS/WebAssembly debugger явно помечен как excluded from `0.1-preview`;
- UI smoke harness создаёт PNG screenshot и JSON analysis. Acceptance требует открыть screenshot, проверить breakpoint gutter, current line highlight, debugger controls, call stack, threads, locals, arguments, watches, exception panel, absence of overflow, absence of 3D/GDScript/AssetLib UI и соответствие `docs/editor/godot4-editor-reference.md`.

## Breakpoint model

Breakpoints хранятся как локальные Editor metadata и не входят в игру, asset pack, runtime snapshot или production export.

Физическое хранение: `.electron2d/user/breakpoints.e2debug`. Каталог `.electron2d/user/` является локальными пользовательскими настройками: игнорируется Git, не входит в `WorkspaceSnapshot` и runtime export, переживает перезапуск Editor и не удаляется при очистке временной runtime-session.

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

`debug_update_breakpoint` управляет включением, отключением и изменением существующего breakpoint через `BreakpointId`. `debug_get_stack()` возвращает stacks всех threads. `debug_get_locals(frameId)` и `debug_get_arguments(frameId)` всегда читают данные выбранного stack frame явно, без скрытой зависимости от текущего UI selection. `debug_get_watches()` возвращает только определения watches без вычисления expressions; `debug_evaluate_watches(frameId)` вычисляет watches в явно указанном frame.

Для `0.1-preview` `debug_attach` не является произвольным `attach(pid)` для агента:

- agent session может attach только к game process активной Editor play/debug session;
- developer может подтвердить дополнительный attach интерактивно;
- arbitrary process attach отсутствует либо требует явного человеческого разрешения.

Editor показывает действия агента в `Script` workspace: изменённые строки, diagnostics, breakpoints, current stack frame и debug session state.

Вычисление expressions debugger может иметь side effects. Для AI baseline — просмотр locals, arguments, watch definitions и простых values. `debug_evaluate_watches(frameId)` должен использовать безопасный режим без side effects там, где adapter это поддерживает; более широкое evaluate требует явного подтверждения разработчика.

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

- `ProjectToolingHost.SupportedCommandNames` и MCP manifest публикуют полный список `script_*` и `debug_*` команд из раздела выше; published команда не может возвращать generic unsupported diagnostic.
- Tooling/MCP script commands применяют text edits, create/rename/delete file, search, symbols и code actions без keyboard emulation. Изменяющие команды принимают `expectedRevision`, создают workspace transaction и возвращают structured diagnostics при stale revision, unsafe path или конфликте.
- Read-only IDE commands возвращают `DocumentRevision`, `SemanticVersion`, `WorkspaceSnapshotUsedForIde=false` и результат live Roslyn Workspace. Для этих запросов не создаётся immutable `WorkspaceSnapshot`.
- AI получает diagnostics/completion/signature help/hover/definition/references/document symbols/code actions через Tooling/MCP по текущему открытому `CodeDocument`, включая unsaved text.
- `script_save` получает базовую revision агента и возвращает structured conflict, если после неё появились ручные unsaved changes; при отсутствии конфликта save выполняет persistence transaction.
- AI ставит и обновляет breakpoint, запускает debug session, выполняет restart, читает stacks всех threads, читает locals/arguments по явному `frameId`, получает watch definitions без вычисления expressions, явно вычисляет watches для выбранного frame, управляет watches и продолжает выполнение.
- `debug_start` и `debug_restart` создают immutable `WorkspaceSnapshot` и возвращают его identity вместе с debug state; build/run/test остаются на том же snapshot contract.
- `debug_attach` для агента ограничен active Editor game process, переданным доверенным Editor-слоем; произвольный attach по pid возвращает structured diagnostic либо требует интерактивного подтверждения разработчика.
- Agent Workspace показывает script/debug operations, links к active task, workspace transactions, jobs и artifacts.
- Подготовлен script/debug integration harness, который используется benchmark-задачей `T-0128`.
- Editor smoke harness создаёт `script-debug-tooling.state.json`, PNG screenshot и JSON analysis, где видны agent-applied text edit, diagnostics, breakpoint, selected stack frame, watches, current task links, transactions/jobs/artifacts, clickability, отсутствие text overflow и отсутствие запрещённых 3D/GDScript/AssetLib UI.

Общие проверки:

- `powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki` проходит перед editor work.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.

## Источники

- [dotnet/roslyn](https://github.com/dotnet/roslyn) - официальный open-source C# compiler и code-analysis API baseline.
- [Debug Adapter Protocol](https://microsoft.github.io/debug-adapter-protocol/) - протокольная граница между Editor и packaged debugger adapter.

## Фактическое состояние, ограничения и проверки

Статус: документация реализации для `T-0046`, `T-0158`, `T-0159`, `T-0160` и `T-0161`.
Дата: 2026-06-23.

## Назначение

`Electron2D.Editor` содержит внутреннюю модель script workflow для `0.1-preview`: создать C# script, открыть его во встроенной модели редактора кода, изменить и сохранить текст, прикрепить script к node, собрать проект и запустить проект после successful rebuild.

Workflow не добавляет runtime compilation и не загружает пользовательские assemblies динамически. Script остаётся обычным `.cs` файлом проекта игры и компилируется обычной .NET toolchain.

Центральное рабочее пространство `Script` описано отдельно: [Script workspace редактора](../editor/script-workspace.md). Встроенные C# подсказки, semantic diagnostics и code actions описаны отдельно: [C# language services в Script workspace](editor-language-services.md). Source-level отладка C# описана отдельно: [Managed C# debugger в редакторе](managed-debugger.md). Tooling/MCP-паритет для агентских script/debug операций описан отдельно: [Script/Debugger Tooling parity](script-debug-tooling-parity.md).

## Текущее поведение

Модель editor script workflow поддерживает:

- создание script file в `Scripts/`;
- генерацию namespace и class name из project name и имени script;
- открытие script file как code document;
- tracking несохранённых изменений;
- сохранение изменённого текста;
- attach script к node через `SceneFileDocument`;
- запуск project build;
- парсинг compiler diagnostics в file/line/column/code/message;
- запуск проекта после successful build.

Attach script меняет serialized node type на полное имя script class и сохраняет scene тем же serializer, который используют Scene Tree dock и Inspector.

Отдельная UI-модель `Script` workspace добавляет проверяемый editor surface: вкладки документов, line gutter, caret, selection, syntax token categories, search/replace, go-to-line, save/save-all, clipboard round-trip, grouped undo для AI/refactoring и обработку внешних изменений файла. Внутренняя `TextBufferEditSession` означает, что набор текста обновляет текущий `CodeDocument`, но не создаёт отдельные project operations в `OperationJournal` на каждый символ.

C# language services вынесены в отдельную сборку `Electron2D.CSharpLanguageServices`. Эта сборка работает внутри процесса Editor, использует Roslyn semantic model и обслуживает live `CodeDocument` с unsaved text. Completion, signature help, hover, live diagnostics, go to definition, references, rename, formatting и basic code action используют `DocumentRevision`, `SemanticVersion` и `ConfigurationHash`; `WorkspaceSnapshot` остаётся для build/test/run/debug и пакетного анализа.

Managed C# debugger вынесен в отдельную сборку `Electron2D.ManagedDebugging`. Эта сборка читает выбранный adapter manifest из `data/debugging/dotnet-debug-adapter-selection.json`, хранит local breakpoints в `.electron2d/user/breakpoints.e2debug`, описывает DAP boundary до `netcoredbg`, debug session state, stack/locals/arguments/watches и stale marker после изменения code document.

Script/debug Tooling parity добавляет путь для локального агента: text edits, diagnostics, completion, signature help, hover, navigation, document symbols, references, rename, formatting, code actions, breakpoints, debug session control, stacks, locals, arguments и watches доступны через `ProjectToolingHost.Script`, `ProjectToolingHost.Debug` и соответствующие MCP tools. Editor smoke показывает эти операции в `Script` workspace и правой панели `Agent Workspace`; агент не использует UI automation для ввода текста.

## Smoke workflow

Локальная проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-workflow-smoke .temp\editor-script-workflow
```

Ожидаемый результат включает:

```text
Electron2D.Editor script workflow smoke passed
CreatedScriptExists=True
CompilerErrorCount>0
FirstCompilerErrorCode=CS1002
FixedBuildSucceeded=True
RunExitCode=0
RerunAfterRebuild=True
```

Smoke-команда создаёт временный проект, записывает невалидный script, проверяет compiler diagnostic, сохраняет исправленный script, прикрепляет его к node, собирает и запускает проект.

Дополнительная UI smoke-команда для `Script` workspace:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-workspace-smoke .temp\script-workspace
```

Она сохраняет state JSON, PNG screenshot и JSON analysis artifact для проверки встроенного C# editor UI, отсутствия text overflow, наличия interactive controls и соответствия workspace switcher `2D | Script | Game | Tasks`.

Дополнительная smoke-команда для C# language services:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-language-services-smoke .temp\script-language-services
```

Она сохраняет state JSON, PNG screenshot и JSON analysis artifact для проверки completion popup, hover/Quick Info panel, signature help, live diagnostics panel, go-to-definition target, references count, rename preview, formatting/code-action result, stale response marker и отсутствия запрещённых UI elements.

Дополнительная smoke-команда для managed C# debugger:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --managed-debugger-smoke .temp\managed-debugger
```

Она сохраняет state JSON, PNG screenshot и JSON analysis artifact для проверки DAP boundary, breakpoint persistence, rename/rebase, Debug build/PDB marker, attach process id, restart strategy, current line, threads, call stack, locals, arguments, watches, exception panel, stale marker и отсутствия запрещённых UI elements.

Дополнительная smoke-команда для Script/Debugger Tooling parity:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-debug-tooling-smoke .temp\script-debug-tooling
```

Она сохраняет state JSON, PNG screenshot и JSON analysis artifact для проверки агентской text edit операции, live diagnostic `CS0103`, completion popup `Sprite2D`, breakpoint marker, stacks всех threads, locals/arguments выбранного frame, watch definitions/evaluation и правой панели `Agent Workspace` с task/transaction/job/artifact links.

## Ограничения

- Это ещё не полный desktop IDE event loop, то есть не постоянный цикл обработки окна, pointer/keyboard input и repaint: C# language services покрыты smoke-моделью и visual harness, а постоянная привязка popup/hover/diagnostics к real-time input будет подключаться следующими editor tasks.
- Managed debugger реализован как model-first core и smoke UI surface. Постоянная desktop-привязка к real-time pointer/keyboard input и packaged adapter binary resolution подключается поверх этого контракта в следующих editor/release tasks.
- Hot Reload не является обязательным workflow. После изменения script проект пересобирается.
- Runtime не компилирует scripts из текста и не загружает их динамически.
- Текущий `Script` workspace является model-first UI surface: сначала проверяется модель состояния UI и screenshot artifact, а live desktop text input и полноценная привязка к окну подключаются следующими задачами.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptWorkflowTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptWorkspaceTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptLanguageServicesTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorManagedDebuggerTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ScriptDebugToolingParityTests" -m:1
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
