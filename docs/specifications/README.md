# Спецификации Electron2D

Этот раздел содержит целевые спецификации: они описывают, каким должен стать движок, релизы и архитектурные границы. Описание уже реализованного поведения должно жить в `docs/documentation/`.

## Архитектура

- [Архитектура и платформенный стек Electron2D](architecture/engine-platform-stack.md) - стек SDL3-CS, SDL_GPU, fallback-рендеринга, физики, аудио, текста, geometry и сетевой основы.

## Релизы

- [Electron2D 0.1.0 Preview](releases/0.1.0-preview.md) - контракт первого вертикального среза: runtime, редактор, экспорт, примеры, критерии качества и явные исключения.

## Релизное управление

- [Тестовая инфраструктура 0.1.0 Preview](release-management/test-infrastructure.md) - unit, integration, runtime smoke и golden-data проверки после clean reset.
- [CI-матрица 0.1.0 Preview](release-management/ci-matrix.md) - desktop matrix, test runner и явная отметка mobile/export gap.
