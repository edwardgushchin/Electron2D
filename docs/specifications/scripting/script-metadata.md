# Script metadata: `[Export]`, `[Signal]`, `[Tool]`

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
