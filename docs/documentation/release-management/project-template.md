# Формат проекта и шаблон `electron2d-empty`

Статус: реализованный минимальный шаблон.
Задача: `T-0006`.
Обновлено: 2026-06-20.

## Где находится шаблон

```text
data/templates/electron2d-empty/
```

Шаблон содержит .NET template metadata, минимальный `.csproj`, `Program.cs`, `Scripts/MainScene.cs`, `project.e2d.json`, `global.json`, `electron2d.lock.json` и пустую сцену `scenes/main.scene.json`.

`project.e2d.json` уже использует текущий project settings формат: `Electron2D.ProjectSettings`, `formatVersion: 1`, имя проекта, версию проекта, engine version, main scene, renderer profile, physics tick rate, пустой `input.actions` и display/window defaults.

`global.json` фиксирует .NET SDK `10.0.101` с `rollForward: latestFeature`. `electron2d.lock.json` фиксирует Electron2D package version, target framework, package metadata, renderer profile, physics backend marker, serialization schema version, export template version и signing policy `referencesOnly`.

Проверка этих файлов описана в [Reproducibility lock и e2d doctor](../project-system/reproducibility-lock-and-doctor.md).

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

При запуске из редактора template также понимает переменную процесса `ELECTRON2D_CURRENT_SCENE`. Это относительный project path для запуска выбранной scene без изменения `project.e2d.json`.

## Script sample

`Scripts/MainScene.cs` наследуется от `Electron2D.Node`, переопределяет `_EnterTree()` и `_Ready()`, а в `_Ready()` обращается к `GetTree()` и `RenderingServer`. Это минимальный baseline C# scripting без runtime compilation и без dynamic assembly load.
