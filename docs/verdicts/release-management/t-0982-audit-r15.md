VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен audit ZIP `T-0982` итерации `r15` как одиночная область задачи. Пакет читается, `repo-after/` и `metadata/repo-file-snapshots.json` доступны, снимки изменённых файлов полные. Область заявлена как закрытие sanitizer-блокеров r01-r14 для imported preflight evidence: repo-root должен заменяться только для доказанных exact repo-root tokens и safe child paths, а нерепозиторные POSIX/машинные absolute paths с quoted/structural suffix должны оставаться блокерами.
* Изменение нельзя принять. r15 закрывает прямой r14 B1 про whitespace start-boundary и частично закрывает r14 B2 про raw quoted structural leaf suffix, но в реализации остаётся fail-open случай: array-looking structural context позволяет скрыть POSIX sibling leaf path вида `["/home/user/repo","backup":0]`. Это не просто пробел в тестах; по текущему коду такой текст будет нормализован в `["<repo>","backup":0]`, после чего обычная проверка локальных путей уже не увидит исходный machine-local absolute path.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r15`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: закрытие r01-r14 sanitizer blockers, включая сохранение `quoted-punctuation or JSON-looking quoted structural leaf non-repository absolute paths` как `E2D-BUILD-AUDIT-ABSOLUTE-PATH` blockers.
* `combined scope`: не используется, область одиночная.
* Проверенные основные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`.
* Проверенные previous verdict files: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r14.md`.
* Проверенные evidence: `evidence/T-0982-r15/preflight/*`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`.

BLOCKERS:

* B1

  * Что не так: sanitizer всё ещё может скрыть нерепозиторный POSIX absolute path с JSON-looking structural leaf suffix, если перед quoted exact-root стоит array bracket. Пример входного preflight `output.txt`: `array-looking path: ["/home/user/repo","backup":0]` при `repoRoot = /home/user/repo`. На POSIX подстрока `/home/user/repo","backup":0]` является absolute path к sibling leaf `repo","backup":0]` под `/home/user`, а не путём внутри `/home/user/repo`. Текущий код примет `[` как структурный opening context, затем после `","backup":0` посчитает tail безопасным quoted-key tail и заменит `/home/user/repo` на `<repo>`.
  * Почему это важно: текущая область r15 прямо требует сохранять JSON-looking quoted structural leaf non-repository absolute paths как `E2D-BUILD-AUDIT-ABSOLUTE-PATH` blockers. После замены строка становится `["<repo>","backup":0]`; последующая проверка `ValidateMachineLocalPathText` уже не содержит исходного `/home/user/repo` и не может заблокировать небезопасное evidence. Это нарушает fail-closed контракт sanitizer-а для audit package.
  * Что исправить: tail-проверка должна учитывать тип структурного контекста. Если exact-root находится внутри array context, нельзя после comma/semicolon принимать quoted-key-looking tail вроде `"backup":0` или `"backup":"value"` как безопасный object-tail. Надёжный вариант — структурно парсить JSON для JSON artifacts и fail closed для raw text; минимальный вариант — разделить object-value context и array-value context, разрешая array exact-root только для доказанно безопасного завершения элемента, например `]`, конец строки/текста или другой явно поддержанный array value без path-like ambiguity.
  * Как проверить исправление: добавить focused regression через production `audit package` на POSIX/non-Windows: imported preflight `output.txt` содержит `["{slashRepoRoot}\",\"backup\":0]` и `["{slashRepoRoot}\",\"backup\":\"value\"]`. Ожидаемый результат — отказ `E2D-BUILD-AUDIT-ABSOLUTE-PATH`, а не ZIP с `["<repo>","backup":0]`. Позитивные cases `["{slashRepoRoot}"]` и `{"repositoryRoot":"{slashRepoRoot}"}` должны продолжать проходить.
  * Проверка опровержения: проверены implementation, focused tests, docs, `metadata.blockerClosureList` и r15 evidence. Тест `AuditPackageRejectsPreflightEvidenceQuotedStructuralLeafSiblingPathTokens` покрывает raw/prose case с `"{repo}\",\"backup\":0"` без array/object opening context, поэтому не опровергает этот сценарий. Тест `AuditPackageSanitizesExactPreflightEvidenceRepositoryRootTokens` покрывает только positive compact array `["{repo}"]`, но не invalid array-looking leaf suffix `["{repo}","backup":0]`. Evidence `t0982-r15-focused-sanitizer-tests` и `audit-loop-stabilization` подтверждают текущие 22 теста, но не содержат этой ветки. Документация не снимает blocker: она прямо относит ambiguous tails вроде `repo\",\"backup\":0` и `repo\",\"backup\":\"value\"` к fail-closed cases.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`; `IsRepoRootExactTokenEndDelimiter`; `IsSafeRepoRootExactTokenTail`; `IsStructuredExactRootOpeningContext`; `IsSafeAfterExactRootListSeparator`; `IsStructuredQuotedKeyTail`; `ValidateMachineLocalPathText`.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/docs/release-management/audit-package.md`, preflight evidence sanitizer contract; `repo-after/TASKS.md`, `Internal substrate acceptance contract` и `Критерии приёмки`.
    * `Evidence`: `IsStructuredExactRootOpeningContext` принимает `[` перед opening quote как достаточный structural context; `IsSafeAfterExactRootListSeparator` затем передаёт quoted tail в `IsStructuredQuotedKeyTail`; `IsStructuredQuotedKeyTail` принимает `"backup":0]` или `"backup":"value"]`, если дальше нет path separator-а. В результате candidate replacement происходит, а subsequent absolute-path validation работает уже по sanitized text.
    * `Impact`: unsafe imported preflight evidence может попасть в audit ZIP как portable-looking `<repo>` artifact, хотя исходная строка содержала нерепозиторный POSIX machine-local path.
    * `Fix`: контекстно-раздельная structural-tail проверка или JSON-aware нормализация; fail closed для array-looking quoted-key leaf suffix без доказанного валидного JSON/token контекста.
    * `Verification`: новый focused sanitizer test должен падать до исправления и проходить после; команда из пакета `dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter ... --no-build --no-restore -v:minimal` должна включать новый regression вместе с текущими 22 проверками.

EVIDENCE_REVIEW:

* Полнота архива проверена. `SHA256SUMS.txt` соответствует ZIP entries, `repo-file-hashes.json` соответствует `repo-after/`, а `metadata/repo-file-snapshots.json` содержит `fullContentIncluded: true` для всех 20 изменённых файлов. Недостатка снимков, который мешал бы читать реализацию, тесты или документацию, не найдено.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`. Основной поток таков: imported text preflight evidence проходит через `PreparePreflightEvidenceSource`, `SanitizePreflightEvidenceText` и `ReplaceRepoRootPathCandidates`, затем уже sanitized artifact проверяется `ValidateSecretPolicy` и archive content validation. Именно поэтому fail-open replacement из B1 критичен: после замены исходный repo-root исчезает до machine-local path scan.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Пакет добавляет focused coverage для readable sanitizer output, broken placeholder-per-character, sibling prefix, punctuation, traversal, spaced traversal, case-variant, exact quoted roots, quoted comma/semicolon, structural suffixes, quoted-key path tails, embedded roots, whitespace roots, POSIX sibling tokens and parent traversal root token. Текущий набор не покрывает array-context structural leaf suffix из B1.
* Документация проверена по `repo-after/docs/release-management/audit-package.md` и generated docs index. Документ описывает atomic `<repo>` replacement, platform-aware separators/case, whole-segment traversal scan, delimiter-aware/current-line start-boundary и fail-closed handling для ambiguous quoted structural tails. Реализация по B1 не соответствует fail-closed части этого контракта.
* Previous verdict files r01-r14 прочитаны. `metadata.previousVerdictChain` указывает все доступные отчёты, а `metadata.blockerClosureList` содержит 22 closure-записи для прошлых blocker-ов. Конкретные r14 B1/B2 closure-записи присутствуют, но r14 B2 закрыт неполно из-за B1 этого аудита. Доказательств подмены или сокращения сохранённых verdict-файлов внутри текущего пакета не найдено.
* Preflight evidence проверены. Заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `22/22`, audit-loop-stabilization `22/22`, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check` и sanitizer fixture. Эти evidence подтверждают выполненный набор проверок, но не покрывают fail-open case B1.
* Проверка области не выявила runtime/API изменений вне задачи. Изменения ограничены audit-package tool, integration tests, release-management documentation, generated docs index, task card, diary и сохранёнными r01-r14 verdict reports для previous verdict chain.
* Проверка секретов и локальных данных не выявила реальных токенов, приватных ключей или паролей. Синтетические POSIX/Windows paths находятся в тестах, документации, patch и saved verdict examples; preflight evidence использует `<repo>` placeholders.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r14.md`
* `previous blockers closure`: r01 `B1`; r02 `B1`; r03 `B1`; r04 `B1`; r05 `B1`; r06 `B1`/`B2`; r07 `B1`; r08 `B1`/`B2`; r09 `B1`/`B2`; r10 `B1`/`B2`; r11 `B1`/`B2`; r12 `B1`/`B2`; r13 `B1`/`B2`; r14 `B1`/`B2` checked, with r14 `B2` still not fully closed.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r15/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: no snapshot gap; behavioral gap and implementation bug for array-context quoted structural leaf suffix.

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. r15 существенно сужает sanitizer и закрывает множество прошлых concrete regressions, но текущая реализация всё ещё может замаскировать POSIX sibling absolute path как `<repo>` в imported preflight evidence. До исправления B1 принимать `T-0982` небезопасно: audit package может выглядеть portable while hiding a machine-local path that is not an actual repo-root token or safe child path.
