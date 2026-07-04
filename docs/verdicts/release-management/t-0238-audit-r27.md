VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен полный текущий пакет T-0238 r27 по полным снимкам файлов, metadata, тестам, документации, сохранённым прошлым verdict-файлам и raw evidence. Архив читается, область пакета согласована, полнота снимков достаточна.
- В r27 действительно есть полезные исправления: `--dump-dom-only` теперь сохраняет базовый DOM и deep-research selected diagnostics до любых ожиданий internal frame, а `DeepResearchSelectedExpression` больше не принимает широкий page/history ancestor с вложенной старой плашкой как выбранный режим.
- Но изменение всё ещё нельзя принять. Во-первых, selected-state защита Deep Research остаётся неполной: интерактивные элементы и их потомки всё ещё могут пройти как «выбранная плашка», хотя документация и closure r25 прямо запрещают кнопки, menu item и похожий интерактивный мусор. Во-вторых, `AUDIT-REQUEST.md` по-прежнему не закрепляет обязательный zero-context контракт для `Suggested new task`, хотя это явный критерий приёмки T-0238.

Техническая привязка:
- `metadata.taskId`: `T-0238`
- `metadata.iteration`: `r27`
- `metadata.scopeTaskIds`: `["T-0238"]`
- `metadata.scopeSummary`: `metadata/audit-package.input.json:7-12`
- Проверены: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`, `repo-after/**`, `repo-before/**`, `evidence/T-0238-r27/checks/**`
- Ключевые файлы реализации и контрактов: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/Program.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`
- Проверенные прошлые verdict-файлы из `metadata.previousVerdictChain`: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `r02`, `r04`, `r16`, `r18`, `r19`, `r20`, `r21`, `r24`, `r25`

BLOCKERS:
- B1
  - Что не так: Проверка выбранного Deep Research режима всё ещё допускает ложноположительное состояние. Текущее выражение умеет отбрасывать сам `button` / `role="button"` / `role="menuitem"` / `role="option"` только когда connector metadata висит на самом проверяемом элементе, но не когда metadata находится на вложенном `span/div` внутри такого интерактивного предка. Кроме того, для элемента с `data-inline-selection-pill` есть ранний успешный путь, который вообще не применяет фильтр `isMenuOrPlainButton(...)`. В результате интерактивная кнопка или menu-like элемент рядом с prompt всё ещё может быть засчитан как «выбранная плашка».
  - Почему это важно: Это не просто новый долг, а неполное закрытие прошлых замечаний по r25/r26. Текущая задача обещает, что перед отправкой ZIP команда повторно удостоверяется в реальном выбранном режиме `Глубокое исследование`, а не в остатках меню, кнопки или старого UI-элемента. Пока выражение допускает интерактивный элемент или его потомка, защита `RequireDeepResearchSelectedAsync(...)` остаётся недостоверной и архив нельзя считать принятым.
  - Что исправить: Нужно запрещать не только сам интерактивный элемент, но и кандидатов, находящихся внутри `button`, `role="button"`, `role="menuitem"` и `role="option"`. Ранний путь для `data-inline-selection-pill` тоже должен проходить через те же ограничения. Дополнительно нужно расширить регрессионные тесты на вложенную connector metadata внутри интерактивного предка и на интерактивный элемент с inline marker.
  - Как проверить исправление: Добавить focused behavioral/regression tests, которые исполняют production `DeepResearchSelectedExpression` на DOM-фикстурах с:
    - дочерним `span[data-id="connector:connector_openai_deep_research"]` внутри `button`,
    - дочерним connector-элементом внутри `role="button"` / `role="menuitem"` / `role="option"`,
    - интерактивным элементом с `data-inline-selection-pill`.
    Во всех этих случаях результат должен быть `false`. После этого повторить focused suite по Deep Research и обязательные checks пакета.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2699-2727`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2728-2731`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:982-994`
    - `File/symbol`: `repo-after/docs/release-management/audit-package.md:128`
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5809-5870`
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:7802-8068`
    - `File/symbol`: closure claim about r25 — `metadata/audit-package.input.json:176`
    - `Criterion`: `previous blockers closure`, `implementation content review`, `test coverage review`, `task compliance review`, `observable behavior`
    - `Evidence`:
      - `isMenuOrPlainButton(...)` проверяет роль и тег самого элемента, а также только близость к `role="menu"`, `role="listbox"` и popover-wrapper; интерактивный предок вроде обычной кнопки ею не отсекается: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2699-2706`
      - `hasConnectorMetadata(...)` возвращает `true` для `isSelectionPill && (directConnector || nestedConnector)` до применения интерактивного фильтра: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2716-2726`
      - список кандидатов включает `div` и `span`, поэтому дочерний connector-элемент внутри кнопки остаётся самостоятельным кандидатом: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2728-2731`
      - текущая фикстура selected-state моделирует metadata на одном `selectedPill`-элементе и не строит случай «metadata на дочернем узле интерактивного предка»; этого сценария в focused tests нет: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:8020-8055`
      - `metadata.blockerClosureList` уже утверждает, что r25 B1/B2 закрыты, но по коду и тестам закрытие выбранного режима остаётся неполным: `metadata/audit-package.input.json:176`
    - `Impact`: ложноположительная проверка активного режима блокирует принятие T-0238, потому что команда может отправить аудит без реально активированного Deep Research
    - `Fix`: расширить подавление интерактивных состояний на потомков и inline-marker branch, затем добавить missing regressions
    - `Verification`: focused tests по `AuditSubmitDeepResearch*`, затем `dotnet run --project eng/Electron2D.Build -- verify audit-followups`, `dotnet run --project eng/Electron2D.Build -- verify docs`, `dotnet run --project eng/Electron2D.Build -- verify licenses`, `git diff --check`

- B2
  - Что не так: `AUDIT-REQUEST.md` по-прежнему не требует, чтобы `Suggested new task` был самодостаточным zero-context описанием новой задачи. Сейчас документ требует только указать, куда перенести finding, приоритет и идею проверки, но не требует заголовок новой задачи, затронутый домен и краткий критерий приёмки/acceptance sketch.
  - Почему это важно: Это прямой невыполненный критерий T-0238. Смысл текущей задачи — сделать `RISKS_AND_NOTES` пригодным для реального follow-up triage без скрытого контекста. Пока `Suggested new task` можно заполнить коротким идентификатором или фразой без структуры, аудиторский отчёт остаётся неполным контрактом и не даёт принимающей стороне гарантированного материала для постановки новой задачи.
  - Что исправить: Обновить `docs/release-management/AUDIT-REQUEST.md`, чтобы для `Suggested new task` было явно прописано zero-context требование: title, priority, affected domain, short acceptance sketch и verification idea. Желательно синхронизировать это тестом или другой проверяемой документационной проверкой, чтобы правило не осталось только в `TASKS.md`.
  - Как проверить исправление: После правки перечитать `AUDIT-REQUEST.md` и убедиться, что zero-context требования названы явно и полно. Затем прогнать `update docs --check`, `verify docs` и focused tests/проверки, связанные с report contract и follow-up fields.
  - Техническая привязка:
    - `File/symbol`: `repo-after/docs/release-management/AUDIT-REQUEST.md:115-123`
    - `File/symbol`: `repo-after/TASKS.md:1785-1786`
    - `File/symbol`: пример текущего минимального покрытия без zero-context surface — `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:4733-4765`
    - `Criterion`: `documentation review`, `task compliance review`
    - `Evidence`:
      - `AUDIT-REQUEST.md` перечисляет только `Куда перенести`, `Рекомендуемый приоритет` и `Как проверить`, но не требует zero-context состава новой задачи: `repo-after/docs/release-management/AUDIT-REQUEST.md:121-123`
      - в `TASKS.md` это требование зафиксировано отдельно и явно: `repo-after/TASKS.md:1786`
      - существующий focused test для structured follow-up findings использует минимальное поле `Suggested new task: T-9999` без title/domain/acceptance sketch, то есть текущий пакет не даёт проверяемой защиты от неполного follow-up описания: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:4748-4756`
    - `Impact`: один из явных критериев приёмки T-0238 остаётся невыполненным; follow-up contract всё ещё допускает неполную постановку новой задачи
    - `Fix`: внести zero-context требования в `AUDIT-REQUEST.md` и синхронизировать documentation/test coverage
    - `Verification`: повторное чтение обновлённого `AUDIT-REQUEST.md`, затем `update docs --check`, `verify docs`, focused contract tests

EVIDENCE_REVIEW:
- Проверены metadata, manifest и declared scope. `metadata.scopeTaskIds` содержит только `T-0238`; признаков скрытого combined scope не найдено. `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json` и фактический набор файлов друг другу не противоречат.
- Полнота снимков достаточна: в `metadata/repo-file-snapshots.json` для changed files стоит `fullContentIncluded: true`; все важные after/before snapshots присутствуют. Patch использовался как карта изменений, но не как замена чтению полных файлов.
- Прочитаны сохранённые прошлые verdict-файлы из `metadata.previousVerdictChain`. Они доступны в пакете и не выглядят переписанными под текущую итерацию. Цепочка closure заявлена в `metadata.blockerClosureList`, но закрытие r25 selected-state остаётся неполным из-за blocker B1 выше.
- Проверены raw evidence checks. Все приложенные проверки завершились успешно, но их зелёный статус не снимает текущие блокеры, потому что отсутствуют focused regressions на интерактивные предки/inline-button false positives и не закрыт documentation contract для zero-context `Suggested new task`.
- Проверка секретов и локальных данных не выявила реальных ключей, токенов, паролей или приватных абсолютных путей в changed scope пакета.
- Scope leak по текущему пакету не обнаружен. Изменения находятся в пределах release-management tooling, документации, task tracking и сохранённых verdict-файлов T-0238.
- Игровой runtime hot path, публичный 2D API и Godot 4.7 parity в этой итерации не меняются; performance review для текущего change ограничивается sanity check и не выявил отдельного runtime blocker-а.

Техническая привязка:
- Metadata и inventory: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`
- Implementation: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/Program.cs`
- Tests: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
- Documentation and agent rules: `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`, `repo-after/TASKS.md`
- Previous verdict files: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `r02`, `r04`, `r16`, `r18`, `r19`, `r20`, `r21`, `r24`, `r25`
- Evidence:
  - `evidence/T-0238-r27/checks/audit-submit-control-followups-and-deep-research-focused-tests-r27/stdout.txt` — `48/48`
  - `evidence/T-0238-r27/checks/verify-audit-followups/stdout.txt` — `7 actionable findings across 85 saved audit reports`
  - `evidence/T-0238-r27/checks/update-docs-check/exit-code.txt`
  - `evidence/T-0238-r27/checks/verify-docs/exit-code.txt`
  - `evidence/T-0238-r27/checks/verify-licenses/exit-code.txt`
  - `evidence/T-0238-r27/checks/git-diff-check/exit-code.txt`

RISKS_AND_NOTES:
- FOLLOW_UP_FINDING F1
  - Идентификатор: `F1`
  - Где найдено: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:4069-4079`; связанный production path — `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:221-239`
  - Проблема: Регрессия для `--dump-dom-only` проверяется source-level тестом по тексту метода, а не выполнением production-path и фактической записью файлов diagnostics. Это слабее, чем поведенческий тест на реальный контракт, который описан в задаче как предпочтительный способ доказательства.
  - Почему не блокирует текущую задачу: Реализация порядка записи в коде читается напрямую и сама по себе выглядит корректной; задача уже и так остаётся открытой из-за блокеров B1 и B2. Но такой тест повышает риск тихой регрессии в следующей итерации.
  - Куда перенести: `Suggested existing task: T-0238`
  - Рекомендуемый приоритет: `P2`
  - Как проверить: заменить source-inspection тест на behavior-level fixture, который вызывает production `DumpDomFromUrlAsync` через контролируемый browser/client contract и подтверждает наличие `deep-research-selected-result.json` и `deep-research-selected-diagnostics.json` до любых deep-research-frame dependent действий.
  - Техническая привязка:
    - `FOLLOW_UP_FINDING F1`
    - `File/symbol`: `RepositoryBuildToolTests.AuditSubmitDumpDomOnlyWritesBaseDomBeforeDeepResearchFrameDiagnostics`
    - `Why not blocker for current task`: actual implementation order is visible and current task is already blocked by stronger defects
    - `Suggested existing task`: `T-0238`
    - `Suggested priority`: `P2`
    - `Verification idea`: behavior-level regression for dump-dom diagnostics ordering

- ACCEPTED_RISK R1
  - Человекочитаемое объяснение: В пакете сохраняется уже оформленный принятый риск: verifier closure notes не проверяет семантически, что `tracked-existing` или `tracked-new` действительно указывают на существующую и подходящую follow-up задачу. Это известный риск, явно записанный в `TASKS.md`, и он не является новым blocker-ом r27.
  - Техническая привязка:
    - `ACCEPTED_RISK R1`
    - `File/symbol`: `repo-after/TASKS.md:1892-1903`
    - `Rationale`: риск уже оформлен как `accepted-risk` с требуемыми полями risk-register модели
    - `Next decision point`: `T-0105 risk register`
    - Служебный класс: `accepted risk`

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений. Пакет r27 содержит достаточные материалы для полноценной проверки и реально закрывает часть прошлых проблем, но текущая selected-state защита Deep Research всё ещё допускает ложноположительный сценарий, а `AUDIT-REQUEST.md` по-прежнему не фиксирует обязательный zero-context follow-up contract для `Suggested new task`.
- До следующего внешнего пакета нужно:
  - закрыть blocker B1 кодом и focused regressions на интерактивные предки/inline-button cases,
  - закрыть blocker B2 через правку `AUDIT-REQUEST.md` и проверяемое подтверждение документационного контракта,
  - после этого повторить focused tests, `verify audit-followups`, `verify docs`, `verify licenses` и `git diff --check`.

Техническая привязка:
- Блокируют: `B1`, `B2`
- Обязательные проверки после исправления: focused `AuditSubmitDeepResearch*` regressions, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses`, `git diff --check`
- Решение относится к текущему пакету: `metadata.taskId = T-0238`, `metadata.iteration = r27`
