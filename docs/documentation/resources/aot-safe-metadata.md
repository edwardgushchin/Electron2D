# AOT-safe metadata для Inspector и serialization

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
