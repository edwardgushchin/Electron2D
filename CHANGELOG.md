# Changelog

## 0.1.0-preview

Статус: clean rewrite baseline.

### Добавлено

- Новый пустой runtime-проект `src/Electron2D/Electron2D.csproj` с package version `0.1.0-preview`.
- Тестовая инфраструктура: unit, integration, runtime smoke и golden-data проекты.
- CI-матрица для Windows, Linux и macOS.
- GitHub Wiki source для таблицы совместимости API.
- Verifier-скрипты для тестов, CI, API compatibility и release metadata.

### Изменено

- `main` возвращён к baseline `4007f36bf6857b33d6fc8cf614732f92e839287d`.
- Старая реализация `src/Electron2D/` удалена полностью.

### Удалено

- Unity-like/component history, включая `IComponent`, `SpriteRenderer`, `SpriteAnimator`, `AudioSource` и legacy physics components.

### Ограничения

- Runtime assembly пока экспортирует `0` публичных типов.
- `0.1.0-preview` ещё не является готовым игровым runtime; дальнейшая реализация идёт задачами из `TASKS.md`.

### Breaking changes policy

- В ветке `0.x` публичный API может меняться между preview-сборками.
- Compatibility layer ради старого API не добавляется.
- Каждое breaking change должно быть явно отражено в changelog и release notes.
