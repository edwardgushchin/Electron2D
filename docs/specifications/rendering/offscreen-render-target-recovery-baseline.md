# Offscreen render target и восстановление GPU resources

Статус: целевая спецификация для `T-0030`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Texture2D resource baseline](texture-resource-baseline.md), [SDL_GPU lifecycle baseline](sdl-gpu-lifecycle.md), [Camera2D, Viewport and presentation baseline](camera-viewport-presentation-baseline.md).

## Цель

Electron2D `0.1.0 Preview` должен иметь минимальный baseline для render-to-texture и восстановления GPU resources после потери или пересоздания устройства:

- `Viewport.GetTexture()` возвращает Godot-like `ViewportTexture`;
- `ViewportTexture` наследуется от `Texture2D` и отражает текущий размер `Viewport`;
- internal texture registry умеет создавать offscreen render target descriptors;
- active texture resources и render targets можно восстановить на новом GPU adapter без изменения public API;
- smoke tests проверяют render target lifecycle и device recreation path без реального GPU/window server.

## Источники совместимости

- Godot `Viewport.get_texture()` возвращает texture текущего viewport.
- Godot `ViewportTexture` наследуется от `Texture2D` и предоставляет содержимое `Viewport` как динамическую texture.
- SDL GPU создаёт texture object через `SDL_CreateGPUTexture`; содержимое undefined, пока в texture не загрузили данные или пока она не использована как render/compute target.

## Публичный API

Новый public surface:

- `Viewport.GetTexture()`;
- `ViewportTexture : Texture2D`.

`ViewportTexture` должен:

- возвращать ширину/высоту из связанного `Viewport.Size`;
- быть `ResourceLocalToScene == true`;
- возвращать `HasAlpha() == true`;
- возвращать `HasMipmaps() == false` и `GetMipmapCount() == 0`;
- возвращать `false` из `IsPixelOpaque()`, пока readback/image API не реализован.

Не допускается добавлять public SDL_GPU handles, public render-target handles или non-Godot API. Восстановление GPU resources остаётся internal behavior.

## Internal render target contract

`TextureResourceRegistry` должен поддерживать:

- `CreateRenderTarget(Vector2I size, bool hasAlpha, TextureSamplingOptions sampling)`;
- descriptor usage `RenderTarget`;
- active handle tracking для render targets;
- release render target через общий `Release()`;
- событие `RenderTargetCreated`.

Размер render target должен быть положительным по обеим осям. Ошибка backend allocation не должна оставлять active handle.

## Device recreation contract

При device loss или полном пересоздании device registry должен уметь восстановить active resources через новый `ITextureGpuApi`:

- сохранить существующие `Rid`;
- переотправить descriptors всех active sampled textures и render targets в новый adapter;
- добавить событие `Restored` для каждого восстановленного resource;
- при ошибке добавить `Error`, выбросить исключение и не удалять active resource records.

Этот baseline не запускает реальный SDL window/GPU в CI. Проверка выполняется через deterministic fake adapters.

## Проверки

Минимальный acceptance набор:

- unit test `Viewport.GetTexture()` и `ViewportTexture` public behavior;
- integration test `CreateRenderTarget()` descriptor и lifecycle;
- integration test restore active uploaded texture и render target на новом adapter с сохранением `Rid`;
- runtime smoke test upload + render target + restore + release без leaks;
- API compatibility verifier отражает `ViewportTexture` в GitHub Wiki source;
- source license verifier проходит для новых C# files.
