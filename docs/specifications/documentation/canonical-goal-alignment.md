# Canonical goal alignment audit

Статус: целевая спецификация.
Задача: `T-0152`.
Обновлено: 2026-06-23.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Архитектура и платформенный стек Electron2D](../architecture/engine-platform-stack.md).

## Назначение

Документы целей и архитектуры должны ссылаться на текущий canonical contract `0.1.0 Preview`, а не возвращать старое component-first или four-platform позиционирование как актуальную цель проекта.

Audit должен защищать от регрессий в tracked documentation:

- release contract включает Windows, Linux, macOS, Android, iOS и WebAssembly browser;
- engine architecture описывает specialized node/resource model, а не прежнюю component model;
- `Node2D` владеет 2D transform, базовый `Node` не получает обязательный transform;
- `scene_attach_script` связывает serialized node с пользовательским C# node type, а не добавляет отдельный Script-компонент;
- old root goal files не возвращаются как tracked canonical sources;
- `engine-platform-stack.md` не объясняет публичное позиционирование через конкретные backend libraries.

## Проверка

Репозиторий должен содержать verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-CanonicalGoalAlignment.ps1
```

Verifier должен запускаться локально и в CI вместе с остальными documentation gates.

## Критерии приёмки

- `engine-platform-stack.md` синхронизирован с `0.1.0 Preview` и явно помечает исторический source как неавторитетный при конфликте.
- Verifier падает на старом backend-specific или component-first positioning и проходит на актуальном canonical contract.
- CI matrix запускает verifier на всех desktop runners.
- Implementation documentation описывает фактическое поведение и команду проверки.
