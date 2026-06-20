# `RenderingServer` и renderer profiles

## Назначение

`RenderingServer` должен стать Godot-like server boundary для всего, что будет отображаться в Electron2D. В `0.1.0 Preview` задача `T-0022` не создаёт настоящий SDL device и не рисует кадр. Она вводит минимальную проверяемую границу:

- публичный singleton-style facade `RenderingServer`;
- два renderer profile: `Compatibility` и `Standard`;
- feature flags через `HasFeature()`;
- internal backend abstraction, скрытую от public node API.

## Источники поведения

- [Godot RenderingServer](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html);
- `docs/specifications/releases/0.1.0-preview.md`, разделы «Два профиля рендеринга» и Android fallback;
- `docs/specifications/architecture/engine-platform-stack.md`, раздел «Ключевая архитектурная граница».

Godot `RenderingServer` описывается как непрозрачный backend для всего видимого. Resources создаются server methods и возвращают `Rid`, а nodes не должны знать, какой concrete backend активен.

## Public API

Минимальная публичная поверхность `T-0022`:

```csharp
namespace Electron2D;

public static class RenderingServer
{
    public enum RenderingProfile
    {
        Compatibility,
        Standard
    }

    public enum RenderingFeature
    {
        Sprites,
        Animation,
        TileMap,
        Ui,
        Text,
        Primitives,
        Camera,
        Clipping,
        StandardBlendModes,
        RenderTargets,
        CustomShaders,
        ShaderMaterial,
        MultiPass,
        AdvancedBlending,
        PostProcessing
    }

    public static RenderingProfile CurrentProfile { get; }

    public static bool HasFeature(RenderingFeature feature);
}
```

Enums вложены в `RenderingServer`, чтобы не разбрасывать renderer-specific имена по корневому namespace и сохранить Godot-like singleton/server shape.

## Internal backend abstraction

Минимальная internal граница:

```csharp
internal interface IRenderingBackend
{
    string Name { get; }
    RenderingServer.RenderingProfile Profile { get; }
    bool HasFeature(RenderingServer.RenderingFeature feature);
}
```

Internal backends `StandardRenderingBackend` и `CompatibilityRenderingBackend` должны реализовывать эту границу. Тесты могут переключать backend через internal test hook, но public API не должен раскрывать concrete backend types.

## Feature policy

`Compatibility` гарантирует:

- `Sprites`;
- `Animation`;
- `TileMap`;
- `Ui`;
- `Text`;
- `Primitives`;
- `Camera`;
- `Clipping`;
- `StandardBlendModes`.

`Compatibility` не гарантирует:

- `RenderTargets`;
- `CustomShaders`;
- `ShaderMaterial`;
- `MultiPass`;
- `AdvancedBlending`;
- `PostProcessing`.

`Standard` включает все `Compatibility` features и дополнительно:

- `RenderTargets`;
- `CustomShaders`;
- `ShaderMaterial`;
- `MultiPass`;
- `AdvancedBlending`;
- `PostProcessing`.

## Acceptance tests

- Default backend сообщает `Compatibility` profile и только compatibility features.
- Internal switch на `StandardRenderingBackend` меняет `CurrentProfile` на `Standard` и включает standard-only features.
- Internal switch обратно на `CompatibilityRenderingBackend` отключает standard-only features.
- Public exported types не содержат internal backend interfaces/classes.
- Public node types не имеют public members, которые раскрывают concrete rendering backend.
- API compatibility table отражает новые public types.

## Ограничения `T-0022`

- SDL3-CS device creation, GPU smoke test, fallback logging и resource restore не реализуются в этой задаче.
- Public `Node2D`, `CanvasItem`, `Sprite2D`, textures, shaders и real canvas item draw submission остаются будущими задачами.
- OpenGL ES backend не добавляется.
