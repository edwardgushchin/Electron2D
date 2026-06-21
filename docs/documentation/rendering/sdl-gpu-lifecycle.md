# SDL_GPU lifecycle baseline

Статус: реализовано.
Задачи: `T-0023`, `T-0034`.
Обновлено: 2026-06-21.

## Назначение

Electron2D использует SDL3-CS как managed binding к SDL3/SDL_GPU. В `0.1.0 Preview` lifecycle SDL_GPU остаётся internal runtime surface и не расширяет public Godot-like API.

Реализованный baseline закрывает:

- закреплённую managed dependency `SDL3-CS` версии `3.4.10.3`;
- shader import dependency `SDL3-CS.Native.Shadercross` версии `3.0.0`, добавленную в `T-0031` для import/export host;
- internal adapter `SdlGpuApi` для `SDL_CreateGPUDeviceWithProperties`, `SDL_ClaimWindowForGPUDevice`, GPU device info, mobile smoke checks, `SDL_AcquireGPUCommandBuffer`, `SDL_SubmitGPUCommandBuffer` и `SDL_DestroyGPUDevice`;
- internal `SdlGpuRenderingBackend` со state machine device/window/frame/shutdown;
- internal Android mobile device create profile с отключёнными optional Vulkan features;
- internal startup policy с `Automatic`/`FailIfUnavailable` fallback behavior;
- диагностический журнал событий lifecycle;
- smoke-тесты без зависимости от реального окна или GPU на CI.

## State machine

`SdlGpuRenderingBackend` проходит состояния:

```text
NotInitialized -> DeviceCreated -> WindowClaimed -> FrameOpen -> WindowClaimed -> Shutdown
```

Ошибки device creation, window claim, command buffer acquire и command buffer submit переводят backend в `Failed`, добавляют событие `DeviceError` и выбрасывают `InvalidOperationException` с исходной SDL/fake-adapter причиной.

## Device create profile

`SdlGpuRenderingBackend` принимает `SdlGpuDeviceCreateInfo`. Старый constructor `SdlGpuRenderingBackend(api, debugMode)` остаётся shorthand для standard profile.

Для Android startup policy использует `SdlGpuDeviceCreateInfo.AndroidMobile(debugMode)`: `ClipDistance`, `DepthClamping`, `IndirectDrawFirstInstance` и `Anisotropy` передаются как `false` в SDL GPU device properties.

## Диагностика

Каждое событие содержит kind, message, размер окна, DPI scale, fullscreen flag и optional error. Сейчас логируются:

- `DeviceCreated`;
- `WindowClaimed`;
- `FrameBegan`;
- `FrameSubmitted`;
- `Resized`;
- `FullscreenChanged`;
- `DeviceError`;
- `Shutdown`.

Этот журнал предназначен для runtime diagnostics и тестового хоста. Он не является публичным API игры.

`SdlGpuStartupResult.ToLogLine()` дополняет lifecycle events однострочным startup result: выбранный backend, profile, fallback flag, GPU name, driver name/version и root reasons.

## Window handle

Production adapter `SdlGpuApi` требует валидный native `SDL_Window` handle в `SdlGpuWindowInfo.NativeWindowHandle`, чтобы вызвать `SDL_ClaimWindowForGPUDevice`. В CI smoke tests используется deterministic fake adapter, потому что headless окружение не гарантирует window server и GPU.

## Android startup policy

`SdlGpuStartupPolicy` выполняет:

1. создаёт `SdlGpuRenderingBackend` с Android mobile или standard create profile;
2. вызывает `Initialize(window)`;
3. запускает `SdlGpuMobileSmokeTest`;
4. при успехе выбирает `SDL_GPU`;
5. при ошибке выбирает `Compatibility`, если policy равна `Automatic`;
6. при ошибке выбрасывает `SdlGpuStartupException`, если policy равна `FailIfUnavailable`.

Подробности описаны в [Android mobile GPU smoke и fallback policy baseline](android-mobile-gpu-fallback-policy.md).

## Проверки

Целевой набор:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore
```

Он проверяет pinned SDL3-CS dependency, successful lifecycle, resize/high-DPI logging, fullscreen logging, ошибки device/command buffer и запрет неверного порядка frame calls.

Дополнительно `SdlGpuStartupPolicyTests` проверяют Android mobile create info, smoke steps, `Automatic` fallback, `FailIfUnavailable` failure и structured startup log.
