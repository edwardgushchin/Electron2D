VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0990` итерации `r04` как одиночная область. Область в metadata и manifest согласована: задача вводит ручной пустой профиль публичного API как единственный источник решений о включении API, а generated manifest/Wiki/CLI/build-tool checks должны падать закрыто до owner approval.
* Закрытие прошлых blocker-ов в основном подтверждено. Stale manifest-status test исправлен, generated `API-Compatibility.md` и public-api-documentation verifier переведены на profile-backed модель, старые Wiki/docs формулировки про manual compatibility/planned surface запрещены verifier-ом, `godotReference` теперь валидируется для `approved`/`deferred`/`unsupported`, а документация `api-manifest.md` больше не ограничивает эту проверку только `approved`.
* Принять изменение нельзя. В активном CI/verifier path остался parser старой формы `API-Compatibility.md`: `verify ui-public-api-gate` продолжает читать таблицу так, будто статус находится в старой колонке, тогда как новая generated page имеет форму `Type | Status | Decision | Rationale`. Поэтому после owner approval корректно сгенерированная compatibility page может быть отвергнута UI public API gate, хотя это тот же build-tool/Wiki contract, который меняется задачей `T-0990`.
* Runtime hot path не менялся: область относится к release-management/tooling/docs/tests/generated artifacts. Реальных секретов, приватных ключей, токенов, паролей, локальных абсолютных путей или конфиденциальных данных в проверенных материалах не найдено; `token=<redacted>`, `password=<redacted>` и `<repo>` являются тестовыми или редактированными audit/evidence строками.

Техническая привязка:

* `metadata.taskId`: `T-0990`
* `metadata.iteration`: `r04`
* `metadata.scopeTaskIds`: `["T-0990"]`
* `metadata.scopeSummary`: ручной пустой public API profile как единственный source-of-truth для API inclusion decisions, fail-closed до owner approval; strict audit packaging allowance для точных saved previous-verdict redacted fixture phrases.
* `metadata.previousVerdictChain`: `["docs/verdicts/release-management/t-0990-audit-r01.md", "docs/verdicts/release-management/t-0990-audit-r02.md", "docs/verdicts/release-management/t-0990-audit-r03.md"]`
* `metadata.blockerClosureList`: заявлены закрытия r01 `B1`/`B2`, r02 `B1`/`B2`, r03 `B1`.
* Область не является `combined scope`.
* `metadata/repo-file-snapshots.json`: важные after-снимки реализации, тестов и документации доступны полностью; blocker-а по `evidence gap` нет.

BLOCKERS:

* B1

  * Что не так: `verify ui-public-api-gate` остался несовместим с новой generated `API-Compatibility.md`. Новая страница генерируется как таблица `| Type | Status | Decision | Rationale |`, где статус находится во второй колонке, а decision — в третьей и пишется в backticks. Но UI gate regex по строкам compatibility page фактически пропускает одну колонку и пытается извлечь статус из следующей текстовой колонки. Для новой строки вида `| `Electron2D.CharacterBody2D`| Supported |`approved` | ... |` такой parser не получает `Supported`; строка либо не совпадает, либо при похожей форме захватывает decision вместо status.
  * Почему это важно: `T-0990` меняет форму и источник `API-Compatibility.md`, а CI продолжает запускать `verify ui-public-api-gate --wiki-path .github/wiki`. Пустой профиль сейчас ожидаемо ломает API gates до owner approval, но после утверждения типов корректная generated page должна проходить весь build-tool path без ручных обходов. Текущий parser создаёт параллельный старый контракт и может отклонить корректную page, сгенерированную новой реализацией.
  * Что исправить: Обновить parser `verify ui-public-api-gate`, чтобы он читал именно колонку `Status` новой таблицы `Type | Status | Decision | Rationale` и игнорировал `Decision`/`Rationale` при проверке UI API gate. Добавить regression test: fixture с approved UI/Text API типом, generated `API-Compatibility.md` новой формы и успешным запуском `verify ui-public-api-gate --wiki-path .github/wiki`.
  * Как проверить исправление: Сгенерировать Wiki из fixture с approved UI/Text type через `dotnet run --project eng/Electron2D.Build -- update wiki --output .github/wiki`, затем выполнить `dotnet run --project eng/Electron2D.Build -- verify ui-public-api-gate --wiki-path .github/wiki`. Дополнительно повторить focused profile tests, `verify public-api-documentation`, `update wiki --check --output .github/wiki` и intentional empty-profile failure checks.
  * Проверка опровержения: Проверены generator новой compatibility page, CI workflow, focused tests и public-api-documentation verifier. Public documentation verifier уже принимает новую форму, но это не снимает blocker: отдельный активный CI-шаг `verify ui-public-api-gate` использует собственный parser, focused evidence не запускает его на generated page с approved UI/Text row, а код parser-а остаётся завязан на старое расположение статуса.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3510-3528`, `VerifyUiPublicApiGateAsync`
    * `File/symbol`: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2468-2489`, generated `API-Compatibility.md` table
    * `File/symbol`: `repo-after/.github/workflows/ci.yml:93-94`, CI step `verify ui-public-api-gate --wiki-path .github/wiki`
    * `Criterion`: `implementation content review`, `test coverage review`, `task compliance review`, `architecture coherence`; критерии `repo-after/TASKS.md:4268-4269`, `repo-after/TASKS.md:4293-4295`
    * `Evidence`: generator пишет строки compatibility page как `| Type | Status | Decision | Rationale |`; UI gate regex в `RepositoryWorkflowVerifiers.cs` не читает вторую колонку `Status` новой таблицы как источник статуса; CI продолжает запускать этот verifier.
    * `Impact`: корректно утверждённый manual profile и generated Wiki могут не пройти активный CI gate без ручной правки Wiki или обхода verifier-а.
    * `Fix`: переписать parsing `API-Compatibility.md` в UI public API gate под новую таблицу и добавить focused regression.
    * `Verification`: targeted test/command для `verify ui-public-api-gate` на generated compatibility page новой формы.

EVIDENCE_REVIEW:

* Проверены metadata и область пакета. `metadata/audit-package.input.json` задаёт `taskId=T-0990`, `iteration=r04`, одиночный `scopeTaskIds=["T-0990"]`, previous verdict chain для r01/r02/r03 и closure list по пяти прошлым blocker-ам. `AUDIT-MANIFEST.md` описывает ту же область и тот же смысл задачи.
* Проверены прошлые verdict files. Файлы r01, r02 и r03 доступны в `repo-after/docs/verdicts/release-management/`. Прошлый r01 `B1` закрыт обновлённым `ApiManifestCarriesStableIdentifiersAndProfileStatus` и tracked manifest. Прошлые r01 `B2` и r02 `B1` закрыты в части generated `API-Compatibility.md`, public API documentation verifier, generated Wiki Home и запрета legacy wording. Прошлый r02 `B2` закрыт реализацией и тестами для `godotReference` на всех decisions. Прошлый r03 `B1` закрыт обновлением `docs/documentation/api-manifest.md` и regression-тестом документации.
* Проверены полные итоговые файлы из `repo-after/`; patch использовался только как карта изменений. Снимки важных файлов реализации, тестов, документации и evidence доступны целиком.
* Проверена реализация ручного профиля. `data/api/electron2d-public-api-profile.json` остаётся пустым manual profile с `schemaVersion`, `release`, `godotBaseline`, `approvalAuthority` и `types: []`. `data/api/electron2d-api-manifest.json` ссылается на `publicApiProfile`, не использует старую `compatibilityPage`, и показывает все runtime public types как `unapproved`.
* Проверен manifest generator. Он читает manual profile, мапит отсутствующую строку в `unapproved`, `approved` в `supported`, `deferred`/`unsupported` в соответствующие исключающие статусы, а `outOfProfile` остаётся true для всего, что не `supported`.
* Проверен build-tool/verifier path. `verify api-compatibility` валидирует profile, проверяет generated manifest shape и падает на exported public type без `approved`. `update wiki` генерирует `API-Compatibility.md` с generated marker, строкой `Generated from data/api/electron2d-public-api-profile.json`, статусами `Supported`/`Deferred`/`Unsupported`/`Unapproved` и текущей runtime-surface секцией. `verify public-api-documentation` проверяет generated page и запрещает старые Wiki/docs формулировки.
* Проверены tests. Focused evidence запускает 29 тестов и проходит. Тесты покрывают empty-profile manifest, generated compatibility page, public API documentation verifier, invalid profile rows для `approved`/`deferred`/`unsupported`, exported public type fail-fast, CLI compare-godot approved/deferred behavior, local docs/update checks, documentation regression для all-decisions `godotReference` wording и audit placeholder allowance.
* Проверены evidence-команды. Успешно прошли `build-tool-build`, `focused-profile-tests`, `audit-loop-stabilization`, `update-docs-check`, `verify-docs`, `verify-ci-matrix`, `verify-licenses`, `verify-audit-contracts`, `previous-verdict-placeholder-scanner`, `git-diff-check`. Intentional failure checks для пустого профиля завершились ожидаемым кодом `1` и diagnostic `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT` для `Electron2D.AnimatedSprite2D`.
* Проверена область и лишние правки. Изменения находятся в release-management/tooling/docs/tests/generated artifacts и связаны с заявленной задачей. Runtime implementation public API не менялся.
* Проверены секреты и локальные данные по `repo-after/`, patch, metadata и evidence. Реальных секретов не найдено. Redacted placeholders находятся в тестовых/аудиторских материалах и соответствуют заявленному scope summary.

Техническая привязка:

* Metadata/manifest: `metadata/audit-package.input.json`, `AUDIT-MANIFEST.md:3-10`, `AUDIT-MANIFEST.md:204-245`
* Snapshot index: `metadata/repo-file-snapshots.json`
* Scope inventory: `repo-file-hashes.json`
* Task contract: `repo-after/TASKS.md:4233-4335`
* Previous verdicts: `repo-after/docs/verdicts/release-management/t-0990-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0990-audit-r02.md`, `repo-after/docs/verdicts/release-management/t-0990-audit-r03.md`
* Manual profile: `repo-after/data/api/electron2d-public-api-profile.json:1-7`
* Generated manifest: `repo-after/data/api/electron2d-api-manifest.json`
* Manifest generator: `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs`
* Profile reader and gates: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:587-716`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1031-1226`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1562-1756`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2413-2490`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2653-2669`
* Workflow verifiers: `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3510-3528`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3536-3616`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3650-3695`
* CI API gates: `repo-after/.github/workflows/ci.yml:84-97`
* Tests: `repo-after/tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `repo-after/tests/Electron2D.Tests.Integration/Electron2DCliWorkflowTests.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* Evidence:

  * `evidence/T-0990-r04/preflight/focused-profile-tests/command.txt`
  * `evidence/T-0990-r04/preflight/focused-profile-tests/T-0990/r04/focused-profile-tests/stdout.txt`
  * `evidence/T-0990-r04/preflight/focused-profile-tests/T-0990/r04/focused-profile-tests/summary.txt`
  * `evidence/T-0990-r04/preflight/audit-loop-stabilization/T-0990/r04/audit-loop-stabilization/summary.txt`
  * `evidence/T-0990-r04/preflight/update-api-manifest-empty-profile-fail/T-0990/r04/update-api-manifest-empty-profile-fail/stdout.txt`
  * `evidence/T-0990-r04/preflight/update-api-manifest-empty-profile-fail/T-0990/r04/update-api-manifest-empty-profile-fail/summary.txt`
  * `evidence/T-0990-r04/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r04/verify-api-compatibility-empty-profile-fail/stdout.txt`
  * `evidence/T-0990-r04/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r04/verify-api-compatibility-empty-profile-fail/summary.txt`
  * остальные `summary.txt`/`stdout.txt`/`stderr.txt`/`exit-code.txt` файлы в `evidence/T-0990-r04/preflight/`

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `AUDIT-MANIFEST.md:233-245`; raw evidence summaries для intentional failure checks.
  * Проблема: В manifest для `update-api-manifest-empty-profile-fail` и `verify-api-compatibility-empty-profile-fail` указан expected exit code `0`, хотя соответствующие raw evidence summaries корректно фиксируют `expectedExitCode=1`, `actualExitCode=1`, `passed=true`.
  * Почему не блокирует текущую задачу: Raw evidence достаточно для проверки поведения: эти проверки действительно являются ожидаемыми отказами и проходят именно как expected-failure checks. Ошибка в manifest ухудшает качество audit package, но не скрывает текущий blocker и не мешает проверить код, тесты и документацию.
  * Куда перенести: Suggested new task — «Синхронизировать expected exit codes в `AUDIT-MANIFEST.md` с preflight evidence summaries». Приоритет P3. Домен: audit packaging/release-management. Критерий приёмки: каждая строка preflight check в manifest показывает тот же expected exit code, что и соответствующий `summary.txt`. Идея проверки: автотест audit package builder-а сравнивает manifest checks с evidence summaries и падает на рассинхроне.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Сгенерировать audit package с одной expected-success и одной expected-failure проверкой; автоматической проверкой сравнить `AUDIT-MANIFEST.md` и `evidence/**/summary.txt`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `AUDIT-MANIFEST.md`, entries `update-api-manifest-empty-profile-fail`, `verify-api-compatibility-empty-profile-fail`
    * `Why not blocker for current task`: raw evidence summaries are correct and sufficient for this audit.

* FOLLOW_UP_FINDING F2

  * Идентификатор: `F2`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1712-1756`, `VerifyGeneratedManifestProfileGate`.
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
  * Почему не блокирует текущую задачу: Само поведение duplicate rejection видно в коде через `entries.TryAdd` и отдельный diagnostic. Текущий blocker связан с несовместимым parser-ом UI gate, а не с отсутствием duplicate-check реализации.
  * Куда перенести: Suggested new task — «Добавить focused regression для duplicate `fullName` в manual API profile verifier». Приоритет P3. Домен: build tooling/API compatibility tests. Критерий приёмки: `verify api-compatibility --wiki-path .github/wiki` на fixture с двумя строками одного `fullName` падает с `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`. Идея проверки: отдельный `[Fact]` или `[InlineData]` в `RepositoryBuildToolTests`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Запустить targeted test filter `FullyQualifiedName~VerifyApiCompatibilityRejectsInvalidManualProfileRows` или новый focused duplicate-test filter.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:663-667`, `RepositoryBuildToolTests.cs:1835-1854`
    * `Why not blocker for current task`: duplicate rejection is implemented; missing explicit regression is a test-hardening gap.

CLOSURE_DECISION:

* Задача `T-0990` остаётся открытой. Пакет r04 закрывает прошлые замечания по test/docs/profile validation, но оставляет активный CI/verifier path, который не понимает новую generated `API-Compatibility.md` table shape.
* Для следующей итерации нужно исправить `verify ui-public-api-gate` под новую таблицу, добавить focused regression на generated compatibility page с approved UI/Text API row, повторить focused profile tests, public documentation checks, Wiki checks и intentional empty-profile failure evidence.
