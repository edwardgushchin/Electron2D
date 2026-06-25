# Вердикт T-0219: ShaderCross shutdown и изоляция тестов

- Задача: `T-0219`.
- Домен: `runtime`.
- Актуально на: `2026-06-25T19:10:00+03:00`.
- Область проверки: терминальное состояние `SDL_shadercross` при shutdown до первого `Acquire()` и отсутствие влияния тестов на process-wide состояние `RuntimeShaderCrossService`.
- Статус вывода: `T-0219` не принимать; оставить `in progress` до закрытия двух точечных блокеров ниже.
- Предыдущий аудит: `docs/verdicts/runtime/t-0219-runtime-presenter-terminal-fault-injection-audit-2026-06-25.md`.
- Следующий аудит: `docs/verdicts/runtime/t-0219-runtime-presenter-shadercross-test-scope-audit-2026-06-25.md`.

## Исходный вердикт

# Вердикт: **точечная доработка**

Архитектурную часть T‑0219 принимаю. Предыдущие блокеры по command-buffer lifecycle, transactional texture cache и fault injection исправлены. Закрывать задачу пока нельзя из-за двух связанных дефектов lifecycle `SDL_shadercross` и изоляции тестов.

## 1. Shutdown до первой инициализации не переводит сервис в терминальное состояние

В `RuntimeShaderCrossService.ShutdownOnRenderThread()` при `initialized == false` выполняется ранний `return`. `shutdownCompleted` при этом не устанавливается.

Поэтому допустима последовательность:

```text
ShutdownOnRenderThread()
Acquire()
Init()
```

Это противоречит критерию T‑0219, согласно которому после application shutdown последующий `Acquire()` должен отклоняться.

Требуемый автомат состояний:

```text
Uninitialized ──Acquire──> Initialized ──Shutdown──> Shutdown
Uninitialized ─────────────Shutdown───────────────> Shutdown
Shutdown ──Acquire──> exception
```

Нужно при shutdown неинициализированного сервиса также устанавливать терминальное состояние:

```csharp
if (!initialized)
{
    shutdownCompleted = true;
    return;
}
```

И добавить отдельный тест:

```text
Shutdown before first Acquire
→ Acquire rejected
→ InitCalls == 0
→ QuitCalls == 0
→ repeated Shutdown is idempotent
```

## 2. `RuntimeHostTests` оставляет глобальный ShaderCross в завершённом состоянии

Тест `RuntimeGpuPresenterRollsBackStagedTextureUploadWhenFrameFailsBeforeSubmit` в `finally` вызывает:

```csharp
RuntimeApplicationServices.ShutdownOnRenderThread();
```

Этот вызов терминально меняет статическое process-wide состояние `RuntimeShaderCrossService`.

После него другие тесты, создающие настоящий GPU presenter, уже не могут получить ShaderCross lease:

```text
Acquire()
→ GpuPresenterUnavailableException
→ RuntimeFramePresenter переключается на SDL_Renderer fallback
```

При этом в том же классе есть тесты, которые требуют:

```text
PresenterKind == SDL_GPU
FallbackUsed == false
```

Следовательно, результат набора зависит от порядка тестов. xUnit не гарантирует пользовательский порядок выполнения; стандартный порядок может рандомизироваться, поэтому тесты не должны обмениваться необратимым process-global состоянием. ([learn.microsoft.com][1])

Оптимальное исправление:

* убрать application shutdown из индивидуального GPU-presenter теста;
* выполнять реальный process shutdown один раз в collection/assembly fixture после завершения всех GPU-тестов;
* проверки терминального состояния выполнять через `UseApiForTests`, который изолирует и восстанавливает статическое состояние;
* добавить проверку запуска focused suite в изменённом или рандомизированном порядке.

Пока это не исправлено, заявленные `59/59` нельзя считать устойчивым acceptance evidence: конкретный локальный порядок мог пройти, другой порядок способен сломать GPU-тесты.

## Что действительно исправлено

Предыдущие замечания закрыты качественно:

* `RuntimeShaderCrossService` теперь имеет терминальное состояние и запрещает повторный `Init()` после обычного `Acquire → Shutdown`;
* CLI и Editor стали явными владельцами process shutdown;
* fault-injection тесты теперь проходят через настоящий `RuntimeGpuFramePresenter.Present()`, а не проверяют отдельно вызванный helper;
* покрыты failures на swapchain acquire, presentation resources, copy pass, render pass, submit, fence submit и fence wait;
* до получения swapchain texture используется cancel, после получения — submit, после начала submit не выполняется второй terminal operation;
* это соответствует ограничениям SDL: command buffer нельзя отменять после получения swapchain texture, а submit инвалидирует command buffer независимо от результата. ([wiki.libsdl.org][2])
* texture cache разделён на staged и committed состояния;
* `TextureUploads` увеличивается только после успешного commit;
* при submit failure staged texture и transfer buffer освобождаются ровно один раз;
* lifecycle diagnostics ограничены по размеру;
* capture allocation metric переименована и имеет честную границу измерения;
* polygon rendering использует vertex colors;
* unsupported commands и textures не маскируются fallback-рендерингом;
* allocation tests имеют warm-up и длительный измеряемый участок;
* forced GPU fixture явно запрещает fallback.

Реализация в целом соответствует проектным требованиям: рендеринг идёт через `RenderQueue`, SDL остаётся внутренней деталью Runtime, а steady-state frame path ориентирован на переиспользование ресурсов и отсутствие GC allocations. 

## Качество постановки T‑0219

Постановка стала значительно лучше. Теперь корректно зафиксированы:

* допустимые terminal paths GPU command buffer;
* process-wide ownership ShaderCross;
* staged/committed модель texture cache;
* семантика diagnostic counters;
* exactly-once освобождение native handles;
* фактическая граница capture allocations;
* различие SDL GPU primary и SDL Renderer fallback.

Остались четыре проблемы формулировки.

### Критерий ShaderCross должен описывать полный автомат

Нужно явно добавить:

```text
Shutdown до первого Acquire также является терминальным;
любой последующий Acquire отклоняется без вызова Init.
```

### Нужен критерий изоляции тестов

Например:

```text
Focused tests не оставляют process-global SDL/ShaderCross state,
влияющее на последующие тесты, и проходят независимо от порядка выполнения.
```

### `12/12 или другое фактическое количество` — непроверяемый критерий

Количество тестов не должно быть плавающей частью acceptance criteria. Лучше:

```text
Указанная focused-команда завершается с 0 failed и 0 errors;
фактическое количество тестов фиксируется в evidence.
```

### «Сначала падающие тесты» нельзя доказать итоговым архивом

Такой критерий требует одного из следующих доказательств:

* ссылки на red и green commits;
* сохранённого CI evidence;
* отдельного red/green отчёта.

Иначе временную последовательность разработки следует убрать из acceptance criteria.

## Какие отметки вернуть в `[ ]`

Минимально:

* критерий process-wide lifecycle ShaderCross;
* критерий focused test run;
* подзадачу ShaderCross lifecycle;
* подзадачу финального запуска focused checks.

Саму T‑0219 оставить:

```text
Состояние: in progress
```

Остальные архитектурные и GPU-resource критерии повторно открывать не требуется.

Независимо выполнить заявленный прогон по архиву невозможно: архив содержит только изменённые файлы без полного solution/project graph, а доступное окружение не содержит `dotnet`. Это не является основанием вердикта — два описанных дефекта непосредственно следуют из реализации.

**Итог: доработка.** Требуется небольшой изолированный патч: терминализировать shutdown до первого `Acquire` и устранить зависимость тестов от глобального ShaderCross state. После этого T‑0219 можно принимать без очередного архитектурного пересмотра.

[1]: https://learn.microsoft.com/en-us/dotnet/core/testing/order-unit-tests?utm_source=chatgpt.com "Order unit tests - .NET"
[2]: https://wiki.libsdl.org/SDL3/SDL_CancelGPUCommandBuffer?utm_source=chatgpt.com "SDL3/SDL_CancelGPUCommandBuffer"
