# ShaderMaterial, uniforms, samplers и canvas built-ins baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0032`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md), [Canvas shaders import и diagnostics baseline](canvas-shader-import-baseline.md), [Texture2D resource baseline](texture-resource-baseline.md), [Resource file baseline](../resources/resource-file-baseline.md).

## Цель

Electron2D `0.1-preview` должен получить минимальный проверяемый baseline для материалов canvas shaders:

- public Electron2D `Material` как базовый resource для визуальных материалов;
- public Electron2D `ShaderMaterial`, который наследуется от `Material`, хранит `Shader` и управляет значениями shader uniforms через `StringName`;
- поддерживаемые uniform values сохраняются в stable internal snapshot, пригодный для будущего resource pipeline;
- `Texture2D` может быть назначен как texture sampler parameter;
- engine-provided canvas built-ins известны движку как reserved names и не могут быть записаны как пользовательские shader parameters;
- unsupported values fail closed: ошибка возникает сразу при записи параметра, а не позднее в renderer.

## Источники совместимости

- Godot `ShaderMaterial` наследуется от `Material`, хранит `Shader` и предоставляет `get_shader_parameter(StringName)` / `set_shader_parameter(StringName, Variant)`: <https://docs.godotengine.org/en/4.5/classes/class_shadermaterial.html>.
- Godot `Material` является базовым resource для материалов, имеет `next_pass` и `render_priority`: <https://docs.godotengine.org/en/4.5/classes/class_material.html>.
- Godot canvas item shaders описывают global, vertex, fragment и light built-ins, которые не являются пользовательскими uniforms: <https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/canvas_item_shader.html>.

## Public API

Новый public surface:

```csharp
namespace Electron2D;

public abstract class Material : Resource
{
    public Material? NextPass { get; set; }

    public int RenderPriority { get; set; }
}

public sealed class ShaderMaterial : Material
{
    public Shader? Shader { get; set; }

    public Variant GetShaderParameter(StringName param);

    public void SetShaderParameter(StringName param, Variant value);
}
```

Ограничения public API:

- `Material` и `ShaderMaterial` добавляются только в Electron2D форме;
- public `SetUniform`, `GetUniform`, `Effect`, `SamplerState`, `IComponent`, renderer handle или compatibility wrapper не добавляются;
- public API не раскрывает SDL_GPU/SDL_shadercross handles и compiled shader bytecode;
- `CanvasItem.Material` не добавляется в T-0032, потому что эта задача закрывает resource/model layer, а привязка к draw pipeline относится к следующим renderer tasks.

## Uniform parameter contract

`ShaderMaterial.SetShaderParameter()` должен:

- требовать непустой `StringName`;
- хранить parameter names case-sensitive, то есть `Tint` и `tint` являются разными параметрами;
- принимать `default(Variant)` / `Variant.Type.Nil` как сброс значения, после которого `GetShaderParameter()` возвращает nil;
- принимать scalar/vector values из поддерживаемого shader subset:
  - `Bool`;
  - `Int`;
  - `Float`;
  - `Vector2`;
  - `Color`;
  - `Transform2D`;
- принимать `Variant.Type.Object`, только если объект является `Texture2D`; это texture sampler parameter;
- отклонять `Object`, который не является `Texture2D`;
- отклонять `Callable`, `Rid`, `String`, `StringName`, `NodePath`, `Dictionary`, `Array`, `Rect2`, `Rect2I`, `Vector2I` и другие значения, которые не входят в shader uniform subset T-0032.

`ShaderMaterial.GetShaderParameter()` должен возвращать ранее записанное значение или nil, если параметр не задан.

## Texture sampler contract

Texture sampler parameter хранит ссылку на `Texture2D` в runtime value и serializable snapshot для tooling.

Snapshot sampler entry должен содержать:

- parameter name;
- kind `Texture2D`;
- texture type name;
- `ResourcePath`;
- `ResourceSceneUniqueId`;
- texture width;
- texture height;
- `HasAlpha()`;
- `HasMipmaps()`;
- `GetMipmapCount()`.

Если texture не имеет `ResourcePath`, snapshot всё равно сериализуем: путь записывается пустой строкой. Это временное ограничение до полного import cache/resource reference pipeline.

## Serializable snapshot contract

Внутренний механизм означает код движка, доступный тестам, будущему редактору и resource pipeline, но не пользовательский public API.

Минимальные internal types:

- `ShaderMaterialParameterKind`;
- `ShaderMaterialParameterSnapshot`;
- `ShaderMaterialParametersSnapshot`;
- `ShaderMaterialParameterTextSerializer`;
- `CanvasShaderBuiltInRegistry`.

Snapshot должен:

- сортировать parameters по имени через ordinal order;
- писать `format = "Electron2D.ShaderMaterialParameters"` и `version = 1`;
- писать shader path как `Shader.ResourcePath`, если shader задан;
- сериализовать value uniforms через действующий stable `VariantTextSerializer`;
- сериализовать texture samplers отдельным object entry, потому что обычный stable `VariantTextSerializer` не сериализует `Object`;
- отказываться сериализовать материал, если в нём оказался unsupported parameter value.

## Engine-provided canvas built-ins

`CanvasShaderBuiltInRegistry` должен хранить reserved names из Godot canvas item shader model:

- global built-ins: `TIME`, `PI`, `TAU`, `E`;
- vertex/fragment/light built-ins: `MODEL_MATRIX`, `CANVAS_MATRIX`, `SCREEN_MATRIX`, `INSTANCE_ID`, `INSTANCE_CUSTOM`, `AT_LIGHT_PASS`, `TEXTURE_PIXEL_SIZE`, `VERTEX`, `VERTEX_ID`, `UV`, `COLOR`, `POINT_SIZE`, `CUSTOM0`, `CUSTOM1`, `FRAGCOORD`, `SCREEN_PIXEL_SIZE`, `REGION_RECT`, `POINT_COORD`, `TEXTURE`, `SPECULAR_SHININESS_TEXTURE`, `SPECULAR_SHININESS`, `SCREEN_UV`, `SCREEN_TEXTURE`, `NORMAL`, `NORMAL_TEXTURE`, `NORMAL_MAP`, `NORMAL_MAP_DEPTH`, `SHADOW_VERTEX`, `LIGHT_VERTEX`, `LIGHT_COLOR`, `LIGHT_ENERGY`, `LIGHT_POSITION`, `LIGHT_DIRECTION`, `LIGHT_IS_DIRECTIONAL`, `LIGHT`, `SHADOW_MODULATE`;
- SDF helper function names: `texture_sdf`, `texture_sdf_normal`, `sdf_to_screen_uv`, `screen_uv_to_sdf`.

`SetShaderParameter()` должен отклонять exact-case совпадение с reserved name. Нижний регистр или другое имя не считается совпадением, потому что shader parameters case-sensitive.

## Fail-closed behavior

Ошибки должны быть явными:

- пустой parameter name -> `ArgumentException`;
- reserved built-in name -> `ArgumentException`;
- unsupported Variant type -> `ArgumentException`;
- `Material.RenderPriority` вне диапазона `-128..127` -> `ArgumentOutOfRangeException`;
- freed material/shader/texture object -> обычная проверка lifetime через `ThrowIfFreed()` там, где она уже существует.

Renderer не должен получать unsupported values и молча заменять их default значениями.

## Проверки

Минимальный acceptance набор:

- unit test `ShaderMaterial` public API: inheritance, `Shader`, `SetShaderParameter()`, `GetShaderParameter()`, case-sensitive names и nil reset;
- unit test `Material` public API: inheritance, `NextPass`, `RenderPriority` range;
- unit test clean runtime baseline: public surface содержит `Material` и `ShaderMaterial`, legacy/component types не возвращаются;
- integration test supported uniforms snapshot serialization;
- integration test texture sampler snapshot serialization;
- integration test unsupported values and reserved built-ins fail closed;
- golden-data test stable JSON text for shader material parameters;
- API compatibility verifier отражает `Material` и `ShaderMaterial` в GitHub Wiki source;
- source license verifier проходит для новых C# files.

## Фактическое состояние, ограничения и проверки

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

`RenderPriority` принимает значения от `-128` до `127`. В `0.1-preview` значение сохраняется для будущего renderer ordering, но текущая 2D очередь всё ещё сортирует команды по canvas layer, z-index, y-sort и tree order.

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
