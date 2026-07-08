VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0982` итерации `r07`. Область пакета одиночная: закрыть sanitizer-блокеры r01-r06, заменять quoted exact repo-root tokens и безопасные дочерние пути с платформенной семантикой регистра и разделителей, отклонять placeholder-per-character artifacts и сохранять direct/punctuation/parent-traversal/case-variant/POSIX-backslash/quoted-punctuation нерепозиторные absolute paths как отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
* Пакет содержит полные snapshots изменённых файлов. Предыдущие r01-r06 verdict-файлы доступны в архиве, `metadata.blockerClosureList` связывает каждый прошлый blocker с текущими focused sanitizer tests и `audit-loop-stabilization`. Конкретные прошлые примеры r01-r06 в основном закрыты тестами: direct sibling prefix, punctuation sibling prefix, `repo/../repo backup`, case-variant root, quoted exact root, POSIX backslash-after-root и POSIX quote-punctuation sibling path.
* Изменение нельзя принять: текущий parser parent-traversal segments всё ещё пропускает `..`, если segment заканчивается не path separator-ом или концом строки, а обычным token delimiter-ом, например кавычкой, пробелом, запятой или точкой с запятой. В таком случае sanitizer заменяет repo-root prefix на `<repo>` и скрывает absolute path, который указывает вне репозитория.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r07`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: `Close T-0982 r01-r06 sanitizer blockers by replacing quoted exact repo-root tokens and safe child paths with platform path-case and separator semantics, rejecting broken placeholder-per-character evidence, and preserving direct, punctuation, parent-traversal, case-variant, POSIX backslash and quoted-punctuation non-repository absolute paths as E2D-BUILD-AUDIT-ABSOLUTE-PATH blockers.`
* `metadata.previousVerdictChain`: `["docs/verdicts/release-management/t-0982-audit-r01.md", "docs/verdicts/release-management/t-0982-audit-r02.md", "docs/verdicts/release-management/t-0982-audit-r03.md", "docs/verdicts/release-management/t-0982-audit-r04.md", "docs/verdicts/release-management/t-0982-audit-r05.md", "docs/verdicts/release-management/t-0982-audit-r06.md"]`
* `metadata.blockerClosureList`: r01 `B1`, r02 `B1`, r03 `B1`, r04 `B1`, r05 `B1`, r06 `B1`/`B2` заявлены закрытыми через `t0982-r07-focused-sanitizer-tests` и `audit-loop-stabilization`
* Проверенные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r02.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r03.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r04.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r05.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r06.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`
* Проверенные артефакты: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`, `evidence/T-0982-r07/preflight/*`
* `combined scope`: не используется, область одиночная

BLOCKERS:

* B1

  * Что не так: sanitizer всё ещё может скрыть parent-traversal absolute path, если `..` стоит в конце path-token-а перед кавычкой, пробелом, запятой, точкой с запятой или похожим текстовым delimiter-ом. Например, строка вида `quoted parent path: "C:\work\repo\.."` при `repoRoot = C:\work\repo` проходит start-boundary, видит `\` после repo-root как child separator, вызывает `HasParentTraversalSegmentAfterRepoRoot`, но этот метод сканирует segment до следующего path separator-а или перевода строки. Закрывающая кавычка не считается концом segment-а, поэтому segment получается не ровно `..`, а `.."`, parent traversal не обнаруживается, и строка превращается в `quoted parent path: "<repo>\.."`. Аналогично для POSIX: `"/home/user/repo/.."` превращается в `"<repo>/.."`.
  * Почему это важно: текущая область r07 прямо требует сохранять parent-traversal и другие нерепозиторные absolute paths как `E2D-BUILD-AUDIT-ABSOLUTE-PATH` blockers. Путь `repoRoot/..` указывает на родительский каталог вне репозитория, а не на безопасный дочерний путь. После текущей замены исходный machine-local prefix исчезает, поэтому последующая проверка локальных путей уже не может заблокировать unsafe evidence.
  * Что исправить: `HasParentTraversalSegmentAfterRepoRoot` должен анализировать полноценный path-token, а не только последовательность до path separator-а или newline. Минимально нужно считать безопасные token delimiters концом path-token-а и обнаруживать segment `..` перед ними. Более надёжный вариант — выделять весь path-token, нормализовать его с платформенной семантикой и заменять только если нормализованный путь остаётся внутри `repoRoot`; ambiguous token-ы с `..` должны fail-closed оставаться под `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
  * Как проверить исправление: добавить focused regression через production `audit package`, где imported preflight `output.txt` содержит `quoted parent path: "{fixture.RepositoryRoot + Path.DirectorySeparatorChar + ".."}"` и slash-normalized вариант `"{fixture.RepositoryRoot.Replace('\\', '/')}/.."`. Ожидаемый результат — команда завершается отказом `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не создаёт ZIP с `<repo>/..` или `<repo>\..`. Существующие позитивные tests для настоящих repo child paths и exact repo-root tokens должны продолжать проходить.
  * Проверка опровержения: проверены текущие tests, документация, previous blocker closure list и preflight evidence. `AuditPackageRejectsPreflightEvidenceTraversalSiblingPathPrefix` покрывает только форму `repo/../repo backup/...`, где после `..` сразу идёт path separator, поэтому текущий parser видит segment ровно `..`. Набор `AuditPackageRejectsPreflightEvidencePosixSiblingPathTokens` покрывает POSIX backslash и quote-punctuation sibling path, но не покрывает `repoRoot/..` перед закрывающей кавычкой или другим delimiter-ом. Evidence `8/8` не опровергает проблему, потому token-ending parent traversal case отсутствует в focused suite и `audit-loop-stabilization`.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `IsRepoRootPathCandidateBoundary`, строки 1449-1477; `HasParentTraversalSegmentAfterRepoRoot`, строки 1507-1542; `ValidateMachineLocalPathText`, строки 4606-4614.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/TASKS.md`, строки 3728-3731, 3759, 3763-3766, 3795; `repo-after/docs/release-management/audit-package.md`, строка 451.
    * `Evidence`: `IsRepoRootPathCandidateBoundary` разрешает child path replacement, если после repo-root стоит platform separator и `HasParentTraversalSegmentAfterRepoRoot` вернул `false`. В `HasParentTraversalSegmentAfterRepoRoot` цикл строк 1519-1521 останавливает segment только на path separator-е или переводе строки, поэтому `..` перед кавычкой/пробелом/запятой не распознаётся как отдельный parent-traversal segment.
    * `Impact`: audit package может включить imported preflight evidence, где absolute path вне репозитория замаскирован как `<repo>/..`; это нарушает fail-closed sanitizer contract текущей задачи.
    * `Fix`: перейти на token-aware/path-normalizing проверку или явно учитывать token delimiters при поиске `..` segments.
    * `Verification`: focused sanitizer tests должны ожидать `E2D-BUILD-AUDIT-ABSOLUTE-PATH` для quoted/spaced/comma-terminated `repoRoot/..`; текущие r01-r06 regression tests и positive exact-root/child-path tests должны остаться зелёными.

EVIDENCE_REVIEW:

* Полнота снимков проверена. `metadata/repo-file-snapshots.json` содержит полные `repo-after/` и `repo-before/` snapshots для всех изменённых файлов; `repo-file-hashes.json` совпадает с `repo-after/`, `SHA256SUMS.txt` совпадает с файлами архива.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`. Imported preflight evidence проходит через staging-copy, `PreparePreflightEvidenceSource`, `WriteNormalizedPreflightMetadata`, `SanitizePreflightEvidenceText`, `ReplaceRepoRootPathCandidates`, `ValidateNoBrokenPreflightPlaceholderText` и затем через обычную archive content validation. Конкретные r01-r06 fixes присутствуют, но parent traversal detection всё ещё не token-aware.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Focused suite содержит восемь tests: positive child path/readability, broken placeholder rejection, direct sibling rejection, punctuation sibling rejection, traversal sibling rejection, case-variant rejection on case-sensitive platforms, exact quoted root sanitization и POSIX sibling token rejection. Нет теста для `repoRoot/..` как самостоятельного quoted/text token-а.
* Документация проверена по `repo-after/docs/release-management/audit-package.md`. Документ описывает atomic `<repo>` replacement только для safe child paths и обещает, что parent-traversal/non-repository paths остаются blockers. Реализация расходится с этим обещанием для `..` перед token delimiter-ом.
* Проверка прошлых verdict-файлов выполнена. В пакете доступны r01-r06 отчёты из `metadata.previousVerdictChain`. Прошлые blocker-и прочитаны: r01 — direct sibling prefix, r02 — punctuation sibling prefix, r03 — parent traversal sibling path через `repo/../repo backup`, r04 — case-insensitive matching on case-sensitive platforms, r05 — exact quoted repo-root token, r06 — POSIX backslash sibling и quote-punctuation sibling. Текущие tests закрывают эти конкретные cases, но не закрывают B1 выше.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `8/8`, audit-loop-stabilization `8/8`, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`, а также sanitizer fixture. Эти artifacts подтверждают выполненные проверки, но не покрывают B1.
* Проверка области не выявила лишних runtime/API изменений. Изменения ограничены audit package build tool, integration tests, release-management documentation, generated docs index, task card, diary и сохранёнными r01-r06 reports для previous verdict chain. Public API, игровой runtime, rendering/input/editor/browser submission path и Godot compatibility surface не менялись.
* Проверка секретов и локальных данных не выявила реальных секретов, приватных ключей, токенов или конфиденциальных локальных путей в current included artifacts. Синтетические Windows/Linux paths находятся в saved audit reports, tests, docs или patch-блоках previous verdict context; current preflight evidence использует `<repo>` placeholders и не раскрывает локальный repo-root.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r02.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r03.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r04.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r05.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r06.md`
* `previous blockers closure`: r01 `B1`, r02 `B1`, r03 `B1`, r04 `B1`, r05 `B1`, r06 `B1`/`B2` проверены; конкретные прошлые examples закрыты, но общий parent-traversal contract остаётся не закрыт из-за B1 выше.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r07/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: нет по полноте snapshots; есть поведенческий gap в sanitizer regression coverage для token-ending parent traversal paths.
* Проверенные evidence paths: `evidence/T-0982-r07/preflight/audit-loop-stabilization/*`, `evidence/T-0982-r07/preflight/t0982-r07-build-tool-build/*`, `evidence/T-0982-r07/preflight/t0982-r07-focused-sanitizer-tests/*`, `evidence/T-0982-r07/preflight/t0982-r07-sanitizer-fixture/*`, `evidence/T-0982-r07/preflight/t0982-r07-verify-audit-contracts/*`, `evidence/T-0982-r07/preflight/t0982-r07-update-docs-check/*`, `evidence/T-0982-r07/preflight/t0982-r07-verify-docs/*`, `evidence/T-0982-r07/preflight/t0982-r07-verify-audit-followups/*`, `evidence/T-0982-r07/preflight/t0982-r07-verify-licenses/*`, `evidence/T-0982-r07/preflight/t0982-r07-git-diff-check/*`

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. Пакет r07 закрывает конкретные r01-r06 blocker-и и улучшает sanitizer boundary handling, но всё ещё может маскировать parent-traversal absolute path `repoRoot/..` как `<repo>/..`, если `..` заканчивается token delimiter-ом. До исправления B1 принимать `T-0982` небезопасно: imported preflight evidence может выглядеть переносимым после скрытия machine-local path, который не является фактическим repo-root или безопасным дочерним путём репозитория.
