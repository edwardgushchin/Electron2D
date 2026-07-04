VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен полный пакет `T-0238` итерации `r24` по полным снимкам файлов из `metadata/repo-file-snapshots.json`, а не по одному patch. Я прочитал manifest, metadata, все изменённые файлы в `repo-after/`, соответствующие baseline-снимки в `repo-before/`, предыдущие verdict-файлы из `metadata.previousVerdictChain`, `TASKS.md`, документацию и приложенные результаты локальных проверок.
- Пакет в целом аккуратно оформлен: scope `T-0238` согласован между manifest, metadata и фактическим diff; полные snapshots присутствуют; контрольная проверка `metadata.previousVerdictChain` и `metadata.blockerClosureList` для прошлых saved verdict-ов выполнена; секретов, токенов, приватных ключей, conversation URL и машинных абсолютных путей в изменённой области я не нашёл.
- Закрытие `r21` по clean-control metadata действительно осталось на месте. Проблема текущего пакета — не в control-аудите, а в заявленном закрытии `r22 pre-send Deep Research selection failure`. В этой области остались две доказуемые функциональные ошибки в production-коде и соответствующие пробелы focused tests. Обе ошибки находятся прямо в новых DOM-выражениях, через которые `audit submit --new-conversation` должен надёжно выбрать настоящий режим `Глубокое исследование` и повторно подтвердить его перед отправкой ZIP.
- Поэтому пакет нельзя принять: текущая итерация заявляет, что r22 закрыт, но production-path по-прежнему может либо не найти реальный пункт `Глубокое исследование`, либо принять скрытую/неактивную connector-метку за «выбранную плашку» и отправить ZIP без реально активного режима.

- Техническая привязка:
  - `metadata.taskId`: `T-0238`
  - `metadata.iteration`: `r24`
  - `metadata.scopeTaskIds`: `["T-0238"]`
  - `metadata.scopeSummary`: описывает сохранение `r21` fix и закрытие `r22` Deep Research selection defect
  - Проверенные материалы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`, `repo-after/**`, `repo-before/**`, `evidence/T-0238-r24/checks/**`
  - Ключевые файлы реализации и критериев: `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `docs/release-management/audit-package.md`, `TASKS.md`, `docs/verdicts/release-management/t-0238-audit-r21.md`
  - Проверенные прошлые verdict-файлы из `metadata.previousVerdictChain`: `t-0238-audit-r01.md`, `r02.md`, `r04.md`, `r16.md`, `r18.md`, `r19.md`, `r20.md`, `r21.md`

BLOCKERS:
- B1
  - Что не так: Выражение выбора пункта `Глубокое исследование` в меню `+` привязано не к тому же visible/enabled plus-контролу, который реально нажимается. `DeepResearchMenuPointExpression` ищет видимую и доступную кнопку `+`, а `DeepResearchItemPointExpression` отдельно берёт просто первый `document.querySelector('button[data-testid="composer-plus-btn"]')`. Если на странице раньше в DOM есть скрытая, старая или неактивная кнопка `+`, геометрия `inComposerMenu(...)` считается от неё, а не от реально открытого меню. В таком состоянии production selector может не найти настоящий пункт `Глубокое исследование` и снова завершиться `E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING`.
  - Почему это важно: Главная цель `r24` — доказать закрытие `r22 pre-send Deep Research selection failure`. Текущий код всё ещё оставляет детерминированный false-negative в самой ключевой точке: выбор настоящего menu item через `+` перед отправкой ZIP. Это не теоретический стиль-вопрос, а прямой сбой текущего acceptance-path.
  - Что исправить: Использовать для `DeepResearchItemPointExpression` тот же критерий поиска plus-кнопки, что и в `DeepResearchMenuPointExpression`: visible, enabled, без `aria-disabled="true"`. Лучше вынести общий helper, чтобы клик по `+` и последующее вычисление области меню исходили из одного и того же DOM-элемента.
  - Как проверить исправление: Добавить focused regression, который исполняет production `DeepResearchItemPointExpression` или полный production-pair `DeepResearchMenuPointExpression` + `DeepResearchItemPointExpression` на фикстуре с двумя кнопками `composer-plus-btn`: первая скрыта или недоступна, вторая видима и рядом с реальным menu item. До исправления такой кейс должен воспроизводить отказ, после исправления — возвращать координату настоящего интерактивного menu item. Затем повторить focused suite и заявленный локальный коридор.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2717-2723`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2736-2757`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2776-2788`
    - `Criterion`: `repo-after/TASKS.md:1824`; `repo-after/docs/release-management/audit-package.md:89`
    - `Evidence`:
      - меню нажимается через видимую и доступную кнопку: `DeepResearchMenuPointExpression` использует `Array.from(...).find((element) => visible(element) && !element.disabled && element.getAttribute('aria-disabled') !== 'true')` в `AuditSubmitCodexChromeCommand.cs:2717-2719`
      - поиск menu item берёт просто первый `querySelector('button[data-testid="composer-plus-btn"]')` без проверки видимости и доступности в `AuditSubmitCodexChromeCommand.cs:2736-2737`
      - фильтрация кандидатов menu item затем зависит от этого `plusRect` через `inComposerMenu(...)` в `AuditSubmitCodexChromeCommand.cs:2747-2757`
      - focused tests покрывают только сценарий с одной единственной plus-кнопкой: `RepositoryBuildToolTests.cs:5734-5747`, helper fixture создаёт один `plusButton` и не моделирует дубликаты/скрытые кнопки в `RepositoryBuildToolTests.cs:8495-8513`
      - `metadata.blockerClosureList` уже заявляет, что r22 selection failure закрыт, но этот false-negative остаётся: `metadata/audit-package.input.json:168`
    - `Impact`: заявленное закрытие r22 неполное; `audit submit --new-conversation` остаётся ненадёжным в production-path выбора Deep Research через `+`
    - `Fix`: синхронизировать источник `plusRect` с реально кликаемой visible/enabled plus-кнопкой и добавить regression test на скрытую/старую plus-кнопку раньше в DOM
    - `Verification`: новый focused DOM regression на duplicate-plus scenario; затем повторить `dotnet test ... --filter ...AuditSubmitDeepResearch...`, `dotnet run --project eng/Electron2D.Build -- verify audit-followups`, `verify docs`, `verify licenses`, `git diff --check`

- B2
  - Что не так: Повторная проверка выбранной плашки Deep Research перед отправкой даёт ложноположительный результат для скрытой connector-метки внутри prompt-а. В `DeepResearchSelectedExpression` есть ранний выход `if (prompt.querySelector(connectorMetadataSelectors) !== null) return true;`, и он не проверяет видимость найденного элемента. То есть достаточно, чтобы внутри prompt DOM остался скрытый или нулевого размера узел с `data-id="connector:connector_openai_deep_research"` или `data-keyword="Deep Research"`, и команда посчитает режим активным, даже если на экране нет отдельной выбранной плашки.
  - Почему это важно: Контракт задачи и документации требует перед отправкой ZIP повторно подтвердить именно отдельную выбранную плашку `Глубокое исследование`, а текстовая подсказка или неактивный DOM-хвост не считаются доказательством активного режима. Сейчас новая защитная проверка `RequireDeepResearchSelectedAsync(...)`, добавленная в `r24`, опирается на выражение, которое может принять скрытую метку за активный режим. Это прямо подрывает заявленное закрытие r22.
  - Что исправить: Убрать безусловный `prompt.querySelector(...) !== null` как достаточное условие или заменить его на проверку видимого selection-pill элемента. Для nested/sibling path нужны одинаковые требования: элемент должен быть видимым, иметь ненулевой rect и не быть menu/listbox/popup-остатком. После этого `RequireDeepResearchSelectedAsync(...)` действительно начнёт проверять активный режим, а не любое скрытое connector metadata.
  - Как проверить исправление: Добавить focused regression, где внутри prompt имеется скрытый descendant с connector metadata (`display:none`, `visibility:hidden` или `width/height = 0`), но реальной выбранной плашки нет. До исправления `DeepResearchSelectedExpression` возвращает `true`; после исправления должен возвращать `false`. Желательно покрыть и скрытый nested element, и скрытого sibling near prompt.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2647-2655`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2657-2669`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2691-2705`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:965-978`
    - `Criterion`: `repo-after/TASKS.md:1824`; `repo-after/docs/release-management/audit-package.md:89`
    - `Evidence`:
      - выражение формально умеет проверять видимость элементов через `visible(...)` в `AuditSubmitCodexChromeCommand.cs:2647-2650`
      - но перед любой visible-проверкой делает ранний `return true` при первом найденном descendant connector metadata внутри prompt-а: `AuditSubmitCodexChromeCommand.cs:2669`
      - только fallback-ветка near-prompt использует visible-фильтр: `AuditSubmitCodexChromeCommand.cs:2702-2705`
      - перед отправкой ZIP код теперь действительно полагается на эту проверку как на защиту: `RequireDeepResearchSelectedAsync(...)` вызывается сразу после `FillPromptAsync(...)` в `AuditSubmitCodexChromeCommand.cs:74-77` и реализована в `AuditSubmitCodexChromeCommand.cs:965-978`
      - focused tests покрывают видимый sibling с metadata, отсутствие `data-inline-selection-pill`, menu item и plain text, но не покрывают скрытую connector-метку: `RepositoryBuildToolTests.cs:5751-5805`; helper fixture моделирует только видимые элементы и не создаёт hidden descendants в `RepositoryBuildToolTests.cs:7720-7805`
      - `metadata.blockerClosureList` утверждает, что selected-state «accepts a real connector metadata pill near the prompt» и «still rejects plain @Глубокое исследование text», но current code принимает и скрытый nested metadata node: `metadata/audit-package.input.json:168`
    - `Impact`: `RequireDeepResearchSelectedAsync(...)` может пропустить отправку ZIP без реально активного Deep Research режима; заявленное закрытие r22 остаётся недостоверным
    - `Fix`: требовать видимую отдельную selected-pill metadata и убрать hidden-descendant false positive
    - `Verification`: focused DOM regression на hidden nested/sibling connector metadata; затем повторить тот же focused suite и обязательные checks пакета

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
  - `docs/verdicts/release-management/t-0238-audit-r19.md`
  - `docs/verdicts/release-management/t-0238-audit-r20.md`
  - `docs/verdicts/release-management/t-0238-audit-r21.md`
  - `eng/Electron2D.Build/AuditFollowupVerifier.cs`
  - `eng/Electron2D.Build/AuditPackageCommand.cs`
  - `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - `eng/Electron2D.Build/AuditSubmitCommand.cs`
  - `eng/Electron2D.Build/Program.cs`
  - `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
- Проверены baseline-снимки в `repo-before/` по тем же путям, где файл существовал в baseline.
- Проверена `previous verdict`-цепочка и способ закрытия прошлых blocker-ов:
  - `r01`, `r02`, `r04`, `r16`, `r18`, `r19`, `r20`, `r21` прочитаны по сохранённым verdict-файлам
  - заявленные закрытия `r21` по control metadata и `r20` по follow-up verifier/ignored target selection подтверждаются текущим кодом и тестами
  - заявленное закрытие `r22 pre-send Deep Research selection failure` не подтверждается из-за блокеров B1 и B2
- Проверены raw evidence checks:
  - `evidence/T-0238-r24/checks/audit-submit-control-followups-and-deep-research-focused-tests-r24/*`
  - `evidence/T-0238-r24/checks/update-docs-check/*`
  - `evidence/T-0238-r24/checks/verify-docs/*`
  - `evidence/T-0238-r24/checks/verify-audit-followups/*`
  - `evidence/T-0238-r24/checks/verify-licenses/*`
  - `evidence/T-0238-r24/checks/git-diff-check/*`
- По evidence подтверждено:
  - focused suite прошёл `43/43`
  - `verify audit-followups` прошёл для `7 actionable findings across 83 saved audit reports`
  - `verify docs`, `verify licenses` и `git diff --check` прошли
- Отдельно проверены обязательные инженерные оси review:
  - `performance`: текущий scope — tooling/automation, а не runtime hot path движка; явного performance blocker-а в hot path 2D runtime не найдено
  - `Public API` / `Godot 4.7`: текущий scope не меняет public engine API, поэтому эти оси проверялись как sanity check на отсутствие скрытой public-surface деградации
  - `test realism`: production DOM-выражения действительно исполняются из собранного production-кода, но важных веток hidden/duplicate DOM state для r24 fix не хватает
  - `C# style/best-practices` и `architecture coherence`: новых safety-проблем, тест-only branch в production или scope leak вне `T-0238` я не нашёл

- Техническая привязка:
  - Полнота snapshots: `metadata/repo-file-snapshots.json`
  - Scope и summary: `metadata/audit-package.input.json`, `AUDIT-MANIFEST.md`
  - Raw evidence summary: `evidence/T-0238-r24/checks/*/exit-code.txt`, `stdout.txt`, `stderr.txt`

RISKS_AND_NOTES:
- ACCEPTED_RISK R1
  - Человекочитаемое объяснение: В пакете остаётся уже известный и явно принятый риск: verifier closure notes по-прежнему не проверяет семантически, что `tracked-existing` или `tracked-new` действительно указывают на существующую и подходящую follow-up задачу. Это аккуратно оформлено как accepted-risk в `TASKS.md` и не ухудшено текущим изменением. Для текущего `r24` это не блокер: пакет не расширил этот риск и не сломал уже зафиксированный способ его отслеживания.
  - Техническая привязка:
    - `Идентификатор`: `R1`
    - `Где найдено`: `repo-after/TASKS.md:1890-1901`
    - `Rationale`: риск уже оформлен как `accepted-risk` с описанием области, влияния, вероятности, mitigation и decision state
    - `Next decision point`: `T-0105 risk register`
    - Служебный класс: `accepted risk`

CLOSURE_DECISION:
- Задача в текущем виде остаётся открытой до исправлений. Пакет качественно оформлен и действительно закрывает большую часть исторических замечаний, но именно заявленное закрытие `r22` пока недоказано: production submit-path всё ещё может промахнуться мимо настоящего menu item из-за неправильной привязки к plus-кнопке и может принять скрытую connector-метку за выбранный режим перед отправкой.
- После исправления двух production-веток и добавления focused regressions на hidden/duplicate DOM scenarios пакет можно будет повторно оценить как полноценный current-scope engineering review для `T-0238 r24` без этих blocker-ов.
