VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен одинарный scope по T-0233 из metadata/audit-package.input.json и AUDIT-MANIFEST.md: изменение заявлено как разграничение ролей AGENTS.md, docs/release-management/AUDIT-REQUEST.md и .codex/prompts/goal-task-loop.md, удаление ручных browser/audit-правил из prompt-а оркестратора и закрепление audit submit как обязательного пути до принятия.
- Выполнены implementation content review, test coverage review, documentation review, task compliance review, secret scanning, scope scanning.
- Изменение нельзя принять, потому что orchestration prompt всё ещё содержит browser/page-specific ручные правила, тест не закрывает этот остаточный дефект, а generated docs index включает необъяснённую внеобластную дельту по чужому verdict-файлу.

BLOCKERS:
- B1
  - File/symbol: .codex/prompts/goal-task-loop.md, раздел SUBMIT; T-0233.patch строки 48-51. Связанный критерий в docs/release-management/audit-package.md: T-0233.patch строки 165-173.
  - Criterion: implementation content review + documentation review + task compliance review. Scope summary требует удалить ручные browser/audit-правила из prompt-а оркестратора; доменный документ отдельно фиксирует, что детали браузера, режима глубокого исследования, прикрепления ZIP, обновления страницы, экспорта Markdown и закрытия вкладки принадлежат только C#-команде audit submit и самому доменному документу.
  - Evidence: в новом prompt-е всё ещё записано: «вся механика браузера, `Глубокое исследование`, прикрепление ZIP, обновление страницы, экспорт Markdown и закрытие собственной вкладки делегированы ей» и отдельно «Не открывай и не закрывай чужие вкладки; не извлекай verdict вручную из страницы, предпросмотра, истории чата или prompt examples.» (T-0233.patch строки 48-51). Одновременно доменный документ утверждает, что такие детали должны принадлежать только audit submit и самому docs/release-management/audit-package.md (T-0233.patch строки 171-173).
  - Impact: заявленное разграничение ролей не доведено до конца. Оркестраторский prompt продолжает не только ссылаться на audit submit, но и задаёт ручные browser/page ограничения и перечисляет browser workflow-детали. Это сохраняет скрытую ручную логику в том месте, откуда её как раз должны были убрать.
  - Fix: сократить раздел SUBMIT в goal-task-loop.md до вызова audit submit и высокоуровневого правила о том, что принимать задачу можно только по сохранённому полному внешнему отчёту. Убрать из prompt-а перечисление browser/page mechanics, режим глубокого исследования, прикрепление ZIP, экспорт Markdown, вкладки, чтение страницы/предпросмотра/истории чата и любые ручные browser-инструкции; оставить их только в C#-команде и доменной документации, если они там действительно нужны.
  - Verification: обновить focused test так, чтобы он падал на текущем содержимом prompt-а, затем повторить focused-documentation-tests и verify-docs на исправленном тексте.

- B2
  - File/symbol: tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs, метод AuditWorkflowDocumentsSeparateAgentRequestAndGoalPromptResponsibilities; T-0233.patch строки 186-248.
  - Criterion: test coverage review. Новый тест должен покрывать важные ветки поведения и проверять фактическое закрытие дефектов задачи.
  - Evidence: тест запрещает только часть старых маркеров (`iframe`, кнопку копирования ответа, chatgpt.com/g/, фразу про обновление страницы, старый task-id и т. п.; строки 210-247), но не запрещает те browser/page-specific правила, которые остались в текущем prompt-е и уже нарушают заявленную границу ролей: `Глубокое исследование`, прикрепление ZIP, экспорт Markdown, закрытие вкладки, ручное извлечение verdict из страницы/предпросмотра/истории чата, а также сам backend-specific submit footprint. Поэтому evidence показывает зелёный прогон тестов, хотя дефект B1 в самом prompt-е остаётся.
  - Impact: тестовое покрытие не доказывает достижение цели задачи и не защищает от регрессии ровно в той зоне, ради которой выполнялась правка. При текущем наборе ассершенов можно получить зелёные тесты и всё ещё сохранить запрещённые manual browser rules.
  - Fix: расширить тест так, чтобы он фиксировал именно допустимый контракт goal-task-loop.md, а не только отсутствие части устаревших формулировок. Минимум — добавить отрицательные проверки на оставшиеся browser/page-specific инструкции и/или задать позитивный whitelist допустимого содержания раздела SUBMIT.
  - Verification: запустить тот же focused-documentation-tests после усиления теста; тест должен воспроизводимо падать на текущей версии prompt-а и проходить только после фактического удаления остаточных ручных browser/page правил.

- B3
  - File/symbol: data/documentation/electron2d-local-docs-index.json; T-0233.patch строки 126-140. Связанные scope-артефакты: metadata/audit-package.input.json строки 7-18, AUDIT-MANIFEST.md строки 9-21.
  - Criterion: scope scanning. Если в diff есть изменения вне metadata.scopeTaskIds или scopeSummary, это blocker даже при успешных проверках.
  - Evidence: diff по generated docs index обновляет hash не только для двух реально редактируемых доменных документов, но и для чужого файла docs/verdicts/release-management/t-0001-audit-r33.md: hash меняется с 3fe454... на 38a246... (T-0233.patch строки 129-131), а sourceDigest тоже меняется (строки 136-140). При этом scopeSummary в metadata и manifest описывает только разграничение ролей AGENTS.md, AUDIT-REQUEST.md и goal-task-loop.md, без combined scope, без previous verdict chain и без какого-либо объяснения, почему в пакет попала дельта по старому verdict-файлу.
  - Impact: пакет содержит внеобластную изменённую информацию в repository-owned артефакте. Даже если это побочный эффект генерации локального индекса, scope перестаёт быть чистым и доказуемо ограниченным заявленной задачей. Внешний аудитор не получает объяснения, почему прошлый verdict-файл участвует в diff.
  - Fix: пересобрать data/documentation/electron2d-local-docs-index.json из clean tree, где присутствуют только in-scope изменения, либо явно расширить и обосновать scope так, чтобы дополнительные затронутые источники были перечислены и доступны для проверки.
  - Verification: в новом diff docs index должен менять только записи, относящиеся к фактически изменённым in-scope документам, после чего нужно повторить verify-docs и перепроверить согласованность AUDIT-MANIFEST.md и metadata/audit-package.input.json со фактическим diff.

EVIDENCE_REVIEW:
- Проверены AUDIT-MANIFEST.md и metadata/audit-package.input.json: taskId T-0233, одиночный scopeTaskIds, scopeSummary, список изменённых файлов, список checks, отсутствие previousVerdictChain и blockerClosureList.
- Проверен repo-file-hashes.json: в scope включены только .codex/prompts/goal-task-loop.md, AGENTS.md, TASKS.md, data/documentation/electron2d-local-docs-index.json, docs/release-management/AUDIT-REQUEST.md, docs/release-management/audit-package.md, tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs.
- Полностью прочитан T-0233.patch как основной материал implementation/doc/task/scope review.
- Проверены изменения по содержанию:
  - .codex/prompts/goal-task-loop.md — полная замена краткого оркестраторского prompt-а;
  - AGENTS.md — секция External Audit Packages;
  - docs/release-management/AUDIT-REQUEST.md — корректировка границы внешнего аудита;
  - docs/release-management/audit-package.md — новый раздел о границах правил и запросов;
  - tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs — новый focused test;
  - TASKS.md — перевод задачи в in progress и запись в дневнике;
  - data/documentation/electron2d-local-docs-index.json — обновлённые sha256/sourceDigest.
- Проверены evidence-артефакты по всем пяти checks:
  - focused-documentation-tests: exit code 0, stdout показывает прогон 4 тестов;
  - verify-docs: exit code 0;
  - verify-line-endings: exit code 0;
  - verify-source-license-headers: exit code 0;
  - git-diff-check: exit code 0, при этом в stderr есть только предупреждения Git о будущей нормализации CRLF/LF для TASKS.md и docs/release-management/audit-package.md, без whitespace errors.
- Выполнен secret scanning по patch, manifest, metadata и evidence: реальных секретов, токенов, приватных ключей, паролей, локальных абсолютных путей и конфиденциальных данных не обнаружено. В evidence/stdout есть лишь placeholder `<repo-root>`, а cwd равен `.`.

RISKS_AND_NOTES:
- previous verdict chain и blocker closure list в metadata пусты, поэтому отдельная проверка verbatim preservation и previous blockers closure в этой итерации не требовалась.
- Архив не содержит отдельных previous verdict files в scope, и это не является blocker-ом само по себе, поскольку цепочка предыдущих verdict-ов не заявлена.
- Основной результат аудита основан на patch и archive evidence, что соответствует тексту AUDIT-REQUEST.md: patch рассматривается как материал для чтения изменения, а отсутствующие дополнительные материалы не считаются blocker-ом без доказуемого влияния.
- Помимо перечисленных blocker-ов, иных подтверждённых секретов или новых source-level дефектов по предоставленному scope не найдено.

CLOSURE_DECISION:
- Задача остаётся открытой. Чтобы её можно было закрыть, нужно: полностью убрать остаточные ручные browser/page правила из goal-task-loop.md, усилить focused test так, чтобы он ловил именно этот класс нарушений, и очистить generated docs index от внеобластной дельты либо явно переобъявить и обосновать расширенный scope. Только после этого пакет можно считать соответствующим заявленной области и контракту задачи.
