# Формат проекта и шаблон `electron2d-empty`

Статус: целевая спецификация.
Задача: `T-0006`.
Обновлено: 2026-06-20.

## Цель

Новый проект Electron2D должен начинаться из минимального шаблона без legacy API. После `T-0044` шаблон также фиксирует минимальную C# script model: обычный class file наследуется от `Node`, компилируется обычной .NET toolchain и получает lifecycle callbacks.

## Шаблон

Canonical template:

```text
data/templates/electron2d-empty/
```

Шаблон должен содержать:

- `.template.config/template.json`
- `Electron2D.Empty.csproj`
- `Program.cs`
- `Scripts/MainScene.cs`
- `project.e2d.json`
- `scenes/main.scene.json`
- `README.md`

## Минимальный формат проекта

`project.e2d.json` должен быть валидным project settings документом `Electron2D.ProjectSettings` и указывать:

- `format`
- `formatVersion`
- `name`
- `version`
- `engineVersion`
- `mainScene`
- `rendererProfile`
- `physicsTicksPerSecond`
- `input.actions`
- `display`

`scenes/main.scene.json` должен описывать пустую сцену без legacy component-полей.

## Верификация

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ProjectTemplate.ps1
```

Verifier должен:

1. собрать локальный package `Electron2D.0.1.0-preview`;
2. создать временный проект из шаблона;
3. восстановить зависимости из локального package source;
4. собрать проект;
5. запустить проект и подтвердить, что пустая сцена найдена;
6. подтвердить, что C# script sample получил `_EnterTree()`/`_Ready()`;
7. подтвердить, что script sample увидел `GetTree()` и `RenderingServer`.

## Editor run override

Шаблон должен понимать переменную процесса `ELECTRON2D_CURRENT_SCENE`. Если она задана, проект запускает указанный scene file как относительный путь внутри проекта и не меняет `project.e2d.json`.
