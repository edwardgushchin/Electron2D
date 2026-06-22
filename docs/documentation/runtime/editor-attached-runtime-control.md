# Editor-attached runtime control

Статус: реализованный внутренний контракт для `T-0144`.
Обновлено: 2026-06-23.
Связанные документы: [Editor-attached runtime control](../../specifications/runtime/editor-attached-runtime-control.md); [Runtime debug bridge и scene inspection](runtime-debug-bridge.md); [WorkspaceSnapshot, materialization и job input identity](../project-system/workspace-snapshot.md); [Локальный MCP adapter](../mcp/mcp-server.md); [Editor Capability Manifest](../tooling/editor-capability-manifest.md).

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
- Managed breakpoints, stack frames, locals and watches belong to the managed debugger tasks.
- Embedded viewport can be added later without changing Tooling/MCP payload because `VisibleMode` is already part of session state.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ToolingServiceBoundaryTests|FullyQualifiedName~Electron2DMcpServerTests|FullyQualifiedName~EditorCapabilityManifestTests"
```

Проверка покрывает Tooling start/control/crash, MCP runtime tools/resources and `Editor Capability Manifest` status for fine-grained runtime controls.
