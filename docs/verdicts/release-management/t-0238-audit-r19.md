VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен полный пакет текущей области по полным снимкам файлов из `metadata/repo-file-snapshots.json`, а не по patch-only inspection: manifest, metadata, `repo-after/`, `repo-before/`, patch, прошлые verdict-файлы из `metadata.previousVerdictChain` и приложенные evidence.
- Пакет действительно закрывает оба blocker-а из `r18`: `FillPromptExpression` теперь сохраняет English selected-pill variant, а новый behavioral test реально исполняет production `FillPromptExpression` вместе с production `DeepResearchSelectedExpression`.
- Но принять изменение нельзя. В основной ветке `audit submit` для обычной отправки исчезло baseline-игнорирование уже существующих Deep Research targets, хотя документация текущей задачи несколько раз утверждает обратное. Это создаёт регрессию именно в штатном primary submit после `NEEDS_FIXES`, то есть в одном из центральных путей T-0238. Дополнительно тесты и приложенный focused corridor этот регресс не ловят.
- Техническая привязка:
  - `metadata.taskId`: `T-0238`
  - `metadata.iteration`: `r19`
  - `metadata.scopeTaskIds`: `["T-0238"]`
  - Проверенные материалы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`, `repo-after/eng/Electron2D.Build/*.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/*.md`, `repo-after/.codex/prompts/goal-task-loop.md`, `repo-after/AGENTS.md`, `repo-after/TASKS.md`, `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `...r02.md`, `...r04.md`, `...r16.md`, `...r18.md`, `evidence/T-0238-r19/checks/*`.

BLOCKERS:
- B1
  - Что не так: В обычной ветке `audit submit` удалено baseline-игнорирование уже существующих Deep Research targets. До изменения штатный primary submit снимал список targets до отправки и потом игнорировал их при поиске готового отчёта. В текущем коде вместо этого передаётся пустой `HashSet`, то есть старые targets из reuse-conversation снова участвуют в выборе готового отчёта.
  - Почему это важно: Это ломает заявленный контракт исправительных primary-итераций после `NEEDS_FIXES`. В reuse-конversation могут оставаться старые ready targets от прошлых отчётов. Без baseline-игнорирования обычный submit может получить неоднозначность или работать по старому target-у вместо текущей отправки. Для T-0238 это блокирующая проблема, потому задача как раз должна сделать внешний audit-loop детерминированным и проверяемым.
  - Что исправить: Вернуть baseline-снимок existing Deep Research targets только для обычного submit path, то есть восстановить вызов `SnapshotDeepResearchTargetIdsAsync(...)` в `SubmitAndWaitForReportAsync`. Пустой ignore-set должен остаться только у `--download-report-only`, где это и было обоснованным исправлением.
  - Как проверить исправление: Добавить regression coverage именно для ordinary submit path и повторно прогнать focused corridor. Минимум нужно доказать, что `SubmitAndWaitForReportAsync` снимает baseline до отправки и что pre-send targets исключаются при последующем выборе ready target.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:60-84`; baseline `repo-before/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:60-84`; `repo-after/docs/release-management/audit-package.md:126`, `130`, `573`, `575`
    - `Criterion`: `implementation content review`, `task compliance review`, `backend path`, `observable behavior`, `documentation review`
    - `Evidence`:
      - в baseline ordinary submit использовал `await SnapshotDeepResearchTargetIdsAsync(browser, tabId, linked.Token)` перед отправкой (`repo-before/...AuditSubmitCodexChromeCommand.cs:68`)
      - в текущем ordinary submit вместо этого создаётся пустой `new HashSet<string>(StringComparer.Ordinal)` (`repo-after/...AuditSubmitCodexChromeCommand.cs:68`)
      - документация текущего пакета прямо утверждает обратное: для обычного submit pre-send targets должны игнорироваться (`repo-after/docs/release-management/audit-package.md:126`; также подтверждено в `:130`, `:573`, `:575`)
      - метод `SnapshotDeepResearchTargetIdsAsync` в текущем файле остался, но ordinary submit его больше не вызывает (`repo-after/...AuditSubmitCodexChromeCommand.cs:1831-1836`)
    - `Impact`: регрессия в штатной primary submit ветке на reuse-conversation; текущая реализация больше не соответствует собственному контракту задачи и может сорвать детерминированный выбор отчёта
    - `Fix`: вернуть baseline ignore-set в `SubmitAndWaitForReportAsync` и оставить пустой ignore-set только в `DownloadReportFromUrlAsync`
    - `Verification`: focused regression test на ordinary submit path с pre-send ready targets; затем повторить `audit-submit-deep-research-fillprompt-focused-tests` и пакетные checks

- B2
  - Что не так: Тесты и приложенные evidence не покрывают регресс ordinary submit path по baseline existing targets. В пакете есть test на то, что `--download-report-only` больше не делает baseline-ignore, и есть новый test на сохранение selected pill при `FillPromptExpression`, но нет ни одного focused test, который проверяет обратную гарантию для обычного submit: pre-send targets должны сниматься в baseline и исключаться после отправки.
  - Почему это важно: Из-за отсутствия такого теста пакет не поймал реальную регрессию из B1, хотя документация продолжает обещать защитное поведение. Это прямое нарушение обязательного test coverage review для важной ветки текущей задачи.
  - Что исправить: Добавить focused regression test на ordinary submit path. Допустимый минимум — source-level/regression test, который проверяет, что `SubmitAndWaitForReportAsync` использует `SnapshotDeepResearchTargetIdsAsync` и передаёт полученный ignore-set в `WaitForReportAsync`. Лучше — behavioral/internal-contract test, который моделирует pre-send и post-send ready targets и подтверждает, что старые targets не участвуют в выборе.
  - Как проверить исправление: Обновлённый focused suite должен явно включать новый ordinary-submit baseline-target test и зелёно проходить в evidence вместе с уже существующим fill-prompt test.
  - Техническая привязка:
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:3973-3981`; `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5651-5664`; `evidence/T-0238-r19/checks/audit-submit-deep-research-fillprompt-focused-tests/command.txt`
    - `Criterion`: `test coverage review`, `realistic tests`, `task compliance review`
    - `Evidence`:
      - текущий source-level regression test покрывает только read-only путь и специально требует пустой ignore-set именно для `DownloadReportFromUrlAsync` (`RepositoryBuildToolTests.cs:3973-3981`)
      - новый behavioral test покрывает только `FillPromptExpression`/`DeepResearchSelectedExpression` и не касается baseline existing targets ordinary submit path (`RepositoryBuildToolTests.cs:5651-5664`)
      - focused corridor из evidence запускает fill-prompt и Deep Research selection/new-conversation tests, но не содержит отдельного кейса на ordinary submit baseline ignore-set (`evidence/T-0238-r19/checks/audit-submit-deep-research-fillprompt-focused-tests/command.txt`)
    - `Impact`: регресс из B1 остался недоказанным и поэтому попал в пакет
    - `Fix`: добавить focused test на ordinary submit baseline-target behavior и включить его в evidence corridor
    - `Verification`: новый focused test должен падать на текущем коде и проходить после возврата `SnapshotDeepResearchTargetIdsAsync` в ordinary submit

EVIDENCE_REVIEW:
- Проверены metadata и состав пакета:
  - `AUDIT-MANIFEST.md`
  - `metadata/audit-package.input.json`
  - `repo-file-hashes.json`
  - `metadata/repo-file-snapshots.json`
  - `T-0238.patch`
- Проверены полные итоговые snapshots changed scope в `repo-after/`:
  - `.codex/prompts/goal-task-loop.md`
  - `AGENTS.md`
  - `TASKS.md`
  - `data/documentation/electron2d-local-docs-index.json`
  - `data/documentation/local-docs-index/documentation.ndjson`
  - `docs/release-management/AUDIT-REQUEST.md`
  - `docs/release-management/audit-package.md`
  - `docs/verdicts/release-management/t-0238-audit-r01.md`
  - `docs/verdicts/release-management/t-0238-audit-r02.md`
  - `docs/verdicts/release-management/t-0238-audit-r04.md`
  - `docs/verdicts/release-management/t-0238-audit-r16.md`
  - `docs/verdicts/release-management/t-0238-audit-r18.md`
  - `eng/Electron2D.Build/AuditFollowupVerifier.cs`
  - `eng/Electron2D.Build/AuditPackageCommand.cs`
  - `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - `eng/Electron2D.Build/AuditSubmitCommand.cs`
  - `eng/Electron2D.Build/Program.cs`
  - `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
- Проверены baseline-снимки в `repo-before/` для сравнения поведения ordinary submit, в том числе `AuditSubmitCodexChromeCommand.cs`, `AuditSubmitCommand.cs`, документации и тестов.
- Проверены прошлые verdict-файлы из `metadata.previousVerdictChain` и `metadata.blockerClosureList`. Закрытие blocker-ов `r18` по English selected pill и behavioral FillPrompt test подтверждается. Существенных проблем по secret scanning, scope scanning и полноте snapshots не найдено.
- Проверены raw evidence checks:
  - `evidence/T-0238-r19/checks/audit-submit-deep-research-fillprompt-focused-tests/*`
  - `evidence/T-0238-r19/checks/update-docs-check/*`
  - `evidence/T-0238-r19/checks/verify-docs/*`
  - `evidence/T-0238-r19/checks/verify-audit-followups/*`
  - `evidence/T-0238-r19/checks/verify-licenses/*`
  - `evidence/T-0238-r19/checks/git-diff-check/*`
- Техническая привязка:
  - Metadata/inventory: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`
  - Реализация: `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `AuditPackageCommand.cs`, `AuditSubmitCodexChromeCommand.cs`, `AuditSubmitCommand.cs`, `Program.cs`
  - Тесты: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - Документация: `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`, `repo-after/TASKS.md`
  - Previous verdict files: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `...r02.md`, `...r04.md`, `...r16.md`, `...r18.md`
  - Evidence artifacts: `evidence/T-0238-r19/checks/*`

RISKS_AND_NOTES:
- FOLLOW_UP_FINDING F1
  - Идентификатор: `F1`
  - Где найдено: `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`; исторические ссылки в `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `...r02.md`, `...r04.md`, `...r16.md`, `...r18.md`
  - Проблема: Исторический технический долг сохраняется: `verify audit-followups` проверяет форму closure note, но не подтверждает, что `tracked-existing`/`tracked-new` действительно указывают на реальную существующую или созданную задачу.
  - Почему не блокирует текущую задачу: Текущий отказ вызван не этим долгом, а регрессией ordinary submit path и отсутствием её coverage. Семантическая проверка target task уже несколько итераций подряд классифицировалась как отдельный follow-up, а не как blocker текущего scope.
  - Куда перенести: существующая или новая release-management задача на semantic validation closure targets
  - Рекомендуемый приоритет: `P2`
  - Как проверить: добавить integration tests, где `tracked-existing` на несуществующую задачу падает, корректный existing target проходит, а `tracked-new` требует проверяемой записи о создании новой задачи
  - Техническая привязка:
    - Служебный класс: `follow-up finding`
    - Связанные технические имена: `verify audit-followups`, `tracked-existing`, `tracked-new`

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений. Пакет действительно закрывает blocker-ы `r18`, но одновременно содержит новую доказуемую регрессию в основном submit path: ordinary `audit submit` больше не игнорирует pre-send Deep Research targets, хотя текущая документация и контракт задачи требуют именно этого защитного поведения.
- Сначала нужно вернуть baseline ignore-set для ordinary submit, затем добавить focused regression test на этот путь и только после этого повторно собирать и отправлять новую итерацию.
