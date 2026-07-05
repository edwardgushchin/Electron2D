# Import cache ресурсов

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0036`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md), [Resource file baseline](resource-file-baseline.md), [Документация реализации](resource-import-cache.md).

## Назначение

`T-0036` вводит import cache: внутренний механизм движка и будущего редактора, который строит производные файлы из исходных ресурсов проекта. Исходные файлы остаются в папке проекта как source assets, а результат импорта хранится отдельно в cache root. Это нужно, чтобы generated files не смешивались с ручными файлами, чтобы cache можно было удалить и пересоздать, и чтобы ошибки импорта не уничтожали последнюю рабочую версию ресурса.

В `0.1-preview` этот механизм не добавляет public `ResourceLoader`, public `ResourceSaver`, file watcher или editor FileSystem dock. Он является внутренним кодом, доступным тестам и будущим инструментам через `InternalsVisibleTo`.

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

## Фактическое состояние, ограничения и проверки

Статус: реализованный internal baseline.
Задача: `T-0036`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен внутренний import cache для ресурсов Electron2D `0.1-preview`. Внутренний означает, что это код runtime assembly для тестов, будущего редактора и инструментов, но не новый пользовательский public API.

Текущие типы находятся в `src/Electron2D/Assets/Resources/Importing/`:

- `ResourceImportPipeline` - выполняет discovery, import, reimport, dependency checks и prune;
- `ResourceImportOptions` - задаёт `projectRoot`, `sourceRoot`, `cacheRoot` и список importers;
- `IResourceImporter` - внутренний контракт importer;
- `ResourceFileImporter` - baseline importer для `.e2res`;
- `TextureImageImporter` - importer для PNG/JPEG image metadata;
- `FontImporter` - importer для TTF/OTF font metadata;
- `ShaderSourceImporter` - importer для `.e2shader` compiled shader metadata;
- `ResourceImportManifest` и `ResourceImportManifestTextSerializer` - stable JSON manifest.

## Поведение pipeline

Pipeline сканирует `sourceRoot` и строит `res://...` path относительно этой папки. Папки generated/cache/build output не сканируются: `.electron2d`, `.git`, `.temp`, `bin`, `obj`, `artifacts`, `publish`, `packages`, `TestResults`, `coverage`.

Cache root задаётся явно. Default-настройка использует:

```text
<projectRoot>/.electron2d/import-cache/
```

Manifest хранится в:

```text
<cacheRoot>/import-cache.json
```

Artifacts хранятся отдельно от source assets:

```text
<cacheRoot>/resources/<uid>/resource.e2res
```

## `.e2res` importer

`ResourceFileImporter` поддерживает `.e2res` source files:

- читает source через `ResourceFileTextSerializer`;
- валидирует формат `.e2res`;
- записывает нормализованный stable JSON как cache artifact;
- берёт UID и type из `.e2res`;
- превращает external references в dependencies.

Dependencies отслеживаются по SHA-256 hash. Если source file не изменился, но изменился один из зависимых файлов, pipeline возвращает reimport reason `DependencyChanged` и заново пишет artifact.

## Сохранность cache при ошибке

Если source asset стал невалидным после успешного импорта, pipeline возвращает item status `Failed`, но:

- старый cache artifact остаётся на месте;
- старый manifest entry остаётся в manifest;
- corrupted source не перезаписывает предыдущий валидный результат.

Если source asset удалён, pipeline считает его unused cache и удаляет manifest entry вместе с его cache artifacts.

Если source asset перенесён на новый path с тем же UID, новый manifest entry удерживает cache artifact, а prune старого source path не удаляет этот artifact. Это защищает rename/move workflow от потери cache file между import и prune.

## Текущие ограничения

- Public `ResourceLoader`/`ResourceSaver` ещё не реализованы.
- File watcher и editor FileSystem dock ещё не реализованы.
- WAV/OGG importer реализуется следующими задачами.
- PNG/JPEG importer уже реализован отдельной задачей `T-0037` и описан в `texture-image-import.md`.
- TTF/OTF importer уже реализован отдельной задачей `T-0038` и описан в `font-import.md`.
- Shader source importer уже реализован отдельной задачей `T-0040` и описан в `shader-source-import.md`.
- Stress data stability gate реализован отдельной задачей `T-0042` и описан в `data-stability-stress.md`.
- Dependency tracking сейчас работает для файлов, которые можно разрешить через `res://...` внутри `sourceRoot`.

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ResourceImportCacheTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~ResourceImportManifestGoldenTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
