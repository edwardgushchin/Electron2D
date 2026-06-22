# Runtime debug bridge и scene inspection

Статус: реализованная внутренняя основа.
Задача: `T-0124`.
Обновлено: 2026-06-22.
Связанные документы: [Runtime debug bridge и scene inspection](../../specifications/runtime/runtime-debug-bridge.md); [Headless runtime automation](headless-runtime-automation.md); [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md); [Diagnostics adapters: JSON, JSONL stream и SARIF](../diagnostics/diagnostics-adapters.md).

## Назначение

`RuntimeDebugBridge` реализован в `Electron2D.ProjectSystem` как общий внутренний контракт для чтения runtime-состояния сцены. Его могут использовать CLI, MCP и будущий Editor `RuntimeController`, не создавая отдельные payloads для scene tree, metrics, diagnostics или screenshots.

Текущая реализация является deterministic preview layer: она читает стабильный scene JSON и не запускает настоящий game process. Видимый Editor-attached запуск подключается отдельной задачей поверх этого же контракта.

## Session model

`RuntimeDebugStartRequest` задаёт:

- `ProjectRoot`;
- `ScenePath`;
- `SessionKind`: `HeadlessPreview` или `EditorAttachedPreview`;
- `DevelopmentMode`;
- `BuildConfigurationHash`.

`RuntimeDebugBridge.Start(...)` возвращает `RuntimeDebugStartResult`. В production mode (`DevelopmentMode = false`) bridge не создаёт session и возвращает structured diagnostic `E2D-RUNTIME-0001`.

`RuntimeDebugSession` хранит:

- `SessionId`;
- `Scene`;
- `SessionKind`;
- `State`;
- `CurrentFrame`;
- `CurrentPhysicsFrame`;
- `InputActions`;
- `BuildConfigurationHash`.

## Runtime actions

Поддержаны:

- `Pause()`;
- `Resume()`;
- `Stop()`;
- `StepFrame(count, fixedDelta)`;
- `StepPhysics(count, fixedDelta)`;
- `InjectInput(action, pressed)`;
- `CaptureScreenshot()`;
- `GetSceneTree()`;
- `InspectNode(path)`;
- `GetMetrics()`.

`StepFrame(...)` и `StepPhysics(...)` работают while paused и не ждут wall-clock time. `CaptureScreenshot()` возвращает stable PNG bytes и SHA-256 hash; сейчас это deterministic placeholder frame, пригодный для проверки route/artifact contract до подключения настоящего renderer output.

## Remote Scene Tree

Bridge строит `RuntimeDebugSceneTreeSnapshot` из scene JSON:

- `Scene`;
- `CurrentFrame`;
- `CurrentPhysicsFrame`;
- `Nodes`.

Каждый `RuntimeDebugNodeSnapshot` содержит:

- `Id`;
- `Type`;
- `Name`;
- `Path` вида `/Root/Player`;
- `ParentPath`;
- `Properties`;
- `Visible`, если property известна.

`InspectNode(path)` возвращает node snapshot. Если path не найден, команда возвращает `E2D-RUNTIME-0001` с `DiagnosticLocation.NodePath`.

## Runtime mutation

Произвольное изменение runtime node properties в текущем Preview запрещено. `TrySetNodeProperty(...)` всегда возвращает failed result с `E2D-RUNTIME-0001` и не меняет snapshot.

Это важно для будущего Editor/MCP workflow: project data меняется через transaction layer, а runtime debug bridge только управляет execution state и читает наблюдаемое состояние.

## CLI

Preview route:

```powershell
e2d run debug `
  --project <project-root> `
  --scene scenes/main.scene.json `
  --session-kind editor `
  --step-frames 2 `
  --step-physics 1 `
  --fixed-delta 0.0166667 `
  --physics-delta 0.0166667 `
  --input-action jump=pressed `
  --inspect-node /Root/Player `
  --screenshot artifacts/debug/frame.png `
  --format json
```

JSON output uses the common CLI envelope:

- `command = "run debug"`;
- `route = "headless"` until the visible Editor runtime adapter is connected;
- `data.mode = "runtime.debugBridge"`;
- `data.session`;
- `data.sceneTree`;
- `data.inspectedNode`;
- `data.metrics`;
- `data.diagnostics`;
- `data.screenshot`.

If `--screenshot` is relative, it must stay inside project root. Absolute output path is allowed only as an explicit path supplied by the caller.

## Текущие ограничения

- Нет настоящего отдельного game process.
- Нет embedded viewport или видимого окна редактора; это scope будущего `T-0144`.
- Screenshot сейчас deterministic placeholder PNG.
- Physics, animation, script execution, renderer output и live process diagnostics ещё не выполняются.
- MCP runtime tools пока имеют manifest names; их production semantics должны подключиться к этому bridge в отдельной задаче.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~RuntimeDebugBridgeTests
```

Проверка покрывает Remote Scene Tree, node inspect, pause/step/input/screenshot/metrics, production-mode fail-closed behavior, runtime mutation rejection и CLI route `e2d run debug --format json`.
