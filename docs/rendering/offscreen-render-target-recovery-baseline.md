# Offscreen render target и восстановление GPU resources

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0030`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md), [Texture2D resource baseline](texture-resource-baseline.md), [SDL_GPU lifecycle baseline](sdl-gpu-lifecycle.md), [Camera2D, Viewport and presentation baseline](camera-viewport-presentation-baseline.md).

## Цель

Electron2D `0.1-preview` должен иметь минимальный baseline для render-to-texture и восстановления GPU resources после потери или пересоздания устройства:

- `Viewport.GetTexture()` возвращает Electron2D `ViewportTexture`;
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

## Фактическое состояние, ограничения и проверки

Статус: реализовано.
Задача: `T-0030`.
Обновлено: 2026-06-21.

## Public API

В runtime добавлен Electron2D public resource:

- `ViewportTexture`.

`Viewport` теперь добавляет:

- `GetTexture()`.

`Viewport.GetTexture()` возвращает один и тот же `ViewportTexture` для конкретного viewport. `ViewportTexture` наследуется от `Texture2D`, создаётся движком через `Viewport.GetTexture()` и помечается как `ResourceLocalToScene == true`.

`ViewportTexture` отражает текущий размер своего `Viewport`:

- `GetWidth()` возвращает `Viewport.Size.X`, но не меньше `0`;
- `GetHeight()` возвращает `Viewport.Size.Y`, но не меньше `0`;
- `GetSize()` приходит из базового `Texture2D` и использует текущие width/height;
- `HasAlpha()` возвращает `true`;
- `HasMipmaps()` возвращает `false`;
- `GetMipmapCount()` возвращает `0`;
- `IsPixelOpaque()` возвращает `false`, потому что публичного чтения пикселей из render target в `0.1-preview` ещё нет.

Public render-target handles и другие backend-specific методы не добавлены.

## Внутренний render target lifecycle

Внутренний механизм `TextureResourceRegistry` теперь умеет создавать offscreen render target через `CreateRenderTarget(Vector2I size, bool hasAlpha, TextureSamplingOptions sampling)`. Внутренний означает, что этот контракт доступен коду движка и тестовому host-проекту, но не входит в пользовательский public API.

Render target:

- требует положительный размер по X и Y;
- получает новый `Rid`;
- отправляет descriptor в текущий `ITextureGpuApi`;
- сохраняется среди active texture resources;
- освобождается общим `Release()`;
- пишет событие `RenderTargetCreated`.

Descriptor содержит `TextureResourceUsage.RenderTarget`, чтобы backend мог отличить обычную sampled texture от texture, в которую можно рендерить.

## Восстановление после пересоздания device

`TextureResourceRegistry.RestoreAfterDeviceLoss(ITextureGpuApi newApi)` переотправляет descriptors всех active sampled textures и render targets в новый адаптер `ITextureGpuApi`.

Поведение:

- существующие `Rid` сохраняются;
- descriptors переотправляются в порядке active resources;
- после успешной переотправки registry переключает release path на новый adapter;
- для каждого восстановленного resource добавляется событие `Restored`;
- при ошибке добавляется `Error`, выбрасывается `InvalidOperationException`, а active resource records не удаляются.

Этот baseline проверяет восстановление через deterministic fake adapters. Реальный window/GPU smoke path остаётся будущей задачей backend integration.

## Проверки

Целевые focused-команды:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~ViewportTexturePublicApiTests|FullyQualifiedName~CleanRuntimeBaselineTests" --no-restore
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~RenderTargetRecoveryTests" --no-restore
dotnet test tests\Electron2D.Tests.RuntimeSmoke\Electron2D.Tests.RuntimeSmoke.csproj --filter "FullyQualifiedName~RenderTargetRecoverySmokeTests" --no-restore
```

Полный release-gate runner:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
