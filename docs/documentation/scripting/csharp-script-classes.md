# C# script classes, inheritance from `Node` и lifecycle

Статус: реализованный baseline.
Задача: `T-0044`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1.0 Preview` фиксирует C# script model без runtime compilation: пользовательский script - это обычный C# класс, который наследуется от `Electron2D.Node` или другого Electron2D node type и компилируется вместе с проектом игры.

Текущий runtime уже предоставляет lifecycle callbacks на `Node`:

- `_EnterTree()`;
- `_Ready()`;
- `_Process(double delta)`;
- `_PhysicsProcess(double delta)`;
- `_Input(InputEvent inputEvent)`;
- `_ExitTree()`.

`SceneTree` вызывает эти методы через существующий traversal. Script получает доступ к дереву через `GetTree()` и к runtime services через публичные facades, например `RenderingServer`.

## Template sample

Шаблон `data/templates/electron2d-empty/` содержит:

- `Scripts/MainScene.cs` - минимальный script class `MainScene : Node`;
- `Program.cs` - создаёт `SceneTree`, добавляет `MainScene` в `Root` и печатает проверочные строки.

Ожидаемый output verifier:

```text
Electron2D empty scene loaded: scenes/main.scene.json
Electron2D C# script lifecycle: _EnterTree,_Ready
Electron2D C# script services: tree=True,text=True
```

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~CSharpScriptModelTests"
powershell -ExecutionPolicy Bypass -File tools\Verify-ProjectTemplate.ps1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```

## Ограничения

- Runtime не компилирует и не загружает C# scripts динамически.
- `[Export]`, `[Signal]`, `[Tool]` metadata реализованы отдельным baseline и описаны в `docs/documentation/scripting/script-metadata.md`.
- Создание script из редактора, attach к node, встроенное редактирование code text и build diagnostics описаны в `docs/documentation/scripting/editor-script-workflow.md`.
