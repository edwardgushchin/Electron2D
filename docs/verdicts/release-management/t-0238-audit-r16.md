VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен полный пакет `T-0238` итерации `r16` по полным снимкам файлов, а не по одному patch: прочитаны `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, все изменённые файлы в `repo-after/`, доступные baseline-версии в `repo-before/`, `T-0238.patch`, прошлые verdict-файлы из `metadata.previousVerdictChain` и приложенные raw-evidence checks.
- По предыдущим замечаниям картина в целом хорошая: старые blocker-ы из `r01`, `r02` и `r04` действительно закрыты в текущем коде и тестах, clean control-audit guard выглядит жёстче и согласованнее, документация синхронизирована, полнота snapshots достаточна, явных секретов, токенов, приватных ключей и локальных абсолютных путей в проверяемом scope не найдено.
- Но текущее заявленное закрытие `r15 old-chat Deep Research limit` не доказано до конца. Основная проблема не в упаковке, а в самом изменении: `--new-conversation` разрешает обход state gate, но не обеспечивает заявленный контракт «всегда открыть корневой URL проекта и создать новый чат проекта». Из-за этого пакет нельзя принять как завершённую реализацию заявленной области `r16`.
- Дополнительно тестовое покрытие не доказывает ключевую ветку поведения, ради которой и была сделана `r16`: focused tests подтверждают только bypass state gate и запрет concrete conversation URL, но не защищают фактическое открытие project root и не ловят URL-bypass на не-conversation адресах.

- Техническая привязка:
  - `metadata.taskId`: `T-0238`
  - `metadata.iteration`: `r16`
  - `metadata.scopeTaskIds`: `["T-0238"]`
  - `metadata.scopeSummary`: `metadata/audit-package.input.json:7-10`
  - Проверенные metadata и инвентарь: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`
  - Проверенные прошлые verdict-файлы из `metadata.previousVerdictChain`: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0238-audit-r02.md`, `repo-after/docs/verdicts/release-management/t-0238-audit-r04.md`
  - Проверенные ключевые файлы реализации и контрактов: `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`, `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`, `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`, `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`, `repo-after/eng/Electron2D.Build/Program.cs`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`, `repo-after/docs/release-management/AUDIT-REQUEST.md`, `repo-after/docs/release-management/audit-package.md`, `repo-after/.codex/prompts/goal-task-loop.md`, `repo-after/AGENTS.md`, `repo-after/TASKS.md`
  - Проверенные raw-evidence checks: `evidence/T-0238-r16/checks/audit-submit-new-conversation-focused-tests/*`, `update-docs-check/*`, `verify-docs/*`, `verify-audit-followups/*`, `verify-licenses/*`, `git-diff-check/*`

BLOCKERS:
- B1
  - Что не так: Флаг `--new-conversation` реализован только как разрешение не падать на state gate после прошлого `VERDICT: NEEDS_FIXES`, но не как жёсткий путь «открыть корневой URL проекта». Код сохраняет любой явно переданный `--project-url`, если он не является concrete conversation URL. Это значит, что с `--new-conversation` можно запустить отправку не только с project root, но и с любым другим не-conversation URL, включая корень `chatgpt.com` или вообще посторонний адрес.
  - Почему это важно: Текущая `r16` заявлена именно как исправление дефекта `r15`, где нужен поддержанный путь для нового project chat. Пока инструмент не принуждает project root, он не гарантирует открытие правильного нового чата проекта и не выполняет собственный контракт. Это уже не мелкое расхождение формулировок, а недоведённый `backend path` для основной функции итерации.
  - Что исправить: Сделать `--new-conversation` структурно жёстким. Либо полностью запретить явный `--project-url` с этим флагом, либо разрешать только точный project-root URL и принудительно подставлять его в submit path. Отдельно нужно закрыть bypass на `https://chatgpt.com/`, на URL другого проекта и на посторонние хосты.
  - Как проверить исправление: Добавить focused tests, которые доказывают одно из двух корректных поведений: либо `--new-conversation` всегда использует точный project root независимо от ввода, либо команда отклоняет любой явный `--project-url`, кроме допустимого project-root URL. После этого повторно прогнать focused tests и приложить raw evidence.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:87-91`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:231-260`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:458-460`, `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:1135-1157`
    - `Criterion`: `task compliance review`, `documentation review`, `implementation content review`, `backend path`, `observable behavior`
    - `Evidence`:
      - `metadata/audit-package.input.json:10` — scopeSummary обещает, что флаг «opens the project root»
      - `repo-after/docs/release-management/audit-package.md:116`, `repo-after/docs/release-management/audit-package.md:575` — документация утверждает, что `--new-conversation` открывает корневой URL проекта
      - `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:231-260` — при `--new-conversation` код отклоняет только concrete conversation URL, но оставляет любой другой явно переданный `projectUrl`
      - `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:87-91` — в браузерную автоматизацию уходят `submitOptions` без нормализации project URL для `--new-conversation`
      - `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs:458-460` — `NewConversation` сейчас используется только как bypass state gate
    - `Impact`: заявленное исправление `r15` не гарантирует открытие нового чата нужного проекта; контракт `--new-conversation` остаётся частично фиктивным
    - `Fix`: нормализовать/зафиксировать project root для `--new-conversation` и запретить URL-bypass
    - `Verification`: focused tests на `--new-conversation` с `https://chatgpt.com/`, URL другого проекта, посторонним URL и корректным project root; все неподходящие входы должны падать до браузера либо принудительно сводиться к project root

- B2
  - Что не так: Тесты и приложенные evidence не доказывают ключевую ветку поведения `r16`. В focused suite есть только два теста на `--new-conversation`: один проверяет, что после прошлого `NEEDS_FIXES` команда больше не падает на `CONVERSATION-REQUIRED`, второй — что concrete conversation URL отклоняется. Но нет теста на главное обещание текущей итерации: что флаг действительно ведёт в project root и не допускает non-conversation URL-bypass. Нет и теста, который бы реально фиксировал рассчитанный URL submit path.
  - Почему это важно: В этой задаче regression history уже длинная, и `r16` закрывает именно повторяемый браузерный сбой. Если тесты не покрывают главный контракт новой ветки, пакет не даёт достаточного доказательства, что проблема действительно закрыта, а не просто перенесена в соседний входной случай.
  - Что исправить: Добавить focused regression tests на реальное вычисление submit URL для `--new-conversation` и на запрет/нормализацию non-conversation URL. Лучше всего вынести выбор URL в отдельную проверяемую функцию или протестировать сбор `submitOptions`, а не только ранний выход на missing pipe. Дополнительно стоит зафиксировать несовместимость с другими режимами отдельными тестами, а не оставлять её только в парсере без доказательств.
  - Как проверить исправление: Обновить focused test-filter для `r16`, чтобы он включал тесты на project-root behavior и URL-bypass cases, затем приложить новый `stdout.txt`/`exit-code.txt` для этого набора проверок.
  - Техническая привязка:
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:3014-3038`, `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:3391-3408`, `metadata/audit-package.input.json:31-48`
    - `Criterion`: `test coverage review`, `realistic tests`, `task compliance review`
    - `Evidence`:
      - `RepositoryBuildToolTests.AuditSubmitPrimaryIterationAfterNeedsFixesAllowsExplicitNewConversationBeforeBrowserLaunch` доказывает только bypass state gate и ранний выход на `E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-UNAVAILABLE`
      - `RepositoryBuildToolTests.AuditSubmitNewConversationRejectsConversationUrlBeforeBrowserLaunch` покрывает только concrete conversation URL
      - `metadata/audit-package.input.json:33-48` — весь focused evidence для `r16` ограничен шестью тестами, среди которых нет проверки project-root behavior и нет проверки non-conversation URL-bypass
    - `Impact`: основной новый контракт `r16` остаётся недоказанным; текущая green evidence не ловит реальный дефект из B1
    - `Fix`: добавить focused tests на project-root forcing/rejection и обновить evidence
    - `Verification`: новый focused filter должен явно включать кейсы `--new-conversation + foreign URL`, `--new-conversation + chatgpt root`, `--new-conversation + correct project root`, а также проверку фактического URL, уходящего в submit path

EVIDENCE_REVIEW:
- Прочитаны и сопоставлены scope и история задачи: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`.
- Прочитаны прошлые verdict-файлы из `metadata.previousVerdictChain`. Blocker-ы `r01`, `r02` и `r04` сопоставлены с `metadata.blockerClosureList`; их старые замечания по scope leak, actionable notes и clean control-audit guard в текущем коде выглядят закрытыми.
- Прочитаны полные итоговые версии changed files в `repo-after/` и доступные baseline-версии в `repo-before/`. Для самой проблемы `r16` ключевыми были: `AuditSubmitCommand.cs`, `AuditSubmitCodexChromeCommand.cs`, `RepositoryBuildToolTests.cs`, `audit-package.md`, `.codex/prompts/goal-task-loop.md`, `AGENTS.md`, `TASKS.md`.
- Проверка полноты snapshots достаточна: `metadata/repo-file-snapshots.json` содержит full snapshots для declared scope; patch использовался только как карта изменений, а не как замена чтению файлов.
- По secret scanning и scope scanning новых блокирующих проблем не найдено: в коде, patch и evidence нет реальных секретов, токенов, приватных ключей, паролей или недопустимых локальных абсолютных путей; лишних repo-файлов вне allowlist в текущем архиве не обнаружено.
- Raw evidence показывает успешное прохождение приложенных локальных checks, но именно по `r16` они покрывают только узкий набор focused tests и не компенсируют пробел в главной ветке поведения `--new-conversation`.

- Техническая привязка:
  - Metadata и manifest: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`
  - Реализация:
    - `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`
    - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
    - `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
    - `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`
    - `repo-after/eng/Electron2D.Build/Program.cs`
  - Тесты:
    - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - Документация и правила:
    - `repo-after/docs/release-management/AUDIT-REQUEST.md`
    - `repo-after/docs/release-management/audit-package.md`
    - `repo-after/.codex/prompts/goal-task-loop.md`
    - `repo-after/AGENTS.md`
    - `repo-after/TASKS.md`
  - Previous verdict files:
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r02.md`
    - `repo-after/docs/verdicts/release-management/t-0238-audit-r04.md`
  - Raw evidence:
    - `evidence/T-0238-r16/checks/audit-submit-new-conversation-focused-tests/*`
    - `evidence/T-0238-r16/checks/update-docs-check/*`
    - `evidence/T-0238-r16/checks/verify-docs/*`
    - `evidence/T-0238-r16/checks/verify-audit-followups/*`
    - `evidence/T-0238-r16/checks/verify-licenses/*`
    - `evidence/T-0238-r16/checks/git-diff-check/*`

RISKS_AND_NOTES:
- FOLLOW_UP_FINDING F1
  - Идентификатор: `F1`
  - Где найдено: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md:60-67`, `repo-after/docs/verdicts/release-management/t-0238-audit-r02.md:58-65`, `repo-after/docs/verdicts/release-management/t-0238-audit-r04.md:71-75`, текущая реализация `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`
  - Проблема: Исторический долг из прошлых verdict-отчётов остаётся открыт: `verify audit-followups` по-прежнему проверяет форму closure note, но не подтверждает, что `tracked-existing` или `tracked-new` действительно ссылаются на реальную существующую или созданную задачу.
  - Почему не блокирует текущую задачу: Это замечание уже несколько раз было явно классифицировано как неблокирующий долг. Текущий отказ связан не с ним, а с тем, что `r16` не довела собственный новый submit-path `--new-conversation` до заявленного и доказанного поведения.
  - Куда перенести: новая или существующая release-management задача на semantic validation closure targets для `verify audit-followups`
  - Рекомендуемый приоритет: `P2`
  - Как проверить: добавить integration tests, где `tracked-existing` на несуществующий task id падает, корректный existing target проходит, а `tracked-new` требует детерминированного tracked creation record
  - Техническая привязка:
    - Служебный класс: `follow-up finding`
    - Источник замечания: сохранённые previous verdict files `r01`, `r02`, `r04`
    - Связанные технические имена: `verify audit-followups`, `tracked-existing`, `tracked-new`

CLOSURE_DECISION:
- Задача остаётся открытой до исправлений, потому что текущий пакет не доказывает главное обещание `r16`: `--new-conversation` пока не является жёстким и проверяемым путём «открыть новый chat именно из project root». Сначала нужно исправить сам URL-resolution/backend path, затем закрыть пробел focused tests на эту ветку и только после этого повторно собирать и отправлять новую итерацию.
