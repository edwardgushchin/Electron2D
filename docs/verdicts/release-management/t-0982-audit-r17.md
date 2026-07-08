VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен audit ZIP `T-0982` итерации `r17` как одиночная область задачи. Архив читается, `repo-after/` доступен, `metadata/repo-file-snapshots.json` содержит полные снимки всех изменённых файлов, а `repo-file-hashes.json` согласован с итоговыми файлами.
* Область пакета заявлена как закрытие sanitizer-блокеров r01-r16: preflight `metadata.json` должен нормализоваться структурно, exact repo-root tokens должны заменяться только когда они изолированы или доказаны как часть полного компактного JSON-фрагмента, а sibling/traversal/delimiter/quoted/JSON-looking non-repository absolute paths должны оставаться под `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
* r17 закрывает конкретный r16-сценарий с parseable JSON prefix плюс внешним same-line suffix, например `{"repositoryRoot":"repo"},"backup":0`. Но в реализации остаётся другой fail-open путь того же класса: JSON-snippet proof принимается для single-quote/backtick exact-root token, который находится внутри JSON string content, а не является реальным JSON string token. Это позволяет скрыть POSIX sibling leaf path в raw preflight evidence.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r17`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: close r01-r16 sanitizer blockers; preserve sibling, traversal, delimiter-embedded, whitespace-embedded, POSIX, quoted-key, JSON-looking structural leaf, array-looking structural leaf, and parseable JSON-prefix plus external JSON-looking suffix non-repository absolute paths as blockers.
* `combined scope`: не используется, область одиночная.
* Проверенные основные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`.
* Проверенные previous verdict files: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r16.md`.
* Проверенные evidence: `evidence/T-0982-r17/preflight/*`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`.

BLOCKERS:

* B1

  * Что не так: raw preflight sanitizer может заменить repo-root внутри JSON string content, если путь обрамлён single quote или backtick, а вся внешняя строка является parseable JSON. Например, при POSIX repo-root `/home/user/repo` строка preflight `output.txt` вида `{"message":" '/home/user/repo','backup':0"}` будет принята как безопасный exact-root token: код заменит только `/home/user/repo` и получит `{"message":" '<repo>','backup':0"}`. Но исходный текстовый фрагмент после пробела может быть POSIX absolute path к sibling leaf `/home/user/repo','backup':0`, потому что `'`, `,` и `:` допустимы в POSIX filename segment.
  * Почему это важно: текущая задача должна fail-closed сохранять quoted / JSON-looking structural leaf non-repository absolute paths до проверки `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. В этом сценарии исходный machine-local path исчезает до `ValidateMachineLocalPathText`, и archive validation уже не видит `/home/user/repo`. Значит, audit ZIP может выглядеть переносимым, хотя raw preflight evidence содержал нерепозиторный локальный абсолютный путь.
  * Что исправить: JSON-snippet proof должен доказывать, что найденный exact-root является реальным JSON string token или JSON property name/value token, а не подстрокой внутри JSON string content. Минимальное исправление: разрешать JSON-snippet proof только для double-quote delimiter-а, который действительно участвует в JSON grammar; single quote и backtick exact-root tokens должны приниматься только как изолированные plain-text tokens в конце строки/текста, без JSON-looking хвоста. Более надёжный вариант — возвращать из JSON parser-а позиции string tokens и принимать replacement только если `start`/`end` совпадают с границами такого токена.
  * Как проверить исправление: добавить focused regression через production `audit package` на POSIX/non-Windows: imported preflight `output.txt` содержит `{"message":" '{slashRepoRoot}','backup':0"}` и `{"message":" `{slashRepoRoot}`,`backup`:0"}`. Ожидаемый результат — отказ с `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не ZIP с `<repo>`. Позитивные cases `{"repositoryRoot":"{slashRepoRoot}"}`, `["{slashRepoRoot}"]`, plain `"repo"` в конце строки и документированные safe child paths должны продолжать проходить.
  * Проверка опровержения: проверены implementation, focused tests, docs, `metadata.blockerClosureList`, previous reports и r17 evidence. Тест `AuditPackageRejectsPreflightEvidenceJsonPrefixStructuralTailTokens` покрывает внешний suffix после закрытого JSON-фрагмента. Тесты `AuditPackageRejectsPreflightEvidenceQuotedStructuralLeafSiblingPathTokens` и `AuditPackageRejectsPreflightEvidenceArrayQuotedStructuralLeafSiblingPathTokens` покрывают double-quote structural tails в raw/prose и array-looking contexts. Ни один тест не покрывает single quote/backtick token, который находится внутри parseable JSON string content. Evidence `t0982-r17-focused-sanitizer-tests` и `audit-loop-stabilization` подтверждают текущие `24/24`, но этот сценарий в них отсутствует. Документация не снимает blocker: она требует, чтобы raw JSON-looking tails без доказанного безопасного token context fail-closed оставались под absolute-path validation.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`; `IsRepoRootExactTokenEndDelimiter`, `IsSafeRepoRootExactTokenTail`, `IsExactRootInsideCompleteParsableJsonSnippet`, `IsRepoRootPathCandidateStartBoundary`, `ValidateMachineLocalPathText`.
    * `Line range`: `AuditPackageCommand.cs` lines `1573-1603`, `1605-1641`, `1914-1949`, `5006-5016`.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/docs/release-management/audit-package.md` line `451`; `repo-after/TASKS.md`, `Internal substrate acceptance contract` and acceptance criteria for fail-closed sanitizer behavior.
    * `Evidence`: `IsRepoRootExactTokenEndDelimiter` accepts delimiter values `"` / `'` / `` ` ``. `IsSafeRepoRootExactTokenTail` accepts any non-EOL tail if `IsExactRootInsideCompleteParsableJsonSnippet` returns true. That JSON proof parses a balanced `{...}` or `[...]` substring, but does not verify that the single quote/backtick around repo-root is part of JSON syntax rather than ordinary JSON string content. `IsRepoRootPathCandidateStartBoundary` also treats a quote/backtick after whitespace as a safe opening delimiter.
    * `Impact`: unsafe imported preflight evidence can be normalized into `<repo>` before machine-local path scanning, so a non-repository POSIX absolute path can be hidden inside the audit ZIP.
    * `Fix`: restrict JSON-snippet proof to actual JSON string token boundaries, or at minimum disallow JSON-snippet proof for single quote/backtick exact-root tokens with non-EOL tails.
    * `Verification`: focused sanitizer test must fail before the fix and pass after it; the r17 sanitizer suite should include the new regression together with the existing 24 tests.

EVIDENCE_REVIEW:

* Полнота архива проверена. `SHA256SUMS.txt` соответствует archive entries, `repo-file-hashes.json` соответствует `repo-after/`, а `metadata/repo-file-snapshots.json` содержит `fullContentIncluded: true` для всех 22 изменённых файлов. Недостатка снимков, который мешал бы читать реализацию, тесты или документацию, не найдено.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, не только по patch. Проверен путь `SelectPreflightCheckEvidenceFiles` → `PreparePreflightEvidenceSource` → `WriteNormalizedPreflightMetadata` / `SanitizePreflightEvidenceText` → `ReplaceRepoRootPathCandidates` → `ValidateSecretPolicy` / archive content validation. Блокирующий дефект находится до final machine-local path scan: sanitizer заменяет repo-root substring, после чего исходный absolute path больше не присутствует в тексте.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Набор покрывает readable output, placeholder-per-character rejection, sibling prefix, punctuation, traversal, spaced traversal, case-sensitive mismatch, exact quoted roots, quoted comma/semicolon tails, structural suffixes, structural leaf tails, array-looking structural leaf tails, JSON-prefix external tails, quoted-key path/value/structural suffixes, embedded start-boundaries, whitespace start-boundaries, POSIX sibling tokens and parent traversal root tokens. Покрытия для single quote/backtick exact-root внутри JSON string content нет.
* Документация проверена по `repo-after/docs/release-management/audit-package.md` и generated docs index. Документ описывает atomic `<repo>` replacement, platform-aware separators/case, whole-segment traversal scan, complete JSON snippet requirement and fail-closed behavior for ambiguous quoted/JSON-looking tails. Реализация по B1 не соответствует этой fail-closed части.
* Previous verdict files r01-r16 прочитаны. `metadata.previousVerdictChain` указывает все доступные отчёты, `metadata.blockerClosureList` содержит closure-записи для всех прошлых blocker-ов. Конкретный r16 blocker про parseable JSON-prefix plus external same-line suffix закрыт новым тестом `AuditPackageRejectsPreflightEvidenceJsonPrefixStructuralTailTokens`, но текущий B1 остаётся новым незакрытым fail-open вариантом в том же sanitizer boundary area.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `24/24`, audit-loop-stabilization `24/24`, sanitizer fixture, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`. Эти evidence подтверждают выполненный набор проверок, но не покрывают B1.
* Проверка области не выявила отдельного scope blocker: изменения ограничены release-management audit-package tool, integration tests, domain documentation, generated docs index, task card, diary and saved previous reports. Public API, runtime behavior, renderer, gameplay hot path and Godot 4.7 API surface не затрагиваются.
* Проверка секретов и локальных данных не выявила реальных токенов, приватных ключей или паролей. Синтетические Windows/POSIX paths находятся в тестах, документации, patch and saved reports as examples. Текущие evidence outputs используют `<repo>` placeholders.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r16.md`
* `previous blockers closure`: r01 `B1`; r02 `B1`; r03 `B1`; r04 `B1`; r05 `B1`; r06 `B1`/`B2`; r07 `B1`; r08 `B1`/`B2`; r09 `B1`/`B2`; r10 `B1`/`B2`; r11 `B1`/`B2`; r12 `B1`/`B2`; r13 `B1`/`B2`; r14 `B1`/`B2`; r15 `B1`; r16 `B1` checked.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r17/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: no snapshot gap; behavioral coverage gap and implementation bug for single quote/backtick exact-root tokens inside parseable JSON string content.

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. r17 закрывает r16 concrete JSON-prefix bypass и сохраняет широкий набор регрессий, но текущая реализация всё ещё может скрыть нерепозиторный POSIX absolute path в imported raw preflight evidence. До исправления B1 принимать `T-0982` небезопасно: sanitizer не доказывает, что accepted exact-root token действительно является repo-root token, а не частью JSON string content с filename-valid structural-looking suffix.
