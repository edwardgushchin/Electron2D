VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Выполнен полный review содержимого пакета по полным снимкам файлов из `repo-after/` и `repo-before/`, а не по patch-only inspection. Проверены `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, все 10 изменённых repo-owned файлов из `repo-after/`, их baseline-снимки из `repo-before/`, `T-0237.patch` как навигационная карта и доказательства в `evidence/T-0237-r01/checks/`.
- Область пакета согласована: `metadata.scopeTaskIds` и `AUDIT-MANIFEST.md` указывают одиночный scope `T-0237`; `scopeSummary` соответствует фактическому diff; `previousVerdictChain` и `blockerClosureList` пусты, поэтому проверка previous verdict files и previous blockers closure не выявила скрытых зависимостей.
- Формальные evidence gaps по наличию surfaces нет: `metadata/repo-file-snapshots.json` присутствует, `repo-after/` и `repo-before/` доступны, а SHA-256 в `repo-file-hashes.json` и `metadata/repo-file-snapshots.json` согласуются с фактическим содержимым файлов архива.
- Однако реализация и focused tests не закрывают несколько критичных веток контракта T-0237: verifier допускает некорректный snapshot index, не закрывает обязательный rename-case и не закрепляет reuse state machine после control `NEEDS_FIXES`. Поэтому изменение нельзя принимать.

BLOCKERS:
- B1
  - File/symbol: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `VerifyRepositoryFileSnapshotManifest` и `VerifySnapshotArchiveEntry` (`2787-2865`, `2891-2920`); `VerifyNoOrphanSnapshotArchiveEntries` (`2868-2888`).
  - Criterion: Контракт требует, чтобы внешнему аудитору были доступны полные снимки именно под `repo-after/` и `repo-before/`, и чтобы `metadata/repo-file-snapshots.json` указывал на эту поверхность чтения. Это прямо зафиксировано в `repo-after/docs/release-management/AUDIT-REQUEST.md:9-13,21-27` и `repo-after/docs/release-management/audit-package.md:209-219,223-229`.
  - Evidence: verifier нормализует и принимает любое `afterSnapshot`/`beforeSnapshot`, если по этому пути есть ZIP entry с нужным SHA-256 (`2891-2919`). Отдельной проверки, что `afterSnapshot` начинается с `repo-after/`, а `beforeSnapshot` — с `repo-before/`, нет. Orphan-check смотрит только на реальные ZIP entries, уже начинающиеся с `repo-after/` или `repo-before/` (`2875-2881`), поэтому manifest может ссылаться на другой архивный путь и всё равно пройти verify. Focused negative tests проверяют только duplicate/path/scope в поле `path` (`repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:2345-2388`), но не tampering `afterSnapshot`/`beforeSnapshot` location.
  - Impact: пакет может формально пройти `audit package verify`, даже если полная поверхность чтения кода уходит из `repo-after/`/`repo-before/` в произвольные ZIP entries. Это ломает основной смысл T-0237: внешний аудитор больше не получает гарантированную full-file surface для implementation/test/documentation review.
  - Fix: в verifier жёстко требовать соответствие `afterSnapshot == "repo-after/<path>"` и `beforeSnapshot == "repo-before/<path>"` для каждой записи, а также отклонять любые альтернативные snapshot paths даже при совпадающем SHA-256.
  - Verification: добавить focused tests, которые подменяют `afterSnapshot` и `beforeSnapshot` на путь вне `repo-after/`/`repo-before/`, и требовать детерминированный отказ `audit package verify` с snapshot/path diagnostic до browser submit.

- B2
  - File/symbol: `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `VerifyRepositoryFileSnapshotManifest` (`2805-2865`) и `VerifyPatchControlPaths` (`2988-3005`); task contract в `repo-after/TASKS.md:1755-1761`; documentation contract в `repo-after/docs/release-management/audit-package.md:219-229`.
  - Criterion: T-0237 требует negative coverage и фактическое закрытие кейсов wrong before/after content, missing old-side deleted entry/snapshot for rename и корректную status matrix для snapshot index. По документу rename должен быть представлен как `deleted` old path + `added` new path, а snapshot index не должен лгать аудитору о статусах файла.
  - Evidence: verifier строит `expectedPaths` только из `repo-file-hashes.json` (`2805-2809`) и затем проверяет лишь существование snapshot entries и SHA-256 (`2839-2860`, `2891-2920`). Он не валидирует `status` вообще: в коде проверяются только `fullContentIncluded` и то, что `contentKind` входит в `"text" | "binary"` (`2819-2826`), но нет проверки, что `status` согласован с before/after presence и фактическим состоянием файла. Также verifier не извлекает rename/delete semantics из patch: `VerifyPatchControlPaths` проверяет только формат control paths (`2988-3005`), а не наличие old-side deleted entry/snapshot для rename. Focused tests покрывают happy-path rename (`2004-2117`) и общие negative cases missing/orphan/tampered/duplicate/path/scope (`2272-2388`), но не покрывают missing old-side rename snapshot и wrong status.
  - Impact: verifier может принять пакет с вводящим в заблуждение `metadata/repo-file-snapshots.json`: modified файл можно пометить как `added`/`unchanged`, а rename можно свести к одному новому пути без старого baseline-side snapshot. Внешний аудитор в таком случае теряет важный diff context и может ошибочно посчитать область изменения закрытой.
  - Fix: вычислять допустимый `status` из before/after presence и hash relation, явно валидировать его в verify; дополнительно сверять rename/delete semantics с patch name-status или patch headers так, чтобы old-side path и `repo-before/` snapshot были обязательны для rename cases declared scope.
  - Verification: добавить отдельные focused tests, которые: а) подменяют `status` на ложный при неизменных snapshot files; б) удаляют old-side rename entry/snapshot из rename package. Оба случая должны детерминированно падать на `audit package verify`.

- B3
  - File/symbol: `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `ValidateAuditSubmitState` (`346-385` в нумерации метода, фактически строки файла `346-385` попадают в участок `346-385` вокруг `371-379`); доменный контракт в `repo-after/docs/release-management/audit-package.md:67,85-87`; orchestration prompt в `repo-after/.codex/prompts/goal-task-loop.md:28,31-35`.
  - Criterion: scopeSummary и обновлённый workflow требуют mandatory primary conversation reuse after `NEEDS_FIXES`, а после control `NEEDS_FIXES` — возврат в primary loop с сохранённой ссылкой задачи, без потери истории blocker-ов.
  - Evidence: доменный документ прямо говорит, что если control audit вернул `VERDICT: NEEDS_FIXES`, следующий package должен снова пройти ordinary primary loop с сохранённой ссылкой задачи (`audit-package.md:67`). Goal prompt тоже закрепляет, что если исправления идут в ту же задачу, следующая `rNN` отправляется с `--reuse-conversation`, а после control `NEEDS_FIXES` надо вернуться к primary loop (`goal-task-loop.md:28,31-35`). Но код state gate смотрит только на previous primary verdicts: `Where(verdict => !verdict.Control && verdict.IterationNumber < zipIdentity.IterationNumber)` и требует reuse лишь когда latest previous primary verdict равен `VERDICT: NEEDS_FIXES` (`AuditSubmitCommand.cs:371-379`). Focused tests покрывают только primary `NEEDS_FIXES` (`2878-2902`) и precondition для control audit / control after primary accept (`2907-2963`), но не сценарий “primary ACCEPT -> control NEEDS_FIXES -> next primary iteration without reuse”.
  - Impact: после control `NEEDS_FIXES` следующий primary submit может молча уйти в project root без reuse gate, хотя задача ещё в том же blocker-chain. Это нарушает заявленное hardening одной задачи в одном conversation до закрытия исправлений и создаёт риск потери истории контрольного blocker-а.
  - Fix: учитывать последний сохранённый control verdict в state machine и требовать тот же reuse gate для следующей primary-итерации после control `NEEDS_FIXES`; синхронизировать focused tests с этим правилом.
  - Verification: создать saved reports для сценария `primary ACCEPT` + `control NEEDS_FIXES`, затем запустить следующую primary iteration без `--reuse-conversation` и без concrete conversation URL. Команда должна завершаться отказом `E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-REQUIRED` до browser launch.

EVIDENCE_REVIEW:
- Проверены входные метаданные пакета:
  - `AUDIT-MANIFEST.md`
  - `metadata/audit-package.input.json`
  - `repo-file-hashes.json`
  - `metadata/repo-file-snapshots.json`
- Выполнен full-file review всех изменённых repo-owned файлов из `repo-after/` и соответствующих baseline snapshots из `repo-before/`:
  - `.codex/prompts/goal-task-loop.md`
  - `AGENTS.md`
  - `TASKS.md`
  - `data/documentation/electron2d-local-docs-index.json`
  - `docs/release-management/AUDIT-REQUEST.md`
  - `docs/release-management/audit-package.md`
  - `eng/Electron2D.Build/AuditPackageCommand.cs`
  - `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - `eng/Electron2D.Build/AuditSubmitCommand.cs`
  - `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
- `T-0237.patch` использован только как карта изменённых мест; verdict сформирован по полным файлам, а не по patch-only inspection.
- Проверены raw evidence по configured checks в `evidence/T-0237-r01/checks/`:
  - `build-tool-build`
  - `integration-project-build`
  - `focused-t0237-tests`
  - `update-docs-check`
  - `verify-docs`
  - `verify-licenses`
  - `git-diff-check`
  Во всех приложенных evidence `actual: 0`, то есть package builder, integration build, focused tests и документные/лицензионные проверки на стороне автора прошли, но они не закрывают найденные выше логические и coverage gaps.
- Выполнена scope scanning проверка: список файлов в manifest, metadata и snapshots согласован; лишних repo-owned файлов вне declared scope в самом присланном пакете не обнаружено.
- Выполнена secret scanning проверка по проверяемым файлам, patch и evidence: реальных секретов, приватных ключей, токенов, паролей, локальных абсолютных путей машины и конфиденциальных данных не найдено; встречаются только синтетические test fixtures и примерные ChatGPT URLs в тестах/patch, что не является утечкой.
- Previous verdict chain review:
  - `metadata.previousVerdictChain` пуст.
  - `metadata.blockerClosureList` пуст.
  - previous verdict files и verbatim preservation в этой итерации не применялись.

RISKS_AND_NOTES:
- Остаточный риск вне найденных blocker-ов: документация, task entry и prompt в целом синхронизированы по high-level intent, а snapshots/hashes внутри текущего пакета консистентны. Проблема не в текущем архиве как таковом, а в том, что реализованный verifier всё ещё допускает класс некорректных пакетов, которые T-0237 должен был запретить.
- Второй служебный архив operator workflow не оценивался как самостоятельный acceptance target, что соответствует контракту запроса: аудит касался содержимого основного ZIP и repo-owned change surface.
- Evidence gap как отсутствие `metadata/repo-file-snapshots.json` или `repo-after/`/`repo-before/` в этом пакете не выявлен; blocker-ы относятся к неполному закрытию логики проверки и покрытию negative cases.

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений, потому что изменение не доводит T-0237 до fail-closed состояния. Сейчас можно собрать и проверить пакет, где snapshot index нарушает обязательную full-file reading surface или вводит аудитора в заблуждение по rename/status semantics, а submit state machine не закрепляет reuse после control `NEEDS_FIXES`. Пока эти ветки не будут закрыты в коде, focused tests и документации, package нельзя считать соответствующим заявленному контракту и закрывать задачу нельзя.
