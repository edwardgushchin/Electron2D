VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проведена полная инженерная проверка T-1137 r22.
* Код сохраняет принятый в r21 общий десятисекундный предел и меняет тайм-аут отдельной попытки с одной до трёх секунд.
* Однако текущая область r22 прямо заявляет, что трёхсекундное значение выбрано по измеренному двухсекундному времени production DOM evaluation и закрывает сбой чистого контрольного запуска. В пакете нет самого измерения или успешного контрольного запуска, а тесты не исполняют production-конфигурацию с трёхсекундным тайм-аутом и двухсекундным запросом.
* Поэтому пакет доказывает сохранение общего алгоритма отмены, но не доказывает главное изменение r22 — достаточность новой настройки для фактического текущего Web UI.
* Публичный API, ранее принятые fail-closed ограничения и игровой runtime не регрессировали.

Техническая привязка:

* `metadata.taskId`: `T-1137`
* `metadata.iteration`: `r22`
* `metadata.scopeTaskIds`: `["T-1137"]`
* `metadata.scopeSummary`: закрытие clean-control pre-send latency mismatch на основании примерно двухсекундного production DOM evaluation
* `combined scope`: нет
* Baseline: `df40ddeba69fd013f7ce879f80f298becaddd96e`
* `metadata.previousVerdictChain`: включает первичный принятый отчёт `docs/verdicts/release-management/t-1137-audit-r21.md`
* `metadata/repo-file-snapshots.json`: 107 записей, неполных снимков нет

BLOCKERS:

* B1

  * Что не так: Новое значение `AttachmentMenuActionAttemptTimeout = 3 секунды` обосновано в metadata якобы измеренным временем production DOM evaluation около двух секунд. Но измерение, диагностический вывод или успешный чистый контрольный запуск в архив не включены. Целевой тест вызывает общий polling helper с искусственными значениями 40, 50 и 200 мс и потому не проверяет новую трёхсекундную production-настройку.

  * Почему это важно: Текущая область r22 состоит именно в закрытии реального pre-send latency mismatch. Простая замена константы без воспроизводимого измерения или теста соответствующего класса задержки не доказывает, что производственный путь теперь успевает получить результат. Следующий чистый запуск может снова завершиться до прикрепления ZIP.

  * Что исправить:

    * Добавить в пакет очищенное доказательство измерения production lookup на текущей загруженной странице: точный selector/expression, длительность, полученный единственный результат и контекст запуска; либо приложить успешное доказательство чистого control submit, дошедшего как минимум до подтверждённой ZIP-плашки без ручного обхода.
    * Дополнительно закрепить калибровку исполняемым тестом через production-конфигурацию: запрос должен завершаться после задержки больше прежней одной секунды, но меньше нового трёхсекундного предела, а общий десятисекундный deadline и внешняя отмена должны сохраняться.
    * Тест не должен ограничиваться поиском имени константы в исходном тексте.

  * Как проверить исправление:

    * Целевой тест вызывает production driver/configuration или внутренний контракт, который получает реальные `AttachmentMenuActionReadyTimeout`, `AttachmentMenuActionAttemptTimeout` и `AttachmentMenuActionPollInterval`.
    * Cancellation-aware lookup возвращает точку примерно через две секунды; результат должен быть принят.
    * Никогда не завершающийся lookup по-прежнему заканчивается около общего десятисекундного предела.
    * Внешняя отмена по-прежнему распространяется как `OperationCanceledException`.
    * Evidence содержит очищенный результат реального current-Web или clean-control запуска.

  * Проверка опровержения: Проверены production-код, весь целевой тест, вспомогательный polling harness, metadata, manifest, документация и все текущие r22 preflight-файлы. Preflight подтверждает один успешный интеграционный тест и восемь зелёных проверок, но не содержит заявленного двухсекундного измерения. В тесте production-константа проверяется только как строка; поведенческие вызовы используют переданные вручную тайм-ауты 40/50/200 мс.

  * Техническая привязка:

    * `File/symbol`:

      * `metadata/audit-package.input.json`, `metadata.scopeSummary`
      * `AUDIT-MANIFEST.md`, `scopeSummary`
      * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `AttachmentMenuActionAttemptTimeout`
      * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `AuditSubmitAttachmentUploadDriver.FindMenuItemPointAsync`
      * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `PollForAttachmentMenuItemAsync`
      * `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `AuditSubmitCodexChromeUsesOrdinaryChatAndKeepsLegacyReportRecovery`
      * `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `InvokeAuditSubmitAttachmentMenuPollingAsync`
      * `evidence/T-1137-r22/preflight/r22-current-web-lookup-latency-closure/`
    * `Criterion`: соответствие текущей задаче, реалистичность тестов, проверяемое доказательство заявленного времени выполнения
    * `Evidence`: production-константа равна трём секундам; поведенческие тесты используют только искусственные attempt timeout 40/50/200 мс; измерение около двух секунд отсутствует
    * `Impact`: заявленное закрытие фактического clean-control latency mismatch не подтверждено
    * `Fix`: приложить измерение или успешный чистый контрольный запуск и проверить production timeout исполняемым сценарием задержанного результата
    * `Verification`: тест задержанного примерно на две секунды lookup через production-конфигурацию плюс очищенное current-Web/control evidence

EVIDENCE_REVIEW:

* Полностью прочитана итоговая реализация `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`.

* Подтверждено, что общий алгоритм r21 сохранён:

  * общий linked timeout — 10 секунд;
  * отдельная попытка ограничивается оставшимся временем;
  * задержка ограничивается временем до deadline;
  * внутреннее истечение возвращает отсутствие результата;
  * внешняя отмена не маскируется.

* Единственное существенное изменение r22 в этом механизме — увеличение `AttachmentMenuActionAttemptTimeout` до трёх секунд.

* Полностью прочитан релевантный участок `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Тест продолжает проверять зависший lookup, позднее появление результата и внешнюю отмену, но использует уменьшенные параметры helper-а и не проверяет новую production-калибровку.

* Проверена документация `repo-after/docs/release-management/audit-package.md`. Она корректно описывает общий deadline и ограниченные попытки, но не заменяет отсутствующее доказательство фактической задержки.

* Прочитан сохранённый первичный отчёт r21. Его принятое решение и отсутствие blocker-ов не опровергают новый отдельный критерий r22 о production latency mismatch.

* Текущий r22 preflight прошёл 8 из 8 проверок:

  * целевой интеграционный тест;
  * сборка build tool;
  * синхронизация документации;
  * проверка аудиторских контрактов;
  * проверка документации;
  * лицензии;
  * follow-up-записи;
  * `git diff --check`.

* Эти проверки подтверждают сборку и прежний polling-контракт, но не содержат current-Web измерения или чистого control submit.

* Проверены `T-1137.patch`, полные снимки и текущие evidence на секреты и локальные данные. Реальных токенов, паролей, приватных ключей или конфиденциальных машинных путей не обнаружено; найденные маркеры относятся к защитным тестам, замещённым значениям и сохранённым историческим отчётам.

* Изменение находится в служебной browser automation и не влияет на игровой горячий путь.

* Признаков добавления поведения вне области T-1137 или подмены прошлых verdict-файлов не выявлено.

Техническая привязка:

* Реализация:

  * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  * `AttachmentMenuActionAttemptTimeout`
  * `FindMenuItemPointAsync`
  * `PollForAttachmentMenuItemAsync`

* Тесты:

  * `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  * `AuditSubmitCodexChromeUsesOrdinaryChatAndKeepsLegacyReportRecovery`
  * `InvokeAuditSubmitAttachmentMenuPollingAsync`

* Документация:

  * `repo-after/docs/release-management/audit-package.md`

* Прошлый отчёт:

  * `repo-after/docs/verdicts/release-management/t-1137-audit-r21.md`

* Evidence:

  * `evidence/T-1137-r22/archive-only/audit-evidence/T-1137-r22/preflight-sanitized/summary.json`: `8/8`
  * `evidence/T-1137-r22/preflight/r22-current-web-lookup-latency-closure/T-1137-r22/preflight-sanitized/01-attachment-new-chat-contract.output.txt`
  * остальные файлы `02`–`08` того же preflight-набора

* Выполненные проверки: `full current-scope engineering review`, `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `evidence gap`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `full file review`, `blocker disproof`, `architecture coherence`.

RISKS_AND_NOTES:

* INFO_NOTE I1

  Полное поведенческое совпадение всей публичной поверхности с Godot 4.7 не входит в r22. Публичный API в этой итерации не изменён, а `strictParityEvidence.status = not_verified` сохранён.

  `Actionable: false`

  Техническая привязка:

  * Идентификатор: `I1`
  * Служебный класс: `out-of-scope/info note`
  * `File/symbol`: `data/api/electron2d-api-manifest.json`, `strictParityEvidence.status`

CLOSURE_DECISION:

* T-1137 r22 остаётся открытой до закрытия B1.
* Код сохраняет корректный общий deadline, но пакет не доказывает, что новая трёхсекундная настройка действительно закрывает заявленный двухсекундный production lookup и чистый контрольный сценарий.
* Нужен новый audit ZIP с воспроизводимым доказательством production latency или успешного чистого control submit и исполняемым тестом новой production-калибровки.
