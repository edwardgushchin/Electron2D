# Референс интерфейса редактора Godot 4

Статус: целевая спецификация для `0.1.0 Preview`.
Задача: `T-0157`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor` использует Godot 4 как канонический UX- и layout-референс: знакомая структура меню, переключения рабочих пространств, центральной области, docks и нижних panels должна сохраняться, чтобы разработчик быстро понимал редактор без отдельного обучения.

Эта спецификация фиксирует именно интерфейсную структуру. Она не меняет runtime API-контракт: публичный C# API Electron2D `0.1.0` остаётся 100% совместимым с утверждённым 2D-профилем Godot `4.7-stable` .NET/C# API под namespace `Electron2D`.

## Baseline

Baseline для `0.1.0`:

- версия UI-референса: Godot `4.7-stable`;
- документация: [первый взгляд на интерфейс Godot](https://docs.godotengine.org/ru/4.x/getting_started/introduction/first_look_at_the_editor.html);
- тип референса: layout, информационная архитектура, названия основных зон, ожидаемое расположение docks и рабочий цикл редактора.

Ссылка `/4.x/` используется только как человекочитаемая документация. Для acceptance перед реализацией `T-0157` нужно сохранить локальный screenshot/reference packet для выбранной версии Godot, чтобы будущие изменения документации Godot не меняли baseline задним числом.

## Default layout

Структура по умолчанию:

```text
┌────────────────────────────────────────────────────────────────────────────┐
│ Scene  Project  Debug  Editor  Help     [2D] [Script] [Game] [Tasks]      │
│                                              Run Scene  Run Project  Stop  │
├────────────────────────────────────────────────────────────────────────────┤
│ Scene tabs / document tabs                                                │
├──────────────────┬──────────────────────────────────┬──────────────────────┤
│ Scene            │                                  │ Inspector / Node     │
│                  │                                  │                      │
│                  │       Active workspace           ├──────────────────────┤
│                  │                                  │ Agent Workspace      │
│  Scene Tree      │  2D / Script / Game / Task Board │                      │
├──────────────────┤                                  │                      │
│ FileSystem       │                                  │                      │
├──────────────────┴──────────────────────────────────┴──────────────────────┤
│ Output | Debugger | Diagnostics | Search | Animation | Audio | Tests       │
└────────────────────────────────────────────────────────────────────────────┘
```

Default docks:

| Godot 4 zone | Electron2D zone |
| --- | --- |
| Верхнее меню | `Scene`, `Project`, `Debug`, `Editor`, `Help` |
| Main screen buttons | `2D`, `Script`, `Game`, `Tasks` |
| Scene dock | `Scene` dock слева |
| FileSystem dock | `FileSystem` dock слева под `Scene` |
| Inspector/Node dock | `Inspector`/`Node` dock справа |
| Bottom panel | `Output`, `Debugger`, `Diagnostics`, `Search`, `Animation`, `Audio`, `Tests` |
| Run controls | `Run Current Scene`, `Run Project`, `Stop`, `Pause`, `Restart` там, где уместно |

## Центральные workspaces

### `2D`

Основное редактирование игровых сцен:

- 2D viewport;
- выделение и трансформация узлов;
- move, rotate, scale;
- snapping, guides и pivot;
- `Camera2D` preview;
- collision shapes;
- `TileMap`;
- `Control` layout;
- вспомогательная визуализация physics bounds и navigation, если соответствующая 2D-подсистема включена в профиль.

### `Script`

Центральное рабочее пространство со встроенной базовой C# IDE. Разработчик должен написать, исследовать, собрать и отладить игровой код без выхода из `Electron2D.Editor` и без установки внешней IDE.

Минимальный состав:

- вкладки открытых `.cs` документов;
- C# syntax highlighting;
- line numbers;
- автоматические отступы и настройка tabs/spaces;
- matching скобок и кавычек;
- code folding;
- текущая строка;
- поиск и замена в файле;
- поиск по проекту;
- переход к строке;
- clipboard;
- Undo/Redo;
- save file и `Save All`;
- dirty state;
- восстановление открытых вкладок после перезапуска Editor;
- project-aware IntelliSense;
- live compiler diagnostics;
- go to definition;
- find references;
- rename symbol;
- document formatting;
- basic code actions;
- C# debugger.

Внешняя IDE может существовать как дополнительный workflow после `0.1.0`, но не является способом закрыть обязательные требования `Script` workspace.

Проверяемый минимум UI для `T-0158`:

- `Script` выбран в общем workspace switcher и занимает центральную область.
- Видны document tabs открытых `.cs` файлов, active tab и dirty marker.
- Центральная editor surface показывает gutter с line numbers, C# highlighting, current line, caret и selection.
- Search/replace in file, project search, go to line, save, `Save All`, Undo/Redo и clipboard actions видны как controls или machine-readable commands harness.
- В правом `Inspector` или вспомогательной панели видны сведения `CodeDocument`: `DocumentId`, path, revision, persisted revision, dirty state, diagnostics и semantic version.
- External-change conflict marker и `WorkspaceSnapshot` input identity видны в model/analysis, если harness использует synthetic conflict.
- PNG screenshot и JSON analysis фиксируют bounds tabs/gutter/editor/search/actions, caret/selection state, text overflow result и отсутствие GDScript/3D/AssetLib UI.

Проверяемый минимум UI для `T-0159`:

- `Script` workspace остаётся выбранным в общем workspace switcher и не открывает внешнюю IDE.
- В central editor surface виден unsaved C# buffer, для которого запрошены language services.
- Completion popup находится рядом с caret/current line, содержит Electron2D API entries, local symbols и selected item, текст popup не выходит за bounds.
- Hover/Quick Info panel виден рядом с символом и показывает symbol display plus XML documentation summary.
- Diagnostics panel или inline diagnostics strip виден в нижней/правой зоне, содержит live compiler diagnostic code, severity и project-relative source location.
- Signature help state, current active parameter, go-to-definition target, references count, rename preview, formatting/code-action result и stale-response marker присутствуют в JSON analysis.
- Screenshot и analysis фиксируют keyboard selection/focus state для completion popup, отсутствие text overflow и отсутствие GDScript/3D/AssetLib UI.

Проверяемый минимум UI для `T-0160`:

- `Script` workspace остаётся выбранным в общем workspace switcher и показывает C# debugger как часть встроенного редактора, без внешней IDE.
- В gutter виден активный breakpoint с enabled/verified state; у breakpoint есть доступная click target область для enable/disable/remove.
- В editor surface видна current execution line highlight, не совпадающая с dirty buffer после `stale` marker.
- Верхние debugger controls показывают `Start Debug`, `Attach`, `Pause`, `Continue`, `Stop`, `Restart`, `Step Into`, `Step Over`, `Step Out`; controls кликабельны и не меняют layout.
- В нижней или правой Debugger панели видны threads, call stack, selected stack frame, locals, arguments, watches, watch evaluation result, exception info, stack trace и Debug Output.
- JSON analysis фиксирует bounds breakpoint gutter, current line highlight, controls, call stack, threads, locals/arguments/watches, exception panel, debug output, `stale` marker, DAP boundary и отсутствие text overflow.
- Screenshot и analysis фиксируют отсутствие `3D`, `AssetLib`, GDScript UI, `.gd` files и remote mobile/WebAssembly debugger controls в обязательном `0.1.0` workflow.

### `Game`

Видимый тестовый запуск проекта:

- запуск текущей сцены;
- запуск main scene;
- stop, restart и pause;
- step frame;
- step physics frame;
- input injection;
- screenshot;
- runtime scene tree;
- просмотр runtime properties;
- visual diff;
- performance counters.

`Game` workspace использует `RuntimeController` и debug bridge. Managed C# debugger остаётся отдельной подсистемой `Script`/`Debugger`, но обе сессии должны ссылаться на один `WorkspaceSnapshot`, когда запуск выполняется из Editor.

### `Tasks`

`ProjectTaskManager` является центральным workspace, а не dock-панелью и не нижней вкладкой.

Колонки по умолчанию:

```text
Backlog | Ready | In Progress | Blocked | Review |
Awaiting Acceptance | Done | Cancelled
```

Выбранная карточка отображается в `Inspector` как `Task Details`:

- description;
- priority;
- labels;
- assignee;
- dependencies;
- subtasks;
- acceptance criteria;
- activity;
- linked transactions;
- linked jobs;
- linked diagnostics;
- screenshots;
- runtime artifacts;
- linked scenes, resources и nodes.

`Tasks` workspace должен полностью работать вручную без AI.

Проверяемый минимум UI для `T-0155`:

- `Tasks` выбран в общем workspace switcher и занимает центральную область, а не dock и не bottom panel.
- Board показывает колонки `Backlog`, `Ready`, `In Progress`, `Blocked`, `Review`, `Awaiting Acceptance`, `Done`, `Cancelled`.
- Карточки показывают task id, title, priority, labels, assignee, readiness, blocking reasons и rank.
- У выбранной карточки справа в `Inspector` виден `Task Details`, включая description, dependencies, acceptance criteria, activity и linked artifacts.
- На board видны drag-and-drop affordances, то есть понятные зоны/подсказки перемещения карточки между колонками и ручной сортировки.
- Действия `Accept`, `Request changes`, `Cancel`, `Create`, `Edit`, `Archive` и `Hard delete` видны как отдельные controls; `Accept`/`Request changes` являются trusted interactive Editor actions и не выполняются от имени AI.
- `Review` и `Awaiting Acceptance` визуально различимы, а manual blocker и dependency blocker показаны разными markers.
- Фильтры status, priority, labels, assignee, text и linked object находятся в верхней области board.
- PNG screenshot и JSON analysis фиксируют bounds board/details/actions, количество кликабельных controls, отсутствие text overflow и отсутствие forbidden UI entries.

## Agent Workspace dock

`Agent Workspace` — постоянная dock-панель, доступная одновременно с любым центральным workspace.

Default placement:

```text
Right dock area, below Inspector/Node or in the same dock group.
```

Панель должна быть:

- dockable;
- resizable;
- hideable;
- movable;
- maximizable;
- сохраняющей layout между запусками;
- доступной в `2D`, `Script`, `Game` и `Tasks`.

Содержимое:

- активный AI-agent и MCP handshake state;
- текущая задача;
- terminal session;
- последнее действие;
- changeset;
- изменённые сцены, nodes, resources и scripts;
- diagnostics;
- job progress;
- screenshots;
- runtime snapshots;
- cancel/stop;
- grouped Undo.

## Script workspace: C#-only

В `Electron2D.Editor` отсутствуют:

- GDScript language selector;
- `.gd` files;
- GDScript templates;
- GDScript language server;
- GDScript diagnostics;
- GDScript documentation panels;
- visual scripting.

Поддерживается только C#.

## Полное исключение 3D

3D не является частью продукта `0.1.0`, даже как disabled UI.

В Editor отсутствуют:

- workspace `3D`;
- `Node3D` в create-node UI;
- 3D viewport;
- perspective/orthographic camera controls;
- 3D transform gizmos;
- 3D grid;
- 3D navigation;
- 3D physics debug;
- mesh/skeleton/material editors для 3D;
- импорт 3D scenes;
- 3D project settings;
- 3D renderer options;
- disabled 3D buttons;
- 3D shortcuts;
- 3D tutorial hints.

`AssetLib` не копируется из Godot в `0.1.0`. Если в будущем появится каталог ассетов, он должен иметь отдельную спецификацию и задачи.

## Layout persistence

Editor должен сохранять:

- видимость docks;
- размеры panels;
- dock placement;
- порядок tabs;
- выбранный central workspace;
- открытые scene/code/task documents;
- selection, scroll, zoom и editor camera state там, где это применимо.

Workspace switching не должен сбрасывать:

- selection;
- scroll/zoom;
- открытые документы;
- текущую задачу;
- active debug/run session display;
- Agent Workspace state.

## Keyboard baseline

Минимальные shortcuts должны соответствовать ожиданиям Godot-style редактора, но без 3D и GDScript:

- `F5` — run project;
- `F6` — run current scene;
- `F7` — switch to Script workspace или build, если выбран code context;
- `F8` — stop/pause active play/debug session по текущему mode;
- `Ctrl+S` — save current document;
- `Ctrl+Shift+S` — save all;
- `Ctrl+F` — search in current document или active panel;
- `Ctrl+Shift+F` — project search;
- `Ctrl+Z` / `Ctrl+Y` — undo/redo active document или current workspace command;
- `Ctrl+P` — quick open project file;
- `Ctrl+G` — go to line в Script workspace.

Точный shortcut map должен быть machine-readable и проверяться, чтобы 3D/GDScript shortcuts не появлялись в `0.1.0`.

## Visual acceptance reference

Каждая задача `Electron2D.Editor`, которая создаёт или меняет видимый UI, должна включать screenshot/golden acceptance: исполнитель открывает реальное окно редактора или documented automated harness, сохраняет screenshot как артефакт задачи и явно анализирует layout до передачи на приёмку. Одной сборки, model test или headless smoke-команды недостаточно, если UI должен быть видимым пользователю.

Минимальный visual checklist для таких задач:

- default layout на Windows, Linux и macOS;
- верхний workspace switcher содержит только `2D`, `Script`, `Game`, `Tasks`;
- слева видны `Scene` и `FileSystem`;
- справа видны `Inspector`/`Node` и `Agent Workspace`;
- снизу видна сворачиваемая bottom panel;
- новые кнопки, поля, вкладки, панели и списки находятся в ожидаемой области layout;
- pointer/keyboard interaction работает для изменённых элементов там, где это применимо;
- текст не выходит за границы контейнеров и не перекрывает соседние элементы;
- hover, active, focus, selected и disabled states не меняют layout непредсказуемо;
- `3D` и `AssetLib` отсутствуют во всех видимых menus, shortcuts и create-node flows;
- `Tasks` занимает центральную область;
- `Agent Workspace` сохраняет dock placement после restart;
- переключение workspaces не сбрасывает selection/open tabs/layout.

## T-0157 shell layout harness

До появления постоянного desktop event loop задача `T-0157` закрывает default shell через документированный automated harness. Harness является проверяемым способом построить тот же layout model, который создаёт стартовый `EditorApplication`, сохранить визуальный PNG-артефакт и машинно-читаемый JSON-анализ.

Harness должен:

- строить `EditorShellLayoutSnapshot` с фиксированным viewport `1280x720`;
- включать верхнее меню `Scene`, `Project`, `Debug`, `Editor`, `Help`;
- включать workspace switcher только `2D`, `Script`, `Game`, `Tasks`;
- размещать `Scene` и `FileSystem` в left dock area;
- размещать `Inspector`, `Node` и `Agent Workspace` в right dock area;
- размещать bottom panel с `Output`, `Debugger`, `Diagnostics`, `Search`, `Animation`, `Audio`, `Tests`;
- поддерживать collapse/expand bottom panel;
- сохранять и восстанавливать layout docks, размеры, открытые document tabs и выбранный workspace через JSON state file;
- сохранять per-workspace state при переключении: selection, scroll, zoom и open documents;
- отдавать machine-readable shortcut map без 3D/GDScript actions;
- проверять, что видимые labels, shortcut actions и create-node/workspace entries не содержат `3D`, `AssetLib`, `GDScript`, `.gd` или disabled 3D controls;
- сохранять PNG screenshot и JSON analysis artifact в указанную harness directory;
- записывать в analysis координаты workspace switcher, left docks, right docks, Agent Workspace и bottom panel, результат проверки переполнения текста, кликабельности основных controls и список forbidden UI matches.

PNG-артефакт должен быть пригоден для ручного просмотра агентом перед закрытием задачи. JSON-анализ не заменяет просмотр screenshot: исполнитель обязан открыть сохранённый PNG, убедиться, что layout читаем и соответствует этой спецификации, и зафиксировать результат в дневнике разработки.
