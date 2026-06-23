# Тестовая инфраструктура `0.1.0 Preview`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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

## Фактическое состояние, ограничения и проверки

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
- Agent acceptance benchmark release gate:
  - `data/quality/agent-acceptance-benchmarks.json`;
  - `tools/Run-AgentAcceptanceBenchmarks.ps1`;
  - документация `docs/testing/agent-acceptance-benchmarks.md`.

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

Agent-native release gate можно проверить без запуска тяжёлых smoke-команд:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-AgentAcceptanceBenchmarks.ps1 -DryRun -OutputDirectory .temp/agent-acceptance-benchmarks
```

Полный запуск benchmark выполняет evidence steps из manifest последовательно и создаёт `benchmark-result.json`.

## Текущее состояние runtime

Runtime assembly `Electron2D` существует, загружается и экспортирует текущий Electron2D baseline public API. Unit-тесты дополнительно проверяют, что в новый baseline не вернулись legacy-типы `IComponent`, `SpriteRenderer`, `SpriteAnimator`, `Rigidbody` и `Collider`.
