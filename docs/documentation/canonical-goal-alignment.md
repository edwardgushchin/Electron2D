# Canonical goal alignment audit

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0152`.
Обновлено: 2026-06-23.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md); [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Архитектура и платформенный стек Electron2D](../architecture/engine-platform-stack.md).

## Назначение

Документы целей и архитектуры должны ссылаться на текущий canonical contract `0.1-preview`, а не возвращать старое component-first или four-platform позиционирование как актуальную цель проекта.

Audit должен защищать от регрессий в tracked documentation:

- release contract включает Windows, Linux, macOS, Android, iOS и WebAssembly browser;
- engine architecture описывает specialized node/resource model, а не прежнюю component model;
- `Node2D` владеет 2D transform, базовый `Node` не получает обязательный transform;
- `scene_attach_script` связывает serialized node с пользовательским C# node type, а не добавляет отдельный Script-компонент;
- old root goal files не возвращаются как tracked canonical sources;
- `engine-platform-stack.md` не объясняет публичное позиционирование через конкретные backend libraries.

## Проверка

Репозиторий должен содержать verifier:

```bash
dotnet run --project eng/Electron2D.Build -- verify canonical-goal-alignment
```

Verifier должен запускаться локально и в CI вместе с остальными documentation gates.

## Критерии приёмки

- `engine-platform-stack.md` синхронизирован с `0.1-preview` и явно помечает исторический source как неавторитетный при конфликте.
- Verifier падает на старом backend-specific или component-first positioning и проходит на актуальном canonical contract.
- CI matrix запускает verifier на всех desktop runners.
- Implementation documentation описывает фактическое поведение и команду проверки.

## Фактическое состояние, ограничения и проверки

Статус: реализованный documentation gate.
Задача: `T-0152`.
Обновлено: 2026-06-23.

## Что проверяется

`dotnet run --project eng/Electron2D.Build -- verify canonical-goal-alignment` проверяет, что tracked goal/architecture документы не возвращают устаревшее позиционирование как действующую цель проекта.

Проверка подтверждает:

- `docs/releases/0.1-preview.md` фиксирует native-plus-browser release contract: Windows, Linux, macOS, Android, iOS и WebAssembly browser;
- `docs/architecture/agent-native-workflow.md` фиксирует `Node2D` transform, отсутствие обязательного transform у базового `Node`, смысл `scene_attach_script` и native-plus-browser contract;
- `docs/architecture/engine-platform-stack.md` помечен как синхронизированный с release contract, описывает specialized node/resource model и не объясняет публичную архитектуру через конкретные backend libraries;
- root `GOAL.md` и `исторического GOAL-файла` не возвращаются как tracked canonical sources.

## Команда

```bash
dotnet run --project eng/Electron2D.Build -- verify canonical-goal-alignment
```

Эта команда также подключена к CI matrix. Если старый goal-текст снова появится в tracked документации как актуальный source of truth, проверка должна завершиться ошибкой.

## Текущие ограничения

Audit намеренно точечный: он защищает canonical goal/architecture documents, а не сканирует весь репозиторий на технические идентификаторы. Некоторые спецификации могут упоминать legacy symbols только как запрет или как тестовый список, который не возвращается в public API.
