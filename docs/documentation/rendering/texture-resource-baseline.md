# Texture2D resource baseline

Статус: реализовано.
Задача: `T-0025`, обновлено в `T-0030`.
Обновлено: 2026-06-21.

## Public API

В runtime добавлены Godot-like public ресурсы:

- `Texture2D`;
- `AtlasTexture`;
- `ViewportTexture` через отдельный baseline [Offscreen render target и восстановление GPU resources](offscreen-render-target-recovery-baseline.md).

`Texture2D` является abstract base resource. Он предоставляет:

- `GetWidth()`;
- `GetHeight()`;
- `GetSize()`;
- `HasAlpha()`;
- `HasMipmaps()`;
- `GetMipmapCount()`;
- `IsPixelOpaque(int x, int y)`.

`AtlasTexture` наследуется от `Texture2D` и добавляет:

- `Atlas`;
- `Region`;
- `Margin`;
- `FilterClip`.

Если у `Region.Size.X` или `Region.Size.Y` значение `0`, для соответствующей оси используется размер atlas texture. Размер региона округляется вниз до `int`, как integer image size.

## Transparency

`Texture2D.IsPixelOpaque()` возвращает `true` только для пикселей внутри texture bounds. Базовая реализация считает texture без alpha полностью непрозрачной внутри bounds, а alpha texture требует concrete override.

`AtlasTexture.IsPixelOpaque()` переводит координаты в `Region.Position + (x, y)` и делегирует проверку atlas texture. Запросы за пределами region или atlas возвращают `false`.

## Internal Lifetime Registry

Internal `TextureResourceRegistry` управляет upload/reload/release lifecycle через `ITextureGpuApi`. В этом документе `internal` означает код движка, доступный тестам и будущему runtime/editor host, но не пользовательский public API.

Registry хранит active texture handles, пишет события:

- `Uploaded`;
- `Reloaded`;
- `Released`;
- `RenderTargetCreated`;
- `Restored`;
- `Error`.

`LeakCount` равен количеству active handles, которые ещё не были release. Runtime smoke tests проверяют циклы upload -> reload -> release и upload -> render target -> restore -> release, ожидая `LeakCount == 0`.

Начиная с `T-0030`, registry также создаёт offscreen render targets через `CreateRenderTarget()` и восстанавливает active texture resources после пересоздания device через `RestoreAfterDeviceLoss()`. Подробности описаны в [Offscreen render target и восстановление GPU resources](offscreen-render-target-recovery-baseline.md).

## Sampling Descriptor

Filter/repeat сейчас представлены internal `TextureSamplingOptions`. Public `CanvasItem` уже существует, но GPU sampling ещё не вынесен в public API, потому что настоящий texture drawing pipeline остаётся следующим шагом.

## Ограничения

- PNG/JPEG import и public image-backed texture class остаются задачей `T-0037`.
- Real SDL_GPU transfer-buffer upload ещё не реализован; T-0025 фиксирует registry contract и backend adapter boundary.
- `DrawTexture()` реализован в immediate drawing baseline `T-0028`; `DrawTextureRect()` и `DrawTextureRectRegion()` остаются будущими drawing задачами.

## Проверки

Целевые наборы:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --no-restore
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore
dotnet test tests\Electron2D.Tests.RuntimeSmoke\Electron2D.Tests.RuntimeSmoke.csproj --no-restore
```

Они проверяют public metadata/atlas/viewport texture behavior, internal upload/reload/release registry, atlas upload descriptor, failed upload cleanup, unknown handle rejection, render target descriptor, restore path и no-leak smoke cycles.
