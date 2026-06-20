# Спецификации Electron2D

Этот раздел содержит целевые спецификации: они описывают, каким должен стать движок, релизы и архитектурные границы. Описание уже реализованного поведения должно жить в `docs/documentation/`.

## Архитектура

- [Архитектура и платформенный стек Electron2D](architecture/engine-platform-stack.md) - стек SDL3-CS, SDL_GPU, fallback-рендеринга, физики, аудио, текста, geometry и сетевой основы.
- [AI-friendly workflow Electron2D 0.1](architecture/ai-friendly-workflow.md) - архитектурный контракт CLI/MCP/tooling, текстовых форматов, диагностики, автоматизированного запуска и AI benchmark.

## Релизы

- [Electron2D 0.1.0 Preview](releases/0.1.0-preview.md) - контракт первого вертикального среза: runtime, редактор, экспорт, примеры, критерии качества и явные исключения.

## Релизное управление

- [Тестовая инфраструктура 0.1.0 Preview](release-management/test-infrastructure.md) - unit, integration, runtime smoke и golden-data проверки после clean reset.
- [CI-матрица 0.1.0 Preview](release-management/ci-matrix.md) - desktop matrix, test runner и явная отметка mobile/export gap.
- [Таблица совместимости Godot-like API](release-management/api-compatibility.md) - GitHub Wiki source и verifier для public API surface.
- [Версионирование и release metadata 0.1.0 Preview](release-management/release-metadata.md) - package metadata, changelog и release notes.
- [Формат проекта и шаблон electron2d-empty](release-management/project-template.md) - минимальный project manifest, scene manifest и проверка build/run.
- [Performance budgets и soak-критерии 0.1.0 Preview](release-management/performance-budgets.md) - целевые устройства, 60 FPS, memory budgets и mobile cycles.

## Объектная модель

- [Базовые типы Object, RefCounted, Resource](object-model/base-object-lifetime.md) - начальный Godot-like runtime API и правила lifetime.
- [Node и SceneTree lifecycle](object-model/node-scene-tree-lifecycle.md) - начальный lifecycle baseline и порядок callbacks.
- [Иерархия Node, ownership и безопасное удаление](object-model/node-hierarchy-ownership.md) - parent-child инварианты, `Owner`, reparent/move и `QueueFree()`.
- [`NodePath` и разрешение node paths](object-model/node-path-resolution.md) - relative/absolute path resolution, `GetNode()` и `GetNodeOrNull()`.
- [Группы Node и group calls](object-model/node-groups.md) - group membership, persistent metadata и `SceneTree` group queries/calls.
- [Сигналы, Callable и emission semantics](object-model/signals-callable.md) - user signals, `Connect()`, `Disconnect()`, `EmitSignal()` и `Callable`.
- [Deferred calls и безопасное изменение дерева](object-model/deferred-calls-safe-traversal.md) - `CallDeferred()`, deferred queue и безопасный traversal при изменении дерева.
- [`PackedScene` и смена активной сцены](object-model/packed-scene.md) - pack/instantiate owned subtree и `SceneTree.ChangeSceneToPacked()` baseline.
- [Runtime diagnostics пользовательского кода](object-model/runtime-diagnostics.md) - внутренняя диагностика для lifecycle, signals, deferred calls и group calls.
