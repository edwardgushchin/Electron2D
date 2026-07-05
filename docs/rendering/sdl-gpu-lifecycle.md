# SDL_GPU lifecycle baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`T-0023` должен довести внутренний SDL_GPU lifecycle до проверяемого release-baseline для `0.1-preview`. Эта задача не реализует canvas drawing, textures или shaders. Она вводит:

- controlled dependency на SDL3-CS release line;
- internal lifecycle adapter для SDL_GPU device, swapchain/window claim, command buffer acquire/submit;
- состояние renderer lifecycle;
- smoke tests для resize, fullscreen, high-DPI и device errors;
- диагностический журнал lifecycle events.

## Источники поведения

- [SDL3-CS](https://github.com/edwardgushchin/SDL3-CS) release line `3.4.10.x`;
- [SDL_CreateGPUDevice](https://wiki.libsdl.org/SDL3/SDL_CreateGPUDevice);
- [SDL_ClaimWindowForGPUDevice](https://wiki.libsdl.org/SDL3/SDL_ClaimWindowForGPUDevice);
- [SDL_AcquireGPUCommandBuffer](https://wiki.libsdl.org/SDL3/SDL_AcquireGPUCommandBuffer);
- [SDL_SubmitGPUCommandBuffer](https://wiki.libsdl.org/SDL3/SDL_SubmitGPUCommandBuffer).

SDL создаёт GPU context через `SDL_CreateGPUDevice`, claim окна создаёт swapchain structure, command buffer должен быть acquired и submitted на том же thread, а после submit command buffer нельзя использовать повторно.

## Dependency policy

Runtime project должен ссылаться на managed package:

```xml
<PackageReference Include="SDL3-CS" Version="3.4.10.3" />
```

Platform native packages не добавляются в library project `Electron2D`, потому что они зависят от target app/export profile. Они будут подключаться export/template задачами. `T-0023` фиксирует managed API dependency и internal adapter contract.

## Internal API

Минимальная internal surface:

```csharp
internal sealed class SdlGpuRenderingBackend : RenderingBackend
{
    SdlGpuLifecycleState State { get; }
    IReadOnlyList<SdlGpuLifecycleEvent> Events { get; }

    void Initialize(SdlGpuWindowInfo window);
    SdlGpuFrame BeginFrame();
    void EndFrame(SdlGpuFrame frame);
    void Resize(int width, int height, float dpiScale);
    void SetFullscreen(bool fullscreen);
    void Shutdown();
}
```

Native calls проходят через internal adapter:

```csharp
internal interface ISdlGpuApi
{
    SdlGpuDeviceHandle CreateDevice(bool debugMode, out string? error);
    bool ClaimWindow(SdlGpuDeviceHandle device, SdlGpuWindowInfo window, out string? error);
    SdlGpuCommandBufferHandle AcquireCommandBuffer(SdlGpuDeviceHandle device, out string? error);
    bool SubmitCommandBuffer(SdlGpuCommandBufferHandle commandBuffer, out string? error);
    void DestroyDevice(SdlGpuDeviceHandle device);
}
```

`SdlGpuApi` является production adapter для SDL3-CS. Smoke tests используют deterministic fake adapter, чтобы CI не зависел от реального GPU/window server.

## Lifecycle states

`SdlGpuLifecycleState`:

- `NotInitialized`;
- `DeviceCreated`;
- `WindowClaimed`;
- `FrameOpen`;
- `Failed`;
- `Shutdown`.

Разрешённый путь:

```text
NotInitialized -> DeviceCreated -> WindowClaimed -> FrameOpen -> WindowClaimed -> Shutdown
```

При ошибке device, claim, acquire или submit backend переходит в `Failed`, записывает событие с причиной и запрещает дальнейшее использование до `Shutdown()`.

## Diagnostics

Каждое событие содержит:

- `Kind`;
- `Message`;
- `Width`;
- `Height`;
- `DpiScale`;
- `Fullscreen`;
- `Error`.

Обязательные события:

- `DeviceCreated`;
- `WindowClaimed`;
- `FrameBegan`;
- `FrameSubmitted`;
- `Resized`;
- `FullscreenChanged`;
- `DeviceError`;
- `Shutdown`.

## Smoke tests

Smoke tests должны покрыть:

- successful lifecycle: initialize -> begin frame -> end frame -> shutdown;
- resize logging с width/height/high-DPI scale;
- fullscreen logging;
- device creation failure;
- command buffer acquire/submit failure;
- запрет `BeginFrame()` до initialize;
- запрет double `BeginFrame()`.

## Ограничения `T-0023`

- Реальное окно SDL и GPU smoke triangle не запускаются в CI.
- Texture upload, resource restore и GPU leak tracking остаются задачей `T-0025`.
- Canvas sorting/batching остаётся задачей `T-0024`.
- Public API не расширяется новыми renderer lifecycle types; всё остаётся internal runtime surface.

## Фактическое состояние, ограничения и проверки

Статус: реализовано.
Задачи: `T-0023`, `T-0034`.
Обновлено: 2026-06-21.

## Назначение

Electron2D использует SDL3-CS как managed binding к SDL3/SDL_GPU. В `0.1-preview` lifecycle SDL_GPU остаётся internal runtime surface и не расширяет public Electron2D API.

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
