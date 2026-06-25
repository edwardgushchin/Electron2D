# Verdict: T-0220 audit r04

- Задача: `T-0220`
- Домен: runtime
- Актуально на: 2026-06-26T01:38:51+03:00
- Область проверки: `T-0220-audit-r04.zip`, SHA-256 `6dc4d231a63b7cdd6c7977e6e8587d9f9734156148a0f5bfa10da32589dc6efe`; постановка, реализация, архитектура, regressions, тесты, evidence, `TASKS.md`, дневник, release notes и правила репозитория.
- Статус вывода: `VERDICT: ACCEPT`
- Предыдущий аудит: `docs/verdicts/runtime/t-0220-audit-r03.md`
- Следующий аудит: не требуется; r04 принят без blocker-ов.

## Полный ответ аудитора

VERDICT: ACCEPT

Blocker-ов не обнаружено. Архив T-0220-audit-r04.zip принят для T-0220.

Проверка целостности: SHA-256 архива совпадает с заявленным 6dc4d231a63b7cdd6c7977e6e8587d9f9734156148a0f5bfa10da32589dc6efe. Запрещённых вложений уровня .git, bin, obj, nested ZIP или временных каталогов в архиве не выявлено.

Итог по ключевым критериям

1. Scope T-0220 соблюдён.
Задача закрывает именно runtime/frame timing/fixed physics defect: старый интерактивный loop больше не использует фиксированный _Process(1/60) и не добавляет unconditional fixed post-render wait к бюджету кадра. r04 заявлена как evidence/restore-итерация после r03, и это соответствует содержимому: runtime-состояние согласовано с T-0220, а добавленная ценность r04 — baseline-red и clean restore evidence.

2. Runtime/frame scheduler архитектурно приемлем.
RuntimeHost теперь использует measured monotonic delta через scheduler, clamp MaxDeltaTimeSeconds, fixed-step accumulator на SceneTree.FixedPhysicsStep, bounded catch-up через MaxPhysicsStepsPerFrame, dropped-time diagnostics и deadline wait только на остаток бюджета кадра. Бюджет начинается до input/process/render/present, что закрывает дефект “render/present вне frame budget”. Окно minimized/unfocused не накапливает delta: scheduler reset выполняется вокруг pause sleep.

3. Fixed physics step реализован без public API regression.
Интерактивный runtime вызывает fixed physics через единый SceneTree.FixedPhysicsStep, а legacy/bounded deterministic path сохранён для smoke/test сценариев. RuntimeHostOptions, scheduler, presenter settings и diagnostics остаются internal/runtime-level контрактами; признаков утечки SDL/Box2D/Serilog типов в публичный API по изменённым файлам не выявлено.

4. R03-1 закрыт.
В red evidence теперь есть две baseline-failing проверки:

RuntimeHostInteractiveLoopUsesMeasuredProcessDelta;

RuntimeHostInteractiveLoopDoesNotAddFixedPostRenderWaitToFrameBudget.

Baseline red evidence корректно падает с dotnet test exit code: 1. Вторая проверка действительно ловит старый fixed post-render wait: frame-start intervals выходят за 60 Hz deadline после per-frame work. Это достаточная красная демонстрация исходного дефекта, а не только косвенная проверка measured delta.

5. R03-2 закрыт.
Restore evidence выполнен от чистого detached worktree baseline 1f8fda08c370f2d2a7cce5eb669019906ed825d4 с последовательным применением patch-ей:

T-0220-tracked-full.patch;

T-0220-untracked-verdicts.patch;

T-0220-workflow-local-snapshots.patch.

Затем материализованы exact state snapshots для нейтрализации Windows line-ending conversion, выполнено SHA-256 сравнение task-owned bytes, и итог restore summary показывает требуемый результат:

worktree_add=0 apply=T-0220-tracked-full.patch:0,T-0220-untracked-verdicts.patch:0,T-0220-workflow-local-snapshots.patch:0 reset=0 snapshot=0 hash=PASS test=0 restore=0 build=0 tasks=0 docs=0 license=0 diff=0

Это закрывает воспроизводимость от baseline.

6. Green evidence достаточен.
Focused runtime tests проходят: 70/70 passed, exit code 0. Build проходит с 0 warnings / 0 errors. Также проходят:

Verify-Tasks;

Verify-LocalDocumentation;

Verify-SourceLicenseHeaders;

git diff --check.

Restore worktree повторяет focused tests/build/verifiers с exit code 0, что сильнее обычного локального green evidence.

7. TASKS.md, diary, release notes, verdict chain — приемлемо.
TASKS.md оставляет T-0220 в состоянии in progress с незакрытым external audit checkbox, что корректно до acceptance-аудита. Документы runtime/physics/release notes отражают новую модель scheduler-а, measured delta, fixed physics, presentation sync/software limiter и diagnostics. Verdict chain r01/r02/r03 включён; r04 evidence явно направлен на два r03 blocker-а. Дневник и worker evidence согласованы по хронологии.

8. AUDIT-MANIFEST.md и evidence inventory полные.
Manifest содержит baseline/head, branch, git status, scope, evidence map, restore summary, SHA/checks и объяснение included ignored completed-task snapshots. Evidence matrix покрывает red/green/restore/verifier/workflow доказательства. State snapshots и hash comparison дают достаточную основу для воспроизводимого acceptance review.

Неблокирующие замечания

В restore log есть patch warnings по trailing whitespace в untracked verdict patch, но финальный git diff --check проходит с exit code 0, а runtime/task-owned bytes проходят SHA comparison. Это не blocker.

Также архив содержит Windows-ориентированные пути/локали дневника и completed-tasks; restore evidence выполнен именно в целевой Windows-среде репозитория и компенсирует line-ending conversion через state snapshots. Для этой задачи это не blocker.

Acceptance decision

T-0220 r04 принимается. Оба r03 blocker-а закрыты доказательно: baseline-red теперь ловит оба исходных runtime-дефекта, а clean restore от baseline подтверждает patch/state reproducibility с hash=PASS и всеми проверками 0.
