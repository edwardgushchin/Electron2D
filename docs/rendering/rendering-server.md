# `RenderingServer` и renderer profiles

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`RenderingServer` должен стать Electron2D server boundary для всего, что будет отображаться в Electron2D. В `0.1.0 Preview` задача `T-0022` не создаёт настоящий SDL device и не рисует кадр. Она вводит минимальную проверяемую границу:

- публичный singleton-style facade `RenderingServer`;
- два renderer profile: `Compatibility` и `Standard`;
- feature flags через `HasFeature()`;
- internal backend abstraction, скрытую от public node API.

## Источники поведения

- [Godot RenderingServer](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html);
- `docs/releases/0.1.0-preview.md`, разделы «Два профиля рендеринга» и Android fallback;
- `docs/architecture/engine-platform-stack.md`, раздел «Ключевая архитектурная граница».

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

Enums вложены в `RenderingServer`, чтобы не разбрасывать renderer-specific имена по корневому namespace и сохранить Electron2D singleton/server shape.

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
- Public `Node2D`, `CanvasItem`, `Sprite2D`, shaders, real texture GPU transfer и real canvas item draw submission остаются будущими задачами.
- OpenGL ES backend не добавляется.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задачи: `T-0022`, `T-0023`, `T-0024`, `T-0025`, `T-0031`, `T-0032`, `T-0033`, `T-0034`.
Обновлено: 2026-06-21.

## Public API

Текущий runtime экспортирует:

- `Electron2D.RenderingServer`;
- `Electron2D.RenderingServer.RenderingProfile`;
- `Electron2D.RenderingServer.RenderingFeature`.

`RenderingServer` - Electron2D singleton-style facade для запроса активного renderer profile и feature flags. Concrete backend classes не являются public API.

## Профили

`RenderingProfile.Compatibility` - гарантированный минимальный профиль. Он активен по умолчанию, пока настоящий window startup/fallback pipeline ещё не реализован.

Начиная с `T-0033` internal `CompatibilityRenderingBackend` строит compatibility frame plan из `CanvasItemRenderPlan`. Этот план описывает команды для sprites, UI/text, primitives и tile-like texture copies, но пока не создаёт real-window presentation.

`RenderingProfile.Standard` - профиль для расширенного graphics backend. Начиная с `T-0023` internal standard backend ведёт state machine для создания graphics device, привязки окна, begin/end frame и shutdown. Этот lifecycle не является public API.

Начиная с `T-0034` internal startup policy умеет создать graphics device в Android mobile-compatible profile, выполнить smoke steps и выбрать `Compatibility` fallback либо structured failure по policy.

## Feature flags

Compatibility profile поддерживает:

- `Sprites`;
- `Animation`;
- `TileMap`;
- `Ui`;
- `Text`;
- `Primitives`;
- `Camera`;
- `Clipping`;
- `StandardBlendModes`.

Standard profile поддерживает все compatibility features и дополнительно:

- `RenderTargets`;
- `CustomShaders`;
- `ShaderMaterial`;
- `MultiPass`;
- `AdvancedBlending`;
- `PostProcessing`.

## Internal backend boundary

Внутренняя граница включает renderer profile adapters, startup policy, smoke-test result, canvas command queue, texture registry, shader import boundary, material parameter snapshot и compatibility frame plan. Эти типы доступны runtime/tests, но не экспортируются из assembly. Публичные node-типы не раскрывают concrete rendering backend через public members.

## Ограничения

- Real-window graphics smoke test ещё не запускается в CI: проверяется deterministic fake adapter, а production adapter требует реальный native window handle.
- Startup fallback result уже содержит selected backend, GPU, driver и reasons; интеграция этого result в editor/CLI logging будет отдельной задачей.
- `Compatibility` уже строит internal command plan, но реальный вызов native renderer functions и screenshot из окна ещё не реализованы.
- Public `ShaderMaterial`, uniforms, samplers и reserved canvas built-ins реализованы как resource/model layer.
- Real texture GPU transfer и real canvas item draw submission ещё не реализованы.
- Shader import/material baseline уже создаёт compiled artifacts и serializable material parameter snapshots, но не привязывает их к реальному draw pipeline.
- OpenGL ES backend не добавлен.

## Проверки

- Unit tests фиксируют default compatibility profile, feature flags, public API baseline и отсутствие public backend leaks.
- Integration tests переключают internal `StandardRenderingBackend`/`CompatibilityRenderingBackend` и проверяют `CurrentProfile`/`HasFeature()`.
- Integration tests проверяют internal standard graphics lifecycle: initialize, frame begin/submit, resize/high-DPI, fullscreen, device errors и недопустимый порядок frame calls.
- Integration tests проверяют Android mobile graphics create options, smoke steps texture/pipeline/command buffer/first submit, `Automatic` fallback, `FailIfUnavailable` и startup log.
- Integration tests проверяют internal canvas item render queue: stable sorting, y-sort, visibility, modulate и batching.
- Unit/integration/runtime smoke tests проверяют public texture metadata/atlas behavior и internal texture upload/reload/release leak tracking.
- Unit/integration/runtime smoke tests проверяют public `Shader`, import-time vertex/fragment compilation boundary, diagnostics file/line/column и iOS artifact без runtime compilation.
- Unit/integration/golden-data tests проверяют public `Material`/`ShaderMaterial`, supported uniforms, texture sampler snapshot, reserved built-ins и stable JSON material parameter snapshot.
- Integration/golden-data tests проверяют compatibility frame plan, documented limitations и stable reference scene command stream.
