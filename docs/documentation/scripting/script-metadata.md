# Script metadata: `[Export]`, `[Signal]`, `[Tool]`

Статус: реализованный baseline.
Задача: `T-0045`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1.0 Preview` предоставляет три публичных Godot-like marker attributes:

- `ExportAttribute` - `[Export]` для fields/properties, которые должны попасть в serialization/Inspector metadata;
- `SignalAttribute` - `[Signal]` для delegate declarations, описывающих signal payload;
- `ToolAttribute` - `[Tool]` для script classes, предназначенных для editor-time workflows.

Атрибуты намеренно не содержат дополнительных публичных properties или Electron2D-specific hints. Расширенные Inspector hints и generated signal name helpers должны добавляться отдельными Godot-like задачами.

## Internal metadata bridge

Связь атрибутов с serialization/Inspector реализована внутренней AOT-safe моделью:

- `ScriptObjectMetadataRegistry` хранит metadata по script type и script name;
- `ScriptObjectTypeMetadata` хранит exported properties, signals и tool-state;
- `ScriptExportPropertyMetadata` описывает typed getter/setter для exported value;
- `ScriptSignalMetadata` описывает signal name и delegate type;
- `ScriptObjectSerializer` делает capture/restore exported properties через `SerializedPropertyValue`.

Runtime не сканирует assemblies и не делает automatic public property discovery. Metadata регистрируется явно и использует typed delegates, чтобы будущий source generator мог выдавать такие descriptors без reflection fallback.

## Export round-trip

`ScriptObjectSerializer.CaptureExportedProperties()` возвращает только properties из зарегистрированной metadata. Имена сортируются ordinal, поэтому snapshot стабилен для scene/resource serialization и Inspector.

`ScriptObjectSerializer.RestoreExportedProperties()` применяет только известные exported properties. Runtime-only properties, которых нет в metadata, не изменяются.

## Signals

`ScriptObjectMetadataRegistry.ApplySignals()` добавляет signals из metadata на конкретный script instance через `AddUserSignal()`.

После применения metadata signal работает через уже реализованный Godot-like signal API:

```csharp
script.Connect("health_changed", Callable.From<int>(value => { }));
script.EmitSignal("health_changed", 7);
```

.NET events не используются и не становятся публичной моделью сигналов.

## `[Tool]`

Для `[Tool]` metadata фиксируются признаки:

- `IsTool`;
- `IsToolExperimental`;
- `IsToolExecutionSandboxed`.

Это означает только классификацию script class для editor tooling. Безопасное editor-time выполнение `[Tool]` scripts остаётся отдельной задачей и не запускается runtime path.

## Проверки

Сфокусированная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ScriptMetadataTests"
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
