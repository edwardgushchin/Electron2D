# Canvas shaders import и diagnostics baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0031`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [`RenderingServer` и renderer profiles](rendering-server.md), [SDL_GPU lifecycle baseline](sdl-gpu-lifecycle.md).

## Цель

Electron2D `0.1.0 Preview` должен получить минимальный проверяемый baseline canvas shaders:

- public Electron2D `Shader` resource для canvas shader source;
- импортный формат source-файла с vertex/fragment entry points;
- компиляцию shader stages через SDL_shadercross boundary во время import/export, а не во время запуска игры на iOS;
- structured diagnostics с file/line/column для editor/tooling;
- compiled artifact, который хранит stage bytecode по target platform.

`ShaderMaterial`, uniforms, samplers и engine-provided built-ins остаются отдельной задачей `T-0032`.

## Источники совместимости

- Godot `Shader` наследуется от `Resource`, имеет `code` и `get_mode()`, а `MODE_CANVAS_ITEM` используется для 2D drawing.
- SDL_shadercross умеет компилировать HLSL в SPIR-V/DXIL и получать MSL из SPIR-V. В Electron2D это используется как import/export-time boundary.

## Public API

Новый public surface:

```csharp
namespace Electron2D;

public sealed class Shader : Resource
{
    public enum Mode
    {
        CanvasItem = 1
    }

    public string Code { get; set; }

    public Mode GetMode();
}
```

Ограничения public API:

- `Shader` поддерживает только canvas item mode в `0.1.0 Preview`;
- `ShaderMaterial` не добавляется в `T-0031`;
- public uniforms API, sampler API и native shader inspection API не добавляются в `T-0031`;
- SDL_shadercross handles, SDL_GPU shader handles и compiled bytecode не становятся public API.

## Source format

Документированный формат `Electron2D canvas shader v1` хранит HLSL source с небольшим header:

```hlsl
shader_type canvas_item;
vertex_entry VSMain;
fragment_entry PSMain;

struct VSInput
{
    float2 position : POSITION;
};

float4 VSMain(VSInput input) : SV_Position
{
    return float4(input.position, 0.0, 1.0);
}

float4 PSMain() : SV_Target
{
    return float4(1.0, 1.0, 1.0, 1.0);
}
```

Import parser должен:

- принимать только `shader_type canvas_item;`;
- требовать `vertex_entry <identifier>;`;
- требовать `fragment_entry <identifier>;`;
- сохранять line mapping: строки header заменяются пустыми строками перед передачей в compiler, чтобы line numbers compiler diagnostics совпадали с исходным файлом;
- возвращать diagnostic вместо silent fallback при unsupported mode, отсутствующем entry point или ошибке compiler.

## Internal import contract

Внутренний механизм означает код движка, import/export tooling и тестовый host, но не пользовательский public API.

Минимальные internal types:

- `CanvasShaderImportPipeline`;
- `CanvasShaderImportRequest`;
- `CanvasShaderImportResult`;
- `CanvasShaderDiagnostic`;
- `CanvasShaderCompiledStage`;
- `CanvasShaderStage`;
- `CanvasShaderTargetPlatform`;
- `ICanvasShaderCompiler`;
- `SdlShaderCrossCompiler`.

Import result должен содержать:

- `Success`;
- `RequiresRuntimeCompilation`;
- `Diagnostics`;
- compiled stages для vertex/fragment и target platforms.

Для iOS `RequiresRuntimeCompilation` должен быть `false`, если import завершился успешно и artifact содержит iOS target stages. Runtime не должен запускать SDL_shadercross на iOS.

## Diagnostics

Diagnostic должен содержать:

- `Severity`;
- `FilePath`;
- `Line`;
- `Column`;
- `Message`;
- `Stage`;
- `TargetPlatform`.

Compiler diagnostics в форме `path(line,column): error ...` должны маппиться в эти поля. Если compiler возвращает ошибку без line/column, `Line` и `Column` равны `0`, но `FilePath`, `Stage`, `TargetPlatform` и `Message` должны сохраняться.

## Dependency policy

Runtime project может ссылаться на managed `SDL3-CS` shadercross bindings. Native SDL_shadercross package подключается как runtime/import dependency, но platform-specific export packaging остаётся задачами export/tooling.

## Resource import cache integration

Задача `T-0040` должна подключить этот shader import contract к общему resource import cache. `.e2shader` source file становится source asset, optional sidecar `<shader>.e2import.json` задаёт target platforms, а cache artifact `shader.e2shader.json` хранит compiled stages и diagnostics в stable JSON.

Этот слой не должен добавлять новый public API. Он только делает результат `CanvasShaderImportPipeline` доступным будущему editor/export pipeline через cache artifact.

## Проверки

Минимальный acceptance набор:

- unit test `Shader` public API: `Code`, `GetMode()` и отсутствие `ShaderMaterial` до `T-0032`;
- integration test успешного import: vertex/fragment stages компилируются fake compiler для desktop и iOS targets;
- integration test compiler diagnostic: file/line/column/stage/target сохраняются;
- runtime smoke test: successful iOS artifact имеет `RequiresRuntimeCompilation == false`;
- API compatibility verifier отражает `Shader` и `Shader.Mode` в GitHub Wiki source;
- source license verifier проходит для новых C# files.

## Фактическое состояние, ограничения и проверки

Статус: реализовано.
Задача: `T-0031`.
Обновлено: 2026-06-21.

## Public API

В runtime добавлен Electron2D public resource:

- `Shader`.

`Shader` наследуется от `Resource` и содержит:

- `Code`;
- `GetMode()`;
- nested enum `Shader.Mode`;
- `Shader.Mode.CanvasItem`.

В `0.1.0 Preview` поддерживается только canvas item shader mode. `Shader.Code` хранит исходный текст как пользователь написал его, а `GetMode()` возвращает `Shader.Mode.CanvasItem`.

`ShaderMaterial`, uniforms, samplers и reserved canvas built-ins реализованы отдельным baseline [ShaderMaterial, uniforms, samplers и canvas built-ins baseline](shader-material-baseline.md). Default texture parameters, global uniforms и native shader inspection остаются будущими renderer/resource задачами.

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

## Связь с import cache

`T-0040` подключает canvas shader import к общему resource import cache через `ShaderSourceImporter`. Файлы `.e2shader` импортируются в `shader.e2shader.json`, где сохраняются compiled stages, diagnostics и `requiresRuntimeCompilation`.

Sidecar `<shader>.e2import.json` может ограничить список target platforms. Если sidecar меняется, import cache переимпортирует shader как изменённую dependency. Подробный формат artifact описан в [Импорт shader source в platform-specific artifacts](../resources/shader-source-import.md).

## Ограничения

- `ShaderMaterial` resource layer реализован отдельно, но не привязан к реальному draw pipeline.
- Default texture parameters и global uniforms не реализованы.
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
