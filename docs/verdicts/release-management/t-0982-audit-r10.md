VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0982` итерации `r10`. Область пакета одиночная: закрыть sanitizer-блокеры r01-r09 для imported preflight evidence, заменять exact repo-root tokens и безопасные дочерние repo-root paths с платформенной семантикой регистра, разделителей, границ token-а, whole-segment traversal и quoted-tail, отклонять placeholder-per-character artifacts и сохранять нерепозиторные machine-local absolute paths как `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
* Пакет содержит полные snapshots изменённых файлов. Предыдущие verdict-файлы r01-r09 доступны в архиве, `metadata.blockerClosureList` связывает каждый прошлый blocker с текущими focused sanitizer tests и `audit-loop-stabilization`. Конкретные прошлые examples r01-r09 в основном закрыты текущими tests и изменениями: direct/punctuation sibling prefix, traversal sibling, spaced traversal, parent-traversal root-token, case-variant root, POSIX backslash, quoted comma/semicolon tails, quoted structural tails и compact JSON exact-root tokens.
* Изменение нельзя принять: sanitizer всё ещё не доказывает, что найденный `repoRoot` является самостоятельным exact token-ом или настоящим дочерним путём репозитория. Остались два доказуемых fail-open случая: POSIX quoted-key tail вида `"<repo>","backup":/logs` и repo-root occurrence внутри более длинного POSIX absolute path перед допустимым start delimiter-ом вроде `:`. В обоих случаях нерепозиторный absolute path может быть частично заменён на `<repo>`, после чего штатная проверка local paths уже не видит исходный repo-root.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r10`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: `Close T-0982 r01-r09 sanitizer blockers by replacing exact repo-root tokens and safe child paths with platform path-case, separator, token-boundary, whole-segment traversal and structural quoted-tail semantics, rejecting broken placeholder-per-character evidence, and preserving direct, punctuation, parent-traversal, spaced-segment parent-traversal, parent-traversal-root-token, case-variant, POSIX backslash and quoted-punctuation non-repository absolute paths as E2D-BUILD-AUDIT-ABSOLUTE-PATH blockers.`
* `metadata.previousVerdictChain`: `docs/verdicts/release-management/t-0982-audit-r01.md` ... `docs/verdicts/release-management/t-0982-audit-r09.md`
* `metadata.blockerClosureList`: содержит closure-записи для r01 `B1`, r02 `B1`, r03 `B1`, r04 `B1`, r05 `B1`, r06 `B1`/`B2`, r07 `B1`, r08 `B1`/`B2`, r09 `B1`/`B2`
* Проверенные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r09.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`
* Проверенные артефакты: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`, `evidence/T-0982-r10/preflight/*`
* `combined scope`: не используется, область одиночная

BLOCKERS:

* B1

  * Что не так: quoted exact-root sanitizer всё ещё может скрыть POSIX sibling absolute path, если после закрывающей кавычки идёт comma/semicolon и quoted-key-looking tail. Метод `IsSafeAfterExactRootListSeparator` принимает quote/backtick/apostrophe после `,` или `;`, а `IsStructuredQuotedKeyTail` возвращает `true`, когда видит `"key":`. Он не проверяет, что после `:` не продолжается path-like suffix. Например, строка `"/home/user/repo","backup":/logs.txt` при `repoRoot = /home/user/repo` будет преобразована в `"<repo>","backup":/logs.txt`. Исходная POSIX path-строка содержит sibling segment `repo","backup":` и указывает вне `/home/user/repo`, но после замены repo-root исчезает из текста.
  * Почему это важно: текущая задача требует сохранять quoted-punctuation и другие нерепозиторные absolute paths как `E2D-BUILD-AUDIT-ABSOLUTE-PATH` blockers. Документация r10 прямо обещает, что structural delimiter безопасен только когда он сам не продолжает path-like suffix. Текущий код это условие не доказывает для quoted-key tail: `","backup":/logs.txt` выглядит структурно только до двоеточия, но дальше продолжает POSIX path.
  * Что исправить: сделать quoted exact-root replacement действительно token-aware. После comma/semicolon и quoted-key-looking tail нужно проверять хвост после `:`; если дальше продолжается path-like suffix, такой input должен fail closed и оставаться под `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. Более надёжный вариант — для `metadata.json` нормализовать JSON values структурно, а для raw `output.txt`/`result.txt` заменять только строго доказанные exact-root tokens без неоднозначного POSIX path-tail.
  * Как проверить исправление: добавить focused regression через production `audit package`, желательно active на POSIX/non-Windows, где imported preflight `output.txt` содержит `"{slashRepoRoot}","backup":/logs.txt` и `"{slashRepoRoot}";"backup":/logs.txt`. Ожидаемый результат — отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не ZIP с `"<repo>","backup":/logs.txt`. Позитивный compact JSON exact-root test должен продолжать проходить для настоящих isolated tokens.
  * Проверка опровержения: проверены текущие tests и evidence. `AuditPackageRejectsPreflightEvidenceQuotedStructuralSuffixSiblingPathTokens` покрывает `",]backup`, `","backup/logs.txt` и `";}backup`, но не покрывает quoted-key-looking tail с двоеточием и path suffix после него. `AuditPackageSanitizesExactPreflightEvidenceRepositoryRootTokens` покрывает настоящие compact JSON object/array/result forms, но не доказывает отказ для POSIX sibling path вида `repo","backup":/logs`. Evidence `12/12` и `audit-loop-stabilization` не опровергают проблему, потому этот case отсутствует.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `IsSafeAfterExactRootListSeparator`, строки 1517-1535; `IsStructuredQuotedKeyTail`, строки 1559-1577; `ValidateMachineLocalPathText`, строки 4702-4710.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/TASKS.md`, строки 3795 и 3813-3815; `repo-after/docs/release-management/audit-package.md`, строка 451.
    * `Evidence`: строки 1530-1532 принимают quoted-key tail, а строки 1559-1577 проверяют только наличие closing quote и `:`, не проверяя path-like продолжение после `:`. После замены `ValidateMachineLocalPathText` уже не видит `/home/user/repo`.
    * `Impact`: imported preflight evidence может выглядеть переносимым после маскировки нерепозиторного POSIX absolute path.
    * `Fix`: token-aware quoted-tail validation или структурная JSON-normalization только для JSON artifacts; ambiguous raw text tails должны fail closed.
    * `Verification`: focused sanitizer test должен ожидать `E2D-BUILD-AUDIT-ABSOLUTE-PATH` для `repoRoot","backup":/logs.txt` и сохранять успешную замену для настоящего compact JSON exact-root token.

* B2

  * Что не так: start-boundary проверка repo-root candidate тоже остаётся fail-open. `IsRepoRootPathCandidateStartBoundary` считает `:`, `,`, `;`, скобки, кавычки, `=` и whitespace безопасной границей перед `repoRoot`. На POSIX эти символы могут быть частью имени файла. Поэтому строка вида `/home/user/prefix:/home/user/repo/logs.txt` при `repoRoot = /home/user/repo` будет преобразована в `/home/user/prefix:<repo>/logs.txt`: совпадение `/home/user/repo` находится внутри более длинного absolute path, а не является самостоятельным repo-root token-ом или дочерним путём репозитория.
  * Почему это важно: текущий scope r10 говорит про `token-boundary` semantics и сохранение нерепозиторных machine paths как blockers. До sanitization такая строка содержит literal repo-root и была бы остановлена `E2D-BUILD-AUDIT-ABSOLUTE-PATH`; после частичной замены остаётся `/home/user/prefix:<repo>/logs.txt`, где исходный repo-root уже скрыт. Это тот же класс риска, который r01-r09 пытались закрыть на правой границе token-а, но теперь на левой границе.
  * Что исправить: проверять не одиночный символ перед candidate, а полный path-token. Если перед `repoRoot` в той же path-like строке есть POSIX filename characters и путь фактически начинается раньше, совпадение нельзя заменять. Практический вариант — выделять весь absolute path-token вокруг candidate и заменять только если весь token равен repo-root или нормализуется в дочерний путь внутри repo-root; остальные occurrences должны оставаться под `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
  * Как проверить исправление: добавить focused regression через production `audit package`, где imported preflight `output.txt` содержит POSIX path вроде `/home/user/prefix:{slashRepoRoot}/logs.txt` или `{Path.GetDirectoryName(slashRepoRoot)}/prefix:{slashRepoRoot}/logs.txt` на case-sensitive POSIX. Ожидаемый результат — `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не ZIP с `/home/user/prefix:<repo>/logs.txt`. Позитивные cases `path: {slashRepoRoot}/src/file.cs` и JSON exact-root tokens должны продолжать заменяться.
  * Проверка опровержения: проверены tests, documentation и preflight evidence. Текущие negative tests проверяют suffix-boundary cases после repo-root: direct sibling, punctuation sibling, traversal sibling, spaced traversal, case-variant, POSIX backslash, quoted punctuation and structural tails. Нет теста, который проверяет repo-root occurrence inside a larger POSIX absolute path with a filename-valid character immediately before the candidate. Документация не снимает проблему, потому обещает token-boundary semantics и сохранение нерепозиторных machine paths.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `ReplaceRepoRootPathCandidate`, строки 1405-1447; `IsRepoRootPathCandidateStartBoundary`, строки 1652-1661; `ValidateMachineLocalPathText`, строки 4702-4710.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/TASKS.md`, строка 3795; `repo-after/docs/release-management/audit-package.md`, строка 451.
    * `Evidence`: строка 1660 принимает `:`/`,`/`;`/`=`/whitespace as start-boundary без проверки полного path-token-а. Для `/home/user/prefix:/home/user/repo/logs.txt` это разрешает replacement, а после replacement строка больше не содержит `/home/user/repo`, которую ищет `ValidateMachineLocalPathText`.
    * `Impact`: sanitizer может скрыть repo-root occurrence внутри нерепозиторного absolute path и пропустить machine-local path leak в audit ZIP.
    * `Fix`: full token parsing around candidate before replacement; replacement only for whole exact repo-root token or normalized safe child token.
    * `Verification`: focused sanitizer test должен ожидать `E2D-BUILD-AUDIT-ABSOLUTE-PATH` для POSIX absolute path, где repo-root starts after filename-valid punctuation inside a longer path; current positive exact-root and child-path tests must remain green.

EVIDENCE_REVIEW:

* Полнота снимков проверена. `metadata/repo-file-snapshots.json` содержит `fullContentIncluded: true` для всех 15 изменённых файлов. `repo-file-hashes.json` совпадает с `repo-after/`; `SHA256SUMS.txt` успешно проверен по файлам архива.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`. Imported preflight evidence проходит через `PreparePreflightEvidenceSource`, `WriteNormalizedPreflightMetadata`, `SanitizePreflightEvidenceText`, `ReplaceRepoRootPathCandidates`, boundary checks, placeholder validation и затем через archive content validation. Основная проблема текущей итерации — sanitizer всё ещё основан на локальных delimiter heuristics, а не на доказанной принадлежности полного path-token-а к repo-root.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Focused suite содержит 12 tests: readable child path sanitization, broken placeholder rejection, direct/punctuation sibling rejection, traversal sibling rejection, spaced-segment traversal rejection, case-variant rejection on case-sensitive platforms, exact quoted root sanitization, quoted comma/semicolon sibling rejection, quoted structural suffix rejection, POSIX sibling token rejection и parent-traversal root-token rejection. Эти tests не покрывают B1 quoted-key-tail path continuation и B2 left-boundary occurrence inside a longer POSIX absolute path.
* Документация проверена по `repo-after/docs/release-management/audit-package.md`. Документ описывает atomic `<repo>` replacement только для whole repo-root/safe child paths, platform-aware case/separator semantics, whole-segment traversal scanning, quoted/backtick exact-root handling and fail-closed behavior for ambiguous quoted-punctuation tails. Реализация расходится с этим документом по B1 и B2.
* Проверка прошлых verdict-файлов выполнена. В пакете доступны r01-r09 reports из `metadata.previousVerdictChain`. Прошлые blockers прочитаны и сопоставлены с `metadata.blockerClosureList`; текущие tests закрывают конкретные исторические examples, но не закрывают общий token-boundary contract из-за B1/B2 выше. Доказательств переписывания или сокращения сохранённых отчётов внутри текущего пакета не найдено.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `12/12`, `audit-loop-stabilization` `12/12`, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`, а также sanitizer fixture. Эти evidence подтверждают выполненный набор проверок, но не покрывают два residual cases выше.
* Проверка области не выявила лишних runtime/API изменений. Изменения ограничены audit package build tool, integration tests, release-management documentation, generated docs index, task card, diary и сохранёнными r01-r09 reports для previous verdict chain. Public API, игровой runtime, rendering/input/editor/browser submission path и Godot compatibility surface не менялись.
* Проверка секретов и локальных данных не выявила реальных секретов, приватных ключей, токенов или конфиденциальных локальных путей в current included artifacts. Синтетические Windows/Linux/POSIX paths находятся в tests, docs, patch или saved audit reports как примеры прошлых blocker-ов; current preflight evidence использует `<repo>` placeholders и не раскрывает локальный repo-root.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r09.md`
* `previous blockers closure`: r01 `B1`, r02 `B1`, r03 `B1`, r04 `B1`, r05 `B1`, r06 `B1`/`B2`, r07 `B1`, r08 `B1`/`B2`, r09 `B1`/`B2` проверены; конкретные прошлые examples закрыты, но current token-boundary contract остаётся не закрыт из-за B1/B2.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r10/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: нет по полноте snapshots; есть поведенческий gap в sanitizer regression coverage для quoted-key path continuation и left-boundary occurrence inside a larger POSIX absolute path.
* Проверенные evidence paths: `evidence/T-0982-r10/preflight/audit-loop-stabilization/*`, `evidence/T-0982-r10/preflight/t0982-r10-build-tool-build/*`, `evidence/T-0982-r10/preflight/t0982-r10-focused-sanitizer-tests/*`, `evidence/T-0982-r10/preflight/t0982-r10-sanitizer-fixture/*`, `evidence/T-0982-r10/preflight/t0982-r10-verify-audit-contracts/*`, `evidence/T-0982-r10/preflight/t0982-r10-update-docs-check/*`, `evidence/T-0982-r10/preflight/t0982-r10-verify-docs/*`, `evidence/T-0982-r10/preflight/t0982-r10-verify-audit-followups/*`, `evidence/T-0982-r10/preflight/t0982-r10-verify-licenses/*`, `evidence/T-0982-r10/preflight/t0982-r10-git-diff-check/*`

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. Пакет r10 закрывает много конкретных regressions из r01-r09 и содержит полезный focused suite, но sanitizer всё ещё может маскировать нерепозиторные POSIX absolute paths как `<repo>...` из-за неполной проверки полного path-token-а. До исправления B1 и B2 принимать `T-0982` небезопасно: imported preflight evidence может выглядеть переносимым после скрытия machine-local path, который не является фактическим repo-root или безопасным дочерним путём репозитория.
