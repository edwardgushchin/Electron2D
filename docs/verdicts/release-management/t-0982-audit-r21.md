VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен основной ZIP `T-0982` итерации `r21` как одиночная область задачи. Архив читается, `repo-after/` и `metadata/repo-file-snapshots.json` доступны, снимки изменённых файлов полные. Область заявлена как закрытие sanitizer-блокеров r01-r20: структурная нормализация preflight `metadata.json` и защищённых compact JSON snippets, fail-closed поведение для незащищённых JSON-looking фрагментов, traversal/external tail cases и единый machine-local path classifier, включая Windows drive paths.
* r21 закрывает центральные r20 B1/B2: raw replacement теперь проверяет start/end boundary по координатам исходного полного текста, поэтому незащищённый JSON snippet больше не превращает соседний raw segment в искусственную границу token-а. Это подтверждается кодом и focused evidence.
* Изменение всё ещё нельзя принять: Windows drive classifier допускает реальные absolute paths, если после `G:\u0022` или `G:\u0027` идёт разрешённая пунктуация, а затем следующий path separator. Это нарушает текущий scope: исключение для `u0022`/`u0027` должно быть только для isolated punctuation-like values, а не для реальных drive-root paths.

Техническая привязка:

* `metadata.taskId`: `T-0982`
* `metadata.iteration`: `r21`
* `metadata.scopeTaskIds`: `["T-0982"]`
* `metadata.scopeSummary`: закрытие r01-r20 sanitizer blockers через full-context raw token, protected compact JSON structural normalization и unified machine-path gate.
* `combined scope`: не используется, область одиночная.
* Проверенные основные файлы: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/dev-diary/2026/07 Июль/07-07-2026.md`, `repo-after/data/dev-diary/2026/07 Июль/08-07-2026.md`.
* Проверенные previous verdict files: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r20.md`.
* Проверенные evidence: `evidence/T-0982-r21/preflight/*`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`, `T-0982.patch`.

BLOCKERS:

* B1

  * Что не так: Windows drive detector слишком широко считает `\u0022` и `\u0027` безопасной drive-like punctuation. Он запрещает `G:\u0022\local\copied-task.md`, но пропускает такие же реальные Windows absolute paths, если после marker-а стоит пунктуация, например `G:\u0022.local\copied-task.md`, `G:\u0027-local\copied-task.md` или `G:\u0022)\local\copied-task.md`. Это всё ещё drive-root path после `G:\`, а не isolated punctuation-like value.
  * Почему это важно: текущая задача прямо требует, чтобы decoded/raw/path-like значения проходили через единый classifier, а Windows drive absolute paths fail closed. Исключение должно сохранять только безопасный текст вроде `D:\u0022`, но не путь с дальнейшим path separator. Сейчас archive content может содержать локальный Windows path в raw text или JSON string form и не получить `E2D-BUILD-AUDIT-ABSOLUTE-PATH`.
  * Что исправить: сузить `IsIsolatedEscapedQuoteLikeDriveValue` так, чтобы после marker-а `u0022`/`u0027` до конца token-а не было `/` или `\`. Безопасным должен оставаться только isolated punctuation-like текст без последующего path separator-а. Тот же classifier должен применяться к decoded JSON string values.
  * Как проверить исправление: добавить package и verify regressions через production `audit package` / `audit package verify` для raw и JSON-string вариантов `G:\u0022.local\copied-task.md`, `G:\u0027-local\copied-task.md`, `G:\u0022)\local\copied-task.md`. Ожидаемый результат — отказ с `E2D-BUILD-AUDIT-ABSOLUTE-PATH`. Положительный тест для isolated `D:\u0022` / `D:\u0027` должен остаться зелёным только когда после marker-а нет дальнейшего path separator-а.
  * Проверка опровержения: проверены реализация, тесты, документация, `metadata.blockerClosureList`, r20 blocker closure и r21 evidence. Текущие тесты `AuditPackageRejectsEscapedQuoteLikeWindowsDrivePathsInArchiveContent` и `AuditPackageVerifyRejectsEscapedQuoteLikeWindowsDrivePathsInArchiveContent` покрывают только непосредственный separator после `u0022`/`u0027`. Положительный тест `AuditPackageAllowsJsonEscapedQuotedTextAfterDriveLikePunctuation` покрывает только isolated `D:\u0022`. Сценарий «пунктуация после marker-а, затем следующий separator» тестами не закрыт, а код явно допускает его.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`; `WindowsDrivePathPattern`; `ContainsWindowsDrivePathValue`; `IsIsolatedEscapedQuoteLikeDriveValue`; `TryGetEscapedQuoteMarkerEnd`; `IsEscapedQuoteLikeDriveTailBoundary`; `ContainsDecodedJsonStringMachineLocalPath`.
    * `Line range`: `AuditPackageCommand.cs` lines `73-75`, `4958-4967`, `4970-4988`, `4990-5005`, `5007-5035`, `5043-5058`.
    * `Criterion`: `metadata.scopeSummary`; `repo-after/docs/release-management/audit-package.md` lines `453` and `457`; mandatory `secret scanning` / local absolute path criterion.
    * `Evidence`: `WindowsDrivePathPattern` matches `G:\`, then `IsIsolatedEscapedQuoteLikeDriveValue` treats `u0022`/`u0027` as an allowed marker. `IsEscapedQuoteLikeDriveTailBoundary` checks only the immediate next character after marker-end: it rejects `/`, `\`, letters, digits and `_`, but accepts punctuation such as `.`, `-` or `)`. It does not scan the rest of the token for a later path separator. Therefore `G:\u0022.local\copied-task.md` is accepted because the immediate next character is `.`, even though the same token later contains `\copied-task.md`.
    * `Impact`: real Windows drive-root paths can pass package and verify validation when the first segment begins with `u0022`/`u0027` plus punctuation.
    * `Fix`: treat the escaped-quote exception as safe only when it is isolated through the end of the current token/line; if a later `/` or `\` appears before token termination, classify the value as a Windows drive absolute path.
    * `Verification`: targeted package/verify tests must fail before the fix and pass after; existing isolated punctuation positive test must remain green.

EVIDENCE_REVIEW:

* Полнота архива проверена. `metadata/repo-file-snapshots.json` содержит 27 файлов с `fullContentIncluded: true`; `repo-after/` и `repo-before/` snapshots присутствуют и совпадают с SHA-256, указанными в snapshot index. Недостатка снимков, который мешал бы читать реализацию, тесты или документацию, не найдено.
* Реализация проверена по полному `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, не только по patch. Проверен путь `SelectPreflightCheckEvidenceFiles` → `PreparePreflightEvidenceSource` → `WriteNormalizedPreflightMetadata` / `SanitizePreflightEvidenceText` → `SanitizePreflightEvidenceTextSegments` → `NormalizePreflightMetadataNode` / `ReplaceRepoRootPathCandidates` → `ValidateArchiveContent` / `ValidateMachineLocalPathText`.
* r20 B1/B2 по unprotected JSON snippet boundaries проверены как закрытые в текущем коде: `ReplaceRepoRootPathCandidates(text, start, end, ...)` ограничивает поиск segment range-ом, но `IsRepoRootPathCandidateBoundary` и `IsRepoRootPathCandidateStartBoundary` смотрят на исходный полный `text`, поэтому `{`, `[`, `}` или `]` из незащищённого snippet-а больше не становятся искусственным началом или концом token-а.
* Тесты проверены по полному `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Набор r21 покрывает exact repo-root tokens, compact JSON object/array positives, escaped solidus and unicode escaped solidus positives/negatives, embedded/unprotected JSON snippet boundaries, JSON string content, external same-line tails, POSIX sibling paths, traversal, case-sensitive root matching, previous-verdict reviewer scanner и Windows drive cases. Покрытия для B1 punctuation-plus-later-separator Windows drive paths нет.
* Документация проверена по `repo-after/docs/release-management/audit-package.md` и generated docs index. Документ соответствует новому full-context JSON/raw sanitizer contract, но фактическая реализация Windows escaped-quote exception шире задокументированного isolated поведения.
* Previous verdict chain r01-r20 прочитана. `metadata.blockerClosureList` содержит closure entries для всех прошлых blocker-ов; r21 evidence называет текущие checks для закрытия. Закрытие прежних blocker-ов в целом проверяемо, кроме обнаруженной новой blind spot в том же machine-local classifier class.
* Preflight evidence проверены. Все заявленные проверки завершились exit code `0`: build tool build, focused sanitizer tests `28/28`, machine-local classifier tests `9/9`, previous-verdict reviewer scanner tests `5/5`, `audit-loop-stabilization`, sanitizer fixture, dead-tail-helper search, `verify audit-contracts`, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`. Эти evidence подтверждают выполненный набор, но не покрывают B1.
* Проверка области не выявила отдельного scope blocker: изменения ограничены audit-package tool, integration tests, domain documentation, generated docs index, task notes, dev diary and saved previous verdict reports. Public API, renderer, runtime/game loop and Godot API surface не затрагиваются.
* Проверка секретов и локальных данных в текущем пакете не выявила реальных токенов, приватных ключей или паролей. Синтетические Windows/POSIX paths находятся в тестах, документации, patch и сохранённых previous reports как примеры.

Техническая привязка:

* `implementation content review`: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
* `test coverage review`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
* `documentation review`: `repo-after/docs/release-management/audit-package.md`, `repo-after/data/documentation/electron2d-local-docs-index.json`
* `task compliance review`: `repo-after/TASKS.md`, `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`
* `previous verdict files`: `repo-after/docs/verdicts/release-management/t-0982-audit-r01.md` ... `repo-after/docs/verdicts/release-management/t-0982-audit-r20.md`
* `previous blockers closure`: r01 `B1`; r02 `B1`; r03 `B1`; r04 `B1`; r05 `B1`; r06 `B1`/`B2`; r07 `B1`; r08 `B1`/`B2`; r09 `B1`/`B2`; r10 `B1`/`B2`; r11 `B1`/`B2`; r12 `B1`/`B2`; r13 `B1`/`B2`; r14 `B1`/`B2`; r15 `B1`; r16 `B1`; r17 `B1`; r18 `B1`; r19 `B1`/`B2`; r20 `B1`/`B2`/`B3`.
* `secret scanning`: `repo-after/`, `repo-before/`, `T-0982.patch`, `evidence/T-0982-r21/preflight/*`
* `scope scanning`: `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `AUDIT-MANIFEST.md`
* `evidence gap`: no snapshot gap; behavioral test gap for Windows escaped-quote marker followed by punctuation and a later path separator.

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. r21 исправляет основной full-context raw token bug вокруг unprotected JSON snippets и закрывает большую часть r01-r20 цепочки, но machine-local classifier всё ещё имеет доказуемый fail-open case для Windows drive absolute paths. До исправления B1 audit package нельзя считать безопасным переносимым артефактом для внешнего аудита.
