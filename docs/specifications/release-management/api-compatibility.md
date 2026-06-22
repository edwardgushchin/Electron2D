# Таблица совместимости Electron2D API

Статус: целевая спецификация.
Задача: `T-0004`.
Обновлено: 2026-06-20.

## Цель

Для `0.1.0 Preview` нужно поддерживать публичный API только в рамках согласованного Electron2D 2D-поднабора. Все публичные типы runtime assembly должны быть отражены в compatibility table с одним из статусов:

- `Supported`
- `Partial`
- `Experimental`
- `Planned`

## GitHub Wiki

Compatibility table должна храниться в GitHub Wiki repository проекта. Репозиторий не должен добавлять локальный сайт, static site generator или отдельный local docs portal ради этой таблицы. Каталог `.github/wiki/` допустим только как игнорируемый локальный клон `Electron2D.wiki.git`.

Canonical location для текущей задачи:

```text
https://github.com/edwardgushchin/Electron2D.wiki.git
API-Compatibility.md
```

Этот файл предназначен для публикации в GitHub Wiki проекта.

## Clean baseline

После clean reset runtime assembly может временно не экспортировать публичных типов. Это допустимый baseline, если:

- verifier подтверждает `0` exported public types;
- legacy/component API не существует в public surface;
- planned Electron2D типы перечислены как `Planned`.

## UI gate before Editor

`Electron2D.Editor` нельзя начинать до отдельного UI public API gate. Этот gate считается закрытым только когда все UI-related public API строки в GitHub Wiki `API-Compatibility.md` переведены в `Supported` на основании фактической реализации, тестов, XML documentation, generated Wiki pages, спецификаций и документации реализации.

Запрещено переводить UI rows из `Partial` в `Supported` только ради разблокировки редактора. Если для редактора, Project Manager, Inspector, dock UI, встроенного редактора кода или AI-friendly terminal panel не хватает публичного UI API, соответствующая задача должна оставаться заблокированной до реализации этого API в runtime.

Список UI/Text rows берётся из generated GitHub Wiki page `API-UI-and-Text.md`. Локальная и CI-проверка `tools/Verify-UiPublicApiGate.ps1` должна падать, если любая строка из этого списка отсутствует в `API-Compatibility.md` или имеет статус не `Supported`.

## Запрещённый API

Следующие имена не должны появляться в public surface новой реализации:

- `IComponent`
- `SpriteRenderer`
- `SpriteAnimator`
- `AudioSource`
- `Rigidbody`
- `Collider`
- `BoxCollider`
- `CircleCollider`
- `PolygonCollider`
- `PhysicsBodyType`

## Верификация

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ApiCompatibility.ps1
powershell -ExecutionPolicy Bypass -File tools/Verify-UiPublicApiGate.ps1 -WikiPath .github/wiki
```

Verifier должен собрать runtime, прочитать exported public types и убедиться, что каждый публичный тип отражён в GitHub Wiki clone с допустимым статусом. Legacy/component API должен запрещаться по public surface, но не публиковаться отдельным списком в Wiki.
