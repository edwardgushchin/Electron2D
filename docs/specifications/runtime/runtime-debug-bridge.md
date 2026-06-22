# Runtime debug bridge и scene inspection

Статус: целевая спецификация для `T-0124`.
Обновлено: 2026-06-22.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [AI-friendly workflow Electron2D 0.1](../architecture/ai-friendly-workflow.md), [Headless runtime automation](headless-runtime-automation.md), [Diagnostics adapters: JSON, stream и SARIF](../diagnostics/diagnostics-adapters.md).

## Назначение

Runtime debug bridge - это общий внутренний контракт для наблюдения за запущенной сценой. Его должны использовать будущий Editor `RuntimeController`, MCP tools и CLI/headless adapters, чтобы читать одно и то же runtime-состояние без GUI automation и без прямого доступа к process internals.

`T-0124` фиксирует минимальный bridge contract и детерминированную preview-реализацию. Она может читать сцену из стабильного scene JSON и не обязана запускать настоящий отдельный game process: видимый Editor-attached process подключается задачей `T-0144` поверх того же контракта. При этом bridge уже должен различать headless preview session и editor-attached preview session, чтобы future UI/MCP adapters не меняли payload.

## Session model

Runtime debug session содержит:

- `sessionId`;
- `sessionKind`: `HeadlessPreview` или `EditorAttachedPreview`;
- `projectRoot`;
- `scene`;
- `state`: `Running`, `Paused` или `Stopped`;
- `currentFrame`;
- `currentPhysicsFrame`;
- `inputActions`;
- `diagnostics`;
- `metrics`;
- `sceneTree`.

Bridge доступен только в development/debug mode. Production mode должен fail closed с structured diagnostic `E2D-RUNTIME-0001`.

## Управление runtime

Минимальные команды:

- `pause` переводит session в `Paused`;
- `resume` переводит session в `Running`;
- `step frame` увеличивает `currentFrame`, накапливает simulated seconds и не требует wall-clock ожидания;
- `step physics` увеличивает `currentPhysicsFrame`;
- `inject input` меняет action state на `pressed` или `released`;
- `capture screenshot` возвращает stable PNG bytes и metadata;
- `stop` переводит session в `Stopped`.

`step frame` и `step physics` должны работать while paused, чтобы Editor и AI могли проверять сцену покадрово. Детерминированные headless tests не должны ломаться от появления bridge: одинаковые inputs дают одинаковые snapshots и screenshot bytes.

## Scene tree и inspect

Bridge строит Remote Scene Tree snapshot:

- `path` вида `/Root/Player`;
- `id`;
- `type`;
- `name`;
- `parentPath`;
- `properties`;
- `visible` для будущих visual adapters, если значение известно.

`inspect node` возвращает один node snapshot по `path`. Отсутствующий node возвращает `E2D-RUNTIME-0001` с `DiagnosticLocation.NodePath`.

`metrics` содержит текущие frame counters, simulated seconds, last frame delta, last physics delta, fps и session state.

## Runtime mutation policy

В `0.1.0 Preview` runtime debug bridge является наблюдающим и управляющим execution state слоем. Он не меняет serialized project documents и не изменяет arbitrary runtime node properties. Попытка mutation через bridge должна возвращать failed result с `E2D-RUNTIME-0001`.

Будущие безопасные runtime actions, если они появятся, должны быть отдельными явно описанными командами и не должны обходить project transaction layer.

## CLI contract

CLI получает preview route:

```powershell
e2d run debug --project <path> --scene scenes/main.scene.json --session-kind editor --step-frames 2 --step-physics 1 --fixed-delta 0.0166667 --physics-delta 0.0166667 --input-action jump=pressed --inspect-node /Root/Player --screenshot artifacts/debug/frame.png --format json
```

JSON output использует общий CLI envelope:

- `command = "run debug"`;
- `route = headless` до появления live Editor runtime adapter;
- `data.mode = "runtime.debugBridge"`;
- `data.session`;
- `data.sceneTree`;
- `data.inspectedNode`, если запрошен;
- `data.metrics`;
- `data.screenshot`, если запрошен.

Если `--screenshot` относительный, path должен оставаться внутри project root. Absolute path разрешён только как явно переданный output path.

## Acceptance criteria

- Спецификация runtime debug bridge существует отдельно от headless runtime automation.
- Bridge доступен только в development/debug mode и fail-closed возвращает `E2D-RUNTIME-0001`.
- Bridge открывает scene JSON, строит Remote Scene Tree и inspect node по path.
- Bridge поддерживает `pause`, `resume`, `step frame`, `step physics`, `inject input`, `capture screenshot`, `stop`.
- CLI route `e2d run debug --format json` возвращает session, scene tree, metrics, diagnostics и optional screenshot metadata через общий CLI envelope.
- Runtime mutation через bridge явно запрещена и возвращает structured diagnostic.
- Детерминированность preview bridge покрыта focused tests без готового Editor UI, MCP transport или настоящего game process.
- Implementation documentation в `docs/documentation/runtime/` описывает фактический scope, ограничения и focused test command.
