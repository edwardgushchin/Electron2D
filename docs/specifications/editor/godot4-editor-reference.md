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

## Acceptance reference

`T-0157` должен включать screenshot/golden acceptance:

- default layout на Windows, Linux и macOS;
- верхний workspace switcher содержит только `2D`, `Script`, `Game`, `Tasks`;
- слева видны `Scene` и `FileSystem`;
- справа видны `Inspector`/`Node` и `Agent Workspace`;
- снизу видна сворачиваемая bottom panel;
- `3D` и `AssetLib` отсутствуют во всех видимых menus, shortcuts и create-node flows;
- `Tasks` занимает центральную область;
- `Agent Workspace` сохраняет dock placement после restart;
- переключение workspaces не сбрасывает selection/open tabs/layout.
