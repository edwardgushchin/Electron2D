# Формат проекта и шаблон `electron2d-empty`

Статус: реализованный минимальный шаблон.
Задача: `T-0006`.
Обновлено: 2026-06-20.

## Где находится шаблон

```text
templates/electron2d-empty/
```

Шаблон содержит .NET template metadata, минимальный `.csproj`, `Program.cs`, `project.e2d.json` и пустую сцену `scenes/main.scene.json`.

## Как проверять

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ProjectTemplate.ps1
```

Команда собирает локальный package `Electron2D.0.1.0-preview`, копирует шаблон во временную директорию, восстанавливает проект из локального package source, собирает и запускает его.

## Ограничение baseline

Шаблон пока не использует runtime API, потому что Godot-like object model ещё не реализована. Он фиксирует формат проекта и проверяемый запуск пустой сцены-манифеста без legacy component API.
