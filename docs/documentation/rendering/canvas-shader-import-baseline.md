# Canvas shaders import и diagnostics baseline

Статус: реализовано.
Задача: `T-0031`.
Обновлено: 2026-06-21.

## Public API

В runtime добавлен Godot-like public resource:

- `Shader`.

`Shader` наследуется от `Resource` и содержит:

- `Code`;
- `GetMode()`;
- nested enum `Shader.Mode`;
- `Shader.Mode.CanvasItem`.

В `0.1.0 Preview` поддерживается только canvas item shader mode. `Shader.Code` хранит исходный текст как пользователь написал его, а `GetMode()` возвращает `Shader.Mode.CanvasItem`.

`ShaderMaterial`, uniforms, samplers, default texture parameters и native shader inspection не добавлены в T-0031. Эти части относятся к T-0032 и следующим renderer/resource задачам.

## Source format

Первый документированный формат называется `Electron2D canvas shader v1`. Это HLSL source с header:

```hlsl
shader_type canvas_item;
vertex_entry VSMain;
fragment_entry PSMain;
```

После header идёт обычный HLSL-код. Parser принимает только `shader_type canvas_item;`, потому что 3D, particles, sky/fog и compute shaders не входят в `0.1.0 Preview`.

Header lines заменяются пустыми строками перед передачей source в compiler. Так diagnostics от compiler сохраняют line numbers исходного файла.

## Внутренний import pipeline

Внутренний механизм означает код движка, import/export tooling и тестовый host, но не пользовательский public API.

Реализованы:

- `CanvasShaderImportPipeline`;
- `CanvasShaderImportRequest`;
- `CanvasShaderImportResult`;
- `CanvasShaderDiagnostic`;
- `CanvasShaderCompiledStage`;
- `CanvasShaderStage`;
- `CanvasShaderTargetPlatform`;
- `ICanvasShaderCompiler`;
- `SdlShaderCrossCompiler`.

`CanvasShaderImportPipeline` компилирует vertex и fragment stages для запрошенных target platforms. Успешный результат содержит compiled stages и `RequiresRuntimeCompilation == false`.

`SdlShaderCrossCompiler` использует `SDL3.ShaderCross`:

- Windows target получает DXIL bytecode;
- Linux и Android получают SPIR-V bytecode;
- macOS и iOS получают MSL source, полученный из SPIR-V.

Runtime project ссылается на `SDL3-CS.Native.Shadercross` `3.0.0`, чтобы import/export host мог загрузить native SDL_shadercross libraries.

## Diagnostics

Compiler output формата:

```text
res://shaders/bad.e2shader(7,13): error X3000: unexpected token
```

преобразуется в structured diagnostic:

- severity;
- file path;
- line;
- column;
- message;
- stage;
- target platform.

Если compiler вернул ошибку без file/line/column, diagnostic сохраняет исходный message, stage и target platform, но line/column остаются `0`.

## iOS policy

Успешный iOS import создаёт compiled vertex/fragment stages заранее и возвращает `RequiresRuntimeCompilation == false`. Игра на iOS не должна запускать SDL_shadercross во время runtime.

## Ограничения

- Public `ShaderMaterial` не реализован.
- Uniform metadata, samplers и engine-provided built-ins не реализованы.
- Реальная привязка shader artifact к draw pipeline ещё не реализована.
- Compatibility backend не обязан поддерживать custom shaders.
- Visual shader editor не входит в `0.1.0 Preview`.

## Проверки

Целевые focused-команды:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~ShaderPublicApiTests|FullyQualifiedName~CleanRuntimeBaselineTests" --no-restore
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~CanvasShaderImportTests" --no-restore
dotnet test tests\Electron2D.Tests.RuntimeSmoke\Electron2D.Tests.RuntimeSmoke.csproj --filter "FullyQualifiedName~CanvasShaderImportSmokeTests" --no-restore
```

Полный release-gate runner:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
