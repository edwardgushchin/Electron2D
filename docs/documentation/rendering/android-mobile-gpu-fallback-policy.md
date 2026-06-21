# Android mobile GPU smoke и fallback policy baseline

Статус: реализовано.
Задача: `T-0034`.
Обновлено: 2026-06-21.

## Назначение

Добавлена внутренняя startup policy для renderer backend. Она создаёт SDL_GPU backend с platform-specific create info, выполняет smoke test и выбирает один из вариантов:

- `SDL_GPU`, если smoke проходит;
- `Compatibility`, если smoke падает и policy равна `Automatic`;
- structured failure, если smoke падает и policy равна `FailIfUnavailable`.

Публичный API не изменился. Mobile profile остаётся internal policy для SDL_GPU device creation, а не новым public `RenderingProfile`.

## Android mobile create profile

Для Android используется `SdlGpuDeviceCreateInfo.AndroidMobile(debugMode)`. В нём отключены optional Vulkan features:

- `ClipDistance`;
- `DepthClamping`;
- `IndirectDrawFirstInstance`;
- `Anisotropy`.

Production adapter `SdlGpuApi` создаёт device через `SDL_CreateGPUDeviceWithProperties` и передаёт эти значения в SDL properties. Стандартный desktop profile оставляет эти optional features включёнными.

## Smoke test

`SdlGpuMobileSmokeTest` выполняет шаги в стабильном порядке:

1. `Texture` - базовая проверка texture path через adapter;
2. `Pipeline` - проверка pipeline capability через shader format availability;
3. `CommandBuffer` - `BeginFrame()` и acquire command buffer;
4. `FirstSubmit` - `EndFrame()` и submit command buffer.

Если ранний шаг падает, поздние шаги помечаются как skipped с причиной `previous smoke step failed.`, но startup result выводит только корневые причины отказа.

## Startup result и log

`SdlGpuStartupResult` содержит:

- selected backend name;
- selected `RenderingProfile`, если backend выбран;
- `UsedFallback`;
- GPU name;
- driver name;
- driver version/info;
- root reasons;
- smoke step results.

`ToLogLine()` формирует однострочный structured log:

```text
backend=Compatibility|profile=Compatibility|fallback=True|gpu=Adreno 730|driver=vulkan|driverVersion=1.3.250|reasons=graphics pipeline creation failed on Vulkan driver.
```

Эта строка предназначена для будущих runtime diagnostics и CLI/editor output.

## Ограничения

- Реальный Android export/package ещё не реализован.
- `Pipeline` smoke пока проверяет доступность shader formats, а не создаёт полноценный graphics pipeline с platform-specific compiled shader.
- Реальный swapchain screenshot и device-specific Android run не входят в эту задачу.
- Project settings для `automatic`/`fail_if_unavailable` ещё не добавлены.

## Проверки

Фокусные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore --filter FullyQualifiedName~SdlGpuStartupPolicyTests
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore --filter FullyQualifiedName~SdlGpuLifecycleTests
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
