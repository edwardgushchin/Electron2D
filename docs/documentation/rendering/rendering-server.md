# `RenderingServer` и renderer profiles

Статус: реализованный baseline.
Задачи: `T-0022`, `T-0023`, `T-0024`, `T-0025`.
Обновлено: 2026-06-21.

## Public API

Текущий runtime экспортирует:

- `Electron2D.RenderingServer`;
- `Electron2D.RenderingServer.RenderingProfile`;
- `Electron2D.RenderingServer.RenderingFeature`.

`RenderingServer` - Godot-like singleton-style facade для запроса активного renderer profile и feature flags. Concrete backend classes не являются public API.

## Профили

`RenderingProfile.Compatibility` - гарантированный минимальный профиль. Он активен по умолчанию, пока настоящий SDL startup/fallback pipeline ещё не реализован.

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

Эти типы доступны runtime/tests, но не экспортируются из assembly. Публичные node-типы не раскрывают concrete rendering backend через public members.

## Ограничения

- Real-window GPU smoke test ещё не запускается в CI: проверяется deterministic fake adapter, а production adapter требует реальный `SDL_Window` handle.
- Backend fallback logging и resource restore будут отдельными задачами.
- Public `Node2D`, `CanvasItem`, `Sprite2D`, shaders, real texture GPU transfer и real canvas item draw submission ещё не реализованы.
- OpenGL ES backend не добавлен.

## Проверки

- Unit tests фиксируют default compatibility profile, feature flags, public API baseline и отсутствие public backend leaks.
- Integration tests переключают internal `StandardRenderingBackend`/`CompatibilityRenderingBackend` и проверяют `CurrentProfile`/`HasFeature()`.
- Integration tests проверяют internal SDL_GPU lifecycle: initialize, frame begin/submit, resize/high-DPI, fullscreen, device errors и недопустимый порядок frame calls.
- Integration tests проверяют internal canvas item render queue: stable sorting, y-sort, visibility, modulate и batching.
- Unit/integration/runtime smoke tests проверяют public texture metadata/atlas behavior и internal texture upload/reload/release leak tracking.
