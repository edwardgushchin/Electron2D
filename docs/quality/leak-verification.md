# Leak verification для `0.1-preview`

Обновлено: 2026-06-30.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0103`.
Обновлено: 2026-06-30.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md); [Performance budgets и soak-критерии 0.1-preview](../release-management/performance-budgets.md); [Performance verification для 0.1-preview](performance-verification.md).

## Цель

`0.1-preview` должен иметь короткую автоматическую проверку утечек для подсистем, где уже есть счётчики жизненного цикла: реестр текстур и целей рендеринга, голоса звуковой подсистемы, идентификаторы физики (`RID`) и циклы загрузки/выгрузки сцен.

Утечка здесь означает ресурс, который остаётся активным после завершения сценария освобождения: дескриптор текстуры или цели рендеринга, голос звуковой подсистемы, физический `RID` или экземпляр сцены. Рост управляемой памяти означает рост памяти .NET между начальным и финальным замером после принудительной сборки мусора. Рост внутренних ресурсов в этой задаче означает рост счётчиков дескрипторов или `RID`, потому что полноценное профилирование памяти на уровне ОС относится к 30-минутной длительной проверке и выпускному контролю кандидата релиза.

## Команда проверки

В репозитории должна существовать проверка в C#-инструменте сборки:

```text
dotnet run --project eng/Electron2D.Build -- verify leak-checks
```

Проверка обязана:

1. запускать точечный автоматический тест `LeakVerificationTests`;
2. читать отслеживаемый артефакт `data/quality/leak-verification-report.json`;
3. проверять, что артефакт содержит все обязательные сценарии и файлы доказательств;
4. завершаться ошибкой, если какой-либо сценарий показывает `activeResourceCount != 0`, `nativeHandleDelta != 0`, `monotonicGrowthDetected = true` или превышает managed growth budget;
5. создавать scratch-output только внутри `.temp/leak-verification/`.

## Обязательные сценарии

Artifact `data/quality/leak-verification-report.json` должен содержать четыре сценария:

| Scenario id | Проверяемый жизненный цикл |
| --- | --- |
| `gpu-texture-render-target-cycles` | Загрузка, перезагрузка и освобождение текстуры, а также создание и освобождение внеэкранной цели рендеринга через внутренние счётчики реестра. |
| `audio-voice-cycles` | Запуск, остановка и освобождение голоса через счётчики управляемой звуковой подсистемы. |
| `physics-rid-cycles` | Создание и освобождение физических пространств, областей, тел, соединений и форм через счётчики управляемой физической подсистемы. |
| `scene-load-unload-cycles` | Повторная загрузка, переключение и выгрузка `PackedScene` через `SceneTree.ChangeSceneToPacked()` с инвалидированием предыдущих экземпляров сцены. |

Для каждого сценария обязательны поля:

- `scenarioId`;
- `subsystem`;
- `iterations`;
- `managedGrowthBytes`;
- `nativeHandleDelta`;
- `activeResourceCount`;
- `monotonicGrowthDetected`;
- `evidence`.

## Бюджеты

Проверка обязана применять такие бюджеты:

| Metric | Budget |
| --- | ---: |
| `iterations` | `>= 64` |
| `managedGrowthBytes` | `<= 1048576` |
| `nativeHandleDelta` | `0` |
| `activeResourceCount` | `0` |
| `monotonicGrowthDetected` | `false` |

Бюджет управляемой памяти допускает небольшой шум сборщика мусора и служебных данных JIT/среды выполнения. Постоянный рост объектов подсистем должен проявляться через ненулевые активные счётчики или флаг монотонного роста и обязан проваливать проверку.

## Automated test contract

Focused test `LeakVerificationTests` должен:

- реально выполнить короткие cycles для texture/render-target registry и подтвердить `LeakCount = 0`;
- реально выполнить audio voice cycles и подтвердить отсутствие active voices в backend после release;
- реально выполнить physics RID cycles и подтвердить отсутствие active bodies/areas после release;
- реально выполнить repeated scene switch/unload cycles и подтвердить, что текущая сцена не накапливает children, а предыдущие instances становятся invalid;
- проверить документ, проверку и контракт отслеживаемого артефакта.

## Критерии приёмки

- Спецификация, implementation documentation и tracked artifact описывают один и тот же набор сценариев, бюджетов и команд.
- Точечный автоматический тест падает до появления проверки и отслеживаемого артефакта и проходит после реализации.
- `dotnet run --project eng/Electron2D.Build -- verify leak-checks` запускает focused test и проверяет `data/quality/leak-verification-report.json`.
- Artifact содержит сценарии для GPU texture/render-target, audio voices, physics RIDs и scene load/unload cycles.
- Документация в `docs/quality/` описывает, как запускать проверку, как читать отчёт и какие проверки остаются за 30-минутной длительной проверкой и выпускным контролем кандидата релиза.

## Фактическое состояние, ограничения и проверки

Статус: текущая проверка качества.
Задача: `T-0103`.
Обновлено: 2026-06-30.
Спецификация: [Leak verification для 0.1-preview](leak-verification.md).

## Команда

Текущая проверка запускается так:

```text
dotnet run --project eng/Electron2D.Build -- verify leak-checks
```

Команда выполняет focused test:

```text
LeakVerificationTests.LeakVerificationCyclesReleaseSubsystemResourcesAndDoNotGrowMonotonically
```

После теста проверка читает отслеживаемый отчёт `data/quality/leak-verification-report.json` и проверяет бюджеты, пути доказательств и итоговые счётчики.

## Что проверяется

Текущий gate содержит четыре сценария:

- `gpu-texture-render-target-cycles` - 64 цикла upload/reload/release texture и create/release offscreen render target через `TextureResourceRegistry`; ожидаемый итог `ActiveTextureCount = 0` и `LeakCount = 0`.
- `audio-voice-cycles` - 64 цикла play/stop/release voice через managed audio backend; ожидаемый итог `ActiveVoiceCount = 0`.
- `physics-rid-cycles` - 64 цикла create/free space, area, body, joint и shape RIDs через managed physics backend; ожидаемый итог `ActiveObjects = 0`.
- `scene-load-unload-cycles` - 64 цикла `PackedScene` switch через `SceneTree.ChangeSceneToPacked()`; ожидаемый итог: root не накапливает children, предыдущие scene instances invalidated, final scene вручную выгружена.

## Бюджеты

`data/quality/leak-verification-report.json` задаёт текущие бюджеты:

- `minimumIterations = 64`;
- `maxManagedGrowthBytes = 1048576`;
- `maxNativeHandleDelta = 0`;
- `maxActiveResourceCount = 0`;
- `allowMonotonicGrowth = false`.

`nativeHandleDelta` в этой проверке означает изменение внутренних счётчиков: дескрипторов текстур, голосов звуковой подсистемы, физических `RID` или экземпляров сцен. Полное профилирование памяти на уровне ОС остаётся частью 30-минутной длительной проверки и выпускного контроля кандидата релиза.

## Артефакты

Durable tracked artifact:

- `data/quality/leak-verification-report.json`.

Scratch-output создаётся только в `.temp/leak-verification/` и не входит в commit.

## Что не входит

Эта проверка не подтверждает 30-минутную длительную проверку, запуск на реальном устройстве и общий выпускной контроль кандидата релиза. Она закрывает короткую воспроизводимую проверку утечек для текущих счётчиков. Длительные проверки роста памяти и платформенное профилирование внутренних ресурсов остаются в `T-0093`, `T-0104` и связанных платформенных задачах.
