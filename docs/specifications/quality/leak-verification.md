# Leak verification для `0.1.0 Preview`

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
- Документация в `docs/documentation/quality/` описывает, как запускать verifier, как читать report и какие проверки остаются за 30-minute soak/release candidate gate.
