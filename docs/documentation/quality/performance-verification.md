# Performance verification для `0.1.0 Preview`

Статус: текущая проверка качества.
Задача: `T-0102`.
Обновлено: 2026-06-23.
Спецификация: [Performance verification для 0.1.0 Preview](../../specifications/quality/performance-verification.md).

## Что проверяет команда

Текущий локальный verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ReferencePerformance.ps1
```

Команда выполняет три группы проверок:

1. запускает `tools\Verify-ReferencePlatformer.ps1` и `tools\Verify-UiHeavyReference.ps1`, чтобы подтвердить, что performance metrics для reference games собираются только после проверки валидных проектов `Electron2D.Editor`;
2. читает tracked artifact `data/quality/performance-reference-metrics.json`;
3. проверяет бюджеты 60 FPS, отсутствие steady managed allocations после прогрева, наличие documented device и batching evidence.

`steady managed allocations` означает повторяющиеся выделения управляемой памяти .NET на каждом кадре после прогрева. Значение `0 B/frame` в artifact является обязательным для всех четырёх сценариев `T-0102`.

## Сценарии

Tracked artifact содержит четыре сценария:

- `empty-scene` - минимальная сцена из `data/quality/reference-performance/empty-scene.scene.json`;
- `sprite-scene` - типовая sprite-сцена из `data/quality/reference-performance/sprite-scene.scene.json`;
- `reference-platformer` - проект `examples/reference-platformer`;
- `ui-heavy-reference` - проект `examples/ui-heavy-reference`.

Для каждого сценария artifact фиксирует:

- `warmupFrames >= 120`;
- `measuredFrames >= 600`;
- `targetFps = 60`;
- `p95FrameTimeMs <= 16.67`;
- `p99FrameTimeMs <= 25` для `empty-scene` и `<= 33` для остальных сценариев;
- `steadyManagedAllocatedBytesPerFrame = 0`;
- evidence paths, которые должны существовать в репозитории.

## Batching

`drawCallBatching` в `data/quality/performance-reference-metrics.json` проверяет `sprite-scene`.

Текущий baseline:

- `commandCount = 6`;
- `drawCallCount = 3`;
- `reductionRatio = 2.0`;
- минимально допустимый `reductionRatio = 1.5`.

Verifier падает, если количество draw calls не меньше количества команд или если ratio ниже порога. Evidence ссылается на `CanvasItemRenderQueueTests`, где проверяется построение batched render plan.

## Артефакты

Durable tracked artifacts:

- `data/quality/performance-reference-metrics.json`;
- `data/quality/reference-performance/empty-scene.scene.json`;
- `data/quality/reference-performance/sprite-scene.scene.json`.

Scratch-output создаётся только в `.temp/reference-performance/` и не входит в commit.

## Что не входит

Эта проверка не является 30-минутным soak test и не подтверждает полный platform release gate. Длительные прогоны, background/foreground cycles, реальные device/simulator smoke checks и memory-growth checks остаются задачами `T-0093`, `T-0096`, `T-0103` и `T-0104`.
