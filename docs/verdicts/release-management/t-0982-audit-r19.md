VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен audit ZIP `T-0982` итерации `r19` как одиночная область задачи. Архив читается, `repo-after/` доступен, `metadata/repo-file-snapshots.json` содержит полные снимки изменённых файлов, а `repo-file-hashes.json` согласован с итоговыми файлами.
* Область пакета заявлена как закрытие sanitizer-блокеров r01-r18: `metadata.json` и complete compact JSON raw snippets должны нормализоваться структурно по decoded JSON string values; literal slash, escaped solidus `\/` и unicode escaped solidus `\u002F` exact repo-root/safe child paths должны становиться `<repo>`; JSON string content, traversal values, external same-line JSON-looking tails и non-JSON raw path-token cases должны fail closed.
* r19 закрывает конкретный r18 blocker про escaped solidus / unicode escaped solidus repo-root внутри complete compact JSON snippet. Но реализацию нельзя принять: class-level invariant всё ещё имеет два доказуемых fail-open случая. Первый скрывает repo-root внутри compact JSON snippet, встроенного в более длинный POSIX path-token. Второй пропускает Windows drive absolute path, если drive colon закодирован как JSON unicode escape внутри raw JSON string literal вне complete object/array snippet.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r19`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: close r01-r18 sanitizer blockers with structural preflight metadata/JSON snippet normalization, fail-closed non-JSON raw text, and final validation for literal paths, reversible JSON path encodings and decoded JSON string literals.
* `combined scope`: не используется, область одиночная.
* Проверенные основные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`, `repo-after/data/dev-diary/2026/07 Июль/08-07-2026.md`.
* Проверенные previous verdict files: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r18.md`.
* Проверенные evidence: `evidence/T-0982-r19/preflight/*`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`.

BLOCKERS:

* B1

  * Что не так: complete compact JSON snippet нормализуется даже тогда, когда сам snippet встроен внутрь более длинного POSIX path-token после path separator-а. Например, на POSIX при `repoRoot = /home/user/repo` raw preflight `output.txt` может содержать строку `embedded path: /opt/e2d{"repositoryRoot":"/home/user/repo"}`. Текущий код найдёт `{...}` начиная с `{`, распарсит его как complete JSON snippet, заменит decoded string value на `<repo>` и запишет `embedded path: /opt/e2d{"repositoryRoot":"<repo>"}`. После этого финальная проверка уже не увидит `/home/user/repo`.
  * Почему это важно: исходная строка содержит локальный repo-root внутри POSIX absolute path-token. Для non-JSON raw text уже есть правило fail-closed: path separator перед opening delimiter/candidate на той же строке делает replacement недопустимым. Новый JSON-snippet путь обходит это правило, потому `SanitizePreflightEvidenceTextSegments` проверяет только правую границу snippet-а и не проверяет, что `{`/`[` сам не встроен в path-token.
  * Что исправить: перед structural normalization complete JSON snippet-а нужно проверять левый контекст snippet-а. Если перед `{` или `[` на той же строке есть path separator внутри текущего token-а, snippet нельзя нормализовать структурно; он должен оставаться raw и затем падать на `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. Позитивные prose cases вроде `compact object root: {"repositoryRoot":"<repo>"}` должны продолжать проходить, потому там перед `{` нет path-token с separator-ом.
  * Как проверить исправление: добавить focused regression через production `audit package` на POSIX/non-Windows. Imported preflight `output.txt` должен содержать `embedded path: /opt/e2d{"repositoryRoot":"{slashRepoRoot}"}` и вариант с escaped solidus/unicode escaped solidus внутри того же embedded JSON snippet-а. Ожидаемый результат — отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не ZIP с `/opt/e2d{"repositoryRoot":"<repo>"}`. Позитивные cases `compact object root: {"repositoryRoot":"{slashRepoRoot}"}` и `compact array root: ["{slashRepoRoot}"]` должны остаться зелёными.
  * Проверка опровержения: проверены реализация, тесты, документация, `metadata.blockerClosureList`, previous reports и r19 evidence. Тесты `AuditPackageRejectsPreflightEvidenceEmbeddedRepoRootStartBoundary`, `AuditPackageRejectsPreflightEvidenceOpeningDelimiterEmbeddedRepoRootStartBoundary`, `AuditPackageRejectsPreflightEvidenceStructuralDelimiterEmbeddedRepoRootStartBoundary` покрывают raw root occurrences внутри path-token, но не complete JSON snippet, встроенный после path separator-а. Тест `AuditPackageSanitizesExactPreflightEvidenceRepositoryRootTokens` доказывает positive compact JSON snippets с prose prefix, но не path-token prefix. Evidence `t0982-r19-focused-sanitizer-tests` и `audit-loop-stabilization` подтверждают текущие 26/26 и 31/31, но этот сценарий в них отсутствует.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`; `SanitizePreflightEvidenceTextSegments`; `IsCompleteJsonSnippetLine`; `NormalizePreflightMetadataNode`; `ValidateMachineLocalPathText`.
    * `Line range`: `AuditPackageCommand.cs` lines `1437-1474`, `1506-1510`, `1345-1367`, `4837-4840`, `4909-4919`.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/docs/release-management/audit-package.md` lines `453-457`; `repo-after/TASKS.md` lines `3798-3801`.
    * `Evidence`: snippet discovery starts at any `{` or `[` without left-boundary validation; `IsCompleteJsonSnippetLine` checks only text after `snippetEnd`; complete snippet values are normalized structurally before final archive validation.
    * `Impact`: imported preflight evidence can hide a local repo-root that originally appeared inside a longer POSIX absolute path-token.
    * `Fix`: add safe left-boundary/path-token guard for protected JSON snippets, or fail closed for embedded snippets when a path separator exists earlier in the same token.
    * `Verification`: focused sanitizer regression must fail before the fix and pass after it; `audit-loop-stabilization` should include the new embedded JSON-snippet case.

* B2

  * Что не так: финальная проверка decoded JSON string literals не применяет общий Windows drive absolute path detector к decoded value. Raw text вроде `encoded drive path: "G\u003A/local/copied-task.md"` содержит JSON string literal, который декодируется в `G:/local/copied-task.md`. Текущий `ValidateMachineLocalPathText` не блокирует такой текст: raw `WindowsDrivePathPattern` не видит literal `G:`, `NormalizeJsonPathEscapesForMachinePathScan` не декодирует `\u003A`, а `ContainsDecodedJsonStringMachineLocalPath` проверяет decoded value только против repo-root/temp candidates, но не против Windows drive pattern.
  * Почему это важно: документация r19 утверждает, что финальная проверка ищет decoded JSON string literals и что нерепозиторные машинные пути остаются блокерами. Literal Windows drive paths уже блокируются отдельным тестом и regex-ом; тот же путь не должен проходить только из-за JSON unicode escape для colon. Это позволяет передать в audit ZIP локальный absolute path в обратимо декодируемом виде.
  * Что исправить: после `TryReadJsonStringLiteral` нужно проверять decoded value не только на `machineLocalPathCandidates`, но и на Windows drive absolute path pattern, желательно после нормализации backslash-to-slash. Дополнительно можно расширить `NormalizeJsonPathEscapesForMachinePathScan` на `\u003A`/`\u003a`, но основная защита должна быть на decoded string value.
  * Как проверить исправление: добавить focused test для package и verify: текстовый artifact или archive entry содержит `encoded drive path: "G\u003A/local/copied-task.md"` и `encoded drive path: "G\u003A\u005Clocal\u005Ccopied-task.md"`. Ожидаемый результат — `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. Существующий positive case `D:\u0022` после drive-like punctuation должен остаться разрешённым, потому он не является decoded drive path.
  * Проверка опровержения: проверены tests и evidence. `AuditPackageRejectsWindowsDrivePathsInArchiveContent` и `AuditPackageVerifyRejectsWindowsDrivePathsInArchiveContent` покрывают literal `G:/...` / `G:\...`, но не JSON unicode escaped colon. `AuditPackageRejectsPreflightEvidenceJsonEscapedRepositoryRootTokens` покрывает escaped repo-root candidates, а не arbitrary non-repository Windows drive paths. `AuditPackageAllowsJsonEscapedQuotedTextAfterDriveLikePunctuation` покрывает false-positive guard для `D:\u0022`, но не decoded `G\u003A/...` absolute path. Документация не снимает blocker: она прямо заявляет decoded JSON string literal scanning и сохранение нерепозиторных machine paths как blockers.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`; `WindowsDrivePathPattern`; `ValidateMachineLocalPathText`; `NormalizeJsonPathEscapesForMachinePathScan`; `ContainsDecodedJsonStringMachineLocalPath`; `TryReadJsonStringLiteral`.
    * `Line range`: `AuditPackageCommand.cs` lines `73-75`, `4909-4920`, `4923-4926`, `4928-4948`, `4950-4989`.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/docs/release-management/audit-package.md` line `457`; mandatory `secret scanning` / local absolute path audit criterion.
    * `Evidence`: decoded JSON string values are checked only by `machineLocalPathCandidates.Any(candidate => normalizedValue.Contains(candidate, ...))`; `WindowsDrivePathPattern.IsMatch` is applied only to raw `text`.
    * `Impact`: a Windows absolute path can be included in text evidence or archive content in reversible JSON-escaped form while package validation succeeds.
    * `Fix`: apply Windows drive path detection to decoded JSON string values, and cover escaped-colon drive paths in package and verify tests.
    * `Verification`: targeted regression must fail before the fix and pass after; existing literal-drive and false-positive escaped-quote tests must remain green.

EVIDENCE_REVIEW:

* Полнота архива проверена. ZIP entries читаются, `SHA256SUMS.txt` согласован при UTF-8-aware extraction, `repo-file-hashes.json` соответствует `repo-after/`, а `metadata/repo-file-snapshots.json` содержит `fullContentIncluded: true` для всех 25 изменённых файлов. Недостатка снимков, который мешал бы читать реализацию, тесты или документацию, не найдено.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, не только по patch. Проверен путь `SelectPreflightCheckEvidenceFiles` → `PreparePreflightEvidenceSource` → `WriteNormalizedPreflightMetadata` / `SanitizePreflightEvidenceText` → `SanitizePreflightEvidenceTextSegments` → `NormalizePreflightMetadataNode` / `ReplaceRepoRootPathCandidates` → `ValidateSecretPolicy` → `ValidateArchiveFiles` / `ValidateMachineLocalPathText`.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Набор r19 покрывает readable sanitizer output, placeholder-per-character rejection, sibling prefix, punctuation, traversal, spaced traversal, case-sensitive mismatch, exact quoted roots, compact JSON object/array/result positives, escaped solidus/unicode escaped solidus repo-root positives, JSON string-content negatives, JSON-prefix external tails, quoted structural tails, embedded raw root boundaries, whitespace boundaries, POSIX sibling tokens, parent traversal root token and previous-verdict reviewer scanner boundaries. Покрытия для B1 embedded complete JSON snippet after path separator and B2 JSON-unicode-escaped Windows drive colon нет.
* Документация проверена по `repo-after/docs/release-management/audit-package.md` и generated docs index. Документ описывает structural metadata/compact JSON normalization, fail-closed raw string content, external same-line tail rejection, non-JSON whole path-token rules and final validation for literal paths, reversible JSON path encodings and decoded JSON string literals. B1 и B2 противоречат этой documented fail-closed модели.
* Previous verdict files r01-r18 прочитаны. `metadata.previousVerdictChain` указывает все доступные отчёты, а `metadata.blockerClosureList` содержит 26 closure-записей для прошлых blocker-ов. Конкретный r18 blocker про escaped solidus/unicode escaped solidus repo-root внутри compact JSON закрыт новым/расширенным покрытием `AuditPackageRejectsPreflightEvidenceJsonEscapedRepositoryRootTokens` и `AuditPackageSanitizesExactPreflightEvidenceRepositoryRootTokens`. r18 `FOLLOW_UP_FINDING F1` про dead tail-parser helpers закрыт удалением старых helper-ов и evidence `t0982-r19-dead-tail-helper-search`.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `26/26`, previous-verdict reviewer scanner tests `5/5`, audit-loop-stabilization `31/31`, sanitizer fixture, dead-tail-helper search, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`. Эти evidence подтверждают выполненный набор проверок, но не покрывают B1 и B2.
* Проверка области не выявила отдельного scope blocker: изменения ограничены release-management audit-package tool, integration tests, domain documentation, generated docs index, task card, dev diary and saved previous reports. Public API, runtime behavior, renderer, gameplay hot path and Godot 4.7 API surface не затрагиваются.
* Проверка секретов и локальных данных в текущих файлах не выявила реальных токенов, приватных ключей или паролей. Синтетические Windows/POSIX paths находятся в тестах, документации, patch and saved previous reports as examples. Текущие evidence outputs используют `<repo>` placeholders.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r18.md`
* `previous blockers closure`: r01 `B1`; r02 `B1`; r03 `B1`; r04 `B1`; r05 `B1`; r06 `B1`/`B2`; r07 `B1`; r08 `B1`/`B2`; r09 `B1`/`B2`; r10 `B1`/`B2`; r11 `B1`/`B2`; r12 `B1`/`B2`; r13 `B1`/`B2`; r14 `B1`/`B2`; r15 `B1`; r16 `B1`; r17 `B1`; r18 `B1` checked.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r19/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: no snapshot gap; behavioral and implementation gaps for B1 and B2.

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. r19 заметно улучшает sanitizer и закрывает r18 escaped-root blocker, но текущая class-level модель всё ещё может скрыть repo-root внутри embedded compact JSON path-token и пропустить decoded Windows drive absolute path через JSON-unicode-escaped colon. До исправления B1 и B2 audit package нельзя считать безопасным переносимым артефактом для внешнего аудита.
