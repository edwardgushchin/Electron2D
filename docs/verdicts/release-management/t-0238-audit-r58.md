VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

Проверен основной архив T-0238 итерации r58 как новая primary-итерация после сохранённого r45 ACCEPT и локально остановленного r57. Область пакета заявлена как одиночная задача T-0238: базовый audit submit должен работать обычным запросом ChatGPT без @Глубокое исследование, --deep-research остаётся явным резервным режимом, а r58 дополнительно закрывает локальный отказ r57 — раннюю прокрутку страницы проекта до отправки prompt-а.

По полным файлам repo-after/ видно, что прямой вызов ScrollConversationToBottomAsync действительно удалён из SubmitAndWaitForReportAsync, а обычный путь теперь вызывает WaitForOrdinaryChatReportAsync. Однако изменение нельзя принять: документация содержит противоречие по обязательной прокрутке, новый regression-тест не доказывает поведение submit-пути, проверка закрытия follow-up замечаний пропускает сохранённые записи из r45 ACCEPT, а исключение для previous verdict-файлов отключает secret scan шире допустимого.

Техническая привязка:

metadata.taskId: T-0238

metadata.iteration: r58

metadata.scopeTaskIds: [T-0238]

metadata.scopeSummary: ordinary ChatGPT submit by default; --deep-research as explicit reserve mode; r58 removes early ScrollConversationToBottomAsync from SubmitAndWaitForReportAsync.

Проверенные основные файлы: repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs, repo-after/eng/Electron2D.Build/AuditPackageCommand.cs, repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs, repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs, repo-after/docs/release-management/audit-package.md, repo-after/.codex/prompts/goal-task-loop.md, repo-after/TASKS.md, сохранённые verdict-файлы под repo-after/docs/verdicts/release-management/.

Проверенные доказательства: AUDIT-MANIFEST.md, metadata/audit-package.input.json, repo-file-hashes.json, metadata/repo-file-snapshots.json, T-0238.patch, evidence/T-0238-r58/checks/*.

BLOCKERS:

B1

Что не так: Доменный документ всё ещё требует от audit submit прокручивать обсуждение вниз перед прикреплением ZIP и вставкой сообщения. Это прямо противоречит заявленной цели r58 — убрать раннюю прокрутку страницы проекта до отправки prompt-а. В том же пакете TASKS.md утверждает, что доменный документ уже говорит о штатном ожидании поля без предварительной прокрутки, но полный файл документа содержит обратное требование.

Почему это важно: audit submit является процессным инструментом приёмки. Если контракт документа и реализация расходятся, оператор и будущие исправления могут вернуть именно то поведение, которое r58 должна была устранить. Для задачи про автоматизацию внешнего аудита документация является частью проверяемого поведения.

Что исправить: Привести все места docs/release-management/audit-package.md к одному контракту: обычный submit должен ждать поле сообщения без предварительной прокрутки страницы проекта, прикреплять ZIP, вставлять prompt, проверять payload и отправлять. Оставить прокрутку только там, где она действительно относится к read-only download/report recovery path, если такой путь нужен и явно отделён от pre-prompt submit.

Как проверить исправление: Добавить/обновить документационный regression, который проверяет отсутствие требования pre-prompt scroll в основном описании audit submit, и выполнить update docs --check и verify docs.

Техническая привязка:

File/symbol: repo-after/docs/release-management/audit-package.md:129; repo-after/TASKS.md:2125

Criterion: documentation review, task compliance review, architecture coherence

Evidence: строка audit-package.md:129 говорит: «дождаться доступности поля сообщения, прокрутить обсуждение вниз, прикрепить...». При этом TASKS.md:2125 заявляет, что доменный документ «прямо говорит» об отсутствии предварительной прокрутки.

Impact: пакет не фиксирует единый операторский контракт r58; ACCEPT создаст противоречивую спецификацию для приёмочного инструмента.

Fix: удалить или уточнить pre-prompt scroll из основного submit-контракта; явно отделить report/download-only scrolling от отправки нового prompt-а.

Verification: focused documentation test + dotnet run --project eng/Electron2D.Build --no-build -- update docs --check; dotnet run --project eng/Electron2D.Build --no-build -- verify docs.

B2

Что не так: Новый тест AuditSubmitDoesNotScrollProjectPageBeforePromptSubmission проверяет C#-файл как текст и ищет отсутствие строки ScrollConversationToBottomAsync в теле SubmitAndWaitForReportAsync. Он не исполняет submit-путь, не использует стабильный внутренний драйвер и не доказывает, что страница проекта не прокручивается перед заполнением prompt-а. При этом сам текущий пакет добавляет правило, что тест по тексту C# не доказывает поведение audit submit.

Почему это важно: Локальный отказ r57 был поведенческим: пользователь увидел прокрутку страницы перед отправкой. Текстовый тест поймает только прямой вызов конкретного метода в конкретном методе-обёртке. Он не поймает прокрутку, перенесённую в WaitForComposerAsync, screenshot/hydration helper, новый helper с другим именем или DOM-выражение внутри production-пути. Такое покрытие не закрывает критерий реалистичных тестов для текущей задачи.

Что исправить: Заменить или дополнить source-level проверку поведенческим regression-тестом через стабильный внутренний driver/test harness для orchestration-пути SubmitAndWaitForReportAsync до SubmitPromptAsync. Тест должен фиксировать порядок событий: открыть проект, дождаться composer, не выполнять scroll/evaluate-scroll до attach/fill/send, затем отправить prompt.

Как проверить исправление: Новый тест должен падать на версии с ранним scroll и проходить на исправленной реализации. Его нужно включить в focused submit suite и evidence текущей итерации.

Техническая привязка:

File/symbol: repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:4215-4227; repo-after/.codex/prompts/goal-task-loop.md:19; repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:43-85

Criterion: test coverage review, realistic tests, source-level proof, backend path, observable behavior

Evidence: тест читает AuditSubmitCodexChromeCommand.cs через File.ReadAllText, извлекает тело метода и делает Assert.DoesNotContain("ScrollConversationToBottomAsync", methodBody). В goal-task-loop.md:19 прямо записано, что тест по тексту C# не доказывает поведение audit submit.

Impact: закрытие r57 scroll failure не доказано реалистичным тестом; future regression может пройти текущий тест и снова прокрутить страницу перед prompt submission.

Fix: добавить driver-based orchestration test или иной стабильный production-level контракт, который наблюдает отсутствие scroll-call/evaluate-scroll до prompt submission.

Verification: focused test name for new behavior-level regression + dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --no-build --no-restore --filter <new-test-or-focused-submit-suite>.

B3

Что не так: verify audit-followups пропускает сохранённые actionable follow-up записи из t-0238-audit-r45.md, потому что parser не распознаёт Markdown-жирное оформление вида - **FOLLOW_UP_FINDING F1**. В результате evidence сообщает успешное закрытие 14 actionable findings, хотя сохранённый r45 ACCEPT содержит как минимум два follow-up finding-а без closure note в TASKS.md.

Почему это важно: T-0238 как раз вводит обязательное проверяемое закрытие RISKS_AND_NOTES. Если verifier не видит валидные сохранённые записи из предыдущего ACCEPT, задача может быть закрыта при незакрытых follow-up findings. Это ломает основной смысл текущей задачи и previous blockers closure.

Что исправить: Исправить parser так, чтобы он распознавал структурные маркеры с допустимой Markdown-разметкой вокруг маркера, например - **FOLLOW_UP_FINDING F1**, - *FOLLOW_UP_FINDING F1*, и обычную форму без выделения. Добавить regression на r45-подобный report. Затем закрыть или явно triage-ить r45 FOLLOW_UP_FINDING F1 и FOLLOW_UP_FINDING F2 в tracked closure surface.

Как проверить исправление: verify audit-followups должен сначала падать на незакрытых r45 F1/F2 после исправления parser-а, затем проходить только после добавления проверяемых closure notes. Evidence должно показывать увеличенный или корректно объяснённый набор actionable findings.

Техническая привязка:

File/symbol: repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs:253-290; repo-after/docs/verdicts/release-management/t-0238-audit-r45.md:16-31; repo-after/TASKS.md; evidence/T-0238-r58/checks/verify-audit-followups/stdout.txt

Criterion: previous blockers closure, previous verdict files, RISKS_AND_NOTES, FOLLOW_UP_FINDING, test coverage review

Evidence: regex StructuredRiskEntryPattern ожидает FOLLOW_UP_FINDING сразу после optional bullet, но строка r45 имеет - **FOLLOW_UP_FINDING F1** и - **FOLLOW_UP_FINDING F2**. Evidence при этом сообщает: Audit follow-up closure verification passed for 14 actionable findings across 95 saved audit reports. В TASKS.md нет closure entry с source: docs/verdicts/release-management/t-0238-audit-r45.md для F1/F2.

Impact: проверка закрытия follow-up замечаний даёт ложный PASS и не обеспечивает обязательный closure step перед закрытием задачи.

Fix: расширить parser Markdown-обрамления; добавить тест; добавить closure notes или явное решение по r45 F1/F2.

Verification: focused parser/verifier tests + dotnet run --project eng/Electron2D.Build --no-build -- verify audit-followups.

B4

Что не так: Исключение для previous verdict-файлов отключает не только проверку синтетических локальных Windows-путей, но и secret scanning. В SelectRepositoryFiles existing previous verdict-файлы не проходят ValidateSecretPolicy; в финальной проверке ZIP ValidateArchiveContent не вызывает ValidateSecretText для repo-after//repo-before/ entries из previousVerdictChain; а ValidatePatchText вырезает diff-блоки previous verdict-файлов перед secret scan. Тесты дополнительно закрепляют, что secret-like evidence в previous verdict проходит packaging и verify.

Почему это важно: Исправление r57 должно было разрешить безопасный placeholder Windows path из сохранённого внешнего отчёта. Вместо этого оно создаёт слепую зону: если previous verdict случайно содержит реальный токен, пароль или приватный ключ, штатная упаковка и verify могут сохранить и восстановить его без отказа. Это нарушение обязательной проверки секретов и локальных данных. Для audit package это проблема безопасности процесса, а не косметический долг.

Что исправить: Разделить исключения: previous verdict-файлы могут иметь узкое исключение только для явно синтетических локальных путей, если это нужно для сохранения внешнего отчёта, но secret scanning должен применяться к их содержимому, ZIP-снимкам и patch-блокам. Если нужно разрешить цитирование false-positive evidence, оно должно быть явно redacted/placeholder, а не обходить scanner целиком.

Как проверить исправление: Добавить regression, где previous verdict содержит token=<non-placeholder> или private key marker, и убедиться, что audit package или audit package verify завершается с E2D-BUILD-AUDIT-SECRET-DETECTED. Отдельный тест с C:/Users/example/source.md в previous verdict должен продолжать проходить, если это принятое исключение.

Техническая привязка:

File/symbol: repo-after/eng/Electron2D.Build/AuditPackageCommand.cs:729-735, 3543-3572, 3575-3585; repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:7877-7908, 7910-7951, 7955-7989

Criterion: secret scanning, global safety blocker, task compliance review

Evidence: ValidateSecretPolicy пропускается для existingPreviousVerdicts; ValidateSecretText пропускается для isPreviousVerdictEntry; patch scan получает OmitPreviousVerdictPatchBlocks(...). Тест AuditPackageIncludesPreviousVerdictWithQuotedSecretEvidenceVerbatim ожидает successful package/verify и сохранение quotedSecretEvidence в restored previous verdict.

Impact: пакетный инструмент больше не гарантирует отсутствие secret-like значений в части архива, которая явно включается в previousVerdictChain и восстанавливается как repo-owned evidence.

Fix: сохранить исключение только для machine-local path placeholder, но вернуть secret scan для previous verdict content/snapshots/patch или требовать redaction.

Verification: focused tests for previous-verdict secret rejection + existing placeholder-path allow test + verify licenses, git diff --check.

EVIDENCE_REVIEW:

Прочитаны метаданные пакета, manifest, patch-карта, полные итоговые версии изменённых файлов из repo-after/, исходные версии из repo-before/ для изменённых файлов, сохранённые previous verdict-файлы и evidence проверок. metadata/repo-file-snapshots.json содержит 33 записи, все проверяемые snapshots присутствуют, fullContentIncluded выставлен, SHA-256 снимков совпадают с файлами архива. Это не patch-only inspection.

Проверки из evidence завершились с кодом 0: focused test suite AuditSubmit|AuditPackageMessage|AuditPackageCopiesStaticRequestVerbatim|AuditRequestRequiresReadableRussianReportExplanations|AuditPackageDocumentationDefines|AuditPackageAllowsPlaceholderWindowsUserPathsInPreviousVerdicts прошёл 136/136; update docs --check, verify docs, verify audit-followups, verify licenses, git diff --check также прошли. Эти PASS-результаты просмотрены, но они не закрывают найденные выше содержательные проблемы.

Реальных секретов в текущем содержимом архива по проверенным файлам не обнаружено. Блокер B4 относится к реализации защитного правила: текущий код создаёт путь, при котором previous verdict-содержимое выводится из secret scan.

Изменения не затрагивают игровой runtime hot path, отрисовку, ввод, ресурсы или Public API Electron2D/Godot-подмножество. Блокеров по производительности игрового цикла и Public API в текущей области не найдено.

Техническая привязка:

Metadata and manifest: AUDIT-MANIFEST.md, metadata/audit-package.input.json, repo-file-hashes.json, metadata/repo-file-snapshots.json

Code review: repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs, repo-after/eng/Electron2D.Build/AuditPackageCommand.cs, repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs, repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs, repo-after/eng/Electron2D.Build/Program.cs

Tests: repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs

Documentation and rules: repo-after/docs/release-management/audit-package.md, repo-after/docs/release-management/AUDIT-REQUEST.md, repo-after/.codex/prompts/goal-task-loop.md, repo-after/AGENTS.md, repo-after/TASKS.md

Previous verdict files: repo-after/docs/verdicts/release-management/t-0238-audit-r01.md, r02, r04, r16, r18, r19, r20, r21, r24, r25, r27, r29, r31, r32, r33, r36, r40, r41, r42, r45

Evidence: evidence/T-0238-r58/checks/audit-submit-and-package-focused-tests-r58/*, update-docs-check/*, verify-docs/*, verify-audit-followups/*, verify-licenses/*, git-diff-check/*

RISKS_AND_NOTES:

None. Все доказуемые проблемы в пределах текущей области задачи являются блокирующими и перечислены в BLOCKERS.

CLOSURE_DECISION:

Задача T-0238 в итерации r58 остаётся открытой. Принять пакет нельзя до синхронизации документации submit-контракта, замены source-level scroll regression на поведенческое доказательство, исправления parser-а verify audit-followups с закрытием r45 follow-up findings и возврата secret scanning для previous verdict-содержимого. После исправлений нужен новый primary package с обновлёнными полными файлами, tests/evidence и корректным metadata.blockerClosureList.
