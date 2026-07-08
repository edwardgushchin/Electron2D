VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0990` итерации `r09` по полным файлам из `repo-after/`, metadata, manifest, snapshots, patch и evidence. Область пакета одиночная: внедрение ручного пустого профиля публичного API как источника решений о включении API, перевод manifest/Wiki/local-docs/build gates на этот профиль, а также hardening audit tooling для CDP recovery и строгого secret scanner.
* Основная реализация ручного профиля в целом соответствует заявленному направлению: добавлен `data/api/electron2d-public-api-profile.json`, manifest теперь ссылается на него, все текущие экспортированные типы помечены как `unapproved`, verifier-ы и CI остаются fail-closed до явного owner approval. Изменения CDP recovery и package secret scanner имеют покрытие в тестах и evidence.
* Принять текущий пакет нельзя: в изменённой области остался интеграционный тест, который теперь детерминированно расходится с новым порядком проверки `verify api-compatibility`. Новый manual-profile gate возвращает ошибку `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT` раньше проверки forbidden legacy public type, а тест продолжает ожидать `E2D-BUILD-API-COMPATIBILITY-FORBIDDEN-TYPE`. Это не гипотеза о внешней среде, а противоречие между кодом verifier-а и тестом в текущем `repo-after/`.

Техническая привязка:

* `metadata.taskId`: `T-0990`
* `metadata.iteration`: `r09`
* `metadata.scopeTaskIds`: `["T-0990"]`
* `metadata.scopeSummary`: ручной пустой public API profile как источник решений, fail-closed manifest/Wiki/docs/build gates, hardening CDP recovery и package scanner.
* `metadata.previousVerdictChain`: `[]`
* `metadata.blockerClosureList`: `[]`
* Проверенные материалы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0990.patch`, `repo-before/`, `repo-after/`, `evidence/`.
* Ключевые проверенные файлы: `.github/workflows/ci.yml`, `data/api/electron2d-api-manifest.json`, `data/api/electron2d-public-api-profile.json`, `eng/Electron2D.ApiManifestGenerator/Program.cs`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs`, `eng/Electron2D.Build/AuditPackageCommand.cs`, `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `eng/Electron2D.Build/LocalDocumentationVerifier.cs`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `tests/Electron2D.Tests.Integration/LocalDocumentationCliTests.cs`, `tests/Electron2D.Tests.Integration/Electron2DCliWorkflowTests.cs`, документация в `docs/documentation/`, `docs/release-management/`, `docs/releases/`, `docs/rendering/`, `docs/scripting/`.

BLOCKERS:

* B1

  * Что не так: проверка `verify api-compatibility` теперь сначала запускает gate ручного public API profile и при первом неутверждённом экспортированном типе сразу возвращает ошибку. При этом тест `VerifyApiCompatibilityRejectsForbiddenLegacyPublicType` создаёт manifest с `Electron2D.IComponent`, но не добавляет этот тип в ручной профиль. Поэтому verifier не доходит до проверки forbidden legacy type, хотя тест продолжает ожидать диагностический код forbidden-type.
  * Почему это важно: текущая задача меняет именно build-tool gates вокруг public API compatibility. Нельзя принять пакет, в котором изменённый verifier и изменённый интеграционный тест описывают разные контракты поведения. Это означает, что полный набор проверок текущей области содержит сломанную проверку либо потерял наблюдаемое покрытие для forbidden legacy public types.
  * Что исправить: нужно синхронизировать контракт verifier-а и тестов. Предпочтительный вариант — сохранить наблюдаемую forbidden-type диагностику для запрещённых legacy exports, например проверять forbidden types до раннего возврата profile gate или собирать обе ошибки. Альтернативно, если новый контракт намеренно отдаёт приоритет manual-profile gate, нужно обновить тест и добавить отдельную проверку, где forbidden type имеет валидную запись ручного профиля и verifier действительно достигает forbidden-type ветки.
  * Как проверить исправление: запустить точечный тест на forbidden legacy type вместе с profile-gate тестами и затем штатную проверку совместимости API без ручных обходов.
  * Проверка опровержения: проверены fixture, тест, порядок вызовов verifier-а и evidence. В `CreateApiWikiFixture` ручной профиль содержит только `Electron2D.CharacterBody2D`; тест затем заменяет manifest на `Electron2D.IComponent`, но не обновляет профиль. `ApiCompatibilityVerifier.Verify` вызывает `VerifyManualProfileGate` и возвращает ошибку до `VerifyCompatibilityPage`, где находится forbidden-type проверка. Evidence `focused-profile-tests` и `audit-loop-stabilization` не запускает `VerifyApiCompatibilityRejectsForbiddenLegacyPublicType` и не опровергает этот разрыв.
  * Техническая привязка:

    * `File/symbol`: `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:2247-2282`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:18245-18360`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1038-1083`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1155-1208`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1211-1233`.
    * `Criterion`: `test coverage review`, `task compliance review`, `observable behavior`, `architecture coherence` для build-tool API compatibility gate.
    * `Evidence`: тест `VerifyApiCompatibilityRejectsForbiddenLegacyPublicType` ожидает `E2D-BUILD-API-COMPATIBILITY-FORBIDDEN-TYPE`; fixture не включает `Electron2D.IComponent` в `data/api/electron2d-public-api-profile.json`; `VerifyManualProfileGate` возвращает `E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT` до вызова forbidden-type проверки в `VerifyCompatibilityPage`.
    * `Impact`: пакет нельзя считать зелёным и самосогласованным в текущей области, потому что изменённая интеграционная проверка либо падает, либо не доказывает сохранение forbidden-type gate.
    * `Fix`: изменить порядок/агрегацию диагностик verifier-а или обновить тестовый контракт с отдельным достижимым покрытием forbidden-type ветки.
    * `Verification`: `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~VerifyApiCompatibilityRejectsForbiddenLegacyPublicType|FullyQualifiedName~VerifyApiCompatibilityRejectsExportedTypeMissingFromManualProfile|FullyQualifiedName~VerifyApiCompatibilityRejectsInvalidManualProfileRows" --no-restore -v:minimal`; затем `dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki`.

EVIDENCE_REVIEW:

* Проверены metadata и область пакета. `metadata.scopeTaskIds` содержит только `T-0990`, поэтому combined scope отсутствует. `metadata.previousVerdictChain` и `metadata.blockerClosureList` пустые, прошлые blocker-ы для закрытия в текущем архиве не заявлены.
* Проверена полнота снимков. Для всех 25 файлов из `metadata/repo-file-snapshots.json` присутствуют полные итоговые snapshots, доступные файлы в `repo-after/`, а hashes согласованы с `repo-file-hashes.json`. Проверка не ограничивалась patch-only inspection.
* Проверена реализация ручного public API profile. `data/api/electron2d-public-api-profile.json` содержит пустой список `types`, manifest ссылается на этот профиль, а текущие экспортированные типы и members в generated manifest помечены как `unapproved`. Build-tool verifier-ы читают ручной профиль, валидируют обязательные поля, решения `approved/deferred/unsupported`, rationale и существование Godot API packet для указанного `godotReference`.
* Проверена документация. Документы по API manifest, GitHub Wiki API reference, local documentation pipeline, release management API compatibility, audit package, CI matrix и release preview обновлены под ручной профиль и fail-closed модель. Отдельная несогласованность CLI-семантики вынесена в последующее замечание F1.
* Проверены тесты и evidence. Evidence показывает успешные focused profile tests, audit loop stabilization, CDP reattach stabilization, build-tool build, update-docs check, verify-docs, verify-ci-matrix, verify-licenses, verify-audit-contracts, previous-verdict placeholder scanner, git-diff-check, а также ожидаемые fail-closed проверки empty profile для `update api-manifest` и `verify api-compatibility`. Эти evidence не закрывают B1, потому что не запускают сломанный forbidden legacy public type тест.
* Проверены секреты, локальные данные и лишние правки. Реальных токенов, приватных ключей, паролей или конфиденциальных локальных путей в изменённых материалах не найдено; совпадения относятся к тестовым фикстурам, redacted placeholders и sanitizer-аудиту. Изменения находятся в области release-management/API tooling/docs/tests и не затрагивают runtime hot path игрового цикла.

Техническая привязка:

* Metadata/snapshots: `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`.
* Реализация API profile и manifest: `data/api/electron2d-public-api-profile.json`, `data/api/electron2d-api-manifest.json`, `eng/Electron2D.ApiManifestGenerator/Program.cs`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs`.
* Local docs verifier: `eng/Electron2D.Build/LocalDocumentationVerifier.cs`, `data/documentation/electron2d-local-docs-index.json`, `data/documentation/local-docs-index/documentation.ndjson`.
* Audit tooling hardening: `eng/Electron2D.Build/AuditPackageCommand.cs`, `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`.
* Tests: `tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `tests/Electron2D.Tests.Integration/Electron2DCliWorkflowTests.cs`, `tests/Electron2D.Tests.Integration/LocalDocumentationCliTests.cs`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`.
* Документация: `docs/documentation/api-manifest.md`, `docs/documentation/github-wiki-api-reference.md`, `docs/documentation/local-documentation-pipeline.md`, `docs/release-management/api-compatibility.md`, `docs/release-management/audit-package.md`, `docs/release-management/ci-matrix.md`, `docs/releases/0.1-preview.md`, `docs/rendering/texture-resource-baseline.md`, `docs/scripting/csharp-script-classes.md`.
* Evidence artifacts: `evidence/focused-profile-tests/result.txt`, `evidence/audit-loop-stabilization/result.txt`, `evidence/audit-submit-reattach-stabilization/result.txt`, `evidence/build-tool-build/result.txt`, `evidence/update-docs-check/result.txt`, `evidence/verify-docs/result.txt`, `evidence/verify-ci-matrix/result.txt`, `evidence/verify-licenses/result.txt`, `evidence/verify-audit-contracts/result.txt`, `evidence/previous-verdict-placeholder-scanner/result.txt`, `evidence/git-diff-check/result.txt`, `evidence/update-api-manifest-empty-profile-fail/result.txt`, `evidence/verify-api-compatibility-empty-profile-fail/result.txt`.

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1590`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1670`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1865-1878`, `eng/Electron2D.Build/Program.cs:70`, `docs/documentation/api-manifest.md:159-167`.
  * Проблема: документация и usage теперь описывают `update api-manifest` как команду без `--wiki-path`, потому что Wiki больше не является source input для manifest. Но parser всё ещё принимает `--wiki-path`, а `GenerateManifestAsync` всё ещё имеет неиспользуемый параметр `wikiPath`.
  * Почему не блокирует текущую задачу: фактическая генерация manifest уже не читает Wiki и использует ручной profile; CI и документация используют чистую команду без `--wiki-path`; fail-closed проверки empty profile подтверждены evidence. Это устаревший совместимый CLI-хвост и неоднозначность интерфейса, но не нарушение основного поведения текущего изменения.
  * Куда перенести: новая задача — «Remove stale `--wiki-path` compatibility from `update api-manifest`». Рекомендуемый домен: release-management/API manifest tooling. Критерий приёмки: `update api-manifest --wiki-path <path>` явно отклоняется с usage, который совпадает с документацией; `ApiManifestArguments` больше не хранит `WikiPath`; `GenerateManifestAsync` больше не принимает неиспользуемый `wikiPath`; тесты покрывают штатную команду и отказ от устаревшего аргумента.
  * Рекомендуемый приоритет: `P2`.
  * Как проверить: добавить тест наподобие `UpdateApiManifestRejectsWikiPath`; запустить `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~UpdateApiManifestRejectsWikiPath --no-restore -v:minimal`; вручную проверить, что `dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki` завершается ошибкой usage, а `dotnet run --project eng/Electron2D.Build -- update api-manifest --check` остаётся штатным путём.
  * Техническая привязка:

    * `File/symbol`: `ApiManifestCommand.Parse`, `ApiManifestCommand.GenerateManifestAsync`, `Program.ShowUsage`.
    * `Why not blocker for current task`: генерация уже profile-driven и не зависит от Wiki; это cleanup публичного CLI-интерфейса.
    * `Suggested new task`: `Remove stale --wiki-path compatibility from update api-manifest`.
    * `Suggested priority`: `P2`.
    * `Verification idea`: тест на отказ от устаревшего аргумента плюс проход текущих docs/CI verifiers.

CLOSURE_DECISION:

* Задача остаётся открытой до исправления B1. Текущий пакет доказывает основную fail-closed интеграцию ручного public API profile и hardening audit tooling, но содержит доказуемую несогласованность между новым verifier control flow и изменённым интеграционным тестом forbidden legacy public type. После исправления нужно повторно проверить полный текущий scope, включая targeted API compatibility tests и штатные evidence-команды без ручных обходов.
