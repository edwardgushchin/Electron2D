VERDICT: ACCEPT

TASK_ASSESSMENT:

* Проведена полная повторная инженерная проверка T-1137 r16. Новая активация attachment-контекста для primary/control chats реализована в штатном browser path и не ослабляет существующие fail-closed проверки.
* Новый чат перед установкой ZIP активирует только точную видимую и доступную кнопку `button[data-testid="composer-plus-btn"]` координатным CDP-кликом.
* Инструмент не ищет кнопку по неоднозначным словам, не выбирает пункт меню и не открывает native file chooser.
* Для `--reuse-conversation` сохраняется прямой путь; если input всё же отсутствует, остаётся ограниченная повторная попытка через ту же точную кнопку.
* Закрытия r12–r15, принятый Public API результат и ordinary-chat routing не регрессировали.
* Доказуемых блокирующих проблем, лишних правок, реальных секретов или ухудшения игрового горячего пути не найдено.

Техническая привязка:

* `metadata.taskId`: `T-1137`
* `metadata.iteration`: `r16`
* `metadata.scopeTaskIds`: `["T-1137"]`
* `combined scope`: нет
* Baseline: `df40ddeba69fd013f7ce879f80f298becaddd96e`
* `metadata.previousVerdictChain`: 17 отчётов, включая принятый `docs/verdicts/release-management/t-1137-audit-r15.md`
* `metadata.blockerClosureList`: содержит проверяемые закрытия прошлых блокирующих проблем и запись о наблюдавшемся r15 attachment activation failure
* `metadata/repo-file-snapshots.json`: 101 полный снимок, неполных записей нет

BLOCKERS:

* No blockers found.

EVIDENCE_REVIEW:

* Полностью прочитан сохранённый verdict r15. Его решение, доказательства и ограничения сохранены.
* Проверен выбор режима активации:

  * обычный новый primary submit получает `activateAttachmentMenu = true`;
  * `--new-conversation` получает `true`;
  * новый `--control-audit` получает `true`;
  * `--reuse-conversation` получает `false`;
  * parser запрещает несовместимые сочетания `--reuse-conversation` с `--control-audit` и `--new-conversation`.
* `OpenAttachmentMenuAsync`:

  * получает координаты через production `AttachmentMenuButtonPointExpression`;
  * возвращает отказ, если подходящей кнопки нет;
  * выполняет координатный `ClickAtAsync`;
  * даёт DOM 500 мс на активацию.
* `AttachmentMenuButtonPointExpression`:

  * ищет только `button[data-testid="composer-plus-btn"]`;
  * исключает невидимые элементы;
  * исключает `disabled` и `aria-disabled="true"`;
  * прокручивает выбранную кнопку в центр;
  * вычисляет координаты после прокрутки;
  * не выполняет DOM `click()` и не выбирает пункт upload/native chooser.
* В новом primary/control чате отсутствие кнопки приводит к `E2D-BUILD-AUDIT-SUBMIT-ATTACHMENT-MISSING`, а не к продолжению без активации.
* После активации сохраняются все предыдущие гарантии:

  * exact `#upload-files` либо composer-only fallback;
  * отсутствие глобального file-input fallback;
  * семантическое исключение image-only inputs;
  * identity-bound backend node и page-global registry;
  * точная проверка имён и числа файлов;
  * события `input` и `change`;
  * ожидание единственной ZIP-плашки при пустом composer;
  * финальная проверка prompt и attachment непосредственно перед Send.
* Reuse path сначала пытается использовать уже доступный input без обязательного открытия меню. Если input отсутствует, выполняется одна попытка активации той же точной кнопкой и повторный поиск.
* Интеграционный fixture исполняет production point expression и проверяет набор из скрытой, disabled и валидной кнопок. Возвращена центральная точка валидной кнопки; подтверждён вызов `scrollIntoView`.
* Существующий browser-contract тест продолжает исполнять production JavaScript для выбора/commit attachment input и покрывает foreign, image-only, filename/count и disconnected cases.
* Документация синхронизирована: новый primary/control path описывает точную plus-кнопку, координатный клик, отсутствие menu-item/native-chooser действий и прямой reuse path.
* `--deep-research` отсутствует в parser, usage и new-send contract. Legacy report-card recovery остаётся read-only.
* Повторно проверены manual Public API profile, generated manifest и subset-member gate. `RenderingServer` intentional differences и `strictParityEvidence.status = not_verified` сохранены.
* Целевой r16 preflight прошёл 8 из 8 проверок. `AuditSubmitCodexChromeUsesOrdinaryChatAndKeepsLegacyReportRecovery` успешно выполнен.
* Все текущие package checks имеют ожидаемый и фактический код завершения `0`: generated API/docs/wiki freshness, API compatibility, audit contracts, follow-ups, documentation, licenses и git checks.
* Все 101 важный файл представлен полным снимком. Доказательственного пробела нет.
* В `repo-after/`, patch и evidence не найдено реальных ключей, токенов, паролей или иных credentials.
* Изменение относится к операторскому browser tooling и не влияет на производительность игрового runtime.

Техническая привязка:

* Реализация:

  * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  * `AuditSubmitPromptSubmissionDriver`
  * `AttachFilesAsync`
  * `OpenAttachmentMenuAsync`
  * `AttachmentMenuButtonPointExpression`
  * `QueryFileInputBackendNodeIdAsync`
  * `WaitForAttachmentChipAsync`
* Parser:

  * `eng/Electron2D.Build/AuditSubmitCommand.cs`
* Тест:

  * `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  * `AuditSubmitCodexChromeUsesOrdinaryChatAndKeepsLegacyReportRecovery`
  * `RunAuditSubmitAttachmentMenuButtonFixtureAsync`
* Документация:

  * `docs/release-management/audit-package.md`
* Прошлый принятый отчёт:

  * `docs/verdicts/release-management/t-1137-audit-r15.md`
* Evidence:

  * `evidence/T-1137-r16/preflight/r16-attachment-activation-closure/T-1137-r16/preflight-sanitized/summary.json`: `8/8`
  * `01-attachment-new-chat-contract.output.txt`: тест успешно пройден
  * `evidence/T-1137-r16/checks/*-current/`: все коды `0`
  * архивные preflight-наборы r04–r15
* Проверки: `full current-scope engineering review`, `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `blocker disproof`, `full file review`

RISKS_AND_NOTES:

* INFO_NOTE I1

  * Полное поведенческое совпадение всех одобренных типов с Godot 4.7 не входит в текущую область. Корректно сохраняется `strictParityEvidence.status = not_verified`; class-level parity остаётся за задачами ROADMAP Section 2.
  * `Actionable: false`
  * Техническая привязка:

    * Идентификатор: `I1`
    * Служебный класс: `out-of-scope/info note`

CLOSURE_DECISION:

* T-1137 r16 можно закрыть.
* Проверка опровержения не выявила неоднозначного UI-поиска или обхода attachment guards: новый чат активируется только точной доступной plus-кнопкой, после чего прежний identity-bound и chip-before-prompt контракт остаётся обязательным.
* Незакрытых блокирующих проблем текущей области не осталось.
