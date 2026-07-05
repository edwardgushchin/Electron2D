# Stress data stability для scene/resource pipeline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0042`.
Обновлено: 2026-06-21.
Связанные документы: [Сериализация сцен, ресурсов и переносимых property values](scene-resource-serialization.md), [Import cache ресурсов](resource-import-cache.md), [Resource file baseline](resource-file-baseline.md), [Electron2D 0.1-preview](../releases/0.1-preview.md).

## Назначение

`T-0042` вводит stress gate для данных `0.1-preview`: многократный save/load, rename/move ресурсов, rebuild import cache и сценарии повреждения файлов должны быть проверяемыми автоматическими тестами. Цель - поймать silent property loss до того, как будут добавлены public loader/saver, editor и project tooling.

## Проверяемые сценарии

### 100 save/load cycles

Stable scene/resource serializers должны выдерживать 100 последовательных циклов:

```text
serialize -> deserialize -> serialize
```

Финальный текст должен совпадать с исходным canonical JSON. Это проверяет, что массивы, словари, enum, nullable и resource reference slots не теряют данные и не меняют порядок полей.

### Rename/move resources

Resource UID должен переживать перенос source asset на новый `res://...` path:

- исходный `.e2res` импортируется и получает cache artifact;
- source asset переносится в другой путь с тем же UID и обновлённым fallback path;
- import cache создаёт entry для нового source path и pruning entry для старого source path;
- UID в manifest и cache artifact остаётся тем же.

### Import cache rebuild

Если cache root удалён, повторный import должен пересоздать manifest и cache artifacts из source assets без изменения UID и без записи generated data рядом с source files.

### Corruption diagnostics

Если source asset повреждён после успешного import:

- import item получает status `Failed`;
- error message содержит понятную диагностику malformed/corrupted input;
- предыдущий валидный cache artifact остаётся на диске;
- manifest не теряет старую валидную запись.

## Границы

`T-0042` не добавляет public API и не реализует public `ResourceLoader`/`ResourceSaver`. Это проверочный release gate поверх internal formats и import cache. Если stress tests выявляют дефект, задача должна включить минимальное исправление production code и обновление документации.

## Критерии приёмки

- Integration tests проверяют 100 save/load cycles для scene/resource documents.
- Integration tests проверяют rename/move resource с сохранением UID и pruning старого source path.
- Integration tests проверяют import cache rebuild после удаления cache root.
- Integration tests проверяют corruption diagnostics без silent data loss.
- Документация реализации описывает команды проверки и текущие ограничения.

## Фактическое состояние, ограничения и проверки

Статус: реализованный release-gate baseline.
Задача: `T-0042`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен stress gate для данных `0.1-preview`. Он проверяет, что scene/resource serializers и import cache не теряют значения, ссылки и cache artifacts в сценариях, которые часто ломают проектные форматы.

Текущий набор проверок находится в `tests/Electron2D.Tests.Integration/DataStabilityStressTests.cs`.

## Проверяемые сценарии

### 100 save/load cycles

`SerializedResourceTextSerializer` и `SceneFileTextSerializer` проходят 100 последовательных циклов:

```text
serialize -> deserialize -> serialize
```

Финальный canonical JSON должен совпасть с исходным. Проверка включает arrays, dictionaries, enum, nullable и resource reference slots.

### Rename/move resources

Проверяется перенос `.e2res` source asset на новый `res://...` path с тем же UID:

- старый source path импортируется;
- файл переносится на новый path и сохраняет UID;
- новый path импортируется;
- старый path попадает в prune report;
- cache artifact с тем же UID не удаляется prune-этапом;
- manifest указывает на новый source path и прежний UID.

В рамках T-0042 исправлен defect import cache: prune старого source entry больше не удаляет cache file, если этот file уже удерживается новым manifest entry. Это важно для rename/move ресурсов с одинаковым UID.

### Import cache rebuild

Если cache root удалён, следующий import пересоздаёт manifest и artifacts из source files. UID остаётся прежним, а generated data не появляется внутри source root.

### Corruption diagnostics

Если source asset повреждён после успешного import:

- item получает status `Failed`;
- error message содержит диагностику malformed input;
- предыдущий валидный artifact остаётся на диске;
- manifest не теряет старую валидную запись.

## Текущие ограничения

- Это internal release gate, а не public loader/saver.
- Stress gate пока покрывает text documents и `.e2res` import cache; binary/audio/gpu asset stability добавляются в соответствующих importer/export задачах.
- Проверка corruption diagnostics сейчас ориентирована на malformed JSON `.e2res`.

## Проверки

Сфокусированная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~DataStabilityStressTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
