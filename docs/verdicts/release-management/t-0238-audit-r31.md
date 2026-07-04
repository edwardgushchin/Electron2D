VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен пакет `T-0238` итерации `r31` как полный инженерный аудит текущей области задачи. Архив читается, `metadata/repo-file-snapshots.json` присутствует, полные итоговые версии changed files доступны в `repo-after/`, а patch использовался только как карта изменений.
- Область пакета согласована как одиночная задача `T-0238`. Проверены реализация, тесты, документация, сохранённые прошлые verdict-отчёты, closure notes в `TASKS.md`, raw evidence и заявленное закрытие прошлых замечаний. Большая часть предыдущих блокеров реально закрыта: selected-state для plain menu rows усилен, behavior-level regression для already-open menu item есть, zero-context правило для `Suggested new task` перенесено в `AUDIT-REQUEST.md`, а сохранённые прошлые verdict-файлы теперь включены в пакет и verifier.
- Но принять изменение нельзя. Заявленное закрытие r30 local stale-download failure неполное: новый helper действительно ждёт newest non-baseline target на уровне выбора ready target id, однако обычный polling path всё ещё может уйти в page-level export fallback и скачать старый Markdown, после чего команда аварийно остановится на `E2D-BUILD-AUDIT-SUBMIT-REPORT-STALE` вместо ожидания текущего отчёта. Дополнительно тесты и task/docs overclaim-ят закрытие этого сценария: доказан только helper-level кусок и source-level наличие флага, а не реальный production polling/export path.

Техническая привязка:
- `metadata.taskId`: `T-0238`
- `metadata.iteration`: `r31`
- `metadata.scopeTaskIds`: `["T-0238"]`
- `metadata.scopeSummary`: r31 заявляет закрытие saved primary `r29 NEEDS_FIXES` и локального r30 stale-download failure без saved verdict report.
- Проверенные ключевые файлы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `T-0238.patch`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/eng/Electron2D.Build/Program.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`, сохранённые files under `repo-after/docs/verdicts/release-management/`.
- Проверенные классы работ: `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `full file review`.

BLOCKERS:
- B1
  - Что не так: r31 исправляет только выбор ready target на уровне helper-а, но не закрывает сам stale-export path обычного submit. Если newest non-baseline Deep Research target ещё не готов, `TryFindSingleReadyDeepResearchTargetIdAsync(...)` теперь может вернуть `null`, однако `DownloadReportCandidatesAsync(...)` после этого без дополнительных условий падает обратно на page-level export fallback. Этот fallback глобально ищет единственную export/download кнопку и единственный Markdown menu item по всей странице и может кликнуть старую готовую report-card выше по истории. Дальше polling policy принимает скачанный Markdown как кандидат отчёта, а строгая проверка iteration уже после выхода из браузерной автоматизации валит всё как `E2D-BUILD-AUDIT-SUBMIT-REPORT-STALE`. То есть ordinary submit всё ещё может скачать старый отчёт вместо ожидания готовности текущего.
  - Почему это важно: это прямое незакрытое ядро r30 local stale-download failure. В `scopeSummary`, `TASKS.md` и `audit-package.md` заявлено более сильное поведение: когда после latest ready target есть более новый `ready=false` target, ordinary submit должен ждать дальше и не экспортировать старый отчёт. При текущей реализации это обещание не выполняется end-to-end.
  - Что исправить: ordinary submit должен блокировать page-level export fallback, если по `Target.getTargets` видно, что после latest ready non-baseline target есть более новый non-baseline target без ready surface. Возможные безопасные варианты:
    - возвращать из polling path явное состояние «ждать дальше», не доходя до page-level export;
    - либо разрешать page-level fallback только когда он привязан к текущему ready frame/target surface, а не к любой старой report-card на странице.
    После исправления нужно синхронизировать документацию про ordinary submit wait semantics.
  - Как проверить исправление: добавить behavior-level regression через production polling/export path или стабильный внутренний browser contract, который моделирует reused-чат со следующими условиями:
    - baseline targets уже проигнорированы;
    - есть старый ready non-baseline report surface на странице;
    - есть более новый non-baseline target, который ещё `ready=false`;
    - page-level export button/menu item для старой карточки видим и единственен.
    В ordinary submit результат должен быть `Wait`/следующий polling cycle, а не скачивание stale Markdown и не выброс `E2D-BUILD-AUDIT-SUBMIT-REPORT-STALE`. Нужен также положительный сценарий, где newest target становится ready и только тогда отчёт экспортируется.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1303-1331`, `CapturePollingDecisionAsync(...)`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1504-1555`, `DownloadReportCandidatesAsync(...)`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1727-1731`, `DownloadReportCandidatesFromDeepResearchTargetAsync(...)`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1940-1965`, ordered `SelectReadyDeepResearchTargetId(...)`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2583-2745`, page-level `ReportExportButtonClickExpression` / `ExportReportMarkdownMenuItemClickExpression`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:3172-3213`, `IsGeneratingExpression`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:95-150`, `ValidateReportMatchesSubmitIteration(...)`
    - `File/symbol`: `repo-after/docs/release-management/audit-package.md:93`, ordinary submit "ждёт дальше и не экспортирует старый отчёт"
    - `File/symbol`: `repo-after/docs/release-management/audit-package.md:136`, page-level DOM fallback при отсутствии выбранной frame/target surface
    - `File/symbol`: `repo-after/TASKS.md:1835`, критерий задачи отмечен как закрытый
    - `File/symbol`: `repo-after/TASKS.md:2002-2004`, описание реального r30 stale-download failure и заявленного fix
    - `Criterion`: `implementation content review`, `task compliance review`, `previous blockers closure`, `observable behavior`
    - `Evidence`:
      - helper `SelectReadyDeepResearchTargetId(...)` действительно возвращает `null`, когда newest non-baseline target ещё не ready;
      - но `DownloadReportCandidatesAsync(...)` после этого безусловно переходит к page-level export fallback;
      - page-level export selectors глобальны по странице и не привязаны к newest target;
      - `IsGeneratingExpression` читает только видимое состояние страницы и не доказывает, что более новый target в фоне не продолжает генерироваться;
      - stale validation происходит уже после скачивания и завершает submit error-ом, а не переводит его обратно в wait state.
    - `Impact`: ordinary submit может воспроизводить тот же класс отказа, который r31 заявляет как закрытый: старый Markdown скачивается и процесс завершается stale-ошибкой, вместо ожидания актуального отчёта.
    - `Fix`: запретить ordinary page-level fallback при наличии более нового non-ready non-baseline target либо жёстко привязать fallback только к подтверждённой current report surface.
    - `Verification`: behavior-level regression на production polling/export path + повтор `audit-submit-control-followups-and-deep-research-focused-tests-r31`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`.

- B2
  - Что не так: тесты и task/docs недодоказывают и одновременно переоценивают закрытие r30 stale-download failure. Единственный новый regression `AuditSubmitReadyTargetSelectionWaitsForNewestNonBaselineTarget` проверяет только приватный helper выбора target id по спискам строк. Дополнительный `AuditSubmitOrdinaryPollingAllowsLatestReadyTargetAfterBaseline` вообще делает лишь source-level проверку текста метода `CapturePollingDecisionAsync(...)` на наличие флага `allowLatestReadyTargetFallback: true`. Ни один из этих тестов не исполняет production polling/export path с page-level fallback и поэтому не способен поймать фактическую дыру из B1. При этом `TASKS.md` уже помечает закрытие r30 failure как выполненное именно этой regression-проверкой.
  - Почему это важно: для T-0238 тесты должны доказывать реальное поведение production path или стабильного внутреннего контракта. Здесь claim сформулирован как functional closure реального локального отказа ordinary submit, но тесты проверяют только helper и наличие флага в исходнике. Это недостаточное доказательство предыдущего blocker closure и отдельный блокер по критерию реалистичности тестов.
  - Что исправить: заменить или дополнить текущие helper/source-level проверки behavior-level regression-ом, который исполняет production polling/export corridor. После этого `TASKS.md` и, при необходимости, `audit-package.md` должны ссылаться уже на реальный сценарий, а не на helper-only coverage.
  - Как проверить исправление: нужен тест через production код или стабильный внутренний browser contract, который моделирует ordinary polling при reused-чате с old ready report surface и newer non-ready target. Тест должен доказывать, что:
    - ordinary submit не возвращает stale report candidate;
    - page-level export не используется до готовности newest target;
    - после ready newest target polling начинает возвращать именно текущий report.
    Дополнительно полезен negative test, который падал бы на текущей r31 реализации.
  - Техническая привязка:
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:4058-4081`, `AuditSubmitReadyTargetSelectionWaitsForNewestNonBaselineTarget`
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:4126-4134`, `AuditSubmitOrdinaryPollingAllowsLatestReadyTargetAfterBaseline`
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:4287-4317`, существующие source-level checks вокруг fallback-логики
    - `File/symbol`: `repo-after/TASKS.md:1835`, claim о закрытии r30 failure
    - `Criterion`: `test coverage review`, `test realism`, `previous blockers closure`, `evidence gap`
    - `Evidence`:
      - current regression invokes only `SelectReadyDeepResearchTargetId(...)`, а не `CapturePollingDecisionAsync(...)`/`DownloadReportCandidatesAsync(...)`;
      - current source-level test не исполняет production behavior вообще;
      - именно поэтому текущий пакет пропустил незакрытый stale-export path из B1, хотя focused suite `65/65` зелёный.
    - `Impact`: пакет не даёт достаточно сильного автоматического доказательства, что r30 failure действительно закрыт, и task state в `TASKS.md` становится недостоверным.
    - `Fix`: добавить behavior-level regression на реальный ordinary submit polling/export path и обновить task/doc wording под фактическое доказательство.
    - `Verification`: новый regression должен падать на r31 и проходить после исправления B1; затем повторяются все checks из evidence.

EVIDENCE_REVIEW:
- Проверены metadata и область пакета. `metadata.scopeTaskIds` содержит только `T-0238`; признаков `combined scope` не найдено. `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json` и фактический состав архива согласованы.
- Полнота снимков достаточна. `metadata/repo-file-snapshots.json` содержит полные snapshot-ы всех 25 changed files; для всех записей `fullContentIncluded: true`. Существенного `evidence gap` по отсутствующим implementation/test/doc files не найдено.
- Прочитаны полные итоговые версии changed implementation, tests и documentation files в `repo-after/`; `T-0238.patch` использовался только как карта изменений.
- Проверены прошлые verdict-файлы из `metadata.previousVerdictChain`. В архиве доступны и прочитаны `r01`, `r02`, `r04`, `r16`, `r18`, `r19`, `r20`, `r21`, `r24`, `r25`, `r27` и `r29`. Признаков их переписывания или сокращения внутри текущего пакета не найдено. Отсутствие `r22`, `r23`, `r26`, `r28`, `r30` обосновано тем, что saved verdict reports для них не было; это согласовано с `scopeSummary` и `TASKS.md`.
- Проверен `metadata.blockerClosureList`. Закрытие r29 B1/B2 в текущем пакете подтверждается: plain menu rows отвергаются selected-state, а already-open Deep Research menu loop теперь доказан behavior-level test-ом внутреннего driver-контракта. Но заявленное закрытие r30 local stale-download failure остаётся неполным и недодоказанным по причинам B1 и B2 выше.
- Проверены raw evidence checks. Все приложенные команды завершились успешно:
  - `audit-submit-control-followups-and-deep-research-focused-tests-r31` — `65/65 passed`;
  - `update docs --check` — passed;
  - `verify docs` — passed;
  - `verify audit-followups` — passed, `9 actionable findings across 87 saved audit reports`;
  - `verify licenses` — passed;
  - `git diff --check` — passed.
  Эти результаты учитываются, но не снимают blocker-ы, потому что ключевой ordinary stale-download сценарий не покрыт нужным уровнем реалистичных tests и всё ещё остаётся возможным в production path.
- Проверка секретов и локальных данных в пределах текущего scope не выявила реальных токенов, приватных ключей, паролей, concrete conversation URL или других конфиденциальных данных, включённых в пакет.
- Текущая задача относится к release-management tooling. Изменения не затрагивают runtime hot path игрового движка, публичный 2D API или Godot 4.7 parity surface, поэтому отдельного performance/Public API blocker-а по этой итерации не найдено.

Техническая привязка:
- Metadata/artifacts: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0238.patch`
- Implementation: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/eng/Electron2D.Build/Program.cs`
- Tests: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
- Docs/rules: `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`
- Previous verdict files: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `t-0238-audit-r02.md`, `t-0238-audit-r04.md`, `t-0238-audit-r16.md`, `t-0238-audit-r18.md`, `t-0238-audit-r19.md`, `t-0238-audit-r20.md`, `t-0238-audit-r21.md`, `t-0238-audit-r24.md`, `t-0238-audit-r25.md`, `t-0238-audit-r27.md`, `t-0238-audit-r29.md`
- Evidence: `evidence/T-0238-r31/checks/audit-submit-control-followups-and-deep-research-focused-tests-r31/stdout.txt`, `evidence/T-0238-r31/checks/update-docs-check/stdout.txt`, `evidence/T-0238-r31/checks/verify-docs/stdout.txt`, `evidence/T-0238-r31/checks/verify-audit-followups/stdout.txt`, `evidence/T-0238-r31/checks/verify-licenses/stdout.txt`, `evidence/T-0238-r31/checks/git-diff-check/exit-code.txt`

RISKS_AND_NOTES:
- FOLLOW_UP_FINDING F1
  - Идентификатор: `F1`
  - Где найдено: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:4097-4106`
  - Проблема: проверка `--dump-dom-only` по-прежнему остаётся source-level inspection текста метода `DumpDomFromUrlAsync`, а не behavior-level regression-ом, который реально вызывает production path и подтверждает запись diagnostics файлов в нужном порядке.
  - Почему не блокирует текущую задачу: это инженерный долг внутри той же области T-0238, но текущий пакет уже блокируется более прямой проблемой ordinary stale-download path и недостаточным доказательством его закрытия. Отсутствие behavior-level test для dump-dom diagnostics ухудшает строгость, но не является главным препятствием для текущего verdict-а.
  - Куда перенести: `Suggested existing task: T-0238`
  - Рекомендуемый приоритет: `P2`
  - Как проверить: заменить source-level check на behavior-level fixture, которая вызывает production `DumpDomFromUrlAsync` через контролируемый browser/client contract и подтверждает создание `deep-research-selected-result.json` и `deep-research-selected-diagnostics.json` до deep-research-frame dependent шагов.
  - Техническая привязка:
    - `FOLLOW_UP_FINDING F1`
    - `File/symbol`: `AuditSubmitDumpDomOnlyWritesBaseDomBeforeDeepResearchFrameDiagnostics`
    - `Why not blocker for current task`: текущий пакет уже блокируется B1/B2
    - `Suggested existing task`: `T-0238`
    - `Suggested priority`: `P2`
    - `Verification idea`: behavior-level regression for dump-dom diagnostics ordering

- ACCEPTED_RISK R1
  - Человекочитаемое объяснение: в пакете по-прежнему явно оформлен принятый риск по semantic validation closure target. `verify audit-followups` проверяет форму и трассируемость closure notes, но не доказывает семантически, что `tracked-existing` или `tracked-new` действительно указывают на существующую и корректную по смыслу задачу. Этот риск уже осознанно оформлен и не является новым blocker-ом текущей итерации.
  - Техническая привязка:
    - `ACCEPTED_RISK R1`
    - `File/symbol`: `repo-after/TASKS.md:1901-1912`
    - `Rationale`: структурная closure-verification реализована; semantic inventory validation отложена как отдельное усиление
    - `Next decision point`: `T-0105 risk register`
    - Служебный класс: `accepted risk`

CLOSURE_DECISION:
- Задача остаётся открытой. Пакет `T-0238 r31` содержит достаточные материалы для полноценного аудита и реально закрывает значительную часть исторических замечаний, но принять его нельзя, пока ordinary submit всё ещё может уйти в stale page-level export вместо ожидания newest non-baseline target, а тесты не доказывают закрытие этого сценария на production path.
- До следующей итерации нужно:
  - закрыть B1 кодом, а не только helper-логикой выбора target id;
  - закрыть B2 behavior-level regression-ами на реальный ordinary polling/export path;
  - синхронизировать `TASKS.md` и `docs/release-management/audit-package.md` с фактическим поведением после исправления;
  - затем повторить focused suite, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check`.

Техническая привязка:
- Блокируют: `B1`, `B2`
- Обязательные проверки после исправления: новый behavior-level stale-download regression на ordinary submit, повтор `evidence/T-0238-rXX/checks/audit-submit-control-followups-and-deep-research-focused-tests-*`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`
- Решение относится к текущему пакету: `metadata.taskId = T-0238`, `metadata.iteration = r31`
