VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен полный пакет `T-0238` итерации `r29` по полным итоговым файлам, metadata, тестам, документации, сохранённым прошлым verdict-отчётам и приложенным raw evidence. Архив читается, `metadata/repo-file-snapshots.json` присутствует, `repo-after/` доступен, scope пакета согласован как одиночная задача `T-0238`.
- В пакете действительно есть полезные исправления. `AUDIT-REQUEST.md` теперь закрепляет zero-context требование для `Suggested new task`, прошлые сохранённые verdict-файлы добавлены в репозиторий и включены в `metadata.previousVerdictChain`, `verify audit-followups` присутствует как отдельная командная поверхность, а цикл выбора `Глубокое исследование` в коде сначала проверяет уже открытый пункт меню перед повторным нажатием `+`.
- Но изменение всё ещё нельзя принять. Осталась одна реальная функциональная дыра в selected-state защите для современных plain menu row типов ChatGPT, а закрытие r28 submit-loop failure доказано недостаточно строго: автоматическая проверка этого ключевого поведения сведена к source-level regex, а не к поведенческому тесту через производственный код или стабильный внутренний контракт.

Техническая привязка:
- `metadata.taskId`: `T-0238`
- `metadata.iteration`: `r29`
- `metadata.scopeTaskIds`: `["T-0238"]`
- `metadata.scopeSummary`: пакет заявляет закрытие saved primary `r27 NEEDS_FIXES` и локального r28 pre-send failure без saved verdict.
- Проверены: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`, `repo-after/**`, `repo-before/**`, `evidence/T-0238-r29/checks/**`.
- Ключевые files/symbols: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/eng/Electron2D.Build/Program.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/TASKS.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`.

BLOCKERS:
- B1
  - Что не так: Проверка выбранного `Глубокое исследование` всё ещё не отбрасывает те plain interactive row типы, которые эта же автоматизация теперь считает реальными строками меню ChatGPT: `.__menu-item` и элементы с парой `data-fill` + `tabindex`. В `DeepResearchSelectedExpression` фильтр `isMenuOrPlainButton(...)` исключает `button`, `role="button"`, `role="menuitem"`, `role="option"` и контейнеры `role="menu"` / `role="listbox"` / popover, но не исключает `.__menu-item` и `[data-fill][tabindex]`. При этом `DeepResearchItemPointExpression` прямо объявляет эти два типа реальными интерактивными пунктами меню. Если ChatGPT даст connector metadata на такой строке или на её потомке, selected-state снова может принять меню за выбранную плашку.
  - Почему это важно: Это блокирует текущую задачу, потому что T-0238 именно усиливает guard против ложного selected-state перед отправкой ZIP. После r28 пакет сам документирует, что реальные строки меню ChatGPT могут быть plain `div.__menu-item` / `data-fill` + `tabindex`. Нельзя считать задачу закрытой, пока selected-state защита не отбрасывает все реально используемые интерактивные menu row типы, а не только ARIA/button-варианты.
  - Что исправить: Нужно расширить интерактивный фильтр selected-state так, чтобы он отвергал не только `button` и ARIA menu/button роли, но и `.__menu-item`, `[data-fill][tabindex]` и их потомков. Лучше вынести единое определение «интерактивной menu row ChatGPT» и использовать его и в click selector, и в selected-state validator. После этого добавить регрессионные тесты на plain row с connector metadata на самой строке и на её дочернем элементе.
  - Как проверить исправление: Добавить поведенческие tests, которые исполняют production `DeepResearchSelectedExpression` на DOM-фикстурах с:
    - `div.__menu-item[data-fill][tabindex]` рядом с prompt и прямой connector metadata;
    - `div.__menu-item[data-fill][tabindex]` с дочерним `span[data-id="connector:connector_openai_deep_research"]`;
    - таким же plain row внутри/рядом с composer menu.
    Во всех случаях результат должен быть `false`. Затем повторить focused suite по Deep Research, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check`.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2708-2715`, `isMenuOrPlainButton(...)`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2725-2741`, `DeepResearchSelectedExpression`
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2914-2969`, `interactiveSelector` и `DeepResearchItemPointExpression`
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5772-5787`, `AuditSubmitDeepResearchItemSelectorClicksPlainChatGptMenuRow`
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:8747-8751`, фикстура plain ChatGPT menu row (`class="__menu-item"`, `data-fill`, `tabindex`)
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5901-5929`, текущие отрицательные selected-state tests покрывают только `button` / `role=*`, но не `.__menu-item` / `[data-fill][tabindex]`
    - `File/symbol`: `repo-after/docs/release-management/audit-package.md:118`, документ подтверждает, что реальная строка меню может быть plain `div.__menu-item` / `data-fill` + `tabindex`
    - `Criterion`: `implementation content review`, `test coverage review`, `previous blockers closure`, `observable behavior`, `architecture coherence`
    - `Evidence`:
      - tool сам признаёт `.__menu-item` и `[data-fill][tabindex]` интерактивными пунктами меню при выборе Deep Research;
      - selected-state фильтр эти типы не исключает;
      - текущие регрессии selected-state не моделируют эти реальные интерактивные row типы.
    - `Impact`: selected-state guard остаётся неполным и может снова дать ложноположительное подтверждение включённого инструмента.
    - `Fix`: синхронизировать набор запрещённых интерактивных menu row типов между click selector и selected-state validator и добавить missing regressions.
    - `Verification`: новые negative tests на plain menu row metadata + полный focused corridor из evidence.

- B2
  - Что не так: Ключевое заявление r29 о закрытии r28 submit-loop failure доказано недостаточно строго. Сам production-код `EnableDeepResearchAsync(...)` действительно сначала ищет уже открытый пункт `Глубокое исследование`, а только потом нажимает `+`, но автоматический тест на это поведение — `AuditSubmitDeepResearchSelectionChecksOpenMenuBeforeTogglingPlus` — проверяет лишь порядок строк в исходнике через regex. Это source-level inspection, а не поведенческий тест через production code path или стабильный внутренний контракт. Для основной проблемы текущей итерации это слабое доказательство.
  - Почему это важно: Здесь задача не про косметику, а про закрытие реального pre-send failure, который уже срывал отправку. Критерий test realism прямо требует доказательства через производственный путь или стабильный внутренний контракт, а не через проверку текста файла. Пока главный сценарий r28 закрыт только source-level regex-тестом, пакет не даёт достаточно сильного машинно-проверяемого доказательства, что submit loop больше не закрывает медленно открывшееся меню повторным нажатием `+`.
  - Что исправить: Нужен поведенческий regression test на production logic `EnableDeepResearchAsync(...)` или на выделенный стабильный внутренний контракт, который действительно моделирует цикл: сначала open item уже виден, затем автоматизация должна кликнуть этот пункт и не нажимать `+` повторно. Такой тест должен считать реальные вызовы browser abstraction, а не анализировать текст метода.
  - Как проверить исправление: Построить controlled browser/client fixture или внутренний mock на уровне уже используемой browser abstraction и доказать, что при видимом open menu item:
    - `EvaluatePointAsync(...DeepResearchItemPointExpression...)` возвращает точку;
    - вызывается `ClickAtAsync` по этой точке;
    - повторный вызов `EvaluatePointAsync(...DeepResearchMenuPointExpression...)` и повторный click по `+` в этом цикле не происходят;
    - после клика selected-state становится `true`.
    Затем прогнать focused suite и стандартные verify-команды.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:929-978`, `EnableDeepResearchAsync(...)`
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5807-5817`, `AuditSubmitDeepResearchSelectionChecksOpenMenuBeforeTogglingPlus`
    - `File/symbol`: `AUDIT-MANIFEST.md`, `scopeSummary` и `metadata.blockerClosureList` заявляют, что r28 submit-loop/tooling failure закрыт именно этим изменением
    - `Criterion`: `test coverage review`, `test realism`, `previous blockers closure`, `evidence gap`
    - `Evidence`:
      - production-код содержит нужный порядок действий;
      - автоматический тест на этот claim является regex-проверкой исходника, а не behavior-level proof;
      - в `evidence/T-0238-r29/checks/audit-submit-control-followups-and-deep-research-focused-tests-r29/stdout.txt` зелёный статус `56/56` не компенсирует отсутствие реалистичного теста на самый важный сценарий r29.
    - `Impact`: закрытие r28 failure остаётся недодоказанным в обязательной части тестов и не даёт достаточной уверенности для `ACCEPT`.
    - `Fix`: заменить source-level regex-проверку на behavior-level fixture для production submit loop.
    - `Verification`: новый regression test должен падать на старой логике и проходить на текущей, после чего повторяются focused tests и verify-команды из пакета.

EVIDENCE_REVIEW:
- Проверены metadata и область пакета. `metadata.scopeTaskIds` содержит только `T-0238`; признаков скрытого `combined scope` не найдено. `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json` и фактический состав архива согласованы.
- Полнота снимков достаточна: `metadata/repo-file-snapshots.json` содержит 24 snapshot entries, все важные changed files имеют `fullContentIncluded: true`. Существенного `evidence gap` по отсутствующим implementation/test/doc snapshots не найдено.
- Проверены прошлые verdict-отчёты из `metadata.previousVerdictChain`: в пакете доступны `r01`, `r02`, `r04`, `r16`, `r18`, `r19`, `r20`, `r21`, `r24`, `r25`, `r27`. Признаков их переписывания внутри текущего архива не видно. `r28` отсутствует в chain обоснованно: saved verdict report для него не заявлен и в архив не включён.
- Проверен `metadata.blockerClosureList`. Большая часть прошлых блокеров закрыта проверяемо, но текущий пакет всё ещё неполно закрывает слой selected-state для современных plain menu row типов и недостаточно доказывает закрытие r28 submit-loop issue автоматическими тестами.
- Прочитаны изменённые implementation files, tests и документация. Проверка включала не только patch map, но и полные файлы `repo-after/`.
- Проверены raw evidence checks. Все приложенные проверки завершились успешно:
  - focused suite — `56/56`;
  - `update docs --check` — passed;
  - `verify docs` — passed;
  - `verify audit-followups` — passed, `8 actionable findings across 86 saved audit reports`;
  - `verify licenses` — passed;
  - `git diff --check` — passed.
  Эти результаты учитываются, но не снимают blocker-ы выше, потому что один core claim selected-state остаётся неполно покрыт, а другой core claim r28 closure доказан лишь source-level regex.
- Проверка секретов и локальных данных в пределах changed scope не выявила реальных токенов, приватных ключей, паролей или абсолютных локальных путей, которые были бы секретом текущего пакета.
- Текущий change не затрагивает runtime hot path игрового движка, публичный 2D API и Godot 4.7 parity surface. Производительность и Public API review для этой итерации ограничиваются sanity check и не дают отдельного blocker-а.

Техническая привязка:
- Metadata/artifacts: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`, `SHA256SUMS.txt`
- Implementation: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/eng/Electron2D.Build/Program.cs`
- Tests: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
- Documentation/rules: `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`, `repo-after/TASKS.md`
- Previous verdict files: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `r02`, `r04`, `r16`, `r18`, `r19`, `r20`, `r21`, `r24`, `r25`, `r27`
- Evidence:
  - `evidence/T-0238-r29/checks/audit-submit-control-followups-and-deep-research-focused-tests-r29/stdout.txt`
  - `evidence/T-0238-r29/checks/verify-audit-followups/stdout.txt`
  - `evidence/T-0238-r29/checks/update-docs-check/exit-code.txt`
  - `evidence/T-0238-r29/checks/verify-docs/stdout.txt`
  - `evidence/T-0238-r29/checks/verify-licenses/stdout.txt`
  - `evidence/T-0238-r29/checks/git-diff-check/exit-code.txt`

RISKS_AND_NOTES:
- FOLLOW_UP_FINDING F1
  - Идентификатор: `F1`
  - Где найдено: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:4069-4079`; связанный production path — `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:221-239`
  - Проблема: Регрессия для `--dump-dom-only` всё ещё проверяется source-level inspection по тексту метода, а не поведенческим тестом на фактическую запись diagnostic dump файлов через production path.
  - Почему не блокирует текущую задачу: Это остаётся инженерным долгом, но текущий пакет уже блокируется более прямыми и критичными проблемами B1 и B2. Порядок действий в коде виден, диагностика r26/r27 уже описана в task history, поэтому эта запись пока остаётся follow-up, а не отдельным blocker-ом.
  - Куда перенести: `Suggested existing task: T-0238`
  - Рекомендуемый приоритет: `P2`
  - Как проверить: заменить source-level test на behavior-level fixture, который вызывает production `DumpDomFromUrlAsync` через контролируемый browser/client contract и подтверждает создание `deep-research-selected-result.json` и `deep-research-selected-diagnostics.json` до deep-research-frame dependent действий.
  - Техническая привязка:
    - `FOLLOW_UP_FINDING F1`
    - `File/symbol`: `AuditSubmitDumpDomOnlyWritesBaseDomBeforeDeepResearchFrameDiagnostics`
    - `Why not blocker for current task`: текущая задача уже блокируется более прямыми defects в selected-state и test realism r28 closure
    - `Suggested existing task`: `T-0238`
    - `Suggested priority`: `P2`
    - `Verification idea`: behavior-level regression for dump-dom diagnostics ordering

- ACCEPTED_RISK R1
  - Человекочитаемое объяснение: В пакете по-прежнему явно оформлен принятый риск по semantic validation closure target. `verify audit-followups` проверяет структуру и привязку closure notes, но не доказывает семантически, что `tracked-existing` или `tracked-new` действительно указывают на существующую и подходящую задачу. Этот риск уже осознанно оформлен в `TASKS.md` и не является новым blocker-ом текущей итерации.
  - Техническая привязка:
    - `ACCEPTED_RISK R1`
    - `File/symbol`: `repo-after/TASKS.md:1894-1905`
    - `Rationale`: риск уже оформлен как `accepted-risk` с требуемыми полями risk-register модели
    - `Next decision point`: `T-0105 risk register`
    - Служебный класс: `accepted risk`

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений. Пакет `r29` содержит полноценные материалы для проверки и реально продвигает T-0238, но принять его нельзя из-за двух незакрытых пунктов в текущем scope: selected-state guard для `Глубокое исследование` всё ещё не синхронизирован с реальными plain menu row типами ChatGPT, а заявленное закрытие r28 submit-loop failure недодоказано автоматическими тестами нужного уровня реализма.
- До следующей итерации нужно:
  - закрыть B1 кодом и negative regressions на `.__menu-item` / `[data-fill][tabindex]` cases;
  - закрыть B2 behavior-level тестом для production submit loop вместо source-level regex;
  - после этого повторить focused suite, `verify audit-followups`, `verify docs`, `verify licenses` и `git diff --check`.

Техническая привязка:
- Блокируют: `B1`, `B2`
- Обязательные проверки после исправления: новые negative selected-state regressions, behavior-level regression для `EnableDeepResearchAsync(...)`, затем `evidence/T-0238-rXX/checks/audit-submit-control-followups-and-deep-research-focused-tests-*`, `verify audit-followups`, `verify docs`, `verify licenses`, `git diff --check`
- Решение относится к текущему пакету: `metadata.taskId = T-0238`, `metadata.iteration = r29`
