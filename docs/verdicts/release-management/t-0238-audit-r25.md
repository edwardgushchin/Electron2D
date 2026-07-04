VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен полный текущий пакет задачи, а не только упаковка. Я прочитал `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, все изменённые полные файлы из `repo-after/`, соответствующие baseline-снимки из `repo-before/`, `T-0238.patch`, сохранённые прошлые verdict-файлы из `metadata.previousVerdictChain` и предоставленные raw evidence checks.
- Основная часть заявленного объёма действительно реализована: пакет содержит обновлённый контракт внешнего аудита, CLI-поверхность `verify audit-followups`, сохранённые прошлые `NEEDS_FIXES` verdict-файлы, проверки control-audit metadata и focused regressions по r24.
- Но текущее изменение нельзя принять. В production-path повторной проверки выбранного режима Deep Research остался доказуемый ложноположительный сценарий: видимый интерактивный элемент с connector metadata, вложенный в prompt, принимается как «выбранная плашка», хотя документация и критерии задачи требуют отличать настоящую selected pill от menu item. Кроме того, документация внутри текущего scope сама себе противоречит по правилу `data-inline-selection-pill` и не совпадает с фактической реализацией.
- Техническая привязка: `metadata.taskId = T-0238`, `metadata.iteration = r25`; проверены `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `metadata/repo-file-snapshots.json`, `repo-file-hashes.json`, `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/Program.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/.codex/prompts/goal-task-loop.md`, `repo-after/TASKS.md`, `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `r02`, `r04`, `r16`, `r18`, `r19`, `r20`, `r21`, `r24`, а также `evidence/T-0238-r25/checks/*`.

BLOCKERS:
- B1
  - Что не так: Повторная проверка выбранного режима Deep Research перед отправкой всё ещё принимает неправильное состояние за валидную selected pill. В `DeepResearchSelectedExpression` есть ранний успешный путь для любого видимого descendant-элемента с connector metadata внутри prompt-а. Этот путь срабатывает раньше фильтра, который должен отбрасывать menu item и обычные кнопки. Поэтому интерактивный элемент меню, если он видим и вложен в prompt DOM, будет засчитан как выбранный режим, хотя это не отдельная выбранная плашка.
  - Почему это важно: В текущей задаче это центральный production-path. Контракт прямо требует перед отправкой ZIP повторно подтвердить отдельную выбранную плашку `Глубокое исследование`, а не просто любой видимый кусок connector metadata. Если команда ошибочно считает menu item выбранной плашкой, она может отправить аудит без реально активного Deep Research режима. Это ломает заявленное закрытие исторической проблемы выбора Deep Research и делает `RequireDeepResearchSelectedAsync(...)` недостоверной защитой.
  - Что исправить: Убрать ранний позитивный путь «любой видимый descendant connector metadata внутри prompt-а» как достаточное условие. Для nested path должны действовать те же требования, что и для sibling path: элемент должен быть именно selected pill или её допустимым эквивалентом, а не menu item, button, listbox/popup residue или другой интерактивный элемент меню.
  - Как проверить исправление: Добавить focused regression, который исполняет production `DeepResearchSelectedExpression` на DOM-фикстуре с видимым `role="menuitem"` или `button` с `connector_openai_deep_research`, вложенным внутрь prompt. До исправления выражение даёт `true`; после исправления должно давать `false`. Затем повторить focused suite по Deep Research и обязательные checks пакета.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2652-2669`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2683-2705`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:965-978`
    - `Criterion`: `repo-after/TASKS.md:1804`, `repo-after/TASKS.md:1824`; `repo-after/docs/release-management/audit-package.md:89`, `repo-after/docs/release-management/audit-package.md:128`
    - `Evidence`:  
      - prompt-level fast path возвращает `true`, если `Array.from(prompt.querySelectorAll(connectorMetadataSelectors)).some(visible)`; в этом месте нет проверки `isMenuOrPlainButton(...)` и нет требования selected pill  
      - отбрасывание menu item / plain button реализовано только позже, в fallback-ветке `hasConnectorMetadata(...)` вне prompt fast path  
      - перед отправкой ZIP код опирается именно на это выражение: `RequireDeepResearchSelectedAsync(...)` вызывается сразу после `FillPromptAsync(...)`
    - `Impact`: ложноположительная проверка активного режима блокирует принятие T-0238, потому что задача заявляет надёжную повторную валидацию выбранного Deep Research перед отправкой
    - `Fix`: синхронизировать nested-path selected-state с правилом «настоящая selected pill, а не menu item»
    - `Verification`: новый focused regression на visible nested menu item / button inside prompt; затем `dotnet test ... --filter FullyQualifiedName~AuditSubmitDeepResearch`, `dotnet run --project eng/Electron2D.Build -- verify audit-followups`, `verify docs`, `verify licenses`, `git diff --check`

- B2
  - Что не так: Документация внутри текущего scope противоречит и задаче, и фактическому поведению. В `audit-package.md` один абзац говорит, что выбранный инструмент нужно распознавать рядом с prompt по connector metadata, если ChatGPT рисует pill как sibling-элемент. Но дальше тот же документ утверждает, что connector metadata засчитывается только вместе с `data-inline-selection-pill` или как потомок такого pill. Это не совпадает с текущей реализацией и тестами, где connector metadata без старого `data-inline-selection-pill` намеренно считается валидным сценарием.
  - Почему это важно: В этой задаче документация — часть приёмочного контракта, а не декоративный текст. Когда операторская документация одновременно описывает два разных правила selected-state, нельзя понять, какое поведение считается правильным. Это мешает и реализации, и будущим аудитам, и объясняет, почему текущий production-path и focused tests расходятся с одним из текстовых правил.
  - Что исправить: Переписать правило selected-state в `docs/release-management/audit-package.md` в одну непротиворечивую формулировку, которая совпадает с фактическим intended behavior задачи. Если валидны pill-варианты без `data-inline-selection-pill`, это нужно явно написать. Если невалидны menu item и plain buttons, это правило должно одинаково распространяться и на nested, и на sibling случаи.
  - Как проверить исправление: После правки документа должен остаться один согласованный контракт selected-state без противоречия между соседними абзацами, а focused tests должны покрывать все описанные варианты: valid pill without old inline marker, reject plain text, reject menu item near prompt, reject menu item nested in prompt.
  - Техническая привязка:
    - `File/symbol`: `repo-after/docs/release-management/audit-package.md:122-128`; `repo-after/TASKS.md:1804-1805`, `repo-after/TASKS.md:1824`; `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5779-5790`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5810-5824`
    - `Criterion`: `documentation review`; `repo-after/TASKS.md:1739-1760`, `repo-after/TASKS.md:1804-1805`, `repo-after/TASKS.md:1824`
    - `Evidence`:  
      - `audit-package.md:122` допускает sibling-pill распознавание по `data-id` / `data-system-hint-type` / `data-keyword`  
      - `audit-package.md:128` утверждает более жёсткое и другое правило: только вместе с `data-inline-selection-pill` или как потомок такого pill  
      - тест `AuditSubmitDeepResearchSelectedSelectorAcceptsConnectorMetadataWithoutInlineMarker` подтверждает, что текущая реализация считает connector metadata без inline marker допустимым состоянием  
      - task history в `TASKS.md:1824` прямо фиксирует цель «даже без старого data-inline-selection-pill»
    - `Impact`: текущая документация недостоверна для операторского и аудиторского контракта задачи, поэтому пакет не проходит обязательную documentation review
    - `Fix`: привести документацию к одному согласованному правилу, совпадающему с intended behavior и с focused tests
    - `Verification`: перечитать обновлённый `docs/release-management/audit-package.md`, затем повторить `update docs --check`, `verify docs` и focused Deep Research tests

EVIDENCE_REVIEW:
- Проверены metadata и область пакета:
  - `AUDIT-MANIFEST.md`
  - `metadata/audit-package.input.json`
  - `repo-file-hashes.json`
  - `metadata/repo-file-snapshots.json`
  - `T-0238.patch`
- Проверены полные итоговые snapshots changed scope в `repo-after/` и baseline-снимки в `repo-before/`:
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
  - `docs/verdicts/release-management/t-0238-audit-r19.md`
  - `docs/verdicts/release-management/t-0238-audit-r20.md`
  - `docs/verdicts/release-management/t-0238-audit-r21.md`
  - `docs/verdicts/release-management/t-0238-audit-r24.md`
  - `eng/Electron2D.Build/AuditFollowupVerifier.cs`
  - `eng/Electron2D.Build/AuditPackageCommand.cs`
  - `eng/Electron2D.Build/AuditSubmitCommand.cs`
  - `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - `eng/Electron2D.Build/Program.cs`
  - `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
- Проверены прошлые verdict-файлы из `metadata.previousVerdictChain` и заявленные закрытия в `metadata.blockerClosureList`. По текущему архиву подтверждаются закрытия r01/r02/r04/r16/r18/r19/r20/r21 и закрытие r24 B1 по visible/enabled plus button. Но заявленная надёжная selected-state защита остаётся неполной из-за blocker B1.
- Проверены raw evidence checks:
  - `evidence/T-0238-r25/checks/audit-submit-control-followups-and-deep-research-focused-tests-r25/*`
  - `evidence/T-0238-r25/checks/update-docs-check/*`
  - `evidence/T-0238-r25/checks/verify-docs/*`
  - `evidence/T-0238-r25/checks/verify-audit-followups/*`
  - `evidence/T-0238-r25/checks/verify-licenses/*`
  - `evidence/T-0238-r25/checks/git-diff-check/*`
- По evidence подтверждено:
  - focused suite прошёл `45/45`
  - `verify audit-followups` прошёл и сообщил `7 actionable findings across 84 saved audit reports`
  - `verify docs`, `verify licenses` и `git diff --check` прошли
- Дополнительно проверено:
  - Полнота snapshots достаточна: в `metadata/repo-file-snapshots.json` у changed files стоит `fullContentIncluded: true`
  - Scope leak в текущем пакете не обнаружен: declared scope `T-0238` согласуется с фактическими изменениями
  - Реальных секретов, приватных ключей и локальных абсолютных путей в changed scope пакета не найдено
  - Техническая привязка: `metadata/audit-package.input.json:7-10`, `149-174`; `metadata/repo-file-snapshots.json`; `evidence/T-0238-r25/checks/*/stdout.txt`, `stderr.txt`, `exit-code.txt`

RISKS_AND_NOTES:
- ACCEPTED_RISK R1
  - Человекочитаемое объяснение: В пакете по-прежнему присутствует уже известный и явно оформленный риск: verifier closure notes не проверяет семантически, что `tracked-existing` или `tracked-new` действительно указывают на существующую и подходящую follow-up задачу. Это не новый дефект текущей итерации и не причина отказа именно для r25. Риск уже записан как принятый и имеет следующий момент пересмотра.
  - Техническая привязка:
    - `Идентификатор`: `R1`
    - `Где найдено`: `repo-after/TASKS.md:1891-1902`
    - `Rationale`: риск оформлен как `accepted-risk` с `affected area`, `impact`, `likelihood`, `mitigation`, `owner/next decision point`, `decision state`
    - `Next decision point`: `T-0105 risk register`
    - Служебный класс: `accepted risk`

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений. Пакет в целом качественно оформлен, содержит полные snapshots, прошлые verdict-файлы и зелёный локальный коридор, но обязательный production-path selected-state для Deep Research остаётся недостоверным, а операторская документация в текущем scope описывает противоречивый контракт. Пока эти два дефекта не закрыты, текущий пакет T-0238 r25 нельзя считать завершённым.
- Техническая привязка: блокируют `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/docs/release-management/audit-package.md`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/TASKS.md`, `metadata/audit-package.input.json` r25 closure summary.
