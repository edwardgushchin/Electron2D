# Inspector редактора

Статус: документация реализации для `T-0082`.
Дата: 2026-06-22.

## Назначение

Inspector в `Electron2D.Editor` редактирует свойства выбранного узла или ресурса сохранённой сцены. Текущий слой является внутренней моделью редактора: он не добавляет публичные типы в runtime assembly `Electron2D` и не создаёт отдельный формат данных.

Inspector работает с `SceneFileDocument`, `SceneFileNode.Properties`, internal resources и `SerializedPropertyValue`. Поэтому изменения можно сохранить через существующий `SceneFileTextSerializer`, а затем загрузить обратно без потери значения.

## Текущее поведение

Модель Inspector поддерживает:

- чтение списка properties из editor descriptors, построенных на registered metadata;
- отметку properties, пришедших из export metadata;
- edit primitive values;
- edit `NodePath`;
- edit enum и flags enum values;
- edit arrays;
- edit internal resource reference;
- edit nested property у internal resource;
- reset property to default;
- undo и redo для property edits, reset и nested resource edits.

Undo/redo хранит снимки `SceneFileDocument` до и после операции. После undo или redo Inspector перечитывает текущий document, поэтому отображаемые значения совпадают с сохранённой моделью сцены.

## Smoke workflow

Локальная проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --inspector-smoke .temp\editor-inspector
```

Ожидаемый результат включает:

```text
Electron2D.Editor inspector smoke passed
PropertyCount=8
ExportedProperties=8
SerializedHealth=42
NestedMaxHealth=250
RoundTripStable=True
```

Smoke-команда создаёт scene file, выполняет edits для primitive, enum, flags, array, resource reference, `NodePath` и nested resource, проверяет reset default и undo/redo, затем сохраняет и перечитывает scene JSON.

## Ограничения

- В этой задаче Inspector реализован как внутренняя модель и smoke-команда, а не как постоянное визуальное окно с pointer/keyboard input.
- Inspector не открывает внешние resource files и не выполняет import pipeline. Он редактирует только данные, уже находящиеся в `SceneFileDocument`.
- Inspector не делает runtime reflection discovery. Если metadata не передана, property не становится редактируемым автоматически.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorInspectorTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
