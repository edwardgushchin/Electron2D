VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0982` итерации `r09`. Область пакета одиночная: закрыть sanitizer-блокеры r01-r08, заменять exact repo-root tokens и безопасные дочерние пути с платформенной семантикой регистра, разделителей, границ token-а и whole-segment traversal, отклонять placeholder-per-character artifacts и сохранять нерепозиторные absolute paths как отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
* Пакет содержит полные snapshots изменённых файлов. Предыдущие r01-r08 verdict-файлы доступны в архиве, `metadata.blockerClosureList` связывает каждый прошлый blocker с текущими focused sanitizer tests и `audit-loop-stabilization`. Конкретные прошлые случаи в значительной части закрыты: direct/punctuation sibling prefix, traversal sibling, spaced traversal, parent-traversal root-token, case-variant root, POSIX backslash и direct quoted comma/semicolon tails.
* Изменение нельзя принять: текущая проверка quoted exact-root boundary всё ещё не является полноценной проверкой token-а. Она может скрыть POSIX sibling absolute path после comma/semicolon + structural-looking delimiter, а также не заменяет безопасный exact repo-root token перед прямым `}` или `]` в compact JSON/text output.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r09`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: `Close T-0982 r01-r08 sanitizer blockers by replacing exact repo-root tokens and safe child paths with platform path-case, separator, token-boundary and whole-segment traversal semantics, rejecting broken placeholder-per-character evidence, and preserving direct, punctuation, parent-traversal, spaced-segment parent-traversal, parent-traversal-root-token, case-variant, POSIX backslash and quoted-punctuation non-repository absolute paths as E2D-BUILD-AUDIT-ABSOLUTE-PATH blockers.`
* `metadata.previousVerdictChain`: `["docs/verdicts/release-management/t-0982-audit-r01.md", "docs/verdicts/release-management/t-0982-audit-r02.md", "docs/verdicts/release-management/t-0982-audit-r03.md", "docs/verdicts/release-management/t-0982-audit-r04.md", "docs/verdicts/release-management/t-0982-audit-r05.md", "docs/verdicts/release-management/t-0982-audit-r06.md", "docs/verdicts/release-management/t-0982-audit-r07.md", "docs/verdicts/release-management/t-0982-audit-r08.md"]`
* Проверенные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r08.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`
* Проверенные артефакты: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`, `evidence/T-0982-r09/preflight/*`
* `combined scope`: не используется, область одиночная

BLOCKERS:

* B1

  * Что не так: quoted exact-root replacement всё ещё может скрыть POSIX sibling absolute path. Метод `IsRepoRootExactTokenEndDelimiter` принимает comma/semicolon после закрывающей кавычки, если после них стоит `"`, `'`, `` ` ``, `}` или `]`, и сразу возвращает `true`. Он не проверяет, что этот structural-looking символ действительно завершает token, а не является частью POSIX path segment с path-like suffix дальше по строке. Например, строка `"/home/user/repo",]backup/logs.txt` при `repoRoot = /home/user/repo` будет преобразована в `"<repo>",]backup/logs.txt`. Исходный абсолютный POSIX path указывает на sibling segment `repo",]backup`, то есть вне репозитория.
  * Почему это важно: текущий scope r09 прямо требует сохранять quoted-punctuation non-repository absolute paths как `E2D-BUILD-AUDIT-ABSOLUTE-PATH` blockers. Документация текущего пакета также говорит, что comma/semicolon после closing delimiter безопасны только если последующий structural delimiter сам не продолжает path-like suffix. Текущий код проверяет только первый символ после comma/semicolon и поэтому снова может замаскировать нерепозиторный machine-local path как `<repo>...`.
  * Что исправить: сделать quoted exact-root replacement token-aware. После comma/semicolon нужно проверять не только ближайший structural-looking символ, но и хвост после него: если дальше продолжается filename/path-like suffix с segment text и `/`, такой input должен fail closed и попадать под `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. Надёжнее выделять полный path-token и заменять только доказанный exact repo-root token.
  * Как проверить исправление: добавить focused regression через production `audit package` на POSIX/non-Windows path, где imported preflight `output.txt` содержит matching-opening-quote cases вроде `"{slashRepoRoot}",]backup/logs.txt`, `"{slashRepoRoot}","backup/logs.txt` и `"{slashRepoRoot}";}backup/logs.txt`. Ожидаемый результат — отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не ZIP с `"<repo>",]backup/...`. Позитивный exact-root test должен продолжать заменять настоящие isolated tokens.
  * Проверка опровержения: проверены текущие focused tests и evidence. `AuditPackageRejectsPreflightEvidenceQuotedCommaSiblingPathTokens` проверяет direct tails `repo",backup/...` и `repo";backup/...`, но fixture не содержит matching opening quote перед repo-root и не проверяет comma/semicolon followed by accepted structural char plus path-like suffix. `AuditPackageRejectsPreflightEvidencePosixSiblingPathTokens` расширен comma/semicolon tails, но также не покрывает `",]backup`/`","backup`-подобный хвост. Evidence `11/11` не опровергает проблему, потому этот case отсутствует.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `IsRepoRootExactTokenEndDelimiter`, строки 1480-1521; `ValidateMachineLocalPathText`, строки 4636-4644.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/TASKS.md`, строки 3795 и 3812-3814; `repo-after/docs/release-management/audit-package.md`, строка 451.
    * `Evidence`: строки 1509-1521 принимают comma/semicolon и затем `"`, `'`, `` ` ``, `}` или `]` без проверки последующего path-like suffix. После замены исходный `/home/user/repo` исчезает, поэтому `ValidateMachineLocalPathText` уже не видит local absolute path.
    * `Impact`: sanitizer может скрыть нерепозиторный machine-local POSIX path, что нарушает fail-closed safety contract текущей задачи.
    * `Fix`: token-aware tail validation или full path-token normalization для quoted exact-root replacement.
    * `Verification`: focused sanitizer tests должны ожидать `E2D-BUILD-AUDIT-ABSOLUTE-PATH` для quoted comma/semicolon + structural-looking delimiter + path suffix; прежние r01-r08 regressions и positive exact-root tests должны остаться зелёными.

* B2

  * Что не так: sanitizer не заменяет безопасный quoted exact repo-root token, если сразу после закрывающей кавычки стоит прямой `}` или `]`. Например, raw preflight `output.txt` со строкой `{"repositoryRoot":"/home/user/repo"}` или `["/home/user/repo"]` содержит isolated exact repo-root token. Текущий `IsRepoRootExactTokenEndDelimiter` для такого token-а возвращает `false`, потому после closing quote он принимает только конец текста/строки или comma/semicolon flow. В результате local repo-root остаётся в staging artifact и затем блокируется обычной проверкой локальных путей.
  * Почему это важно: текущий scope r09 требует заменять exact repo-root tokens. Compact JSON/text output с `}` или `]` после string value — обычная форма preflight output/result evidence. Такое evidence безопасно для нормализации в `<repo>`, но текущая реализация не создаёт переносимый artifact и завершает `audit package` отказом.
  * Что исправить: расширить exact-root token boundary так, чтобы direct `}`/`]` после closing quote принимались как безопасная структурная граница только при отсутствии path-like suffix. Это исправление нужно делать вместе с B1, чтобы не открыть новый masking case вроде `"/repo"]backup/logs.txt`.
  * Как проверить исправление: добавить focused integration test через production `audit package`, где `output.txt` и/или `result.txt` содержат compact JSON lines `{"repositoryRoot":"{slashRepoRoot}"}` и `["{slashRepoRoot}"]`. Ожидаемый результат — успешный ZIP, `<repo>` внутри этих строк и отсутствие native/slash repo-root. Negative cases из B1 должны при этом оставаться отказами.
  * Проверка опровержения: проверены текущие exact-root tests. `AuditPackageSanitizesExactPreflightEvidenceRepositoryRootTokens` покрывает indented `metadata.json` и text lines, где closing quote после repo-root стоит перед переводом строки; compact JSON/object или array case перед direct `}`/`]` не покрыт. Документация говорит о safe closing delimiter и structural boundary, но код прямые `}`/`]` не принимает.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `IsRepoRootExactTokenEndDelimiter`, строки 1480-1521; `SanitizePreflightEvidenceText`, строки 1332-1342; `ValidateMachineLocalPathText`, строки 4636-4644.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/TASKS.md`, строки 3740-3743, 3795 и 3813-3814; `repo-after/docs/release-management/audit-package.md`, строка 451.
    * `Evidence`: строка 1509 возвращает `false`, если символ после closing quote не comma/semicolon; direct `}`/`]` therefore не заменяется, а строки 4639-4644 затем блокируют оставшийся repo-root как machine-local path.
    * `Impact`: safe imported preflight evidence с exact repo-root token в compact JSON/text form не становится переносимым `<repo>` artifact, хотя это входит в текущий контракт задачи.
    * `Fix`: token-aware acceptance direct structural delimiters for exact-root values, with fail-closed suffix validation.
    * `Verification`: focused sanitizer test должен проходить на compact JSON exact-root output/result и одновременно сохранять отказ для quoted-punctuation sibling tails.

EVIDENCE_REVIEW:

* Полнота снимков проверена. `metadata/repo-file-snapshots.json` содержит полные `repo-after/` и `repo-before/` snapshots для всех изменённых файлов. `repo-file-hashes.json` совпадает с `repo-after/`, `SHA256SUMS.txt` успешно проверяется по файлам архива.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`. Imported preflight evidence проходит через `PreparePreflightEvidenceSource`, `WriteNormalizedPreflightMetadata`, `SanitizePreflightEvidenceText`, `ReplaceRepoRootPathCandidates`, `IsRepoRootPathCandidateBoundary`, `IsRepoRootExactTokenEndDelimiter`, `HasParentTraversalSegmentAfterRepoRoot`, `ValidateNoBrokenPreflightPlaceholderText` и затем через обычную archive content validation. Основная проблема текущей итерации находится в quoted exact-root tail parsing.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Focused suite содержит 11 tests: positive child path/readability, broken placeholder rejection, direct sibling rejection, punctuation sibling rejection, traversal sibling rejection, spaced-segment traversal rejection, case-variant rejection on case-sensitive platforms, exact quoted root sanitization, quoted comma/semicolon sibling rejection, POSIX sibling token rejection и parent-traversal root-token rejection. Не покрыты B1 `",]backup`/`","backup`-подобные tails и B2 compact JSON exact-root перед direct `}`/`]`.
* Документация проверена по `repo-after/docs/release-management/audit-package.md`. Документ описывает атомарную замену только whole repo-root/safe child paths, quoted/backtick exact-root handling, platform-aware separators/case semantics, whole-segment traversal scanning и fail-closed поведение для quoted-punctuation non-repository paths. Реализация расходится с этим документом по B1 и B2.
* Проверка прошлых verdict-файлов выполнена. В пакете доступны r01-r08 отчёты из `metadata.previousVerdictChain`; прошлые blocker-и прочитаны и сопоставлены с `metadata.blockerClosureList`. Конкретные прошлые cases r01-r08 в коде и тестах в основном закрыты, но общий quoted exact-root boundary contract остаётся не закрыт из-за B1/B2.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `11/11`, `audit-loop-stabilization` `11/11`, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`, а также sanitizer fixture. Эти artifacts подтверждают выполненные проверки, но не покрывают два residual cases выше.
* Проверка области не выявила лишних runtime/API изменений. Изменения ограничены audit package build tool, integration tests, release-management documentation, generated docs index, task card, diary и сохранёнными r01-r08 reports для previous verdict chain. Public API, игровой runtime, rendering/input/editor/browser submission path и Godot compatibility surface не менялись.
* Проверка секретов и локальных данных не выявила реальных секретов, приватных ключей, токенов или конфиденциальных локальных путей в current included artifacts. Синтетические Windows/Linux/POSIX paths находятся в tests, docs, patch или saved audit reports как примеры прошлых blocker-ов; current preflight evidence использует `<repo>` placeholders и не раскрывает локальный repo-root.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r02.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r03.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r04.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r05.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r06.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r07.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r08.md`
* `previous blockers closure`: r01 `B1`, r02 `B1`, r03 `B1`, r04 `B1`, r05 `B1`, r06 `B1`/`B2`, r07 `B1`, r08 `B1`/`B2` проверены; конкретные прошлые examples закрыты, но текущий quoted exact-root contract остаётся не закрыт из-за B1/B2 выше.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r09/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: нет по полноте snapshots; есть поведенческий gap в sanitizer regression coverage для quoted exact-root direct structural delimiters и comma/semicolon + structural-looking path-like tails.
* Проверенные evidence paths: `evidence/T-0982-r09/preflight/audit-loop-stabilization/*`, `evidence/T-0982-r09/preflight/t0982-r09-build-tool-build/*`, `evidence/T-0982-r09/preflight/t0982-r09-focused-sanitizer-tests/*`, `evidence/T-0982-r09/preflight/t0982-r09-sanitizer-fixture/*`, `evidence/T-0982-r09/preflight/t0982-r09-verify-audit-contracts/*`, `evidence/T-0982-r09/preflight/t0982-r09-update-docs-check/*`, `evidence/T-0982-r09/preflight/t0982-r09-verify-docs/*`, `evidence/T-0982-r09/preflight/t0982-r09-verify-audit-followups/*`, `evidence/T-0982-r09/preflight/t0982-r09-verify-licenses/*`, `evidence/T-0982-r09/preflight/t0982-r09-git-diff-check/*`

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. Пакет r09 закрывает многие конкретные regressions из r01-r08, но quoted exact-root boundary handling всё ещё не соответствует заявленному контракту: один residual case может скрыть нерепозиторный POSIX absolute path, другой блокирует безопасный compact JSON/text exact-root token. До исправления B1 и B2 принимать `T-0982` небезопасно.
