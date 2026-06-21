# Документация реализации Electron2D

Этот раздел предназначен для описания фактически реализованного поведения движка, редактора, инструментов, экспортного пайплайна и проверок.

Целевые требования и проектные контракты живут в `docs/specifications/`. Когда задача меняет исполняемый код, API, доменную модель, конфигурацию или пользовательское поведение, соответствующая справка должна обновляться здесь вместе с реализацией.

## Текущий baseline после reset

С 2026-06-20 реализация `0.1.0 Preview` строится заново: старый каталог `src/Electron2D/` удалён, а дальнейшие задачи должны создавать только Godot-like публичный API без compatibility layer и без Unity-like component history.

## Релизное управление

- [Тестовая инфраструктура 0.1.0 Preview](release-management/test-infrastructure.md) - текущие тестовые проекты, команды запуска и baseline-режим.
- [CI-матрица 0.1.0 Preview](release-management/ci-matrix.md) - текущий GitHub Actions workflow и локальная проверка его структуры.
- [Таблица совместимости Godot-like API](release-management/api-compatibility.md) - GitHub Wiki source и verifier текущего public API surface.
- [Версионирование и release metadata 0.1.0 Preview](release-management/release-metadata.md) - текущие package metadata, changelog и release notes.
- [Формат проекта и шаблон electron2d-empty](release-management/project-template.md) - текущий минимальный шаблон проекта и проверка его сборки/запуска.
- [Performance budgets и soak-критерии 0.1.0 Preview](release-management/performance-budgets.md) - текущая матрица устройств и release-gate бюджеты.

## Репозиторий

- [Политика MIT License и source headers](repository/license-policy.md) - текущая проверка корневой лицензии и license header в исходных файлах.

## Рендеринг

- [`RenderingServer` и renderer profiles](rendering/rendering-server.md) - текущий server boundary, renderer profile и feature flags.
- [SDL_GPU lifecycle baseline](rendering/sdl-gpu-lifecycle.md) - текущий internal lifecycle adapter для SDL_GPU device/window/frame state machine.
- [CanvasItem render queue baseline](rendering/canvas-item-render-queue.md) - текущая internal сортировка canvas item команд и contiguous batching.
- [Texture2D resource baseline](rendering/texture-resource-baseline.md) - текущие public `Texture2D`/`AtlasTexture` и internal texture lifetime registry.
- [Canvas node submission baseline](rendering/canvas-node-submission-baseline.md) - текущие public `CanvasItem`/`Node2D`/`Sprite2D`/`CanvasLayer` и internal sprite submission model.

## Базовые типы

- [2D math baseline](core-types/2d-math-baseline.md) - текущие `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color` и `Mathf`.
- [RandomNumberGenerator](core-types/random-number-generator.md) - текущий Godot-like RNG с `Seed`, `State`, range API и deterministic sequence tests.
- [`StringName` и `Rid`](core-types/stringname-rid.md) - текущие identity-типы для имён и низкоуровневых resource handles.
- [`Variant`](core-types/variant.md) - текущий closed-list value carrier и Godot-like коллекции `Array`/`Dictionary`.
- [Stable `Variant` serialization](core-types/variant-serialization.md) - текущий internal canonical JSON round-trip для сериализуемого Variant subset.

## Объектная модель

- [Базовые типы Object, RefCounted, Resource](object-model/base-object-lifetime.md) - текущий baseline public API и правила lifetime.
- [Node и SceneTree lifecycle](object-model/node-scene-tree-lifecycle.md) - текущий baseline lifecycle callbacks и порядок обхода.
- [Иерархия Node, ownership и безопасное удаление](object-model/node-hierarchy-ownership.md) - текущий parent-child API, `Owner`, `Reparent()` и `QueueFree()`.
- [`NodePath` и разрешение node paths](object-model/node-path-resolution.md) - текущий relative/absolute lookup через `GetNode()` и `GetNodeOrNull()`.
- [Группы Node и group calls](object-model/node-groups.md) - текущие group membership, persistent metadata и `SceneTree.CallGroup()`.
- [Сигналы, Callable и emission semantics](object-model/signals-callable.md) - текущие user signals, `Connect()`, `Disconnect()`, `EmitSignal()` и `Callable`.
- [Deferred calls и безопасный traversal](object-model/deferred-calls-safe-traversal.md) - текущие `Object.CallDeferred()`, `Callable.CallDeferred()`, deferred queue и безопасное изменение дерева во время обхода.
- [`PackedScene` и смена активной сцены](object-model/packed-scene.md) - текущие `Pack()`, `Instantiate()`, owned subtree snapshot и `SceneTree.ChangeSceneToPacked()`.
- [Runtime diagnostics пользовательского кода](object-model/runtime-diagnostics.md) - внутренняя диагностика и правило продолжения работы после ошибок в lifecycle, group calls, deferred calls и signals.
