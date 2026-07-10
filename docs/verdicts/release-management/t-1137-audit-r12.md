VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проведена полная инженерная проверка T-1137 r12. Принятое в r11 закрытие профиля публичного API сохранено: регрессий в `electronApiContract`, generated manifest и subset-member gate не обнаружено.
* Новое изменение r12 — выбор и подтверждение file input при отправке audit ZIP — реализует заявленный контракт некорректно. Объединённый CSS-селектор не задаёт приоритет `#upload-files` и содержит глобальный fallback на любой non-image file input страницы. Поэтому ZIP может быть установлен не в composer input.
* Тесты проверяют только наличие строк в исходнике и не воспроизводят страницу с несколькими file input. Они не обнаруживают эту ошибку.
* Пакет является одиночной областью, а не `combined scope`.
* Предыдущий принятый отчёт r11 доступен и сохранён без изменения содержательного решения.

Техническая привязка:

* `metadata.taskId`: `T-1137`
* `metadata.iteration`: `r12`
* `metadata.scopeTaskIds`: `["T-1137"]`
* Baseline: `df40ddeba69fd013f7ce879f80f298becaddd96e`
* `metadata.previousVerdictChain`: 13 отчётов, включая принятый `docs/verdicts/release-management/t-1137-audit-r11.md`
* `metadata.blockerClosureList`: содержит адресные закрытия предыдущих блокирующих проблем
* `metadata/repo-file-snapshots.json`: 97 полных снимков, `fullContentIncluded = true`
* Выполнены `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `full file review`

BLOCKERS:

* B1

  * Что не так: Инструмент обещает сначала выбирать `#upload-files`, а затем использовать fallback только внутри формы. Фактический селектор объединяет три ветки:

    * `#upload-files`;
    * `form input[type="file"]:not([accept="image/*"])`;
    * глобальный `input[type="file"]:not([accept="image/*"])`.
  * `DOM.querySelector` и `document.querySelector` возвращают первый подходящий элемент в порядке документа, а не первый селектор из списка. Поэтому расположенный выше по DOM посторонний non-image file input будет выбран даже при наличии правильного `#upload-files`. Последняя ветка прямо разрешает произвольный file input вне composer/form.
  * После `DOM.setFileInputFiles` код заново выполняет тот же неоднозначный selector через `document.querySelector`. Он не доказывает, что проверяется тот же backend node, в который был установлен файл. Проверяется только `files.length`, но не имя ожидаемого основного ZIP. При нескольких inputs подтверждение может относиться к другому элементу.
  * Почему это важно: Новая функциональность r12 добавлена именно для надёжного прикрепления основного audit ZIP к composer. При выборе другого input ChatGPT не зарегистрирует вложение в сообщении либо события будут отправлены не тому элементу. Это возвращает исходный отказ чистой контрольной отправки и нарушает заявленный штатный путь без ручного обхода.
  * Что исправить:

    * Выбирать элементы последовательно: сначала точный `#upload-files`, затем composer/form-scoped non-image input.
    * Удалить глобальный fallback на произвольный `input[type=file]` либо ограничить его доказуемым корнем текущего composer.
    * После `DOM.setFileInputFiles` проверять именно выбранный ранее элемент, например через сохранённый backend node/resolved object, а не повторный глобальный поиск.
    * Проверять ожидаемое количество и имя основного ZIP, а не только `files.length`.
    * Отправлять `input`/`change` тому же подтверждённому элементу.
  * Как проверить исправление:

    * Выполнить производственный selector/выбор на DOM-фикстуре, где раньше composer расположены image input и посторонний non-image input, а `#upload-files` находится ниже. Выбран должен быть только `#upload-files`.
    * Добавить случай без `#upload-files`, но с composer-scoped fallback и посторонним глобальным input; должен выбираться composer input.
    * Добавить случай, где установленный backend node и результат повторного глобального поиска различаются; guard должен завершиться отказом.
    * Добавить проверки неправильного имени файла, нулевого и лишнего количества файлов.
  * Проверка опровержения: Проверены полный `AttachFilesAsync`, CDP-поиск backend node, `AttachmentInputCommitExpression`, payload guard, документация, интеграционный тест и `06-attachment-input-contract.output.txt`. Последующий ZIP-chip guard может остановить отправку после неправильного прикрепления, но не делает новый attachment path рабочим. Тесты и preflight лишь ищут текстовые фрагменты и не проверяют семантику выбора при нескольких input.
  * Техническая привязка:

    * `File/symbol`:

      * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1235-1282`, `AttachFilesAsync`
      * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1287-1321`, `QueryFileInputBackendNodeIdAsync`
      * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:3649-3665`, `AttachmentInputSelector` и `AttachmentInputCommitExpression`
      * `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:9344-9347`
      * `docs/release-management/audit-package.md:688`
      * `evidence/T-1137-r12/archive-only/audit-evidence/T-1137-r12/preflight-sanitized/06-attachment-input-contract.output.txt`
    * `Criterion`: заявленный ChatGPT Web attachment contract; штатный путь без ручных обходов; реалистичность тестов; соответствие документации реализации
    * `Evidence`: глобальная третья ветка селектора и повторный `querySelector`; тест проверяет только `Assert.Contains`; preflight использует `source.Contains`
    * `Impact`: основной audit ZIP может быть установлен и подтверждён не в composer input
    * `Fix`: последовательный composer-scoped выбор, сохранение идентичности элемента, проверка имени и количества файлов
    * `Verification`: исполняемый DOM-тест с несколькими inputs и отрицательные проверки wrong node/wrong filename

EVIDENCE_REVIEW:

* Прочитаны полные итоговые версии всех изменённых файлов, включая browser automation, CLI parser, документацию, task/diary records, API profile, generated manifest, build verifiers и тесты. Patch использовался только как карта изменений.
* Сопоставление r11→r12 показывает, что функционально новые изменения сосредоточены в:

  * attachment input flow;
  * удалении `--deep-research` из allowlist и usage;
  * документации;
  * строковых regression assertions.
* Удаление `--deep-research` выполнено согласованно: флага нет в allowlist и usage, тест ожидает `Unexpected option` до подключения к браузеру.
* Прежнее закрытие `RenderingServer` проверено повторно. Экспортированные subset-члены по-прежнему имеют явные `electronApiDecision`, а валидатор отклоняет неклассифицированные решения.
* Сохранённый `t-1137-audit-r11.md` содержит принятый отчёт предыдущей итерации; его решение и технические основания не были сокращены или подменены.
* Все 23 preflight-проверки r12 заявлены успешными; focused run прошёл 17 из 17 тестов. Все текущие package checks имеют ожидаемый и фактический код завершения `0`.
* Успех attachment-проверки не опровергает B1: `06-attachment-input-contract` только ищет фрагменты `#upload-files`, `:not([accept=`, `input.files?.length` и вызовы событий.
* Все 97 важных файлов имеют полные снимки. Хеши содержимого совпадают с индексом; пять кириллических путей отображаются используемым ZIP-клиентом в иной кодировке, но соответствующие SHA-256 совпадают, поэтому доказательственного пробела по их содержимому нет.
* Реальных секретов, приватных ключей, токенов, паролей или живых credentials не найдено. Совпадения относятся к redacted/synthetic fixtures и к реализации защитного сканера.
* Изменение browser automation не затрагивает игровой горячий путь; отдельной блокирующей проблемы производительности не обнаружено.

Техническая привязка:

* Текущий evidence:

  * `evidence/T-1137-r12/archive-only/audit-evidence/T-1137-r12/preflight-sanitized/summary.json`: 23/23
  * `01-focused-r11-closure.output.txt`: 17/17
  * `06-attachment-input-contract.output.txt`: код `0`, текстовая проверка исходника
  * `evidence/T-1137-r12/checks/*-current/`: ожидаемые и фактические коды `0`
* Снимки:

  * `metadata/repo-file-snapshots.json`
  * `repo-after/`
  * `repo-before/`
  * `repo-file-hashes.json`
  * `SHA256SUMS.txt`
* Прошлые отчёты:

  * `metadata.previousVerdictChain`
  * `docs/verdicts/release-management/t-1137-audit-r01.md` — `r11.md`
  * `docs/verdicts/release-management/t-1137-audit-control-r07.md`
  * `docs/verdicts/release-management/t-1137-audit-control-r10.md`

RISKS_AND_NOTES:

* INFO_NOTE I1

  * Полное поведенческое совпадение всех одобренных типов с Godot 4.7 не входит в текущую область. Сохраняется корректный статус `strictParityEvidence.status = not_verified`.
  * `Actionable: false`
  * Техническая привязка:

    * Идентификатор: `I1`
    * Служебный класс: `out-of-scope/info note`

CLOSURE_DECISION:

* T-1137 r12 остаётся открытой до исправления B1.
* Принятый результат r11 по Public API не регрессировал, однако добавленный r12 attachment path не обеспечивает заявленный composer-scoped выбор и подтверждение конкретного ZIP.
* После исправления требуется новый пакет с исполняемыми DOM-регрессиями, а не только поиском строк в исходном коде.
