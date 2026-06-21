# Импорт shader source в platform-specific artifacts

Статус: реализованный internal baseline.
Задача: `T-0040`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен внутренний shader source importer для import cache. Внутренний означает, что код находится внутри runtime assembly и доступен тестам, будущему редактору, export pipeline и будущим инструментам, но не добавляет новые пользовательские public классы.

Текущие типы находятся в `src/Electron2D/Assets/Resources/Importing/`:

- `ShaderSourceImporter` - importer для `.e2shader`;
- `ShaderImportMetadata` - stable cache metadata model;
- `ShaderImportCompiledStage` - metadata compiled stage;
- `ShaderImportMetadataTextSerializer` - stable JSON serializer.

`ShaderSourceImporter` использует уже реализованный `CanvasShaderImportPipeline` и `ICanvasShaderCompiler`. Default `ResourceImportOptions.CreateDefault()` теперь регистрирует `ResourceFileImporter`, `TextureImageImporter`, `FontImporter` и `ShaderSourceImporter`.

## Sidecar настройки

Optional sidecar хранится рядом с shader source:

```text
res://shaders/water.e2shader.e2import.json
```

Sidecar является source data. Если он меняется, import cache видит это как изменение dependency и переимпортирует shader.

Поддерживаемое поле:

- `targets`: список target platforms: `Windows`, `Linux`, `MacOS`, `Android`, `Ios`.

Если sidecar отсутствует, importer компилирует source для всех пяти default targets. Пустой список `targets` и неизвестное имя target считаются ошибкой импорта.

## Cache artifact

Importer пишет:

```text
<cacheRoot>/resources/<uid>/shader.e2shader.json
```

Artifact содержит:

- source path;
- stable UID;
- `requiresRuntimeCompilation`;
- compiled stages с `stage`, `target`, `entryPoint` и base64 bytecode;
- diagnostics с `severity`, `file`, `line`, `column`, `message`, `stage` и `target`.

Compiled stages сортируются стабильно, чтобы artifact было удобно сравнивать в diff. Diagnostics тоже сортируются стабильно.

## Diagnostics и iOS

Compiler output вида:

```text
res://shaders/bad.e2shader(7,13): error X3000: unexpected token
```

сохраняется как structured diagnostic с `file`, `line`, `column`, `stage` и `target`. Это нужно будущему editor/import dock: пользователь сможет получить позицию ошибки без парсинга произвольного текста.

Если import для `Ios` успешен, artifact содержит vertex и fragment stages для `Ios`, `HasPrecompiledArtifacts(CanvasShaderTargetPlatform.Ios)` возвращает `true`, а `requiresRuntimeCompilation` равно `false`. Это фиксирует правило: iOS/export использует заранее созданные artifacts, а не компиляцию shader source в runtime.

## Текущие ограничения

- Public `ResourceLoader`/`ResourceSaver` ещё не реализованы.
- Export packaging ещё не берёт artifacts из cache автоматически.
- Runtime draw pipeline ещё не привязан к shader/material GPU artifacts.
- Compatibility backend не обязан поддерживать custom shaders.

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ShaderSourceImportCacheTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~ShaderImportMetadataGoldenTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
