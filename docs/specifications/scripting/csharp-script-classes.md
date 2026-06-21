# C# script classes, inheritance from `Node` и lifecycle

Статус: целевая спецификация для `T-0044`.
Обновлено: 2026-06-21.
Связанные документы: [Формат проекта и шаблон `electron2d-empty`](../release-management/project-template.md), [Script metadata: `[Export]`, `[Signal]`, `[Tool]`](script-metadata.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`0.1.0 Preview` поддерживает только C# script classes. Script class - это обычный C# тип в проекте пользователя, который наследуется от Godot-like `Electron2D.Node` или его наследников и компилируется обычной .NET toolchain вместе с проектом игры.

Задача не добавляет GDScript, visual scripting, обязательный Hot Reload, runtime C# compilation, dynamic iOS code load или встроенный полноценный IDE.

## Контракт script class

Script class должен:

- наследоваться от `Node` или Godot-like node subclass;
- использовать обычные C# overrides для lifecycle callbacks: `_EnterTree()`, `_Ready()`, `_Process(double)`, `_PhysicsProcess(double)`, `_Input(InputEvent)`, `_ExitTree()`;
- получать lifecycle через существующий `SceneTree` traversal;
- обращаться к дереву через `GetTree()`;
- обращаться к runtime services через публичные Godot-like facades, например `RenderingServer`.

## Template sample

Шаблон `templates/electron2d-empty/` должен содержать `Scripts/MainScene.cs`.

Минимальный sample:

- компилируется через `dotnet build`;
- запускается через `dotnet run`;
- создаёт `SceneTree`;
- добавляет `MainScene : Node` в `SceneTree.Root`;
- подтверждает `_EnterTree()` и `_Ready()`;
- подтверждает доступ к `GetTree()` и `RenderingServer`.

## Ошибки и ограничения

- Script classes не загружаются динамически на runtime path.
- iOS не требует runtime compilation или dynamic assembly loading.
- Export metadata, signals metadata и `[Tool]` описаны отдельной спецификацией `script-metadata.md`.
- Создание/attach script из редактора и открытие внешнего IDE остаются задачей `T-0046`.

## Проверки

- Integration tests проверяют script class, который наследуется от `Node`, получает lifecycle callbacks и читает engine services.
- `tools/Verify-ProjectTemplate.ps1` собирает свежий local package, создаёт проект из template, восстанавливает его в изолированный packages folder, собирает обычным .NET toolchain и проверяет lifecycle/services output.
