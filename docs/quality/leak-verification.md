# Leak verification для `0.1.0 Preview`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0103`.
Обновлено: 2026-06-23.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Performance budgets и soak-критерии 0.1.0 Preview](../release-management/performance-budgets.md); [Performance verification для 0.1.0 Preview](performance-verification.md).

## Цель

`0.1.0 Preview` должен иметь короткую автоматическую leak verification проверку для подсистем, где уже есть lifecycle counters: rendering texture/render-target registry, audio voices, physics RIDs и scene load/unload cycles.

`leak` здесь означает ресурс, который остаётся активным после завершения release-cycle сценария: texture/render-target handle, audio voice, physics RID или scene instance. `managed growth` означает рост управляемой памяти .NET между baseline и final sample после forced collection. `native growth` в этой задаче означает рост внутренних счётчиков handles/RIDs, потому что полноценный OS-level native memory profiler относится к 30-minute soak и release candidate gate.

## Команда проверки

В репозитории должен существовать verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LeakChecks.ps1
```

Verifier обязан:

1. запускать focused automated test `LeakVerificationTests`;
2. читать tracked artifact `data/quality/leak-verification-report.json`;
3. проверять, что artifact содержит все обязательные сценарии и evidence-файлы;
4. завершаться ошибкой, если какой-либо сценарий показывает `activeResourceCount != 0`, `nativeHandleDelta != 0`, `monotonicGrowthDetected = true` или превышает managed growth budget;
5. создавать scratch-output только внутри `.temp/leak-verification/`.

## Обязательные сценарии

Artifact `data/quality/leak-verification-report.json` должен содержать четыре сценария:

| Scenario id | Проверяемый lifecycle |
| --- | --- |
| `gpu-texture-render-target-cycles` | Upload/reload/release texture и create/release offscreen render target через internal registry counters. |
| `audio-voice-cycles` | Play/stop/release voice lifecycle через managed audio backend counters. |
| `physics-rid-cycles` | Create/free physics space, area, body, joint и shape RIDs через managed physics backend counters. |
| `scene-load-unload-cycles` | Repeated `PackedScene` load/switch/unload path через `SceneTree.ChangeSceneToPacked()` и invalidation of previous scene instances. |

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

Verifier обязан применять такие бюджеты:

| Metric | Budget |
| --- | ---: |
| `iterations` | `>= 64` |
| `managedGrowthBytes` | `<= 1048576` |
| `nativeHandleDelta` | `0` |
| `activeResourceCount` | `0` |
| `monotonicGrowthDetected` | `false` |

Managed memory budget допускает небольшой шум сборщика мусора и JIT/runtime metadata. Постоянный рост объектов подсистем должен проявляться через non-zero active counters или monotonic growth flag и обязан проваливать проверку.

## Automated test contract

Focused test `LeakVerificationTests` должен:

- реально выполнить короткие cycles для texture/render-target registry и подтвердить `LeakCount = 0`;
- реально выполнить audio voice cycles и подтвердить отсутствие active voices в backend после release;
- реально выполнить physics RID cycles и подтвердить отсутствие active bodies/areas после release;
- реально выполнить repeated scene switch/unload cycles и подтвердить, что текущая сцена не накапливает children, а предыдущие instances становятся invalid;
- проверить spec, verifier и tracked artifact contract.

## Критерии приёмки

- Спецификация, implementation documentation и tracked artifact описывают один и тот же набор сценариев, бюджетов и команд.
- Focused automated test падает до появления verifier/artifact и проходит после реализации.
- `tools\Verify-LeakChecks.ps1` запускает focused test и проверяет `data/quality/leak-verification-report.json`.
- Artifact содержит сценарии для GPU texture/render-target, audio voices, physics RIDs и scene load/unload cycles.
- Документация в `docs/quality/` описывает, как запускать verifier, как читать report и какие проверки остаются за 30-minute soak/release candidate gate.

## Фактическое состояние, ограничения и проверки

Статус: текущая проверка качества.
Задача: `T-0103`.
Обновлено: 2026-06-23.
Спецификация: [Leak verification для 0.1.0 Preview](leak-verification.md).

## Команда

Текущий verifier запускается так:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LeakChecks.ps1
```

Команда выполняет focused test:

```text
LeakVerificationTests.LeakVerificationCyclesReleaseSubsystemResourcesAndDoNotGrowMonotonically
```

После теста verifier читает tracked report `data/quality/leak-verification-report.json` и проверяет бюджеты, evidence paths и итоговые counters.

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

`nativeHandleDelta` в этой проверке означает изменение внутренних counters: texture handles, audio voices, physics RIDs или scene instances. Полный OS-level native memory profile остаётся частью 30-minute soak/release candidate gate.

## Артефакты

Durable tracked artifact:

- `data/quality/leak-verification-report.json`.

Scratch-output создаётся только в `.temp/leak-verification/` и не входит в commit.

## Что не входит

Эта проверка не подтверждает 30-минутный soak, real device platform run и общий release candidate gate. Она закрывает короткий воспроизводимый leak gate для текущих counters. Длительные memory-growth checks и platform-specific native profiling остаются в `T-0093`, `T-0104` и связанных platform tasks.
