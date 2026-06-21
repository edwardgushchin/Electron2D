# Формат проекта и шаблон `electron2d-empty`

Статус: реализованный минимальный шаблон.
Задача: `T-0006`.
Обновлено: 2026-06-20.

## Где находится шаблон

```text
templates/electron2d-empty/
```

Шаблон содержит .NET template metadata, минимальный `.csproj`, `Program.cs`, `Scripts/MainScene.cs`, `project.e2d.json` и пустую сцену `scenes/main.scene.json`.

## Как проверять

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ProjectTemplate.ps1
```

Команда собирает локальный package `Electron2D.0.1.0-preview`, копирует шаблон во временную директорию, восстанавливает проект из fresh local package source и NuGet.org в изолированный packages folder, собирает и запускает его.

Verifier проверяет output:

```text
Electron2D empty scene loaded: scenes/main.scene.json
Electron2D C# script lifecycle: _EnterTree,_Ready
Electron2D C# script services: tree=True,text=True
```

## Script sample

`Scripts/MainScene.cs` наследуется от `Electron2D.Node`, переопределяет `_EnterTree()` и `_Ready()`, а в `_Ready()` обращается к `GetTree()` и `RenderingServer`. Это минимальный baseline C# scripting без runtime compilation и без dynamic assembly load.
