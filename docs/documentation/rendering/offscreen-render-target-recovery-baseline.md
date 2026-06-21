# Offscreen render target и восстановление GPU resources

Статус: реализовано.
Задача: `T-0030`.
Обновлено: 2026-06-21.

## Public API

В runtime добавлен Godot-like public resource:

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
- `IsPixelOpaque()` возвращает `false`, потому что публичного чтения пикселей из render target в `0.1.0 Preview` ещё нет.

Public SDL_GPU handles, public render-target handles и другие non-Godot методы не добавлены.

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
