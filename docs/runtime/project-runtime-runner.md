# Project runtime runner

Обновлено: 2026-06-24.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для исправления playable reference games `0.1.0 Preview`.
Обновлено: 2026-06-24.
Связанные документы: [Platformer](../examples/platformer.md), [Headless runtime automation](headless-runtime-automation.md), [Runtime debug bridge и scene inspection](runtime-debug-bridge.md).

## Назначение

Electron2D должен иметь минимальный project runtime runner — инфраструктурный механизм запуска проекта, который не становится отдельным публичным API-типом и не вызывается из пользовательского кода. Пользовательский игровой код создаёт `Node`, `Node2D`, UI controls и resources, описывает поведение через callbacks (`_Ready`, `_Process`, `_PhysicsProcess`, `_Input`, `_Draw`) и остаётся внутри существующего профиля Electron2D, совместимого с выбранным API-подмножеством Godot. Окно, event loop, перевод платформенного ввода в `InputEvent`, продвижение кадров, построение canvas draw commands, показ кадра и screenshot выполняет runner проекта: CLI или Editor.

Reference games не должны иметь собственный оконный loop, `Program.cs`, прямые вызовы backend/window/input библиотек, `Console.ReadKey` или ASCII-псевдоинтерфейс. Игровые скрипты должны быть обычными `Node`/`Control`-скриптами на API Electron2D. Runner движка не должен попадать в API manifest, GitHub Wiki или compatibility таблицы как публичный тип и не должен быть доступен проектам reference games через `InternalsVisibleTo`.

## Public API boundary

Запрещено добавлять эти публичные API-типы:

- `Electron2DApplication`;
- `Electron2DRunOptions`;
- `Electron2DRunResult`;
- любой другой отдельный public static application/bootstrap class, отсутствующий в выбранном API-подмножестве Godot.

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
- `FrameLimit > 0` выполняет bounded smoke loop, то есть короткий автоматический прогон для тестов и verifier-ов. `FrameLimit == 0` оставляет окно работать до закрытия пользователем или до Escape, если `QuitOnEscape == true`.
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

## Проверки

Focused проверка runtime host, canvas batching и lifecycle:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~RuntimeHostTests|FullyQualifiedName~CanvasItemRenderQueueTests|FullyQualifiedName~SdlGpuLifecycleTests|FullyQualifiedName~SdlGpuStartupPolicyTests"
```

Последняя проверка `2026-06-25` прошла: 63/63.
