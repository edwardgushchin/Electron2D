# Runtime diagnostics пользовательского кода

Статус: реализованный internal baseline.
Задача: `T-0016`.
Обновлено: 2026-06-20.

## Public API

Новый public API не добавлен. Diagnostics остаётся internal surface для test host и будущего editor/output UI.

Это важно для чистого Godot-like публичного API: временные diagnostic helper-типы не экспортируются из runtime assembly и не становятся compatibility burden для `0.1.0`.

## Diagnostic record

Internal `SceneTreeDiagnostic` содержит:

- `Node` - node context, если он известен;
- `Callback` - lifecycle callback, group method, deferred method или signal name;
- `Kind` - источник ошибки;
- `Exception` - исходное exception;
- `Message` - `Exception.Message`;
- `StackTrace` - stack trace исходного exception или пустая строка.

Internal `RuntimeUserCodeFailureKind` различает:

- `LifecycleCallback`;
- `GroupCall`;
- `DeferredCall`;
- `SignalEmission`.

## Recover policy

`SceneTree` не даёт user-code exception оборвать текущий runtime pass:

- lifecycle callbacks пишут diagnostic и продолжают traversal siblings;
- `CallGroup()` пишет diagnostic и продолжает вызовы следующих group nodes;
- deferred calls пишут diagnostic и продолжают drain очереди;
- signal callbacks пишут diagnostic, `EmitSignal()` возвращает `Error.Failed` и продолжает emission остальных callbacks.

Signature mismatch без исходного user exception остаётся `Error.Failed`; stack trace diagnostic для такого случая не обязателен.

## Deferred queue isolation

Deferred queue привязана к конкретному `SceneTree`. Вызовы, поставленные из lifecycle/process/physics/input traversal или из deferred callback, попадают в queue текущего tree. Это предотвращает перемешивание deferred calls между несколькими `SceneTree` в тестах и будущих host contexts.

## Ограничения текущего baseline

- Public diagnostics API, severity levels и editor output panel ещё не реализованы.
- Source location для script/code diagnostics появится после scripting/editor задач.
