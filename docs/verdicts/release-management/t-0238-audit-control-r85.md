VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен clean control audit-пакет для `T-0238` итерации `r85`. Область пакета согласована как одиночная задача `T-0238`: чистый контрольный аудит без прошлых verdict-отчётов, без `TASKS.md`, без дневника, с пустыми `metadata.previousVerdictChain` и `metadata.blockerClosureList`.
* По полным файлам из `repo-after/` проверены изменения в release-management tooling, документации, агентских правилах, проверке последующих замечаний, clean-control audit-потоке, отправке аудита через Chrome, генерации локального индекса документации и интеграционных тестах. Снимки файлов полные, hash-модель для изменённых файлов сходится, evidence-команды завершились успешно.
* Изменение нельзя принять, потому что реализация `audit submit` не доказывает ключевой контракт задачи: перед отправкой проверяется наличие ожидаемого ZIP-чипа, но не проверяется отсутствие второго или лишнего вложения в composer. Это нарушает заявленное требование «ровно один основной ZIP, без sidecar/лишних файлов» и создаёт риск отправки загрязнённого аудиторского контекста.

Техническая привязка:

* `metadata.taskId`: `T-0238`
* `metadata.iteration`: `r85`
* `metadata.scopeTaskIds`: `["T-0238"]`
* `metadata.scopeSummary`: clean control audit for accepted `T-0238 r85`; same accepted implementation surface; no previous verdict reports; no `TASKS.md` process history; empty `previousVerdictChain`; empty `blockerClosureList`; no saved Markdown reports in archive.
* Проверенные основные артефакты: `AUDIT-MANIFEST.md`, `AUDIT-REQUEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`, `repo-before/`, `repo-after/`, `evidence/T-0238-r85/checks/`.
* Проверенные изменённые файлы: `.codex/prompts/goal-task-loop.md`, `AGENTS.md`, `docs/release-management/AUDIT-REQUEST.md`, `docs/release-management/audit-package.md`, `eng/Electron2D.Build/AuditFollowupVerifier.cs`, `eng/Electron2D.Build/AuditPackageCommand.cs`, `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `eng/Electron2D.Build/AuditSubmitCommand.cs`, `eng/Electron2D.Build/LocalDocumentationVerifier.cs`, `eng/Electron2D.Build/Program.cs`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `data/documentation/electron2d-local-docs-index.json`, `data/documentation/local-docs-index/documentation.ndjson`.
* Критерии: `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `full file review`, `architecture coherence`.

BLOCKERS:

* B1

  * Что не так: Перед отправкой аудиторского запроса проверка payload подтверждает, что найден ожидаемый ZIP-файл, но не подтверждает, что это единственное вложение в поле отправки. Реализация фильтрует attachment-root только по ожидаемому имени файла и затем требует ровно один root среди совпавших элементов. Лишний файл с другим именем рядом с ожидаемым ZIP не попадает в этот подсчёт, поэтому состояние может быть признано готовым, а `ClickSendAsync` отправит загрязнённый запрос.
  * Почему это важно: Текущая задача меняет именно контракт audit package/submission/control audit. Документация и правила требуют, чтобы обычная отправка прикрепляла ровно один основной ZIP и не прикрепляла sidecar или другие файлы. Если инструмент не отбраковывает лишнее вложение, он не обеспечивает clean-command path: операторский ZIP, старый архив или приватный файл могут уйти вместе с основным audit ZIP. Это ломает доказуемость внешнего аудита и может раскрыть лишний локальный контекст.
  * Что исправить: В проверке перед отправкой нужно считать все видимые attachment/file-chip элементы, относящиеся к текущему composer, а не только элементы с ожидаемым именем. Готовность должна быть истинной только если фактически найден ровно один attachment-root/чип и его имя совпадает с ожидаемым основным ZIP. Любое второе вложение, включая sidecar или старый ZIP, должно давать явную ошибку до клика отправки.
  * Как проверить исправление: Добавить регрессионный интеграционный тест, где composer содержит ожидаемый `T-0238-audit-rNN.zip` и дополнительный файл, например `T-0238-audit-rNN.operator-workflow.zip` или произвольный `unexpected.txt`. Тест должен подтверждать, что payload-ready guard возвращает ошибку и отправка не вызывается. После исправления должны пройти focused audit-submit/package tests, docs verification, license verification и diff check.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `SubmitPromptAsync`, `RequirePromptPayloadReadyAsync`, `PromptPayloadStatusExpression`.
    * `Criterion`: `task compliance review`; `implementation content review`; `test coverage review`; контракт `audit submit` — ровно один основной ZIP без sidecar/лишних вложений.
    * `Evidence`: В `SubmitPromptAsync` отправка идёт после `AttachFilesAsync([zipPath])`, заполнения prompt и `RequirePromptPayloadReadyAsync(message, zipPaths)`, затем вызывается `ClickSendAsync`. В `PromptPayloadStatusExpression` проверяется, что ожидаемых имён файлов в конфигурации ровно одно, затем DOM-элементы фильтруются по ожидаемому имени; готовность вычисляется как `roots.length === 1` только среди совпавших по имени root-элементов. Общий список фактических вложений composer не подсчитывается. В тестах `RepositoryBuildToolTests.cs` есть проверки отсутствующего вложения, текста без attachment и вложения в истории, но нет сценария «ожидаемый ZIP плюс лишнее вложение».
    * `Impact`: Инструмент может отправить больше одного файла, хотя задача и документация требуют один основной ZIP. Это блокирует принятие release-management hardening-задачи.
    * `Fix`: Переписать payload-ready guard так, чтобы он перечислял все фактические вложения текущего composer и требовал ровно один фактический чип с ожидаемым именем.
    * `Verification`: Новый regression test на лишнее вложение; повторный запуск `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --no-build --no-restore --filter FullyQualifiedName~AuditSubmit|FullyQualifiedName~AuditPackageMessage|FullyQualifiedName~AuditPackageDocumentation|FullyQualifiedName~UpdateDocsExcludesSavedAuditVerdictsFromGeneratedDocumentationIndex --blame-hang-timeout 10m -v:minimal`; повторные `update docs --check`, `verify docs`, `verify licenses`, `git diff --check`.

EVIDENCE_REVIEW:

* Архив читается и содержит полные материалы для содержательной проверки: `metadata/repo-file-snapshots.json` указывает полные after/before snapshots для изменённых файлов, а `repo-after/` даёт итоговые версии кода, тестов и документации. Проверка не была сведена к patch-only inspection.
* `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json` и snapshot index согласованы по области `T-0238 r85`. Явной combined scope нет: пакет заявляет одну задачу. Правок вне заявленной области не выявлено.
* `metadata.previousVerdictChain` пуст и `metadata.blockerClosureList` пуст. Для clean control audit это согласуется с `scopeSummary`; прошлых verdict-файлов в архиве нет, поэтому проверки verbatim preservation и previous blockers closure здесь неприменимы как содержательная обязанность текущего control-пакета.
* Документация в `docs/release-management/audit-package.md`, `docs/release-management/AUDIT-REQUEST.md`, `AGENTS.md` и `.codex/prompts/goal-task-loop.md` описывает жёсткий процесс: после primary ACCEPT область не меняется; control audit использует чистый ZIP без старых verdict reports и без `TASKS.md`/дневника; `audit submit` должен отправлять один основной ZIP и не sidecar; follow-up findings из сохранённых отчётов должны закрываться проверяемым способом.
* Реализация в `AuditSubmitCommand.cs` содержит полезные clean-control gate-проверки: режим `--control-audit` несовместим с conversation URL, требует сохранённый primary ACCEPT той же итерации, проверяет пустые previous lists, отсутствие `TASKS.md`, dev diary и saved verdict paths в clean archive. Эти части соответствуют документации.
* Реализация в `AuditFollowupVerifier.cs` добавляет отдельную проверку последующих замечаний из сохранённых отчётов: `FOLLOW_UP_FINDING` считается actionable по умолчанию, `OUT_OF_SCOPE_NOTE` и `INFO_NOTE` становятся actionable только при явном маркере, closure-note требует проверяемый статус и обоснование. Это соответствует задаче на machine-checkable follow-up closure surface.
* Реализация в `LocalDocumentationVerifier.cs` и сгенерированные индексы исключают сохранённые verdict-файлы из локального документационного индекса. В after-индексах `docs/verdicts` не найден.
* Evidence-команды в архиве завершились с кодом `0`: focused integration tests — `189 passed`; `update docs --check`; `verify docs`; `verify licenses`; `git diff --check`. Эти проверки подтверждают заявленные части реализации, но не покрывают сценарий лишнего вложения в composer, из-за чего B1 остаётся доказуемым дефектом.
* Проверка секретов и локальных данных по коду, patch и evidence не выявила реальных токенов, приватных ключей, паролей или конфиденциальных абсолютных локальных путей. Найденные Windows-path строки относятся к синтетическим тестовым фикстурам secret-scan/local-path проверок, а не к реальному локальному окружению.

Техническая привязка:

* Snapshot/hash materials: `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `SHA256SUMS.txt`.
* Metadata: `metadata/audit-package.input.json`.
* Manifest/request: `AUDIT-MANIFEST.md`, `AUDIT-REQUEST.md`, `repo-after/docs/release-management/AUDIT-REQUEST.md`.
* Documentation: `repo-after/docs/release-management/audit-package.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`.
* Implementation: `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/LocalDocumentationVerifier.cs`, `repo-after/eng/Electron2D.Build/Program.cs`.
* Tests: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`.
* Generated docs index: `repo-after/data/documentation/electron2d-local-docs-index.json`, `repo-after/data/documentation/local-docs-index/documentation.ndjson`.
* Evidence:

  * `evidence/T-0238-r85/checks/audit-submit-and-package-focused-tests-r85-control/`
  * `evidence/T-0238-r85/checks/update-docs-check/`
  * `evidence/T-0238-r85/checks/verify-docs/`
  * `evidence/T-0238-r85/checks/verify-licenses/`
  * `evidence/T-0238-r85/checks/git-diff-check/`
* Проверки: `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `evidence gap`.

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. До принятия нужно исправить B1: `audit submit` должен технически гарантировать, что перед отправкой в composer находится ровно одно вложение — ожидаемый основной audit ZIP — и должен отклонять любой второй файл. Исправление должно быть подтверждено регрессионным тестом на лишнее вложение и новыми evidence-проверками.
