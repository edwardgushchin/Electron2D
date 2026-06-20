# Electron2D 0.1.0 Preview

Дата baseline: 2026-06-20.

`0.1.0 Preview` перезапущен как clean rewrite. Старая реализация `src/Electron2D/` удалена, а предыдущая работа сохранена в локальной ветке `private/pre-rewrite-backup-2026-06-20`.

## Что есть сейчас

- Новый пустой runtime-проект `Electron2D`.
- Тестовая инфраструктура и desktop CI matrix.
- GitHub Wiki source для API compatibility.
- Package metadata `0.1.0-preview`.

## Чего пока нет

- Runtime ещё не содержит публичных Godot-like типов.
- `Electron2D.Node` пока отсутствует; это намеренно зафиксировано baseline-тестом `SceneTreeBaselineFailsUntilNodeExists`.
- Экспорт Android/iOS пока отмечен как явный release gap, а не как active CI gate.

## Правило API

В новый runtime не переносится Unity-like/component history. Публичный API должен появляться только как согласованный Godot-like 2D-поднабор.

## Breaking changes policy для 0.x

До стабильной версии `1.0` публичный API может меняться между preview-сборками. Breaking changes допустимы только при явной записи в `CHANGELOG.md`, `RELEASE-NOTES.md` и compatibility table; compatibility layer ради старого API не добавляется.
