# Source domain layout

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

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceDomainLayout.ps1
```

Проверка падает, если non-core домен возвращается в `Core`, если мелкий домен оказывается на верхнем уровне `src/Electron2D`, если обязательный root/nested domain отсутствует, если audio runtime code лежит не в `Runtime/Audio`, если export presets лежат не в `Export/Presets`, или если source file добавляет неподдержанный namespace.
