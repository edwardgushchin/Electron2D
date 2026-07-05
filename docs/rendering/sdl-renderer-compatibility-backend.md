# SDL_Renderer Compatibility backend baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация `T-0033`.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md), [Архитектура и платформенный стек Electron2D](../architecture/engine-platform-stack.md), [`RenderingServer` и renderer profiles](rendering-server.md), [CanvasItem render queue baseline](canvas-item-render-queue.md), [Immediate drawing baseline](immediate-drawing-baseline.md), [Text backend baseline через SDL_ttf](text-backend-baseline.md).

## Назначение

`Compatibility` в этой задаче означает renderer fallback profile поверх `SDL_Renderer`. Это не слой совместимости со старым API Electron2D и не возврат Unity-like/component истории. Публичный API остаётся Electron2D, а concrete backend types остаются внутренними.

`T-0033` должна сделать проверяемый baseline: текущий `CanvasItemRenderPlan` преобразуется во внутренний SDL_Renderer-совместимый frame plan. Такой план можно тестировать без реального окна и GPU, а поздний platform startup сможет передавать эти операции в SDL3-CS.

## Источники поведения

SDL3 renderer API поддерживает базовый 2D-набор: линии, заполненные прямоугольники, текстуры, 2D polygons и стандартные режимы смешивания. Для Electron2D это соответствует минимальному профилю `Compatibility`.

Основные SDL3 операции, на которые должен опираться backend:

- [`SDL_RenderTexture`](https://wiki.libsdl.org/SDL3/SDL_RenderTexture) для спрайтов, tile-like texture copies и простого `DrawTexture()`;
- [`SDL_RenderTextureRotated`](https://wiki.libsdl.org/SDL3/SDL_RenderTextureRotated) для texture copy с rotation/flip;
- [`SDL_RenderLine`](https://wiki.libsdl.org/SDL3/SDL_RenderLine) для одиночных линий;
- [`SDL_RenderGeometry`](https://wiki.libsdl.org/SDL3/SDL_RenderGeometry) для полигонов и будущей triangulation path;
- [`SDL_Renderer` category](https://wiki.libsdl.org/SDL3/CategoryRender) как общий контракт 2D renderer API.

Текст использует текущую модель `Font`/`TextLayout` и будущий SDL_ttf renderer text engine. Backend не должен раскрывать SDL_ttf handles в public API.

## Internal contract

`CompatibilityRenderingBackend` должен уметь построить internal frame plan из `CanvasItemRenderPlan`.

Минимальная форма:

```csharp
internal sealed class CompatibilityRenderingBackend : RenderingBackend
{
    internal SdlRendererFramePlan CreateFramePlan(CanvasItemRenderPlan renderPlan);
    internal IReadOnlyList<string> Limitations { get; }
}
```

`SdlRendererFramePlan` должен хранить:

- имя backend;
- renderer profile;
- список поддерживаемых feature flags;
- исходное количество batches как draw-call estimate;
- детерминированный список SDL_Renderer-compatible draw commands;
- стабильный список limitation IDs.

`SdlRendererFramePlanTextSerializer` должен давать stable text snapshot для golden-data тестов.

## Mapping

| `CanvasItemRenderCommandKind` | Compatibility draw command | SDL3 direction |
| --- | --- | --- |
| `Texture` | texture copy | `SDL_RenderTexture` или `SDL_RenderTextureRotated` при flip/rotation |
| `Line` | line | `SDL_RenderLine`; толщина и antialiasing фиксируются как limitation |
| `Rect` | filled или outline rect | SDL renderer rectangle path |
| `Circle` | segmented circle | будущая internal geometry approximation |
| `Polygon` | polygon geometry | `SDL_RenderGeometry`, texture optional |
| `String` | text | SDL_ttf renderer text engine через текущий `TextLayout` |

TileMap в текущем public API ещё не представлен отдельным узлом. На уровне backend TileMap сводится к тем же texture copy commands, что и sprite/atlas/tile submissions. Поэтому `Compatibility` продолжает объявлять `RenderingFeature.TileMap`, но `T-0033` не добавляет public `TileMapLayer`.

## Ограничения

Backend должен явно сообщать ограничения:

- `custom-shaders-unsupported` - пользовательские `Shader` не выполняются в `Compatibility`;
- `shader-material-unsupported` - `ShaderMaterial` не применяется к draw commands в этом профиле;
- `post-processing-unsupported` - post-processing не входит в baseline;
- `render-targets-not-guaranteed` - render targets доступны только если конкретный SDL_Renderer path их поддержит в будущей integration task;
- `primitive-antialiasing-approximated` - line/rect/circle antialiasing и большая толщина являются приближением;
- `pixel-perfect-standard-parity-not-guaranteed` - визуальная идентичность с `Standard` не гарантируется.

Per-frame limitations могут добавлять дополнительные IDs, например `circle-rendered-as-segmented-geometry`, если план содержит circles.

## Acceptance tests

- Integration test строит reference scene со `Sprite2D`, UI `Label`, `DrawString()`, `DrawTexture()`, `DrawLine()`, `DrawRect()`, `DrawCircle()` и `DrawPolygon()`, затем проверяет, что `CompatibilityRenderingBackend.CreateFramePlan()` возвращает texture/text/primitive commands без standard-only features.
- Integration test проверяет documented limitations и feature policy: `Compatibility` поддерживает sprites, animation, TileMap, UI, text, primitives, camera, clipping и standard blend modes, но не поддерживает custom shaders, `ShaderMaterial`, render targets, multipass, advanced blending и post-processing.
- Golden-data test сериализует compatibility reference frame plan и сравнивает его со стабильным текстом.
- Public API compatibility verifier не должен увидеть новых public types.
- Source license verifier должен пройти для всех новых C# source files.

## Не входит в `T-0033`

- Реальное создание `SDL_Renderer`, окна и swapchain.
- Platform fallback policy `automatic`/`fail_if_unavailable`.
- Public `TileMapLayer`, `TextureRect`, `NinePatchRect`, `Polygon2D`, `Line2D` и другие будущие nodes.
- Pixel screenshot из реального SDL window. До появления window presentation для SDL_Renderer визуальная проверка выполняется как deterministic golden snapshot SDL_Renderer command stream.

## Фактическое состояние, ограничения и проверки

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
