# Импорт shader source в platform-specific artifacts

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
