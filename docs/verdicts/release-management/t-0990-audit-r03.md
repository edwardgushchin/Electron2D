VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0990` итерации `r03` как одиночная область. Область в metadata и manifest согласована: задача вводит ручной пустой профиль публичного API как единственный источник решений о включении API, а generated manifest/Wiki/CLI/build-tool checks должны падать закрыто до owner approval.
* Закрытие прошлых проблем в основном подтверждено: stale integration-тест из r01 обновлён на empty-profile модель, generated Wiki/API Compatibility и public-api-documentation verifier переведены на новую модель, а `godotReference` теперь проверяется для `approved`, `deferred` и `unsupported`.
* Принять изменение нельзя из-за оставшейся противоречивой документации. Центральный документ `docs/documentation/api-manifest.md` всё ещё описывает проверку `godotReference` как обязательную только для `approved`, хотя текущий код и тесты уже валидируют её для всех строк `types[]`. Для задачи, где ручной профиль становится источником истины, такой рассинхрон документации с реальным contract является блокирующим.
* Runtime hot path не менялся: проверяемая область относится к release-management/tooling/docs/tests/generated artifacts. Реальных секретов, приватных ключей, токенов, паролей, локальных абсолютных путей или конфиденциальных данных в проверенных материалах не найдено; `token=<redacted>` и `password=<redacted>` находятся в тестовой/аудиторской фикстуре.

Техническая привязка:

* `metadata.taskId`: `T-0990`
* `metadata.iteration`: `r03`
* `metadata.scopeTaskIds`: `["T-0990"]`
* `metadata.scopeSummary`: ручной пустой public API profile как единственный source-of-truth для API inclusion decisions, fail-closed до owner approval.
* `metadata.previousVerdictChain`: `["docs/verdicts/release-management/t-0990-audit-r01.md", "docs/verdicts/release-management/t-0990-audit-r02.md"]`
* `metadata.blockerClosureList`: заявлены закрытия r01 `B1`/`B2` и r02 `B1`/`B2`.
* Область не является `combined scope`.
* `metadata/repo-file-snapshots.json`: 26 файлов, проверенные after-снимки полные; blocker-а по `evidence gap` нет.

BLOCKERS:

* B1

  * Что не так: Документация профиля публичного API противоречит текущему поведению verifier-а. В `docs/documentation/api-manifest.md` написано, что verifier должен падать на отсутствующий или несуществующий Godot 4.7 class packet только для `approved`. В r03 код уже проверяет `godotReference` для каждой строки профиля, включая `deferred` и `unsupported`, а тесты это подтверждают.
  * Почему это важно: `T-0990` делает ручной профиль единственным источником решений о публичной API-поверхности. Для такого contract документация должна точно описывать, какие строки профиля считаются валидными. Текущее описание создаёт ложное правило: владелец или агент может решить, что `deferred`/`unsupported` допускают непроверяемый `godotReference`, хотя инструмент это отвергнет. Это также оставляет неполным закрытие проблемы r02 по проверке `godotReference` для всех owner decisions.
  * Что исправить: Обновить `docs/documentation/api-manifest.md`, чтобы там было явно сказано, что `godotReference` валидируется для всех строк `types[]` и всех решений `approved`/`deferred`/`unsupported`. Желательно добавить проверку документации или тест, который не позволит вернуть формулировку «только для approved».
  * Как проверить исправление: Повторить focused profile tests и documentation checks. Минимальная проверка должна включать `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~VerifyApiCompatibilityRejectsInvalidManualProfileRows|FullyQualifiedName~VerifyPublicApiDocumentationAcceptsGeneratedCompatibilityPage" --no-restore -v:minimal`, `dotnet run --project eng/Electron2D.Build -- verify docs` и проверку, что `docs/documentation/api-manifest.md` больше не ограничивает `godotReference` validation только `approved`.
  * Проверка опровержения: Проверены implementation code, tests, task contract, previous r02 report и current docs. Код и тесты действительно исправляют поведение verifier-а, но это не снимает blocker: центральная документация API manifest остаётся неточной, а evidence не содержит проверки этой конкретной смысловой формулировки.
  * Техническая привязка:

    * `File/symbol`: `repo-after/docs/documentation/api-manifest.md:133-139`
    * `Criterion`: `documentation review`, `task compliance review`, `previous blockers closure`, `Public API source-of-truth contract`
    * `Evidence`: `repo-after/docs/documentation/api-manifest.md:138` говорит про несуществующий Godot class packet «для `approved`»; при этом `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:638-667` читает все `types[]` rows и `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:658-661` вызывает проверку Godot packet для строки профиля без ограничения на `approved`; `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1835-1854` содержит cases для `approved`, `deferred` и `unsupported`.
    * `Impact`: документация source-of-truth профиля публичного API не соответствует фактическому verifier contract, поэтому текущую задачу нельзя считать полностью закрытой.
    * `Fix`: синхронизировать `api-manifest.md` с implemented verifier behavior и добавить regression check для этой формулировки.
    * `Verification`: focused profile tests, `verify docs`, semantic check документации на all-decisions `godotReference` validation.

EVIDENCE_REVIEW:

* Проверены metadata и область пакета. `metadata/audit-package.input.json` задаёт `taskId=T-0990`, `iteration=r03`, одиночный `scopeTaskIds=["T-0990"]`, previous verdict chain для r01/r02 и closure list по четырём прошлым blocker-ам. `AUDIT-MANIFEST.md` описывает ту же область.
* Проверены прошлые verdict files. Файлы r01 и r02 доступны в `repo-after/docs/verdicts/release-management/`. Прошлый r01 `B1` закрыт изменённым integration-тестом и tracked manifest. Прошлый r01/r02 documentation/Wiki blocker в части generated Wiki/public-api-documentation verifier закрыт. Прошлый r02 `B2` в части реализации и тестов закрыт, но документация всё ещё содержит неточное ограничение, оформленное выше как текущий blocker.
* Проверены полные итоговые файлы из `repo-after/`; patch использовался только как карта изменений. Снимки важных файлов реализации, тестов и документации доступны целиком.
* Проверена реализация ручного профиля. `data/api/electron2d-public-api-profile.json` остаётся пустым manual profile с `schemaVersion`, `release`, `godotBaseline`, `approvalAuthority` и `types: []`. `data/api/electron2d-api-manifest.json` ссылается на `publicApiProfile`, не использует старую `compatibilityPage`, и показывает `unapproved: 175`.
* Проверен manifest generator. Он читает manual profile, мапит отсутствующую строку в `unapproved`, `approved` в `supported`, `deferred`/`unsupported` в соответствующие исключающие статусы, а `outOfProfile` остаётся true для всего, что не `supported`.
* Проверен build-tool/verifier path. `verify api-compatibility` валидирует profile, проверяет generated manifest shape и падает на exported public type без `approved`. `update wiki` генерирует `API-Compatibility.md` с generated marker, строкой `Generated from data/api/electron2d-public-api-profile.json`, статусами `Supported`/`Deferred`/`Unsupported`/`Unapproved` и текущей runtime-surface секцией.
* Проверены tests. Focused evidence запускает 28 тестов и проходит. Тесты покрывают empty-profile manifest, generated compatibility page, public API documentation verifier, invalid profile rows для `approved`/`deferred`/`unsupported`, exported public type fail-fast, CLI compare-godot approved/deferred behavior и local docs/update checks.
* Проверены evidence-команды. Успешно прошли `build-tool-build`, `focused-profile-tests`, `update-docs-check`, `verify-docs`, `verify-ci-matrix`, `verify-licenses`, `verify-audit-contracts`, `git-diff-check`, `previous-verdict-placeholder-scanner`. Intentional failure checks для пустого профиля завершились ожидаемым кодом `1` и diagnostic `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT` для `Electron2D.AnimatedSprite2D`.
* Проверена область и лишние правки. Изменения находятся в release-management/tooling/docs/tests/generated artifacts и связаны с заявленной задачей. Runtime implementation public API не менялся.
* Проверены секреты и локальные данные по `repo-after/`, patch, metadata и evidence. Реальных секретов не найдено; `<repo>` в evidence является redacted path marker.

Техническая привязка:

* Metadata/manifest: `metadata/audit-package.input.json`, `AUDIT-MANIFEST.md`
* Snapshot index: `metadata/repo-file-snapshots.json`
* Scope inventory: `repo-file-hashes.json`
* Task contract: `repo-after/TASKS.md:4233-4321`
* Previous verdicts: `repo-after/docs/verdicts/release-management/t-0990-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0990-audit-r02.md`
* Manual profile: `repo-after/data/api/electron2d-public-api-profile.json:1-7`
* Generated manifest summary/profile source: `repo-after/data/api/electron2d-api-manifest.json:1-28`
* Example unapproved manifest rows: `repo-after/data/api/electron2d-api-manifest.json` entries for `Electron2D.AnimatedSprite2D` and `Electron2D.Control`
* Manifest generator: `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:40-67`, `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:155-181`
* Profile reader and gate: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:591-716`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1031-1118`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1204-1226`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1712-1755`
* Wiki generation/checking: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1985-2023`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2269-2311`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2468-2489`
* Public documentation verifier: `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3536-3616`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3650-3695`
* Tests: `repo-after/tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `repo-after/tests/Electron2D.Tests.Integration/Electron2DCliWorkflowTests.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* Evidence:

  * `evidence/T-0990-r03/preflight/focused-profile-tests/command.txt`
  * `evidence/T-0990-r03/preflight/focused-profile-tests/T-0990/r03/focused-profile-tests/stdout.txt`
  * `evidence/T-0990-r03/preflight/focused-profile-tests/T-0990/r03/focused-profile-tests/summary.txt`
  * `evidence/T-0990-r03/preflight/update-api-manifest-empty-profile-fail/T-0990/r03/update-api-manifest-empty-profile-fail/stdout.txt`
  * `evidence/T-0990-r03/preflight/update-api-manifest-empty-profile-fail/T-0990/r03/update-api-manifest-empty-profile-fail/summary.txt`
  * `evidence/T-0990-r03/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r03/verify-api-compatibility-empty-profile-fail/stdout.txt`
  * `evidence/T-0990-r03/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r03/verify-api-compatibility-empty-profile-fail/summary.txt`
  * остальные `summary.txt`/`stdout.txt`/`stderr.txt`/`exit-code.txt` файлы в `evidence/T-0990-r03/preflight/`

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `AUDIT-MANIFEST.md`, раздел preflight checks для intentional failure checks.
  * Проблема: В manifest для `update-api-manifest-empty-profile-fail` и `verify-api-compatibility-empty-profile-fail` указан expected exit code `0`, хотя соответствующие raw evidence summaries корректно фиксируют `expectedExitCode=1`, `actualExitCode=1`, `passed=true`.
  * Почему не блокирует текущую задачу: Raw evidence достаточно для проверки поведения: эти проверки действительно являются ожидаемыми отказами и проходят именно как expected-failure checks. Ошибка в manifest ухудшает качество audit package, но не меняет вывод по реализации.
  * Куда перенести: Suggested new task — «Синхронизировать expected exit codes в `AUDIT-MANIFEST.md` с preflight evidence summaries». Приоритет P3. Домен: audit packaging/release-management. Критерий приёмки: каждая строка preflight check в manifest показывает тот же expected exit code, что и соответствующий `summary.txt`. Идея проверки: автотест audit package builder-а сравнивает manifest checks с evidence summaries и падает на рассинхроне.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Сгенерировать audit package с одной expected-success и одной expected-failure проверкой; автоматической проверкой сравнить `AUDIT-MANIFEST.md` и `evidence/**/summary.txt`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `AUDIT-MANIFEST.md`, entries `update-api-manifest-empty-profile-fail`, `verify-api-compatibility-empty-profile-fail`
    * `Why not blocker for current task`: raw evidence summaries are correct and sufficient for this audit.

* FOLLOW_UP_FINDING F2

  * Идентификатор: `F2`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1712-1755`, `VerifyGeneratedManifestProfileGate`.
  * Проблема: `update api-manifest --check` читает manual profile и накапливает validation errors, но затем сначала проходит по manifest types и может вернуть `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT`. Если profile одновременно невалиден и runtime экспортирует public types, profile validation diagnostics могут не попасть в вывод этой команды.
  * Почему не блокирует текущую задачу: Основной verifier `verify api-compatibility` печатает profile validation errors до exported-type gate, а intentional empty-profile failure для `update api-manifest --check` доказан evidence. Это диагностический долг, а не нарушение обязательного fail-closed behavior.
  * Куда перенести: Suggested new task — «Сделать диагностику manual API profile в `update api-manifest` приоритетной перед exported-type gate». Приоритет P3. Домен: build tooling/API manifest. Критерий приёмки: при невалидном `data/api/electron2d-public-api-profile.json` команда `update api-manifest --check` печатает profile validation diagnostics до unapproved export diagnostic или вместе с ним. Идея проверки: fixture с invalid profile и exported type, targeted integration test для `update api-manifest --check`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Добавить тест в `RepositoryBuildToolTests.cs`, который портит manual profile и ожидает соответствующий `E2D-BUILD-API-PROFILE-*` diagnostic от `update api-manifest --check`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryPolicyVerifiers.cs`, `VerifyGeneratedManifestProfileGate`
    * `Why not blocker for current task`: обязательный empty-profile fail-fast path доказан; проблема касается полноты диагностического вывода.

* FOLLOW_UP_FINDING F3

  * Идентификатор: `F3`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:663-667`; `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1835-1854`.
  * Проблема: Duplicate `fullName` validation реализована в production code, но focused invalid-profile tests не содержат явного regression-case для diagnostic `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`.
  * Почему не блокирует текущую задачу: Само поведение duplicate rejection видно в коде через `entries.TryAdd` и отдельный diagnostic. Текущие blocker-ы не связаны с отсутствием этой проверки в реализации; это недостающее regression-покрытие.
  * Куда перенести: Suggested new task — «Добавить focused regression для duplicate `fullName` в manual API profile verifier». Приоритет P3. Домен: build tooling/API compatibility tests. Критерий приёмки: `verify api-compatibility --wiki-path .github/wiki` на fixture с двумя строками одного `fullName` падает с `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`. Идея проверки: отдельный `[Fact]` или `[InlineData]` в `RepositoryBuildToolTests`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Запустить targeted test filter `FullyQualifiedName~VerifyApiCompatibilityRejectsInvalidManualProfileRows` или новый focused duplicate-test filter.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:663-667`, `RepositoryBuildToolTests.cs:1835-1854`
    * `Why not blocker for current task`: duplicate rejection is implemented; missing explicit regression is a test-hardening gap.

CLOSURE_DECISION:

* Задача `T-0990` остаётся открытой. Реализация и тесты почти доведены до требуемой модели, но документация source-of-truth профиля публичного API всё ещё содержит правило, противоречащее текущему verifier behavior.
* Для следующей итерации нужно исправить `docs/documentation/api-manifest.md`, добавить проверку от возврата старой формулировки и повторить focused profile tests, documentation checks и intentional empty-profile failure evidence.
