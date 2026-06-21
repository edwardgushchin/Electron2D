# Stress data stability для scene/resource pipeline

Статус: целевая спецификация для `T-0042`.
Обновлено: 2026-06-21.
Связанные документы: [Сериализация сцен, ресурсов и переносимых property values](scene-resource-serialization.md), [Import cache ресурсов](resource-import-cache.md), [Resource file baseline](resource-file-baseline.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`T-0042` вводит stress gate для данных `0.1.0 Preview`: многократный save/load, rename/move ресурсов, rebuild import cache и сценарии повреждения файлов должны быть проверяемыми автоматическими тестами. Цель - поймать silent property loss до того, как будут добавлены public loader/saver, editor и project tooling.

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
