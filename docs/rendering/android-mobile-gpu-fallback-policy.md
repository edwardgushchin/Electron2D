# Android mobile GPU smoke и fallback policy baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация `T-0034`.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Архитектура и платформенный стек Electron2D](../architecture/engine-platform-stack.md), [SDL_GPU lifecycle baseline](sdl-gpu-lifecycle.md), [SDL_Renderer Compatibility backend baseline](sdl-renderer-compatibility-backend.md).

## Назначение

`T-0034` должна добавить внутреннюю startup policy для renderer backend:

1. на Android создавать SDL_GPU device в mobile-compatible profile;
2. выполнять smoke test, который проверяет texture path, pipeline capability, command buffer и первый submit;
3. при успехе выбирать `SDL_GPU` backend;
4. при ошибке применять `Compatibility` fallback или останавливать запуск по настройке policy;
5. сохранять structured result/log с GPU, driver, выбранным backend и причинами fallback/failure.

Это не compatibility layer для старого public API. Публичная Electron2D поверхность не расширяется.

## Источники поведения

- [`SDL_CreateGPUDeviceWithProperties`](https://wiki.libsdl.org/SDL3/SDL_CreateGPUDeviceWithProperties) поддерживает свойства создания GPU device, включая optional Vulkan feature flags.
- [`SDL GPU API`](https://wiki.libsdl.org/SDL3/CategoryGPU) описывает базовый workflow: device, window claim, texture/buffer/pipeline resources, command buffer и submit.
- [`SDL_AcquireGPUCommandBuffer`](https://wiki.libsdl.org/SDL3/SDL_AcquireGPUCommandBuffer) и [`SDL_SubmitGPUCommandBuffer`](https://wiki.libsdl.org/SDL3/SDL_SubmitGPUCommandBuffer) остаются обязательными smoke-шагами первого frame submit.

SDL documentation указывает, что отключение optional Vulkan features расширяет поддержку старых Android-устройств. Для Android mobile profile Electron2D должен создавать device с отключёнными:

- `clip_distance`;
- `depth_clamping`;
- `indirect_draw_first_instance`;
- `anisotropy`.

## Internal API

Минимальная внутренняя модель:

```csharp
internal readonly struct SdlGpuDeviceCreateInfo
{
    bool DebugMode { get; }
    SdlGpuOptionalFeaturePolicy OptionalFeatures { get; }
}

internal enum SdlGpuStartupPlatform
{
    Desktop,
    Android
}

internal enum SdlGpuFallbackPolicy
{
    Automatic,
    FailIfUnavailable
}
```

`SdlGpuStartupPolicy` должна принимать:

- `ISdlGpuApi`;
- smoke test implementation;
- `SdlGpuWindowInfo`;
- platform;
- fallback policy;
- debug flag.

Результат startup должен содержать:

- выбранный backend name;
- выбранный `RenderingProfile`;
- был ли применён fallback;
- GPU name;
- driver name;
- driver version/info;
- список reasons;
- smoke step results.

## Smoke steps

Smoke test должен записывать шаги в стабильном порядке:

1. `Texture` - проверка базового texture path;
2. `Pipeline` - проверка pipeline capability;
3. `CommandBuffer` - `BeginFrame()`/acquire command buffer;
4. `FirstSubmit` - `EndFrame()`/submit command buffer.

Если ранний шаг падает, поздние шаги должны быть помечены как не выполненные с понятной причиной. Это важно для diagnostics: агент или человек должен видеть не только итог "fallback", но и точную точку отказа.

## Fallback policy

`Automatic`:

- если SDL_GPU mobile smoke проходит, runtime выбирает `SdlGpuRenderingBackend`;
- если device creation, window claim или smoke step падает, runtime выбирает `CompatibilityRenderingBackend`;
- result/log содержит причины fallback.

`FailIfUnavailable`:

- если SDL_GPU mobile smoke проходит, runtime выбирает `SdlGpuRenderingBackend`;
- если device creation, window claim или smoke step падает, startup выбрасывает internal `SdlGpuStartupException`;
- exception содержит startup result с GPU/driver/backend/reasons, чтобы caller мог вывести понятную ошибку.

## Acceptance tests

- Android startup использует `SdlGpuDeviceCreateInfo` с отключёнными optional Vulkan features.
- Successful smoke test проверяет texture, pipeline, command buffer и first submit, затем выбирает `SDL_GPU` backend.
- `Automatic` policy при smoke failure выбирает `Compatibility` backend и сохраняет reason.
- `FailIfUnavailable` policy при smoke failure выбрасывает `SdlGpuStartupException` и не применяет silent fallback.
- Startup log/result содержит GPU, driver, выбранный backend и reasons.
- Public API compatibility verifier не должен увидеть новых public types.
- Source license verifier должен пройти для новых C# files.

## Не входит в `T-0034`

- Реальное Android package/export.
- Реальный swapchain screenshot.
- Полный shader pipeline smoke с platform-specific compiled shaders.
- Project setting UI для выбора `automatic`/`fail_if_unavailable`.
- Public `RenderingProfile.Mobile`; mobile profile остаётся internal SDL_GPU create policy.

## Фактическое состояние, ограничения и проверки

Статус: реализовано.
Задача: `T-0034`.
Обновлено: 2026-06-21.

## Назначение

Добавлена внутренняя startup policy для renderer backend. Она создаёт standard graphics backend с platform-specific create info, выполняет smoke test и выбирает один из вариантов:

- `Standard`, если smoke проходит;
- `Compatibility`, если smoke падает и policy равна `Automatic`;
- structured failure, если smoke падает и policy равна `FailIfUnavailable`.

Публичный API не изменился. Mobile profile остаётся internal policy для graphics device creation, а не новым public `RenderingProfile`.

## Android mobile create profile

Для Android используется mobile-compatible graphics create profile. В нём отключены optional graphics features, которые чаще всего ограничивают совместимость старых mobile drivers:

- `ClipDistance`;
- `DepthClamping`;
- `IndirectDrawFirstInstance`;
- `Anisotropy`.

Production adapter передаёт эти значения в native graphics device properties. Стандартный desktop profile оставляет эти optional features включёнными.

## Smoke test

Mobile graphics smoke test выполняет шаги в стабильном порядке:

1. `Texture` - базовая проверка texture path через adapter;
2. `Pipeline` - проверка pipeline capability через shader format availability;
3. `CommandBuffer` - `BeginFrame()` и acquire command buffer;
4. `FirstSubmit` - `EndFrame()` и submit command buffer.

Если ранний шаг падает, поздние шаги помечаются как skipped с причиной `previous smoke step failed.`, но startup result выводит только корневые причины отказа.

## Startup result и log

Startup result содержит:

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
backend=Compatibility|profile=Compatibility|fallback=True|gpu=Adreno 730|driver=mobile-driver|driverVersion=1.3.250|reasons=graphics pipeline creation failed on mobile driver.
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
