# Script/Debugger Tooling parity

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт, состояние и проверки

Статус: реализовано для `T-0161`.
Обновлено: 2026-06-23.

## Назначение

`Script/Debugger Tooling parity` означает, что локальный агент меняет C# scripts и управляет managed debugger через смысловые команды внутреннего слоя Tooling и MCP, а не через эмуляцию клавиатуры и мыши. Editor при этом показывает обычное состояние `Script` workspace и правой панели `Agent Workspace`: применённые text edits, diagnostics, completion, breakpoint, stack frames, locals, arguments, watches, current task и ссылки на transaction/job/artifact.

Эта возможность не добавляет внешний AI-провайдер и не требует сетевого сервера. Текущий preview-контракт проверяется in-process integration tests и deterministic screenshot harness.

## Tooling commands

`ProjectToolingHost` теперь публикует `Script` и `Debug` services.

Script service поддерживает:

- `script_create`, `script_open`, `script_read`, `script_rename`, `script_delete`, `script_search_text`, `script_apply_text_edits`, `script_save`, `script_format`;
- `script_get_diagnostics`, `script_get_completions`, `script_get_signature_help`, `script_get_hover`, `script_get_definition`, `script_get_document_symbols`, `script_find_references`, `script_rename_symbol`, `script_get_code_actions`, `script_apply_code_action`.

Read-only IDE-команды используют live Roslyn workspace по текущему `DocumentRevision` и `SemanticVersion`; `WorkspaceSnapshot` для них не создаётся. Изменяющие команды требуют `expectedRevision`, проходят через workspace transaction и возвращают structured diagnostics. `script_save` защищает агентскую сессию от перезаписи ручных несохранённых изменений после базовой revision агента.

Debug service поддерживает:

- `debug_set_breakpoint`, `debug_update_breakpoint`, `debug_remove_breakpoint`;
- `debug_start`, `debug_attach`, `debug_restart`, `debug_pause`, `debug_continue`, `debug_step_into`, `debug_step_over`, `debug_step_out`, `debug_stop`;
- `debug_get_threads`, `debug_get_stack`, `debug_get_locals`, `debug_get_arguments`;
- `debug_get_watches`, `debug_evaluate_watches`, `debug_add_watch`, `debug_update_watch`, `debug_remove_watch`.

`debug_start` и `debug_restart` используют immutable `WorkspaceSnapshot`, то есть неизменяемый снимок входных документов, content revision и build configuration hash для воспроизводимых run/debug jobs. `debug_get_stack()` возвращает stacks всех threads. `debug_get_locals(frameId)` и `debug_get_arguments(frameId)` читают данные явно выбранного frame. `debug_get_watches()` возвращает только определения watch expressions, а `debug_evaluate_watches(frameId)` вычисляет значения в безопасном smoke-режиме без side effects. `debug_attach` для agent context ограничен active Editor game process, если нет явного интерактивного подтверждения разработчика.

## MCP parity

`McpServerSession` публикует те же `script_*` и `debug_*` tool names. MCP route возвращает структурированные JSON payloads для:

- script document state: path, text, document id, revisions, semantic version, diagnostics;
- IDE state: completion items, signature help, hover, diagnostic code, definition, references, document symbols, code actions;
- debug state: breakpoint, session snapshot identity, threads, stack frames, stacks by thread, variables и watches.

Unsupported diagnostic `E2D-MCP-0001` больше не является результатом для этих команд: если tool опубликован, он маршрутизируется через `Tooling.Script` или `Tooling.Debug`.

## Editor visual harness

Команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-debug-tooling-smoke .temp\script-debug-tooling
```

Создаёт:

- `.temp/script-debug-tooling/script-debug-tooling.state.json`;
- `.temp/script-debug-tooling/visual/script-debug-tooling.png`;
- `.temp/script-debug-tooling/visual/script-debug-tooling.analysis.json`.

PNG показывает выбранный `Script` workspace, применённую агентом строку `speed = 280`, diagnostic `CS0103`, completion popup с `Sprite2D`, breakpoint marker, bottom `Debugger` panel с threads/call stack/locals/arguments/watches и правый `Agent Workspace` с `T-0161`, transaction, job и screenshot artifact.

JSON analysis проверяет, что:

- agent edit, diagnostics, breakpoint, stack и watch evaluation видимы;
- current task и ссылки на transaction/job/artifact видимы в `Agent Workspace`;
- clickable controls не меньше 24;
- text overflow отсутствует;
- forbidden UI labels `3D`, `AssetLib`, GDScript, `.gd`, `Node3D` отсутствуют;
- screenshot был создан и просмотрен.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ScriptDebugToolingParityTests" -m:1
```

Visual smoke:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-debug-tooling-smoke .temp\script-debug-tooling
```

Документационный verifier после изменения справки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LocalDocumentation.ps1
```
