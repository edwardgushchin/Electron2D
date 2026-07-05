# C# script classes, inheritance from `Node` и lifecycle

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0044`.
Обновлено: 2026-06-21.
Связанные документы: [Формат проекта и шаблон `electron2d-empty`](../release-management/project-template.md), [Script metadata: `[Export]`, `[Signal]`, `[Tool]`](script-metadata.md), [Electron2D 0.1-preview](../releases/0.1-preview.md).

## Назначение

`0.1-preview` поддерживает только C# script classes. Script class - это обычный C# тип в проекте пользователя, который наследуется от `Electron2D.Node` или другого Electron2D node type и компилируется обычной .NET toolchain вместе с проектом игры.

Задача не добавляет GDScript, visual scripting, обязательный Hot Reload, runtime C# compilation, dynamic iOS code load или managed debugger. Встроенная C# IDE описана отдельной спецификацией `editor-script-workflow.md` и обязательна для полного редакторского workflow `0.1-preview`.

## Контракт script class

Script class должен:

- наследоваться от `Node` или другого Electron2D node type;
- использовать обычные C# overrides для lifecycle callbacks: `_EnterTree()`, `_Ready()`, `_Process(double)`, `_PhysicsProcess(double)`, `_Input(InputEvent)`, `_ExitTree()`;
- получать lifecycle через существующий `SceneTree` traversal;
- обращаться к дереву через `GetTree()`;
- обращаться к runtime services через публичные facades, например `RenderingServer`.

## Template sample

Шаблон `data/templates/electron2d-empty/` должен содержать `Scripts/MainScene.cs`.

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
- Создание/attach script из редактора, встроенное редактирование C#, language services и managed debugger описаны в `docs/scripting/editor-script-workflow.md`.

## Проверки

- Integration tests проверяют script class, который наследуется от `Node`, получает lifecycle callbacks и читает engine services.
- `tools/Verify-ProjectTemplate.ps1` собирает свежий local package, создаёт проект из template, восстанавливает его в изолированный packages folder, собирает обычным .NET toolchain и проверяет lifecycle/services output.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0044`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1-preview` фиксирует C# script model без runtime compilation: пользовательский script - это обычный C# класс, который наследуется от `Electron2D.Node` или другого Electron2D node type и компилируется вместе с проектом игры.

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
- `[Export]`, `[Signal]`, `[Tool]` metadata реализованы отдельным baseline и описаны в `docs/scripting/script-metadata.md`.
- Создание script из редактора, attach к node, встроенное редактирование code text и build diagnostics описаны в `docs/scripting/editor-script-workflow.md`.
