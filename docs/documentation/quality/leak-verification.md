# Leak verification для `0.1.0 Preview`

Статус: текущая проверка качества.
Задача: `T-0103`.
Обновлено: 2026-06-23.
Спецификация: [Leak verification для 0.1.0 Preview](../../specifications/quality/leak-verification.md).

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
