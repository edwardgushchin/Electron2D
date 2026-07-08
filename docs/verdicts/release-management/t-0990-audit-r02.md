VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0990` итерации `r02` как одиночная область. Область в metadata и manifest согласована: задача вводит ручной пустой профиль публичного API как единственный источник решений о включении типов, а generated manifest/Wiki/CLI/build-tool checks должны падать закрыто до утверждения типов владельцем.
* Закрытие прошлого `B1` подтверждено: тест `ApiManifestCarriesStableIdentifiersAndProfileStatus` теперь проверяет empty-profile состояние `unapproved`/`not_verified`/`outOfProfile=true`, focused evidence включает этот тест, а tracked manifest действительно содержит `Electron2D.Control` в таком состоянии.
* Изменение нельзя принять. Закрытие прошлого `B2` неполное: часть документации и generated Wiki Home всё ещё описывают старую модель manual compatibility page / planned surface / `Partial`/`Experimental`/`Planned`. Дополнительно текущий profile verifier не валидирует `godotReference` для `deferred`/`unsupported` строк, хотя ручной профиль объявлен source-of-truth и критерий задачи требует, чтобы неверный `godotReference` ломал verifier.
* Производительный runtime-path не менялся: проверяемая область относится к release-management/tooling/docs/tests/generated artifacts. Секретов, приватных ключей, реальных токенов, паролей, локальных абсолютных путей или конфиденциальных данных в проверенных материалах не найдено; строки `token=<redacted>` и `password=<redacted>` являются тестовой фикстурой.

Техническая привязка:

* `metadata.taskId`: `T-0990`
* `metadata.iteration`: `r02`
* `metadata.scopeTaskIds`: `["T-0990"]`
* `metadata.scopeSummary`: ручной пустой public API profile как единственный source-of-truth для API inclusion decisions, fail-closed до owner approval.
* `metadata.previousVerdictChain`: `["docs/verdicts/release-management/t-0990-audit-r01.md"]`
* `metadata.blockerClosureList`: две строки закрытия для r01 `B1` и `B2`
* Область не является `combined scope`.
* Проверены полные снимки из `repo-after/`; `metadata/repo-file-snapshots.json` содержит 25 файлов, все с `fullContentIncluded: true`.

BLOCKERS:

* B1

  * Что не так: Закрытие прошлого замечания по документации и Wiki-модели неполное. В одном и том же документе сначала правильно сказано, что `API-Compatibility.md` является generated page из `data/api/electron2d-public-api-profile.json` и больше не содержит ручную секцию `Planned 2D Surface`, но ниже этот же документ снова называет compatibility page ручной и утверждает, что audit проверяет `planned preview surface`. Generated Wiki Home также продолжает описывать compatibility page через старые статусы `partial`, `experimental`, `planned` и `planned surface`.
  * Почему это важно: `T-0990` меняет источник истины публичного API. Документация и generated Wiki output не должны одновременно описывать новую profile-backed модель и старую Wiki/manual-status модель. Это прямо относится к прошлому r01 `B2`: в r02 заявлено, что документация и verifier переведены на `Supported`/`Deferred`/`Unsupported`/`Unapproved` и без `Planned 2D Surface`, но проверенные файлы всё ещё содержат старые формулировки.
  * Что исправить: Удалить из `docs/documentation/github-wiki-api-reference.md` остатки про ручную compatibility page и planned preview surface. Обновить generated Home text в `RepositoryPolicyVerifiers` так, чтобы ссылка и описание `API Compatibility` говорили о manual profile decisions и статусах `Supported`/`Deferred`/`Unsupported`/`Unapproved`, а не о planned surface и старых статусах. Добавить verifier/test, который ловит эти старые формулировки в generated Wiki Home и документации.
  * Как проверить исправление: Сгенерировать Wiki через `dotnet run --project eng/Electron2D.Build -- update wiki --output .github/wiki`, затем выполнить `dotnet run --project eng/Electron2D.Build -- verify public-api-documentation --wiki-path .github/wiki`, `dotnet run --project eng/Electron2D.Build -- update wiki --check --output .github/wiki`, `dotnet run --project eng/Electron2D.Build -- verify docs` и focused profile tests. Проверка должна падать, если где-либо в текущих API/Wiki docs остаются `manual compatibility page`, `planned preview surface`, `planned surface` или текущие статусы `Partial`/`Experimental`/`Planned`.
  * Проверка опровержения: Проверены новая генерация `API-Compatibility.md`, обновлённый `VerifyPublicApiDocumentationAsync` и focused evidence. Сама `API-Compatibility.md` shape-проверка исправлена, но это не снимает blocker: stale-текст остался в доменном документе Wiki и в generated Home output, а focused regression проверяет только наличие ссылок в `Home.md` и структуру `API-Compatibility.md`, не запрещая старую модель в Home.
  * Техническая привязка:

    * `File/symbol`: `repo-after/docs/documentation/github-wiki-api-reference.md:58`, `repo-after/docs/documentation/github-wiki-api-reference.md:166`, `repo-after/docs/documentation/github-wiki-api-reference.md:182`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2252-2264`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2269-2311`
    * `Criterion`: `previous blockers closure`, `documentation review`, `task compliance review`, `generated Wiki output`, `architecture coherence`; критерии `repo-after/TASKS.md:4268-4269`, `repo-after/TASKS.md:4293-4295`
    * `Evidence`: прошлый r01 `B2` требовал убрать старую Wiki compatibility модель; текущий `AUDIT-MANIFEST.md:183-189` заявляет closure. Но `github-wiki-api-reference.md:166` всё ещё говорит «Вместе с ручной compatibility page», `github-wiki-api-reference.md:182` всё ещё требует `planned preview surface`, а `RepositoryPolicyVerifiers.cs:2263`, `RepositoryPolicyVerifiers.cs:2284`, `RepositoryPolicyVerifiers.cs:2310` генерируют Home с `planned surface` и `partial, experimental, planned`.
    * `Impact`: принимающая сторона получит generated Wiki/docs, которые продолжают публиковать старый API status contract и тем самым подрывают новую ownership-модель ручного профиля.
    * `Fix`: синхронизировать доменный документ, generated Home text и verifier/test запреты со статусами `Supported`/`Deferred`/`Unsupported`/`Unapproved`.
    * `Verification`: повтор focused profile tests плюс отдельное доказательство, что generated Home и docs больше не содержат старую модель.

* B2

  * Что не так: Profile verifier валидирует существование Godot class packet только для `decision = "approved"`. Для `deferred` и `unsupported` строк он принимает любой непустой `godotReference`, включая несуществующее имя или путь с запрещёнными сегментами, потому вызов `GodotClassPacketExists` стоит под условием `decision == "approved"`.
  * Почему это важно: Ручной профиль объявлен единственным источником решений о публичной поверхности. `deferred` и `unsupported` — такие же утверждённые owner decisions, как `approved`, только исключающие тип из текущего релиза. Если verifier принимает строку с невалидным `godotReference`, профиль перестаёт быть проверяемой source-of-truth моделью: можно зафиксировать исключение для несуществующего или неправильно указанного Godot-типа, и это не будет поймано profile validation.
  * Что исправить: Валидировать `godotReference` для всех строк `types[]`, как минимум на пустое значение, запрещённые path-сегменты и существование соответствующего Godot `4.7-stable` class packet. Если намерение состоит в том, что `deferred`/`unsupported` могут ссылаться на отсутствующий packet, это должно быть явно оформлено как принятое отличие в документации и тестах; в текущем контракте такого исключения нет.
  * Как проверить исправление: Добавить targeted tests в `RepositoryBuildToolTests.VerifyApiCompatibilityRejectsInvalidManualProfileRows` или отдельную теорию: `decision = deferred` + `godotReference = MissingGodotClass`, `decision = unsupported` + `godotReference = ../Missing`, ожидаемый diagnostic `E2D-BUILD-API-PROFILE-GODOT-REFERENCE`. Повторить focused profile tests и intentional empty-profile failure evidence.
  * Проверка опровержения: Проверены `TASKS.md`, `docs/documentation/api-manifest.md`, `docs/release-management/api-compatibility.md`, код reader-а и тесты. Документы требуют `godotReference` как имя Godot class packet для строки профиля, а task criterion отдельно называет неверный `godotReference` ошибкой verifier-а. Тесты покрывают только `approved` + missing packet; accepted risk или документированного исключения для `deferred`/`unsupported` в пакете нет.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:638-667`, `ManualApiProfileReader.Read`; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:681-692`, `GodotClassPacketExists`
    * `Criterion`: `implementation content review`, `test coverage review`, `task compliance review`; критерии `repo-after/TASKS.md:4287-4295`, особенно `repo-after/TASKS.md:4290`
    * `Evidence`: `RepositoryPolicyVerifiers.cs:648-651` проверяет допустимость `decision`, `RepositoryPolicyVerifiers.cs:653-656` проверяет `rationale`, но `RepositoryPolicyVerifiers.cs:658-661` вызывает `GodotClassPacketExists` только при `decision == "approved"`. `RepositoryBuildToolTests.cs:1835-1852` покрывает missing Godot packet только через inline case `approved`.
    * `Impact`: profile verifier не полностью доказывает корректность ручного профиля как источника истины для owner decisions.
    * `Fix`: перенести проверку `godotReference` на все decisions или явно документировать и тестировать исключение.
    * `Verification`: targeted integration tests для invalid `godotReference` на `approved`, `deferred` и `unsupported` decisions.

EVIDENCE_REVIEW:

* Проверены metadata и область пакета. `metadata/audit-package.input.json` задаёт `taskId=T-0990`, `iteration=r02`, одиночный `scopeTaskIds=["T-0990"]`, предыдущую цепочку `docs/verdicts/release-management/t-0990-audit-r01.md` и closure list для r01 `B1`/`B2`. `AUDIT-MANIFEST.md` перечисляет ту же область и тот же summary.
* Проверены прошлые verdict files. Файл r01 доступен в `repo-after/docs/verdicts/release-management/t-0990-audit-r01.md`. Прошлый `B1` закрыт текущим тестом и manifest. Прошлый `B2` закрыт только частично: `API-Compatibility.md` verifier исправлен, но stale-документация и generated Home text остались.
* Проверены полные файлы из `repo-after/`: код generator/build-tool/verifier-ов, тесты, профиль, manifest, документация, CI, diary, предыдущий verdict. Patch использовался только как карта изменений, не как замена full file review.
* Проверена реализация ручного профиля. `data/api/electron2d-public-api-profile.json` действительно пустой и содержит `schemaVersion`, `release`, `godotBaseline`, `approvalAuthority`, `types: []`. `data/api/electron2d-api-manifest.json` больше не содержит `compatibilityPage` в `generatedFrom`, указывает `publicApiProfile` и показывает `unapproved: 175`.
* Проверены generator/build-tool paths. Manifest generator читает manual profile и мапит отсутствующую строку в `unapproved`. `verify api-compatibility` сначала читает profile, затем падает на exported public type без `approved`. `update wiki` генерирует `API-Compatibility.md` с generated marker, строкой `Generated from data/api/electron2d-public-api-profile.json`, статусами `Supported`/`Deferred`/`Unsupported`/`Unapproved` и секцией `Current Public Runtime Surface`.
* Проверены тесты. Focused evidence запускает 26 тестов и проходит. Тесты покрывают empty-profile manifest, generated compatibility page, public API documentation verifier, invalid profile rows для части случаев, fail-fast exported type, CLI compare approved/deferred behavior, local docs и docs update. Найдены blocker B2 и дополнительный тестовый долг F3.
* Проверены evidence-команды. `build-tool-build`, `focused-profile-tests`, `update-docs-check`, `verify-docs`, `verify-ci-matrix`, `verify-licenses`, `verify-audit-contracts`, `git-diff-check` прошли. Intentional failures для пустого профиля корректно завершились кодом `1` и diagnostic `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT` для `Electron2D.AnimatedSprite2D`.
* Проверена область и лишние правки. Runtime public API implementation files не изменялись; изменения находятся в release-management/tooling/docs/tests/generated artifacts. Добавление `docs/verdicts/release-management/t-0990-audit-r01.md` согласовано с `metadata.previousVerdictChain`.
* Проверены секреты и локальные данные по `repo-after/`, patch, metadata и evidence. Реальных секретов не найдено; `<repo>` в evidence является redacted path marker.

Техническая привязка:

* Metadata/manifest: `metadata/audit-package.input.json`, `AUDIT-MANIFEST.md:3-10`, `AUDIT-MANIFEST.md:176-189`
* Snapshot index: `metadata/repo-file-snapshots.json`
* Scope inventory: `AUDIT-MANIFEST.md:13-39`, `repo-file-hashes.json`
* Task contract: `repo-after/TASKS.md:4233-4315`
* Previous verdict: `repo-after/docs/verdicts/release-management/t-0990-audit-r01.md:21-53`
* Profile: `repo-after/data/api/electron2d-public-api-profile.json:1-7`
* Manifest summary/profile source: `repo-after/data/api/electron2d-api-manifest.json:1-28`
* Example unapproved rows: `repo-after/data/api/electron2d-api-manifest.json:52-69`, `repo-after/data/api/electron2d-api-manifest.json:9131-9149`
* Generator: `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:40-67`, `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:155-181`, `repo-after/eng/Electron2D.ApiManifestGenerator/Program.cs:968-1003`
* Build profile reader/gates: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:591-692`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1031-1117`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1204-1226`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1712-1755`
* Wiki generation/checking: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2413-2489`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:2967-2981`, `repo-after/eng/Electron2D.Build/RepositoryWorkflowVerifiers.cs:3536-3616`
* CI API gates: `repo-after/.github/workflows/ci.yml:84-97`
* Tests: `repo-after/tests/Electron2D.Tests.Integration/ApiManifestTests.cs:33-105`, `repo-after/tests/Electron2D.Tests.Integration/Electron2DCliWorkflowTests.cs:422-490`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1754-1782`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1803-1852`
* Evidence:

  * `evidence/T-0990-r02/preflight/focused-profile-tests/command.txt`
  * `evidence/T-0990-r02/preflight/focused-profile-tests/T-0990/r02/focused-profile-tests/stdout.txt`
  * `evidence/T-0990-r02/preflight/focused-profile-tests/T-0990/r02/focused-profile-tests/summary.txt`
  * `evidence/T-0990-r02/preflight/update-api-manifest-empty-profile-fail/T-0990/r02/update-api-manifest-empty-profile-fail/stdout.txt`
  * `evidence/T-0990-r02/preflight/update-api-manifest-empty-profile-fail/T-0990/r02/update-api-manifest-empty-profile-fail/summary.txt`
  * `evidence/T-0990-r02/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r02/verify-api-compatibility-empty-profile-fail/stdout.txt`
  * `evidence/T-0990-r02/preflight/verify-api-compatibility-empty-profile-fail/T-0990/r02/verify-api-compatibility-empty-profile-fail/summary.txt`
  * остальные `summary.txt`/`stdout.txt`/`stderr.txt`/`exit-code.txt` файлы в `evidence/T-0990-r02/preflight/`

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `AUDIT-MANIFEST.md:197-207`; raw evidence summaries для intentional failure checks.
  * Проблема: В `AUDIT-MANIFEST.md` для `update-api-manifest-empty-profile-fail` и `verify-api-compatibility-empty-profile-fail` указан expected exit code `0`, хотя соответствующие `summary.txt` корректно фиксируют `expectedExitCode=1`, `actualExitCode=1`, `passed=true`.
  * Почему не блокирует текущую задачу: Raw evidence достаточно для проверки поведения: intentional failures действительно ожидались и прошли как expected-failure checks. Ошибка в manifest ухудшает качество audit package, но не скрывает текущие blocker-ы и не мешает проверить код, тесты и документацию.
  * Куда перенести: Suggested new task — «Синхронизировать expected exit codes в AUDIT-MANIFEST с preflight evidence summaries». Приоритет P3. Домен: audit packaging/release-management. Критерий приёмки: для каждой preflight check строка в `AUDIT-MANIFEST.md` показывает тот же expected exit code, что и соответствующий `summary.txt`. Идея проверки: автотест audit package builder-а сравнивает manifest checks с evidence summaries и падает на рассинхроне.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Сгенерировать пакет с одной expected-success и одной expected-failure проверкой; автоматической проверкой сравнить `AUDIT-MANIFEST.md` и `evidence/**/summary.txt`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `AUDIT-MANIFEST.md:197-207`
    * `Why not blocker for current task`: raw evidence summaries are correct and sufficient for this audit.

* FOLLOW_UP_FINDING F2

  * Идентификатор: `F2`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1712-1755`, `VerifyGeneratedManifestProfileGate`.
  * Проблема: `update api-manifest --check` читает manual profile и накапливает ошибки validation в `errors`, но затем сначала проходит по manifest types и возвращает `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT`. Если profile одновременно невалиден и runtime экспортирует public types, диагностика схемы/decision/rationale/duplicate/godotReference может не попасть в вывод этой команды.
  * Почему не блокирует текущую задачу: Основной verifier `verify api-compatibility` уже печатает profile validation errors до exported-type gate, а intentional empty-profile failure для `update api-manifest --check` доказан evidence. Это не снимает B2, но отдельный порядок диагностики `update api-manifest` лучше закрывать focused hardening-задачей.
  * Куда перенести: Suggested new task — «Сделать диагностику manual API profile в `update api-manifest` приоритетной перед exported-type gate». Приоритет P3. Домен: build tooling/API manifest. Критерий приёмки: при невалидном `data/api/electron2d-public-api-profile.json` команда `update api-manifest --check` печатает profile validation diagnostics до unapproved export diagnostic или вместе с ним. Идея проверки: fixture с invalid profile и exported type, targeted integration test для `update api-manifest --check`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Добавить тест в `RepositoryBuildToolTests.cs`, который портит manual profile и ожидает соответствующий `E2D-BUILD-API-PROFILE-*` diagnostic от `update api-manifest --check`.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryPolicyVerifiers.cs`, `VerifyGeneratedManifestProfileGate`
    * `Why not blocker for current task`: mandatory empty-profile fail-fast path is proven; richer diagnostic ordering is a hardening gap.

* FOLLOW_UP_FINDING F3

  * Идентификатор: `F3`
  * Где найдено: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1835-1852`; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:663-667`.
  * Проблема: Duplicate `fullName` validation реализована в reader-е, но focused invalid-profile tests не содержат явного regression-case для `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`. Сейчас тестовая теория покрывает invalid decision, empty rationale и missing Godot packet для `approved`.
  * Почему не блокирует текущую задачу: Само поведение duplicate rejection видно в production code через `entries.TryAdd` и diagnostic `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`. Это не основной blocker, потому обнаруженная ошибка B2 относится к отсутствующей реализации проверки для части decisions, а не только к тестовому покрытию.
  * Куда перенести: Suggested new task — «Добавить focused regression для duplicate `fullName` в manual API profile verifier». Приоритет P3. Домен: build tooling/API compatibility tests. Критерий приёмки: `verify api-compatibility --wiki-path .github/wiki` на fixture с двумя строками одного `fullName` падает с `E2D-BUILD-API-PROFILE-DUPLICATE-TYPE`. Идея проверки: отдельный `[Fact]` или `[InlineData]` в `RepositoryBuildToolTests`.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: Запустить targeted test filter `FullyQualifiedName~VerifyApiCompatibilityRejectsInvalidManualProfileRows` или новый focused duplicate-test filter.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `File/symbol`: `RepositoryBuildToolTests.cs:1835-1852`, `RepositoryPolicyVerifiers.cs:663-667`
    * `Why not blocker for current task`: duplicate rejection is implemented; missing explicit regression is a test-hardening gap.

CLOSURE_DECISION:

* Задача `T-0990` остаётся открытой. Пакет r02 нельзя принять, пока не будет полностью закрыт прошлый documentation/Wiki blocker и пока profile verifier не будет согласован с заявленным контрактом `godotReference` validation для manual profile decisions.
* Для следующей итерации нужны исправленные docs/generated Home/verifier tests, targeted profile validation tests для `godotReference` на `deferred`/`unsupported`, повтор focused profile tests и обновлённые intentional empty-profile failure evidence.
