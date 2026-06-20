# Тестовая инфраструктура `0.1.0 Preview`

Статус: целевая спецификация.
Задача: `T-0002`.
Обновлено: 2026-06-20.

## Цель

После reset к чистому baseline Electron2D должен иметь проверяемую тестовую инфраструктуру до появления новой реализации runtime. Инфраструктура нужна для разработки с нуля и не должна подтягивать старый Unity-like/component код.

## Состав

- `src/Electron2D/Electron2D.csproj` - новый пустой runtime-проект `Electron2D` без публичных типов.
- `tests/Electron2D.Tests.Unit/` - unit-тесты чистых контрактов и value-логики.
- `tests/Electron2D.Tests.Integration/` - интеграционные проверки структуры решения и подсистем.
- `tests/Electron2D.Tests.RuntimeSmoke/` - smoke-проверки загрузки runtime assembly и запуска будущего runtime.
- `tests/Electron2D.Tests.GoldenData/` - golden-data проверки сериализации, импортов и стабильных артефактов.
- `tools/Run-Tests.ps1` - единая команда проверки тестовых проектов.

## Baseline-режим

`Run-Tests.ps1` должен сохранять параметр `-IncludeBaseline`, чтобы будущие задачи могли явно запускать baseline-категорию, если она понадобится для документированного red-state.

Обычная команда проверки исключает `Category=Baseline`, чтобы инфраструктура оставалась green-check. После реализации `Node` и `SceneTree` baseline в текущей категории нет обязательного намеренно падающего теста.

## Инварианты

- Тестовые проекты должны быть добавлены в `src/Electron2D.sln`.
- Основной solution на этом этапе не должен собирать старые examples; их миграция выполняется отдельной задачей.
- Runtime assembly на старте не экспортирует публичных legacy-типов: `IComponent`, `SpriteRenderer`, `SpriteAnimator`, legacy physics components.
- Все новые тесты должны ссылаться на новый `src/Electron2D/Electron2D.csproj`, а не на восстановленные файлы старого runtime.

## Команды проверки

Green-check без baseline:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1
```

Baseline-режим, если будущая задача добавит tests с `Category=Baseline`:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1 -IncludeBaseline
```

Текущий runtime уже содержит `Electron2D.Node`; намеренно падающий baseline больше не требуется для объектной модели.
