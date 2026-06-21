# AOT-safe metadata для Inspector и serialization

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
