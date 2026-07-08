VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0982` итерации `r04`. Область пакета одиночная: закрыть прошлые sanitizer-блокеры r01, r02 и r03, заменять локальный repo-root только для точного корня или безопасных дочерних путей, отклонять placeholder-per-character evidence и сохранять direct/punctuation/parent-traversal sibling absolute paths как отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
* Предыдущие r01/r02/r03 blocker-и по прямому sibling prefix, punctuation sibling prefix и parent traversal sibling path действительно закрыты тестами текущего пакета: focused sanitizer suite и audit-loop-stabilization проходят `5/5`.
* Изменение всё ещё нельзя принять: replacement выполняется case-insensitive всегда. На case-sensitive файловых системах Linux path с тем же текстом, но другой капитализацией сегмента, не является дочерним путём текущего репозитория, однако текущий sanitizer может заменить его на `<repo>` и тем самым скрыть нерепозиторный machine-local absolute path.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r04`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: `Close T-0982 r01, r02 and r03 sanitizer blockers by replacing repo-root only for exact roots or safe child paths, rejecting broken placeholder-per-character evidence, and preserving direct, punctuation and parent-traversal sibling absolute paths as E2D-BUILD-AUDIT-ABSOLUTE-PATH blockers.`
* `metadata.previousVerdictChain`: `["docs/verdicts/release-management/t-0982-audit-r01.md", "docs/verdicts/release-management/t-0982-audit-r02.md", "docs/verdicts/release-management/t-0982-audit-r03.md"]`
* `metadata.blockerClosureList`: r01 `B1`, r02 `B1`, r03 `B1` заявлены закрытыми через `t0982-r04-focused-sanitizer-tests` и `audit-loop-stabilization`
* Проверенные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r02.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r03.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`
* Проверенные артефакты: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`, `evidence/T-0982-r04/preflight/*`
* `combined scope`: не используется, область одиночная

BLOCKERS:

* B1

  * Что не так: sanitizer ищет `repoRoot` через case-insensitive comparison независимо от платформы. На Linux путь `/tmp/e2d/Repo/logs.txt` при фактическом `repoRoot = /tmp/e2d/repo` не является дочерним путём этого репозитория на case-sensitive файловой системе. Текущий код всё равно найдёт `/tmp/e2d/Repo` как совпадение с `/tmp/e2d/repo`, увидит после него `/`, не найдёт `..`-сегмент и заменит prefix на `<repo>`. В архив попадёт `<repo>/logs.txt`, а исходный нерепозиторный абсолютный путь будет скрыт.
  * Почему это важно: текущая задача разрешает заменять только настоящий локальный repo-root или безопасный дочерний путь внутри него. Case-variant path на case-sensitive платформе является нерепозиторным machine-local path. После текущей замены обычная проверка `E2D-BUILD-AUDIT-ABSOLUTE-PATH` уже не увидит исходный абсолютный путь, потому drive/root-prefix удалён. Это повторяет класс проблемы r01-r03: sanitizer скрывает не тот путь, который имел право нормализовать.
  * Что исправить: сделать replacement platform-aware или fail-closed. Минимально: для Windows можно оставить case-insensitive сравнение, а для Linux использовать ordinal case-sensitive matching при поиске repo-root candidates. Более надёжный вариант — выделять path-token, нормализовать его как путь и заменять только если он доказуемо относится к текущему `repoRoot` с корректной для платформы семантикой сравнения; все case-variant/non-repository absolute paths должны оставаться для `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
  * Как проверить исправление: добавить focused regression для non-Windows/case-sensitive path. Пример: при `fixture.RepositoryRoot = /tmp/.../repo` записать в imported preflight `output.txt` путь с изменённой капитализацией последнего сегмента, например `ToggleCase(fixture.RepositoryRoot) + "/logs.txt"` или case-variant sibling path, который не равен `fixture.RepositoryRoot` ordinal. На Linux ожидаемый результат — `audit package` завершается отказом `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не создаёт ZIP с `<repo>/logs.txt`. Существующие r01-r03 regressions и позитивный child-path test должны продолжать проходить.
  * Проверка опровержения: проверены текущие tests, документация и evidence. Набор `AuditPackageSanitizesPreflightEvidenceRepositoryRootAtomically`, `AuditPackageRejectsPreflightEvidencePathThatOnlySharesRepositoryRootPrefix`, `AuditPackageRejectsPreflightEvidencePunctuationSiblingPathPrefix` и `AuditPackageRejectsPreflightEvidenceTraversalSiblingPathPrefix` покрывает ordinary text, настоящие child paths, прямой sibling prefix, punctuation sibling prefix и `..` traversal. Case-variant non-repository absolute path не покрыт. Документация не снимает проблему, потому она требует заменять только repo-root/end-of-line или safe child path, а нерепозиторные machine paths оставлять блокерами.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `ReplaceRepoRootPathCandidates`, строки 1356-1375; `ReplaceRepoRootPathCandidate`, строки 1385-1423; `IsRepoRootPathCandidateBoundary`, строки 1425-1449; `ValidateMachineLocalPathText`, строки 4536-4544
    * `Criterion`: `metadata.scopeSummary`; `repo-after/TASKS.md`, строки 3728-3731, 3740-3743, 3759, 3763-3766, 3771-3774, 3795; `repo-after/docs/release-management/audit-package.md`, строка 451
    * `Evidence`: строка 1358 создаёт candidate set с `StringComparer.OrdinalIgnoreCase`; строка 1397 ищет candidate через `text.IndexOf(candidate, searchIndex, StringComparison.OrdinalIgnoreCase)`. Boundary-проверка на строках 1437-1448 проверяет только следующий separator и отсутствие `..`, но не проверяет case-sensitive принадлежность path-token к `repoRoot`.
    * `Impact`: на case-sensitive платформах sanitizer может замаскировать нерепозиторный local absolute path как `<repo>/...`, и archive content validation уже не увидит исходный path.
    * `Fix`: использовать платформенно корректное сравнение или path-token normalization before replacement; case-variant non-repository paths должны fail closed.
    * `Verification`: focused integration test на Linux/non-Windows должен ожидать `E2D-BUILD-AUDIT-ABSOLUTE-PATH` для case-variant absolute path; focused sanitizer suite должен продолжать проходить для настоящих repo child paths и прежних r01-r03 regressions.

EVIDENCE_REVIEW:

* Полнота снимков проверена. `metadata/repo-file-snapshots.json` содержит полные `repo-after/` и `repo-before/` snapshots для всех изменённых файлов; `repo-file-hashes.json` и `SHA256SUMS.txt` согласованы с содержимым архива.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`. Imported preflight text evidence теперь проходит через staging-copy, `SanitizePreflightEvidenceText`, `ReplaceRepoRootPathCandidates`, `ValidateNoBrokenPreflightPlaceholderText`, затем через обычную secret/path validation. Текущий blocker находится в semantics replacement comparison.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Focused suite покрывает readable output, native child path, slash-normalized child path, стабильность существующего `<repo>`, отказ на placeholder-per-character pattern, отказ на `repo backup/...`, отказ на `repo)backup/...` и отказ на `repo/../repo backup/...`. Case-variant non-repository path на case-sensitive платформе не покрыт.
* Документация проверена по `repo-after/docs/release-management/audit-package.md`. Документ описывает атомарную замену только целого локального repo-root/end-of-line или safe child path и сохранение sibling/non-repository paths как blockers. Реализация расходится с этим для case-variant absolute paths на case-sensitive файловых системах.
* Проверка прошлых отчётов выполнена. `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md`, `t-0982-audit-r02.md` и `t-0982-audit-r03.md` доступны из `metadata.previousVerdictChain`. Текущие r04 tests закрывают конкретные прошлые examples: direct sibling prefix, punctuation sibling prefix и parent traversal sibling path. Доказательств текущего переписывания этих reports в пределах доступного пакета не найдено.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `5/5`, audit-loop-stabilization `5/5`, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`, а также sanitizer fixture. Эти evidence подтверждают заявленные проверки, но не опровергают B1, потому case-variant regression в них отсутствует.
* Проверка области не выявила лишних runtime/API изменений. Изменения ограничены audit package build tool, integration tests, release-management documentation, generated docs index, task card, diary и сохранёнными r01/r02/r03 reports для previous verdict chain. Public API, игровой runtime, рендеринг, ввод, editor/browser submission path и Godot compatibility surface не менялись.
* Проверка секретов и локальных данных не выявила реальных секретов, приватных ключей, токенов или конфиденциальных локальных путей в текущих included artifacts. Синтетические Windows paths находятся в сохранённых audit reports и patch-блоках previous verdict context; это исторические аудиторские примеры, а не новый leak текущего пакета.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r02.md`, `repo-after/docs/verdicts/release-management/t-0982-audit-r03.md`
* `previous blockers closure`: r01 `B1`, r02 `B1`, r03 `B1` проверены; их конкретные examples закрыты, но общий sanitizer contract остаётся не закрыт из-за B1 выше
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r04/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: нет по полноте snapshots; есть поведенческий gap в sanitizer regression coverage для case-variant non-repository absolute paths
* Проверенные evidence paths: `evidence/T-0982-r04/preflight/audit-loop-stabilization/*`, `t0982-r04-build-tool-build/*`, `t0982-r04-focused-sanitizer-tests/*`, `t0982-r04-sanitizer-fixture/*`, `t0982-r04-verify-audit-contracts/*`, `t0982-r04-update-docs-check/*`, `t0982-r04-verify-docs/*`, `t0982-r04-verify-audit-followups/*`, `t0982-r04-verify-licenses/*`, `t0982-r04-git-diff-check/*`

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. Пакет r04 закрывает конкретные r01/r02/r03 regressions и стабилизирует focused sanitizer suite, но replacement всё ещё может скрыть нерепозиторный absolute path на case-sensitive платформах из-за unconditional case-insensitive matching. До исправления B1 принимать `T-0982` небезопасно: imported preflight evidence может выглядеть переносимым после маскировки machine-local path, который не является фактическим repo-root child path.
