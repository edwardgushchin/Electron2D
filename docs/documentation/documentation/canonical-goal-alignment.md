# Canonical goal alignment audit

Статус: реализованный documentation gate.
Задача: `T-0152`.
Обновлено: 2026-06-23.

## Что проверяется

`tools\Verify-CanonicalGoalAlignment.ps1` проверяет, что tracked goal/architecture документы не возвращают устаревшее позиционирование как действующую цель проекта.

Проверка подтверждает:

- `docs/specifications/releases/0.1.0-preview.md` фиксирует native-plus-browser release contract: Windows, Linux, macOS, Android, iOS и WebAssembly browser;
- `docs/specifications/architecture/agent-native-workflow.md` фиксирует `Node2D` transform, отсутствие обязательного transform у базового `Node`, смысл `scene_attach_script` и native-plus-browser contract;
- `docs/specifications/architecture/engine-platform-stack.md` помечен как синхронизированный с release contract, описывает specialized node/resource model и не объясняет публичную архитектуру через конкретные backend libraries;
- root `GOAL.md` и `GOAL-0.1.0.md` не возвращаются как tracked canonical sources.

## Команда

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-CanonicalGoalAlignment.ps1
```

Эта команда также подключена к CI matrix. Если старый goal-текст снова появится в tracked документации как актуальный source of truth, проверка должна завершиться ошибкой.

## Текущие ограничения

Audit намеренно точечный: он защищает canonical goal/architecture documents, а не сканирует весь репозиторий на технические идентификаторы. Некоторые спецификации могут упоминать legacy symbols только как запрет или как тестовый список, который не возвращается в public API.
