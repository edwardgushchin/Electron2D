VERDICT: ACCEPT

TASK_ASSESSMENT:

* Проверен пакет `T-0990` итерации `r09` как одиночная область. Изменение можно принять: ручной пустой профиль публичного API остаётся единственным источником решений о включении API, generated manifest больше не берёт статусы из Wiki, generated `API-Compatibility.md` строится из `data/api/electron2d-public-api-profile.json`, а API/CLI/build-tool gates остаются fail-closed до owner approval.
* Закрытие прошлых blocker-ов r01–r08 подтверждено по текущим полным файлам, тестам, документации и evidence. Последний blocker r08 по недостаточному доказательству CDP recovery закрыт: source-only assertions заменены производственным `AuditSubmitCdpRecoveryPolicy`, которым пользуются wrappers `audit submit`, и behavioral tests реально исполняют retry, fail-closed после лимита и no-retry ветки.
* Runtime hot path движка не менялся: область относится к release-management/tooling/docs/tests/generated artifacts. Реальных секретов, приватных ключей, токенов, паролей или конфиденциальных локальных путей в проверенных материалах не найдено; обнаруженные redacted placeholders и synthetic paths находятся в тестах, saved verdict-файлах или sanitizer/audit regression-контексте.

Техническая привязка:

* `metadata.taskId`: `T-0990`
* `metadata.iteration`: `r09`
* `metadata.scopeTaskIds`: `["T-0990"]`
* `metadata.scopeSummary`: manual public API profile source-of-truth; r07 ACCEPT closure preservation; r08 follow-ups in `TASKS.md`; r08 `B1` closure through executable `AuditSubmitCdpRecoveryPolicy`; package-local scanner closure for saved r08 placeholder line.
* `metadata.previousVerdictChain`: r01–r08 under `docs/verdicts/release-management/`
* `metadata.blockerClosureList`: r01 `B1`/`B2`, r02 `B1`/`B2`, r03 `B1`, r04 `B1`, r05 `B1`, r06 `B1`, r07 follow-ups `F1`–`F4`, r08 `B1`, r08 follow-ups, package-local r09 scanner closure.
* Область не является `combined scope`.
* Основной контракт задачи: `repo-after/TASKS.md:4233-4296`; r08/r09 notes: `repo-after/TASKS.md:4385-4389`
* Snapshot index: `metadata/repo-file-snapshots.json`, 35 entries, все `fullContentIncluded: true`.

BLOCKERS:

* No blockers found.

EVIDENCE_REVIEW:

* Прочитаны metadata, manifest, snapshot index, `repo-file-hashes.json`, patch как карта изменений, полные итоговые файлы `repo-after/`, previous verdict files r01–r08 и raw evidence. Недоступных или неполных важных after-снимков реализации, тестов или документации не найдено.
* Проверены previous verdict files и closure map. Блокеры r01–r06 остаются закрыты текущими файлами. r07 был принят; его follow-up findings перенесены в `T-0991`–`T-0994`. r08 blocker по source-only CDP recovery proof закрыт production component + behavioral tests. Package-local r09 scanner closure также подтверждён exact previous-verdict-only allowlist и negative scanner coverage.
* Проверена manual profile/API часть. `data/api/electron2d-public-api-profile.json` остаётся пустым hand-authored profile с `schemaVersion`, `release`, `godotBaseline`, `approvalAuthority` и `types: []`. `data/api/electron2d-api-manifest.json` указывает `generatedFrom.publicApiProfile`, не использует старый `compatibilityPage` input и показывает 175 exported runtime public types как `unapproved`.
* Проверен manifest generator. Он читает manual profile, мапит отсутствующую строку в `unapproved`, `approved` в `supported`, `deferred`/`unsupported` в исключающие статусы, а `outOfProfile` остаётся true для всего, что не `supported`.
* Проверен build-tool/verifier path. `ManualApiProfileReader` валидирует обязательные поля строк профиля, `decision`, `rationale`, `godotReference` для всех решений и duplicate `fullName`. `verify api-compatibility` сначала валидирует profile, затем падает на exported public type без `approved`.
* Проверены Wiki/docs/UI gates. `API-Compatibility.md` генерируется из manual profile, содержит generated marker и таблицу `Type | Status | Decision | Rationale`. `verify public-api-documentation` проверяет generated shape и legacy wording. `verify ui-public-api-gate` нормализует nested UI/Text names в dotted `Electron2D.*` форму, поэтому ранее найденный r06 blocker по `TextureRect.StretchMode` закрыт.
* Проверена r09 audit-submit recovery реализация. `AttachTargetWithRecoveryAsync`, `ExecuteCdpAsync` и `ExecuteCdpOnTargetAsync` используют `AuditSubmitCdpRecoveryPolicy.ExecuteAsync`. Политика исполняет recoverable retry до лимита, reattach/recover между попытками, fail-closed после исчерпания попыток и single-dispatch path для `allowTransientRecovery: false`.
* Проверены r09 behavioral tests. `AuditSubmitCdpRecoveryPolicyRetriesRecoverableFailuresAndReattachesBetweenAttempts` реально моделирует recoverable failures и проверяет порядок `execute`/`recover` вызовов. `AuditSubmitCdpRecoveryPolicyFailsClosedAfterAttemptLimit` проверяет fail-closed после лимита. `AuditSubmitCdpRecoveryPolicyDoesNotRetryNonRetriedActions` проверяет, что non-retried action не повторяется после recoverable failure. Это закрывает r08 `B1`, где source-level proof был недостаточным.
* Проверены evidence-команды. `focused-profile-tests` прошёл 33/33, `audit-loop-stabilization` прошёл 16/16, `audit-submit-reattach-stabilization` прошёл 7/7. Build/docs/CI/license/audit checks прошли. Intentional empty-profile checks завершились ожидаемым кодом `1` с diagnostic `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT` для `Electron2D.AnimatedSprite2D`.
* Проверена область и лишние правки. Изменения находятся в release-management/tooling/docs/tests/generated artifacts и соответствуют r09 scope summary. Runtime public API implementation не менялся.
* Проверены секреты и локальные данные по `repo-after/`, patch, metadata и evidence. Реальных секретов не найдено. `token=<redacted>`, `password=<redacted>`, `<repo>`, `/home/user/repo` и synthetic paths находятся только в previous verdict prose, тестовых fixtures или sanitizer/audit regression-контексте.

Техническая привязка:

* Metadata/manifest: `metadata/audit-package.input.json`, `AUDIT-MANIFEST.md:3-10`, `AUDIT-MANIFEST.md:210-232`, `AUDIT-MANIFEST.md:240-258`
* Snapshot index: `metadata/repo-file-snapshots.json`
* Scope inventory: `repo-file-hashes.json`
* Task contract and r09 notes: `repo-after/TASKS.md:4233-4296`, `repo-after/TASKS.md:4385-4389`
* Follow-up tasks: `repo-after/TASKS.md:4391-4510`, `repo-after/TASKS.md:4511-4625`, `repo-after/TASKS.md:4631-4737`, `repo-after/TASKS.md:4743-4823`
* Previous verdicts: `repo-after/docs/verdicts/release-management/t-0990-audit-r01.md` through `repo-after/docs/verdicts/release-management/t-0990-audit-r08.md`
* Manual profile: `repo-after/data/api/electron2d-public-api-profile.json:1-7`
* Generated manifest: `repo-after/data/api/electron2d-api-manifest.json:1-28`
* Manifest generator: `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:33-67`, `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:155-181`, `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:968-1030`
* Profile reader/gates: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:591-716`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1038-1124`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1211-1233`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1719-1762`
* Wiki generation/checking: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2259-2271`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2475-2496`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2499-2516`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2660-2678`
* UI/public docs verifier: `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3472-3542`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3544-3624`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3658-3704`
* Audit submit recovery implementation: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5412-5425`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5497-5503`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5530-5549`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5850-5885`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5898-5905`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:6204-6238`
* Tests: `repo-after/tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `repo-after/tests/Electron2D.Tests.Integration/Electron2DCliWorkflowTests.cs:423-490`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1754-1819`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1875-1905`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1956-1971`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:2735-2764`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:6758-6878`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:14657-14709`
* Documentation: `repo-after/docs/documentation/api-manifest.md:22-79`, `repo-after/docs/documentation/api-manifest.md:119-153`, `repo-after/docs/documentation/github-wiki-api-reference.md:50-58`, `repo-after/docs/documentation/github-wiki-api-reference.md:138-168`, `repo-after/docs/release-management/api-compatibility.md:21-31`, `repo-after/docs/release-management/api-compatibility.md:149-155`, `repo-after/docs/release-management/api-compatibility.md:224-229`, `repo-after/docs/release-management/audit-package.md:55`, `repo-after/docs/releases/0.1-preview.md:153-182`, `repo-after/docs/releases/0.1-preview.md:1128-1135`
* Evidence:

  * `evidence/T-0990-r09/preflight/focused-profile-tests/T-0990/r09/focused-profile-tests/result.txt`
  * `evidence/T-0990-r09/preflight/focused-profile-tests/T-0990/r09/focused-profile-tests/output.txt`
  * `evidence/T-0990-r09/preflight/audit-loop-stabilization/T-0990/r09/audit-loop-stabilization/result.txt`
  * `evidence/T-0990-r09/preflight/audit-loop-stabilization/T-0990/r09/audit-loop-stabilization/output.txt`
  * `evidence/T-0990-r09/preflight/audit-submit-reattach-stabilization/T-0990/r09/audit-submit-reattach-stabilization/result.txt`
  * `evidence/T-0990-r09/preflight/audit-submit-reattach-stabilization/T-0990/r09/audit-submit-reattach-stabilization/output.txt`
  * `evidence/T-0990-r09/preflight/build-tool-build/T-0990/r09/build-tool-build/result.txt`
  * `evidence/T-0990-r09/preflight/update-docs-check/T-0990/r09/update-docs-check/result.txt`
  * `evidence/T-0990-r09/preflight/verify-docs/T-0990/r09/verify-docs/result.txt`
  * `evidence/T-0990-r09/preflight/verify-ci-matrix/T-0990/r09/verify-ci-matrix/result.txt`
  * `evidence/T-0990-r09/preflight/verify-licenses/T-0990/r09/verify-licenses/result.txt`
  * `evidence/T-0990-r09/preflight/verify-audit-contracts/T-0990/r09/verify-audit-contracts/result.txt`
  * `evidence/T-0990-r09/preflight/verify-audit-followups/T-0990/r09/verify-audit-followups/result.txt`
  * `evidence/T-0990-r09/preflight/previous-verdict-placeholder-scanner/T-0990/r09/previous-verdict-placeholder-scanner/result.txt`
  * `evidence/T-0990-r09/preflight/git-diff-check/T-0990/r09/git-diff-check/result.txt`
  * `evidence/T-0990-r09/preflight/update-api-manifest-empty-profile-fail/T-0990/r09/update-api-manifest-empty-profile-fail/output.txt`
  * `evidence/T-0990-r09/preflight/update-api-manifest-empty-profile-fail/T-0990/r09/update-api-manifest-empty-profile-fail/result.txt`
  * `evidence/T-0990-r09/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r09/verify-api-compatibility-empty-profile-fail/output.txt`
  * `evidence/T-0990-r09/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r09/verify-api-compatibility-empty-profile-fail/result.txt`

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `AUDIT-MANIFEST.md:255`, `AUDIT-MANIFEST.md:257`; raw evidence result files для intentional failure checks.
  * Проблема: В manifest для `update-api-manifest-empty-profile-fail` и `verify-api-compatibility-empty-profile-fail` указан expected exit code `0`, хотя соответствующие `result.txt` фиксируют `expectedExitCode=1`, `actualExitCode=1`, `status=PASS`.
  * Почему не блокирует текущую задачу: Raw evidence корректно показывает expected-failure behavior, а долг уже перенесён в `T-0991` и проверен `verify audit-followups`. Это дефект качества audit manifest, а не реализации ручного API profile или audit-submit recovery.
  * Куда перенести: Suggested existing task — `T-0991: Синхронизировать expected exit codes в audit manifest с evidence summaries`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Закрывающая задача должна сгенерировать audit package с expected-success и expected-failure checks и автоматически сравнить expected exit codes в `AUDIT-MANIFEST.md` с `evidence/**/result.txt`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `AUDIT-MANIFEST.md:255`, `AUDIT-MANIFEST.md:257`; `repo-after/TASKS.md:4391-4510`
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
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:1719-1762`; `repo-after/TASKS.md:4511-4625`
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
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:663-667`, `RepositoryBuildToolTests.cs:1887-1905`; `repo-after/TASKS.md:4631-4737`
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
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:1861-1885`; `repo-after/TASKS.md:4743-4823`
    * `Why not blocker for current task`: stale argument is not used as source input and is tracked as CLI cleanup.

* FOLLOW_UP_FINDING F5

  * Идентификатор: `F5`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:600-627`; `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:975-985`; `repo-after/docs/documentation/api-manifest.md:46-57`.
  * Проблема: Документация и текущий профиль используют top-level metadata `schemaVersion`, `release`, `godotBaseline`, `approvalAuthority`, но reader/verifier path в основном проверяет наличие root object и массива `types`, а не валидирует эти top-level поля как часть схемы ручного профиля.
  * Почему не блокирует текущую задачу: Текущий профиль содержит нужные top-level поля, а обязательное поведение задачи — пустой manual profile, row validation, generated artifacts и fail-closed gates — доказано кодом, тестами и evidence. Это hardening на случай будущей порчи metadata профиля, а не нарушение текущей принятой модели.
  * Куда перенести: Suggested new task — «Валидировать top-level metadata ручного public API profile». Приоритет P3. Домен: build tooling/API profile. Критерий приёмки: `verify api-compatibility` и manifest generator отклоняют profile с отсутствующим или неверным `schemaVersion`, `release`, `godotBaseline` или `approvalAuthority` стабильными diagnostics. Идея проверки: добавить invalid-profile fixtures и targeted integration tests для missing/wrong top-level fields.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Добавить tests в `RepositoryBuildToolTests` или отдельный profile-schema test set, где top-level поля отсутствуют или имеют неверные значения, и подтвердить stable failure diagnostics.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `ManualApiProfileReader.Read`, `ManualApiProfile.Load`, `docs/documentation/api-manifest.md`
    * `Why not blocker for current task`: current profile metadata is present and current fail-closed source-of-truth behavior is proven.

CLOSURE_DECISION:

* Пакет `T-0990` r09 можно закрыть. Текущая задача достигла цели: ручной пустой профиль является source-of-truth для API inclusion decisions, generated manifest/Wiki/docs/tooling работают по profile-backed модели, gates fail-closed до owner approval, и все прошлые blockers r01–r08 закрыты проверяемыми изменениями.
* Оставшиеся замечания являются P3 hardening/test/CLI/audit-package debt. Они не делают приёмку `T-0990` небезопасной и уже частично вынесены в отдельные задачи `T-0991`–`T-0994`; новый пункт F5 можно закрыть отдельной schema-hardening задачей.
