# `RenderingServer` и renderer profiles

Статус: реализованный baseline.
Задача: `T-0022`.
Обновлено: 2026-06-21.

## Public API

Текущий runtime экспортирует:

- `Electron2D.RenderingServer`;
- `Electron2D.RenderingServer.RenderingProfile`;
- `Electron2D.RenderingServer.RenderingFeature`.

`RenderingServer` - Godot-like singleton-style facade для запроса активного renderer profile и feature flags. Concrete backend classes не являются public API.

## Профили

`RenderingProfile.Compatibility` - гарантированный минимальный профиль. Он активен по умолчанию, пока настоящий SDL startup/fallback pipeline ещё не реализован.

`RenderingProfile.Standard` - будущий SDL_GPU-oriented профиль. В `T-0022` он представлен internal backend implementation и проверяется тестами, но не создаёт SDL device.

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

Эти типы доступны runtime/tests, но не экспортируются из assembly. Публичные node-типы не раскрывают concrete rendering backend через public members.

## Ограничения

- SDL3-CS device creation и GPU smoke test ещё не реализованы.
- Backend fallback logging и resource restore будут отдельными задачами.
- `Node2D`, `CanvasItem`, `Sprite2D`, textures, shaders и canvas item commands ещё не реализованы.
- OpenGL ES backend не добавлен.

## Проверки

- Unit tests фиксируют default compatibility profile, feature flags, public API baseline и отсутствие public backend leaks.
- Integration tests переключают internal `StandardRenderingBackend`/`CompatibilityRenderingBackend` и проверяют `CurrentProfile`/`HasFeature()`.
