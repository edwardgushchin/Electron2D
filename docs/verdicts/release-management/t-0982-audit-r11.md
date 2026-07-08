VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0982` итерации `r11`. Область пакета одиночная: закрыть sanitizer-блокеры r01-r10 для imported preflight evidence, заменять exact repo-root tokens и safe child paths с платформенной семантикой регистра, разделителей, границ token-а, traversal и quoted-tail, отклонять placeholder-per-character artifacts и сохранять нерепозиторные machine-local absolute paths как `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
* Архив читается, `metadata/repo-file-snapshots.json` содержит полные snapshots изменённых файлов, `repo-after/` доступен. Предыдущие r01-r10 отчёты находятся в `repo-after/docs/verdicts/release-management/`, а `metadata.blockerClosureList` содержит проверяемые closure-записи для всех прошлых blocker-ов.
* Изменение нельзя принять. r11 закрывает конкретные r10 examples для `:`, `,`, `;`, `=` перед embedded repo-root и для quoted-key tails с unquoted path suffix after colon, но текущая boundary-логика всё ещё может скрывать нерепозиторные POSIX absolute paths: через opening delimiter перед embedded repo-root и через quoted-key tail, где path-like suffix начинается внутри quoted value.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r11`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: `Close T-0982 r01-r10 sanitizer blockers by replacing exact repo-root tokens and safe child paths with platform path-case, separator, token-boundary, whole-segment traversal, structural quoted-tail and strict start-boundary semantics, rejecting broken placeholder-per-character evidence, and preserving direct, punctuation, parent-traversal, spaced-segment parent-traversal, parent-traversal-root-token, embedded-root, case-variant, POSIX backslash and quoted-punctuation non-repository absolute paths as E2D-BUILD-AUDIT-ABSOLUTE-PATH blockers.`
* `metadata.previousVerdictChain`: `docs/verdicts/release-management/t-0982-audit-r01.md` ... `docs/verdicts/release-management/t-0982-audit-r10.md`
* Проверенные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r10.md`
* Проверенные артефакты: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`, `evidence/T-0982-r11/preflight/*`
* `combined scope`: не используется, область одиночная

BLOCKERS:

* B1

  * Что не так: start-boundary проверка всё ещё принимает opening delimiter перед `repoRoot` без проверки, что этот delimiter сам не находится внутри более длинного POSIX path-token-а. Например, при `repoRoot = /home/user/repo` строка `/opt/prefix(/home/user/repo/logs.txt` является нерепозиторным absolute path: совпадение `/home/user/repo` находится внутри пути `/opt/prefix(...`, а не является настоящим repo-root или его дочерним путём. Текущий код примет `(` как допустимую левую границу, увидит `/` после repo-root как child separator и заменит фрагмент на `/opt/prefix(<repo>/logs.txt`.
  * Почему это важно: r11 scope прямо требует `strict start-boundary semantics` и сохранение `embedded-root` non-repository absolute paths как blockers. После такой замены исходный repo-root исчезает из текста, и последующая проверка machine-local paths уже не может поймать путь, который до sanitization содержал локальный repo-root.
  * Что исправить: start-boundary должен проверять полный path-token вокруг найденного candidate. Opening quote/backtick/bracket/brace/parenthesis можно считать безопасной границей только если этот delimiter сам не является частью path segment-а. Практически: выделять весь surrounding path-token и заменять только если весь token равен exact repo-root или нормализуется в safe child path внутри repoRoot; embedded occurrences inside longer POSIX paths должны оставаться для `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
  * Как проверить исправление: добавить focused regression через production `audit package` на POSIX/non-Windows: imported preflight `output.txt` содержит `/opt/prefix({slashRepoRoot}/logs.txt`, `/opt/prefix[{slashRepoRoot}/logs.txt` и `/opt/prefix"{slashRepoRoot}/logs.txt`. Ожидаемый результат — отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не ZIP с `/opt/prefix(<repo>/logs.txt`. Позитивные cases вида `path: {slashRepoRoot}/logs.txt`, quoted exact root и compact JSON exact root должны продолжать проходить.
  * Проверка опровержения: проверены implementation, tests, docs и evidence. Текущий regression `AuditPackageRejectsPreflightEvidenceEmbeddedRepoRootStartBoundary` покрывает только embedded root после `:`, `,` и `=`, но не после `(`, `[`, `{`, quote/backtick/apostrophe. Документация не снимает проблему: она обещает strict start-boundary и запрет filename-valid punctuation inside longer path-token. Evidence `14/14` не покрывает opening-delimiter embedded path.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `ReplaceRepoRootPathCandidate`, строки 1405-1447; `IsRepoRootPathCandidateBoundary`, строки 1449-1477; `IsRepoRootPathCandidateStartBoundary`, строки 1688-1697; `ValidateMachineLocalPathText`, строки 4738-4746.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/TASKS.md`, строки 3795 и 3815-3817; `repo-after/docs/release-management/audit-package.md`, строка 451.
    * `Evidence`: строки 1695-1697 принимают `"`/`'`/`` ` ``/`(`/`[`/`{`/whitespace as start-boundary без проверки полного path-token-а. Строки 1472-1477 затем разрешают child-path replacement, если после repo-root стоит platform separator и нет `..`.
    * `Impact`: sanitizer может скрыть embedded repo-root внутри нерепозиторного POSIX absolute path и пропустить unsafe evidence в audit ZIP.
    * `Fix`: full token parsing around candidate; replacement only for whole exact repo-root token or normalized safe child token.
    * `Verification`: focused sanitizer test должен ожидать `E2D-BUILD-AUDIT-ABSOLUTE-PATH` для opening-delimiter embedded POSIX paths и сохранять успешную замену для настоящих exact-root/child-path cases.

* B2

  * Что не так: quoted-key tail validation всё ещё fail-open для path-like suffix, который начинается внутри quoted value. Например, при `repoRoot = /home/user/repo` строка `"/home/user/repo","backup":"/logs.txt` может быть POSIX absolute path `/home/user/repo","backup":"/logs.txt`: quotes, comma, colon and slash-valid segment tail are legal POSIX filename/path characters after the repo-root prefix. Текущий код считает это safe structured quoted-key tail, потому после colon сразу стоит quote, и возвращает `true` без проверки, что внутри quoted value дальше идёт `/logs.txt`.
  * Почему это важно: r10 blocker `B1` закрывался именно для quoted-key-looking tails с path suffix after colon. r11 тесты закрывают unquoted tails `: /logs` и `: logs/path`, но quoted value `:"/logs"` остаётся незащищённым. Это снова нарушает требование сохранять quoted-punctuation/non-repository absolute paths как `E2D-BUILD-AUDIT-ABSOLUTE-PATH` blockers.
  * Что исправить: quoted-key tail нужно проверять не только до `:`, но и после него. Если после colon начинается quoted/backtick value, sanitizer должен либо структурно распарсить этот фрагмент как JSON/text token и доказать, что это не path-like continuation, либо fail-closed оставить исходный absolute path для `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. Для `metadata.json` безопаснее нормализовать JSON values структурно; для raw `output.txt`/`result.txt` ambiguous quoted-key tails с `:"/..."`, `:".../..."`, `:["/..."]` и похожими path-like continuations должны отклоняться.
  * Как проверить исправление: добавить focused regression через production `audit package` на POSIX/non-Windows: imported preflight `output.txt` содержит `"{slashRepoRoot}","backup":"/logs.txt` и `"{slashRepoRoot}";"backup":"logs/path.txt`. Ожидаемый результат — отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не ZIP с `"<repo>","backup":"/logs.txt`. Позитивный compact JSON exact-root test должен продолжать заменять настоящие isolated exact-root tokens.
  * Проверка опровержения: проверены текущие tests и evidence. `AuditPackageRejectsPreflightEvidenceQuotedKeyPathTailSiblingPathTokens` покрывает `repo","backup":/logs.txt` и `repo";"backup":logs/path.txt`, но не покрывает quoted value after colon. `AuditPackageRejectsPreflightEvidenceQuotedStructuralSuffixSiblingPathTokens` покрывает `",]backup`, `","backup/logs` и `";}backup`, но не проверяет `","key":"/logs`. Evidence `14/14` не опровергает этот случай.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `IsSafeAfterExactRootListSeparator`, строки 1517-1535; `IsStructuredQuotedKeyTail`, строки 1559-1593; `HasPathSeparatorBeforeStructuralBoundary`, строки 1596-1614; `ValidateMachineLocalPathText`, строки 4738-4746.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/TASKS.md`, строки 3795 и 3815-3817; `repo-after/docs/release-management/audit-package.md`, строка 451.
    * `Evidence`: строки 1588-1590 сразу возвращают `true`, если value после colon начинается с quote/backtick/apostrophe, `{` или `[`. Проверка `HasPathSeparatorBeforeStructuralBoundary` на строке 1593 применяется только к unquoted tail и не проверяет path separator внутри quoted value.
    * `Impact`: sanitizer может скрыть нерепозиторный POSIX absolute path с quoted-key-looking suffix как `<repo>...`, после чего archive content validation уже не видит исходный repo-root.
    * `Fix`: token-aware quoted-tail validation after colon или structural JSON-only normalization; ambiguous raw text tails должны fail closed.
    * `Verification`: focused sanitizer test должен ожидать `E2D-BUILD-AUDIT-ABSOLUTE-PATH` для quoted-key tails with quoted path-like value; existing exact-root, compact JSON and r01-r10 regression tests должны остаться зелёными.

EVIDENCE_REVIEW:

* Полнота снимков проверена. `metadata/repo-file-snapshots.json` содержит `fullContentIncluded: true` для всех изменённых файлов. Хэши в `repo-file-hashes.json` и `SHA256SUMS.txt` согласованы с содержимым архива.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`. Imported preflight evidence проходит через `PreparePreflightEvidenceSource`, `WriteNormalizedPreflightMetadata`, `SanitizePreflightEvidenceText`, `ReplaceRepoRootPathCandidates`, boundary checks, placeholder validation и затем через archive content validation. Основной остаточный риск — delimiter heuristics без проверки полного path-token-а.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Focused suite содержит 14 tests и закрывает исторические examples r01-r10: readable child path, broken placeholder, direct/punctuation sibling, traversal sibling, spaced traversal, case-variant root, exact quoted root, quoted comma/semicolon, quoted structural suffix, quoted-key unquoted path tail, embedded root after `:`, `,`, `=`, POSIX sibling tokens and parent traversal root token. Нет тестов для B1 opening-delimiter embedded root and B2 quoted-key tail with quoted path-like value.
* Документация проверена по `repo-after/docs/release-management/audit-package.md`. Документ описывает atomic `<repo>` replacement, platform-aware separators/case semantics, whole-segment traversal scanning, strict start-boundary, quoted/backtick exact-root handling and fail-closed behavior for ambiguous quoted-punctuation/non-repository paths. Реализация расходится с этим документом по B1 и B2.
* Проверка прошлых отчётов выполнена. В пакете доступны r01-r10 reports из `metadata.previousVerdictChain`; прошлые blockers прочитаны и сопоставлены с `metadata.blockerClosureList`. Конкретные исторические examples в основном закрыты текущими tests, но общий sanitizer contract остаётся не закрыт из-за B1/B2 выше. Доказательств сокращения или подмены предыдущих отчётов внутри текущего пакета не найдено.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `14/14`, `audit-loop-stabilization` `14/14`, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`, а также sanitizer fixture. Эти artifacts подтверждают выполненный набор проверок, но не покрывают residual cases B1/B2.
* Проверка области не выявила лишних runtime/API изменений. Изменения ограничены audit package build tool, integration tests, release-management documentation, generated docs index, task card, diary and saved r01-r10 reports for previous verdict chain. Public API, игровой runtime, rendering/input/editor/browser submission path and Godot compatibility surface не менялись.
* Проверка секретов и локальных данных не выявила реальных секретов, приватных ключей, токенов или конфиденциальных локальных путей в current included artifacts. Синтетические Windows/Linux/POSIX paths находятся в tests, docs, patch or saved audit reports as reviewer examples; current preflight evidence uses `<repo>` placeholders and does not disclose the local repo-root.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r10.md`
* `previous blockers closure`: r01 `B1`, r02 `B1`, r03 `B1`, r04 `B1`, r05 `B1`, r06 `B1`/`B2`, r07 `B1`, r08 `B1`/`B2`, r09 `B1`/`B2`, r10 `B1`/`B2` checked; closure remains incomplete due to current B1/B2.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r11/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: no snapshot gap; behavioral coverage gap for opening-delimiter embedded POSIX paths and quoted-key tails with quoted path-like value.
* Проверенные evidence paths: `evidence/T-0982-r11/preflight/audit-loop-stabilization/*`, `evidence/T-0982-r11/preflight/t0982-r11-build-tool-build/*`, `evidence/T-0982-r11/preflight/t0982-r11-focused-sanitizer-tests/*`, `evidence/T-0982-r11/preflight/t0982-r11-sanitizer-fixture/*`, `evidence/T-0982-r11/preflight/t0982-r11-verify-audit-contracts/*`, `evidence/T-0982-r11/preflight/t0982-r11-update-docs-check/*`, `evidence/T-0982-r11/preflight/t0982-r11-verify-docs/*`, `evidence/T-0982-r11/preflight/t0982-r11-verify-audit-followups/*`, `evidence/T-0982-r11/preflight/t0982-r11-verify-licenses/*`, `evidence/T-0982-r11/preflight/t0982-r11-git-diff-check/*`

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. Пакет r11 закрывает многие конкретные regressions из r01-r10 and contains useful focused tests/docs/evidence, but sanitizer still has two fail-open path-token cases. До исправления B1 и B2 принимать `T-0982` небезопасно: imported preflight evidence can still look portable after masking a machine-local repo-root occurrence that is not an actual repo-root token or safe child path.
