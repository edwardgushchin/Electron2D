# SDL_Renderer Compatibility backend baseline

Статус: реализовано.
Задача: `T-0033`.
Обновлено: 2026-06-21.

## Назначение

`CompatibilityRenderingBackend` теперь не только сообщает feature flags, но и строит внутренний `SdlRendererFramePlan` из текущего `CanvasItemRenderPlan`. Это подготовка к реальному fallback поверх SDL3 `SDL_Renderer`: сцена уже сводится к операциям, которые соответствуют SDL renderer path.

Это не compatibility layer для старого Electron2D API. Публичный API не расширялся, а concrete backend types остаются внутренними и доступны только runtime/tests.

## Что поддерживает frame plan

`CreateFramePlan()` принимает отсортированный и batched canvas plan и создаёт команды:

- `Texture` - спрайты, `DrawTexture()` и будущие tile-like texture copies;
- `Line` - одиночные линии;
- `Rect` - filled или outline rectangle path;
- `Circle` - segmented geometry approximation;
- `Polygon` - polygon geometry, optional texture;
- `Text` - `DrawString()` и `Label` text layout через будущий SDL_ttf renderer text path.

Frame plan хранит:

- backend name и renderer profile;
- поддерживаемые `RenderingServer.RenderingFeature`;
- draw-call estimate из `CanvasItemRenderPlan.DrawCallCount`;
- детерминированный список SDL_Renderer-compatible commands;
- limitation IDs.

`SdlRendererFramePlanTextSerializer` создаёт stable LF-only text snapshot для golden-data тестов.

## Ограничения backend

Базовые limitation IDs:

- `custom-shaders-unsupported`;
- `shader-material-unsupported`;
- `post-processing-unsupported`;
- `render-targets-not-guaranteed`;
- `primitive-antialiasing-approximated`;
- `pixel-perfect-standard-parity-not-guaranteed`.

Если frame plan содержит круги, добавляется `circle-rendered-as-segmented-geometry`, потому что SDL_Renderer не предоставляет отдельную высокоуровневую circle primitive path в текущем baseline.

## TileMap

Публичного `TileMapLayer` в текущем runtime ещё нет. На уровне compatibility backend tile rendering сводится к тем же texture copy commands, что и sprite/atlas submissions. Поэтому `RenderingFeature.TileMap` остаётся включённым, но отдельный public TileMap node будет реализован в своей задаче.

## Что ещё не реализовано

- Реальное создание `SDL_Renderer` и привязка к окну.
- Передача `SdlRendererFramePlan` в SDL3-CS вызовы.
- Screenshot из реального SDL window.
- Fallback policy `automatic`/`fail_if_unavailable`.
- Public `TileMapLayer`, `TextureRect`, `NinePatchRect`, `Polygon2D`, `Line2D`.

До появления real-window compatibility presentation визуальная проверка выполняется через golden snapshot command stream.

## Проверки

Фокусные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore --filter FullyQualifiedName~SdlRendererCompatibilityBackendTests
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --no-restore --filter FullyQualifiedName~SdlRendererCompatibilityGoldenTests
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
