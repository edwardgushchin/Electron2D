# Формат проекта и шаблон `electron2d-empty`

Статус: целевая спецификация.
Задача: `T-0006`.
Обновлено: 2026-06-20.

## Цель

Новый проект Electron2D должен начинаться из минимального шаблона без legacy API. На текущем clean baseline шаблон проверяет структуру проекта, package reference и запуск пустой сцены-манифеста; он не должен требовать ещё не реализованные runtime-типы вроде `Node`.

## Шаблон

Canonical template:

```text
templates/electron2d-empty/
```

Шаблон должен содержать:

- `.template.config/template.json`
- `Electron2D.Empty.csproj`
- `Program.cs`
- `project.e2d.json`
- `scenes/main.scene.json`
- `README.md`

## Минимальный формат проекта

`project.e2d.json` должен указывать:

- `name`
- `version`
- `engineVersion`
- `mainScene`

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
5. запустить проект и подтвердить, что пустая сцена найдена.
