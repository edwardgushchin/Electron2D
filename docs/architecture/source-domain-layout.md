# Source domain layout

Обновлено: 2026-06-30.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0134`.
Обновлено: 2026-06-22.

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
- `Runtime/Audio`;
- `Runtime/Input`;
- `Runtime/Localization`;
- `Runtime/Scripting`;
- `Runtime/Settings`.

`Export` в runtime project содержит только публичные data contracts для export preset и платформенно-нейтральной конфигурации. Фактическая orchestration экспорта, поиск toolchain, signing, генерация Xcode project, вызовы Android SDK, упаковка desktop distributions и запуск внешних процессов должны жить в `Electron2D.Tooling` или отдельном tooling/export project.

Разрешённая граница:

```text
src/Electron2D/Export/Presets/
    public data contracts

src/Electron2D.Tooling/Export/
    export orchestration

src/Electron2D.Tooling/Export/Android/
src/Electron2D.Tooling/Export/iOS/
src/Electron2D.Tooling/Export/Desktop/
    platform implementations
```

## Namespace policy

Для runtime project `src/Electron2D` разрешены namespaces:

- `Electron2D`;
- `Electron2D.Collections`.

Новые namespaces добавляются только отдельной задачей с явным API-обоснованием. Совпадение namespace с папкой не является причиной для добавления namespace.

## Проверка

`dotnet run --project eng/Electron2D.Build -- verify source-domain-layout` должен проверять:

- `Core` содержит только разрешённые core domains;
- root domains существуют только в крупной форме `Assets`, `Core`, `Export`, `Graphics`, `Physics`, `Runtime`;
- nested domains существуют внутри своих root domains;
- audio runtime code лежит в `src/Electron2D/Runtime/Audio`;
- export presets лежат в `src/Electron2D/Export/Presets`;
- C# files в `src/Electron2D` используют только разрешённые namespaces.

## Критерии приёмки

- Каталоги исходников разнесены согласно этой спецификации.
- Public API namespace не изменён из-за физического переноса файлов.
- `dotnet run --project eng/Electron2D.Build -- verify source-domain-layout` проходит локально и в CI.
- Сборка и тесты проходят после переноса.

## Фактическое состояние, ограничения и проверки

Текущий runtime project `src/Electron2D` разнесён по крупным доменным каталогам. `Core` сохранён как узкий слой ядра, а не как общий контейнер всех подсистем.

## Core

В `src/Electron2D/Core` находятся только базовые домены:

- `Collections`;
- `Identity`;
- `Math`;
- `ObjectModel`;
- `Random`;
- `SceneTree`;
- `Variant`.

Эти каталоги содержат основу runtime: `Object`, `Node`, `SceneTree`, `Variant`, базовые value-типы, идентификаторы и коллекции.

## Root domains

На верхнем уровне `src/Electron2D/` находятся крупные домены:

- `Assets`;
- `Core`;
- `Export`;
- `Graphics`;
- `Physics`;
- `Runtime`.

Детальные подсистемы живут вторым уровнем:

- `Assets/Resources`;
- `Graphics/Display`;
- `Graphics/Rendering`;
- `Graphics/Text`;
- `Graphics/UI`;
- `Runtime/Animation`;
- `Runtime/Audio`;
- `Runtime/Input`;
- `Runtime/Localization`;
- `Runtime/Scripting`;
- `Runtime/Settings`.

`Export/Presets` содержит runtime data contracts для export preset и платформенно-нейтральной конфигурации. Platform-specific export orchestration, поиск toolchain, signing, генерация Xcode project, вызовы Android SDK, упаковка desktop distributions и запуск внешних процессов не должны находиться в runtime project `src/Electron2D`; их место — `Electron2D.Tooling` или отдельный tooling/export project.

## Namespace

Папки не задают namespace. В runtime project разрешены:

- `Electron2D` для основных типов;
- `Electron2D.Collections` для коллекций.

Например, файл может физически лежать в `src/Electron2D/Graphics/Rendering`, но public type остаётся в namespace `Electron2D`, если так устроен публичный API.

## Проверка

```bash
dotnet run --project eng/Electron2D.Build -- verify source-domain-layout
```

Проверка падает, если non-core домен возвращается в `Core`, если мелкий домен оказывается на верхнем уровне `src/Electron2D`, если обязательный root/nested domain отсутствует, если audio runtime code лежит не в `Runtime/Audio`, если export presets лежат не в `Export/Presets`, или если source file добавляет неподдержанный namespace.
