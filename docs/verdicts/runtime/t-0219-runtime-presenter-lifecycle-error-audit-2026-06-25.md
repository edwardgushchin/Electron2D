# Аудит T-0219: жизненный цикл и ошибки runtime presenter

- Задача: `T-0219`.
- Домен: `runtime`.
- Актуально на: `2026-06-25T17:45:00+03:00`.
- Область проверки: жизненный цикл `SDL_shadercross`, допустимые пути завершения GPU command buffer, ограничение диагностических событий, смысл счётчика выделений при снимке кадра, транзакционность загрузки текстур и проверка цвета polygon в visual fixture.
- Статус вывода: `T-0219` не принимать; оставить `in progress` до закрытия всех блокеров ниже.

# Решение: **вернуть T‑0219 на доработку**

Задачу оставить в состоянии `in progress`. Архитектурную реализацию откатывать не нужно, но закрывать и переносить в completed archive пока нельзя.

## Блокирующие дефекты

### 1. Lifecycle `SDL_shadercross` фактически не завершён

Критерий задачи требует, чтобы `SDL_ShaderCross_Quit()` вызывал явный владелец процесса или приложения.

В представленных изменениях:

* `RuntimeShaderCrossService.Release()` ничего не делает — `RuntimeShaderCrossService.cs:60–63`;
* `ShutdownOnRenderThread()` реализован — строки `65–82`;
* production-вызова `ShutdownOnRenderThread()` в архиве нет;
* тест лишь проверяет наличие строк `Init`, `Quit`, `ShutdownOnRenderThread` и отдельно закрепляет, что `RuntimeHost` shutdown не вызывает — `RuntimeHostTests.cs:657–663`.

То есть создана функция завершения, но не создан владелец жизненного цикла. Последовательные sessions используют один `Init`, однако финального управляемого `Quit` нет.

Это особенно важно, поскольку upstream-контракт требует вызывать `SDL_ShaderCross_Init()` и `SDL_ShaderCross_Quit()` по одному разу из одного потока. ([GitHub][1])

Нужно:

* назначить конкретный process/application composition root;
* вызвать shutdown до завершения владеющего render thread;
* добавить поведенческий тест через инъецируемый ShaderCross adapter:

  * две последовательные `RuntimeHost` sessions → `Init == 1`;
  * завершение каждой session → `Quit == 0`;
  * application shutdown → `Quit == 1`;
  * повторный shutdown идемпотентен;
  * shutdown с другого потока отклоняется;
  * shutdown при активной lease запрещён либо имеет явно определённую семантику.

### 2. Некорректная отмена GPU command buffer после получения swapchain texture

В `RuntimeGpuFramePresenter.Present()`:

1. swapchain texture получается в `RuntimeFramePresenter.cs:359–367`;
2. после этого выполняются создание ресурсов, uploads, render passes и submit;
3. общий `catch` при любой ошибке вызывает `SDL.CancelGPUCommandBuffer()` — строки `411–419`.

SDL прямо указывает, что `SDL_CancelGPUCommandBuffer()` нельзя вызывать после получения swapchain texture. Такой вызов является ошибкой. ([Wiki SDL][2]) Этот путь срабатывает, в частности, при:

* первом сбое создания pipeline после получения swapchain;
* ошибке texture upload;
* ошибке render/copy pass;
* ошибке submit;
* ошибке подготовки screenshot.

Следовательно, typed fallback присутствует, но часть recoverable GPU failures завершается с нарушением SDL lifecycle.

Нужно разделить error paths:

* до получения swapchain texture command buffer может быть отменён;
* после получения swapchain должен использоваться допустимый SDL terminal path;
* failure-prone setup следует максимально перенести до swapchain acquisition;
* добавить fault-injection tests для acquire, pipeline, upload, render pass, copy pass, submit, fence submit и fence wait;
* проверять, что command buffer завершается ровно один раз и допустимым способом.

## Остальные обязательные исправления

### Диагностическая коллекция всё ещё не bounded

`SdlGpuRenderingBackend` хранит события в обычном неограниченном `List`:

* поле `_events` — `SdlGpuRenderingBackend.cs:50`;
* каждый resize и fullscreen transition добавляется через `_events.Add(...)` — строки `195–208`, `245–267`;
* подавляются только повторные `FrameBegan` и `FrameSubmitted`.

Тест `SdlGpuBackendKeepsLifecycleEventsBoundedAcrossSteadyFrames()` проверяет 700 кадров без resize — `SdlGpuLifecycleTests.cs:134–148`. Он не обнаружит рост списка при длительном перетаскивании границы окна.

Это противоречит критериям `TASKS.md:1954`, `1966` и подзадаче `1984`.

Нужен фиксированный ring buffer, ограничение количества событий с `DroppedEventCount` либо хранение последнего события каждого вида. Тест должен выполнить сотни или тысячи чередующихся resize/fullscreen transitions.

### `PngManagedBytesAllocated` имеет недостоверную семантику

Счётчик начинается перед всем вызовом `activePresenter.Present()` — `RuntimeFramePresenter.cs:92`, а при `captureFrame` вся полученная разница записывается как PNG allocations — строки `148–177`.

Поэтому в него попадают:

* первичное расширение внутренних списков;
* построение presenter geometry;
* texture resolver/cache allocations;
* однократная инициализация capture resources;
* `RuntimeFrameSnapshot`;
* непосредственно массив пикселей.

При этом в него **не входят** PNG encoding и запись файла, выполняемые позже через `SaveScreenshotIfRequested()` — `RuntimeHost.cs:211–213`.

XML-документация, однако, называет значение «managed bytes allocated by one-time PNG readback» — `RuntimeHostResult.cs:439–446`.

Нужно выбрать один точный контракт:

* либо переименовать свойство в `CapturePresenterManagedBytesAllocated` и документировать весь capture-вызов;
* либо измерять непосредственно readback/output-buffer section;
* PNG encoding и файловую запись при необходимости учитывать отдельными метриками.

### GPU texture upload criterion не доказан failure-тестом

Texture добавляется в `textureCache` до успешной отправки command buffer — `RuntimeFramePresenter.cs:1451–1454`. Pending uploads очищаются в `UploadPendingResources()` до вызова `backend.EndFrame()` — строки `1191–1195`.

В штатном wrapper-пути submit failure приводит к disposal всего GPU presenter, поэтому итоговая утечка обычно маскируется. Но критерий `TASKS.md:1958` сформулирован сильнее: не сохранять texture в кеше, если upload не был отправлен. Поведенческого теста этого сценария нет.

Нужна транзакционная модель:

```text
created -> pending -> submitted -> committed to cache
```

При любой ошибке до submit texture должна удаляться из staged/cache state. Это следует проверять через инъецируемый GPU API, а не поиском строк в исходнике.

### Visual fixture подтверждает геометрию, но не корректность цвета polygon

Fixture передаёт три зелёных polygon colors — `RuntimeHostTests.cs:1251–1263`, однако ожидает белый пиксель — строка `1110`.

Оба presenter-а используют для polygon только `command.EffectiveModulate`:

* GPU: `RuntimeFramePresenter.cs:992–1007`;
* fallback: `RuntimeSdlRendererFramePresenter.cs:307–323`.

Это не обязательно нарушает текущую узкую формулировку «ожидаемая геометрия», но такой тест нельзя считать доказательством визуальной эквивалентности нового renderer-а. Цвет, alpha и per-vertex colors должны быть либо проверены, либо явно вынесены из scope.

## Что выполнено качественно

Можно сохранить без переработки основного подхода:

* `RuntimeHost` больше не использует `RuntimePixelCanvas` и software framebuffer как обычный интерактивный renderer;
* основной путь действительно использует SDL GPU pipelines, render passes и draw calls;
* `SDL_Renderer` оформлен как отдельный fallback;
* pipelines, vertex buffers, transfer buffers, sampler и texture cache переиспользуются;
* PNG получается из активного presenter-а и не читается каждый интерактивный кадр;
* unsupported texture и неизвестная render-команда больше не превращаются в прямоугольник;
* fallback ограничен `GpuPresenterUnavailableException`;
* реальные allocation tests используют текст, 120 warm-up и 600 measured frames для GPU и fallback;
* исправлены texture flip, transform, толстая fallback-линия и MSL byte length;
* planned batches отделены от actual draw calls;
* заявленные `45/45` арифметически сходятся: `27 + 8 + 7 + 3`.

Независимо воспроизвести прогон по архиву нельзя: в нём нет solution/project files, а в доступном окружении отсутствует `dotnet`. Статические блокеры выше достаточны для решения о возврате независимо от заявленных зелёных тестов.

# Оценка постановки T‑0219

Основная цель поставлена правильно:

* хорошо описан исходный performance blocker;
* корректно разделены SDL GPU primary и `SDL_Renderer` fallback;
* presenter-boundary allocations отделены от полного runtime-frame бюджета `T‑0221`;
* полная Godot-совместимость `Texture2D`/`AtlasTexture` обоснованно оставлена за `T‑0226`/`T‑0227`.

Но задача стала чрезмерно большой: в одной T‑0219 объединены presenter architecture, GPU lifecycle, error taxonomy, texture caching, ShaderCross lifetime, PNG readback, allocations, diagnostics, visual semantics и документация. Из-за этого критерии несколько раз отмечались `[x]` после поверхностных source-checks.

Следует исправить формулировки:

* заменить «tests сначала падают» на проверяемую ссылку на commit/evidence; финальный snapshot сам по себе порядок разработки не доказывает;
* удалить «12/12 или другое фактическое количество» — это нефальсифицируемый критерий;
* задать точный process owner для ShaderCross, а не абстрактного «владельца приложения»;
* определить допустимые terminal states GPU command buffer до и после swapchain acquisition;
* определить точную границу PNG/capture allocation metric;
* указать максимальную ёмкость diagnostic storage;
* для `0 B/frame` зафиксировать Release, debugger detached, поток, отсутствие screenshot, representative text/texture plan и повторяемость результата;
* уточнить, проверяется только геометрия или также цвет, alpha, blend и per-vertex attributes.

`Зависимости: нет` допустимо только в том случае, если process/application owner входит непосредственно в T‑0219. Иначе должна появиться явная зависимость от задачи, создающей composition root. Зависимости от `T‑0226` и `T‑0227` добавлять не нужно.

## Какие отметки нужно снять в `TASKS.md`

Минимум вернуть в `[ ]`:

* `1954` — red tests не ловят resize-driven diagnostic growth и незаконный command-buffer error path;
* `1958` — unsubmitted GPU upload cache behaviour не доказано;
* `1966` — PNG boundary и bounded diagnostic collections не выполнены;
* `1971` — отсутствует production owner ShaderCross shutdown;
* `1972` — error cleanup не подтверждён fault-injection tests и содержит незаконный cancel path;
* `1975` — документация PNG metric и ShaderCross lifecycle не соответствует production-поведению;
* `1984` — lifecycle log остаётся неограниченным при resize;
* `1985` — ShaderCross lifetime не завершён.

**Итоговый вердикт: доработка.** Архитектура близка к принимаемой, но два P0 error/lifecycle дефекта и несколько недостоверно закрытых критериев не позволяют принять T‑0219.

[1]: https://github.com/libsdl-org/SDL_shadercross/blob/main/include/SDL3_shadercross/SDL_shadercross.h "SDL_shadercross/include/SDL3_shadercross/SDL_shadercross.h at main · libsdl-org/SDL_shadercross · GitHub"
[2]: https://wiki.libsdl.org/SDL3/SDL_CancelGPUCommandBuffer "SDL3/SDL_CancelGPUCommandBuffer - SDL Wiki"
