# Внешний аудит T-0220 r01

- Задача: T-0220
- Домен: runtime
- Актуально на: 2026-06-25T21:20:30+03:00
- Область проверки: архив `T-0220-audit-r01.zip`, задача T-0220, runtime frame scheduler, тесты, документы, workflow evidence
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: нет
- Следующий аудит: T-0220 audit r02 после исправлений blocker-ов

## Полный ответ аудитора

VERDICT: NEEDS_FIXES

Обнаружено **8 blocker-ов**. Задача T-0220 не проходит acceptance.

### B1. Два независимых physics accumulator нарушают fixed-step контракт

**File/symbol:**
`RuntimeHost.cs:240-244`, `RuntimeHostFrameScheduler.AdvancePhysics():654-675`; `RuntimeHostOptions.FixedDelta`; `SceneTree.PhysicsFrame()`.

**Нарушенный критерий:**
`TASKS.md:1958-1960, 1967` — один фиксированный шаг, реальное ограничение catch-up, точные dropped-time и diagnostics.

**Доказательство из архива:**
Scheduler накапливает время по произвольному `runOptions.FixedDelta`, а затем для каждого своего шага вызывает:

```csharp
sceneTree.PhysicsFrame(fixedDelta);
```

При этом сам `SceneTree.PhysicsFrame()` уже содержит второй accumulator и всегда исполняет `_PhysicsProcess` шагом `1 / 60`. ([GitHub][1])

`RuntimeHostOptions` допускает любое положительное `FixedDelta`, а документ `fixed-physics-step-and-rigid-body-motion.md:39-45` утверждает, что один вызов scheduler даёт ровно один physics tick, потому что шаги якобы совпадают. Код это равенство не обеспечивает.

Следствия:

* при `FixedDelta = 1/30` один scheduler step вызывает **два** `_PhysicsProcess(1/60)`;
* лимит `MaxPhysicsStepsPerFrame = 5` фактически допускает до **10** physics callbacks;
* `SchedulerPhysicsSteps` сообщает 5, хотя реально выполнено 10;
* при `FixedDelta = 1/120` scheduler считает шаг, но `SceneTree` исполняет callback только после двух вызовов;
* тест `RuntimeHostBoundedRunStaysDeterministicUnlessSchedulerIsEnabled` использует `1/30`, но проверяет только `_Process`, не physics callbacks.

**Требуемое исправление:**
Оставить один источник fixed-step истины. Допустимые варианты:

1. Scheduler напрямую исполняет один реальный physics tick, а accumulator удаляется из `SceneTree`;
2. catch-up и dropped-time полностью переносятся в `SceneTree`, который возвращает число реально исполненных тиков;
3. до реализации настраиваемого physics rate жёстко запретить `FixedDelta`, отличный от `1/60`.

Вложенные accumulator недопустимы.

**Проверка исправления:**
Добавить behavioral tests для `FixedDelta = 1/30` и `1/120`, которые проверяют:

* фактическое число `_PhysicsProcess` callbacks;
* переданный callback delta;
* равенство `SchedulerPhysicsSteps` реальному числу callbacks;
* ограничение catch-up по реальным тикам;
* точное dropped/clamped time.

---

### B2. Deadline начинается после platform input, поэтому input не входит в frame budget

**File/symbol:**
`RuntimeHost.RunLoop():257-339`, особенно `frameStart` на строке 278 и `GetDeadlineWait()`.

**Нарушенный критерий:**
`TASKS.md:1949, 1962, 1965` — ожидание только остатка после **всей** кадровой работы.

**Доказательство из архива:**
Platform input выполняется и измеряется на строках 261-266. Только после него устанавливается:

```csharp
var frameStart = runOptions.Clock.Now;
```

Затем deadline вычисляется как `frameStart + frameInterval`. Поэтому фактический период равен:

```text
platform input следующего кадра + target interval
```

Например, при 60 Hz и 4 ms input цикл занимает около 20,67 ms даже при нулевой остальной работе.

Нормативный pipeline проекта относит input к кадровым стадиям перед physics, process и render, а не за пределами frame budget. 

Существующие тесты не могут обнаружить дефект: `RuntimeHostLoopDriver` не позволяет инъецировать стоимость platform input, а deadline-тесты выполняются с нулевой работой.

**Требуемое исправление:**
Начинать бюджет до platform input либо вести устойчивый `nextDeadline`, не зависящий от положения текущей стадии. В budget должны входить input, physics, process, render-plan, submit и presentation-related CPU work.

**Проверка исправления:**

* при 60 Hz, 4 ms input и 3 ms остальной работы sleep должен быть около 9,67 ms;
* при работе дольше interval sleep должен быть нулевым;
* несколько последовательных кадров не должны накапливать drift;
* input cost должен инъецироваться через test driver, без реального ожидания.

---

### B3. `PresentationSyncEnabled` не управляет production presenter

**File/symbol:**
`RuntimeHost.Run():197-220`; `RuntimeHostOptions.PresentationSyncEnabled`; `RuntimeFramePresenter`; SDL renderer fallback.

**Нарушенный критерий:**
`TASKS.md:1962, 1964, 1965` — software limiter и presentation sync должны быть раздельными, управляемыми и корректными для 60/120/144/165 Hz.

**Доказательство из архива:**
Production presenter создаётся так:

```csharp
new RuntimeFramePresenter(window, runOptions.WindowSize)
```

В него не передаются ни `PresentationSyncEnabled`, ни `TargetFrameRate`. Флаг только запрещает software sleep после `Present()`.

В baseline GPU presenter жёстко устанавливает `GPUPresentMode.VSync`. SDL renderer fallback создаётся через `SDL.CreateRenderer()` без настройки VSync. ([GitHub][2])

SDL renderer по умолчанию создаётся с отключённым VSync, пока приложение явно не вызовет соответствующую настройку. ([Wiki SDL][3])

Следствия:

* `PresentationSyncEnabled = false` не отключает GPU VSync;
* `TargetFrameRate = 60` на 144 Hz GPU-пути не обеспечивает 60 Hz;
* fallback фактически может быть unsynchronized, но при дефолтном `PresentationSyncEnabled = true` software limiter отключён;
* fallback способен работать без ограничения частоты;
* тесты используют fake presenter и проверяют только software sleep, а не production wiring.

**Требуемое исправление:**
Создать единую pacing policy:

* presenter должен получать требуемый режим синхронизации;
* backend должен сообщать фактически активный режим;
* GPU present mode и SDL renderer VSync должны конфигурироваться согласованно;
* software limiter должен включаться по фактическому отсутствию presentation sync, а не по предположению caller-а;
* контракт должен явно определить поведение `TargetFrameRate`, если monitor refresh отличается.

**Проверка исправления:**

* GPU path: sync enabled/disabled;
* SDL fallback: sync enabled/disabled;
* 60 target при synthetic 144 Hz presenter;
* fallback без VSync не работает unbounded;
* нет одновременного VSync wait и второго полного software wait.

---

### B4. Диагностические `submit` и `present` buckets семантически неверны

**File/symbol:**
`RuntimeHost.RunLoop():268-273, 316-338`; `RuntimeHostResult` timing fields.

**Нарушенный критерий:**
`TASKS.md:1967` — корректное разделение `input`, `physics`, `process`, `render-plan`, `submit`, `present`.

**Доказательство из архива:**
Весь вызов:

```csharp
dependencies.Presenter.Present(...)
```

измеряется как `SubmitTimeSeconds`.

Однако GPU `Present()` включает acquisition swapchain texture, построение/загрузку ресурсов, рендеринг и submission; ожидание swapchain также находится внутри этого вызова. ([GitHub][2])

`PresentTimeSeconds`, напротив, получает:

* запрошенную длительность software sleep;
* фиксированные 50 ms pause sleep.

Реально прошедшее время sleep не измеряется. Oversleep не отражается. Pause wait ошибочно классифицируется как presentation.

В `RuntimeHostTests.cs` отсутствуют assertions для:

* `InputTimeSeconds`;
* `PhysicsTimeSeconds`;
* `ProcessTimeSeconds`;
* `RenderPlanTimeSeconds`;
* `SubmitTimeSeconds`;
* `PresentTimeSeconds`.

Следовательно, поля существуют, но их смысл не доказан.

**Требуемое исправление:**

* разделить presenter submission и presentation/swapchain wait либо возвращать stage timings из presenter;
* измерять фактически прошедшее время sleep по clock;
* вынести paused wait в отдельную метрику;
* документировать requested wait и observed wait отдельно.

**Проверка исправления:**
Детерминированный fake clock должен добавлять уникальную длительность каждой стадии. Тест обязан проверять точные значения всех buckets, включая VSync wait, oversleep и pause.

---

### B5. Тесты и red/green evidence недостаточны для acceptance

**File/symbol:**
`RuntimeHostTests.cs:995-1009`; `AUDIT-MANIFEST.md:53-84`; весь audit bundle.

**Нарушенный критерий:**
`TASKS.md:1954, 1956-1968`; repository Feature Gate; требование пользователя о достаточных и воспроизводимых доказательствах.

**Доказательство из архива:**
Два заявленных regression tests являются source-string checks:

```csharp
Assert.DoesNotContain("Thread.Sleep(16)", source);
Assert.DoesNotContain("sceneTree.ProcessFrame(runOptions.FixedDelta)", source);
```

Они не доказывают frame budget или runtime behavior и обходятся переименованием/перестановкой кода.

Отсутствуют behavioral tests на:

* включение input в budget;
* уменьшение sleep после реальной работы;
* zero-wait при overrun;
* cumulative drift и oversleep;
* нестандартный `FixedDelta`;
* production VSync/fallback wiring;
* корректность timing buckets.

Manifest утверждает red-запуск теста `RuntimeHostInteractiveLoopRemovesPostRenderFixedSleepFromSixtyHertzBudget`, но в приложенном green source такого теста нет. Red-only source/patch также отсутствует.

Все результаты представлены только текстом:

```text
Result: passed 11/11
Result: passed 60/60
Result: succeeded
```

Нет TRX, полного stdout/stderr, exit-code artifact или хэша проверяемого состояния. Red worktree удалён, что зафиксировано дневником.

Repository Feature Gate требует behavioral tests до production code и согласованности документов, тестов и реализации. ([GitHub][4])

**Требуемое исправление:**

* заменить source-string tests behavioral tests;
* приложить red-only patch/source для точного baseline;
* приложить machine-readable red и green результаты;
* согласовать названия тестов в manifest с артефактами;
* приложить достаточно файлов или инструкций, чтобы проверки воспроизводились на указанном commit.

**Проверка исправления:**

1. На чистом baseline red patch должен падать по ожидаемому поведению.
2. После применения итогового patch те же тесты должны пройти.
3. Auditor должен воспроизвести focused tests, связанный набор, build и repository verifiers.
4. Должна присутствовать матрица `критерий TASKS → behavioral test → evidence artifact`.

---

### B6. `AUDIT-MANIFEST.md` повреждён и не обеспечивает целостность архива

**File/symbol:**
`AUDIT-MANIFEST.md`.

**Нарушенный критерий:**
Полный и валидный audit manifest, позволяющий проверить состав, baseline, patch и evidence.

**Доказательство из архива:**

* строки 9-10 разрывают `node_repl.exe` на `...найденного` и `ode_repl.exe`;
* строки 15, 28 и 55 содержат `` `\text`` с tab вместо корректного fenced block;
* строки 49 и 51 содержат bare carriage return внутри слов `render-plan` и `ready`;
* файл одновременно использует LF, CRLF и bare CR;
* `AUDIT-MANIFEST.md` отсутствует в собственном списке task-owned файлов;
* отсутствуют SHA-256 и размеры файлов;
* отсутствует checksum самого ZIP;
* отсутствует сопоставление каждого evidence artifact с командой и commit;
* `T-0220.patch` сохранён целиком с CRLF, но manifest не фиксирует требования по нормализации и его checksum.

**Требуемое исправление:**
Перегенерировать manifest как UTF-8/LF:

* валидный Markdown;
* полный список всех ZIP entries;
* размер и SHA-256 каждого файла;
* SHA-256 архива и patch;
* baseline commit и HEAD;
* точные команды, exit codes и пути evidence;
* отсутствие управляющих символов;
* описание line-ending policy для patch.

**Проверка исправления:**

* контрольный scan на bare CR/control characters;
* Markdown lint;
* checksum verification;
* список manifest должен в точности совпасть с содержимым ZIP;
* `git apply --check T-0220.patch` на чистом baseline без предварительного ручного ремонта patch.

---

### B7. Patch содержит несвязанный metadata drift от T-0219

**File/symbol:**
`T-0220.patch`, diff `data/documentation/electron2d-local-docs-index.json`.

**Нарушенный критерий:**
Scope T-0220 и правило отсутствия unrelated metadata churn.

**Доказательство из архива:**
Patch T-0220 изменяет индекс для несвязанных rendering documents:

* `canvas-item-render-queue.md`;
* `rendering-server.md`;
* `texture-resource-baseline.md`.

Также добавляет записи для семи verdict-документов T-0219, включая:

* `t-0219-runtime-presenter-audit.md`;
* lifecycle-error audit;
* reaudit;
* review;
* shadercross isolation;
* shadercross test-scope;
* terminal fault-injection audit.

Эти документы не входят в T-0220 patch и не приложены к архиву. Их hashes и metadata невозможно проверить по bundle.

Правила репозитория требуют ограничивать изменения поведением задачи и избегать постороннего metadata churn. ([GitHub][4])

**Требуемое исправление:**
Перегенерировать индекс от чистого baseline с применёнными только изменениями T-0220. Если T-0219 drift действительно является обязательной prerequisite-правкой, вынести его в отдельный атомарный patch с соответствующими исходными документами и evidence.

**Проверка исправления:**

* diff индекса содержит только записи документов T-0220;
* каждый изменённый hash соответствует файлу baseline или T-0220 patch;
* `Verify-LocalDocumentation.ps1` проходит на чистом baseline после применения patch;
* в T-0220 diff нет T-0219 verdict metadata.

---

### B8. Single-writer workflow не соблюдён и описан противоречиво

**File/symbol:**
`AUDIT-MANIFEST.md:11`; `dev-diary/2026/06 Июнь/25-06-2026.md:301-322`.

**Нарушенный критерий:**
Single-writer workflow и достоверные workflow-записи.

**Доказательство из архива:**
Manifest утверждает:

> После уточнения single-writer правила оркестратор больше не менял worker-owned файлы.

Дневник одновременно фиксирует:

* worker `Kant` реализовал runtime/tests;
* в 20:42 оркестратор получил worker diff;
* затем «при review» заменил `RunForTests` / `RuntimeHostTestLoop` на `Run(..., RuntimeHostLoopDriver)`.

Это является изменением worker-owned production/test code после handoff. Reviewer перестал быть только reviewer и стал вторым writer.

В архиве также отсутствуют:

* ownership map;
* исходный worker diff;
* handoff artifact;
* указание, кто именно внёс финальные строки;
* последовательность ревизий, подтверждающая заявленное исправление процесса.

**Требуемое исправление:**
Повторить доработку с одним назначенным writer для каждого файла:

* reviewer выдаёт findings, но не меняет worker-owned код;
* исправления выполняет тот же writer либо оформляется явная передача ownership до начала изменений;
* manifest и дневник должны одинаково отражать фактический процесс;
* приложить worker handoff diff и chronology.

**Проверка исправления:**

* один writer на каждый production/test файл;
* отсутствуют reviewer edits после worker handoff;
* ownership map совпадает с diff chronology;
* manifest, дневник и worker artifact не противоречат друг другу.

Архив при этом технически распаковывается, не содержит `.git`, `bin`, `obj`, `.temp` или вложенных ZIP; patch после нормализации имеет внутренне согласованные blob identities. Эти положительные свойства не устраняют архитектурные, тестовые и workflow blocker-ы.

[1]: https://github.com/edwardgushchin/Electron2D/blob/1f8fda08c370f2d2a7cce5eb669019906ed825d4/src/Electron2D/Core/SceneTree/SceneTree.cs "Electron2D/src/Electron2D/Core/SceneTree/SceneTree.cs at 1f8fda08c370f2d2a7cce5eb669019906ed825d4 · edwardgushchin/Electron2D · GitHub"
[2]: https://github.com/edwardgushchin/Electron2D/blob/1f8fda08c370f2d2a7cce5eb669019906ed825d4/src/Electron2D/Runtime/Application/RuntimeFramePresenter.cs "Electron2D/src/Electron2D/Runtime/Application/RuntimeFramePresenter.cs at 1f8fda08c370f2d2a7cce5eb669019906ed825d4 · edwardgushchin/Electron2D · GitHub"
[3]: https://wiki.libsdl.org/SDL3/SDL_SetRenderVSync "https://wiki.libsdl.org/SDL3/SDL_SetRenderVSync"
[4]: https://raw.githubusercontent.com/edwardgushchin/Electron2D/1f8fda08c370f2d2a7cce5eb669019906ed825d4/AGENTS.md "raw.githubusercontent.com"

