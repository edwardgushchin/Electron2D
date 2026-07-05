# Editor-attached runtime control

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0144`.
Обновлено: 2026-06-23.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md); [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Run/output workflow редактора](../editor/run-output-workflow.md); [Runtime debug bridge и scene inspection](runtime-debug-bridge.md); [WorkspaceSnapshot, job input identity и dirty export policy](../project-system/workspace-snapshot.md).

## Назначение

`Editor-attached runtime control` связывает видимый запуск игры из Editor с общим runtime-control контрактом для человека, Agent Workspace и MCP. Видимый запуск остаётся отдельным game process: падение игры не должно завершать Editor. Управление состоянием запуска, снимки runtime tree, screenshots, metrics, input и diagnostics должны быть доступны через shared workspace model, а не через управление мышью или клавиатурой редактора.

Эта спецификация не вводит managed C# debugger. Debugger с breakpoints, stack frames, locals и watches закрывается отдельными scripting/debugger задачами.

## Session model

При запуске current scene или project Editor создаёт `WorkspaceSnapshot` и связывает run job с runtime session:

- `SessionId` из `RuntimeDebugBridge`;
- `SessionKind = EditorAttachedPreview`;
- `InputSnapshotId`;
- `InputWorkspaceRevision`;
- `InputContentRevision`;
- `InputDocumentRevisions`;
- `InputBuildConfigurationHash`;
- `ScenePath`;
- `State`: `Running`, `Paused`, `Stopped` или `Crashed`;
- `VisibleMode`: `SeparateWindow` или `EmbeddedViewport`;
- `IsProcessIsolated = true`;
- `Diagnostics`;
- `HighlightedNodePath`, если Editor, Inspector или агент выбрали runtime node.

`SeparateWindow` является допустимым видимым режимом для `0.1-preview`; embedded viewport может быть добавлен поверх того же session contract без изменения Tooling/MCP payload.

## Управление

Runtime-control commands работают только с активной Editor-attached session:

- `runtime_start` создаёт run job, snapshot identity и active runtime session;
- `runtime_stop` останавливает session и очищает active state;
- `runtime_pause` переводит session в `Paused`;
- `runtime_resume` переводит session в `Running`;
- `runtime_step` поддерживает `frame` и `physics`;
- `runtime_inject_input` принимает action name и `pressed`/`released`;
- `runtime_capture_frame` возвращает screenshot metadata и stable artifact id;
- `runtime_get_scene_tree` возвращает Remote Scene Tree;
- `runtime_get_diagnostics` возвращает runtime diagnostics;
- `runtime_highlight_node` сохраняет выбранный runtime node path для Remote Scene Tree/Inspector/Agent Workspace.

Если active session отсутствует, command должен fail-closed со structured diagnostic `E2D-RUNTIME-0001`.

## Crash isolation

Editor-owned game process может завершиться аварийно. В этом случае:

- Editor workspace остаётся доступным;
- active runtime session получает state `Crashed`;
- diagnostics содержат exit code и stderr summary без секретов;
- `runtime_get_diagnostics` и `electron2d://runtime/session` показывают crash state;
- повторный `runtime_start` может заменить crashed session новой session.

## MCP и Agent Workspace visibility

`electron2d://runtime/session` возвращает active runtime session state, snapshot identity, current metrics, highlighted node и diagnostics. Agent Workspace использует тот же payload внутри Editor; MCP не получает отдельную приватную модель.

MCP tools должны использовать те же Tooling runtime commands, что и Editor. AI-агент не должен управлять видимым запуском через GUI automation.

## Acceptance criteria

- Спецификация Editor-attached runtime control существует отдельно от deterministic runtime debug bridge.
- Runtime session создаётся с `SessionKind = EditorAttachedPreview`, snapshot identity и `IsProcessIsolated = true`.
- `runtime_start` создаёт active session и run job; repeated start заменяет stopped/crashed session, но не silently теряет running session.
- `runtime_pause`, `runtime_resume`, `runtime_step`, `runtime_inject_input`, `runtime_capture_frame`, `runtime_get_scene_tree`, `runtime_get_diagnostics`, `runtime_highlight_node` и `runtime_stop` работают через общий Tooling layer.
- Remote Scene Tree показывает runtime nodes; highlighted node path сохраняется в session payload.
- Crash game process не завершает workspace, переводит session в `Crashed` и возвращает structured diagnostics.
- MCP resource `electron2d://runtime/session` показывает active play session, snapshot identity, metrics, highlighted node и diagnostics.
- MCP runtime tools используют тот же runtime-control layer и не требуют внешнего AI-провайдера.
- Editor Capability Manifest помечает fine-grained runtime controls как `supported` для Editor/Tooling/MCP, если production semantics реализованы.
- Implementation documentation описывает фактический scope, ограничения и focused test command.

## Фактическое состояние, ограничения и проверки

Статус: реализованный внутренний контракт для `T-0144`.
Обновлено: 2026-06-23.
Связанные документы: [Editor-attached runtime control](editor-attached-runtime-control.md); [Runtime debug bridge и scene inspection](runtime-debug-bridge.md); [WorkspaceSnapshot, materialization и job input identity](../project-system/workspace-snapshot.md); [Локальный MCP adapter](../mcp/mcp-server.md); [Editor Capability Manifest](../tooling/editor-capability-manifest.md).

## Назначение

Editor-attached runtime control связывает запуск игры из Editor с общей моделью runtime session в `ProjectWorkspace`. Developer видит запуск через Editor-owned game process, а Agent Workspace и MCP читают тот же state через workspace runtime session. AI не управляет редактором через mouse/keyboard automation: он вызывает Tooling/MCP runtime commands.

Текущая реализация использует уже существующий Editor run workflow как владельца отдельного game process и `RuntimeDebugBridge` как общий deterministic read/control layer для scene tree, pause/step/input, screenshot и metrics. Настоящий renderer frame и managed debugger подключаются отдельными задачами; текущий screenshot остаётся stable preview PNG для проверки contract.

## Workspace runtime session

`ProjectWorkspace.Runtime` хранит `ActiveSession`. Session содержит:

- `SessionId`;
- `SessionKind = EditorAttachedPreview`;
- `State`: `Running`, `Paused`, `Stopped` или `Crashed`;
- `VisibleMode`: сейчас используется `SeparateWindow`;
- `IsProcessIsolated = true`;
- `InputSnapshotId`, `InputWorkspaceRevision`, `InputContentRevision`, `InputDocumentRevisions`, `InputBuildConfigurationHash`;
- `ScenePath`;
- counters frame/physics frame;
- input action states;
- `HighlightedNodePath`;
- runtime diagnostics.

`ToolingRuntimeService.StartEditorAttached(...)` создаёт immutable `WorkspaceSnapshot`, materializes it into `.electron2d/workspaces/<session>/<snapshot>/`, enqueues a run job and starts `RuntimeDebugBridge` with `SessionKind = EditorAttachedPreview`. Source files, persisted revisions and dirty state are not changed.

## Tooling commands

`ProjectToolingHost.Runtime` exposes:

- `Queue(...)` for the existing job-only `project.run` path;
- `StartEditorAttached(...)`;
- `Pause()`;
- `Resume()`;
- `Stop()`;
- `Step(RuntimeStepKind.Frame|Physics, count, fixedDelta)`;
- `InjectInput(action, pressed)`;
- `CaptureFrame()`;
- `GetSceneTree()`;
- `GetDiagnostics()`;
- `HighlightNode(nodePath)`;
- `ReportProcessCrash(exitCode, stderr)`.

All commands fail closed with `E2D-RUNTIME-0001` when there is no active attached session or the session cannot accept the command.

## MCP visibility

`electron2d://runtime/session` returns active session state, snapshot identity, metrics, highlighted node path, input actions and diagnostics.

Runtime MCP tools now call the same Tooling runtime service:

- `runtime_start`;
- `runtime_stop`;
- `runtime_pause`;
- `runtime_resume`;
- `runtime_step`;
- `runtime_inject_input`;
- `runtime_capture_frame`;
- `runtime_get_scene_tree`;
- `runtime_get_diagnostics`;
- `runtime_highlight_node`;
- `runtime_report_crash`.

`runtime_start` returns a queued run job event with snapshot identity. Read/control tools return the current runtime payload and do not require an external AI provider.

## Crash isolation

`ReportProcessCrash(exitCode, stderr)` marks the active session as `Crashed` and appends `E2D-RUNTIME-0001` diagnostics with a sanitized stderr summary. The `ProjectWorkspace` remains usable, MCP can still read diagnostics and runtime resource state, and `Stop()` clears the active session.

## Limitations

- Current screenshot bytes are deterministic preview output, not a real renderer capture.
- Current runtime scene tree is parsed from the materialized snapshot scene JSON.
- Process pause/step is represented by the debug bridge control state; low-level OS process suspension is not part of this contract.
- Managed breakpoints, stack frames, locals and watches belong to `Electron2D.ManagedDebugging`; runtime debug bridge remains responsible only for scene/runtime inspection and frame control.
- Embedded viewport can be added later without changing Tooling/MCP payload because `VisibleMode` is already part of session state.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ToolingServiceBoundaryTests|FullyQualifiedName~Electron2DMcpServerTests|FullyQualifiedName~EditorCapabilityManifestTests"
```

Проверка покрывает Tooling start/control/crash, MCP runtime tools/resources and `Editor Capability Manifest` status for fine-grained runtime controls.
