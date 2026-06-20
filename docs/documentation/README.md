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

## Объектная модель

- [Базовые типы Object, RefCounted, Resource](object-model/base-object-lifetime.md) - текущий baseline public API и правила lifetime.
- [Node и SceneTree lifecycle](object-model/node-scene-tree-lifecycle.md) - текущий baseline lifecycle callbacks и порядок обхода.
