# Аудит T-0219: terminal lifecycle и fault injection runtime presenter

- Задача: `T-0219`.
- Домен: `runtime`.
- Актуально на: `2026-06-25T18:35:00+03:00`.
- Область проверки: терминальное состояние `SDL_shadercross`, реальные error phases `RuntimeGpuFramePresenter.Present()`, освобождение native handles при submit failure и семантика счётчика `TextureUploads`.
- Статус вывода: `T-0219` не принимать; оставить `in progress` до закрытия блокеров ниже.
- Следующий аудит: `docs/verdicts/runtime/t-0219-runtime-presenter-shadercross-isolation-audit-2026-06-25.md`.

# Вердикт: **T‑0219 вернуть на точечную доработку**

Основная архитектура теперь выглядит приемлемо, и почти все прошлые дефекты устранены. Переписывать renderer не требуется. Остались два блокера в lifecycle/error-path и один дефект диагностики.

## Блокеры

### 1. `SDL_shadercross` всё ещё может инициализироваться повторно в одном процессе

В `RuntimeShaderCrossService.cs`:

* `ShutdownOnRenderThread()` вызывает `Quit()` и устанавливает `initialized = false` — строки `75–97`;
* следующий `Acquire()` снова вызовет `Init()` — строки `37–60`;
* терминального состояния наподобие `ShutdownCompleted` нет;
* теста `Acquire after application shutdown -> rejected` нет.

Следовательно, сервис обеспечивает один `Init` только **между двумя shutdown-вызовами**, но не один `Init` на процесс, как требует `TASKS.md:1971`.

Upstream-контракт отдельно говорит, что `SDL_ShaderCross_Init()` и `SDL_ShaderCross_Quit()` следует вызывать только один раз из одного потока. ([GitHub][1])

Нужно:

```text
Uninitialized -> Initialized -> Shutdown
```

После `Shutdown` production-вызов `Acquire()` должен отклоняться. Сброс состояния допустим только через изолированный test hook.

Обязательный тест:

```text
Acquire -> Release -> Shutdown -> Acquire
```

Последний `Acquire` должен выбрасывать исключение, а `InitCalls` оставаться равным `1`.

### 2. Новые GPU failure tests не проверяют реальные failure phases

`RuntimeGpuCommandBufferFinalizerUsesValidTerminalPathForFailurePhase()` в `RuntimeHostTests.cs:797–832` выглядит как fault matrix, но фактически:

```csharp
_ = failurePhase;
```

Параметры `pipeline`, `upload`, `copy-pass`, `render-pass`, `submit`, `fence-submit`, `fence-wait` нигде не влияют на исполнение. Тест проверяет только комбинацию двух заранее переданных `bool`:

* `swapchainTextureAcquired`;
* `terminalPathStarted`.

Он не вызывает `RuntimeGpuFramePresenter.Present()` и не доказывает, что эти флаги правильно устанавливаются в каждой реальной фазе.

Это существенно: SDL запрещает отменять command buffer после получения swapchain texture, а после вызова submit command buffer уже нельзя использовать повторно. ([wiki.libsdl.org][2])

Сама основная ветка `Present()` теперь структурно выглядит правильно:

* до acquire — cancel;
* после acquire — submit;
* после начатого submit — никаких повторных terminal calls.

Но критерии `TASKS.md:1970` и `1972` требуют доказать это на реальных error paths, а не на отдельно вызванном helper-е.

Особенно слаб rollback-тест `RuntimeHostTests.cs:440–486`:

* texture создаётся и помещается в staged cache;
* затем unknown command ломает `BuildFrameGeometry()`;
* до `UploadPendingResources()` выполнение не доходит;
* submit failure не моделируется;
* освобождение native texture/transfer buffer не проверяется;
* проверяется только нулевой размер двух словарей.

Нулевой размер словаря не доказывает отсутствие утечки native handle.

Нужен инъецируемый presenter-level GPU adapter и fault-injection tests как минимум для:

| Failure point                            | Ожидаемое завершение                                                              |
| ---------------------------------------- | --------------------------------------------------------------------------------- |
| Swapchain acquire                        | `Cancel == 1`, `Submit == 0`                                                      |
| Pipeline/resource creation после acquire | `Cancel == 0`, `Submit == 1`                                                      |
| Texture upload/copy pass                 | `Cancel == 0`, `Submit == 1`                                                      |
| Render pass                              | `Cancel == 0`, `Submit == 1`                                                      |
| Ordinary submit failure                  | ровно одна попытка submit, повторного terminal call нет                           |
| Fence submit failure                     | то же                                                                             |
| Fence wait failure                       | повторного submit/cancel нет, fence освобождён один раз                           |
| Encoded texture upload + submit failure  | staged/committed cache пуст, texture и transfer buffer освобождены ровно один раз |

## Дефект диагностики

`textureUploads` увеличивается при создании staged resource:

```text
RuntimeFramePresenter.cs:1501–1504
```

Это происходит до:

* `SDL.UploadToGPUTexture()` — строка `1226`;
* успешного command-buffer submit — строки `405–416`;
* commit staged cache — строки `1596–1609`.

При ошибке между staging и submit ресурс откатывается, но счётчик не откатывается. Следующий успешный кадр сообщит лишнюю «загрузку текстуры».

То есть сейчас `TextureUploads` означает скорее `TextureUploadAttempts`, тогда как документация и `RuntimeHostResult` называют его фактическими загрузками.

Нужно либо:

* увеличивать `TextureUploads` при успешном commit;
* либо завести отдельные `TextureUploadAttempts` и `TextureUploadsCommitted`.

Тот же принцип желательно применить к `TextureCacheHits`, если диагностика должна учитывать только успешно отправленные кадры.

## Что теперь сделано хорошо

Предыдущие крупные дефекты действительно устранены:

* `e2d` и `Electron2D.Editor` стали явными application owners для ShaderCross shutdown;
* `Quit()` больше не вызывается после каждой `RuntimeHost` session;
* основной error path больше не вызывает `SDL_CancelGPUCommandBuffer()` после swapchain acquisition;
* texture cache разделён на staged и committed состояния;
* lifecycle diagnostics ограничены `MaxLifecycleEventCount = 128`, есть `DroppedEventCount`;
* capture-метрика переименована в `CapturePresenterManagedBytesAllocated` и теперь документирована честно;
* polygon использует per-vertex colors в обоих presenter-ах;
* unsupported texture и unknown command не маскируются GPU fallback-ом;
* реальные GPU и SDL Renderer allocation tests требуют 120 warm-up и 600 измеряемых кадров;
* визуальный GPU fixture явно запрещает fallback.

Архитектурное направление соответствует целям Electron2D: единый RenderQueue-путь, отсутствие SDL-типов в пользовательском API и запрет покадровых выделений памяти. 

## Оценка постановки T‑0219

Постановка стала существенно точнее:

* явно описаны terminal paths command buffer;
* названы владельцы ShaderCross shutdown;
* зафиксирована transactional staged/committed модель;
* определена семантика capture allocation metric;
* полная совместимость `Texture2D`/`AtlasTexture` корректно оставлена за другими задачами.

Но остаются проблемы формулировки:

1. `TASKS.md:1974` — «12/12 или другое фактическое количество» не является проверяемым критерием. Следует писать конкретную команду и требовать `0 failed`.
2. `TASKS.md:1953–1954` — порядок «сначала документы», «сначала падающие tests» нельзя доказать финальным snapshot. Нужны commit references либо сохранённый red/green evidence.
3. `TASKS.md:1970` должен прямо требовать fault injection через production sequencing, иначе тест с неиспользуемыми названиями фаз формально выглядит подходящим.
4. `TASKS.md:1972` должен требовать native release counts и exactly-once semantics.
5. `TASKS.md:1965` должен определить, что именно означает `TextureUploads`: attempt, encoded upload, submitted upload или committed upload.
6. T‑0219 остаётся чрезмерно широкой P0-задачей: renderer architecture, GPU lifecycle, ShaderCross process lifecycle, cache transaction, diagnostics, allocations и visual equivalence сведены в один acceptance unit.

## Какие отметки снять

Минимально вернуть в `[ ]`:

* `1958` — submit-failure cache behaviour не доказано;
* `1965` — `TextureUploads` недостоверен после rollback;
* `1970` — реальные failure phases не покрыты;
* `1971` — после shutdown возможен повторный `Init`;
* `1972` — native transfer/texture releases при submit failure не проверены;
* `1983` — диагностика ещё требует исправления;
* `1985` — lifecycle и error-path закрыты не полностью.

Остальные исправления можно сохранить.

Заявленные `58/58` я независимо не воспроизвёл: в архиве только изменённые файлы, без `.sln`/`.csproj`, а в доступном окружении нет `dotnet`. Это не является причиной отказа — перечисленных статических и тестовых дефектов достаточно.

**Итог: доработка.** Остался небольшой изолированный патч: терминальное состояние ShaderCross, настоящий fault-injection seam и транзакционные diagnostic counters.

[1]: https://raw.githubusercontent.com/libsdl-org/SDL_shadercross/main/include/SDL3_shadercross/SDL_shadercross.h "raw.githubusercontent.com"
[2]: https://wiki.libsdl.org/SDL3/SDL_CancelGPUCommandBuffer?utm_source=chatgpt.com "SDL3/SDL_CancelGPUCommandBuffer"
