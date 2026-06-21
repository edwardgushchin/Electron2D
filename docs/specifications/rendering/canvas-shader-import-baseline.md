# Canvas shaders import и diagnostics baseline

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
