# Script metadata: `[Export]`, `[Signal]`, `[Tool]`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0045`.
Обновлено: 2026-06-21.
Связанные документы: [C# script classes](csharp-script-classes.md), [Безопасное editor-time выполнение `[Tool]` scripts](tool-script-execution.md), [AOT-safe metadata для Inspector и serialization](../resources/aot-safe-metadata.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`0.1.0 Preview` должен дать C# script classes минимальную metadata-модель:

- `[Export]` помечает property или field как сериализуемое и видимое для Inspector значение;
- `[Signal]` помечает delegate как описание сигнала script class;
- `[Tool]` помечает script class как editor-time capable.

Эти атрибуты являются публичным API для пользовательского кода. Они не являются compatibility layer и не добавляют component model из старой реализации.

## Контракт публичных атрибутов

Публичный API задачи ограничен marker attributes:

- `Electron2D.ExportAttribute`;
- `Electron2D.SignalAttribute`;
- `Electron2D.ToolAttribute`.

Атрибуты не должны добавлять публичные Electron2D-specific properties, методы или hints сверх baseline этой задачи. Подробные Inspector hints, groups, ranges и generated signal name helpers остаются отдельными задачами.

## AOT-safe metadata bridge

Runtime не должен сканировать assemblies, обходить все public properties через reflection или искать script types динамически.

Связь с serialization/Inspector задаётся internal metadata:

- typed descriptors для exported properties;
- typed descriptors для signal delegates;
- признак `Tool` как experimental и sandboxed metadata;
- stable ordering по имени свойства или сигнала;
- явная регистрация metadata по script type и script name.

Этот bridge предназначен для будущего source generator/editor tooling, но текущая задача может регистрировать metadata вручную в тестах и внутренних сценариях.

## Serialization и Inspector

Exported properties должны round-trip через переносимую модель `SerializedPropertyValue`, используемую scene/resource serialization. Inspector baseline получает список exported property descriptors из той же metadata, а не из reflection-discovery.

Требования:

- только metadata-listed exported properties попадают в serialized snapshot;
- неэкспортированные runtime-only properties не меняются при restore;
- property names стабильны и сортируются ordinal;
- unsupported value types fail closed через существующий converter.

## Signals

`[Signal]` описывает delegate в script class. Registered signal metadata должна уметь добавить signal на экземпляр через существующий `Object.AddUserSignal()`.

После применения metadata signal должен работать через текущие API:

- `Connect()`;
- `IsConnected()`;
- `EmitSignal()`;
- `Disconnect()`.

.NET events не входят в public signal model.

## `[Tool]`

В `0.1.0 Preview` `[Tool]` помечается как experimental и sandboxed metadata. Safe editor-time execution, exception isolation и запрет dynamic assembly load описаны отдельной спецификацией `tool-script-execution.md`.

## Проверки

- Integration tests проверяют export property round-trip.
- Integration tests проверяют, что signal из metadata регистрируется и вызывается через `Callable`.
- Integration tests проверяют, что `[Tool]` metadata помечена как experimental и sandboxed.
- API compatibility verifier проверяет, что все новые public attribute types есть в GitHub Wiki source.
- Source license verifier проходит для новых C# files.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0045`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1.0 Preview` предоставляет три публичных marker attributes:

- `ExportAttribute` - `[Export]` для fields/properties, которые должны попасть в serialization/Inspector metadata;
- `SignalAttribute` - `[Signal]` для delegate declarations, описывающих signal payload;
- `ToolAttribute` - `[Tool]` для script classes, предназначенных для editor-time workflows.

Атрибуты намеренно не содержат дополнительных публичных properties или Electron2D-specific hints. Расширенные Inspector hints и generated signal name helpers должны добавляться отдельными задачами публичного API.

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

После применения metadata signal работает через уже реализованный signal API:

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

Это означает классификацию script class для editor tooling. Безопасное editor-time выполнение `[Tool]` scripts реализовано внутренним host и описано в `docs/scripting/tool-script-execution.md`.

## Проверки

Сфокусированная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ScriptMetadataTests"
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
