# `RenderingServer` и renderer profiles

Статус: реализованный baseline.
Задачи: `T-0022`, `T-0023`, `T-0024`, `T-0025`, `T-0031`, `T-0032`, `T-0033`.
Обновлено: 2026-06-21.

## Public API

Текущий runtime экспортирует:

- `Electron2D.RenderingServer`;
- `Electron2D.RenderingServer.RenderingProfile`;
- `Electron2D.RenderingServer.RenderingFeature`.

`RenderingServer` - Godot-like singleton-style facade для запроса активного renderer profile и feature flags. Concrete backend classes не являются public API.

## Профили

`RenderingProfile.Compatibility` - гарантированный минимальный профиль. Он активен по умолчанию, пока настоящий SDL startup/fallback pipeline ещё не реализован.

Начиная с `T-0033` internal `CompatibilityRenderingBackend` строит `SdlRendererFramePlan` из `CanvasItemRenderPlan`. Этот план описывает SDL_Renderer-compatible commands для sprites, UI/text, primitives и tile-like texture copies, но пока не создаёт реальное SDL window presentation.

`RenderingProfile.Standard` - SDL_GPU-oriented профиль. Начиная с `T-0023` internal `SdlGpuRenderingBackend` ведёт state machine для создания SDL_GPU device, claim окна, begin/end frame и shutdown. Этот lifecycle не является public API.

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

Внутренняя граница:

- `IRenderingBackend`;
- `RenderingBackend`;
- `CompatibilityRenderingBackend`;
- `StandardRenderingBackend`.
- `SdlGpuRenderingBackend`;
- `ISdlGpuApi`;
- `SdlGpuApi`.
- `CanvasItemRenderQueue`;
- `CanvasItemRenderCommand`;
- `CanvasItemRenderPlan`.
- `TextureResourceRegistry`;
- `ITextureGpuApi`;
- `CanvasShaderImportPipeline`;
- `ICanvasShaderCompiler`;
- `SdlShaderCrossCompiler`;
- `ShaderMaterialParametersSnapshot`;
- `ShaderMaterialParameterTextSerializer`;
- `CanvasShaderBuiltInRegistry`;
- `SdlRendererFramePlan`;
- `SdlRendererDrawCommand`;
- `SdlRendererFramePlanTextSerializer`;

Эти типы доступны runtime/tests, но не экспортируются из assembly. Публичные node-типы не раскрывают concrete rendering backend через public members.

## Ограничения

- Real-window GPU smoke test ещё не запускается в CI: проверяется deterministic fake adapter, а production adapter требует реальный `SDL_Window` handle.
- Backend fallback logging будет отдельной задачей.
- `Compatibility` уже строит SDL_Renderer-compatible command plan, но реальный вызов SDL3-CS renderer functions и screenshot из окна ещё не реализованы.
- Public `ShaderMaterial`, uniforms, samplers и reserved canvas built-ins реализованы как resource/model layer.
- Real texture GPU transfer и real canvas item draw submission ещё не реализованы.
- Shader import/material baseline уже создаёт compiled artifacts и serializable material parameter snapshots, но не привязывает их к реальному draw pipeline.
- OpenGL ES backend не добавлен.

## Проверки

- Unit tests фиксируют default compatibility profile, feature flags, public API baseline и отсутствие public backend leaks.
- Integration tests переключают internal `StandardRenderingBackend`/`CompatibilityRenderingBackend` и проверяют `CurrentProfile`/`HasFeature()`.
- Integration tests проверяют internal SDL_GPU lifecycle: initialize, frame begin/submit, resize/high-DPI, fullscreen, device errors и недопустимый порядок frame calls.
- Integration tests проверяют internal canvas item render queue: stable sorting, y-sort, visibility, modulate и batching.
- Unit/integration/runtime smoke tests проверяют public texture metadata/atlas behavior и internal texture upload/reload/release leak tracking.
- Unit/integration/runtime smoke tests проверяют public `Shader`, import-time vertex/fragment compilation boundary, diagnostics file/line/column и iOS artifact без runtime compilation.
- Unit/integration/golden-data tests проверяют public `Material`/`ShaderMaterial`, supported uniforms, texture sampler snapshot, reserved built-ins и stable JSON material parameter snapshot.
- Integration/golden-data tests проверяют SDL_Renderer compatibility frame plan, documented limitations и stable reference scene command stream.
