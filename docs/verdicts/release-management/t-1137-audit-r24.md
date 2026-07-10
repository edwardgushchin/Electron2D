VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проведена полная инженерная проверка T-1137 r24.
* Отправка единственного `change` вместо пары `input` + `change` реализована в production DOM expression и исполняется существующей DOM-фикстурой.
* Проверка уже подготовленного payload также добавлена перед открытием меню и выбором file input.
* Однако новая ветка повторного использования payload не исполняется тестами. Тесты отдельно проверяют DOM-выражение распознавания payload и отдельно порядок обычной отправки через абстрактный driver, но не доказывают, что `AttachFilesAsync` при уже существующей ZIP-плашке действительно возвращается до открытия attachment UI и `DOM.setFileInputFiles`.
* Это важная часть текущей области r24: она должна предотвращать повторную загрузку после прерванного r23 submit. Статический поиск имени метода в исходнике не является поведенческим доказательством.
* Поэтому изменение пока нельзя принять полностью.

Техническая привязка:

* `metadata.taskId`: `T-1137`
* `metadata.iteration`: `r24`
* `metadata.scopeTaskIds`: `["T-1137"]`
* `metadata.scopeSummary`: единственное событие `change` и повторное использование уже готового payload без второго upload
* `combined scope`: нет
* Baseline: `df40ddeba69fd013f7ce879f80f298becaddd96e`
* `metadata.previousVerdictChain`: включает отчёты по r22 и принятый r23
* `metadata.blockerClosureList`: закрытие r22 B1 сохранено
* `metadata/repo-file-snapshots.json`: 108 полных записей, неполных снимков нет

BLOCKERS:

* B1

  * Что не так: Ветка `HasExpectedComposerPayloadAsync → return` в production `AttachFilesAsync` не покрыта исполняемым тестом. Тест `AuditSubmitPromptPayloadReadyAllowsEmptyPromptOnlyForZipOnlyReuse` проверяет только JavaScript-выражение распознавания payload. Он не вызывает `AttachFilesAsync` и не доказывает отсутствие открытия меню, перехвата chooser, поиска input, `DOM.setFileInputFiles`, commit event и ожидания новой плашки.

  * Статическая проверка лишь убеждается, что имя `HasExpectedComposerPayloadAsync` присутствует в исходнике. Она не проверяет порядок вызова или фактический ранний выход.

  * Почему это важно: Текущая область r24 прямо требует безопасно продолжать прерванную отправку с уже прикреплённым ZIP без повторного upload. Именно эта ветка должна предотвращать повторное прикрепление при следующем запуске. Без её исполнения тестами пакет доказывает распознавание DOM и обычную отправку по отдельности, но не доказывает требуемую оркестрацию.

  * Что исправить:

    * Вынести проверку и загрузку в стабильный внутренний driver-контракт либо добавить иной управляемый production seam.
    * Исполняемым тестом вызвать production `AttachFilesAsync` при точном готовом payload с ожидаемым prompt и ZIP-плашкой.
    * Отдельно проверить вариант с пустым prompt и ожидаемой ZIP-плашкой.
    * В обоих случаях подтвердить, что не вызываются attachment-menu activation, chooser interception, поиск file input, `DOM.setFileInputFiles`, commit и повторное ожидание новой плашки.
    * Добавить отрицательные случаи: неверное имя ZIP, отсутствующая плашка, посторонний prompt и неоднозначные attachment roots должны продолжать обычный upload либо завершаться fail-closed согласно контракту.

  * Как проверить исправление:

    * Поведенческий тест возвращает трассу production orchestration.
    * Для уже готового payload трасса содержит только проверку payload и ранний успешный выход.
    * Для отсутствующего или неподходящего payload трасса переходит к штатному attachment path.
    * Тест должен исполнять production-ветвление, а не искать строки в C#-файле.

  * Проверка опровержения: Проверены полный production-файл, все релевантные тесты, DOM-fixtures, документация, metadata и текущий r24 evidence. DOM-фикстура подтверждает единственное событие `change`; payload-fixtures подтверждают работу `PromptPayloadReadyExpression`; prompt-flow driver подтверждает общий порядок `attach → fill → guard → send`. Ни одна проверка не вызывает production `AttachFilesAsync` с уже готовым payload и не наблюдает отсутствие upload-side effects.

  * Техническая привязка:

    * `File/symbol`:

      * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `AttachFilesAsync`
      * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `HasExpectedComposerPayloadAsync`
      * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `PromptPayloadReadyExpression`
      * `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `AuditSubmitPromptPayloadReadyAllowsEmptyPromptOnlyForZipOnlyReuse`
      * `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `AuditSubmitPromptSubmissionUsesOrdinaryChatByDefault`
      * `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, статическая проверка `Assert.Contains("HasExpectedComposerPayloadAsync", source, ...)`
    * `Criterion`: покрытие важной ветки текущей задачи, реалистичность тестов, fail-closed browser orchestration
    * `Evidence`: production early-return существует, но тесты исполняют только его DOM-предикат и отдельную абстрактную prompt-flow последовательность
    * `Impact`: повторный запуск после прерванной отправки не имеет регрессионного доказательства отсутствия второго upload
    * `Fix`: поведенчески исполнить production ready-payload branch и проверить отсутствие всех upload-side effects
    * `Verification`: управляемая трасса production `AttachFilesAsync` для готового, пустого, неверного и неоднозначного payload

EVIDENCE_REVIEW:

* Полностью прочитана реализация в `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`.

* Подтверждено исправление событий file input:

  * после точной проверки выбранного input и имён файлов отправляется ровно одно bubbling/composed событие `change`;
  * событие `input` из `AttachmentInputCommitExpression` удалено;
  * marker и registry entry очищаются;
  * неправильное имя, число файлов или disconnected input по-прежнему отклоняются.

* Исполняемая DOM-фикстура использует production commit expression и подтверждает точную последовательность событий `["change"]`.

* Подтверждено, что `AttachFilesAsync` вызывает `HasExpectedComposerPayloadAsync` до attachment-menu activation и возвращается при положительном результате.

* `HasExpectedComposerPayloadAsync` проверяет два допустимых состояния:

  * ожидаемый prompt и точная ZIP-плашка;
  * пустой prompt и точная ZIP-плашка.

* `PromptPayloadStatusExpression` ограничивает поиск видимым composer-контекстом, отбрасывает историю и ссылки, требует одно ожидаемое имя файла и ровно один attachment root.

* Соответствующие DOM-fixtures проверяют prompt, пустой prompt, отсутствие вложения, plain filename без attachment root и вложение в истории.

* При этом связующая ветка раннего выхода тестом не исполняется, что образует B1.

* Production-калибровка r23 сохранена: общий предел 10 секунд, attempt budget 3 секунды и задержка 250 мс. Очищенное измерение current-Web latency около 1,9 секунды остаётся в пакете.

* Документация `repo-after/docs/release-management/audit-package.md` соответствует задуманному поведению: один `change` и повторное использование готового payload.

* Текущий preflight прошёл 8 из 8 проверок. Целевой запуск выполнил два теста; build tool, документация, лицензии, аудиторские контракты, follow-up-записи и whitespace прошли.

* Проверены доступные прошлые verdict-файлы и список закрытия замечаний. Закрытие r22 B1 не регрессировало; признаков подмены исторических отчётов не найдено.

* Все важные файлы представлены полными снимками; доказательственного пробела по содержимому файлов нет.

* Проверены код, patch и evidence на реальные секреты, приватные ключи, пароли, токены и конфиденциальные локальные пути. Таких данных не найдено; обнаруженные маркеры относятся к защитным тестам, замещённым значениям и историческим отчётам.

* Изменение относится к служебной browser automation и не влияет на игровой горячий путь или Public API.

Техническая привязка:

* Реализация:

  * `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  * `SubmitPromptAsync`
  * `AttachFilesAsync`
  * `HasExpectedComposerPayloadAsync`
  * `AttachmentInputCommitExpression`
  * `PromptPayloadReadyExpression`
  * `PromptPayloadStatusExpression`

* Тесты:

  * `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  * `AuditSubmitCodexChromeUsesOrdinaryChatAndKeepsLegacyReportRecovery`
  * `AuditSubmitPromptSubmissionUsesOrdinaryChatByDefault`
  * `AuditSubmitPromptPayloadReadyRequiresPromptTextAndAuditZipChip`
  * `AuditSubmitPromptPayloadReadyAllowsEmptyPromptOnlyForZipOnlyReuse`
  * `RunAuditSubmitAttachmentInputFixtureAsync`
  * `RunAuditSubmitPromptPayloadReadyFixtureAsync`

* Документация:

  * `repo-after/docs/release-management/audit-package.md`

* Evidence:

  * `evidence/T-1137-r24/preflight/r24-single-upload-closure/T-1137-r24/preflight-sanitized/summary.json`: `8/8`
  * `01-attachment-new-chat-contract.output.txt`: два теста пройдены
  * `02-build-build-tool.output.txt`
  * `03-update-docs-check.output.txt`
  * `04-verify-audit-contracts.output.txt`
  * `05-verify-docs.output.txt`
  * `06-verify-licenses.output.txt`
  * `07-verify-audit-followups.output.txt`
  * `08-git-diff-check.output.txt`
  * `evidence/T-1137-r24/preflight/r23-production-latency-evidence/T-1137-r23/current-web-menu-lookup-sanitized.txt`

* Выполненные проверки: `full current-scope engineering review`, `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `full file review`, `blocker disproof`, `architecture coherence`.

RISKS_AND_NOTES:

* INFO_NOTE I1

  В пакете нет отдельного current-Web запуска, который наблюдаемо подтверждает ровно одну загрузку после перехода на единственное событие `change`. Production DOM expression и исполняемая фикстура дают достаточное доказательство самого event-контракта, поэтому одно лишь отсутствие live-запуска не оформляется вторым blocker-ом.

  `Actionable: false`

  Техническая привязка:

  * Идентификатор: `I1`
  * Служебный класс: `unsupported concern`
  * Недостающее доказательство: очищенная трасса React uploader после единственного `change`

* INFO_NOTE I2

  Полное поведенческое совпадение всей публичной поверхности с Godot 4.7 не входит в r24. Публичный API не менялся; `strictParityEvidence.status = not_verified` сохранён.

  `Actionable: false`

  Техническая привязка:

  * Идентификатор: `I2`
  * Служебный класс: `out-of-scope/info note`

CLOSURE_DECISION:

* T-1137 r24 остаётся открытой до закрытия B1.
* Исправление двойного DOM-события доказано, но новая центральная ветка повторного использования уже прикреплённого ZIP проверена только по частям.
* Нужен новый audit ZIP с исполняемым тестом production ready-payload orchestration и наблюдаемым отсутствием повторных upload-side effects.
