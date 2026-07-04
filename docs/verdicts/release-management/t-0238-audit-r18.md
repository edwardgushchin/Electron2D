VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен полный пакет `T-0238` итерации `r18` по полным снимкам файлов, а не по одному patch: прочитаны `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, изменённые файлы в `repo-after/`, доступные baseline-снимки в `repo-before/`, `T-0238.patch`, приложенные raw-evidence checks и прошлые verdict-файлы из `metadata.previousVerdictChain`.
- Пакет в целом закрывает прошлые блокеры `r01`, `r02`, `r04` и `r16`: scope leak убран, clean control-audit guard ужесточён, `--new-conversation` теперь действительно запрещает любой явный `--project-url`, а focused tests на этот контракт добавлены.
- Но текущее изменение `r18` нельзя принять из-за дефекта именно в заявленном исправлении Deep Research selection flow. Код повторно проверяет выбранную плашку перед отправкой, но сам `FillPromptExpression` всё ещё может удалить корректно выбранную плашку в одном из уже поддержанных вариантов разметки и тем самым сорвать штатную отправку.
- Тесты и приложенные evidence этот риск не закрывают: они подтверждают порядок вызовов и часть selector-веток, но не проверяют проблемный путь “prompt fill сохраняет выбранную Deep Research pill после вставки текста”.

- Техническая привязка:
  - `metadata.taskId`: `T-0238`
  - `metadata.iteration`: `r18`
  - `metadata.scopeTaskIds`: `["T-0238"]`
  - `metadata.scopeSummary`: `metadata/audit-package.input.json`
  - Проверенные ключевые файлы реализации:  
    `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`  
    `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`  
    `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`  
    `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`  
    `repo-after/eng/Electron2D.Build/Program.cs`
  - Проверенные тесты и документы:  
    `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`  
    `repo-after/docs/release-management/AUDIT-REQUEST.md`  
    `repo-after/docs/release-management/audit-package.md`  
    `repo-after/.codex/prompts/goal-task-loop.md`  
    `repo-after/AGENTS.md`  
    `repo-after/TASKS.md`
  - Проверенные прошлые verdict-файлы:  
    `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`  
    `repo-after/docs/verdicts/release-management/t-0238-audit-r02.md`  
    `repo-after/docs/verdicts/release-management/t-0238-audit-r04.md`  
    `repo-after/docs/verdicts/release-management/t-0238-audit-r16.md`

BLOCKERS:
- B1
  - Что не так: В `FillPromptExpression` логика сохранения выбранной Deep Research плашки неполная и не совпадает с новой логикой определения выбранного режима. `DeepResearchSelectedExpression` считает корректной выбранную плашку с English metadata `data-keyword="Deep Research"` и `data-inline-selection-pill`, но `FillPromptExpression` этот вариант не распознаёт. Для contenteditable prompt это переводит код в fallback-ветку с `element.textContent = ''`, которая удаляет дочерние узлы prompt-а перед вставкой текста. В результате валидная выбранная плашка может быть уничтожена самой вставкой prompt-а, после чего команда упадёт на повторной проверке `RequireDeepResearchSelectedAsync`.
  - Почему это важно: Текущая итерация `r18` заявлена как исправление дефекта `r17`, где режим `Глубокое исследование` не должен silently теряться между выбором инструмента и отправкой. Сейчас повторная проверка перед отправкой добавлена, но один из уже поддержанных вариантов выбранной плашки всё ещё разрушается самим путём `FillPromptAsync`. Это означает, что контракт “prompt insertion cannot silently remove the selected tool” выполнен не полностью.
  - Что исправить: Нужно унифицировать селектор/canonical detection выбранной плашки между `DeepResearchSelectedExpression` и `FillPromptExpression`, чтобы путь вставки prompt-а сохранял все поддержанные варианты выбранного Deep Research pill, включая English metadata variant. Надёжный вариант — переиспользовать тот же набор selector-ов, что и в `DeepResearchSelectedExpression`, вместо отдельного усечённого selector-а.
  - Как проверить исправление: Добавить focused regression test, который исполняет production `FillPromptExpression` на DOM-фикстуре с contenteditable prompt и вложенной выбранной плашкой вида `[data-keyword="Deep Research"][data-inline-selection-pill]`; после заполнения prompt-а `DeepResearchSelectedExpression` должен оставаться `true`, а дочерняя selection pill не должна исчезать.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:2646-2652`, `2667-2672`, `2801-2823`
    - `Criterion`: `implementation content review`, `task compliance review`, `backend path`, `observable behavior`
    - `Evidence`:
      - `DeepResearchSelectedExpression` явно поддерживает English selected-pill selector `[data-keyword="Deep Research"][data-inline-selection-pill]` (`2646-2652`, `2667-2672`)
      - `FillPromptExpression` проверяет только `[data-id="connector:connector_openai_deep_research"]`, `[data-system-hint-type="connector:connector_openai_deep_research"]` и русский `[data-keyword="Глубокое исследование"][data-inline-selection-pill]`, но не English keyword variant (`2801`)
      - fallback-ветка `FillPromptExpression` очищает prompt через `element.textContent = ''` перед вставкой текста (`2821-2823`)
      - acceptance criterion задачи требует, чтобы `audit submit` повторно проверял отдельную выбранную плашку после заполнения prompt-а (`repo-after/TASKS.md:1804`)
    - `Impact`: один из поддержанных вариантов выбранной плашки может теряться именно на шаге, который `r18` должна была стабилизировать
    - `Fix`: унифицировать selectors для сохранения selection pill в `FillPromptExpression` и добавить regression coverage на in-prompt selected-pill variants
    - `Verification`: focused DOM-fixture test на `FillPromptExpression` + `DeepResearchSelectedExpression` для English keyword pill; затем повторить приложенный focused test suite

- B2
  - Что не так: Focused tests и приложенные evidence не покрывают проблемную ветку `FillPromptExpression`. В пакете есть тесты на selection by connector metadata, поиск sibling pill, reject menu item/plain text и source-level порядок `FillPromptAsync -> RequireDeepResearchSelectedAsync -> ClickSendAsync`, но нет теста, который реально исполняет production `FillPromptExpression` и доказывает, что вставка prompt-а сохраняет выбранную Deep Research плашку во всех поддержанных вариантах разметки.
  - Почему это важно: Для `r18` главное обещание не в одном лишь наличии pre-send guard, а в том, что prompt insertion больше не сможет “тихо” убрать выбранный режим до отправки. Без прямого теста на сам `FillPromptExpression` пакет не доказывает ключевую ветку текущего исправления и не защищает от уже существующего дефекта из B1.
  - Что исправить: Добавить focused behavioral tests на production JavaScript для prompt fill. Минимальный набор:  
    1) contenteditable prompt с вложенной выбранной плашкой;  
    2) English metadata variant `[data-keyword="Deep Research"][data-inline-selection-pill]`;  
    3) проверка, что после `FillPromptExpression` selected-state остаётся `true`, а pill не удаляется.
  - Как проверить исправление: Расширить focused test filter и evidence so that it includes new prompt-fill preservation tests, затем снова приложить `stdout.txt`/`exit-code.txt` для focused suite и убедиться, что пакетный набор checks остаётся зелёным.
  - Техническая привязка:
    - `File/symbol`: `metadata/audit-package.input.json:32-49`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:5595-5648`, `5377-5380`
    - `Criterion`: `test coverage review`, `realistic tests`, `task compliance review`
    - `Evidence`:
      - focused filter в `metadata/audit-package.input.json` запускает только selector/order/new-conversation tests и не включает ни одного test case для `FillPromptExpression`
      - `RepositoryBuildToolTests.AuditSubmitCodexChromeClicksDeepResearchTool` проверяет только source-level порядок вызовов, а не сохранение selected pill после реальной вставки текста (`5377-5380`)
      - отдельные DOM-fixture tests покрывают item selector и `DeepResearchSelectedExpression`, но не `FillPromptExpression` (`5595-5648`)
      - приложенный evidence `evidence/T-0238-r18/checks/audit-submit-deep-research-selection-focused-tests/*` подтверждает прохождение именно этого узкого списка из 13 тестов, а не проблемной ветки prompt fill
    - `Impact`: главный regression surface `r18` остаётся недоказанным и уже содержит реальный дефект
    - `Fix`: добавить focused tests, исполняющие production `FillPromptExpression` на поддержанных selected-pill shapes
    - `Verification`: обновлённый focused filter должен явно включать новые prompt-fill preservation tests и зелёно проходить в evidence

EVIDENCE_REVIEW:
- Проверены metadata и область пакета: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`.
- Проверена полнота snapshots: для всех заявленных changed files есть полные `repo-after/` снимки, а для baseline-файлов — соответствующие `repo-before/` версии. Недостатка доказательств вида `patch-only inspection` не обнаружено.
- Проверены ключевые файлы реализации:
  - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`
  - `repo-after/eng/Electron2D.Build/Program.cs`
- Проверены ключевые тесты и документация:
  - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - `repo-after/docs/release-management/AUDIT-REQUEST.md`
  - `repo-after/docs/release-management/audit-package.md`
  - `repo-after/.codex/prompts/goal-task-loop.md`
  - `repo-after/AGENTS.md`
  - `repo-after/TASKS.md`
- Проверены прошлые verdict-файлы из `metadata.previousVerdictChain`: `r01`, `r02`, `r04`, `r16`. Их blocker-ы по scope leak, actionable note coverage, clean control-audit guard и `--new-conversation` project-root contract в текущем пакете в целом действительно закрыты.
- Проверены raw-evidence checks:
  - `evidence/T-0238-r18/checks/audit-submit-deep-research-selection-focused-tests/*`
  - `evidence/T-0238-r18/checks/update-docs-check/*`
  - `evidence/T-0238-r18/checks/verify-docs/*`
  - `evidence/T-0238-r18/checks/verify-audit-followups/*`
  - `evidence/T-0238-r18/checks/verify-licenses/*`
  - `evidence/T-0238-r18/checks/git-diff-check/*`
- По secret scanning и scope scanning новых блокирующих проблем не найдено: в текущем scope нет реальных секретов, приватных ключей, токенов, паролей или недопустимых локальных абсолютных путей; лишние repo-файлы вне заявленного scope в архив не попали.

- Техническая привязка:
  - Metadata/inventory: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`
  - Реализация: `repo-after/eng/Electron2D.Build/*.cs`
  - Тесты: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - Документация: `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/AGENTS.md`, `repo-after/.codex/prompts/goal-task-loop.md`, `repo-after/TASKS.md`
  - Previous verdict files: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `...r02.md`, `...r04.md`, `...r16.md`
  - Evidence artifacts: `evidence/T-0238-r18/checks/*`

RISKS_AND_NOTES:
- FOLLOW_UP_FINDING F1
  - Идентификатор: `F1`
  - Где найдено: `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, исторические ссылки в `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `...r02.md`, `...r04.md`, `...r16.md`
  - Проблема: Исторический технический долг сохраняется: `verify audit-followups` проверяет форму closure note, но не подтверждает, что `tracked-existing`/`tracked-new` действительно указывают на реальную существующую или созданную задачу.
  - Почему не блокирует текущую задачу: Это замечание уже несколько итераций подряд классифицировалось как неблокирующий долг. Текущий отказ связан не с ним, а с незакрытым defect path в `r18` вокруг сохранения выбранной Deep Research pill при заполнении prompt-а.
  - Куда перенести: существующая или новая release-management задача на semantic validation closure targets
  - Рекомендуемый приоритет: `P2`
  - Как проверить: добавить integration tests, где `tracked-existing` на несуществующую задачу падает, корректный existing target проходит, а `tracked-new` требует проверяемой записи о создании новой задачи
  - Техническая привязка:
    - Служебный класс: `follow-up finding`
    - Связанные технические имена: `verify audit-followups`, `tracked-existing`, `tracked-new`

- INFO_NOTE I1
  - Проблема: Пакет содержит достаточные полные снимки файлов и доступные previous verdict files для проверки текущей области; существенного `evidence gap` по main implementation/test/doc scope не обнаружено.
  - Почему не блокирует текущую задачу: Это нейтральная фиксация состояния доказательств. Отказ вызван содержательной проблемой реализации и тестового покрытия, а не нехваткой материалов.
  - Техническая привязка:
    - Служебный класс: `info note`

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений. Пакет уже закрывает предыдущие блокеры по `--new-conversation`, clean control-audit guard и history chain, но текущая `r18` не доводит до конца собственный Deep Research submit path: вставка prompt-а всё ещё может уничтожить валидную выбранную плашку в одном из поддержанных вариантов разметки, а focused tests этого не ловят.
- Сначала нужно исправить `FillPromptExpression`, затем добавить прямые regression tests на сохранение selected pill после вставки текста и только после этого повторно собирать и отправлять новую итерацию.
