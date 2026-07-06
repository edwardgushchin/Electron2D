# Project runtime runner

Обновлено: 2026-06-25.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для исправления playable reference games `0.1-preview`.
Обновлено: 2026-06-25.
Связанные документы: [Platformer](../examples/platformer.md), [Headless runtime automation](headless-runtime-automation.md), [Runtime debug bridge и scene inspection](runtime-debug-bridge.md).

## Назначение

Electron2D должен иметь минимальный project runtime runner — инфраструктурный механизм запуска проекта, который не становится отдельным публичным API-типом и не вызывается из пользовательского кода. Пользовательский игровой код создаёт `Node`, `Node2D`, UI controls и resources, описывает поведение через callbacks (`_Ready`, `_Process`, `_PhysicsProcess`, `_Input`, `_Draw`) и остаётся внутри утверждённого Electron2D Godot 4.7 public API contract. Окно, event loop, перевод платформенного ввода в `InputEvent`, продвижение кадров, построение canvas draw commands, показ кадра и screenshot выполняет runner проекта: CLI или Editor.

Reference games не должны иметь собственный оконный loop, `Program.cs`, прямые вызовы backend/window/input библиотек, `Console.ReadKey` или ASCII-псевдоинтерфейс. Игровые скрипты должны быть обычными `Node`/`Control`-скриптами на API Electron2D. Runner движка не должен попадать в API manifest, GitHub Wiki или compatibility таблицы как публичный тип и не должен быть доступен проектам reference games через `InternalsVisibleTo`.

## Public API boundary

Запрещено добавлять эти публичные API-типы:

- `Electron2DApplication`;
- `Electron2DRunOptions`;
- `Electron2DRunResult`;
- любой другой отдельный public static application/bootstrap class, отсутствующий в generated Godot `4.7-stable` API packets или не утверждённый отдельной задачей совместимости.

Минимальный непубличный runner, доступный CLI, Editor и автоматическим тестам, имеет право принимать:

- main `Node` или готовый `SceneTree`;
- title и logical window size;
- `FrameLimit`, где `0` означает обычный интерактивный запуск до закрытия окна, а число больше `0` означает автоматический smoke/script run;
- `FixedDelta`;
- optional `ScreenshotPath`;
- `QuitOnEscape`;
- clear color.

Runner возвращает machine-readable результат для CLI/tests: `Succeeded`, `WindowCreated`, `WindowShown`, `FramePresented`, `EventPumpObserved`, `InputEventsDispatched`, `FrameCount`, `DrawCommands`, window/pixel size, video driver, screenshot path/status и diagnostic message.

Правила:

- runner может быть internal API движка, доступным тестам, CLI и Editor через контролируемую сборочную границу;
- runner не должен расширять public API manifest и не должен требовать XML documentation как публичный API;
- публичные игровые скрипты используют только существующие `SceneTree`, `Node`, `Node2D`, `CanvasItem`, `Control`, resource, rendering и input API;
- невалидные параметры fail closed через исключение до открытия окна.

## Project launch contract

`e2d run --project <project-root>` является пользовательской точкой запуска проекта в Preview. Команда должна:

- прочитать `project.e2d.json` и применить project settings, включая Input Map и display settings;
- найти `mainScene`, прочитать root `script` из scene JSON и загрузить C# assembly проекта;
- создать root node из пользовательского script class через обычную сценовую модель проекта;
- передать `ProjectRoot` в script, если script предоставляет такое свойство;
- открыть runtime window через непубличный runner движка;
- для автоматических проверок принять `--play-script <commands>` и `--screenshot <path>`, вызвать script-level playable entrypoint только как test/acceptance hook и вернуть machine-readable результат.

Reference games должны собираться как project assemblies и запускаться через `e2d run --project ...`, а не через `dotnet run` пользовательского проекта.

## Runtime loop

Loop должен:

- создать пользовательское окно с указанным title и размером;
- установить `SceneTree.Root` как `Viewport.Size = RuntimeHostOptions.WindowSize` до `_Ready()` при запуске main `Node` и перед первым кадром при запуске готового `SceneTree`;
- переводить platform input events в публичные `InputEvent*` и отправлять их в `SceneTree`;
- вызывать physics/process/draw callbacks через существующий `SceneTree`;
- строить draw commands через canvas submission;
- показывать последний frame в окне;
- сохранять screenshot, если задан `ScreenshotPath`;
- возвращать machine-readable результат для verifier/tests.

Внутренний механизм отрисовки не раскрывается наружу. Пользовательский код не получает нативные дескрипторы и не зависит от платформенных пакетов напрямую.

## Планировщик кадров

Обычный интерактивный запуск (`FrameLimit == 0`) использует планировщик кадров в `RuntimeHost`. Планировщик берёт время из монотонных часов, то есть источника времени, который не откатывается при изменении системных часов, и считает:

```text
measuredDeltaTime = now - previous
deltaTime = Min(measuredDeltaTime, MaxDeltaTime)
```

`MaxDeltaTime` для `0.1-preview` равен `0.25` секунды. Это ограничение не ускоряет игру после долгой остановки процесса, а защищает физику и пользовательский `_Process(double delta)` от патологически большого скачка. Значение `FixedDelta` по умолчанию остаётся `1/60` секунды для ограниченного автоматического прогона, но интерактивная физика берёт реальный шаг из `SceneTree.FixedPhysicsStep`.

`RuntimeHost` имеет собственный накопитель времени для догоняющих шагов физики. На каждом отображаемом кадре он добавляет `deltaTime` в накопитель и выполняет:

```text
while accumulator >= SceneTree.FixedPhysicsStep and physicsSteps < MaxPhysicsStepsPerFrame:
    SceneTree.PhysicsFixedStep()
    accumulator -= SceneTree.FixedPhysicsStep
```

`MaxPhysicsStepsPerFrame` равен `5`. Если после пяти реальных шагов накопитель всё ещё содержит `SceneTree.FixedPhysicsStep` или больше, остаток отбрасывается, а диагностика считает отброшенное или ограниченное время (`dropped/clamped time`). Такой сброс предотвращает бесконечное догоняющее выполнение физики после просадки кадра. `_PhysicsProcess(double delta)` получает только `SceneTree.FixedPhysicsStep`; `_Process(double delta)` получает `deltaTime` отображаемого кадра, а не фиксированное значение без связи с реальным временем. `RuntimeHostOptions.FixedDelta`, отличный от `1/60`, больше не создаёт второй независимый источник физического шага в интерактивном планировщике. Внутренний метод `SceneTree.PhysicsFixedStep()` выполняет один реальный физический шаг без добавления времени во внутренний накопитель `SceneTree.PhysicsFrame(double delta)`, поэтому счётчик `SchedulerPhysicsSteps` совпадает с фактическими вызовами `_PhysicsProcess`.

Бюджет кадра начинается до чтения платформенного ввода. После ввода, физики, пользовательского обновления, построения плана отрисовки (`render-plan`), отправки команд и показа кадра планировщик ждёт только остаток до целевого срока следующего кадра. Безусловного ожидания после всей работы быть не должно. Активный объект показа кадра получает `RuntimePresentationSettings`: требуемую синхронизацию показа и выбранную целевую частоту. Он сообщает фактически наблюдаемую синхронизацию через внутренний статус. Программный ограничитель частоты кадров включается только когда фактическая синхронизация показа отсутствует; если модуль показа действительно ждёт синхронизацию, runtime не добавляет второе программное ожидание.

Целевая частота кадра по умолчанию равна `60 Hz`. Планировщик корректно строит целевой срок кадра (`deadline`) для `60`, `120`, `144` и `165 Hz`; если вызывающий код передаёт другое значение, runtime выбирает ближайшее поддержанное значение и записывает выбранное значение в диагностику вместо молчаливого изменения скорости игры. Когда объект показа кадра сообщает фактическую синхронизацию с экраном, эта синхронизация задаёт ритм показа, а `TargetFrameRate` остаётся диагностическим выбором и целью для программного ограничителя только на несинхронизированном пути.

Когда окно свёрнуто или потеряло фокус и политика паузы считает его временно приостановленным, планировщик не должен крутить пустой цикл на полной скорости и не должен накапливать время паузы в physics-накопителе. В такой паузе он выполняет короткое ожидание через внедрённый sleeper, сбрасывает предыдущую отметку времени на момент возобновления и продолжает без догоняющего скачка.

Ограниченный автоматический прогон (`bounded smoke`, `FrameLimit > 0`) остаётся детерминированным по умолчанию: он продвигает кадры фиксированным `FixedDelta`, не зависит от системного времени и не выполняет реального ожидания. Это сохраняет воспроизводимость проверяющих скриптов (`verifier`) и screenshot-тестов. Интерактивный планировщик, clock и sleeper внедряются отдельно, чтобы его можно было проверять без реального ожидания и без зависимости от скорости машины.

`RuntimeHostResult` должен сохранять диагностику по времени кадра как отдельные группы: `input`, `physics`, `process`, `render-plan`, `submit` и `present`. `submit` означает работу по отправке кадра в объект показа, `present` означает ожидание или работу внутри самого показа кадра. Запрошенное программное ожидание до срока кадра, фактически наблюдённое программное ожидание и ожидание во время паузы окна считаются отдельными полями `SchedulerRequestedWaitTimeSeconds`, `SchedulerObservedWaitTimeSeconds` и `SchedulerPauseWaitTimeSeconds`. Эти значения нужны `T-0221`, чтобы строить артефакт производительности, а не смешивать пользовательское обновление, планирование отрисовки, отправку кадра и ожидание в один счётчик.

## Интерактивная подача кадра

Обычный интерактивный запуск (`FrameLimit == 0`) должен использовать общий путь отрисовки runtime: `SceneTree` строит canvas-команды через `CanvasSubmissionContext`, затем `RenderingServer`/внутренний модуль отрисовки превращает их в план кадра, а долгоживущий объект показа кадра (`presenter`) повторно использует ресурсы окна между кадрами. Такой объект создаётся один раз на окно, хранит связанные с окном ресурсы и обновляет их только при изменении размера или при изменении исходной текстуры.

Основной `presenter` использует SDL GPU: создаёт ресурсы окна, получает текстуру цепочки представления (`swapchain texture`), открывает проход отрисовки (`render pass`), привязывает графические конвейеры (`graphics pipelines`) и отправляет вызовы draw по пакетам из `CanvasItemRenderPlan`. Если SDL GPU недоступен на платформе или драйвер отклоняет создание GPU-ресурсов, runtime должен перейти на запасной `SDL_Renderer` `presenter`. Запасной путь не является основным и не должен маскировать рабочий SDL GPU, но он обязан сохранить интерактивный запуск на платформах без рабочего SDL GPU. Он также живёт между кадрами, использует тот же `CanvasItemRenderPlan`, кеширует текстуры и не возвращается к покадровой загрузке во временную поверхность окна.

Интерактивный путь не должен на каждом кадре создавать новый `RuntimePixelCanvas`, новый управляемый RGBA-буфер кадра (`framebuffer`), закреплять управляемый массив через `GCHandle.Alloc(...)`, создавать временный ресурс поверхности окна для показа кадра и уничтожать его сразу после показа. Если запрошен PNG-снимок, файл должен создаваться из текущего пути показа кадра: основной SDL GPU `presenter` читает GPU-текстуру через буфер чтения после отрисовки того же `CanvasItemRenderPlan`, а запасной `SDL_Renderer` `presenter` читает текущую цель отрисовки через `RenderReadPixels`. Запасной `SDL_Renderer` `presenter` остаётся интерактивным объектом показа кадра, а не программным запасным `framebuffer`. `RuntimePreviewFrameRasterizer` не должен подменять активный `presenter` при обычном сохранении PNG.

`RuntimeHostResult` должен сообщать не только количество draw commands, но и признаки пути кадра: источник отрисовки, выбранный модуль показа кадра, факт перехода на запасной путь, причину перехода, количество плановых пакетов, фактических draw-вызовов, переключений текстуры и pipeline state, успешно зафиксированных загрузок и повторных использований текстурного кеша, созданных ресурсов показа кадра, пересозданий принадлежащих Electron2D ресурсов, наблюдённых изменений размера окна, фактических перенастроек backend/swapchain и фактическую разницу `GC.GetAllocatedBytesForCurrentThread()` после прогрева на границе `presenter`. `TextureUploads` увеличивается только после успешной отправки кадра и переноса staged texture в committed cache; `TextureCacheHits` фиксирует попадания в кеш только для успешно отправленных кадров. Для обычного запуска без PNG эта оценка должна оставаться нулевой внутри `presenter`, но она не доказывает отсутствие выделений памяти при построении `CanvasItemRenderPlan`; полный бюджет кадра проверяется отдельным performance gate. При запрошенном PNG отдельная метрика `CapturePresenterManagedBytesAllocated` отражает весь capture-вызов активного `presenter`, включая readback buffer, но не включает кодирование PNG и запись файла. Эти значения нужны последующим проверкам производительности, чтобы отличать устойчивое состояние от скрытого покадрового пути через `framebuffer`.

Переход на запасной `SDL_Renderer` `presenter` допустим только для документированных recoverable GPU failures: отказ создания GPU device, claim окна, параметров swapchain, pipeline/resources или документированная device-loss ошибка. Ошибки данных, нарушенные внутренние invariants, `ArgumentException`, ошибки ресурса и другие programming errors должны проходить наружу, чтобы не скрывать дефект основного renderer-а. Проверки отказов должны проходить через реальный `RuntimeGpuFramePresenter.Present()`: до получения swapchain texture command buffer отменяется, после получения swapchain texture выполняется единственный допустимый terminal path через отправку command buffer или fence flow без повторной отмены.

`SDL_shadercross` инициализируется через единый runtime-сервис на потоке, владеющем отрисовкой. Жизненный цикл одного `presenter` и одного запуска `RuntimeHost` не должен напрямую вызывать `ShaderCross.Init()`/`ShaderCross.Quit()`: несколько последовательных runtime sessions в одном процессе используют тот же initialized service. Завершение работы компилятора выполняет владелец процесса или приложения явным shutdown-вызовом на том же потоке. После такого shutdown сервис переходит в terminal state: production `Acquire()` отклоняется и не выполняет повторный `Init()` в уже завершающемся процессе. Shutdown до первого `Acquire()` также является terminal state и не вызывает ни `Init()`, ни `Quit()`. В текущей реализации владельцами shutdown являются application roots `e2d` и `Electron2D.Editor`, которые вызывают shutdown в `finally` перед выходом из процесса; shutdown с другого потока или при активной lease отклоняется. Тесты терминального состояния используют изолированный test hook `UseApiForTests`: он временно направляет `RuntimeShaderCrossService` на отдельный `RuntimeShaderCrossLifetime` с fake API, не вызывает `Quit()` у прежнего lifetime, при выходе завершает только временный lifetime и возвращает прежний объект вместе с его уже существующим initialized или terminal state. Вложенные test scopes должны закрываться в обратном порядке, а активная lease запрещает замену lifetime.

## Acceptance criteria

- `Electron2DApplication`, `Electron2DRunOptions` и `Electron2DRunResult` отсутствуют среди exported public types runtime assembly.
- Project runtime runner реализован в `src/Electron2D/`, а project launch command реализован в `src/Electron2D.Cli/`; ни один из них не является helper внутри `examples/`.
- Focused test создаёт `Node2D`, рисует через `_Draw`, запускает internal project runtime runner, получает visible/window markers и PNG screenshot.
- Focused test подтверждает, что root `Viewport.Size` равен logical window size уже в `_Ready()` и `_Process()`.
- Platformer запускается командой `e2d run --project ...` в режиме разработки или через export player, не содержит `Program.cs`, не вызывает `RuntimeHost`, `ProjectRuntimeRunner` или другой engine-owned bootstrap из user code.
- Reference games не содержат `Console.ReadKey`, ASCII frame output, прямые вызовы `SDL`/window backend или custom event loop.
- `e2d run --project examples/platformer --play-script ... --screenshot <path>` возвращает `WindowCreated=True`, `WindowShown=True`, `FramePresented=True`, `DrawCommands > 0`, `ScreenshotPath=<path>` и создаёт PNG нормального размера.
- Implementation documentation в `docs/runtime/` описывает фактический scope и ограничения project runtime runner.
- API compatibility, API manifest, GitHub Wiki generation, source license/header checks и focused reference verifiers проходят, не добавляя out-of-profile public bootstrap API.

## Фактическое состояние, ограничения и проверки

`Project runtime runner` - это внутренний механизм запуска игры из CLI, Editor и export player. Он не является публичным API для пользовательского кода: игровые проекты описывают сцену, ресурсы и скрипты, а runner создаёт окно, продвигает `SceneTree`, отправляет input events, строит draw commands и сохраняет screenshot.

## Текущее поведение

- `RuntimeHost.Run(Node, RuntimeHostOptions?)` создаёт новый `SceneTree`, устанавливает размер root `Viewport` из `RuntimeHostOptions.WindowSize`, добавляет main scene и запускает loop.
- `RuntimeHost.Run(SceneTree, RuntimeHostOptions?)` синхронизирует размер root `Viewport` с `RuntimeHostOptions.WindowSize` перед первым кадром уже готового дерева.
- Размер root `Viewport` доступен уже в `_Ready()` при запуске через overload с main `Node`. Это важно для `Camera2D`, `CanvasLayer`, `Control` anchors, touch normalization через `GetVisibleRect()` и screenshot-проверок.
- `FrameLimit > 0` выполняет ограниченный автоматический прогон для тестов и проверяющих скриптов. Этот режим детерминированно использует `FixedDelta`, не читает системное время и не ждёт реальное время.
- `FrameLimit == 0` оставляет окно работать до закрытия пользователем или до Escape, если `QuitOnEscape == true`, и использует интерактивный планировщик с монотонными часами, `DeltaTime`, physics-накопителем, ограничением догоняющих physics-шагов и ожиданием только до целевого срока кадра либо через синхронизацию показа кадра.
- В интерактивном запуске бюджет кадра начинается до платформенного ввода. `SchedulerSoftwareWaits` растёт только на несинхронизированном пути показа кадра, а `SchedulerPauseWaitTimeSeconds` не попадает в `PresentTimeSeconds`.
- `ScreenshotPath`, если задан, записывает PNG финального кадра для ограниченного запуска `FrameLimit > 0`; интерактивный запуск `FrameLimit == 0` записывает первый показанный кадр один раз, чтобы не читать изображение каждый кадр до закрытия окна.

## Границы

- Runner остаётся internal и не добавляет public bootstrap types вроде `Electron2DApplication`, `Electron2DRunOptions` или `Electron2DRunResult`.
- Пользовательские scripts не должны напрямую вызывать runner, оконный backend или platform input backend.
- Runtime host сейчас использует logical window size из options как размер root viewport. Live resize разделяет наблюдённое изменение размера, фактическую перенастройку backend/swapchain и пересоздание принадлежащих Electron2D ресурсов снимка; простое наблюдение нового размера не считается перенастройкой backend. Полноценная user-facing resize policy остаётся отдельной задачей.
- PNG-снимок создаётся из активного `presenter`: через буфер чтения GPU-текстуры в SDL GPU пути или через `RenderReadPixels` в запасном `SDL_Renderer` пути.
- `RuntimePreviewFrameRasterizer` остаётся ограниченным программным инструментом для проверок, но не подменяет текущий путь показа кадра при обычном сохранении PNG.
- `SDL_Renderer` остаётся запасным интерактивным `presenter` для платформ и драйверов без рабочего SDL GPU; он не должен использовать `RuntimePixelCanvas` как промежуточный оконный `framebuffer`.
- Texture cache обязан сверять версию содержимого ресурса и повторно загружать texture resource после изменения версии. GPU upload проходит транзакционно: новый texture handle сначала живёт в staged state, используется текущим кадром и попадает в committed cache только после успешной отправки command buffer; ошибка до успешного commit освобождает staged resource, а `TextureUploads` и `TextureCacheHits` не учитывают откатившийся кадр.
- Запасной `presenter` должен либо отправлять геометрию пакетами, либо отдельно сообщать `ActualDrawCalls`, чтобы diagnostics не выдавали плановые пакеты за фактические вызовы низкоуровневой отрисовки.
- `MaxPresenterManagedBytesPerFrame` измеряет только управляемые выделения памяти внутри активного `presenter` после построения `CanvasItemRenderPlan`; полное отсутствие выделений памяти на всём пути кадра остаётся задачей performance gate `T-0221`. `CapturePresenterManagedBytesAllocated` измеряет весь capture-вызов активного `presenter` и поэтому не называется PNG-only метрикой.
- Неподдержанный тип `Texture2D` должен завершать показ кадра явной диагностируемой ошибкой, а не заменяться прямоугольником и не запускать запасной путь как будто это отказ драйвера.
- Интерактивный планировщик считает `MaxDeltaTime = 0.25` секунды и `MaxPhysicsStepsPerFrame = 5` фактическими runtime-ограничениями. Догоняющие physics-шаги считаются по реальным вызовам `_PhysicsProcess`, а не по произвольному `RuntimeHostOptions.FixedDelta`. Отброшенное из-за этих ограничений время попадает в диагностику, чтобы проверка производительности видела просадки, а не только сглаженный `deltaTime`.
- Ограниченный автоматический прогон специально не является доказательством ритма кадров: для него важнее воспроизводимость. Проверки ритма кадров используют внедрённые clock/sleeper и интерактивную ветку планировщика без настоящего ожидания.

## Проверки

Focused проверка runtime host, canvas batching и lifecycle:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~RuntimeHostTests|FullyQualifiedName~CanvasItemRenderQueueTests|FullyQualifiedName~SdlGpuLifecycleTests|FullyQualifiedName~SdlGpuStartupPolicyTests"
```

Focused проверка планировщика, runtime host и fixed physics:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~RuntimeHostTests|FullyQualifiedName~FixedPhysicsStepAndRigidBodyMotionTests"
```

Последняя проверка области runtime presenter `2026-06-25` прошла: 63/63. Проверка планировщика, runtime host и fixed physics `2026-06-25` прошла: 69/69. Новые focused проверки scheduler-а прошли: 20/20.
