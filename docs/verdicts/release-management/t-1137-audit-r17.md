VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проведена полная повторная инженерная проверка T-1137 r17. Новый file-chooser lifecycle в основном встроен последовательно: точная plus-кнопка, включение CDP interception, точные upload actions, прежняя identity-bound установка ZIP и последующее отключение interception.
* Однако двухступенчатая ветка `Attach photos and files → Upload from computer` ошибочно сообщает об успехе, даже если второй обязательный пункт не найден и не нажат.
* Тесты исполняют выражения поиска каждого пункта отдельно, но не проверяют производственную оркестрацию и её отрицательную ветку.
* Это нарушает заявленный fail-closed контракт новой функциональности и не позволяет принять r17.
* Предыдущие закрытия Public API и attachment path r12–r16 не регрессировали.

Техническая привязка:

* `metadata.taskId`: `T-1137`
* `metadata.iteration`: `r17`
* `metadata.scopeTaskIds`: `["T-1137"]`
* `combined scope`: нет
* Baseline: `df40ddeba69fd013f7ce879f80f298becaddd96e`
* `metadata.previousVerdictChain`: 18 отчётов, включая `docs/verdicts/release-management/t-1137-audit-r16.md`
* `metadata.blockerClosureList`: содержит запись о локальном r16 activation failure и заявленном закрытии в r17
* `metadata/repo-file-snapshots.json`: 102 полных снимка, неполных записей нет

BLOCKERS:

* B1

  * Что не так: В `ClickAttachmentUploadPathAsync` при отсутствии прямого `Upload from computer` код нажимает группу `Attach photos and files`, повторно ищет прямой пункт и нажимает его только при наличии. Но после этого метод безусловно возвращает `true`, даже если повторный поиск вернул `null`.
  * `ActivateAttachmentInputAsync` воспринимает этот `true` как успешное прохождение upload action и оставляет interception активным. Дальнейшая логика может найти уже существовавший input и выполнить `DOM.setFileInputFiles`, хотя обязательная в текущем контракте прямая upload action не была активирована.
  * Почему это важно: Текущая область r17 прямо утверждает двухступенчатый путь «точная группа, затем точная прямая action» и fail-closed поведение. Реализация вместо этого принимает неполный путь за успешный. Это тот же класс ошибки активации, который уже дважды проявился при чистой контрольной отправке.
  * Что исправить:

    * После нажатия группы возвращать `false`, если повторный поиск `Upload from computer` не нашёл допустимый пункт.
    * Возвращать `true` только после фактического координатного клика по прямой upload action.
    * При неуспехе гарантированно отключать `Page.setInterceptFileChooserDialog` и выдавать `E2D-BUILD-AUDIT-SUBMIT-ATTACHMENT-MISSING`.
    * Желательно различать в диагностике отсутствие группы и отсутствие прямого пункта после раскрытия группы.
  * Как проверить исправление:

    * Исполнить production orchestration с прямым пунктом на первом уровне: результат `true`, один клик по direct action.
    * Исполнить ветку с группой и появляющимся после неё direct action: результат `true`, клики строго `group → direct`.
    * Исполнить ветку с группой, после которой direct action не появляется: результат `false`, отключение interception, отсутствие установки файла и Send.
    * Исполнить случай без direct и без group: результат `false`.
  * Проверка опровержения: Проверены `ActivateAttachmentInputAsync`, `ClickAttachmentUploadPathAsync`, finally-блок `AttachFilesAsync`, production point expressions, интеграционный fixture, документация и r17 evidence. Отдельные fixture-проверки доказывают, что оба элемента можно найти в искусственном DOM, но не исполняют ветвление метода и не проверяют отсутствие direct action после клика группы. Последующий ZIP-chip guard способен остановить Send, однако не делает ложный результат активации корректным и не доказывает заявленный file-chooser lifecycle.
  * Техническая привязка:

    * `File/symbol`:

      * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `ClickAttachmentUploadPathAsync`, безусловный `return true` после group branch
      * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `ActivateAttachmentInputAsync`
      * `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `RunAuditSubmitAttachmentMenuItemFixtureAsync`
      * `docs/release-management/audit-package.md`, описание двухступенчатого upload path
    * `Criterion`: fail-closed штатный attachment path; заявленная последовательность group → direct action; реалистичность тестов; согласованность документации и реализации
    * `Evidence`: `directPoint` после group может быть `null`, но метод всё равно выполняет `return true`
    * `Impact`: неполная активация file-chooser lifecycle считается успешной
    * `Fix`: успех только после фактического direct-action click
    * `Verification`: исполняемые orchestration-тесты четырёх веток с проверкой кликов и interception cleanup

EVIDENCE_REVIEW:

* Полностью прочитан сохранённый verdict r16. Его решение и доказательства сохранены.
* Проверена новая реализация:

  * plus-кнопка выбирается точным `data-testid`;
  * file chooser interception включается через `Page.setInterceptFileChooserDialog`;
  * прямой и групповой пункты ищутся по ограниченным русским и английским подписям;
  * клики выполняются координатно;
  * native chooser не используется для выбора файла;
  * ZIP устанавливается прежним `DOM.setFileInputFiles`;
  * interception отключается в `finally` после установки/commit;
  * сохраняются exact filename/count и ZIP-chip-before-prompt guards.
* Прямая ветка `Upload from computer` корректно возвращает успех только после клика.
* Групповая ветка содержит B1: результат второго поиска не влияет на итоговый `true`.
* Интеграционный тест исполняет три production point expressions:

  * plus;
  * direct upload;
  * attachment group.
* Fixture всегда предоставляет одновременно и group, и direct. Он проверяет только вычисленные координаты; производственный `ClickAttachmentUploadPathAsync` не вызывается, последовательность появления direct после group не моделируется, отрицательная ветка отсутствует.
* Документация заявляет точный direct action либо group с последующим direct action; тем самым не документирует наблюдаемое безусловное принятие одной group action.
* Предыдущие attachment-гарантии проверены повторно и сохранены: composer-only input, image-only filtering, registry identity, exact file validation, события, chip gate и final payload guard.
* `--deep-research` отсутствует в parser/usage/new-send automation; legacy recovery остаётся read-only.
* Public API profile и generated manifest не регрессировали; `strictParityEvidence.status = not_verified` сохранён.
* Все 102 важных файла имеют полные снимки. Доказательственного пробела нет.
* Целевые и текущие проверки завершаются успешно, но не покрывают B1, поскольку проверяют элементы отдельно, а не orchestration.
* Реальных секретов, приватных ключей, токенов, паролей или живых credentials не найдено.
* Изменение относится к browser tooling и не влияет на игровой горячий путь.

Техническая привязка:

* Реализация:

  * `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  * `AttachFilesAsync`
  * `ActivateAttachmentInputAsync`
  * `ClickAttachmentUploadPathAsync`
  * `SetFileChooserInterceptionBestEffortAsync`
  * `AttachmentMenuButtonPointExpression`
  * `AttachmentMenuItemPointExpression`
* Тест:

  * `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  * `AuditSubmitCodexChromeUsesOrdinaryChatAndKeepsLegacyReportRecovery`
  * `RunAuditSubmitAttachmentMenuItemFixtureAsync`
* Документация:

  * `docs/release-management/audit-package.md`
* Прошлый отчёт:

  * `docs/verdicts/release-management/t-1137-audit-r16.md`
* Evidence:

  * `evidence/T-1137-r17/`
  * текущие `checks/*-current/`
  * целевой file-chooser lifecycle preflight
* Проверки: `full current-scope engineering review`, `implementation content review`, `test coverage review`, `documentation review`, `task compliance review`, `secret scanning`, `scope scanning`, `previous verdict files`, `verbatim preservation`, `previous blockers closure`, `blocker disproof`, `full file review`

RISKS_AND_NOTES:

* INFO_NOTE I1

  * Полное поведенческое совпадение всех одобренных типов с Godot 4.7 не входит в текущую область. Корректно сохраняется `strictParityEvidence.status = not_verified`.
  * `Actionable: false`
  * Техническая привязка:

    * Идентификатор: `I1`
    * Служебный класс: `out-of-scope/info note`

CLOSURE_DECISION:

* T-1137 r17 остаётся открытой до исправления B1.
* После исправления нужен новый audit ZIP с тестом именно производственной двухступенчатой оркестрации, включая обязательный отказ, когда direct upload action не появляется после раскрытия группы.
