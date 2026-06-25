# Повторный аудит T-0219: runtime presenter allocations

- Задача: `T-0219`.
- Домен verdicts: `runtime`.
- Актуально на: `2026-06-25T17:00:00+03:00`.
- Область проверки: повторная проверка real presenter allocations, lifecycle `SDL_shadercross`, resize diagnostics, invariant errors и fallback line width.
- Статус вывода: `T-0219` не принимать; оставить `in progress` до устранения блокеров ниже.

## Вердикт

**`T-0219` не принимать. Оставить `in progress`.**

Большинство прежних блокеров исправлено, но заявленный критерий `0 B/frame` всё ещё нарушается реальным presenter-ом.

### Блокирующий дефект

[`RuntimePixelFont.GetGlyph()`](../../../src/Electron2D/Runtime/Application/RuntimeHost.cs) возвращает новый `string[]` при каждом вызове:

```csharp
'A' => ["01110", ...]
```

Метод вызывается для каждого символа каждого кадра:

* в [GPU presenter](../../../src/Electron2D/Runtime/Application/RuntimeFramePresenter.cs);
* в [SDL_Renderer fallback](../../../src/Electron2D/Runtime/Application/RuntimeSdlRendererFramePresenter.cs).

Следовательно, любой HUD или текст создаёт managed allocations внутри измеряемой границы presenter-а.

При этом тест `RuntimeFramePresenterReportsZeroManagedAllocationsAcrossMeasuredSteadyFrames()` в [RuntimeHostTests.cs](../../../tests/Electron2D.Tests.Integration/RuntimeHostTests.cs) использует `FakeRuntimeFramePresenter` и пустой render plan. Он проверяет арифметику счётчика, но не реальный GPU или fallback presenter и не содержит текстовой команды.

Это прямо противоречит отмеченным `[x]` критериям `T-0219`:

* отсутствие managed allocations в steady state;
* 120 кадров прогрева и 600 измеряемых кадров;
* `MaxPresenterManagedBytesPerFrame == 0`;
* наличие red tests, обнаруживающих покадровые allocations.

### Второй блокер: lifecycle `SDL_shadercross`

[`RuntimeShaderCrossService`](../../../src/Electron2D/Runtime/Application/RuntimeShaderCrossService.cs) вызывает `ShaderCross.Init()`, а [`RuntimeHost`](../../../src/Electron2D/Runtime/Application/RuntimeHost.cs) вызывает `ShutdownOnRenderThread()` после каждого запуска. Shutdown выполняет `ShaderCross.Quit()` и сбрасывает `initialized = false`.

Поэтому два последовательных запуска `RuntimeHost` в одном процессе выполнят:

```text
Init -> Quit -> Init -> Quit
```

Документация SDL3-CS и upstream-контракт указывают, что `Init` и `Quit` должны вызываться по одному разу из одного потока. Текущая реализация является lifecycle одного запуска, а не process-wide lifecycle. ([GitHub][1])

Нужен владелец уровня процесса или приложения:

```text
process/application startup -> ShaderCross.Init()
несколько RuntimeHost sessions
process/application shutdown -> ShaderCross.Quit()
```

## Остальные дефекты

**Resize diagnostics пока семантически недостоверны.** В fallback счётчик `PresentationBackendReconfigurations` увеличивается при обнаружении нового размера окна, хотя никакой операции перенастройки backend не выполняется. В GPU-пути `backend.Resize()` только обновляет внутренние метаданные окна. При этом `PresentationResourcesRecreated` фактически содержит число пересозданий screenshot texture/buffer, хотя публичное описание называет его общим счётчиком presentation resources.

**Неизвестная render-команда всё ещё превращается в прямоугольник.** В обоих presenter-ах default-ветка рисует `Rect`, хотя критерий задачи требует пропускать invariant/programming errors наружу.

**Fallback игнорирует толщину линии.** GPU строит геометрию с учётом `command.Width`, а fallback вызывает `SDL.RenderLine`. Текущий visual fixture линию не проверяет.

## Что исправлено качественно

Можно сохранить выполненными:

* настоящий SDL GPU pipeline/render pass/draw path;
* проверку, что visual fixture действительно использовал `SDL_GPU` без fallback;
* PNG из активного presenter-а;
* одноразовый PNG readback в интерактивном режиме;
* отдельный `SDL_Renderer` fallback;
* typed fallback boundary;
* явную ошибку для неподдерживаемой текстуры;
* общий texture resolver и cache invalidation;
* очистку transfer buffers;
* bounded lifecycle diagnostics;
* корректный MSL `code_size` в байтах;
* разделение planned batches и actual draw calls.

## Изменения в `TASKS.md`

В [TASKS.md](../../../TASKS.md) следует снять `[x]` минимум с критериев:

* red tests на per-frame allocations;
* truthful resize/resource diagnostics;
* отсутствие managed allocations в presenter steady state;
* проверка `120 + 600` реальных кадров;
* прохождение invariant errors наружу;
* корректный lifecycle `SDL_shadercross`;
* подзадача об исправлении allocation diagnostics и shadercross lifetime.

Формулировку shadercross нужно заменить на однозначную:

> `SDL_shadercross` инициализируется один раз на процесс на потоке-владельце рендера и завершается один раз при shutdown процесса или приложения на том же потоке. Последовательные RuntimeHost sessions не повторяют Init/Quit.

Для allocations нужен тест реального presenter-а с непустым кадром и постоянной текстовой командой после прогрева, а не fake presenter с пустым планом.

## Verdict-файлы

Оба документа содержат неработающие относительные ссылки вида `src/...`, `tests/...` и `TASKS.md`. Из каталога `docs/verdicts/runtime/` они ведут в несуществующие вложенные пути. Это не renderer-блокер, но противоречит заявленной проверке verdict links.

## Проверки архива

Архив целостный и содержит 28 файлов. Заявленные `41/41` внутренне правдоподобны по количеству включённых тестов, но независимо повторить сборку нельзя: в архиве нет solution/project-файлов, а доступное окружение не содержит `dotnet`. Статический дефект `GetGlyph()` достаточен для отклонения независимо от результата текущего набора тестов.

**Issue в SDL3-CS не требуется:** оставшиеся проблемы находятся в Electron2D — allocations, lifecycle, diagnostics и fallback semantics.

[1]: https://github.com/edwardgushchin/SDL3-CS/wiki/API-ShaderCross "API ShaderCross · edwardgushchin/SDL3-CS Wiki · GitHub"
