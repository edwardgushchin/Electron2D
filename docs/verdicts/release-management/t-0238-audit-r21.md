VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен полный пакет `T-0238` итерации `r21` по полным снимкам файлов, а не по одному patch. Я прочитал manifest, metadata, полный changed scope в `repo-after/`, baseline-снимки в `repo-before/`, предыдущие verdict-файлы из цепочки, `TASKS.md`, документацию и приложенные результаты локальных проверок.
- Пакет действительно закрывает значимую часть замечаний из `r20`: `verify audit-followups` теперь читает сохранённые отчёты с первой строкой `VERDICT: ACCEPT` и `VERDICT: NEEDS_FIXES`, в `TASKS.md` добавлены closure notes для семи исторических `FOLLOW_UP_FINDING F1`, для ordinary submit добавлен behavior-level test на ignored baseline targets, а временный scope leak с активной `T-0240` из текущей области убран.
- Принять пакет всё равно нельзя. В текущем состоянии реализация `audit submit --control-audit` не выполняет собственный строгий контракт clean-control ZIP: она пропускает ZIP, где `metadata.previousVerdictChain` или `metadata.blockerClosureList` вообще отсутствуют, хотя документация, критерии задачи и сам `metadata.blockerClosureList` этой итерации утверждают более жёсткое правило — эти поля должны быть массивами длиной ровно `0`. Это делает заявленное закрытие прошлого blocker-а неполным и оставляет обход zero-context guard до подключения к браузеру.

- Техническая привязка:
  - `metadata.taskId`: `T-0238`
  - `metadata.iteration`: `r21`
  - `metadata.scopeTaskIds`: `["T-0238"]`
  - `metadata.scopeSummary`: `T-0238 r21 follows saved primary r20 NEEDS_FIXES...`
  - Полные проверенные материалы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`, `repo-after/**`, `repo-before/**`, `evidence/T-0238-r21/checks/**`
  - Ключевые файлы по реализации и контракту: `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/Program.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`, `repo-after/TASKS.md`
  - Проверенные прошлые verdict-файлы из `metadata.previousVerdictChain`: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `...r02.md`, `...r04.md`, `...r16.md`, `...r18.md`, `...r19.md`, `...r20.md`

BLOCKERS:
- B1
  - Что не так: Проверка clean-control ZIP в `audit submit --control-audit` остаётся слабее заявленного контракта. Метод `ValidateControlAuditMetadataArrayEmpty` просто выходит без ошибки, если свойства `metadata.previousVerdictChain` или `metadata.blockerClosureList` отсутствуют. То есть malformed control ZIP без этих полей пройдёт pre-browser guard, хотя текущая документация и критерии задачи требуют не «отсутствие или пустоту», а именно массивы длиной ровно `0`.
  - Почему это важно: Эта задача должна доказать независимый control audit с нулевым контекстом прошлых внешних отчётов. Если malformed ZIP с отсутствующими полями проходит, zero-context contract enforced не полностью. Дополнительно это ломает проверяемое закрытие прошлых замечаний: в `metadata.blockerClosureList` текущего пакета уже записано, что blocker `B1 r04` закрыт и что эти поля «must have exactly zero elements», но код на самом деле этого не требует.
  - Что исправить: Сделать проверку структурно строгой. Для `--control-audit` оба свойства должны обязательно присутствовать в `metadata/audit-package.input.json`, иметь тип `array` и длину ровно `0`. Отсутствующее свойство, `null`, объект, строка или любой другой тип должны отклоняться той же диагностикой `E2D-BUILD-AUDIT-SUBMIT-CONTROL-CONTEXT`.
  - Как проверить исправление: Добавить focused regression tests на два кейса: control ZIP без `previousVerdictChain` и control ZIP без `blockerClosureList`; оба должны падать до запуска браузера. После этого повторно прогнать focused control-audit tests и заявленные package checks.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:513-531`
    - `Criterion`: `repo-after/TASKS.md:1807`; `repo-after/docs/release-management/audit-package.md:79`; `repo-after/docs/release-management/audit-package.md:118`
    - `Evidence`:
      - Код пропускает отсутствие свойства: `AuditSubmitCommand.cs:515-518`
      - Документация требует пустые массивы: `audit-package.md:79`, `audit-package.md:118`
      - Критерий задачи требует массивы любой длины кроме `0` отклонять до браузера: `TASKS.md:1807`
      - Текущий `metadata.blockerClosureList` уже утверждает более сильное правило, чем реально реализовано: `metadata/audit-package.input.json:161`
      - Focused tests покрывают только non-empty/dirty arrays, но не отсутствие свойства: `RepositoryBuildToolTests.cs:3152-3246`
      - Тестовый helper сериализует оба поля как массивы по умолчанию, поэтому missing-property bypass не проверяется: `RepositoryBuildToolTests.cs:10272-10306`
    - `Impact`: clean control ZIP contract и previous blockers closure остаются недоказанными; malformed control ZIP может пройти pre-browser guard с неполной metadata-моделью
    - `Fix`: требовать обязательное наличие обоих свойств как `[]` и добавить targeted tests на missing-field cases
    - `Verification`: focused tests для missing `previousVerdictChain` / missing `blockerClosureList`; затем повторить `dotnet test ... --filter ...ControlAudit...`, `dotnet run --project eng/Electron2D.Build -- verify audit-followups`, `dotnet run --project eng/Electron2D.Build -- verify docs`, `dotnet run --project eng/Electron2D.Build -- verify licenses`, `git diff --check`

EVIDENCE_REVIEW:
- Проверены все основные файлы пакета, необходимые для full file review. `metadata/repo-file-snapshots.json` согласован с `repo-file-hashes.json`: в scope 20 файлов, для всех есть `afterSnapshot`, для всех существующих baseline-файлов есть `beforeSnapshot`, orphan или missing snapshot для важных файлов реализации, тестов и документации не обнаружены.
- По коду и тестам отдельно прочитаны изменения в verifier-е follow-up closure, submit-guard для control audit, ordinary-submit target selection, CLI routing и integration tests. По документации прочитаны `AUDIT-REQUEST`, доменный документ, агентские правила и prompt цикла задачи. По прошлым отчётам прочитана вся `previousVerdictChain` и проверено, что `blockerClosureList` указывает заявленные способы закрытия.
- Проверены приложенные evidence-артефакты. Они подтверждают успешное прохождение заявленного локального коридора: focused integration tests `18/18`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check`. Реальных секретов, приватных ключей, токенов, паролей, конкретных ChatGPT conversation URL или локальных абсолютных путей машины в текущем scope не найдено.
- Scope leak в пределах `r21`, аналогичный `T-0240` из прошлого отчёта, в текущем пакете не обнаружен. Summary, manifest и фактический diff согласованно описывают область `T-0238`.

- Техническая привязка:
  - Metadata и инвентарь: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`
  - Полные итоговые snapshots changed scope:
    - `repo-after/.codex/prompts/goal-task-loop.md`
    - `repo-after/AGENTS.md`
    - `repo-after/TASKS.md`
    - `repo-after/data/documentation/electron2d-local-docs-index.json`
    - `repo-after/data/documentation/local-docs-index/documentation.ndjson`
    - `repo-after/docs/release-management/AUDIT-REQUEST.md`
    - `repo-after/docs/release-management/audit-package.md`
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r02.md`
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r04.md`
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r16.md`
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r18.md`
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r19.md`
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r20.md`
    - `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`
    - `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
    - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
    - `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`
    - `repo-after/eng/Electron2D.Build/Program.cs`
    - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - Baseline comparison: соответствующие файлы под `repo-before/`
  - Проверенные raw evidence:
    - `evidence/T-0238-r21/checks/audit-submit-followups-and-deep-research-focused-tests/*`
    - `evidence/T-0238-r21/checks/update-docs-check/*`
    - `evidence/T-0238-r21/checks/verify-docs/*`
    - `evidence/T-0238-r21/checks/verify-audit-followups/*`
    - `evidence/T-0238-r21/checks/verify-licenses/*`
    - `evidence/T-0238-r21/checks/git-diff-check/*`

RISKS_AND_NOTES:
- ACCEPTED_RISK R1
  - Человекочитаемое объяснение: В пакете по-прежнему сознательно не реализована семантическая проверка того, что `tracked-existing` или `tracked-new` в closure note указывают на действительно существующую и подходящую follow-up задачу. Эта проблема уже не раз фиксировалась как неблокирующий долг и в `r21` закрывается не кодом, а явным accepted-risk в `TASKS.md` с переносом на `T-0105`. Для текущего `r21` это не blocker: область этой итерации — закрытие замечаний `r20`, а не полная semantic inventory validation target tasks.
  - Техническая привязка:
    - `Идентификатор`: `R1`
    - `Где найдено`: `repo-after/TASKS.md:1889-1900`; historical context in `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md:60-67`, `...r02.md:58-65`, `...r04.md:71-77`, `...r16.md:91+`, `...r18.md:106+`, `...r19.md:91+`, `...r20.md:118+`
    - `Проблема`: semantic validation closure targets остаётся отдельным долгом
    - `Почему не блокирует текущую задачу`: текущий scope `r21` закрывает `r20` blockers про NEEDS_FIXES follow-up scanning, behavior-level evidence for ignored baseline targets и scope leak `T-0240`; semantic target resolver не входит в acceptance этой итерации и уже явно зафиксирован как accepted risk
    - `Rationale`: риск явно принят в tracked `TASKS.md`
    - `Next decision point`: `T-0105 risk register` / отдельная задача на semantic validation closure targets

CLOSURE_DECISION:
- Пакет `T-0238 r21` пока нельзя закрыть. Он действительно исправляет значительную часть проблем из `r20`, но оставляет незакрытую блокирующую дыру в clean-control guard: documented contract требует для `metadata.previousVerdictChain` и `metadata.blockerClosureList` пустые массивы длиной `0`, а код допускает их полное отсутствие и тем самым не доводит до конца заявленное закрытие прошлого blocker-а.
- После того как `audit submit --control-audit` начнёт отклонять missing-field cases так же строго, как non-empty arrays, и это будет покрыто focused tests, пакет можно повторно оценивать как кандидат на принятие.
