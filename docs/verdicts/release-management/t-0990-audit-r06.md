VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0990` итерации `r06` как одиночная область. Metadata, manifest, snapshots, `repo-after/`, тесты, документация, previous verdict files и evidence читаются. Важные after-снимки реализации, тестов и документации полные; blocker-а по недоступности файлов нет.
* Основная модель `T-0990` в r06 реализована: ручной профиль публичного API пустой, generated manifest больше не берёт статусы из Wiki, `API-Compatibility.md` генерируется из `data/api/electron2d-public-api-profile.json`, `update api-manifest --check` и `verify api-compatibility --wiki-path .github/wiki` ожидаемо падают на exported public types без `approved`, CLI compare-godot использует profile-backed decisions, документация обновлена, а прошлые blocker-ы r01–r05 в заявленных частях закрыты.
* Принять пакет нельзя. В активном `verify ui-public-api-gate` осталась ошибка нормализации имён вложенных UI/Text типов. Generated `API-UI-and-Text.md` и generated `API-Compatibility.md` используют dotted public display names вида `Electron2D.TextureRect.StretchMode`, но verifier превращает имя из UI category page в `Electron2D.TextureRect+StretchMode` и затем ищет exact match в compatibility table. В текущем manifest уже есть несколько вложенных `UI and Text` enum types, поэтому после owner approval корректно сгенерированная Wiki всё равно не сможет пройти UI public API gate для этих строк.
* Runtime hot path не менялся: область относится к release-management/tooling/docs/tests/generated artifacts. Реальных секретов, приватных ключей, токенов, паролей, локальных абсолютных путей или конфиденциальных данных в проверенных материалах не найдено; `token=<redacted>`, `password=<redacted>`, `<repo>` и `/home/user/repo` находятся в тестовых, redacted или сохранённых audit-fixture строках.

Техническая привязка:

* `metadata.taskId`: `T-0990`
* `metadata.iteration`: `r06`
* `metadata.scopeTaskIds`: `["T-0990"]`
* `metadata.scopeSummary`: ручной пустой public API profile как единственный source-of-truth для API inclusion decisions; fail-closed manifest/Wiki/CLI/build-tool checks до owner approval; закрытие r05 UI public API gate documentation drift; перенос r01-r05 follow-ups в `TASKS.md`.
* `metadata.previousVerdictChain`: `docs/verdicts/release-management/t-0990-audit-r01.md`, `t-0990-audit-r02.md`, `t-0990-audit-r03.md`, `t-0990-audit-r04.md`, `t-0990-audit-r05.md`
* `metadata.blockerClosureList`: заявлены закрытия r01 `B1`/`B2`, r02 `B1`/`B2`, r03 `B1`, r04 `B1`, r05 `B1`, а также перенос r01-r05 follow-ups в `T-0991`, `T-0992`, `T-0993`.
* Область не является `combined scope`.
* Основные критерии задачи: `repo-after/TASKS.md:4265-4295`
* Snapshot index: `metadata/repo-file-snapshots.json`, 32 файла, `fullContentIncluded: true` для всех snapshot entries.

BLOCKERS:

* B1

  * Что не так: `verify ui-public-api-gate` неправильно сопоставляет вложенные UI/Text типы между generated category page и generated compatibility page. UI category parser берёт link text из `API-UI-and-Text.md` и заменяет `.` на `+`, поэтому строка `[TextureRect.StretchMode](TextureRect.StretchMode)` становится ключом `Electron2D.TextureRect+StretchMode`. Compatibility parser при этом кладёт в словарь точное имя из `API-Compatibility.md`, то есть `Electron2D.TextureRect.StretchMode`. Эти ключи не совпадают.
  * Почему это важно: `T-0990` делает ручной профиль source-of-truth и должен дать проверяемый путь после owner approval. Сейчас даже если владелец корректно утвердит вложенные UI/Text enum types как `approved`, generated Wiki останется несовместимой с активным UI public API gate. Это тот же класс риска, который уже блокировал r04: корректно сгенерированная compatibility page не должна требовать ручных правок или обходов verifier-а.
  * Что исправить: Нормализовать имена в `verify ui-public-api-gate` одинаково для обеих таблиц. Самый простой вариант — сравнивать public display names с точками, без преобразования `.` в `+`, потому generated manifest/Wiki уже публикуют вложенные типы как `Electron2D.TextureRect.StretchMode`. Альтернативно можно нормализовать обе стороны в одну форму перед сравнением. Добавить regression test с вложенным UI/Text type, например `TextureRect.StretchMode` или `BaseButton.ActionMode`, где `API-UI-and-Text.md` содержит dotted link text, а `API-Compatibility.md` содержит `Electron2D.TextureRect.StretchMode` со статусом `Supported`.
  * Как проверить исправление: Добавить targeted integration test для nested UI/Text row и выполнить focused profile tests. Минимально: `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~VerifyUiPublicApiGateAcceptsGeneratedCompatibilityPageWithDecisionColumn|FullyQualifiedName~VerifyUiPublicApiGate" --no-restore -v:minimal`. Также повторить `verify ci-matrix`, `verify docs`, `verify api-compatibility --wiki-path .github/wiki` на fixture с approved nested UI/Text type и intentional empty-profile failure checks.
  * Проверка опровержения: Проверены текущий manifest, generator новой Wiki, `verify ui-public-api-gate` и focused tests. r06 действительно закрывает r05 documentation drift и flat-row parser issue, но focused regression использует только плоский row `[CharacterBody2D](CharacterBody2D)` и не проверяет вложенные UI/Text имена. В текущем manifest уже есть реальные UI/Text rows с dotted nested names, поэтому это не гипотетический внешний случай.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3498-3502`, парсинг `API-UI-and-Text.md` и преобразование `.` → `+`.
    * `File/symbol`: `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3510-3517`, парсинг `API-Compatibility.md` с exact dotted name.
    * `File/symbol`: `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3520-3528`, ошибка missing/not supported при несовпадении ключей.
    * `File/symbol`: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2513-2516`, generated category page пишет `type.Name`, например `TextureRect.StretchMode`.
    * `File/symbol`: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2491-2495`, generated compatibility page пишет `row.FullName`, например `Electron2D.TextureRect.StretchMode`.
    * `File/symbol`: `repo-after/data/api/electron2d-api-manifest.json:4224-4238`, `Electron2D.BaseButton.ActionMode` имеет `category: "UI and Text"` и `name: "BaseButton.ActionMode"`.
    * `File/symbol`: `repo-after/data/api/electron2d-api-manifest.json:34285-34299`, `Electron2D.TextureRect.StretchMode` имеет `category: "UI and Text"` и `name: "TextureRect.StretchMode"`.
    * `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1770-1789`, existing regression покрывает только flat row `CharacterBody2D`.
    * `Criterion`: `implementation content review`, `test coverage review`, `task compliance review`, `architecture coherence`; критерии `repo-after/TASKS.md:4268-4269`, `repo-after/TASKS.md:4293-4295`.
    * `Evidence`: current manifest contains 9 nested `UI and Text` public enum rows, while verifier compares `Electron2D.<name-with-plus>` from UI page against `Electron2D.<name-with-dot>` from compatibility page.
    * `Impact`: после owner approval generated Wiki для реальной UI/Text public surface может быть отклонена активным CI gate `verify ui-public-api-gate --wiki-path .github/wiki` без нарушения manual profile.
    * `Fix`: единая нормализация public display names в UI gate и regression для nested UI/Text type.
    * `Verification`: focused nested UI gate regression плюс повтор r06 focused/evidence checks.

EVIDENCE_REVIEW:

* Проверены metadata и область пакета. `metadata/audit-package.input.json` задаёт `taskId=T-0990`, `iteration=r06`, одиночный `scopeTaskIds=["T-0990"]`, previous verdict chain для r01–r05 и closure list для всех прошлых blocker-ов. `AUDIT-MANIFEST.md:3-10` и `AUDIT-MANIFEST.md:210-225` описывают ту же область и закрытия.
* Проверены previous verdict files. Файлы r01–r05 доступны в `repo-after/docs/verdicts/release-management/`. По текущим файлам подтверждено: r01 `B1` закрыт обновлённым manifest-status тестом; r01 `B2` и r02 `B1` закрыты новой generated Wiki/API Compatibility моделью и public documentation verifier; r02 `B2` закрыт validation `godotReference` для всех decisions; r03 `B1` закрыт обновлением `docs/documentation/api-manifest.md`; r04 `B1` закрыт parser-ом новой `Type | Status | Decision | Rationale` таблицы для flat rows; r05 `B1` закрыт release-management документацией и regression-ами против старой C# migration-debt формулировки.
* Проверены полные итоговые файлы из `repo-after/`; patch использовался только как карта изменений. `metadata/repo-file-snapshots.json` не показывает отсутствующих или неполных after-снимков важных файлов реализации, тестов или документации.
* Проверена реализация ручного профиля. `data/api/electron2d-public-api-profile.json:1-7` остаётся пустым manual profile с `schemaVersion`, `release`, `godotBaseline`, `approvalAuthority` и `types: []`. `data/api/electron2d-api-manifest.json:1-28` ссылается на `publicApiProfile`, не использует старую `compatibilityPage`, и показывает `unapproved: 175`.
* Проверен manifest generator. `eng/Electron2D.ApiManifestGenerator/Program.cs:155-181` мапит отсутствующую строку manual profile в `unapproved`, `approved` в `supported`, `deferred`/`unsupported` в исключающие статусы, а `outOfProfile` остаётся true для всего, что не `supported`.
* Проверен build-tool/verifier path. `RepositoryPolicyVerifiers.ManualApiProfileReader` валидирует обязательные поля, `decision`, `rationale`, `godotReference` и duplicate `fullName`; `verify api-compatibility` валидирует profile и падает на exported public type без `approved`; `update wiki` генерирует `API-Compatibility.md` с generated marker, строкой `Generated from data/api/electron2d-public-api-profile.json`, статусами `Supported`/`Deferred`/`Unsupported`/`Unapproved` и секцией `Current Public Runtime Surface`.
* Проверен UI public API gate. r06 исправляет r04/r05 класс проблем для flat rows: parser берёт статус из второй колонки новой compatibility table, а release-management docs описывают `verify ui-public-api-gate --wiki-path .github/wiki` как текущую C#-проверку. Однако найден текущий blocker B1: name normalization для nested UI/Text rows остаётся несовместимой с generated Wiki display names.
* Проверены тесты. Focused evidence запускает 32 теста и проходит. Покрыты empty-profile manifest, generated compatibility page, public API documentation verifier, invalid profile rows для `approved`/`deferred`/`unsupported`, exported public type fail-fast, CLI compare-godot approved/deferred behavior, local docs/update checks, documentation regressions, flat UI gate parser, audit placeholder allowance, audit-loop stabilization и audit-submit reattach stabilization.
* Проверены evidence-команды. Успешно прошли `build-tool-build`, `focused-profile-tests`, `audit-loop-stabilization`, `audit-submit-reattach-stabilization`, `update-docs-check`, `verify-docs`, `verify-ci-matrix`, `verify-licenses`, `verify-audit-contracts`, `verify-audit-followups`, `previous-verdict-placeholder-scanner`, `git-diff-check`. Intentional failure checks для пустого профиля завершились ожидаемым кодом `1` и diagnostic `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT` для `Electron2D.AnimatedSprite2D`.
* Проверена область и лишние правки. Изменения находятся в release-management/tooling/docs/tests/generated artifacts и связаны с заявленной задачей. Runtime implementation public API не менялся. Изменение `AuditSubmitCodexChromeCommand.cs` включено в metadata allowlist и evidence как audit-submit reattach stabilization для текущей audit-chain упаковки/отправки; оно не меняет runtime/API behavior.
* Проверены секреты и локальные данные по `repo-after/`, patch, metadata и evidence. Реальных секретов не найдено. Найденные `token=<redacted>`, `password=<redacted>`, `<repo>` и `/home/user/repo` относятся к тестовым fixtures, redacted previous verdict prose или audit sanitizer regression-ам.

Техническая привязка:

* Metadata/manifest: `metadata/audit-package.input.json`, `AUDIT-MANIFEST.md:3-10`, `AUDIT-MANIFEST.md:210-225`, `AUDIT-MANIFEST.md:242-256`
* Snapshot index: `metadata/repo-file-snapshots.json`
* Scope inventory: `repo-file-hashes.json`
* Task contract: `repo-after/TASKS.md:4233-4296`
* Follow-up tasks: `repo-after/TASKS.md:4373-4612`
* Previous verdicts: `repo-after/docs/verdicts/release-management/t-0990-audit-r01.md`, `t-0990-audit-r02.md`, `t-0990-audit-r03.md`, `t-0990-audit-r04.md`, `t-0990-audit-r05.md`
* Manual profile: `repo-after/data/api/electron2d-public-api-profile.json:1-7`
* Generated manifest: `repo-after/data/api/electron2d-api-manifest.json:1-28`, `repo-after/data/api/electron2d-api-manifest.json:4224-4238`, `repo-after/data/api/electron2d-api-manifest.json:34285-34299`
* Manifest generator: `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:155-181`
* Profile reader/gates: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:638-667`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1719-1756`
* Wiki generation/checking: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2475-2495`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2513-2516`
* Workflow verifiers: `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3472-3533`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3536-3616`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3650-3695`
* CI API gates: `repo-after/.github/workflows/ci.yml`
* Tests: `repo-after/tests/Electron2D.Tests.Integration/ApiManifestTests.cs:70-105`, `repo-after/tests/Electron2D.Tests.Integration/Electron2DCliWorkflowTests.cs:423-490`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1754-1789`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1856-1875`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1925-1941`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:2718-2733`
* Evidence:

  * `evidence/T-0990-r06/preflight/focused-profile-tests/command.txt`
  * `evidence/T-0990-r06/preflight/focused-profile-tests/T-0990/r06/focused-profile-tests/result.txt`
  * `evidence/T-0990-r06/preflight/focused-profile-tests/T-0990/r06/focused-profile-tests/output.txt`
  * `evidence/T-0990-r06/preflight/audit-loop-stabilization/T-0990/r06/audit-loop-stabilization/result.txt`
  * `evidence/T-0990-r06/preflight/audit-submit-reattach-stabilization/T-0990/r06/audit-submit-reattach-stabilization/result.txt`
  * `evidence/T-0990-r06/preflight/verify-audit-followups/T-0990/r06/verify-audit-followups/result.txt`
  * `evidence/T-0990-r06/preflight/update-api-manifest-empty-profile-fail/T-0990/r06/update-api-manifest-empty-profile-fail/result.txt`
  * `evidence/T-0990-r06/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r06/verify-api-compatibility-empty-profile-fail/result.txt`
  * остальные `output.txt`/`result.txt`/`command.txt` файлы в `evidence/T-0990-r06/preflight/`

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `AUDIT-MANIFEST.md:249`, `AUDIT-MANIFEST.md:251`; raw evidence result files для intentional failure checks.
  * Проблема: В manifest для `update-api-manifest-empty-profile-fail` и `verify-api-compatibility-empty-profile-fail` указан expected exit code `0`, хотя соответствующие `result.txt` корректно фиксируют `expectedExitCode=1`, `actualExitCode=1`, `status=PASS`.
  * Почему не блокирует текущую задачу: Raw evidence достаточно для проверки поведения: эти проверки действительно являются ожидаемыми отказами и проходят именно как expected-failure checks. Кроме того, r06 перенёс этот долг в отдельную задачу `T-0991`, а `verify-audit-followups` прошёл. Текущий blocker связан не с упаковкой evidence, а с UI gate implementation.
  * Куда перенести: Suggested existing task — `T-0991: Синхронизировать expected exit codes в audit manifest с evidence summaries`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Закрывающая задача должна сгенерировать audit package с expected-success и expected-failure preflight checks и автоматически сравнить expected exit codes в `AUDIT-MANIFEST.md` с `evidence/**/result.txt`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `AUDIT-MANIFEST.md:249`, `AUDIT-MANIFEST.md:251`; `repo-after/TASKS.md:4373-4408`
    * `Why not blocker for current task`: raw evidence results are correct and follow-up is tracked as `T-0991`.

* FOLLOW_UP_FINDING F2

  * Идентификатор: `F2`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1719-1756`, `VerifyGeneratedManifestProfileGate`.
  * Проблема: `update api-manifest --check` читает manual profile и накапливает validation errors, но затем может сначала вернуть `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT`. Если profile одновременно невалиден и runtime экспортирует public types, profile validation diagnostics могут не попасть в вывод этой команды.
  * Почему не блокирует текущую задачу: Основной verifier `verify api-compatibility` печатает profile validation errors до exported-type gate, а intentional empty-profile failure для `update api-manifest --check` доказан evidence. Долг перенесён в отдельную задачу `T-0992`; это не ослабляет обязательный fail-closed behavior текущей задачи.
  * Куда перенести: Suggested existing task — `T-0992: Приоритизировать diagnostics manual API profile в update api-manifest`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Закрывающая задача должна добавить fixture с invalid manual profile и exported runtime public type, затем проверить, что `update api-manifest --check` печатает profile validation diagnostics до unapproved export diagnostic или вместе с ним.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:1719-1756`; `repo-after/TASKS.md:4475-4510`
    * `Why not blocker for current task`: обязательный empty-profile fail-fast path доказан; проблема касается порядка диагностического вывода.

* FOLLOW_UP_FINDING F3

  * Идентификатор: `F3`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:663-667`; `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1856-1875`.
  * Проблема: Duplicate `fullName` validation реализована в production code, но focused invalid-profile tests всё ещё не содержат отдельного regression-case для diagnostic `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`.
  * Почему не блокирует текущую задачу: Само поведение duplicate rejection видно в коде через `entries.TryAdd` и отдельный diagnostic. Долг перенесён в отдельную задачу `T-0993`; текущий blocker относится к active UI gate name normalization, а не к отсутствию duplicate-check реализации.
  * Куда перенести: Suggested existing task — `T-0993: Добавить regression для duplicate fullName в manual API profile`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Закрывающая задача должна добавить targeted test, где `verify api-compatibility --wiki-path .github/wiki` на profile с двумя строками одного `fullName` падает с `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryPolicyVerifiers.cs:663-667`, `RepositoryBuildToolTests.cs:1856-1875`; `repo-after/TASKS.md:4577-4612`
    * `Why not blocker for current task`: duplicate rejection is implemented and tracked for explicit regression coverage.

CLOSURE_DECISION:

* Задача `T-0990` остаётся открытой. r06 закрывает прошлые blocker-ы по тестам, документации, profile validation, generated `API-Compatibility.md`, flat UI gate parser и r05 release-management wording drift, но оставляет активный UI public API gate, который не может корректно сопоставить реальные вложенные UI/Text public types из текущего manifest.
* Для следующей итерации нужно исправить name normalization в `verify ui-public-api-gate`, добавить focused regression на nested UI/Text type из generated Wiki/compatibility table, повторить focused profile tests, CI/docs checks и intentional empty-profile failure evidence.
