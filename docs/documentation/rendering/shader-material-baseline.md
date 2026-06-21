# ShaderMaterial, uniforms, samplers и canvas built-ins baseline

Статус: реализовано.
Задача: `T-0032`.
Обновлено: 2026-06-21.

## Public API

В runtime добавлены Electron2D public ресурсы:

- `Material`;
- `ShaderMaterial`.

`Material` наследуется от `Resource` и содержит:

- `NextPass`;
- `RenderPriority`.

`RenderPriority` принимает значения от `-128` до `127`. В `0.1.0 Preview` значение сохраняется для будущего renderer ordering, но текущая 2D очередь всё ещё сортирует команды по canvas layer, z-index, y-sort и tree order.

`ShaderMaterial` наследуется от `Material` и содержит:

- `Shader`;
- `SetShaderParameter(StringName param, Variant value)`;
- `GetShaderParameter(StringName param)`.

Parameter names чувствительны к регистру. `Tint` и `tint` хранят разные значения. Если параметр не задан, `GetShaderParameter()` возвращает nil `Variant`. Запись nil `Variant` удаляет ранее сохранённое значение.

## Supported uniforms

`SetShaderParameter()` принимает только значения, которые входят в baseline shader uniform subset:

- `Bool`;
- `Int`;
- `Float`;
- `Vector2`;
- `Color`;
- `Transform2D`;
- `Texture2D` как sampler parameter.

Обычные `Object`-значения, `Callable`, `Rid`, `String`, `StringName`, `NodePath`, коллекции, `Rect2`, `Rect2I`, `Vector2I` и другие значения отклоняются сразу через `ArgumentException`. Это fail-closed поведение: renderer не получает неподдерживаемые параметры и не заменяет их молча default-значениями.

## Texture samplers

`Texture2D` можно передать как значение shader parameter:

```csharp
var material = new ShaderMaterial
{
    Shader = shader
};

material.SetShaderParameter("albedo_texture", texture);
```

В runtime `GetShaderParameter("albedo_texture")` возвращает тот же `Texture2D` внутри `Variant.Type.Object`.

Для tooling и будущего resource pipeline создан внутренний serializable snapshot. Внутренний означает код движка, доступный тестам, будущему редактору и import/export pipeline, но не пользовательский public API.

Snapshot sampler entry содержит:

- parameter name;
- kind `Texture2D`;
- runtime type name;
- `ResourcePath`;
- `ResourceSceneUniqueId`;
- width/height;
- alpha flag;
- mipmap flag;
- mipmap count.

Если texture не имеет `ResourcePath`, snapshot всё равно сериализуется с пустым путём. Полная stable resource reference запись относится к будущему import cache/resource pipeline.

## Serializable snapshot

Внутренние типы:

- `ShaderMaterialParameterKind`;
- `ShaderMaterialParameterSnapshot`;
- `ShaderMaterialParametersSnapshot`;
- `ShaderMaterialParameterTextSerializer`;
- `ShaderMaterialParameterValidator`;
- `CanvasShaderBuiltInRegistry`.

`ShaderMaterialParameterTextSerializer` пишет JSON:

- `format = "Electron2D.ShaderMaterialParameters"`;
- `version = 1`;
- `shader` как `Shader.ResourcePath`;
- `parameters` в stable ordinal order.

Value uniforms сериализуются через действующий stable `VariantTextSerializer`. Texture samplers пишутся отдельным entry, потому что stable `VariantTextSerializer` намеренно не сериализует произвольные `Object`-значения.

## Reserved canvas built-ins

`CanvasShaderBuiltInRegistry` хранит reserved names canvas shader model, включая `TIME`, `PI`, `TAU`, `E`, `MODEL_MATRIX`, `CANVAS_MATRIX`, `SCREEN_MATRIX`, `VERTEX`, `UV`, `COLOR`, `TEXTURE`, `TEXTURE_PIXEL_SIZE`, `NORMAL_TEXTURE`, light built-ins и SDF helper functions.

`SetShaderParameter()` отклоняет exact-case совпадения с этими именами. Например, `TEXTURE` и `texture_sdf` нельзя записать как user parameter. `time` не совпадает с `TIME`, потому что shader parameter names case-sensitive.

## Ограничения

- `CanvasItem.Material` ещё не добавлен: T-0032 закрывает resource/model layer, а привязка материала к draw pipeline остаётся следующими renderer tasks.
- Реальный GPU binding uniforms/samplers в SDL_GPU pipeline ещё не реализован.
- Default texture parameter hints, global uniforms, per-instance uniforms, visual shader editor и native shader inspection не входят в этот baseline.
- Compatibility backend не обязан поддерживать custom shader execution.

## Проверки

Целевые focused-команды:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~ShaderMaterialPublicApiTests|FullyQualifiedName~CleanRuntimeBaselineTests" --no-restore
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ShaderMaterialParameterTests" --no-restore
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~ShaderMaterialParameterGoldenTests" --no-restore
```

Полный release-gate runner:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
