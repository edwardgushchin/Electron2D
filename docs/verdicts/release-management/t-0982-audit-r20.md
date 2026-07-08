VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен audit ZIP `T-0982` итерации `r20` как одиночная область задачи. Архив читается, `repo-after/` доступен, `metadata/repo-file-snapshots.json` содержит полные снимки всех изменённых файлов, а контрольные суммы архива согласованы при чтении ZIP с корректной UTF-8 обработкой имён файлов.
* Область пакета заявлена как закрытие sanitizer-блокеров r01-r19: `metadata.json` и защищённые complete compact JSON snippets должны нормализоваться структурно только при доказанной левой границе token-а и отсутствии внешнего same-line tail; raw text должен заменять только whole path-token repo-root или safe child paths; embedded JSON snippets, JSON string content, traversal values, external tails и Windows drive paths должны fail closed.
* r20 закрывает конкретные r19 B1/B2: добавлен left-boundary guard для compact JSON snippets после path separator-а и decoded classifier для JSON-escaped Windows drive paths. Но реализацию нельзя принять: при разбиении raw preflight text на JSON snippets и raw segments код теряет оригинальный контекст слева/справа от parseable, но незащищённого JSON snippet-а. Из-за этого repo-root может быть заменён как “конец сегмента” или “начало сегмента”, хотя в исходной строке он был встроен в более длинный path-token или external tail. Дополнительно Windows drive classifier всё ещё имеет слишком широкий false-positive guard для `\u0022`/`\u0027`, который пропускает реальные drive-root paths с первым сегментом `u0022` или `u0027`.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r20`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: закрытие r01-r19 sanitizer blockers через unified JSON token and machine-path gate, включая fail-closed handling для embedded snippets, JSON string content, traversal values, external tails, decoded/raw machine paths and Windows drive paths.
* `combined scope`: не используется, область одиночная.
* Проверенные основные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, дневниковые записи за `07-07-2026` и `08-07-2026`.
* Проверенные previous verdict files: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r19.md`.
* Проверенные evidence: `evidence/T-0982-r20/preflight/*`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`.

BLOCKERS:

* B1

  * Что не так: sanitizer делит raw preflight text на raw segments и parseable JSON snippets, но raw replacement выполняется на сегментах уже без исходного соседнего символа. Если snippet parseable, но не является защищённым complete JSON token-ом, он всё равно становится границей сегментов. Поэтому строка вида `/home/user/repo{"ok":true}` при POSIX `repoRoot = /home/user/repo` превращается в `<repo>{"ok":true}`: raw segment до `{` заканчивается ровно на repo-root, и `ReplaceRepoRootPathCandidate` принимает конец подстроки как безопасный конец token-а. В исходном тексте это не был exact repo-root token: `{` является допустимым символом POSIX filename segment-а, а весь путь указывает на sibling leaf `repo{"ok":true}`.
  * Почему это важно: текущая задача требует fail-closed handling для embedded JSON snippets и non-JSON raw path-token cases. В этом случае исходный нерепозиторный absolute path исчезает до `ValidateMachineLocalPathText`; итоговый ZIP содержит `<repo>{"ok":true}` и больше не содержит `/home/user/repo`. Такой пакет выглядит переносимым, хотя raw preflight evidence содержал machine-local absolute path вне доказанного repo-root/safe-child token-а.
  * Что исправить: не применять raw replacement к сегментам, отрезанным от parseable, но незащищённого JSON snippet-а, без исходного контекста. Минимально: если snippet не прошёл `IsCompleteJsonSnippetToken`, весь соседний path-token должен обрабатываться как единая raw surface, а не как три независимые части. Альтернатива: `AppendSanitizedRawPreflightSegment` должен получать исходный символ справа/слева и запрещать replacement, если candidate был бы небезопасен в полном тексте.
  * Как проверить исправление: добавить focused regression через production `audit package`. На POSIX/non-Windows `output.txt` должен содержать `/home/user/repo{"ok":true}` и `/home/user/repo[0]`; ожидаемый результат — отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не ZIP с `<repo>{"ok":true}` или `<repo>[0]`. Отдельно проверить traversal-вариант `/home/user/repo/dir{"ok":true}/../../repo backup/logs.txt`: он тоже должен fail closed, потому JSON snippet не должен обрывать traversal scan path-token-а.
  * Проверка опровержения: проверены реализация, tests, docs, `metadata.blockerClosureList`, previous reports и r20 evidence. Тест `AuditPackageRejectsPreflightEvidenceEmbeddedJsonSnippetPathTokens` покрывает JSON snippet после `/opt/e2d` с repo-root внутри самого snippet-а, но не repo-root непосредственно перед parseable snippet-ом. Тесты embedded start-boundary покрывают raw repo-root occurrences внутри path-token-а, но не случай, где tokenizer разрезает token перед `{` или `[`. Evidence `t0982-r20-focused-sanitizer-tests` и `audit-loop-stabilization` подтверждают текущие 27/27 и 38/38, но этот сценарий в них отсутствует.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`; `SanitizePreflightEvidenceTextSegments`; `AppendSanitizedRawPreflightSegment`; `ReplaceRepoRootPathCandidate`; `IsRepoRootPathCandidateBoundary`; `ValidateMachineLocalPathText`.
    * `Line range`: `AuditPackageCommand.cs` lines `1446-1483`, `1486-1499`, `1620-1662`, `1664-1693`, `1859-1883`, `4951-4961`.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/docs/release-management/audit-package.md` lines `453-457`; `repo-after/TASKS.md` lines `3761-3775` and `3798-3801`.
    * `Evidence`: `SanitizePreflightEvidenceTextSegments` always calls `AppendSanitizedRawPreflightSegment` before deciding whether the snippet is a protected complete token. For an unprotected snippet, the code appends the raw snippet unchanged, sets `lastRawStart = snippetEnd`, and later sanitizes the following raw segment independently. `ReplaceRepoRootPathCandidate` treats `end >= text.Length` and `start == 0` inside the segment as safe boundaries, so original cross-boundary path-token context is lost.
    * `Impact`: imported preflight evidence can hide sibling paths and traversal paths that are only unsafe when the full raw token is considered.
    * `Fix`: preserve full-token context across unprotected snippets or fail closed by skipping raw replacement adjacent to unprotected parseable snippets.
    * `Verification`: new focused sanitizer tests must fail before the fix and pass after; `audit-loop-stabilization` should include the new cross-boundary snippet cases.

* B2

  * Что не так: тот же segment-splitting bug работает справа от незащищённого JSON snippet-а. Например, строка `{"ok":true}/home/user/repo` не является complete JSON snippet token-ом из-за external same-line tail. Текущий код оставляет snippet raw, но затем sanitizes tail `/home/user/repo` как отдельный raw segment starting at index `0`; `ReplaceRepoRootPathCandidate` принимает начало подстроки как safe start-boundary и заменяет tail на `<repo>`. В исходном полном тексте repo-root tail не имел безопасной start-boundary: перед ним стоял `}`, а не начало строки, whitespace или opening delimiter.
  * Почему это важно: r20 scope и документация прямо требуют, чтобы JSON-looking prefix with external same-line tail не нормализовался частично и оставался под финальной проверкой локальных путей. Текущая реализация частично нормализует external tail, если tail содержит repo-root. Это делает fail-closed правило неполным.
  * Что исправить: если snippet не является protected complete JSON token-ом, raw replacement справа от него должен учитывать оригинальный символ перед tail. Для external same-line tail безопаснее рассматривать весь участок от snippet start до конца текущего path-like token-а как raw protected text без replacement, чтобы финальная проверка увидела исходный локальный путь.
  * Как проверить исправление: добавить focused regression с raw preflight `output.txt`: `{"ok":true}/home/user/repo`, `{"ok":true}/home/user/repo/logs.txt`, `[0]/home/user/repo` и JSON-prefix variant с escaped/unicode repo-root tail, если такой tail поддерживается как reversible local path representation. Ожидаемый результат — `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. Позитивные cases `compact object root: {"repositoryRoot":"{slashRepoRoot}"}` и `compact array root: ["{slashRepoRoot}"]` должны остаться зелёными.
  * Проверка опровержения: проверены тесты `AuditPackageRejectsPreflightEvidenceJsonPrefixStructuralTailTokens`, `AuditPackageRejectsPreflightEvidenceEmbeddedJsonSnippetPathTokens` и positive compact JSON tests. Первый тест покрывает repo-root внутри JSON prefix и structural tail после prefix-а, но не local path в external tail. Второй тест покрывает path-token prefix before JSON snippet, но не local path tail after unprotected snippet. Документация не снимает blocker: она требует fail closed для external same-line tail и final machine-path validation.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`; `SanitizePreflightEvidenceTextSegments`; `IsCompleteJsonSnippetToken`; `IsCompleteJsonSnippetLine`; `AppendSanitizedRawPreflightSegment`; `IsRepoRootPathCandidateStartBoundary`.
    * `Line range`: `AuditPackageCommand.cs` lines `1446-1483`, `1515-1552`, `1486-1499`, `1859-1883`.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/docs/release-management/audit-package.md` lines `453` and `457`.
    * `Evidence`: for unprotected snippets the implementation appends the snippet unchanged and then calls `AppendSanitizedRawPreflightSegment` on text after `snippetEnd`; the right segment no longer knows that its first character was preceded by `}` or `]` in the original line.
    * `Impact`: external same-line tail can be normalized away instead of blocked, which defeats the r16/r20 fail-closed invariant for JSON-prefix tails.
    * `Fix`: carry left context into right-segment replacement or stop splitting unprotected snippets into independently sanitized raw substrings.
    * `Verification`: new regression should fail on current code and pass only when the external-tail local path remains visible to `ValidateMachineLocalPathText`.

* B3

  * Что не так: Windows drive path detector still exempts `\u0022` and `\u0027` too broadly. Pattern `\b[A-Z]:(?:/|\\(?!(?:\\)?u0022\b|(?:\\)?u0027\b))` intentionally allows drive-like escaped quote examples such as `D:\u0022`, but it also allows real Windows absolute paths where `u0022` or `u0027` is the first directory segment, for example `G:\u0022\local\copied-task.md` or JSON string literal `"G:\\u0022\\local\\copied-task.md"`. These are valid drive-root path forms to a directory named `u0022`, but the negative lookahead suppresses the drive-path match because `u0022` is followed by a word boundary before the next separator.
  * Почему это важно: текущая область r20 прямо заявляет unified machine-path classifier для Windows drive absolute paths and JSON escaped/backslash drive strings, while preserving only the drive-like escaped quote false-positive guard. The guard should not allow a path with additional path separators after the escaped-quote-like segment. Иначе audit ZIP может содержать локальный Windows absolute path in reversible raw or JSON string form.
  * Что исправить: сузить false-positive guard. Допускать `D:\u0022` / `D:\u0027` только как isolated drive-like punctuation example, например перед концом строки, whitespace или безопасной non-path punctuation. Если после `u0022`/`u0027` идёт `/` или `\`, это должен быть blocked Windows drive path. Для decoded JSON string values нужно применять тот же classifier.
  * Как проверить исправление: добавить package and verify tests для `G:\u0022\local\copied-task.md`, `G:\u0027\local\copied-task.md`, `"G:\\u0022\\local\\copied-task.md"` и `"G:\\u0027\\local\\copied-task.md"`. Ожидаемый результат — `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. Положительный тест `AuditPackageAllowsJsonEscapedQuotedTextAfterDriveLikePunctuation` должен остаться зелёным только для isolated `D:\u0022` / JSON-escaped metadata representation без following path separator-а.
  * Проверка опровержения: проверены implementation, machine-local classifier tests and docs. Тесты r20 покрывают `G\u003A/local/...` и `G\u003A\u005Clocal...`, но не literal or JSON-string `G:\u0022\local...` / `G:\u0027\local...`. Existing positive test covers only exact escaped quote text after drive-like punctuation and does not justify allowing a following path separator. Документация line `525` states that real drive-root paths with slash/backslash after colon remain forbidden.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`; `WindowsDrivePathPattern`; `ContainsMachineLocalPathValue`; `ContainsDecodedJsonStringMachineLocalPath`.
    * `Line range`: `AuditPackageCommand.cs` lines `73-75`, `4963-4968`, `4975-4994`.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/docs/release-management/audit-package.md` line `525`; mandatory `secret scanning` / local absolute path audit criterion.
    * `Evidence`: regex negative lookahead excludes `\u0022\b` and `\u0027\b` after the drive colon separator. In strings like `G:\u0022\local`, the boundary after `2` before `\` satisfies `\b`, so the regex does not match a drive path. `ContainsDecodedJsonStringMachineLocalPath` delegates decoded values to the same classifier, so JSON strings with doubled backslashes have the same blind spot.
    * `Impact`: real Windows drive-root paths can pass package and verify validation if their first directory segment is `u0022` or `u0027`.
    * `Fix`: restrict the escaped-quote exception to isolated non-path values and add tests for path separator after the escaped-quote-like segment.
    * `Verification`: targeted package and verify regressions must fail before the fix and pass after; existing false-positive guard test must remain green.

EVIDENCE_REVIEW:

* Полнота архива проверена. ZIP entries читаются; `SHA256SUMS.txt` согласован с фактическими ZIP entries при UTF-8-aware чтении; `repo-file-hashes.json` соответствует `repo-after/`; `metadata/repo-file-snapshots.json` содержит `fullContentIncluded: true` для всех 26 изменённых файлов. Недостатка снимков, который мешал бы читать реализацию, тесты или документацию, не найдено.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, не только по patch. Проверен путь `SelectPreflightCheckEvidenceFiles` → `PreparePreflightEvidenceSource` → `WriteNormalizedPreflightMetadata` / `SanitizePreflightEvidenceText` → `SanitizePreflightEvidenceTextSegments` → `NormalizePreflightMetadataNode` / `ReplaceRepoRootPathCandidates` → `ValidateSecretPolicy` → `ValidateArchiveContent` / `ValidateMachineLocalPathText`.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Набор r20 покрывает readable sanitizer output, placeholder-per-character rejection, sibling prefix, punctuation, traversal, spaced traversal, case-sensitive mismatch, exact quoted roots, compact JSON positives, escaped solidus/unicode escaped solidus positives and negatives, embedded JSON snippet after `/opt/e2d`, quoted structural tails, JSON prefix structural tails, JSON string-content tails, raw embedded boundaries, whitespace boundaries, POSIX sibling tokens, parent traversal root token, previous-verdict reviewer scanner boundaries and JSON-escaped Windows drive colon/backslash cases. Покрытия для B1/B2 cross-boundary replacement around unprotected snippets and B3 `\u0022`/`\u0027` first-segment drive paths нет.
* Документация проверена по `repo-after/docs/release-management/audit-package.md` and generated docs index. Документ описывает structural metadata/compact JSON normalization, left/right token boundaries, fail-closed external tails, non-JSON whole path-token rules and final classifier for literal/reversible/decoded local paths. B1/B2 противоречат этой модели из-за потери контекста вокруг unprotected snippets. B3 противоречит правилу, что real drive-root paths remain forbidden.
* Previous verdict files r01-r19 прочитаны. `metadata.previousVerdictChain` указывает все доступные отчёты, а `metadata.blockerClosureList` содержит closure-записи для прошлых blocker-ов. r19 B1 закрыт частично: тест `AuditPackageRejectsPreflightEvidenceEmbeddedJsonSnippetPathTokens` покрывает snippet после path separator-а с repo-root внутри snippet-а. Но новая проверка показывает, что общий механизм segment splitting всё ещё fail-open, когда repo-root находится непосредственно до или после unprotected snippet-а. r19 B2 закрыт для `G\u003A/...` and `G\u003A\u005C...`, но B3 показывает отдельную blind spot в escaped-quote guard.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `27/27`, machine-local classifier tests `6/6`, previous-verdict reviewer scanner tests `5/5`, audit-loop-stabilization `38/38`, sanitizer fixture, dead-tail-helper search, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`. Эти evidence подтверждают выполненный набор проверок, но не покрывают текущие B1-B3.
* Проверка области не выявила отдельного scope blocker: изменения ограничены release-management audit-package tool, integration tests, domain documentation, generated docs index, task card, dev diary and saved previous reports. Public API, runtime behavior, renderer, gameplay hot path and Godot 4.7 API surface не затрагиваются.
* Проверка секретов и локальных данных в текущем пакете не выявила реальных токенов, приватных ключей или паролей. Синтетические Windows/POSIX paths находятся в тестах, документации, patch and saved previous reports as examples. Текущие evidence outputs используют `<repo>` placeholders.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r19.md`
* `previous blockers closure`: r01 `B1`; r02 `B1`; r03 `B1`; r04 `B1`; r05 `B1`; r06 `B1`/`B2`; r07 `B1`; r08 `B1`/`B2`; r09 `B1`/`B2`; r10 `B1`/`B2`; r11 `B1`/`B2`; r12 `B1`/`B2`; r13 `B1`/`B2`; r14 `B1`/`B2`; r15 `B1`; r16 `B1`; r17 `B1`; r18 `B1`; r19 `B1`/`B2` checked.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r20/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: no snapshot gap; behavioral and implementation gaps for cross-boundary unprotected JSON snippet replacement and escaped-quote Windows drive first-segment paths.

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. r20 закрывает заявленные r19 examples, но текущая реализация всё ещё не сохраняет полный raw token context вокруг parseable, но незащищённых JSON snippets, и classifier всё ещё имеет доказуемую blind spot для Windows drive paths с `u0022`/`u0027` as first directory segment. До исправления B1-B3 audit package нельзя считать безопасным переносимым артефактом для внешнего аудита.
