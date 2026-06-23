# Performance verification для `0.1.0 Preview`

Статус: целевая спецификация.
Задача: `T-0102`.
Обновлено: 2026-06-23.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Performance budgets и soak-критерии 0.1.0 Preview](../release-management/performance-budgets.md); [Reference platformer](../examples/reference-platformer.md); [UI-heavy reference game](../examples/ui-heavy-reference.md).

## Цель

`0.1.0 Preview` должен иметь локальную автоматическую проверку производительности для коротких, воспроизводимых сценариев. Проверка не заменяет 30-минутные soak checks из платформенного release gate, но закрывает базовый контракт `T-0102`: 60 FPS для reference games, отсутствие постоянных managed allocations каждый кадр после прогрева и доказательство, что batching уменьшает количество draw calls.

`managed allocations` здесь означает выделения управляемой памяти .NET, которые повторяются на каждом кадре после прогрева. `draw call` означает одну отправку сгруппированной команды отрисовки во внутренний план кадра; batching должен превращать несколько совместимых команд в меньшее число таких отправок без изменения порядка отрисовки.

## Команда проверки

В репозитории должен существовать verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ReferencePerformance.ps1
```

Verifier обязан:

1. проверить, что обе reference games остаются валидными проектами `Electron2D.Editor`, запустив `tools\Verify-ReferencePlatformer.ps1` и `tools\Verify-UiHeavyReference.ps1`;
2. проверить или обновить локальный scratch-output только внутри `.temp/reference-performance/`;
3. прочитать tracked artifact `data/quality/performance-reference-metrics.json`;
4. проверить, что artifact содержит все обязательные сценарии, устройства, бюджеты и фактические метрики;
5. завершиться ошибкой, если хотя бы один обязательный сценарий превышает бюджет, не имеет метрик или не содержит evidence-файлы.

## Обязательные сценарии

Artifact `data/quality/performance-reference-metrics.json` должен содержать четыре сценария:

| Scenario id | Назначение |
| --- | --- |
| `empty-scene` | Минимальная сцена без игровых объектов; проверяет baseline frame loop и отсутствие steady allocations после warm-up. |
| `sprite-scene` | Типовая сцена со спрайтами; проверяет frame budget, отсутствие steady allocations после warm-up и batching. |
| `reference-platformer` | Законченный platformer-проект из `examples/reference-platformer`; проверяется только после project validation. |
| `ui-heavy-reference` | Законченная UI-heavy game из `examples/ui-heavy-reference`; проверяется только после project validation. |

Для каждого сценария обязательны поля:

- `scenarioId`;
- `projectPath`;
- `scenePath`;
- `deviceId`;
- `warmupFrames`;
- `measuredFrames`;
- `targetFps`;
- `p95FrameTimeMs`;
- `p99FrameTimeMs`;
- `averageFrameTimeMs`;
- `steadyManagedAllocatedBytesPerFrame`;
- `evidence`.

`evidence` должен ссылаться на воспроизводимые локальные файлы или команды проверки. Для reference games evidence обязательно включает соответствующий verifier.

## Бюджеты

Verifier обязан применять такие бюджеты:

| Scenario id | p95 | p99 | steady allocations |
| --- | ---: | ---: | ---: |
| `empty-scene` | `<= 16.67 ms` | `<= 25 ms` | `0 B/frame` |
| `sprite-scene` | `<= 16.67 ms` | `<= 33 ms` | `0 B/frame` |
| `reference-platformer` | `<= 16.67 ms` | `<= 33 ms` | `0 B/frame` |
| `ui-heavy-reference` | `<= 16.67 ms` | `<= 33 ms` | `0 B/frame` |

Проверка использует короткий deterministic frame run: фиксированный шаг `1/60`, прогрев не меньше `120` кадров и измерение не меньше `600` кадров. Длительные 30-минутные проверки, background/foreground cycles и platform soak остаются отдельной задачей release gate.

## Batching evidence

Artifact должен содержать объект `drawCallBatching` с полями:

- `scenarioId`: `sprite-scene`;
- `commandCount`;
- `drawCallCount`;
- `reductionRatio`;
- `evidence`.

Verifier обязан проверить:

- `commandCount > drawCallCount`;
- `reductionRatio >= 1.5`;
- evidence ссылается на automated test или verifier, который строит план отрисовки и считает batches.

## Устройства

Artifact должен содержать список `devices`. Для локального verifier обязательна запись `local-windows-x64` или другая запись текущего хоста с:

- `deviceId`;
- `platform`;
- `cpuClass`;
- `memoryGb`;
- `graphicsClass`;
- `notes`.

Платформенные устройства из release matrix могут добавляться отдельными задачами. `T-0102` не считается 30-минутным платформенным soak и не подменяет `T-0093`.

## Критерии приёмки

- Спецификация, implementation documentation и tracked artifact описывают один и тот же набор сценариев, бюджетов и команд.
- Focused automated test падает до появления verifier/artifact и проходит после реализации.
- `tools\Verify-ReferencePerformance.ps1` запускает validators обеих reference games до проверки performance metrics.
- `tools\Verify-ReferencePerformance.ps1` проверяет `data/quality/performance-reference-metrics.json` и падает при превышении p95/p99, наличии steady allocations или отсутствии batching reduction.
- Документация в `docs/documentation/quality/` описывает, как запускать verifier, где читать metrics artifact и какие проверки не входят в `T-0102`.
