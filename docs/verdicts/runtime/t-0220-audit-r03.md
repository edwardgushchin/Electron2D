# External Audit Verdict: T-0220 r03

- Задача: T-0220
- Домен: runtime
- Актуально на: 2026-06-26T00:26:32+03:00
- Область проверки: внешний acceptance audit архива `T-0220-audit-r03.zip`
- Статус вывода: VERDICT: NEEDS_FIXES
- Предыдущий аудит: `docs/verdicts/runtime/t-0220-audit-r02.md`
- Следующий аудит: `docs/verdicts/runtime/t-0220-audit-r04.md` после исправлений.

---

VERDICT: NEEDS_FIXES

T-0220 r03 не принимаю. Реализация runtime-сайд выглядит в целом корректной: scheduler переведён на измеренный DeltaTime, фиксированная физика идёт через accumulator и SceneTree.FixedPhysicsStep, deadline wait считается после input/process/render/present, pause/minimized path не накапливает delta, software limiter отключается при observed presentation sync, diagnostics разбиты по стадиям. ZIP SHA-256 совпадает с заявленным 61aefd009169ce68bfd323db1ed0afb9d4f08cc4b7d1d77a35804327162ec537; явных forbidden files в архиве не обнаружено; manifest inventory и SHA для файлов архива в основном воспроизводятся.

Но ACCEPT невозможен: в r03 остались blocker-ы по доказательной базе и воспроизводимости patch model. Текстовые утверждения в manifest/evidence не закрывают эти требования без исполняемых или машинно-проверяемых доказательств.

BLOCKER R03-1 — red evidence покрывает только _Process delta, но не доказывает исходный post-render Thread.Sleep(16) / 60 FPS budget defect

File/symbol или archive path

TASKS.md, критерий T-0220: “добавлены сначала падающие тесты, доказывающие, что Thread.Sleep(16) после render/present ломает 60 FPS budget и что _Process получает не фактический delta”.

evidence/red/T-0220-red.patch

evidence/red/T-0220-red-dotnet-test.log

evidence/red/T-0220-red.trx

evidence/evidence-matrix.md

Нарушенный критерий

Red phase должен доказывать оба исходных дефекта T-0220:

_Process получает нефактический/fixed delta.

Старый unconditional post-render sleep ломает frame budget / cadence.

В r03 доказан только первый пункт.

Evidence из архива

evidence/red/T-0220-red.patch добавляет один red test: RuntimeHostInteractiveLoopUsesMeasuredProcessDelta. Он проверяет, что baseline отдаёт в _Process фиксированное 1/60, а не измеренный delta после Sleep(80).

evidence/red/T-0220-red-dotnet-test.log запускает только этот test case по фильтру FullyQualifiedName~RuntimeHostInteractiveLoopUsesMeasuredProcessDelta. evidence/red/T-0220-red.trx содержит один failed test. Это достаточное red-доказательство для _Process delta, но не для post-render sleep / deadline budget.

В evidence/evidence-matrix.md строка про removal unconditional post-render wait ссылается на final code и green tests, но не на baseline-failing red evidence. То есть дефект “Thread.Sleep(16) после render/present ломает 60 FPS budget” подтверждён только конечными зелёными тестами, а не red-first доказательством.

Требуемое исправление

Добавить red-only тест или набор тестов, которые на baseline 1f8fda08c370f2d2a7cce5eb669019906ed825d4 воспроизводимо падают именно из-за старого post-render wait / неверного frame budget. Тест должен проверять не просто наличие нового scheduler API, а observable behavior старого цикла: например, что platform input + process/render/present уже занимают часть frame budget, а baseline всё равно добавляет фиксированный post-render wait и уводит cadence за target interval.

Red patch должен оставаться минимальным и применяться к clean baseline отдельно от final implementation patch.

Проверка, которая должна подтвердить исправление

В архиве должны появиться:

red patch с отдельным test case для post-render sleep / budget defect;

dotnet test log и .trx, где на clean baseline падают оба red-test блока: _Process measured delta и post-render budget;

exit code 1 для red run;

evidence matrix, где post-render wait criterion ссылается именно на этот red artifact, а не только на final green tests.

BLOCKER R03-2 — patch reproducibility доказана только через git apply --check, но не доказано восстановление финального audit state из baseline

File/symbol или archive path

patches/T-0220-patch-apply-check.log

patches/T-0220-tracked-full.patch

patches/T-0220-untracked-verdicts.patch

patches/T-0220-workflow-local-snapshots.patch

AUDIT-MANIFEST.md

SHA256SUMS.txt

evidence/green/*

Нарушенный критерий

Для external acceptance archive недостаточно доказать, что patch files синтаксически применимы. Требуется доказать воспроизводимую модель:

clean worktree на baseline 1f8fda08c370f2d2a7cce5eb669019906ed825d4;

применение patch sequence;

итоговые file hashes совпадают с archive manifest / task-owned state;

git status --short --untracked-files=all после применения соответствует manifest;

green tests/verifiers относятся именно к восстановленному patch state, а не только к локальному рабочему дереву.

Это также связано с r02 blocker R02-3 по patch model и archive reproducibility.

Evidence из архива

patches/T-0220-patch-apply-check.log содержит только git apply --check для:

T-0220-tracked-full.patch

T-0220-untracked-verdicts.patch

T-0220-workflow-local-snapshots.patch

T-0220-red.patch

git apply --check доказывает только применимость patch без фактического применения. В архиве нет машинно-проверяемого лога, который:

применяет patch sequence в clean worktree;

после применения считает SHA-256 task-owned файлов;

сравнивает эти SHA с AUDIT-MANIFEST.md / SHA256SUMS.txt;

фиксирует итоговый git status;

запускает focused green tests/verifiers именно в этом reconstructed worktree.

Green logs находятся как evidence успешной локальной проверки, но они не привязаны к восстановленному из patch sequence состоянию. Поэтому archive reproducibility остаётся недоказанной.

Требуемое исправление

Добавить воспроизводимый patch-restore evidence script/log:

создать clean detached worktree на baseline;

применить patch sequence в документированном порядке;

вывести git status --short --untracked-files=all;

посчитать SHA-256 всех файлов, которые должны получиться после patch restore;

сравнить их с manifest/archive inventory;

запустить focused green tests и обязательные verifiers из восстановленного состояния;

сохранить logs, .trx, exit-code files и hash comparison output.

Проверка, которая должна подтвердить исправление

В r04 auditor должен иметь возможность открыть один log/script output и увидеть:

baseline commit подтверждён;

все patches реально применены, не только --check;

итоговые hashes совпали;

итоговый status соответствует manifest;

focused green suite passed;

verifier exit codes равны 0.

Статус r02 blocker-ов

R02-1: в основном закрыт. Local documentation prerequisites вынесены в evidence/local-docs-prereq, snapshots присутствуют. Замечание: source-hashes.md имеет сломанные PowerShell-expansion строки вида $(@{...}.Path), но сами snapshots и manifest позволяют перепроверить hashes, поэтому отдельно blocker не ставлю.

R02-2: закрыт. AUDIT-MANIFEST.md стал читаемым, inventory совпадает с архивом, SHA256SUMS.txt проверяем, control-byte corruption не обнаружена.

R02-3: не закрыт полностью — см. BLOCKER R03-2.

R02-4: не закрыт полностью — машинные red/green artifacts появились, но red coverage неполный; см. BLOCKER R03-1.

R02-5: закрыт. Хронология worker/task notes стала непротиворечивой, worker ownership evidence присутствует.

Non-blocking notes

TASKS.md оставляет T-0220 в ready for acceptance, а последний acceptance criterion про external audit остаётся unchecked. Это корректно для r03 до получения ACCEPT.

Dev diary фиксирует подготовку r03 evidence, но финальный момент сборки архива/manifest желательно явно дописать в r04, чтобы workflow chain был полностью замкнут. Само по себе это не blocker при наличии валидного manifest.

Архитектурная рамка проверки сверялась с приложенными целями Electron2D: публичный API не должен протекать SDL/Box2D/Serilog типами, Core/Runtime остаются внутренним разделением монолитной библиотеки, frame loop должен соответствовать runtime pipeline и no-hot-path-allocation intent. 

1) Цели проекта
