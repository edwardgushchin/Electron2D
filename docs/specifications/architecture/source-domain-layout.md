# Source domain layout

Статус: целевая спецификация для `T-0134`.
Обновлено: 2026-06-21.

## Назначение

`src/Electron2D/Core` нужен, но он не должен быть контейнером для всех подсистем. В `Core` должны находиться только вещи, напрямую относящиеся к ядру движка: объектная модель, дерево сцен, базовые идентификаторы, базовые value-типы и переносимый контейнер значений.

Верхний уровень `src/Electron2D` должен состоять из крупных доменов, а не из списка мелких подсистем. Детализация вроде input, settings, text или UI размещается вторым уровнем внутри крупного домена.

Физическая структура каталогов не задаёт публичный namespace. Namespace должен следовать форме публичного C# API: основные типы находятся в корневом namespace `Electron2D`, коллекции - в `Electron2D.Collections`. Домены вроде rendering, physics, export или input не должны создавать публичные namespaces `Electron2D.Rendering`, `Electron2D.Physics` и так далее только потому, что есть такие папки.

## Core domains

В `src/Electron2D/Core` разрешены только эти домены:

- `Collections`;
- `Identity`;
- `Math`;
- `ObjectModel`;
- `Random`;
- `SceneTree`;
- `Variant`.

Эти домены являются базовым слоем, от которого зависят остальные подсистемы.

## Root domains

На верхнем уровне `src/Electron2D/` разрешены только крупные домены:

- `Assets`;
- `Core`;
- `Export`;
- `Graphics`;
- `Physics`;
- `Runtime`.

## Nested domains

Поддомены размещаются внутри крупных root domains:

- `Assets/Resources`;
- `Graphics/Display`;
- `Graphics/Rendering`;
- `Graphics/Text`;
- `Graphics/UI`;
- `Runtime/Animation`;
- `Runtime/Input`;
- `Runtime/Localization`;
- `Runtime/Scripting`;
- `Runtime/Settings`.

`Export` дополнительно делится на поддомены, например `Export/Presets` и будущий `Export/Windows`.

## Namespace policy

Для runtime project `src/Electron2D` разрешены namespaces:

- `Electron2D`;
- `Electron2D.Collections`.

Новые namespaces добавляются только отдельной задачей с явным API-обоснованием. Совпадение namespace с папкой не является причиной для добавления namespace.

## Проверка

`tools/Verify-SourceDomainLayout.ps1` должен проверять:

- `Core` содержит только разрешённые core domains;
- root domains существуют только в крупной форме `Assets`, `Core`, `Export`, `Graphics`, `Physics`, `Runtime`;
- nested domains существуют внутри своих root domains;
- export presets лежат в `src/Electron2D/Export/Presets`;
- C# files в `src/Electron2D` используют только разрешённые namespaces.

## Критерии приёмки

- Каталоги исходников разнесены согласно этой спецификации.
- Public API namespace не изменён из-за физического переноса файлов.
- `tools/Verify-SourceDomainLayout.ps1` проходит локально и в CI.
- Сборка и тесты проходят после переноса.
