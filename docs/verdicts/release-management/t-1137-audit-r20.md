VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проведена полная повторная инженерная проверка T-1137 r20. Асинхронное ожидание upload action добавлено, а неоднозначные совпадения корректно отклоняются.
* Однако заявленный предел ожидания «не более десяти секунд» фактически не обеспечивается. Каждый отдельный DOM-запрос внутри десятисекундного цикла получает тайм-аут 45 секунд.
* Если browser/CDP evaluation зависнет, один запрос может занять до 45 секунд, после чего цикл ещё проверит свой deadline. Следовательно, новый timing contract не является реальным верхним пределом.
* Тесты лишь проверяют наличие констант и не исполняют polling с зависающим запросом или управляемым временем.
* Это нарушает явную область r20 и не позволяет принять текущую итерацию.
* Предыдущие решения Public API, ordinary-only submission и attachment guards не регрессировали.

Техническая привязка:

* `metadata.taskId`: `T-1137`
* `metadata.iteration`: `r20`
* `metadata.scopeTaskIds`: `["T-1137"]`
* `combined scope`: нет
* Baseline: `df40ddeba69fd013f7ce879f80f298becaddd96e`
* `metadata.previousVerdictChain`: 21 отчёт, включая `docs/verdicts/release-management/t-1137-audit-r19.md`
* `metadata.blockerClosureList`: содержит описание локального r19 timing failure и заявленного закрытия r20
* `metadata/repo-file-snapshots.json`: 105 полных снимков, неполных записей нет

BLOCKERS:

* B1

  * Что не так: `FindMenuItemPointAsync` вычисляет deadline через `AttachmentMenuActionReadyTimeout = 10 секунд`, но каждый вызов `browser.EvaluatePointAsync` получает `UiActionTimeout = 45 секунд`. Deadline проверяется только после завершения вызова и дополнительной задержки.
  * Поэтому десятисекундный предел ограничивает только число успешно и быстро завершившихся итераций. Он не ограничивает фактическую длительность одного DOM/CDP-запроса и всего ожидания.
  * Цикл также всегда выполняет задержку 250 мс после отрицательного результата, даже если deadline уже достигнут, что дополнительно выводит длительность за заявленный предел.
  * Почему это важно: Текущая область r20 прямо закрывает pre-send timing failure bounded polling-контрактом «не более десяти секунд». Реализация может ждать один запрос до 45 секунд и потому не соответствует собственному критерию. При проблемном browser pipe оператор снова получает длительное зависание вместо предсказуемого fail-closed отказа.
  * Что исправить:

    * Для каждой итерации вычислять оставшееся время до общего deadline.
    * Передавать в `EvaluatePointAsync` тайм-аут `min(оставшееся время, короткий per-attempt timeout)`, а не общий `UiActionTimeout`.
    * Не выполнять `Task.Delay`, если deadline уже достигнут; ограничивать задержку оставшимся временем.
    * Либо создать linked cancellation token с `CancelAfter(AttachmentMenuActionReadyTimeout)` и корректно преобразовывать его истечение в `null`/штатный attachment failure, сохраняя внешний cancellation отдельно.
  * Как проверить исправление:

    * Использовать управляемый driver/clock и DOM-запрос, который никогда не завершается самостоятельно; метод должен закончиться около общего десятисекундного предела, а не через 45 секунд.
    * Проверить появление action непосредственно перед deadline: она должна быть принята.
    * Проверить отсутствие action: `null` и cleanup interception.
    * Проверить, что внешний cancellation по-прежнему немедленно прерывает операцию и не маскируется под обычный timeout.
  * Проверка опровержения: Проверены production driver, `EvaluatePointAsync`, его timeout-параметр, polling loop, тесты, документация и r20 evidence. Успешные DOM-fixtures завершают evaluation мгновенно и не моделируют зависший CDP-запрос. Наличие десятисекундной константы само по себе не ограничивает ожидаемый вызов с 45-секундным timeout.
  * Техническая привязка:

    * `File/symbol`:

      * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:37-40`, `UiActionTimeout`, `AttachmentMenuActionReadyTimeout`
      * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1426-1450`, `AuditSubmitAttachmentUploadDriver.FindMenuItemPointAsync`
      * `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:9355-9365`, строковые assertions polling-контракта
    * `Criterion`: `metadata.scopeSummary` — polling не более десяти секунд; fail-closed timing; реалистичность тестов
    * `Evidence`: `EvaluatePointAsync(..., UiActionTimeout, ...)`, где `UiActionTimeout = 45 секунд`
    * `Impact`: заявленный десятисекундный верхний предел может быть превышен более чем в четыре раза
    * `Fix`: общий deadline должен ограничивать также каждый ожидаемый DOM/CDP-вызов и задержку
    * `Verification`: тест зависшего evaluation с измеряемым общим deadline и тест внешней отмены

EVIDENCE_REVIEW:

* Полностью прочитан сохранённый verdict r19. Его решение и основания сохранены.
* Проверено новое поведение r20:

  * upload action опрашивается повторно;
  * poll interval равен 250 мс;
  * nominal deadline равен 10 секундам;
  * direct title/descendant matching сохранён;
  * при нуле или нескольких совпадениях production expression возвращает отказ;
  * `title` включён в набор проверяемых текстов.
* Проверена текущая комбинированная upload row:

  * `div.__menu-item[data-fill][tabindex]`;
  * title `Attach photos and files`;
  * дочерний subtitle `Upload from computer`;
  * direct expression возвращает центр строки.
* Отдельные direct/group варианты разведены в fixture, чтобы unique-match guard не создавал искусственную неоднозначность.
* Сохранено закрытие r17:

  * direct success только после клика;
  * group success только после `group → direct`;
  * group без direct и полное отсутствие actions возвращают `false`;
  * interception очищается на отрицательных ветках.
* Сохранены exact/composer-only input, image filtering, identity-bound file installation, точная проверка имени/числа файлов, ZIP-chip-before-prompt и final payload guard.
* Тест browser contract продолжает исполнять production DOM expressions и production activation orchestration. Новая timing-часть покрыта только поиском строк `AttachmentMenuActionReadyTimeout` и `AttachmentMenuActionPollInterval`; фактический deadline не тестируется.
* Документация говорит об ограниченном ожидании, но не раскрывает, что один внутренний вызов допускает 45 секунд.
* `--deep-research` отсутствует в parser, usage и new-send automation; legacy recovery остаётся read-only.
* Public API profile, generated manifest и subset-member gate не изменены; `strictParityEvidence.status = not_verified` сохранён.
* Целевой r20 preflight и все текущие package checks успешны, но они не покрывают B1.
* Все 105 важных файлов имеют полные снимки. Доказательственного пробела нет.
* Реальных секретов, приватных ключей, токенов, паролей или живых credentials не найдено.
* Изменение относится к browser tooling и не влияет на игровой горячий путь.

Техническая привязка:

* Реализация:

  * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  * `AuditSubmitAttachmentUploadDriver.FindMenuItemPointAsync`
  * `AttachmentMenuItemPointExpression`
  * `ClickAttachmentUploadPathAsync`
* Тест:

  * `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  * `AuditSubmitCodexChromeUsesOrdinaryChatAndKeepsLegacyReportRecovery`
  * `RunAuditSubmitAttachmentMenuItemFixtureAsync`
  * `InvokeAuditSubmitAttachmentActivationAsync`
* Документация:

  * `docs/release-management/audit-package.md`
* Прошлый отчёт:

  * `docs/verdicts/release-management/t-1137-audit-r19.md`
* Evidence:

  * `evidence/T-1137-r20/preflight/r20-async-upload-action-closure/T-1137-r20/preflight-sanitized/summary.json`: `8/8`
  * `evidence/T-1137-r20/checks/*-current/`: все коды `0`
  * предыдущие closure-preflight-наборы r04–r19
* Проверки: `full current-scope engineering review`, `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `blocker disproof`, `full file review`

RISKS_AND_NOTES:

* INFO_NOTE I1

  * Полное поведенческое совпадение всех одобренных типов с Godot 4.7 не входит в текущую область. Корректно сохраняется `strictParityEvidence.status = not_verified`.
  * `Actionable: false`
  * Техническая привязка:

    * Идентификатор: `I1`
    * Служебный класс: `out-of-scope/info note`

CLOSURE_DECISION:

* T-1137 r20 остаётся открытой до исправления B1.
* Новый polling решает асинхронное появление action только при быстро отвечающем CDP, но не обеспечивает заявленный общий десятисекундный предел при зависшем запросе.
* После исправления требуется новый audit ZIP с исполняемым тестом общего deadline и внешней отмены.
