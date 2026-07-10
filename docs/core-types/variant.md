# `Variant`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`Variant` должен стать Electron2D контейнером значения для динамических runtime API Electron2D `0.1-preview`: сигналов, deferred calls, будущей базы свойств, scene/resource serialization и Agent-native cross-platform 2D game engine tooling. Публичный C# API `Variant`, входящий в утверждённую публичную поверхность, должен совпадать с Godot `4.7-stable` .NET/C# API. Значения и типы, которые не закрыты полным evidence, должны быть явно отражены в API manifest, проверках совместимости и, если нужно, в утверждённых строках `Deferred`/`Unsupported` в manual public API profile.

## Источники поведения

- [Godot C# Variant](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_variant.html);
- [Godot Variant](https://docs.godotengine.org/en/stable/classes/class_variant.html);
- [Godot `variant.h`](https://github.com/godotengine/godot/blob/master/core/variant/variant.h).

Godot C# использует `Variant` как `struct`, где `default`/пустой constructor означают null-like `Nil`, а совместимые C# значения преобразуются в `Variant` через implicit conversions или `Variant.From<T>()`.

## Закрытый список типов `0.1-preview`

`Variant.Type` в Electron2D `0.1-preview` поддерживает только эти значения:

- `Nil`;
- `Bool`;
- `Int`;
- `Float`;
- `String`;
- `Vector2`;
- `Vector2I`;
- `Rect2`;
- `Rect2I`;
- `Transform2D`;
- `Color`;
- `StringName`;
- `NodePath`;
- `Rid`;
- `Object`;
- `Callable`;
- `Dictionary`;
- `Array`.

`Resource` и `Node` хранятся как `Object`, потому что они наследуются от `Electron2D.ElectronObject`. Здесь `Object` означает категорию `Variant.Type.Object`, а не обычный CLR `object`. Перечисления хранятся как `Int`, как в Godot C#.

В `0.1-preview` намеренно не входят:

- 3D-типы (`Vector3`, `Transform3D`, `Basis`, `Projection` и связанные типы);
- `Signal`, пока в публичном API нет отдельного Electron2D `Signal`;
- packed arrays;
- editor-only значения;
- произвольные CLR-объекты, не наследующиеся от `Electron2D.ElectronObject`.

## Контракт публичной поверхности

Состав публичных типов, свойств и методов Variant-domain задают generated artifacts: `data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md` и `verify api-compatibility --wiki-path .github/wiki`. Этот документ не повторяет signatures и не является источником истины для списка элементов API.

Поведенческий контракт домена:

- `Variant` остаётся immutable value carrier с явным discriminator текущего `Variant.Type`.
- Пустое значение и `null` создают `Nil`.
- Поддержанные C# значения преобразуются через утверждённые generated API entry points.
- Строгие typed-read операции возвращают значение только для совместимого фактического типа.
- Electron2D collection wrappers хранятся по ссылке и остаются mutable reference-like объектами внутри `Variant`.

`Variant` должен иметь implicit conversions из поддерживаемых не-enum C# типов. Enum значения должны проходить через explicit factory entry points, чтобы не создавать неявных enum overloads.

## Коллекции

Для соответствия Godot C# коллекции живут не как `System.Array`/`System.Collections.Generic.Dictionary`, а как Electron2D collection wrappers, перечисленные в generated API manifest.

`Array` хранит `Variant` элементы в порядке добавления. `Dictionary` хранит пары `Variant` -> `Variant`. Эти контейнеры являются mutable reference-like значениями; `Variant` хранит ссылку на контейнер.

## Правила преобразования

- `default(Variant)` и `Variant.CreateFrom(null)` возвращают `Variant.Type.Nil`.
- `bool` возвращает `Bool`.
- `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long` возвращают `Int`.
- `ulong` возвращает `Int` только если значение помещается в `long`; иначе должна быть понятная ошибка переполнения.
- `float` и `double` возвращают `Float`; внутри `Variant` хранится `double`.
- `decimal`, `DateTime`, `Guid`, arbitrary CLR objects и остальные неподдержанные типы должны давать понятную ошибку `ArgumentException`.
- `As<T>()` и typed `As...()` методы должны быть строгими: если фактический `Variant.Type` не соответствует целевому типу, выбрасывается `InvalidCastException` с указанием фактического и ожидаемого типа.
- `As<T>()` для enum читает `Int` и возвращает enum через его underlying value.

## Критерии приёмки

- Unit-тесты покрывают `Nil`, primitive values, numeric normalization, enum mapping, 2D math values, identity values, `Object`/`Resource`, `Array`, `Dictionary` и ошибки неподдержанных типов.
- Public API для Variant-domain сверяется через generated API manifest и compatibility table; этот документ не задаёт общий exported runtime baseline ручным списком.
- Документация текущего поведения описывает закрытый список и ограничения `0.1-preview`.
- `Signal`, 3D-типы и stable serialization не реализуются в этой задаче.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0020`.
Обновлено: 2026-06-21.

## Источник истины для публичной поверхности

Текущая публичная поверхность Variant-domain фиксируется generated artifacts: `data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md` и проверкой `verify api-compatibility --wiki-path .github/wiki`. Этот документ описывает поведение, ограничения и проверочные сценарии `Variant`, но не является источником истины для полного списка exported runtime public types.

`Variant` - Electron2D value carrier для динамических API. Это `readonly struct`; пустое значение и создание из `null` означают `Nil`.

## Закрытый список `Variant.Type`

В `0.1-preview` поддержаны только:

- `Nil`;
- `Bool`;
- `Int`;
- `Float`;
- `String`;
- `Vector2`;
- `Vector2I`;
- `Rect2`;
- `Rect2I`;
- `Transform2D`;
- `Color`;
- `StringName`;
- `NodePath`;
- `Rid`;
- `Object`;
- `Callable`;
- `Dictionary`;
- `Array`.

`Resource` и `Node` хранятся как `Object`. Enum значения хранятся как `Int`.

## Создание значений

Поддержаны implicit conversions для:

- `bool`;
- `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`;
- `float`, `double`;
- `string?`;
- `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color`;
- `StringName`, `NodePath`, `Rid`, `Callable`;
- `ElectronObject?` и наследники, включая `Resource` и `Node`;
- `Electron2D.Collections.Array?`;
- `Electron2D.Collections.Dictionary?`.

Enum значения создаются через `Variant.From<T>()` или `Variant.CreateFrom(object?)`, по правилам текущего C# API.

## Чтение значений

Состав typed-read API берётся из generated API manifest. Все reader operations должны быть строгими: если фактический `Variant.Type` не совпадает с ожидаемым типом, выбрасывается `InvalidCastException` с указанием фактического и ожидаемого типа. Unsupported CLR values, например `decimal`, `DateTime`, `Guid` или произвольный `System.Object`, дают `ArgumentException`. `ulong`, который не помещается в `long`, даёт `ArgumentOutOfRangeException`.

## Коллекции

Electron2D collection wrappers хранят ordered list значений `Variant` или mapping `Variant` -> `Variant`. Состав их публичных members берётся из generated API manifest; этот документ фиксирует только поведение: операции должны сохранять порядок list-элементов, мутации должны быть видны всем `Variant`, которые держат один и тот же collection instance, а dictionary keys сравниваются по правилам `Variant`.

Коллекции являются mutable reference-like объектами. Если один и тот же экземпляр `Array` или `Dictionary` положен в несколько `Variant`, эти `Variant` ссылаются на один контейнер.

## Ограничения

- `Signal` пока не входит в public API и не поддержан как `Variant.Type`.
- 3D-типы и packed arrays не входят в `0.1-preview`.
- Stable text serialization round-trip реализован отдельной задачей `T-0021` как internal runtime contract.
- Signal/group/deferred/property APIs пока не мигрированы на `Variant`; они будут переводиться отдельными задачами.

## Проверки

- Red-state unit tests сначала падали на отсутствующем `Electron2D.Variant` и `Electron2D.Collections`.
- Green-state `dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --no-restore` проходит и покрывает nil, primitives, enum mapping, 2D math, identity handles, object/resource values, collections и ошибки unsupported/wrong cast.
