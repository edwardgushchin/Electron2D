VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проведена полная повторная инженерная проверка T-1137 r13. Предыдущее замечание r12 в основной части закрыто: выбор теперь упорядочен, глобальный fallback удалён, идентичность выбранного элемента сохраняется, а имя и количество файлов проверяются до событий.
* Добавлен исполняемый тест production JavaScript с несколькими input, неправильным именем, пустым списком, лишним файлом и отсоединённым элементом.
* Однако заявленный выбор именно non-image input реализован неполно. Код распознаёт как image-only только точное значение `accept="image/*"`. Другие стандартные ограничения изображений, например `accept="image/png"` или `accept=".png,.jpg"`, ошибочно принимаются за подходящий audit ZIP input.
* Это оставляет доказуемую ветку выбора неправильного composer input и не позволяет закрыть текущую attachment-доработку.
* Пакет является одиночной областью, а не `combined scope`. Принятый результат r11 по Public API не регрессировал.

Техническая привязка:

* `metadata.taskId`: `T-1137`
* `metadata.iteration`: `r13`
* `metadata.scopeTaskIds`: `["T-1137"]`
* Baseline: `df40ddeba69fd013f7ce879f80f298becaddd96e`
* `metadata.previousVerdictChain`: 14 отчётов, включая `t-1137-audit-r12.md`
* `metadata.blockerClosureList`: содержит адресное закрытие r12 B1
* `metadata/repo-file-snapshots.json`: 98 полных снимков, неполных записей нет

BLOCKERS:

* B1

  * Что не так: Функция `isAuditFileInput` считает input подходящим, если нормализованный `accept` не равен буквально `image/*`. Fallback-селектор также исключает только точный атрибут `[accept="image/*"]`. Поэтому image-only элементы с `accept="image/png"`, `accept="image/jpeg"`, `accept=".png,.jpg"` или эквивалентным списком проходят обе проверки и могут быть выбраны для audit ZIP.
  * Почему это важно: Область r13 и документация обещают fallback именно на non-image file input внутри composer. При наличии нескольких composer inputs код может выбрать image-only input, после чего `DOM.setFileInputFiles` либо завершится ошибкой, либо ZIP не будет зарегистрирован штатной логикой вложений ChatGPT. Это тот же класс отказа, ради которого добавлена текущая доработка.
  * Что исправить:

    * Перебирать file inputs внутри composer, а не брать первый элемент, отличающийся только от точного `accept="image/*"`.
    * Разбирать `accept` как список MIME-шаблонов и расширений.
    * Отклонять input, если его ограничения разрешают только изображения: `image/*`, конкретные `image/...` MIME-типы или только графические расширения.
    * Предпочитать доказуемо общий/document input, например `#upload-files` с допустимым контрактом либо composer input с отсутствующим `accept`/поддержкой ZIP и других документов.
  * Как проверить исправление:

    * Добавить перед правильным fallback элементы с `accept="image/png"`, `accept="image/jpeg,.png"` и `accept=".png,.jpg"`.
    * Доказать, что каждый из них пропускается, а следующий document/general input выбирается.
    * Добавить composer только с image-specific inputs и проверить fail-closed результат.
    * Исполнять тот же production `AttachmentInputSelectionExpression`, как уже сделано для остальных DOM-сценариев.
  * Проверка опровержения: Проверены production selection expression, commit expression, полный DOM-fixture и focused evidence. Тесты моделируют `image/*`, но не другие допустимые формы image-only `accept`. Проверка точного имени ZIP после `DOM.setFileInputFiles` не устраняет неправильный выбор: она подтверждает файл уже после обращения к неподходящему input и не доказывает, что Web UI использует его как document attachment.
  * Техническая привязка:

    * `File/symbol`:

      * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:3695-3714`, `isAuditFileInput`, `fallback`
      * `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:15710-15860`, `RunAuditSubmitAttachmentInputFixtureAsync`
      * `docs/release-management/audit-package.md:688`
    * `Criterion`: выбор non-image file input; рабочий штатный attachment path; реалистичность тестов; согласованность документации и реализации
    * `Evidence`: `return accept !== 'image/*'` и CSS `:not([accept="image/*"])`
    * `Impact`: image-only composer input может быть принят как input основного audit ZIP
    * `Fix`: семантическая проверка списка `accept` и перебор composer inputs
    * `Verification`: production-JavaScript fixture с конкретными image MIME и графическими расширениями

EVIDENCE_REVIEW:

* Прочитан полный сохранённый verdict r12. Его B1 сохранён и имеет проверяемую запись закрытия в metadata.
* Проверены полные версии browser automation, CLI, документации, task/diary records, профиля API, generated manifest, build verifiers и всех изменённых тестов.
* Закрытые части r12 B1 подтверждены:

  * сначала проверяется точный `#upload-files`;
  * fallback ограничен формой текущего composer;
  * глобального file-input fallback больше нет;
  * выбранный объект сохраняется по случайному marker token;
  * commit использует тот же объект, проверяет `isConnected`, точные имена и число файлов;
  * `input` и `change` отправляются этому же объекту;
  * marker и registry entry очищаются после проверки.
* Интеграционный тест действительно выполняет production selection/commit JavaScript через Node fixture и проверяет:

  * посторонний input перед точным;
  * composer fallback;
  * отсутствие composer input;
  * правильный и неправильный filename;
  * нулевое и лишнее количество файлов;
  * отсоединённый элемент;
  * порядок событий `input`, `change`.
* Тест не покрывает доказанную ветку B1: его `FakeForm.querySelector` считает image-only только точное `accept === "image/*"`, тем самым повторяя ограничение production-кода.
* Предыдущее закрытие Public API проверено повторно: `electronApiContract`, `electronApiDecision`, намеренные отличия `RenderingServer` и fail-closed subset gate сохранены.
* Текущие настроенные проверки и preflight evidence завершились успешно. Этот успех не опровергает B1, поскольку отсутствуют варианты конкретных image MIME и расширений.
* Полнота снимков подтверждена: 98 записей с `fullContentIncluded = true`.
* Реальных секретов, приватных ключей, токенов, паролей или живых credentials не обнаружено.
* Изменение относится к операторскому browser tooling и не затрагивает игровой горячий путь; блокирующей проблемы производительности не найдено.

Техническая привязка:

* Основная реализация:

  * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  * `AttachmentInputSelectionExpression`
  * `AttachmentInputCommitExpression`
  * `QueryFileInputBackendNodeIdAsync`
  * `CommitAttachmentInputAsync`
* Тесты:

  * `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  * `AuditSubmitCodexChromeUsesOrdinaryChatAndKeepsLegacyReportRecovery`
  * `RunAuditSubmitAttachmentInputFixtureAsync`
* Документация:

  * `docs/release-management/audit-package.md`
* Прошлый отчёт:

  * `docs/verdicts/release-management/t-1137-audit-r12.md`
* Evidence:

  * `evidence/T-1137-r13/archive-only/audit-evidence/T-1137-r13/preflight-sanitized/01-focused-r12-closure.output.txt`
  * `evidence/T-1137-r13/archive-only/audit-evidence/T-1137-r13/preflight-sanitized/06-attachment-input-contract.output.txt`
  * `evidence/T-1137-r13/checks/*-current/`
* Выполненные проверки: `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `blocker disproof`, `full file review`

RISKS_AND_NOTES:

* INFO_NOTE I1

  * Полное поведенческое совпадение всех одобренных типов с Godot 4.7 не входит в r13. Корректно сохраняется `strictParityEvidence.status = not_verified`.
  * `Actionable: false`
  * Техническая привязка:

    * Идентификатор: `I1`
    * Служебный класс: `out-of-scope/info note`

CLOSURE_DECISION:

* T-1137 r13 остаётся открытой до исправления B1.
* Основная архитектура закрытия r12 стала существенно надёжнее и покрыта исполняемым тестом, но фильтр non-image input всё ещё распознаёт только один частный вариант image-only ограничения.
* После исправления требуется новый audit ZIP с production-fixture сценариями для конкретных image MIME, списков и расширений.
