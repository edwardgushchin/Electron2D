# Повторная проверка T-0219: runtime presenter

- Задача: `T-0219`.
- Домен verdicts: `runtime`.
- Актуально на: `2026-06-25T16:21:15+03:00`.
- Область проверки: повторная приёмка интерактивного runtime presenter-а, diagnostics, PNG-снимка, texture cache, resize, shadercross lifecycle и unsupported texture handling.
- Статус вывода: `T-0219` не принимать; оставить `in progress` до устранения блокеров ниже.

## Решение

**`T-0219` не принимать. Оставить `in progress` и вернуть на доработку.**

Повторная версия существенно лучше: большинство прежних архитектурных замечаний устранено. Однако несколько критериев отмечены `[x]`, хотя реализация и тесты их не подтверждают.

### Блокеры

1. **`0 B/frame` не доказано и для полного runtime frame сейчас неверно.**

В [`RuntimeFramePresenter.cs`](../../../src/Electron2D/Runtime/Application/RuntimeFramePresenter.cs) измерение начинается непосредственно перед `activePresenter.Present()`. Между тем [`RuntimeHost.cs`](../../../src/Electron2D/Runtime/Application/RuntimeHost.cs) строит `CanvasItemRenderPlan` до начала измерения.

Тест на 720 кадров в [`RuntimeHostTests.cs`](../../../tests/Electron2D.Tests.Integration/RuntimeHostTests.cs) использует `FakeRuntimeFramePresenter`, поэтому не выполняет:

* `CanvasSubmissionContext.BuildPlan()`;
* настоящий SDL GPU presenter;
* настоящий `SDL_Renderer` presenter;
* подсчёт texture/pipeline switches;
* фактическую отправку draw-команд.

Кроме того, неизменённый `CanvasItemRenderQueue.BuildPlan()` по-прежнему выполняет `Where().ToList()`, `Select().ToArray()`, создаёт новый список пакетов и новый `CanvasItemRenderPlan` на каждом кадре. `RuntimeHost` вызывает этот путь каждый кадр. ([GitHub][1])

В presenter-ах также остались `foreach` по `IReadOnlyList`:

* GPU: `renderPlan.Batches` и `renderPlan.Commands`;
* fallback: `renderPlan.Commands`, а затем повторные проходы для диагностики.

Это расходится с проектной политикой hot path.

Дополнительно:

* нет счётчика, доказывающего именно 600 измеренных кадров;
* нет отдельной метрики PNG;
* `ManagedFrameBufferBytesAllocated` хранит то максимум steady-state allocation, то allocation кадра снимка;
* XML-документация называет значение cumulative estimate и утверждает, что PNG учитывается отдельно, хотя отдельного свойства нет.

Нужно выбрать одно из двух:

* либо измерять весь runtime frame — до `BuildPlan()` и после `Present()` — и устранить его аллокации;
* либо честно ограничить критерий presenter boundary, переименовать метрику в `MaxPresenterManagedBytesPerFrame`, а полное `0 B/frame` оставить `T-0221`.

Сейчас критерии `steady state`, `600 measured frames` и `separate PNG metric` должны быть сняты с `[x]`.

2. **Сложный visual fixture не доказывает работу SDL GPU.**

`RuntimeHostVisualFixtureCoversTextureFlipTransformsCirclePolygonAndGlyphs()` запускает обычный [`RuntimeHost`](../../../src/Electron2D/Runtime/Application/RuntimeHost.cs), но не проверяет:

```text
PresentationBackend == SDL_GPU
UsedFallbackPresenter == false
```

Если GPU presenter падает с `GpuPresenterUnavailableException`, fixture прозрачно проходит через `SDL_Renderer` и всё равно становится зелёным.

Простой двухкадровый тест отдельно подтверждает SDL GPU, но не проверяет сложную геометрию: texture flip, rotation, circle, polygon и glyphs.

Нужен forced GPU fixture, который либо:

* напрямую создаёт `RuntimeGpuFramePresenter`;
* либо запускает host с принудительным backend и запрещённым fallback;
* либо как минимум проверяет backend и отсутствие fallback после сложного кадра.

Fallback fixture сейчас поставлен правильно: он напрямую создаёт `RuntimeSdlRendererFramePresenter`.

3. **Resize diagnostics fallback-пути фиктивны.**

В [`RuntimeSdlRendererFramePresenter.cs`](../../../src/Electron2D/Runtime/Application/RuntimeSdlRendererFramePresenter.cs) изменение `windowSize` лишь присваивается полю. После этого всегда возвращаются:

```text
PresentationResourcesRecreated = 0
ObservedPresentationResizes = 0
PresentationBackendReconfigurations = 0
```

Более того, `RuntimeHost` передаёт presenter-у неизменный `runOptions.WindowSize`, поэтому fallback вообще не наблюдает реальный resize SDL-окна.

Это прямо противоречит отмеченному `[x]` критерию о разделении:

* observed resize;
* engine-owned resource recreation;
* backend reconfiguration.

Нужны фактические счётчики и отдельный тест изменения размера для fallback presenter-а. Нулевое пересоздание принадлежащих Electron2D ресурсов допустимо, но `ObservedPresentationResizes` не может оставаться нулём после реально замеченного resize.

4. **Lifecycle `SDL_shadercross` не завершён.**

[`RuntimeShaderCrossService.cs`](../../../src/Electron2D/Runtime/Application/RuntimeShaderCrossService.cs) вызывает `ShaderCross.Init()` один раз, но `Release()` является пустым и `ShaderCross.Quit()` не вызывается никогда. Тест дополнительно закрепляет отсутствие `Quit()`.

Документация SDL3-CS и upstream-контракт говорят, что `Init()` и `Quit()` должны вызываться по одному разу из одного потока. ([GitHub][2])

Правильная модель:

```text
process runtime owner starts on render-owning thread
    -> ShaderCross.Init() once

all presenters use the initialized service

controlled engine/application shutdown on the owning thread
    -> ShaderCross.Quit() once
```

Привязывать `Quit()` к каждому presenter-у или `ProcessExit` действительно нельзя. Но это не означает, что `Quit()` следует запретить полностью. Текущий критерий в [`TASKS.md`](../../../TASKS.md) был ослаблен по сравнению с предыдущим вердиктом и допускает незавершённый lifecycle.

5. **Политика интерактивного PNG противоречит документации.**

`ShouldCaptureFrame()` теперь корректно устраняет readback каждого кадра, но при `FrameLimit == 0` снимает **первый кадр**:

```csharp
frameLimit == 0 ? frameIndex == 0 : frameIndex == targetFrames - 1
```

В [`project-runtime-runner.md`](../../runtime/project-runtime-runner.md) по-прежнему сказано, что `ScreenshotPath` записывает PNG последнего отрисованного кадра.

Нужно нормативно выбрать поведение:

* первый представленный кадр;
* финальный кадр при завершении;
* кадр по отдельному screenshot request.

Первый кадр допустим, но тогда документ, CLI-контракт и название теста должны это говорить. Сейчас критерий синхронизации документации выполненным считать нельзя.

6. **Неподдерживаемая texture-команда всё ещё молча превращается в прямоугольник.**

[`rendering-server.md`](../../rendering/rendering-server.md) прямо запрещает заменять неподдерживаемую команду другим примитивом.

Однако:

* GPU presenter при неудаче resolver-а вызывает `AppendSolid()`, где `Texture` превращается в `AppendRect()`;
* fallback `DrawTexture()` при неудаче resolver-а вызывает `DrawRect()`.

Это маскирует ошибку texture resource или resolver-а визуально правдоподобным прямоугольником. Должна возвращаться явная диагностируемая ошибка unsupported texture resource. Она не должна запускать GPU fallback, поскольку это ошибка ресурса, а не отказ device/driver.

## Что действительно исправлено

Эту часть реализации можно сохранить:

* настоящий SDL GPU path с graphics pipelines, render pass и draw calls;
* отдельный `SDL_Renderer` fallback;
* типизированная граница recoverable GPU failures;
* одноразовый readback интерактивного PNG вместо readback каждого кадра;
* полный transform геометрии, texture flip, круги, полигоны и glyphs;
* общий resolver для текущего `ImageTexture`/`AtlasTexture` подмножества;
* cache reload при изменении версии атласа;
* разделение planned batches и actual draw calls;
* `PipelineSwitches`;
* очистка pending/submitted transfer buffers;
* ограниченный lifecycle event log;
* MSL `code_size` в байтах.

Полная совместимость публичных `Texture2D`/`AtlasTexture` с Godot 4.7 правильно оставлена вне `T-0219`; зависимости от `T-0226`/`T-0227` здесь не нужны.

## Качество постановки задачи

Основная цель `T-0219` поставлена корректно. Проблема находится в уточнённых критериях: они одновременно требуют полного отсутствия кадровых аллокаций, но измеряют только часть presenter-вызова и проверяются fake-объектом.

В [`TASKS.md`](../../../TASKS.md) следует снять `[x]` минимум с критериев:

* resize diagnostics;
* steady-state managed allocations;
* 120 + 600 actual-frame measurement;
* отдельная PNG allocation metric;
* forced main/fallback visual verification;
* завершённый `SDL_shadercross` lifecycle;
* полная синхронизация документации.

Также стоит добавить явный критерий:

> Неудача разрешения texture resource завершается resource/unsupported diagnostic и не заменяется прямоугольником и не инициирует GPU fallback.

## Документ вердикта

Перенос в [`docs/verdicts/runtime/t-0219-runtime-presenter-audit.md`](t-0219-runtime-presenter-audit.md) по домену оформлен нормально, но внутри остались неработающие репозиторные ссылки вида:

```text
...
```

и URL с лишними query-параметрами.

Как исторический snapshot документ можно оставить, но ссылки должны стать относительными путями репозитория. Для повторной проверки лучше добавить новый датированный verdict или раздел `Повторная проверка`, не переписывая старые выводы задним числом.

## Проверки

Заявление `38/38` внутренне сходится:

* `RuntimeHostTests`: 20;
* `SdlGpuLifecycleTests`: 7;
* `SdlGpuStartupPolicyTests`: 3;
* заявленные `CanvasItemRenderQueueTests`: 8.

Но архив не содержит solution и project files, поэтому независимо воспроизвести сборку и тестовый прогон по этому вложению невозможно. Даже успешные 38/38 не снимают выявленные проблемы: часть тестов проверяет fake presenter или наличие строк в исходниках, а не заявленное production-поведение.

## SDL3-CS

**Issue в SDL3-CS создавать не нужно.** Признаков ошибки привязок нет. Оставшиеся блокеры находятся в Electron2D: граница измерения, тестовые fixtures, fallback resize, lifecycle shadercross, screenshot contract и обработка unsupported textures.

[1]: https://raw.githubusercontent.com/edwardgushchin/Electron2D/main/src/Electron2D/Graphics/Rendering/CanvasItemRenderQueue.cs "raw.githubusercontent.com"
[2]: https://github.com/edwardgushchin/SDL3-CS/wiki/API-ShaderCross "API ShaderCross · edwardgushchin/SDL3-CS Wiki · GitHub"
