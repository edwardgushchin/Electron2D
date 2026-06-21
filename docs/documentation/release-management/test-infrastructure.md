# Тестовая инфраструктура `0.1.0 Preview`

Статус: реализованная инфраструктура после clean reset.
Задача: `T-0002`.
Обновлено: 2026-06-20.

## Что создано

- Новый пустой runtime-проект: `src/Electron2D/Electron2D.csproj`.
- Четыре тестовых проекта:
  - `tests/Electron2D.Tests.Unit/`
  - `tests/Electron2D.Tests.Integration/`
  - `tests/Electron2D.Tests.RuntimeSmoke/`
  - `tests/Electron2D.Tests.GoldenData/`
- Единая команда запуска: `tools/Run-Tests.ps1`.
- `src/Electron2D.sln` обновлён: тестовые проекты добавлены, старые examples исключены до отдельной миграции.

## Как запускать

Обычная проверка инфраструктуры:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1
```

Проверка вместе с baseline-категорией, если в будущих задачах она будет добавлена:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1 -IncludeBaseline
```

В текущем состоянии baseline-категория не содержит намеренно падающих тестов: новая Electron2D объектная модель уже включает `Node` и `SceneTree` baseline.

## Текущее состояние runtime

Runtime assembly `Electron2D` существует, загружается и экспортирует текущий Electron2D baseline public API. Unit-тесты дополнительно проверяют, что в новый baseline не вернулись legacy-типы `IComponent`, `SpriteRenderer`, `SpriteAnimator`, `Rigidbody` и `Collider`.
