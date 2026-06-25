# Вердикт T-0219: изоляция `ShaderCross` test scope

- Задача: `T-0219`.
- Домен: `runtime`.
- Актуально на: `2026-06-25T19:33:36+03:00`.
- Область проверки: сохранение предшествующего process-wide состояния `RuntimeShaderCrossService` при использовании `UseApiForTests` и запрет тестовому scope преждевременно вызывать реальный `ShaderCross.Quit()`.
- Статус вывода: `T-0219` не принимать; оставить `in progress` до исправления изоляции test hook и повторной проверки.
- Предыдущий аудит: `docs/verdicts/runtime/t-0219-runtime-presenter-shadercross-isolation-audit-2026-06-25.md`.
- Следующий аудит: отсутствует на момент `2026-06-25T19:33:36+03:00`.

## Исходный вердикт

# Вердикт: **T‑0219 вернуть на точечную доработку**

Рендеринг и GPU error-path можно считать принятыми. Остался один блокирующий дефект: `UseApiForTests` не изолирует process-wide lifecycle `SDL_shadercross`.

## Блокер: test scope разрушает предыдущее состояние ShaderCross

В `RuntimeShaderCrossService.UseApiForTests()`:

* при уже инициализированном сервисе вызывается реальный `Quit()` — `RuntimeShaderCrossService.cs:119–129`;
* сохраняется только ссылка на прежний API — строки `131–134`;
* не сохраняются `initialized`, `shutdownCompleted` и `renderThreadId`;
* при выходе из scope безусловно устанавливается `shutdownCompleted = false` — строки `163–165`.

Возможная последовательность:

```text
real GPU test
→ ShaderCross.Init() #1
→ presenter disposed, но service остаётся initialized

lifecycle test
→ UseApiForTests()
→ реальный ShaderCross.Quit() #1
→ fake lifecycle
→ scope dispose
→ production service восстановлен как uninitialized

следующий real GPU test
→ ShaderCross.Init() #2
```

Это напрямую нарушает требование `TASKS.md:1971` об одном `Init`/`Quit` на процесс. Upstream-контракт также указывает, что `SDL_ShaderCross_Init()` и `SDL_ShaderCross_Quit()` должны вызываться по одному разу из одного потока. ([GitHub][1])

Терминальное внешнее состояние также не восстанавливается: если до `UseApiForTests()` было `shutdownCompleted == true`, после scope оно станет `false`.

Тест `RuntimeShaderCrossServiceTestApiScopeRestoresTerminalState()` в `RuntimeHostTests.cs:819–837` этого не обнаруживает. Он проверяет, что terminal state, созданный внутри одного fake scope, **сбрасывается** перед следующим fake scope. Он не проверяет сохранение состояния, существовавшего до scope.

Детерминированный regression test можно построить через вложенные fake scopes:

```text
outer scope:
    Acquire outer API       → InitCalls == 1
    inner fake scope
    Acquire outer API again → должно остаться InitCalls == 1
```

Сейчас получится второй `Init` и преждевременный `Quit`.

## Как исправить

Надёжный вариант:

1. Вынести автомат lifecycle в нестатический `RuntimeShaderCrossLifetime`, принимающий `IRuntimeShaderCrossApi`.
2. Production `RuntimeShaderCrossService` должен владеть одним постоянным экземпляром с реальным SDL API.
3. Тесты должны создавать отдельные экземпляры lifecycle с fake API, не заменяя process-global production singleton.
4. Удалить либо существенно ограничить `UseApiForTests`.

Просто сохранить булевы поля и восстановить их после уже выполненного реального `Quit()` недостаточно: управляемое состояние будет говорить `initialized == true`, хотя native-компилятор уже завершён.

## Что исправлено и принимается

Последние два заявленных исправления действительно внесены:

* shutdown до первого `Acquire()` теперь терминален — `RuntimeShaderCrossService.cs:85–88`;
* real GPU rollback-test больше не выполняет application shutdown;
* CLI и Editor остаются явными владельцами финального shutdown;
* fault-injection matrix проходит через настоящий `RuntimeGpuFramePresenter.Present()`;
* покрыты swapchain acquire, resource creation, copy pass, render pass, submit, fence submit и fence wait;
* до получения swapchain texture используется cancel, после получения — submit/fence path без повторного terminal call. Это соответствует SDL: после получения swapchain texture отменять command buffer нельзя, а presentation происходит при его отправке. ([wiki.libsdl.org][2])
* staged texture cache фиксируется только после успешного submit;
* `TextureUploads` увеличивается после commit;
* texture и transfer buffer при submit failure освобождаются ровно один раз;
* diagnostics ограничены по размеру;
* presenter allocation tests имеют 120 warm-up и 600 measured frames;
* visual fixture проверяет GPU без fallback, геометрию и цвет;
* programming/resource errors не маскируются fallback-ом.

Архитектурно результат соответствует целям Electron2D: команды проходят через `RenderQueue`/render plan, SDL остаётся внутренней реализацией, а ресурсы переиспользуются без покадрового framebuffer churn. 

## Оценка постановки T‑0219

Постановка теперь в основном качественная и проверяемая. Особенно хорошо сформулированы transactional cache, command-buffer terminal paths, diagnostics, allocation boundary и fallback policy.

Нужно исправить следующее:

* `TASKS.md:1972` должен требовать сохранения **предшествующего** process state, а не просто очистки fake state после scope.
* Следует явно запретить test hooks вызывать реальный `ShaderCross.Quit()` либо разрешать повторный native `Init()`.
* `docs/runtime/project-runtime-runner.md:93` сейчас содержит неверное утверждение, что `UseApiForTests` восстанавливает process-wide состояние.
* `TASKS.md:1953` и подзадача про red tests остаются временными требованиями: итоговый архив не доказывает порядок «сначала документация/красный тест». Для них нужны ссылки на red/green commits или сохранённое evidence.

`Зависимости: нет` допустимо: application owners входят в саму T‑0219, а расширенный API `Texture2D`/`AtlasTexture` корректно остаётся вне её scope.

## Какие отметки снять

Минимально вернуть в `[ ]`:

* `1971` — test hook допускает повторный native `Init` в одном процессе;
* `1972` — scope не восстанавливает предшествующее process-wide состояние;
* `1976` — runtime-документ описывает несуществующую изоляцию;
* `1986` — ShaderCross lifecycle закрыт не полностью;
* `1987` — после исправления требуется повторный focused прогон и обновление документов.

T‑0219 оставить в состоянии `in progress`. Остальные GPU/rendering критерии повторно открывать не требуется.

Заявленные `61/61` независимо не воспроизведены: архив содержит только изменённые файлы без `.sln`/`.csproj`, а `dotnet` в доступном окружении отсутствует. Это не причина отказа; блокер непосредственно следует из реализации.

**Итог: доработка.** Требуется один изолированный lifecycle-патч без пересмотра renderer architecture.

[1]: https://raw.githubusercontent.com/libsdl-org/SDL_shadercross/main/include/SDL3_shadercross/SDL_shadercross.h "raw.githubusercontent.com"
[2]: https://wiki.libsdl.org/SDL3/SDL_CancelGPUCommandBuffer "SDL3/SDL_CancelGPUCommandBuffer - SDL Wiki"
