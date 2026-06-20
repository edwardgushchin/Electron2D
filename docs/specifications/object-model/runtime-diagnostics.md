# Runtime diagnostics пользовательского кода

Статус: целевая спецификация.
Задача: `T-0016`.
Обновлено: 2026-06-20.

## Цель

Централизовать обработку исключений из пользовательского кода, чтобы lifecycle, group calls, deferred calls и signal callbacks давали единый internal diagnostic record и не обрывали текущий runtime pass.

## Public API

Новый public API не добавляется. Diagnostics остаётся internal surface для test host, будущего editor output и будущей публичной diagnostics модели.

Internal diagnostic record должен содержать:

- node context, если он известен;
- callback/method/signal context;
- failure kind;
- исходное exception;
- message;
- stack trace.

## Recover policy

- Lifecycle callback exception сохраняется в diagnostics и traversal продолжается.
- `SceneTree.CallGroup()` сохраняет exception в diagnostics и продолжает вызывать следующие nodes.
- Deferred callable exception сохраняется в diagnostics и deferred queue продолжает drain до пустого состояния.
- Signal callback exception сохраняется в diagnostics, `EmitSignal()` возвращает `Error.Failed` и продолжает emission остальных callbacks.
- Signature mismatch без исходного user exception возвращает `Error.Failed`, но не обязан создавать diagnostic с stack trace.

## Ограничения текущего baseline

- Diagnostics API остаётся internal, потому что пользовательский editor/output UI ещё не реализован.
- Fail-fast policy, severity levels, source locations и structured editor output будут выделены отдельными задачами.

## Acceptance tests

- Lifecycle exception diagnostic содержит node, callback, kind, message и stack trace, traversal siblings продолжается.
- Deferred call exception diagnostic содержит node, callback, kind, message и stack trace, queue продолжает drain.
- Signal callback exception diagnostic содержит node context, signal callback context, kind, message и stack trace, emission продолжает callbacks и возвращает `Error.Failed`.
