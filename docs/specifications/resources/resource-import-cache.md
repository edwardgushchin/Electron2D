# Import cache ресурсов

Статус: целевая спецификация для `T-0036`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Resource file baseline](resource-file-baseline.md), [Документация реализации](../../documentation/resources/resource-import-cache.md).

## Назначение

`T-0036` вводит import cache: внутренний механизм движка и будущего редактора, который строит производные файлы из исходных ресурсов проекта. Исходные файлы остаются в папке проекта как source assets, а результат импорта хранится отдельно в cache root. Это нужно, чтобы generated files не смешивались с ручными файлами, чтобы cache можно было удалить и пересоздать, и чтобы ошибки импорта не уничтожали последнюю рабочую версию ресурса.

В `0.1.0 Preview` этот механизм не добавляет public `ResourceLoader`, public `ResourceSaver`, file watcher или editor FileSystem dock. Он является внутренним кодом, доступным тестам и будущим инструментам через `InternalsVisibleTo`.

## Контракт

- Pipeline получает `projectRoot`, `sourceRoot`, `cacheRoot` и набор importers.
- Source discovery рекурсивно обходит `sourceRoot`, но не заходит в `cacheRoot`, `.electron2d`, `.git`, `.temp`, `bin`, `obj`, package/build/test output folders.
- Source path переводится в `res://...` относительно `sourceRoot`.
- Cache manifest хранится как stable JSON файл `import-cache.json` внутри `cacheRoot`.
- Cache artifacts хранятся внутри `cacheRoot/resources/<uid>/...`, а не рядом с исходным файлом.
- Первый importer baseline поддерживает `.e2res`: он читает `ResourceFileTextSerializer`, нормализует JSON и записывает нормализованный артефакт `resource.e2res`.
- UID берётся из `.e2res` документа и записывается в manifest как `uid://...`.
- External references из `.e2res` становятся dependencies: pipeline сохраняет SHA-256 hash каждого зависимого `res://...` файла.
- Повторный запуск без изменений возвращает status `UpToDate`.
- Изменение source file, изменение dependency hash, смена importer или пропавший cache artifact приводят к reimport.
- Если import source file завершается ошибкой, предыдущий валидный cache artifact и manifest entry остаются неизменными.
- Если source file удалён, его manifest entry удаляется, а связанные cache artifacts удаляются.
- Если source file перенесён на новый path с тем же UID, prune старого source path не должен удалять cache artifacts, которые уже удерживаются новым manifest entry.

## Формат manifest

Top-level поля:

1. `format` - строка `Electron2D.ImportCache`;
2. `version` - целое число версии формата, для этой задачи `1`;
3. `entries` - массив импортированных source assets.

Entry хранит:

- `source` - `res://...` путь исходного файла;
- `uid` - `uid://...` главного ресурса;
- `type` - тип ресурса;
- `importer` - стабильное имя importer;
- `sourceHash` - SHA-256 hash исходного файла;
- `cacheFiles` - относительные пути cache artifacts внутри `cacheRoot`;
- `dependencies` - пары `path`/`hash` для зависимостей.

Сериализация должна быть stable: entries сортируются по `source`, dependencies по `path`, cache files по пути.

## Ошибки и сохранность данных

Pipeline должен fail closed: malformed `.e2res`, отсутствующая dependency, path traversal через `res://../...`, ошибка чтения или записи не должны приводить к silent data loss.

Запись cache artifact выполняется через temporary file и replace. Manifest также пишется через temporary file. При ошибке импорта для source, который уже был успешно импортирован раньше, старый cache file остаётся на месте, а старый manifest entry сохраняется.

## Критерии приёмки

- Integration tests проверяют discovery `.e2res`, запись cache вне source root и manifest entry.
- Integration tests проверяют повторный запуск без изменений и reimport при изменении dependency.
- Integration tests проверяют, что malformed source не портит предыдущий валидный cache artifact и manifest.
- Integration tests проверяют prune для удалённого source asset.
- Golden-data test фиксирует exact JSON output `import-cache.json`.
- Документация реализации описывает текущие ограничения и команды проверки.
