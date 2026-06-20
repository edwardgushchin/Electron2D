# Таблица совместимости Godot-like API

Статус: целевая спецификация.
Задача: `T-0004`.
Обновлено: 2026-06-20.

## Цель

Для `0.1.0 Preview` нужно поддерживать публичный API только в рамках согласованного Godot-like 2D-поднабора. Все публичные типы runtime assembly должны быть отражены в compatibility table с одним из статусов:

- `Supported`
- `Partial`
- `Experimental`
- `Planned`
- `Not planned`

## GitHub Wiki

Compatibility table должна готовиться как source для GitHub Wiki. Репозиторий не должен добавлять локальный сайт, static site generator или отдельный local docs portal ради этой таблицы.

Canonical source для текущей задачи:

```text
.github/wiki/API-Compatibility.md
```

Этот файл предназначен для публикации в GitHub Wiki repo проекта.

## Clean baseline

После clean reset runtime assembly может временно не экспортировать публичных типов. Это допустимый baseline, если:

- verifier подтверждает `0` exported public types;
- legacy/component API не существует в public surface;
- planned Godot-like типы перечислены как `Planned`;
- legacy Unity-like/component типы перечислены как `Not planned`.

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
```

Verifier должен собрать runtime, прочитать exported public types и убедиться, что каждый публичный тип отражён в GitHub Wiki source с допустимым статусом.
