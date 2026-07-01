# Тестовая инфраструктура `0.1.0 Preview`

Обновлено: 2026-06-30.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0002`, дополнение `T-0215`.
Обновлено: 2026-06-30.

## Цель

После сброса к чистому baseline Electron2D должен иметь проверяемую тестовую инфраструктуру до появления новой реализации среды выполнения. Инфраструктура нужна для разработки с нуля и не должна подтягивать старый компонентный код.

## Состав

- `src/Electron2D/Electron2D.csproj` - новый пустой проект среды выполнения `Electron2D` без публичных типов.
- `tests/Electron2D.Tests.Unit/` - unit-тесты чистых контрактов и value-логики.
- `tests/Electron2D.Tests.Integration/` - интеграционные проверки структуры решения и подсистем.
- `tests/Electron2D.Tests.RuntimeSmoke/` - короткие проверки загрузки сборки среды выполнения и запуска будущей среды выполнения.
- `tests/Electron2D.Tests.GoldenData/` - golden-data проверки сериализации, импортов и стабильных артефактов.
- `dotnet run --project eng/Electron2D.Build -- test` - единая C#-команда проверки тестовых проектов; CI запускает её как `dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600`.

## Baseline-режим

Команда `test` должна сохранять параметр `--include-baseline`, чтобы будущие задачи могли явно запускать категорию `Category=Baseline`, если она понадобится для документированного красного состояния.

Обычная команда проверки исключает `Category=Baseline`, чтобы штатная проверка оставалась успешной. После реализации `Node` и `SceneTree` baseline в текущей категории нет обязательного намеренно падающего теста.

## Инварианты

- Тестовые проекты должны быть добавлены в `src/Electron2D.sln`.
- Основной solution на этом этапе не должен собирать старые examples; их миграция выполняется отдельной задачей.
- Сборка среды выполнения на старте не экспортирует публичных legacy-типов: `IComponent`, `SpriteRenderer`, `SpriteAnimator`, legacy physics components.
- Все новые тесты должны ссылаться на новый `src/Electron2D/Electron2D.csproj`, а не на восстановленные файлы старой среды выполнения.

## Команды проверки

Штатная проверка без `Category=Baseline`:

```bash
dotnet run --project eng/Electron2D.Build -- test
```

Baseline-режим, если будущая задача добавит tests с `Category=Baseline`:

```bash
dotnet run --project eng/Electron2D.Build -- test --include-baseline
```

Текущая среда выполнения уже содержит `Electron2D.Node`; намеренно падающий baseline больше не требуется для объектной модели.

## Фактическое состояние, ограничения и проверки

Статус: реализованная инфраструктура после сброса к чистому baseline с C#-запускателем тестов из `T-0215`.
Задача: `T-0002`, дополнение `T-0215`.
Обновлено: 2026-06-29.

## Что создано

- Новый пустой проект среды выполнения: `src/Electron2D/Electron2D.csproj`.
- Четыре тестовых проекта:
  - `tests/Electron2D.Tests.Unit/`
  - `tests/Electron2D.Tests.Integration/`
  - `tests/Electron2D.Tests.RuntimeSmoke/`
  - `tests/Electron2D.Tests.GoldenData/`
- Единая команда запуска: `dotnet run --project eng/Electron2D.Build -- test`.
- `src/Electron2D.sln` обновлён: тестовые проекты добавлены, старые examples исключены до отдельной миграции.
- Agent acceptance benchmark, то есть релизный контроль для проверки рабочего процесса агента:
  - `data/quality/agent-acceptance-benchmarks.json`;
  - `dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks`;
  - документация `docs/testing/agent-acceptance-benchmarks.md`.

## Как запускать

Обычная проверка инфраструктуры:

```bash
dotnet run --project eng/Electron2D.Build -- test
```

Проверка вместе с baseline-категорией, если в будущих задачах она будет добавлена:

```bash
dotnet run --project eng/Electron2D.Build -- test --include-baseline
```

В текущем состоянии baseline-категория не содержит намеренно падающих тестов: новая Electron2D объектная модель уже включает `Node` и `SceneTree` baseline.

Команда поддерживает `--timeout-seconds <n>`: это ограничение времени применяется к каждому дочернему `dotnet test`, а истечение времени возвращается отдельной структурированной диагностикой. Значение по умолчанию и явный лимит CI равны `3600` секундам на тестовый проект.

Agent-native релизный контроль можно проверить без запуска тяжёлых коротких проверок запуска:

```bash
dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks --dry-run --output .temp/agent-acceptance-benchmarks
```

Полный запуск benchmark выполняет evidence steps из manifest последовательно и создаёт `benchmark-result.json`.

## Текущее состояние среды выполнения

Сборка среды выполнения `Electron2D` существует, загружается и экспортирует текущий Electron2D baseline public API. Unit-тесты дополнительно проверяют, что в новый baseline не вернулись legacy-типы `IComponent`, `SpriteRenderer`, `SpriteAnimator`, `Rigidbody` и `Collider`.
