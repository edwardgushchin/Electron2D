VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0990` итерации `r08` как одиночная область. Пакет читается: metadata, manifest, snapshot index, `repo-after/`, previous verdict files r01–r07 и evidence доступны; важные after-снимки полные.
* Основная часть уже принятой в r07 модели публичного API остаётся корректной: ручной профиль пустой, generated manifest больше не берёт статусы из Wiki, generated `API-Compatibility.md` строится из manual profile, API/CLI/build-tool gates остаются fail-closed до owner approval, а r07 follow-up `F4` перенесён в `T-0994`.
* Принять r08 нельзя из-за новой части scope: после r07 в пакет добавлено hardening-изменение `audit submit` для recoverable CDP failures, но локальное доказательство этого изменения является только source-level test-ом. Текущий доменный документ прямо говорит, что такие тесты не доказывают поведение `audit submit` и восстановления, а evidence не содержит теста, который исполняет retry/recovery ветки на recoverable `Debugger unattached`/timeout failure.
* Runtime hot path движка не менялся: изменения относятся к release-management/tooling/docs/tests/generated artifacts. Реальных секретов, приватных ключей, токенов, паролей или конфиденциальных локальных путей в проверенных материалах не найдено; обнаруженные `token=<redacted>`, `password=<redacted>`, `<repo>`, `/home/user/repo` и synthetic paths находятся в тестовых fixtures, saved audit reports или sanitizer/audit regression-контексте.

Техническая привязка:

* `metadata.taskId`: `T-0990`
* `metadata.iteration`: `r08`
* `metadata.scopeTaskIds`: `["T-0990"]`
* `metadata.scopeSummary`: manual public API profile source-of-truth, r07 closure preservation, r07 follow-ups including `T-0994`, post-r07 audit-submit recoverable CDP wrapper hardening.
* `metadata.previousVerdictChain`: r01–r07 under `docs/verdicts/release-management/`
* `metadata.blockerClosureList`: r01 `B1`/`B2`, r02 `B1`/`B2`, r03 `B1`, r04 `B1`, r05 `B1`, r06 `B1`, r07 follow-ups `F1`–`F4`, post-r07 submit recovery hardening.
* Область не является `combined scope`.
* Основной контракт задачи: `repo-after/TASKS.md:4233-4296`
* Post-r07/r08 notes: `repo-after/TASKS.md:4379-4383`
* Snapshot index: `metadata/repo-file-snapshots.json`, 34 entries, all `fullContentIncluded: true`.

BLOCKERS:

* B1

  * Что не так: Новое r08-hardening для recoverable CDP wrappers в `audit submit` не имеет поведенческого доказательства. Единственный новый focused test `AuditSubmitRecoverableCdpWrappersUseFiveAttempts` читает `AuditSubmitCodexChromeCommand.cs` как текст и проверяет наличие строк `for (var attempt = 0; attempt < 5; attempt++)`, `IsRecoverableCdpFailure`, `ReattachCdpAsync` и `EnsureTargetAttachedForReadAsync`. Это не исполняет ни одну ветку с recoverable `AuditSubmitCodexChromeException`.
  * Почему это важно: Scope r08 прямо включает hardening после сбоя `download-report-only` на `Debugger unattached`. Для такого изменения важно доказать не только наличие строк в исходнике, а фактическое поведение: повтор исходной CDP-команды после recoverable failure, reattach между попытками, best-effort domain enable, прекращение после лимита попыток и отсутствие повторов для путей с `allowTransientRecovery: false`. Текущий доменный документ сам запрещает считать source-level proof доказательством поведения `audit submit`, DOM/export/download/recovery paths.
  * Что исправить: Добавить поведенческий тест через fake/injected CDP transport или выделенный внутренний retry component. Тест должен программно выбрасывать recoverable `AuditSubmitCodexChromeException` на первых попытках, проверять число retries/reattach calls, успешный повтор исходной команды, fail-closed после исчерпания лимита и отсутствие retries при `allowTransientRecovery: false`. Source-level assertions можно оставить как дополнительный guard, но они не должны быть единственным доказательством.
  * Как проверить исправление: Запустить targeted test, который реально исполняет retry/recovery ветки, затем повторить `audit-submit-reattach-stabilization`, `audit-loop-stabilization`, `build-tool-build`, `verify-audit-contracts`, `verify-audit-followups` и `git-diff-check`.
  * Проверка опровержения: Проверены production code, focused tests, audit evidence и `docs/release-management/audit-package.md`. Реализация действительно увеличивает retry loops с 3 до 5, но evidence запускает только static source assertions; других тестов, которые вызывают `AttachTargetWithRecoveryAsync`, `ExecuteCdpAsync` или `ExecuteCdpOnTargetAsync` с recoverable failure sequence, в текущем пакете не найдено. Документированного accepted risk для отсутствия поведенческого теста нет.
  * Техническая привязка:

    * `File/symbol`: `repo-after/docs/release-management/audit-package.md:55`
    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5412-5427`, `AttachTargetWithRecoveryAsync`
    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5492-5512`, `ExecuteCdpAsync`
    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5530-5560`, `ExecuteCdpOnTargetAsync`
    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5861-5896`, `ReattachCdpAsync`
    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5909-5916`, `IsRecoverableCdpFailure`
    * `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:6758-6805`
    * `Criterion`: `test coverage review`, `realistic tests`, `task compliance review`, `architecture coherence`
    * `Evidence`: r08 metadata/scope adds post-r07 CDP recovery hardening; `RepositoryBuildToolTests.cs:6788-6805` only reads source text and asserts string presence. Evidence `audit-submit-reattach-stabilization` passes 3/3 in `evidence/T-0990-r08/preflight/audit-submit-reattach-stabilization/T-0990/r08/audit-submit-reattach-stabilization/result.txt`, but the command filter contains only source-level tests for this recovery path.
    * `Impact`: пакет может быть принят без доказательства, что исправление реально устраняет class of failure, который r08 добавляет в scope.
    * `Fix`: добавить executable retry/recovery test with fake CDP failures, not only source text assertions.
    * `Verification`: targeted behavioral recovery test plus existing r08 preflight checks.

EVIDENCE_REVIEW:

* Проверены metadata, manifest, snapshot index, `repo-file-hashes.json`, patch как карта изменений, полные итоговые файлы из `repo-after/`, previous verdict files r01–r07 и raw evidence. Недоступных или неполных важных after-снимков реализации, тестов или документации не найдено.
* Проверены previous verdict files и closure map. r01–r06 blocker-ы остаются закрыты текущими файлами; r07 accepted report включён в previous chain; follow-up `F1`–`F3` продолжают ссылаться на `T-0991`, `T-0992`, `T-0993`, а r07 `F4` перенесён в `T-0994`.
* Проверена manual profile/API часть. `data/api/electron2d-public-api-profile.json` остаётся пустым hand-authored profile с `schemaVersion`, `release`, `godotBaseline`, `approvalAuthority`, `types: []`. `data/api/electron2d-api-manifest.json` указывает `generatedFrom.publicApiProfile`, не использует старый `compatibilityPage` input и показывает exported runtime public types как `unapproved`.
* Manifest generator и build-tool verifiers согласованы с profile-backed model: generator читает profile, мапит missing rows в `unapproved`, `approved` в `supported`, `deferred`/`unsupported` в исключающие статусы; `verify api-compatibility` валидирует profile и падает на exported public types без `approved`.
* Wiki/docs/UI gate часть остаётся согласованной: `API-Compatibility.md` генерируется из profile, таблица имеет форму `Type | Status | Decision | Rationale`, `verify public-api-documentation` проверяет generated shape и legacy wording, а `verify ui-public-api-gate` нормализует nested UI/Text names в dotted `Electron2D.*` форму.
* Проверены docs. `docs/documentation/api-manifest.md`, `docs/documentation/github-wiki-api-reference.md`, `docs/release-management/api-compatibility.md` и `docs/releases/0.1-preview.md` описывают новый manual profile ownership contract. `docs/release-management/audit-package.md` также содержит важное правило, что source-level proof не доказывает поведение `audit submit`; именно с ним конфликтует r08 submit-hardening evidence.
* Проверены tests/evidence. `focused-profile-tests` прошёл 33/33, `audit-loop-stabilization` прошёл 12/12, `audit-submit-reattach-stabilization` прошёл 3/3, build/docs/CI/license/audit checks прошли. Intentional empty-profile checks завершились ожидаемым кодом `1` с diagnostic `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT` для `Electron2D.AnimatedSprite2D`.
* Проверена область и лишние правки. Изменения `AuditSubmitCodexChromeCommand.cs`, `AuditPackageCommand.cs` и связанные tests/docs notes объявлены в r08 scope summary и TASKS notes, поэтому не являются скрытыми изменениями. Но audit-submit hardening остаётся недоказанным поведенчески, что оформлено как B1.
* Проверены секреты и локальные данные по `repo-after/`, patch, metadata и evidence. Реальных секретов не найдено; redacted placeholders и synthetic local paths находятся в предыдущих verdict-файлах, тестах и sanitizer fixtures.

Техническая привязка:

* Metadata/manifest: `metadata/audit-package.input.json`, `AUDIT-MANIFEST.md:3-10`, `AUDIT-MANIFEST.md:210-232`, `AUDIT-MANIFEST.md:240-254`
* Snapshot index: `metadata/repo-file-snapshots.json`
* Scope inventory: `repo-file-hashes.json`
* Task contract and r08 notes: `repo-after/TASKS.md:4233-4296`, `repo-after/TASKS.md:4379-4383`
* Follow-up tasks: `repo-after/TASKS.md:4385-4493`, `repo-after/TASKS.md:4499-4607`, `repo-after/TASKS.md:4613-4668`, `repo-after/TASKS.md:4719-4797`
* Manual profile: `repo-after/data/api/electron2d-public-api-profile.json:1-7`
* Generated manifest: `repo-after/data/api/electron2d-api-manifest.json:1-28`
* Manifest generator: `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:33-67`, `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:155-181`, `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:968-1030`
* Profile reader/gates: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:591-716`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1038-1124`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1719-1762`
* Wiki generation/checking: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2259-2271`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2475-2496`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2499-2516`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2660-2678`
* UI/public docs verifier: `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3472-3542`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3544-3624`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3658-3704`
* Audit submit implementation: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5412-5427`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5492-5512`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5530-5560`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5861-5916`
* Tests: `repo-after/tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `repo-after/tests/Electron2D.Tests.Integration/Electron2DCliWorkflowTests.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1754-1819`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1875-1905`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1956-1971`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:2735-2764`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:6758-6805`
* Evidence:

  * `evidence/T-0990-r08/preflight/focused-profile-tests/T-0990/r08/focused-profile-tests/result.txt`
  * `evidence/T-0990-r08/preflight/focused-profile-tests/T-0990/r08/focused-profile-tests/output.txt`
  * `evidence/T-0990-r08/preflight/audit-loop-stabilization/T-0990/r08/audit-loop-stabilization/result.txt`
  * `evidence/T-0990-r08/preflight/audit-loop-stabilization/T-0990/r08/audit-loop-stabilization/output.txt`
  * `evidence/T-0990-r08/preflight/audit-submit-reattach-stabilization/T-0990/r08/audit-submit-reattach-stabilization/result.txt`
  * `evidence/T-0990-r08/preflight/audit-submit-reattach-stabilization/T-0990/r08/audit-submit-reattach-stabilization/output.txt`
  * `evidence/T-0990-r08/preflight/build-tool-build/T-0990/r08/build-tool-build/result.txt`
  * `evidence/T-0990-r08/preflight/update-docs-check/T-0990/r08/update-docs-check/result.txt`
  * `evidence/T-0990-r08/preflight/verify-docs/T-0990/r08/verify-docs/result.txt`
  * `evidence/T-0990-r08/preflight/verify-ci-matrix/T-0990/r08/verify-ci-matrix/result.txt`
  * `evidence/T-0990-r08/preflight/verify-licenses/T-0990/r08/verify-licenses/result.txt`
  * `evidence/T-0990-r08/preflight/verify-audit-contracts/T-0990/r08/verify-audit-contracts/result.txt`
  * `evidence/T-0990-r08/preflight/verify-audit-followups/T-0990/r08/verify-audit-followups/result.txt`
  * `evidence/T-0990-r08/preflight/update-api-manifest-empty-profile-fail/T-0990/r08/update-api-manifest-empty-profile-fail/output.txt`
  * `evidence/T-0990-r08/preflight/update-api-manifest-empty-profile-fail/T-0990/r08/update-api-manifest-empty-profile-fail/result.txt`
  * `evidence/T-0990-r08/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r08/verify-api-compatibility-empty-profile-fail/output.txt`
  * `evidence/T-0990-r08/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r08/verify-api-compatibility-empty-profile-fail/result.txt`

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `AUDIT-MANIFEST.md:247`, `AUDIT-MANIFEST.md:249`; raw evidence result files для intentional failure checks.
  * Проблема: В manifest для `update-api-manifest-empty-profile-fail` и `verify-api-compatibility-empty-profile-fail` указан expected exit code `0`, хотя corresponding `result.txt` фиксируют `expectedExitCode=1`, `actualExitCode=1`, `status=PASS`.
  * Почему не блокирует текущую задачу: Raw evidence корректно показывает expected-failure behavior, а долг уже перенесён в `T-0991` и проверен `verify audit-followups`. Это дефект качества audit manifest, не причина текущего отказа.
  * Куда перенести: Suggested existing task — `T-0991: Синхронизировать expected exit codes в audit manifest с evidence summaries`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Сгенерировать audit package с expected-success и expected-failure checks и автоматически сравнить expected exit codes в `AUDIT-MANIFEST.md` с `evidence/**/result.txt`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `AUDIT-MANIFEST.md:247`, `AUDIT-MANIFEST.md:249`; `repo-after/TASKS.md:4385-4493`
    * `Why not blocker for current task`: raw evidence files are correct; follow-up is tracked.

* FOLLOW_UP_FINDING F2

  * Идентификатор: `F2`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1719-1762`, `VerifyGeneratedManifestProfileGate`.
  * Проблема: `update api-manifest --check` может вывести unapproved export diagnostic до накопленных profile validation diagnostics, если profile одновременно невалиден и runtime экспортирует public types.
  * Почему не блокирует текущую задачу: Обязательный fail-closed path доказан, а основной `verify api-compatibility` печатает profile validation errors до exported-type gate. Долг касается полноты диагностического вывода и уже перенесён в `T-0992`.
  * Куда перенести: Suggested existing task — `T-0992: Приоритизировать diagnostics manual API profile в update api-manifest`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Добавить fixture с invalid manual profile и exported runtime public type; проверить, что `update api-manifest --check` печатает profile validation diagnostic до или вместе с unapproved export diagnostic.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:1719-1762`; `repo-after/TASKS.md:4499-4607`
    * `Why not blocker for current task`: required empty-profile fail-fast behavior remains proven.

* FOLLOW_UP_FINDING F3

  * Идентификатор: `F3`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:663-667`; `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1887-1905`.
  * Проблема: Duplicate `fullName` validation реализована, но focused invalid-profile tests всё ещё не содержат отдельного regression-case для diagnostic `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`.
  * Почему не блокирует текущую задачу: Production code отклоняет duplicate rows через `entries.TryAdd`; недостаёт только explicit regression coverage. Долг уже вынесен в `T-0993`.
  * Куда перенести: Suggested existing task — `T-0993: Добавить regression для duplicate fullName в manual API profile`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Добавить targeted test, где profile с двумя строками одного `fullName` падает с `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:663-667`, `RepositoryBuildToolTests.cs:1887-1905`; `repo-after/TASKS.md:4613-4668`
    * `Why not blocker for current task`: duplicate rejection is implemented and tracked for explicit regression coverage.

* FOLLOW_UP_FINDING F4

  * Идентификатор: `F4`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1861-1885`, `ApiManifestCommand.Parse`.
  * Проблема: `update api-manifest` всё ещё принимает устаревший аргумент `--wiki-path`, хотя Wiki больше не является input для manifest status.
  * Почему не блокирует текущую задачу: Генератор фактически запускается с `--profile`, `--assembly`, `--xml` и `--output`; Wiki не читается как source-of-truth. Долг уже перенесён в `T-0994`.
  * Куда перенести: Suggested existing task — `T-0994: Удалить устаревший --wiki-path из update api-manifest`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Добавить CLI regression для `update api-manifest --wiki-path .github/wiki --check`, который проверяет выбранное поведение: stable rejection diagnostic или явно документированный no-op без чтения Wiki.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:1861-1885`; `repo-after/TASKS.md:4719-4797`
    * `Why not blocker for current task`: stale argument is not used as source input and is tracked as CLI cleanup.

CLOSURE_DECISION:

* Задача `T-0990` остаётся открытой для r08. API-profile часть и закрытие r01–r07 замечаний в основном подтверждены, но новый post-r07 submit recovery hardening включён в scope без реалистичного поведенческого теста.
* Для следующей итерации нужен пакет с executable recovery test for recoverable CDP failures, повторным `audit-submit-reattach-stabilization`, `audit-loop-stabilization`, build/docs/audit checks и теми же intentional empty-profile failure diagnostics.
