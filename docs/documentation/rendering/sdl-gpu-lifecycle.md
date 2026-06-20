# SDL_GPU lifecycle baseline

Статус: реализовано.
Задача: `T-0023`.
Обновлено: 2026-06-21.

## Назначение

Electron2D использует SDL3-CS как managed binding к SDL3/SDL_GPU. В `0.1.0 Preview` lifecycle SDL_GPU остаётся internal runtime surface и не расширяет public Godot-like API.

Реализованный baseline закрывает:

- закреплённую managed dependency `SDL3-CS` версии `3.4.10.3`;
- internal adapter `SdlGpuApi` для `SDL_CreateGPUDevice`, `SDL_ClaimWindowForGPUDevice`, `SDL_AcquireGPUCommandBuffer`, `SDL_SubmitGPUCommandBuffer` и `SDL_DestroyGPUDevice`;
- internal `SdlGpuRenderingBackend` со state machine device/window/frame/shutdown;
- диагностический журнал событий lifecycle;
- smoke-тесты без зависимости от реального окна или GPU на CI.

## State machine

`SdlGpuRenderingBackend` проходит состояния:

```text
NotInitialized -> DeviceCreated -> WindowClaimed -> FrameOpen -> WindowClaimed -> Shutdown
```

Ошибки device creation, window claim, command buffer acquire и command buffer submit переводят backend в `Failed`, добавляют событие `DeviceError` и выбрасывают `InvalidOperationException` с исходной SDL/fake-adapter причиной.

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

## Window handle

Production adapter `SdlGpuApi` требует валидный native `SDL_Window` handle в `SdlGpuWindowInfo.NativeWindowHandle`, чтобы вызвать `SDL_ClaimWindowForGPUDevice`. В CI smoke tests используется deterministic fake adapter, потому что headless окружение не гарантирует window server и GPU.

## Проверки

Целевой набор:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore
```

Он проверяет pinned SDL3-CS dependency, successful lifecycle, resize/high-DPI logging, fullscreen logging, ошибки device/command buffer и запрет неверного порядка frame calls.
