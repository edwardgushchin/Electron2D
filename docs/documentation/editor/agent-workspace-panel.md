# Agent Workspace panel редактора

Статус: реализовано для `T-0150`.
Обновлено: 2026-06-23.

## Назначение

`Agent Workspace` — правая dock-панель `Electron2D.Editor`, которая показывает состояние локального AI-агента и его работу с открытым проектом. Панель не является встроенным чатом и не привязана к одному поставщику моделей: она отображает уже нормализованные данные редактора, project system и tooling-слоёв.

Текущая реализация model-first: `EditorAgentWorkspacePanelSnapshot` собирает состояние для UI и visual harness. Постоянный desktop event loop и live binding к реальному MCP-соединению подключаются следующими задачами поверх этого snapshot-контракта.

## Snapshot model

Snapshot панели содержит:

- session state: `AgentSessionId`, profile id, connection state, handshake state, active Editor route и последнее действие;
- current task: id, статус, acceptance state, linked transactions, linked jobs, linked diagnostics и linked artifacts;
- changeset: изменённые scene, node, resource, script и project settings entries с navigation target для обычного UI редактора;
- diagnostics: полный `StructuredDiagnostic` payload из общего diagnostics layer, включая `location`, `relatedLocations` и `suggestedFixes`;
- artifacts: screenshots, runtime snapshots и job artifacts с `WorkspaceJobInputIdentity`;
- active job: kind, state, progress, cancel availability, stale markers и input identity;
- actions: `Send Review`, `Undo AI`, `Cancel`, `Stop`.

Панель намеренно не создаёт action `Done` и не даёт AI-контексту принять задачу за человека. AI может отправить задачу на human review, но acceptance остаётся ручным действием.

## Dock layout

Default placement:

```text
RightBelowInspectorNode
```

То есть панель находится в правой dock area под `Inspector` и `Node`, доступна одновременно с центральными workspaces:

```text
2D | Script | Game | Tasks
```

Dock state фиксирует, что панель dockable, resizable, hideable, movable, maximizable и сохраняет layout между запусками.

## Visual harness

Команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --agent-workspace-panel-smoke .temp\agent-workspace-panel
```

Создаёт:

- `.temp/agent-workspace-panel/agent-workspace-panel.state.json`;
- `.temp/agent-workspace-panel/visual/agent-workspace-panel.png`;
- `.temp/agent-workspace-panel/visual/agent-workspace-panel.analysis.json`.

PNG является deterministic screenshot artifact для обязательной визуальной проверки UI-задач. JSON analysis содержит bounds dock-панели, visible workspaces, список секций, состояние session/task/job/actions, полный diagnostic payload, artifact input identity, счётчик clickable controls, результат проверки text overflow и признак отсутствия forbidden AI acceptance action.

В текущей проверке агент открыл `agent-workspace-panel.png` и подтвердил:

- `Agent Workspace` находится справа под `Inspector`/`Node`;
- доступны workspaces `2D`, `Script`, `Game`, `Tasks`;
- секции `Session`, `Current Task`, `Changeset`, `Diagnostics`, `Artifacts`, `Runtime`, `Jobs`, `Actions` читаемы;
- кнопки `Send Review`, `Undo AI`, `Cancel`, `Stop` видимы и не ломают layout;
- action `Done` визуально отсутствует;
- текст не выходит за границы контейнеров и не перекрывает соседние элементы.

## Проверки

Focused test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorAgentWorkspacePanelTests"
```

Smoke-команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --agent-workspace-panel-smoke .temp\agent-workspace-panel
```

Документационный verifier после изменения справки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LocalDocumentation.ps1
```
