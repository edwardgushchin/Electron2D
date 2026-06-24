# Agent Workspace panel редактора

Обновлено: 2026-06-24.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт, состояние и проверки

Статус: реализовано для `T-0150`, уточнено для нижней панели и используется script/debug tooling smoke из `T-0161`.
Обновлено: 2026-06-24.

## Назначение

`Agent Workspace` — вкладка `Agent` в нижней панели `Electron2D.Editor`, которая показывает состояние локального AI-агента и его работу с открытым проектом. Панель не является встроенным чатом и не привязана к одному поставщику моделей: она отображает уже нормализованные данные редактора, project system и tooling-слоёв.

Принцип размещения: `Inspector` и `Node` справа описывают выбранный объект, а `Agent Workspace` снизу описывает процесс: сессию, текущую задачу, changeset, jobs, diagnostics, artifacts и terminal. В правом dock такое содержимое слишком узкое для 1280x720, поэтому Agent получает ширину bottom panel, сворачивание, максимизацию и собственную вкладку.

Текущая реализация model-first: `AgentWorkspacePanelSnapshot` собирает состояние для UI и visual harness. Постоянный desktop event loop и live binding к реальному MCP-соединению подключаются следующими задачами поверх этого snapshot-контракта.

## Snapshot model

Snapshot панели содержит:

- session state: `AgentSessionId`, profile id, connection state, handshake state, active Editor route и последнее действие;
- current task: id, статус, acceptance state, linked transactions, linked jobs, linked diagnostics и linked artifacts;
- changeset: изменённые scene, node, resource, script и project settings entries с navigation target для обычного UI редактора;
- diagnostics: полный `StructuredDiagnostic` payload из общего diagnostics layer, включая `location`, `relatedLocations` и `suggestedFixes`;
- artifacts: screenshots, runtime snapshots и job artifacts с `WorkspaceJobInputIdentity`;
- active job: kind, state, progress, cancel availability, stale markers и input identity;
- actions: `Send Review`, `Undo AI`, `Cancel`, `Stop`.

Для script/debug tooling smoke панель дополнительно показывает нормализованные операции агента: `script_apply_text_edits`, current task `T-0161`, ссылку на workspace transaction, ссылку на debug job и ссылку на screenshot artifact. Это проверяет, что агентские изменения в Script workspace видны человеку через обычную нижнюю панель редактора, а не только в машинном ответе Tooling/MCP.

Панель намеренно не создаёт action `Done` и не даёт AI-контексту принять задачу за человека. AI может отправить задачу на human review, но acceptance остаётся ручным действием.

## Bottom panel layout

Default placement:

```text
BottomPanel/Agent
```

Старое persisted placement `RightBelowInspectorNode` мигрируется в `BottomPanel/Agent`. Вкладка нижней панели называется `Agent`, а заголовок открытой панели остаётся `Agent Workspace`.

Панель по умолчанию свёрнута вместе с bottom panel. Она автоматически раскрывается только при ошибке handshake, падении job или запросе review. Обычный прогресс обновляет badge вкладки `Agent`, но не перехватывает focus. Автоматическое раскрытие никогда не забирает keyboard focus у текущего workspace или активного control; пользователь должен сам перейти в `Agent`, если хочет взаимодействовать с панелью.

Внутренние вкладки панели:

```text
Overview | Changes | Jobs | Diagnostics | Artifacts | Terminal
```

Toolbar справа в панели содержит:

```text
Connection | Send Review | Undo AI | Cancel | Stop
```

`Send Review` отправляет текущий результат агентской работы на ручную проверку. `Undo AI` откатывает только изменения, внесённые через agent changeset и связанные undo transaction. `Cancel` отменяет активную agent job, если она поддерживает отмену. `Stop` завершает активную агентскую сессию или подключение, но не останавливает игровую run session редактора.

Внутренняя вкладка `Diagnostics` показывает только diagnostics агентского процесса: handshake, jobs, tool calls, artifacts, route resolution и ошибки agent state. Глобальная вкладка `Diagnostics` в bottom panel показывает общепроектные diagnostics и не смешивается с агентским журналом.

Панель доступна одновременно с центральными workspaces:

```text
2D | Script | Game | Tasks
```

Dock state фиксирует, что панель находится в bottom panel, resizable, hideable, maximizable и сохраняет высоту, раскрытое состояние и активную внутреннюю вкладку между запусками. Двойной клик по вкладке `Agent` максимизирует нижнюю панель.

Если нижней панели не хватает ширины, вкладка `Agent` участвует в общем overflow-menu bottom panel. Вкладки не сжимаются до нечитаемых подписей.

Выбор изменённой сцены, узла или скрипта открывает соответствующий центральный workspace, а подробности выбранного объекта показывает обычный `Inspector`.

## Visual harness

Команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --agent-workspace-panel-smoke .temp\agent-workspace-panel
```

Создаёт:

- `.temp/agent-workspace-panel/agent-workspace-panel.state.json`;
- `.temp/agent-workspace-panel/visual/agent-workspace-panel.png`;
- `.temp/agent-workspace-panel/visual/agent-workspace-panel.analysis.json`.

PNG является deterministic screenshot artifact для обязательной визуальной проверки UI-задач. JSON analysis содержит bounds вкладки `Agent` в bottom panel, visible workspaces, список внутренних вкладок, состояние session/task/job/actions, полный diagnostic payload, artifact input identity, счётчик clickable controls, результат проверки text overflow и признак отсутствия forbidden AI acceptance action.

В текущей проверке агент открыл `agent-workspace-panel.png` и подтвердил:

- справа находятся только `Inspector` и `Node`;
- `Agent Workspace` находится в нижней панели как вкладка `Agent`;
- доступны workspaces `2D`, `Script`, `Game`, `Tasks`;
- внутренние вкладки `Overview`, `Changes`, `Jobs`, `Diagnostics`, `Artifacts`, `Terminal` читаемы;
- кнопки `Send Review`, `Undo AI`, `Cancel`, `Stop` видимы и не ломают layout;
- action `Done` визуально отсутствует;
- текст не выходит за границы контейнеров и не перекрывает соседние элементы.

Дополнительная команда script/debug tooling smoke:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-debug-tooling-smoke .temp\script-debug-tooling
```

Она создаёт `script-debug-tooling.png` и проверяет, что нижняя вкладка `Agent` показывает `T-0161`, `transaction://op-script-agent-edit`, `job://op-debug-start` и `artifact://script-debug-tooling/screenshot.png`.

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
