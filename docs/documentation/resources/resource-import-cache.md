# Import cache ресурсов

Статус: реализованный internal baseline.
Задача: `T-0036`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен внутренний import cache для ресурсов Electron2D `0.1.0 Preview`. Внутренний означает, что это код runtime assembly для тестов, будущего редактора и инструментов, но не новый пользовательский public API.

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
