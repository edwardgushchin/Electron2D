# Тестовая инфраструктура `0.1-preview`

Обновлено: 2026-07-01.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0002`, дополнения `T-0215`, `T-0236`.
Обновлено: 2026-07-01.

## Цель

После сброса к чистому baseline Electron2D должен иметь проверяемую тестовую инфраструктуру до появления новой реализации среды выполнения. Инфраструктура нужна для разработки с нуля и не должна подтягивать старый компонентный код.

## Состав

- `src/Electron2D/Electron2D.csproj` - новый пустой проект среды выполнения `Electron2D` без публичных типов.
- `tests/Electron2D.Tests.Unit/` - unit-тесты чистых контрактов и value-логики.
- `tests/Electron2D.Tests.Integration/` - интеграционные проверки структуры решения и подсистем.
- `tests/Electron2D.Tests.RuntimeSmoke/` - короткие проверки загрузки сборки среды выполнения и запуска будущей среды выполнения.
- `tests/Electron2D.Tests.GoldenData/` - golden-data проверки сериализации, импортов и стабильных артефактов.
- `dotnet run --project eng/Electron2D.Build -- test` - единая C#-команда проверки тестовых проектов.
- CI после `dotnet restore src/Electron2D.sln` должен выполнить одну сборку решения `dotnet build src/Electron2D.sln --no-restore`, затем запускать тесты как `dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600 --integration-slice fast --no-build --no-restore`, чтобы `Electron2D.Tests.Integration` не пересобирал проект и все ссылки на каждом тестовом проекте.
- Для обычного ежедневного цикла разработки и основного задания CI команда поддерживает `--integration-slice fast`: unit-тесты, короткие проверки запуска среды выполнения, golden-data проверки и быстрый срез `Electron2D.Tests.Integration` без проверок внутреннего инструмента репозитория, аудиторского пакета, внешних процессов, медленных тестов и двух проверок резервного представителя кадра, которым на GitHub runners нужен доступный видеодрайвер: `RuntimeHostTests.RuntimeSdlRendererFallbackThrowsForUnsupportedTextureResource` и `RuntimeHostTests.RuntimeSdlRendererFallbackThrowsForUnknownRenderCommandKind`.
- Тяжёлые срезы интеграционных тестов запускаются отдельными заданиями CI: `repository-tooling`, `audit-medium`, `audit-heavy`, `external-process`, `slow`. Они запускают только `tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj` с профильным фильтром. Полный исчерпывающий аудиторский срез `audit-exhaustive` остаётся ручным или ночным маршрутом для изменений самой упаковки и восстановления, чтобы обычный CI не повторял 120 тяжёлых сценариев упаковки после каждой правки.

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

Быстрая проверка после уже выполненных `restore` и `build`:

```bash
dotnet run --project eng/Electron2D.Build -- test --no-build --no-restore
```

Быстрый срез для обычной работы над задачей и основного задания CI:

```bash
dotnet run --project eng/Electron2D.Build -- test --integration-slice fast --no-build --no-restore
```

Тяжёлые срезы для CI, релизного контроля и доказательств аудита:

```bash
dotnet run --project eng/Electron2D.Build -- test --integration-slice repository-tooling --no-build --no-restore
dotnet run --project eng/Electron2D.Build -- test --integration-slice audit-medium --no-build --no-restore
dotnet run --project eng/Electron2D.Build -- test --integration-slice audit-heavy --no-build --no-restore
dotnet run --project eng/Electron2D.Build -- test --integration-slice external-process --no-build --no-restore
dotnet run --project eng/Electron2D.Build -- test --integration-slice slow --no-build --no-restore
```

Baseline-режим, если будущая задача добавит tests с `Category=Baseline`:

```bash
dotnet run --project eng/Electron2D.Build -- test --include-baseline
```

Текущая среда выполнения уже содержит `Electron2D.Node`; намеренно падающий baseline больше не требуется для объектной модели.

## Фактическое состояние, ограничения и проверки

Статус: реализованная инфраструктура после сброса к чистому baseline с C#-запускателем тестов из `T-0215`; оптимизация повторных сборок для CI из `T-0236` реализована.
Задача: `T-0002`, дополнения `T-0215`, `T-0236`.
Обновлено: 2026-07-01.

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

Команда также поддерживает `--no-build` и `--no-restore`. Эти параметры передаются каждому дочернему `dotnet test`. CI пользуется ими после явных шагов `restore` и `build`; локально их нужно применять, когда разработчик уже собрал решение и хочет повторить тесты без повторной сборки.

Параметр `--integration-slice <name>` управляет только проектом `tests/Electron2D.Tests.Integration`:

- `all` - значение по умолчанию; запускает полный integration project за вычетом `Category=Baseline`;
- `fast` - исключает тяжёлые срезы и остаётся обычным быстрым контуром;
- `repository-tooling` - проверки внутреннего инструмента репозитория без аудиторского пакета;
- `audit-medium` - средний аудиторский коридор: поведенческие проверки `audit submit`, verifier-ы и контролируемые дочерние процессы без полной упаковки и восстановления ZIP;
- `audit-heavy` - короткий приёмочный аудиторский коридор: представительские `AuditTier=Heavy` тесты с меткой `AuditCadence=Acceptance`, которые реально создают и проверяют audit ZIP;
- `audit-exhaustive` - полный `AuditTier=Heavy` набор для изменений самой упаковки, восстановления, ZIP-структуры, секретного сканирования или ручного глубокого разбора;
- `external-process` - тесты, которые запускают редактор, CLI или другие внешние процессы;
- `slow` - проверки устойчивости данных, утечек ресурсов и эталонных метрик производительности.

CI запускает `fast` в основном задании вместе с остальными проверками, а `repository-tooling`, `audit-medium`, `audit-heavy`, `external-process` и `slow` - отдельной матрицей заданий. Так тяжёлые интеграционные тесты перестают блокировать один длинный шаг `Run tests` и дают отдельный статус по срезу.

Agent-native релизный контроль можно проверить без запуска тяжёлых коротких проверок запуска:

```bash
dotnet run --project eng/Electron2D.Build -- verify agent-acceptance-benchmarks --dry-run --output .temp/agent-acceptance-benchmarks
```

Полный запуск benchmark выполняет evidence steps из manifest последовательно и создаёт `benchmark-result.json`.

## Текущее состояние среды выполнения

Сборка среды выполнения `Electron2D` существует, загружается и экспортирует текущий Electron2D baseline public API. Unit-тесты дополнительно проверяют, что в новый baseline не вернулись legacy-типы `IComponent`, `SpriteRenderer`, `SpriteAnimator`, `Rigidbody` и `Collider`.
