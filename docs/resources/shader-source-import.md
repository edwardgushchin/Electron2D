# Импорт shader source в platform-specific artifacts

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0040`.
Обновлено: 2026-06-21.
Связанные документы: [Import cache ресурсов](resource-import-cache.md), [Canvas shaders import и diagnostics baseline](../rendering/canvas-shader-import-baseline.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`T-0040` подключает canvas shader source к общему import cache. Source asset `.e2shader` должен импортироваться в стабильный cache artifact с platform-specific compiled stages, structured diagnostics и явным флагом, нужна ли runtime compilation.

В `0.1.0 Preview` задача не добавляет public `ResourceLoader`, public `ShaderFile`, public GPU shader handles и не меняет public `Shader` API. Результат является внутренним механизмом для тестов, будущего редактора, export pipeline и будущего loader.

## Source assets и sidecar

Importer поддерживает:

- `.e2shader`.

Source format остаётся форматом из canvas shader baseline:

```hlsl
shader_type canvas_item;
vertex_entry VSMain;
fragment_entry PSMain;
```

Настройки импорта хранятся в optional sidecar рядом с shader source:

```text
res://shaders/water.e2shader.e2import.json
```

Sidecar является source data, то есть редактируемым файлом проекта. Он не находится в import cache. Если sidecar меняется, shader asset должен переимпортироваться через dependency tracking.

Минимальный sidecar формат:

```json
{
  "targets": [
    "Windows",
    "Ios"
  ]
}
```

Если sidecar отсутствует, importer компилирует shader для default targets:

- `Windows`;
- `Linux`;
- `MacOS`;
- `Android`;
- `Ios`.

Пустой список `targets` является ошибкой импорта. Неизвестное имя target platform является ошибкой импорта.

## Metadata artifact

Importer пишет один cache artifact:

```text
resources/<uid>/shader.e2shader.json
```

Artifact format:

- `format`: `Electron2D.ShaderImportMetadata`;
- `version`: `1`;
- `source`: исходный `res://...`;
- `uid`: stable `uid://...`, созданный из source path;
- `requiresRuntimeCompilation`: `false`, если все requested stages скомпилированы заранее;
- `stages`: compiled vertex/fragment stages по target platform;
- `diagnostics`: structured diagnostics.

JSON output должен быть stable: stages сортируются по target platform, stage и entry point; diagnostics сортируются по file/line/column/message.

## Diagnostics

Importer обязан сохранять diagnostics в artifact, а не терять их в human-only строке. Diagnostic содержит:

- severity;
- file;
- line;
- column;
- message;
- stage;
- target.

Если compiler output содержит `path(line,column): error ...`, `file`, `line` и `column` должны попасть в artifact. Если compiler output не содержит позицию, `line` и `column` остаются `0`, но message, stage и target сохраняются.

## iOS/export policy

Успешный import для `Ios` должен создавать vertex и fragment stage artifacts заранее. Для такого target `ShaderImportMetadata.HasPrecompiledArtifacts(CanvasShaderTargetPlatform.Ios)` должен возвращать `true`, а `requiresRuntimeCompilation` должен быть `false`.

Export pipeline в следующих задачах должен брать iOS shader data из import cache. Runtime на iOS не должен запускать SDL_shadercross для пользовательского shader source.

## Критерии приёмки

- Integration tests проверяют successful `.e2shader` import, cache artifact и наличие vertex/fragment stages для `Ios`.
- Integration tests проверяют, что compiler diagnostics сохраняют file/line/column/stage/target.
- Integration tests проверяют sidecar как dependency: изменение target list вызывает `DependencyChanged` reimport.
- Golden-data test фиксирует exact JSON output `ShaderImportMetadata`.
- Public API compatibility не меняется.
- Source license verifier проходит для новых C# files.

## Фактическое состояние, ограничения и проверки

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
