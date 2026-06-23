# Project Tasks board редактора

Статус: реализовано для `T-0155`.
Обновлено: 2026-06-23.

## Назначение

`Tasks` — центральное рабочее пространство `Electron2D.Editor` для задач пользовательского проекта. Оно показывает встроенный `ProjectTaskManager` как обычную доску задач, а не как Markdown-файл, dock-панель или часть Agent Workspace.

Текущая реализация model-first: `EditorProjectTasksBoardSnapshot` описывает видимую доску, выбранную карточку, правый `Task Details`, фильтры, действия и visual harness. Постоянный desktop event loop, live drag-and-drop pointer events и полная привязка к открытому окну добавляются следующими задачами поверх этого snapshot-контракта.

## Snapshot model

Snapshot доски содержит:

- workspace switcher `2D`, `Script`, `Game`, `Tasks` и выбранный workspace `Tasks`;
- колонки `Backlog`, `Ready`, `In Progress`, `Blocked`, `Review`, `Awaiting Acceptance`, `Done`, `Cancelled`;
- карточки с task id, title, priority, labels, assignee, readiness, blocking reasons, rank и archived state;
- выбранную карточку `T-0155` и правый `Inspector` с заголовком `Task Details`;
- description, acceptance criteria, subtasks, activity kinds, linked transactions, jobs, diagnostics, artifacts и linked scenes/resources/nodes;
- drag-and-drop intent с target status, target rank, allowed result и diagnostic code для запрещённого transition;
- actions `Accept`, `Request Changes`, `Cancel`, `Create`, `Edit`, `Archive`, `Hard Delete`;
- filters `Status`, `Priority`, `Labels`, `Assignee`, `Text`, `Linked Object`;
- признаки stable rank round-trip, archive view, hidden archived task, destructive confirmation, trusted human acceptance и работы без AI/MCP-сессии.

`Accept` и `Request Changes` помечены как trusted interactive Editor actions: они требуют доверенный контекст действия человека внутри редактора. Агентский payload не может включить `AgentAcceptActionAvailable` или принять задачу за пользователя.

## Visual harness

Команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --tasks-board-smoke .temp\project-tasks-board
```

Создаёт:

- `.temp/project-tasks-board/project-tasks-board.state.json`;
- `.temp/project-tasks-board/visual/project-tasks-board.png`;
- `.temp/project-tasks-board/visual/project-tasks-board.analysis.json`.

PNG является deterministic screenshot artifact для обязательной визуальной проверки UI-задач. JSON analysis содержит bounds доски, колонок, правого `Task Details`, filters, actions, drag-and-drop state, acceptance guard, счётчик clickable controls, результат проверки text overflow и список forbidden UI matches.

В текущей проверке агент открыл `project-tasks-board.png` и подтвердил:

- `Tasks` выбран в workspace switcher и занимает центральную область;
- колонки доски читаемы, карточка `T-0155` выделена, drop-зоны видимы;
- справа виден `Inspector` с `Task Details`, `Acceptance`, `Activity` и `Artifacts`;
- действия `Accept`, `Request Changes`, `Cancel`, `Create`, `Edit`, `Archive`, `Hard Delete` видимы и не выходят за контейнеры;
- текст не перекрывает соседние элементы;
- `3D`, `AssetLib`, GDScript UI, `.gd` и disabled 3D controls визуально отсутствуют.

## Проверки

Focused test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectTasksBoardTests"
```

Smoke-команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --tasks-board-smoke .temp\project-tasks-board
```

Документационный verifier после изменения справки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LocalDocumentation.ps1
```
