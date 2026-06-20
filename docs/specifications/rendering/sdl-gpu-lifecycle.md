# SDL_GPU lifecycle baseline

## Назначение

`T-0023` должен довести внутренний SDL_GPU lifecycle до проверяемого release-baseline для `0.1.0 Preview`. Эта задача не реализует canvas drawing, textures или shaders. Она вводит:

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
