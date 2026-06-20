# Runtime diagnostics пользовательского кода

Статус: целевая спецификация.
Задача: `T-0016`.
Обновлено: 2026-06-20.

## Цель

Централизовать обработку исключений из пользовательского кода, чтобы lifecycle, group calls, deferred calls и signal callbacks создавали единую внутреннюю запись диагностики и не обрывали текущий проход дерева.

## Публичный API

Новый публичный API не добавляется. Диагностика остаётся внутренним механизмом: её используют автоматические тесты, а позже будет использовать вывод редактора и будущая публичная модель диагностики.

Внутренняя запись диагностики должна содержать:

- node context, если он известен;
- callback/method/signal context;
- failure kind;
- исходное exception;
- message;
- stack trace.

## Правило восстановления после ошибки

- Исключение из lifecycle callback сохраняется в diagnostics, после чего обход дерева продолжается.
- `SceneTree.CallGroup()` сохраняет exception в diagnostics и продолжает вызывать следующие nodes.
- Исключение из deferred callable сохраняется в diagnostics, после чего deferred queue разбирается до пустого состояния.
- Исключение из signal callback сохраняется в diagnostics, `EmitSignal()` возвращает `Error.Failed` и продолжает вызов остальных callbacks.
- Несовпадение сигнатуры без исходного user exception возвращает `Error.Failed`, но не обязано создавать diagnostic со stack trace.

## Ограничения текущего baseline

- Диагностический API остаётся внутренним, потому что пользовательское окно вывода редактора ещё не реализовано.
- Режим немедленной остановки при ошибке, уровни важности сообщений, местоположение ошибки в исходном коде и структурированный вывод в редакторе будут выделены отдельными задачами.

## Acceptance tests

- Lifecycle exception diagnostic содержит node, callback, kind, message и stack trace, traversal siblings продолжается.
- Deferred call exception diagnostic содержит node, callback, kind, message и stack trace, queue продолжает drain.
- Signal callback exception diagnostic содержит node context, signal callback context, kind, message и stack trace, emission продолжает callbacks и возвращает `Error.Failed`.
