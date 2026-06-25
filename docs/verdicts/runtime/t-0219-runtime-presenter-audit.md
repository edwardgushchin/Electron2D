# Вердикт T-0219: runtime presenter и повторная приёмка

- Задача: `T-0219`.
- Домен verdicts: `runtime`.
- Актуально на: `2026-06-25T15:17:12+03:00`.
- Область проверки: интерактивный путь показа кадра `RuntimeHost`, основной presenter, запасной presenter, PNG-снимок, диагностика, texture cache и связанные тесты.
- Статус вывода: `T-0219` не принимать; оставить `in progress` до устранения блокеров ниже.

## Решение

**`T-0219` не принимать. Оставить `in progress` и вернуть на доработку.**

Основная архитектурная цель достигнута: появился настоящий SDL GPU path с pipelines/render pass/draw, отдельный `SDL_Renderer` fallback, а PNG читается из активного presenter-а. Но текущий результат ещё не доказывает корректный steady-state, cache invalidation, visual semantics и recoverable fallback.

### Что уже можно считать выполненным

* SDL GPU действительно использует `CreateGPUGraphicsPipeline`, `BeginGPURenderPass` и `DrawGPUPrimitives`.
* PNG в GPU-пути читается после fence; это корректная последовательность SDL GPU. ([Wiki SDL][1])
* Fallback читает тот же отрисованный кадр через `RenderReadPixels`, а не через `RuntimePreviewFrameRasterizer`.
* Плановые batches и фактические draw calls теперь разделены.
* Широкий `catch (Exception)` устранён: wrapper переключается только по `GpuPresenterUnavailableException`.
* Старые покадровые одноэлементные массивы и LINQ в polygon fallback удалены.
* Прямая утечка необработанных pending transfer buffers в основных error-path исправлена.

## Блокеры

### 1. Текущий тестовый пакет внутренне противоречив

В [`RuntimeHostTests.cs`](../../../tests/Electron2D.Tests.Integration/RuntimeHostTests.cs), строки 297–301, GPU factory бросает обычный `InvalidOperationException`.

Однако [`RuntimeFramePresenter.cs`](../../../src/Electron2D/Runtime/Application/RuntimeFramePresenter.cs), строки 120–131, при создании presenter-а перехватывает только `GpuPresenterUnavailableException`.

Следовательно, тест `RuntimeFramePresenterUsesSdlRendererFallbackWhenGpuPresenterCreationFails` не может дойти до своих assertions: конструктор должен выбросить исключение.

Дополнительно текущий файл содержит **12 `[Fact]`**, а дневник по-прежнему сообщает `RuntimeHostTests — 8/8`. Значит, записанное зелёное evidence относится к более ранней версии. В приложенном архиве нет solution/csproj, поэтому независимо запустить suite невозможно, но статического противоречия уже достаточно для отклонения.

### 2. При интерактивном запуске PNG читается каждый кадр

[`RuntimeHost.ShouldCaptureFrame()`](../../../src/Electron2D/Runtime/Application/RuntimeHost.cs), строки 374–381:

```csharp
return frameLimit == 0 || frameIndex == targetFrames - 1;
```

При `FrameLimit == 0` и заданном `ScreenshotPath` это означает screenshot readback **на каждом кадре до закрытия окна**.

GPU-путь каждый кадр:

* повторно рендерит сцену в screenshot texture;
* ждёт fence;
* создаёт `new byte[width * height * 4]`.

Fallback каждый кадр делает `RenderReadPixels`, conversion surface и managed copy.

Это нарушает документированный контракт об одноразовом буфере PNG и делает интерактивный screenshot-path особенно тяжёлым.

### 3. Allocation metric недостоверна

В [`RuntimeFramePresenter.cs`](../../../src/Electron2D/Runtime/Application/RuntimeFramePresenter.cs), строки 91–97, измерение начинается только внутри `Present()`. Оно не учитывает:

* input;
* physics/process;
* построение `CanvasItemRenderPlan`;
* остальные части кадра `RuntimeHost`.

При screenshot значение дополнительно считается дважды: размер `RgbaPixels` уже входит в разницу `GC.GetAllocatedBytesForCurrentThread()`, а строки 149–152 ещё раз прибавляют `ManagedFrameBufferBytesAllocated`.

Также прогрев равен одному вызову presenter-а, а наружу отдаётся только значение последнего кадра. Это не метрика, пригодная для `T-0221`.

Есть и фактический источник постоянного роста: [`SdlGpuRenderingBackend`](../../../src/Electron2D/Graphics/Rendering/SdlGpuRenderingBackend.cs), строки 126–150 и 241–243, добавляет по два lifecycle event в `_events` на каждый кадр. Список не ограничивается и не очищается до повторной инициализации. Это даёт неограниченный рост памяти и периодические перевыделения `List`.

Проектный контракт прямо запрещает GC allocations, рост `List`, LINQ и `foreach` по non-span коллекциям в горячем пути. 

### 4. Texture cache invalidation фактически не работает

Оба presenter-а сравнивают `texture.RenderContentVersion`, но в [`Texture2D.cs`](../../../src/Electron2D/Graphics/Rendering/Texture2D.cs), строка 249:

```csharp
internal virtual long RenderContentVersion => 0;
```

В представленном changeset нет конкретной реализации, меняющей версию ресурса. Таким образом, кеш видит любой ранее загруженный объект как неизменный.

Не хватает теста:

```text
первый кадр: upload
второй кадр: cache hit
изменение/reload ресурса
следующий кадр: новый upload
```

Кроме того, если texture upload уже закодирован, но последующая отправка кадра отменяется, transfer buffer освобождается, однако соответствующая texture может остаться в кеше как успешно загруженная.

### 5. Поворот и полный transform draw-команд потеряны

В fallback [`DrawTexture()`](../../../src/Electron2D/Runtime/Application/RuntimeSdlRendererFramePresenter.cs), строка 204, угол жёстко равен `0d`.

Оба presenter-а сначала преобразуют `Rect2`, а затем строят новый axis-aligned rectangle. `Rect2` не может сохранить произвольный поворот:

* GPU: строки 895–914 и 1039–1063;
* fallback: строки 192–205 и 528–535.

Дополнительно:

* текст получает преобразование только стартовой позиции, но не glyph geometry;
* круг преобразует центр, но игнорирует scale transform для радиуса;
* прямоугольники также теряют rotation.

То есть два presenter-а могут совпасть между собой именно потому, что оба рисуют неверно. Нужна не только parity-проверка, но и ожидаемая геометрия.

### 6. Lifecycle `SDL_shadercross` всё ещё неверный

[`RuntimeShaderCrossService.cs`](../../../src/Electron2D/Runtime/Application/RuntimeShaderCrossService.cs) вызывает:

* `Init()` при переходе owner count `0 -> 1`;
* `Quit()` при `1 -> 0`.

После закрытия одного окна следующий presenter снова вызовет `Init()`. Поток вызова также никак не закреплён.

Контракт SDL_shadercross указывает, что `Init` и `Quit` должны вызываться по одному разу из одного потока. ([Simple Directmedia Layer][2])

Нужен process-lifetime owner: одна инициализация и одно завершение при shutdown процесса, а не обычный reference counting между окнами.

### 7. Recoverable fallback покрывает не все GPU failures

Типизированная граница в wrapper-е стала правильнее, но backend по-прежнему бросает обычный `InvalidOperationException` при:

* ошибке acquisition command buffer;
* submit;
* fence submit;
* fence wait.

Это видно в [`SdlGpuRenderingBackend.cs`](../../../src/Electron2D/Graphics/Rendering/SdlGpuRenderingBackend.cs), строки 120–182 и 228–233.

Ряд runtime GPU failures в presenter-е также остаётся обычным `InvalidOperationException`: render pass, copy pass, texture/buffer creation. Поэтому документированная device-loss/resource failure может пройти наружу вместо переключения на `SDL_Renderer`.

Нужна явная классификация:

* `GpuPresenterUnavailableException` или typed error code — только device/driver/swapchain/device-loss failures;
* `ArgumentException`, invalid render plan, resource-data error и invariant violation — наружу без fallback.

### 8. Diagnostics и resize contract не завершены

GPU presenter всегда возвращает:

```csharp
PresentationResourcesRecreated: 0
```

При этом screenshot texture и transfer buffer реально пересоздаются при изменении размера. Pipeline-state switches, которые требуют доменные документы, вообще отсутствуют в `RuntimeFrameDiagnostics`. Сам контракт требует planned batches, actual calls, texture/pipeline switches и фактическую managed allocation delta. 

Формулировку resize-критерия также стоит сделать backend-neutral. SDL-owned swapchain и `SDL_Renderer` не обязаны пересоздаваться тем же способом, что engine-owned size-dependent textures. Нужно считать:

* observed resize;
* engine-owned resource recreations;
* backend/swapchain reconfiguration;

а не требовать фиктивное пересоздание там, где SDL управляет им самостоятельно.

### 9. Ошибка размера MSL-кода на macOS

В [`RuntimeFramePresenter.cs`](../../../src/Electron2D/Runtime/Application/RuntimeFramePresenter.cs), строки 727–738:

```csharp
Marshal.PtrToStringUTF8(code)?.Length
```

возвращает число UTF-16 символов C#-строки, а `SDL_GPUShaderCreateInfo.code_size` требует **количество байтов** shader code. ([Wiki SDL][3])

Для текущего ASCII MSL это может случайно совпадать, но контракт некорректен и создаёт платформенный риск.

## Оценка постановки `T-0219`

Текущее описание основной цели поставлено правильно: SDL GPU primary, SDL Renderer fallback, PNG из активного presenter-а, без отдельного программного framebuffer.

Но в [`TASKS.md`](../../../TASKS.md) нужно исправить несколько вещей.

### Убрать зависимость от `T-0227`

`T-0219` не должна зависеть от полного публичного Godot 4.7 API `Texture2D`/`AtlasTexture`.

Полная рекурсивная семантика `AtlasTexture`, `Margin`, `FilterClip`, `GetImage()` и public draw API — самостоятельный scope. Более того, старый preview rasterizer в текущем [`RuntimeHost.cs`](../../../src/Electron2D/Runtime/Application/RuntimeHost.cs), строки 505–521, уже имел то же ограничение `AtlasTexture -> ImageTexture`. Оно не появилось из-за `T-0219`.

Для `T-0219` достаточно потребовать:

* единый внутренний resolver для уже поддерживаемых texture resources;
* отсутствие расхождения GPU/fallback;
* рабочую content revision/cache invalidation.

### Скорректировать критерии

Можно уже отметить выполненным разделение `RenderBatches` и `ActualDrawCalls`: GPU отправляет draw по batches, fallback честно считает реальные SDL-вызовы. Это соответствует доменному правилу не выдавать плановые batches за фактическую стоимость fallback-а. 

Нужно снять `[x]` с критерия о red tests на allocations: текущие тесты не ловят screenshot readback каждый кадр, unbounded lifecycle log и allocations вне `Present()`.

Критерий shadercross следует заменить на:

> `SDL_shadercross` инициализируется один раз на процесс из render-owning thread и завершается один раз при shutdown процесса.

Критерий managed allocations должен точно определить:

* границу измерения: весь runtime frame либо только presenter;
* минимум 120 warm-up frames;
* минимум 600 измеряемых кадров без PNG;
* `maxAllocatedBytesPerFrame == 0`, а не только среднее;
* отдельную метрику одноразового PNG;
* отсутствие роста внутренних diagnostic collections.

Нужны forced-backend fixtures: один и тот же command plan через принудительный SDL GPU и принудительный SDL Renderer с проверкой texture flip, rotation, glyphs, circle и polygon.

## Минимум для повторной приёмки

1. Исправить противоречащий fallback-тест и прогнать актуальные 12/12.
2. Убрать screenshot readback с каждого интерактивного кадра.
3. Сделать полную и недублирующую allocation-метрику, удалить unbounded frame logging.
4. Реализовать настоящий content revision и тест cache invalidation.
5. Сохранить полный transform геометрии и добавить forced GPU/fallback visual tests.
6. Завершить typed recoverable-failure mapping.
7. Перевести shadercross на one-shot process lifetime.
8. Исправить MSL `code_size` в байтах.
9. Синхронизировать diagnostics, документацию и task checkboxes с фактическим состоянием.

## SDL3-CS

**Issue в SDL3-CS сейчас не требуется.** Массив fence handles соответствует нативной сигнатуре `SDL_GPUFence *const *fences`, а readback после ожидания fence соответствует SDL GPU workflow. ([Wiki SDL][4])

Найденные блокеры находятся в Electron2D: lifecycle, diagnostics, screenshot policy, transforms, cache revision, error classification и тесты. `Thread.Sleep(16)` остаётся дефектом `T-0220` и не является самостоятельной причиной отклонения именно `T-0219`.

[1]: https://wiki.libsdl.org/SDL3/CategoryGPU "SDL3/CategoryGPU"
[2]: https://discourse.libsdl.org/t/sdl-gpu-shadercross-implement-build-system-and-cli-support/55220 "SDL_gpu_shadercross: Implement build system and CLI ..."
[3]: https://wiki.libsdl.org/SDL3/SDL_GPUShaderCreateInfo "SDL3/SDL_GPUShaderCreateInfo"
[4]: https://wiki.libsdl.org/SDL3/SDL_WaitForGPUFences "SDL3/SDL_WaitForGPUFences - SDL Wiki"
