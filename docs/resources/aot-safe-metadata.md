# AOT-safe metadata для Inspector и serialization

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0043`.
Обновлено: 2026-06-21.
Связанные документы: [Сериализация сцен, ресурсов и переносимых property values](scene-resource-serialization.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`T-0043` заменяет reflection fallback в custom `Resource` serialization на явную metadata-модель, пригодную для будущего source generator и Inspector. Serializer не должен сам искать CLR-типы по загруженным сборкам, обходить public properties или создавать resource instance через динамический поиск конструктора.

В `0.1.0 Preview` metadata остаётся internal механизмом: публичные `[Export]`, `[Signal]`, `[Tool]`, editor Inspector UI и source generator добавляются отдельными задачами. Эта задача фиксирует runtime contract, на который они будут опираться.

## Metadata contract

Для каждого serializable custom `Resource` должен быть зарегистрирован `ResourceObjectTypeMetadata`:

- `ResourceType` - конкретный CLR type ресурса;
- `SerializedTypeName` - стабильное имя типа в `.e2res`/scene JSON;
- `factory` - typed delegate, создающий новый resource instance;
- `Properties` - отсортированный по ordinal имени список `ResourceObjectPropertyMetadata`.

Каждый property descriptor хранит:

- stable serialized name, например `display_name`;
- CLR value type для Inspector/tooling;
- getter delegate;
- setter delegate;
- converter между CLR value и `SerializedPropertyValue`.

## Требования AOT

Resource object serializer не должен использовать:

- `Reflection.Emit`;
- runtime IL generation;
- dynamic iOS code load;
- `AppDomain.CurrentDomain.GetAssemblies()` для поиска serialized type;
- reflection property discovery (`GetProperties`) для custom resource serialization;
- `Activator.CreateInstance(Type, ...)` для создания custom resource.

Регистрация metadata может быть написана вручную или сгенерирована source generator на этапе build. В обоих случаях runtime получает обычные typed delegates и не требует динамической генерации кода.

## Ошибки

Serializer должен fail closed:

- capture незарегистрированного resource type даёт `InvalidOperationException`;
- instantiate документа с незарегистрированным `type` даёт `InvalidOperationException`;
- duplicate property names в metadata дают `ArgumentException`;
- property metadata для другого resource type даёт `ArgumentException`;
- unsupported CLR value в property converter даёт ошибку вместо silent fallback.

## Проверки

- Integration tests проверяют, что custom resource round-trip использует registered metadata names, а не public property names.
- Integration tests проверяют fail-closed поведение для resource без metadata.
- Integration tests проверяют stable property order для будущего Inspector.
- `tools/Verify-AotMetadataSafety.ps1 -NativeAot` публикует smoke-приложение trimmed и NativeAOT, запускает опубликованные артефакты и проверяет round-trip через registered metadata.
- Быстрый поиск по `Core/Resources/Serialization` не должен находить forbidden APIs из раздела AOT requirements.

## Фактическое состояние, ограничения и проверки

Статус: реализованный internal baseline.
Задача: `T-0043`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен internal metadata registry для custom `Resource` serialization и будущего Inspector. `ResourceObjectSerializer` больше не обходит public get/set properties через reflection и не ищет типы по загруженным сборкам.

Текущие типы находятся в `src/Electron2D/Assets/Resources/Serialization/`:

- `ResourceObjectMetadataRegistry` - process-local реестр metadata;
- `ResourceObjectTypeMetadata` - описание конкретного serializable `Resource` type;
- `ResourceObjectPropertyMetadata` - typed property descriptor с getter/setter delegates;
- `SerializedPropertyValueConverter` - converter для scalar, enum, nullable, array и dictionary property values;
- `ResourceObjectSerializer` - capture/instantiate поверх зарегистрированной metadata.

## Runtime contract

Перед `ResourceObjectSerializer.Capture()` или `ResourceObjectSerializer.Instantiate()` код должен зарегистрировать metadata:

```csharp
ResourceObjectMetadataRegistry.Register(
    ResourceObjectTypeMetadata.Create(
        typeof(PlayerStatsResource).FullName!,
        () => new PlayerStatsResource(),
        [
            ResourceObjectPropertyMetadata.Create<PlayerStatsResource, string>(
                "display_name",
                resource => resource.DisplayName,
                (resource, value) => resource.DisplayName = value)
        ]));
```

Имена свойств берутся из metadata, а не из CLR property names. Это важно для diff-friendly resource files, Inspector labels и будущих source-generated descriptors.

Built-in resources движка регистрируются самим runtime. На 2026-06-21 это относится к `RectangleShape2D`, `CircleShape2D`, `CapsuleShape2D`, `SegmentShape2D`, `ConvexPolygonShape2D`, `ConcavePolygonShape2D` и `PhysicsMaterial`. Игра не должна регистрировать metadata для этих типов вручную.

## AOT/NativeAOT

Новый serialization metadata path не использует:

- `Reflection.Emit`;
- runtime IL generation;
- dynamic assembly scanning;
- reflection property discovery;
- `Activator.CreateInstance(Type, ...)` для custom resources.

Проверка `tools/Verify-AotMetadataSafety.ps1 -NativeAot` публикует и запускает smoke-приложение:

- trimmed self-contained artifact;
- NativeAOT artifact для текущего RuntimeIdentifier;
- round-trip custom resource через registered metadata.

На 2026-06-21 smoke проходит на `win-x64`. В выводе остаются trim warnings по старым reflective участкам `Callable`, `SceneTree` и in-memory `PackedScene`; они не относятся к `ResourceObjectSerializer`/metadata path и должны закрываться отдельными задачами.

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~SceneResourceSerializationTests"
powershell -ExecutionPolicy Bypass -File tools\Verify-AotMetadataSafety.ps1 -NativeAot
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
