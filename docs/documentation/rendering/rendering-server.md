# `RenderingServer` и renderer profiles

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
