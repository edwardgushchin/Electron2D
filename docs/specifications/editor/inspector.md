# Inspector редактора

Статус: целевая спецификация для `T-0082`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor` должен получить проверяемую модель Inspector для редактирования свойств выбранного узла или ресурса сцены. Inspector должен работать поверх текущего `SceneFileDocument` и `SerializedPropertyValue`, чтобы изменения сохранялись в том же scene JSON, который уже используют Scene Tree dock, resource serialization и будущие project tooling операции.

Эта задача не добавляет новый публичный runtime API. Список редактируемых свойств приходит из уже существующей metadata-модели: script export metadata, resource metadata или editor-provided descriptors для сохранённых scene properties.

## Контракт данных

Inspector должен поддерживать редактирование следующих переносимых значений:

- primitive values: `bool`, integer, floating-point, `string`;
- `NodePath`;
- enum values;
- flags enum values;
- arrays через `SerializedPropertyValueKind.Array`;
- resource references через `SerializedPropertyValueKind.Resource`;
- nested resource properties у internal resources внутри `SceneFileDocument`;
- reset to default для свойства, если descriptor содержит default value.

Все изменения должны попадать в `SceneFileDocument` без второго формата. После serialize/deserialize значения должны оставаться теми же.

## Export metadata

Inspector не должен искать публичные members через runtime reflection. Для script classes он использует explicit export metadata, которая уже создаётся для `[Export]` members. В `T-0082` достаточно проверить editor-facing слой: property descriptor должен знать, что значение пришло из export metadata, и smoke-проверка должна доказать, что только такие properties доступны Inspector.

Если metadata для типа отсутствует, Inspector должен fail closed: не создавать скрытые editable properties и не пытаться угадать имена members.

## Undo/redo

Каждая mutating operation должна проходить через undo/redo слой:

- edit property;
- reset property to default;
- edit nested resource property.

Undo возвращает снимок scene document до операции, redo возвращает снимок после операции. Inspector после undo/redo должен заново читать текущий document и показывать актуальные значения.

## Smoke-режим

Editor executable должен поддерживать аргумент:

```text
--inspector-smoke <work-root>
```

Smoke-режим должен:

- создать временный scene file в `<work-root>`;
- зарегистрировать тестовую editor metadata для script-like node properties;
- открыть `SceneFileDocument` через Inspector model;
- подтвердить, что Inspector видит только exported properties;
- изменить primitive, enum, flags, array, resource reference и `NodePath`;
- изменить property nested internal resource;
- выполнить reset default для одного свойства;
- выполнить undo и redo для reset;
- сохранить и заново загрузить scene file;
- вывести machine-readable строки: `ScenePath`, `PropertyCount`, `ExportedProperties`, `SerializedHealth`, `SerializedName`, `UndoName`, `RedoName`, `SerializedMode`, `SerializedFlags`, `SerializedTags`, `SerializedPath`, `ResourceReference`, `NestedMaxHealth`, `RoundTripStable`;
- вернуть exit code `0`, если все инварианты выполнены.

## Приемочные критерии

- Integration test запускает `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --inspector-smoke ...`.
- Тест подтверждает, что Inspector получает список редактируемых properties из metadata.
- Тест подтверждает primitive, enum, flags, array, resource reference и `NodePath` edits.
- Тест подтверждает редактирование nested internal resource property.
- Тест подтверждает reset default, undo и redo.
- Тест подтверждает, что saved scene JSON остаётся валидным `SceneFileDocument`.
- Документация реализации описывает smoke workflow и ограничения.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki` проходит перед продолжением editor work.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.
