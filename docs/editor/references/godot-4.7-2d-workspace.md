# Пакет визуального референса: Godot 4.7, 2D workspace

Обновлено: 2026-06-24T12:58:00+03:00.

Этот файл фиксирует самодостаточный пакет визуального референса для пользовательского скриншота Godot 4 Editor, приложенного в чате 2026-06-24T02:26:00+03:00 и затем сохранённого в workspace 2026-06-24T12:56:00+03:00. Будущие задачи по UI должны ссылаться на этот пакет, а не на историю чата.

## Артефакт

- Путь: `docs/editor/references/godot-4.7-2d-workspace.png`.
- Источник: исходное пользовательское вложение из чата, сохранённое под каноническим именем.
- Состояние: `available`.
- Размер: 1920 x 1032 px.
- SHA-256: `CE8C799A6AE23423957A904BCA1CD4D28922461DE235A9A5D398864BDE10A84F`.

## Содержимое скриншота

- Окно Godot 4 Editor, тёмная тема.
- Открыта сцена `node_2d.tscn`.
- Выбран центральный workspace `2D`.
- Верхняя строка компактная: меню, workspace switcher и run/debug controls.
- Scene tabs отделены от Script/Tasks context.
- Слева видны реальные docks `Scene` и `FileSystem`.
- В центре виден 2D viewport с toolbar, rulers, grid, origin axes и zoom.
- Справа виден `Inspector` с search, выбранным `Node2D` и группами properties.
- Нижняя панель collapsed/compact, с вкладками вроде Output/Debugger/Audio/Animation.

## Что брать как ориентир

- Информационная архитектура: меню, workspace switcher, docks, viewport, Inspector, bottom panel.
- Плотность и размеры controls: компактные toolbar buttons, tabs и dock headers.
- Ясное разделение контекста объекта справа и процессных инструментов снизу.
- Видимый 2D viewport вместо диагностического текста.

## Что не копировать

- `3D`.
- `Asset Store` / `AssetLib`.
- GDScript UI и `.gd` workflows.
- Точные пиксели, брендинг, иконки или текст Godot.
