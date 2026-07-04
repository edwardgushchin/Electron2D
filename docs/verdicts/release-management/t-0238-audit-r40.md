VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Я прочитал пакет как полную текущую инженерную итерацию по `T-0238`, а не только как проверку упаковки. Архив читается, `metadata/repo-file-snapshots.json` присутствует, полные снимки `repo-after/` и `repo-before/` доступны, scope в metadata и manifest согласован как одиночная область `T-0238`. Я дополнительно проверил код отправки в ChatGPT, regression tests, доменную документацию, `TASKS.md`, предыдущие verdict-файлы из `metadata.previousVerdictChain`, `metadata.blockerClosureList` и сырые evidence-артефакты configured checks.
- Изменение нельзя принять из-за одной блокирующей проблемы в самой заявленной r40-правке. Локальная причина r39 была описана как пустой текущий prompt перед отправкой. Новый код меняет порядок действий и повторно подтверждает только выбранный Deep Research mode, но не доказывает, что после позднего выбора инструмента в composer действительно сохранились оба обязательных элемента payload-а текущей отправки: заполненный prompt и прикреплённый основной audit ZIP. Добавленный regression test проверяет только порядок вызовов через искусственный proxy-driver и не покрывает этот реальный браузерный риск.

Техническая привязка:
- `metadata.taskId`: `T-0238`
- `metadata.iteration`: `r40`
- `metadata.scopeTaskIds`: `["T-0238"]`
- `metadata.scopeSummary`: `metadata/audit-package.input.json`, `AUDIT-MANIFEST.md:9-10`
- Проверенные файлы:
  - `AUDIT-MANIFEST.md`
  - `metadata/audit-package.input.json`
  - `metadata/repo-file-snapshots.json`
  - `repo-file-hashes.json`
  - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`
  - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - `repo-after/docs/release-management/AUDIT-REQUEST.md`
  - `repo-after/docs/release-management/audit-package.md`
  - `repo-after/AGENTS.md`
  - `repo-after/.codex/prompts/goal-task-loop.md`
  - `repo-after/TASKS.md`
  - предыдущие verdict-файлы из `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md` … `t-0238-audit-r36.md`
- Проверенные evidence:
  - `evidence/T-0238-r40/checks/audit-submit-focused-tests-r39/stdout.txt`
  - `evidence/T-0238-r40/checks/verify-audit-followups/stdout.txt`
  - `evidence/T-0238-r40/checks/verify-docs/stdout.txt`
  - `evidence/T-0238-r40/checks/update-docs-check/stdout.txt`
  - `evidence/T-0238-r40/checks/verify-licenses/stdout.txt`
  - `evidence/T-0238-r40/checks/git-diff-check/exit-code.txt`

BLOCKERS:
- B1
  - Что не так: r40 заявлен как закрытие локального pre-send сбоя после прикрепления ZIP и заполнения prompt-а, но production-путь после позднего `EnableDeepResearchAsync(...)` подтверждает только выбранный Deep Research mode. Он не перечитывает и не подтверждает, что текущий prompt по-прежнему заполнен и что основной ZIP по-прежнему прикреплён к этой же отправке. Новая regression-проверка тоже не доказывает это поведение: она использует искусственный `DispatchProxy`, который проверяет только порядок вызовов и булевы флажки, но не моделирует реальный composer/DOM state после повторного выбора инструмента.
  - Почему это важно: Это блокирует именно текущую задачу, потому что исходная локальная проблема r39 в самом архиве описана как пустой текущий prompt перед отправкой. Текущий фикс меняет критический порядок UI-действий с высокой вероятностью побочного сброса payload-а, но доказывает лишь то, что инструмент выбран после attachment/prompt fill. Для приёмки нужно доказать не только порядок вызовов, а работоспособность полного pre-send payload path: один основной ZIP, непустой prompt и выбранный Deep Research mode непосредственно перед `ClickSendAsync`.
  - Что исправить: Нужно добавить проверяемое закрытие полного pre-send payload-а после позднего выбора Deep Research. Приемлемый вариант — production readback перед отправкой, который подтверждает, что в composer остался непустой prompt и ровно один основной audit ZIP, либо эквивалентный стабильный внутренний контракт, действительно используемый production-кодом. Одновременно нужен реалистичный regression test, который доказывает этот контракт через production DOM/browser path или через стабильный внутренний контракт runtime, а не только через order-only proxy.
  - Как проверить исправление: Нужен focused test, который после `AttachFilesAsync` + `FillPromptAsync` + позднего `EnableDeepResearchAsync` проверяет сохранение prompt-а и attachment-а до `ClickSendAsync`, и/или production guard, который падает понятной диагностикой, если prompt/attachment были потеряны перед отправкой. После этого надо заново прогнать focused suite по submit-path и полный `FullyQualifiedName~AuditSubmit`, приложив evidence.
  - Техническая привязка:
    - `File/symbol`:
      - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1228-1243` — `SubmitPromptAsync`
      - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:6087-6104` — `AuditSubmitPromptSubmissionSelectsDeepResearchAfterAttachmentAndPromptFill`
      - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:13065-13141` — `AuditSubmitPromptSubmissionDriverProxy`
      - `repo-after/TASKS.md:2072-2074`
      - `AUDIT-MANIFEST.md:10`
      - `repo-after/docs/release-management/audit-package.md:123`
    - `Criterion`:
      - `implementation content review`
      - `test coverage review`
      - `task compliance review`
      - `observable behavior`
      - `realistic tests`
      - `evidence gap`
    - `Evidence`:
      - В production-коде после `EnableDeepResearchAsync(...)` и `RequireDeepResearchSelectedAsync(...)` нет ни одной проверки сохранения prompt-а или attachment-а; код сразу читает message count и отправляет запрос: `AuditSubmitCodexChromeCommand.cs:1238-1242`.
      - Локальная причина r39 в самом архиве зафиксирована как пустой текущий prompt перед отправкой: `TASKS.md:2072`.
      - Новый test проверяет только список вызовов `AttachFilesAsync -> FillPromptAsync -> EnableDeepResearchAsync -> RequireDeepResearchSelectedAsync -> ReadConversationMessageCountAsync -> ClickSendAsync` и значение `7`, но не проверяет сохранность prompt-а или attachment-а: `RepositoryBuildToolTests.cs:6091-6104`.
      - Proxy-driver моделирует только флаги `filesAttached`, `promptFilled`, `deepResearchSelected`, `deepResearchRequired`; он не хранит и не утверждает post-state prompt-а или file attachment после `EnableDeepResearchAsync`: `RepositoryBuildToolTests.cs:13067-13141`.
      - Manifest и `TASKS.md` явно заявляют, что r40 закрывает именно reset после attachment/prompt fill и что новый regression “proves the order”: `AUDIT-MANIFEST.md:10`, `TASKS.md:2072-2074`.
      - Доменный документ требует перед отправкой иметь ровно один основной ZIP, полный текст prompt-а и подтверждённый Deep Research mode: `audit-package.md:123`.
    - `Impact`: Нельзя доказать, что текущий submit-path в реальном browser/composer состоянии отправляет правильный payload. Архив доказывает только перестановку шагов и selected-mode guard, но не закрывает симптом r39 до уровня приёмочного контракта.
    - `Fix`: добавить production readback или эквивалентный стабильный контракт на сохранение prompt-а и attachment-а после позднего выбора инструмента; дополнить focused regression реальным поведением, а не order-only proxy.
    - `Verification`:
      - focused regression на сохранение prompt-а и attachment-а после `EnableDeepResearchAsync`
      - `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AuditSubmit"`
      - обновлённые evidence stdout/exit-code для focused suite и полного `AuditSubmit`-набора

EVIDENCE_REVIEW:
- Я проверил, что основной ZIP содержит обязательные материалы для полного аудита текущей области: manifest, metadata, hashes, snapshots, `repo-after/`, `repo-before/`, patch, evidence и сохранённые verdict-файлы из `previousVerdictChain`. По `metadata/repo-file-snapshots.json` неполных critical snapshots для проверяемых файлов не обнаружено.
- По коду я прочитал production submit-path, parser/validator для audit package, follow-up verifier и связанный CLI parsing. По тестам я прочитал targeted integration tests вокруг Deep Research selection, submit order, docs/request markers и follow-up verification. По документации я сверил `AUDIT-REQUEST.md`, `audit-package.md`, `AGENTS.md`, `.codex/prompts/goal-task-loop.md` и `TASKS.md` с фактическим поведением кода. По прошлым verdict-файлам я прошёл цепочку `metadata.previousVerdictChain` до r36 и убедился, что текущий пакет действительно включает их как saved reports, а `verify audit-followups` и остальные configured checks отработали успешно.
- Секретов, приватных ключей, паролей, абсолютных Windows-путей или явных scope leak-ов в проверенной области я не нашёл. Evidence checks показывают зелёный коридор по docs, follow-ups, licenses, diff-check и AuditSubmit test slice, но именно r40-специфический pre-send payload contract остаётся недоказанным по содержанию кода и тестов.

Техническая привязка:
- `metadata/audit-package.input.json`
- `metadata/repo-file-snapshots.json`
- `repo-file-hashes.json`
- `AUDIT-MANIFEST.md:3-12, 181-250`
- `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
- `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`
- `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
- `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`
- `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
- `repo-after/docs/release-management/AUDIT-REQUEST.md`
- `repo-after/docs/release-management/audit-package.md`
- `repo-after/AGENTS.md`
- `repo-after/.codex/prompts/goal-task-loop.md`
- `repo-after/TASKS.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r02.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r04.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r16.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r18.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r19.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r20.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r21.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r24.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r25.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r27.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r29.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r31.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r32.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r33.md`
- `repo-after/docs/verdicts/release-management/t-0238-audit-r36.md`
- `evidence/T-0238-r40/checks/audit-submit-focused-tests-r39/stdout.txt` — `117/117`
- `evidence/T-0238-r40/checks/verify-audit-followups/stdout.txt` — `12 actionable findings across 91 saved audit reports`
- `evidence/T-0238-r40/checks/verify-docs/stdout.txt`
- `evidence/T-0238-r40/checks/update-docs-check/stdout.txt`
- `evidence/T-0238-r40/checks/verify-licenses/stdout.txt`
- `evidence/T-0238-r40/checks/git-diff-check/exit-code.txt`

RISKS_AND_NOTES:
- None.

CLOSURE_DECISION:
- Пакет остаётся открытым до исправлений. r40 действительно улучшает pre-send sequence и аккуратно оформлен как audit package, но текущее доказательство не закрывает саму суть заявленного локального сбоя: после позднего выбора Deep Research перед отправкой по-прежнему не доказано сохранение фактического payload-а текущего сообщения. Пока это не подтверждено production-путём или реалистичным стабильным внутренним контрактом, `T-0238 r40` нельзя принимать как внешне проверяемое исправление.
