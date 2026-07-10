VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен независимый контрольный пакет задачи `T-1137`, итерация `r39`. Область включает ручной профиль публичного API, контракт `RenderingServer`, восстановление браузерного управления и однократность побочных эффектов аудита.
* Профиль API, фактический backend-путь `RenderingServer`, документация и основные проверки согласованы. Однако сохранение verdict-файла и владение браузерной вкладкой имеют два доказуемых разрыва, способных нарушить целостность аудиторского процесса.
* Техническая привязка:

  * `metadata.taskId`: `T-1137`
  * `metadata.iteration`: `r39`
  * `metadata.scopeTaskIds`: `["T-1137"]`
  * `metadata.scopeSummary`: `Independent clean control of the accepted T-1137 public API profile, RenderingServer contract, deterministic browser recovery and single-shot audit side effects, without previous verdict or process-ledger context.`
  * Тип проверки: `control audit`, `full current-scope engineering review`
  * `metadata.previousVerdictChain`: пусто
  * `metadata.blockerClosureList`: пусто

BLOCKERS:

* B1

  * Что не так: verdict записывается прямо в конечный файл с перезаписью. Запись не атомарна, существующий отчёт не защищён от замены, а маршрутизация последующих аудитов признаёт verdict только по первой непустой строке. При обрыве процесса после записи `VERDICT: ACCEPT`, но до завершения файла и перевода reservation в `completed`, усечённый контрольный отчёт будет воспринят как принятый clean control.
  * Почему это важно: такой сценарий может преждевременно закрыть аудиторский цикл либо переписать уже сохранённый verdict через `--download-report-only`. Это нарушение неизменности прошлых отчётов и глобальной безопасности процесса.
  * Что исправить: сохранять отчёт через временный файл и атомарное перемещение без перезаписи; существующий verdict разрешать только при доказанном побайтовом совпадении либо отклонять. При чтении сохранённых verdict-ов проверять полный контракт отчёта и текущую identity. Финальный clean-control `ACCEPT` должен считаться терминальным только при reservation со статусом `completed` и подтверждённой связью с сохранённым файлом.
  * Как проверить исправление:

    * существующий verdict не изменяется при повторном download/recovery;
    * усечённый файл только с первой строкой не влияет на маршрут;
    * reservation `report-received` или `failed` вместе с таким файлом не считается завершённым clean control;
    * ошибка или отмена записи не оставляет видимого конечного файла;
    * полный атомарно сохранённый отчёт и reservation `completed` корректно завершают цикл.
  * Проверка опровержения: проверены строгий extractor, проверка `metadata.taskId`/`metadata.iteration`, обработка ошибок записи и тест `AuditSubmitReservationRecordsEveryPostReservationFailure`. Они валидируют текст до записи и отмечают исключение, но не защищают конечный файл от усечения или перезаписи и не мешают `ReadSavedAuditVerdicts` принять частичный файл по первой строке.
  * Техническая привязка:

    * `File/symbol`:

      * `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:145-169`, `ExecuteReservedSubmitAsync`
      * `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:191-204`, `WriteReportAsync`
      * `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:688-717`, `ResolveAutomaticSubmitRoute`
      * `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:1280-1314`, `ReadSavedAuditVerdicts`
    * `Criterion`: `single-shot audit side effects`, `verbatim preservation`, `global safety blocker`, детерминированная маршрутизация аудита
    * `Evidence`: `File.WriteAllTextAsync` пишет непосредственно в `outputPath`; режим не запрещает существующий файл. `ReadSavedAuditVerdicts` проверяет только имя файла и первую непустую строку. `latestAcceptedControlIsClean` требует только `Control`, первую строку `VERDICT: ACCEPT` и `Route == "clean-control"`, но не `Status == "completed"`.
    * `Impact`: частичный или переписанный verdict может стать источником решения о принятии.
    * `Fix`: атомарная неизменяемая запись, полная повторная валидация сохранённого отчёта и проверка завершённого reservation.
    * `Verification`: регрессии на усечённую запись, существующий файл, отмену записи и терминальный clean-control status.

* B2

  * Что не так: создание управляемой вкладки выполняется до входа в `try/finally`. Если `CreateTabAsync` завершится исключением после того, как удалённая сторона уже создала вкладку, `FinalizeTabsAsync` вообще не вызывается. Та же ошибка присутствует в диагностическом DOM-пути.
  * Почему это важно: потеря ответа после внешнего побочного эффекта является именно тем классом отказов, который должна корректно обрабатывать новая модель однократных действий. В результате может остаться бесхозная вкладка с состоянием аудита, несмотря на документированную гарантию закрытия собственной вкладки при любом исходе.
  * Что исправить: охватить созданием `try/finally` весь интервал после подключения browser session и вызывать `finalizeTabs` даже при неоднозначном результате `createTab`. Финализация не требует известного `tabId`, поэтому она может закрыть все вкладки, принадлежащие текущей browser session.
  * Как проверить исправление: добавить тестовый драйвер, в котором `CreateTabAsync` фиксирует удалённое создание и затем бросает исключение. `FinalizeTabsAsync` должен быть вызван ровно один раз. Аналогичная проверка нужна для `DumpDomFromUrlAsync`.
  * Проверка опровержения: тест `AuditSubmitAlwaysUsesOrdinaryChatAndFinalizesOwnedTabsAcrossResults` проверяет успех, ошибку операции и ошибку callback после успешного возврата `CreateTabAsync`. Ветвь исключения непосредственно из `CreateTabAsync` не проверяется; расположение вызова перед `try` однозначно исключает финализацию в этой ветви.
  * Техническая привязка:

    * `File/symbol`:

      * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:189-204`, `RunOwnedTabOperationAsync`
      * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:273-281`, `DumpDomFromUrlAsync`
      * `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:6967-6989`, `AuditSubmitAlwaysUsesOrdinaryChatAndFinalizesOwnedTabsAcrossResults`
    * `Criterion`: `deterministic browser recovery`, `single-shot audit side effects`, гарантированное владение и закрытие вкладок
    * `Evidence`: оба вызова `CreateTabAsync` находятся перед соответствующим `try`; тесты не моделируют исключение при создании.
    * `Impact`: неоднозначный результат `createTab` оставляет внешнее состояние без обязательной очистки.
    * `Fix`: финализация browser session в `finally`, охватывающем и создание вкладки.
    * `Verification`: поведенческие тесты на ошибку создания для обычной отправки и DOM-диагностики.

EVIDENCE_REVIEW:

* Проверены структура и полнота архива:

  * `AUDIT-MANIFEST.md`
  * `metadata/audit-package.input.json`
  * `repo-file-hashes.json`
  * `metadata/repo-file-snapshots.json`
  * `T-1137.patch`
  * `SHA256SUMS.txt`
  * все `repo-after/` и соответствующие `repo-before/`
  * полные archive-only снимки `RenderingServer`, backend-абстракций, `SdlGpuStartupPolicy`, `RuntimeHost` и `RuntimeHostOptions`.
* Все контрольные суммы прошли. Индекс содержит `86` файлов, для всех установлено `fullContentIncluded: true`; отсутствующих обязательных снимков не найдено.
* Профиль содержит `1131` решений: `596 approved`, `18 deferred`, `517 unsupported`; утверждённые типы имеют `godotApiScope`, subset-типы — структурированные контракты. Manifest содержит `175` экспортированных типов и честно публикует `strictParityEvidence.status = not_verified`.
* Проверены реализация и тесты:

  * `eng/Electron2D.ApiManifestGenerator/Program.cs`
  * `eng/Electron2D.Build/AuditSubmitCommand.cs`
  * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  * `eng/Electron2D.Build/AuditPackageCommand.cs`
  * `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs`
  * `src/Electron2D.Cli/CliGeneralCommands.cs`
  * `src/Electron2D/Core/ObjectModel/Object.cs`
  * `src/Electron2D/Runtime/Application/RuntimeFramePresenter.cs`
  * `src/Electron2D/Graphics/Rendering/SdlGpuRenderingBackend.cs`
  * `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  * `tests/Electron2D.Tests.Integration/RuntimeHostTests.cs`
  * `tests/Electron2D.Tests.Integration/ApiManifestTests.cs`
  * `tests/Electron2D.Tests.Integration/RenderingServerBackendTests.cs`
  * `tests/Electron2D.Tests.Unit/RenderingServerPublicApiTests.cs`.
* Проверена документация:

  * `docs/release-management/api-compatibility.md`
  * `docs/release-management/audit-package.md`
  * `docs/documentation/api-manifest.md`
  * `docs/rendering/rendering-server.md`
  * `docs/cli/e2d-cli.md`
  * шаблонные `AGENTS.md` и workflow prompt.
* Evidence показывает успешные сборки без предупреждений и ошибок, `16` выбранных browser/audit тестов, `3` unit-теста `RenderingServer`, `5` backend/manifest/runtime тестов, а также успешные проверки manifest, Wiki, docs, API compatibility, лицензий и audit contracts.
* Изменение runtime hot path ограничено переключением профиля при создании presenter и fallback; измеримых покадровых регрессий не внесено.
* Реальных ключей, токенов, паролей, приватных ключей или конфиденциальных локальных путей не найдено. Найденные маркеры и абсолютные пути находятся в синтетических security-тестах, redacted-тексте и исходных снимках.

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:5515-5558`, `ClickAtAsync`; строки `5937-5940`, `AuditSubmitCdpRecoveryPolicy.ExecuteAsync`.
  * Проблема: одна координатная мышиная операция состоит из `mouseMoved`, `mousePressed` и `mouseReleased`, но каждый вызов с `allowTransientRecovery: false` предварительно выполняет полный CDP reattach. Таким образом, между нажатием и отпусканием меняется CDP session.
  * Почему не блокирует текущую задачу: пакет не доказывает, что Chrome обязательно теряет состояние мыши между этими session-переходами; при недоступном Markdown путь завершается fail-closed. Поэтому это доказанный дефект структуры и лишняя работа, но не отдельное доказательство ошибочного принятия verdict-а.
  * Куда перенести:

    * Suggested new task: «Сделать координатный CDP-клик единой подготовленной транзакцией». Приоритет `P1`, домен `release-management/browser automation`. Критерий приёмки: один reattach до начала жеста, затем три dispatch-команды в одной session без reattach и без повторения после неоднозначного результата.
  * Рекомендуемый приоритет: `P1`
  * Как проверить: типизированный driver-тест должен фиксировать последовательность `recover, moved, pressed, released` и отсутствие восстановления между фазами; ошибка после `pressed` не должна запускать жест повторно.
  * Техническая привязка:

    * Служебный класс: `follow-up finding`
    * `Why not blocker for current task`: отсутствует доказательство неправильного результата Chrome; путь остаётся fail-closed.

* INFO_NOTE I1

  * Контрольный пакет намеренно исключает `TASKS.md` и два файла process ledger, хотя они присутствуют в полном `git diff --name-only`. Это соответствует явному clean-control правилу и `metadata.scopeSummary`; код, тесты, доменная документация и generated evidence включены.
  * Actionable: false
  * Техническая привязка:

    * Служебный класс: `out-of-scope/info note`
    * Артефакт: `evidence/T-1137-r39/checks/git-diff-name-only/stdout.txt`
    * Правило: `docs/release-management/audit-package.md`, clean-control process-history exclusion.

CLOSURE_DECISION:

* Задача остаётся открытой. Профиль API и `RenderingServer` не дали дополнительных блокирующих расхождений, но текущий audit state machine способен принять усечённый либо перезаписанный verdict, а browser orchestration не гарантирует финализацию после неоднозначного `createTab`. После исправления B1 и B2 нужны точечные регрессионные тесты и новый полный контрольный пакет.
