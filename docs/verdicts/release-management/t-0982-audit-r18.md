VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен audit ZIP `T-0982` итерации `r18` как одиночная область задачи. Архив читается, `repo-after/` доступен, `metadata/repo-file-snapshots.json` содержит полные снимки всех изменённых файлов, а контрольные суммы архива и `repo-file-hashes.json` согласованы с итоговыми файлами.
* Область пакета заявлена как закрытие sanitizer-блокеров r01-r17: preflight `metadata.json` должен нормализоваться структурно, raw preflight text должен заменять только доказанные repo-root tokens или безопасные child paths, а sibling/traversal/delimiter/quoted/JSON-looking non-repository absolute paths должны оставаться блокерами.
* r18 закрывает конкретный r17-сценарий с single-quote/backtick exact-root token внутри JSON string content. Но в реализации остаётся доказуемый fail-open вариант для raw compact JSON evidence: POSIX repo-root, записанный в JSON string как escaped solidus path (`\/home\/...`) или unicode-escaped solidus path (`\u002Fhome\u002F...`), не заменяется и не блокируется. Такой путь остаётся в ZIP в обратимо декодируемом виде.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r18`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: закрытие r01-r17 sanitizer blockers, включая structural metadata normalization, complete compact JSON snippet proof with actual double-quoted JSON string-token boundaries, fail-closed handling for JSON-looking tails and single-quote/backtick JSON string-content paths.
* `combined scope`: не используется, область одиночная.
* Проверенные основные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`.
* Проверенные previous verdict files: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r17.md`.
* Проверенные evidence: `evidence/T-0982-r18/preflight/*`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`.

BLOCKERS:

* B1

  * Что не так: raw preflight sanitizer не обрабатывает JSON-escaped POSIX repo-root внутри compact JSON snippets. Например, при POSIX repo-root `/home/user/repo` строка preflight `output.txt` вида `{"repositoryRoot":"\/home\/user\/repo"}` или `{"repositoryRoot":"\u002Fhome\u002Fuser\u002Frepo"}` останется в архиве как есть. Это обратимое представление того же локального абсолютного пути, но текущая замена ищет только literal candidate `/home/user/repo`, а финальная проверка локальных путей не декодирует JSON string escapes.
  * Почему это важно: задача именно про безопасную нормализацию imported preflight evidence. Документация r18 утверждает, что compact JSON forms вроде `{"repositoryRoot":"<repo>"}` поддерживаются через complete parseable JSON snippet и actual double-quoted JSON string-token boundary. Для JSON string token-а с escaped solidus decoded value всё равно является repo-root. Текущий код не заменяет его на `<repo>` и не останавливает пакет, поэтому audit ZIP может раскрыть локальный путь машины в обратимо закодированном виде.
  * Что исправить: raw JSON snippet path должен учитывать decoded JSON string values, а не только raw substring. Безопасный вариант — при complete parseable JSON snippet находить string tokens, декодировать их JSON value и нормализовать decoded repo-root/safe child path в `<repo>` перед записью evidence. Минимально допустимый fail-closed вариант — если raw preflight text содержит JSON-escaped representation текущего repo-root, пакет должен завершаться `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не передавать ZIP с `\/home\/...` или `\u002Fhome...`.
  * Как проверить исправление: добавить focused regression через production `audit package` на POSIX/non-Windows. Imported preflight `output.txt` должен содержать compact JSON snippets `{"repositoryRoot":"\/home\/user\/repo"}` и `{"repositoryRoot":"\u002Fhome\u002Fuser\u002Frepo"}` с actual fixture repo-root. Проверка должна доказать, что итоговый ZIP либо содержит normalized `<repo>` и не содержит escaped local path, либо пакет падает с `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. Текущие positive cases для literal compact JSON `{"repositoryRoot":"{slashRepoRoot}"}` и array `["{slashRepoRoot}"]` должны продолжать проходить.
  * Проверка опровержения: проверены реализация, focused sanitizer tests, documentation, `metadata.blockerClosureList`, previous reports и r18 evidence. Тест `AuditPackageSanitizesExactPreflightEvidenceRepositoryRootTokens` покрывает только literal slash compact JSON. Тест `AuditPackageRejectsPreflightEvidenceJsonStringContentQuotedTailTokens` покрывает r17 single-quote/backtick scenario, но не JSON-escaped slash representation. Evidence `t0982-r18-focused-sanitizer-tests` и `audit-loop-stabilization` подтверждают текущие `25/25` и `30/30`, но этот scenario в них отсутствует. Документация не снимает blocker, потому она описывает compact JSON support и не объявляет escaped solidus local paths допустимым исключением.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`; `SanitizePreflightEvidenceText`; `ReplaceRepoRootPathCandidates`; `GetRepoRootReplacementCandidates`; `IsExactRootInsideCompleteParsableJsonSnippet`; `ValidateMachineLocalPathText`.
    * `Line range`: `AuditPackageCommand.cs` lines `1420-1430`, `1444-1469`, `1607-1649`, `5061-5069`.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/docs/release-management/audit-package.md` line `451`; `repo-after/TASKS.md`, T-0982 acceptance criteria for atomic repo-root sanitizer and readable safe preflight evidence.
    * `Evidence`: `ReplaceRepoRootPathCandidates` searches raw candidate strings such as `/home/user/repo`; `GetRepoRootReplacementCandidates` does not add POSIX JSON-escaped solidus or unicode-escaped solidus variants. `ValidateMachineLocalPathText` only does `text.Replace('\\', '/')` plus literal candidate containment; `\/home\/user\/repo` becomes `//home//user//repo`, and `\u002Fhome\u002Fuser\u002Frepo` becomes `/u002Fhome/u002Fuser/u002Frepo`, so neither matches `/home/user/repo`.
    * `Impact`: imported preflight raw text can disclose the local repository root in a reversible JSON-escaped form while passing package validation and external audit transport checks.
    * `Fix`: decode and normalize JSON string tokens inside complete raw snippets, or fail closed on JSON-escaped local path representations before writing evidence.
    * `Verification`: new focused sanitizer regression must fail before the fix and pass after; `t0982-r18-focused-sanitizer-tests` and `audit-loop-stabilization` should include the new escaped-solidus case.

EVIDENCE_REVIEW:

* Полнота архива проверена. `SHA256SUMS.txt` согласован с ZIP entries, `repo-file-hashes.json` соответствует `repo-after/`, а `metadata/repo-file-snapshots.json` содержит `fullContentIncluded: true` для всех 23 изменённых файлов. Недостатка снимков, который мешал бы читать реализацию, тесты или документацию, не найдено.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, не только по patch. Проверен путь `SelectPreflightCheckEvidenceFiles` → `PreparePreflightEvidenceSource` → `WriteNormalizedPreflightMetadata` / `SanitizePreflightEvidenceText` → `ReplaceRepoRootPathCandidates` → `ValidateSecretPolicy` / archive content validation. r18 добавляет structural metadata normalization, raw evidence sanitization, strict JSON snippet proof, decoded JSON string-token boundary check for literal candidates, and previous verdict reviewer phrase exceptions. Блокирующий дефект B1 находится в raw evidence path: JSON snippet parsing используется только как proof после literal match, а не как механизм decoded-value normalization.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Набор покрывает readable sanitizer output, placeholder-per-character rejection, sibling prefix, punctuation, traversal, spaced traversal, case-sensitive mismatch, exact quoted roots, quoted comma/semicolon tails, structural suffixes, structural leaf tails, array-looking structural leaf tails, JSON-prefix external tails, r17 single-quote/backtick JSON string-content tails, quoted-key path/value/structural suffixes, embedded start-boundaries, whitespace start-boundaries, POSIX sibling tokens, parent traversal root tokens and previous-verdict reviewer scanner boundaries. Покрытия для escaped solidus / unicode-escaped solidus POSIX repo-root in raw compact JSON evidence нет.
* Документация проверена по `repo-after/docs/release-management/audit-package.md` и generated docs index. Документ описывает atomic `<repo>` replacement, platform-aware separators/case, whole-segment traversal scan, complete JSON snippet requirement, actual double-quoted JSON string-token boundary and fail-closed behavior for ambiguous quoted/JSON-looking tails. Реализация по B1 не соответствует смыслу compact JSON support for decoded string values.
* Previous verdict files r01-r17 прочитаны. `metadata.previousVerdictChain` указывает все доступные отчёты, а `metadata.blockerClosureList` содержит closure-записи для всех прошлых blocker-ов. Конкретный r17 blocker закрыт новым тестом `AuditPackageRejectsPreflightEvidenceJsonStringContentQuotedTailTokens` и изменением `IsSafeRepoRootExactTokenTail`, но это не закрывает новый escaped JSON representation scenario из B1.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `25/25`, previous-verdict reviewer scanner tests `5/5`, audit-loop-stabilization `30/30`, sanitizer fixture, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`. Эти evidence подтверждают выполненный набор проверок, но не покрывают B1.
* Проверка области не выявила отдельного scope blocker: изменения ограничены release-management audit-package tool, integration tests, domain documentation, generated docs index, task card, dev diary and saved previous reports. Public API, runtime behavior, renderer, gameplay hot path and Godot 4.7 API surface не затрагиваются.
* Проверка секретов и локальных данных в текущих файлах не выявила реальных токенов, приватных ключей или паролей. Синтетические Windows/POSIX paths находятся в тестах, документации, patch and saved previous reports as examples. Текущие evidence outputs используют `<repo>` placeholders.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r17.md`
* `previous blockers closure`: r01 `B1`; r02 `B1`; r03 `B1`; r04 `B1`; r05 `B1`; r06 `B1`/`B2`; r07 `B1`; r08 `B1`/`B2`; r09 `B1`/`B2`; r10 `B1`/`B2`; r11 `B1`/`B2`; r12 `B1`/`B2`; r13 `B1`/`B2`; r14 `B1`/`B2`; r15 `B1`; r16 `B1`; r17 `B1` checked.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r18/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: no snapshot gap; behavioral coverage gap and implementation bug for JSON-escaped POSIX repo-root strings in raw compact JSON preflight evidence.

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, private helpers `IsSafeAfterExactRootListSeparator`, `IsSafeAfterStructuralTokenDelimiter`, `IsStructuredQuotedKeyTail`, `IsSafeQuotedValueTail`, `HasPathSeparatorBeforeStructuralBoundary`.
  * Проблема: после перехода r18 на `IsExactRootInsideCompleteParsableJsonSnippet` эти private helpers больше не вызываются. Они остались как мёртвый tail-parser от прежней реализации и усложняют аудит sanitizer-а: читатель видит правила, которые фактически не участвуют в принятии решения.
  * Почему не блокирует текущую задачу: это не меняет runtime behavior текущего sanitizer-а и не является причиной утечки B1. Сборка и focused tests проходят. Проблема относится к сопровождаемости кода после исправления blocker-а.
  * Куда перенести: Suggested new task — “Remove dead preflight sanitizer tail-parser helpers”. Рекомендуемый приоритет: `P3`. Домен: `release-management`. Критерий приёмки: в `AuditPackageCommand.cs` нет неиспользуемых private helpers старого exact-root tail parser-а; focused sanitizer tests and audit-loop-stabilization остаются зелёными. Идея проверки: статический поиск показывает, что каждый private helper вызывается или удалён; `dotnet build eng\Electron2D.Build\Electron2D.Build.csproj --no-restore -v:minimal` и focused sanitizer suite проходят.
  * Рекомендуемый приоритет: `P3`
  * Как проверить: удалить или подключить эти helpers осознанно, затем запустить build tool build и focused sanitizer tests; code review должен подтвердить отсутствие unreachable sanitizer logic.
  * Техническая привязка:

    * `FOLLOW_UP_FINDING F1`
    * `File/symbol`: `AuditPackageCommand.cs` lines `1769-1895`
    * `Why not blocker for current task`: не влияет на текущий observable behavior и не закрывает/не создаёт B1 напрямую.

CLOSURE_DECISION:

* Задача остаётся открытой. r18 закрывает r17 blocker и добавляет полезную проверку previous-verdict reviewer false-positive boundary, но текущая реализация всё ещё может передать локальный POSIX repo-root в raw preflight evidence как JSON-escaped string. До исправления B1 принимать `T-0982` небезопасно: audit ZIP может выглядеть очищенным от literal machine-local paths, но содержать тот же путь в обратимо декодируемом JSON form.
