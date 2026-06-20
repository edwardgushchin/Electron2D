# Таблица совместимости Godot-like API

Статус: реализованная проверка compatibility baseline.
Задача: `T-0004`.
Обновлено: 2026-06-20.

## Где находится таблица

Compatibility table хранится как GitHub Wiki source:

```text
.github/wiki/API-Compatibility.md
```

Это не локальный сайт и не generated documentation portal. Файл предназначен для публикации в GitHub Wiki проекта.

## Текущий baseline

Новый runtime assembly `Electron2D` экспортирует только начальные типы объектной модели:

- `Electron2D.Object`
- `Electron2D.RefCounted`
- `Electron2D.Resource`
- `Electron2D.Node`
- `Electron2D.NodePath`
- `Electron2D.SceneTree`
- `Electron2D.InputEvent`

Это осознанный минимальный baseline после удаления старого `src/Electron2D/`: каждый новый публичный тип должен добавляться только через задачу и только в Godot-like форме.

Wiki source содержит:

- легенду статусов `Supported`, `Partial`, `Experimental`, `Planned`, `Not planned`;
- planned Godot-like 2D surface;
- явно исключённый legacy/component API.

## Локальная проверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ApiCompatibility.ps1
```

Verifier собирает `src/Electron2D/Electron2D.csproj`, читает exported public types из `Electron2D.dll`, сверяет их с `.github/wiki/API-Compatibility.md` и запрещает возврат legacy/component типов.
