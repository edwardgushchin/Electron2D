VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Проверен пакет задачи `T-0238`, итерация `r20`, как полный инженерный аудит текущей области по полным снимкам файлов, а не по одному patch. Кодовая правка `ordinary submit` в `AuditSubmitCodexChromeCommand` действительно восстановлена: обычная отправка снова снимает baseline уже существующих Deep Research targets, а `--download-report-only` остаётся на пустом ignore-set пути. Документация этой ветки в целом синхронизирована с реализацией.
- Принять пакет нельзя по трём доказуемым причинам. Во-первых, tool-enforced closure для `RISKS_AND_NOTES` реализован не по критерию задачи: helper игнорирует сохранённые primary/control отчёты с результатом NEEDS_FIXES, хотя acceptance-критерии говорят о saved primary/control reports без такого фильтра. Во-вторых, закрытие blocker-а `r19` по ordinary submit доказано только source-level regex-тестом по тексту метода, а не проверкой реального поведения через производственный код или стабильный внутренний контракт. В-третьих, пакет содержит новую отдельную задачу `T-0240` и связанное обновление roadmap, хотя `metadata.scopeTaskIds` и `scopeSummary` заявляют только `T-0238 r20` про ordinary submit baseline/ignore-set.
- Проверка секретов, локальных абсолютных путей и полноты снимков блокирующих проблем не выявила: полный индекс `metadata/repo-file-snapshots.json` присутствует, declared scope files включены полными snapshots, а evidence не показывает реальных секретов или приватных ключей.

Техническая привязка:
- `metadata.taskId`: `T-0238`
- `metadata.iteration`: `r20`
- Проверенные metadata и инвентарь: `AUDIT-MANIFEST.md:3-33`, `metadata/audit-package.input.json:1-181`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0238.patch`
- Ключевая реализация ordinary submit/read-only: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:60-84`, `142-157`
- Ключевые тесты: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:3973-3998`, `4702-4861`, `5393-5395`, `5654-5690`
- Ключевая документация и критерии: `repo-after/docs/release-management/AUDIT-REQUEST.md:35-58`, `repo-after/docs/release-management/audit-package.md:89-106`, `repo-after/TASKS.md:1729-1820`, `1918-2024`
- Проверенные прошлые отчёты: `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md`, `...r02.md`, `...r04.md`, `...r16.md`, `...r18.md`, `...r19.md`
- Проверенные evidence: `evidence/T-0238-r20/checks/audit-submit-deep-research-ordinary-submit-focused-tests/*`, `verify-audit-followups/*`, `verify-docs/*`, `update-docs-check/*`, `verify-licenses/*`, `git-diff-check/*`

BLOCKERS:
- B1
  - Что не так: Реализация `verify audit-followups` проверяет closure notes только для сохранённых audit-отчётов с первой строкой ACCEPT. Это противоречит acceptance-критерию текущей задачи, где речь идёт о saved primary/control reports вообще, а не только о subset с ACCEPT. В результате helper пропускает реальные `FOLLOW_UP_FINDING` из уже сохранённых отчётов `T-0238` с NEEDS_FIXES.
  - Почему это важно: T-0238 должна дать именно tool-enforced closure actionable записей из `RISKS_AND_NOTES`. Сейчас инструмент сообщает, что actionable findings нет, хотя в сохранённых отчётах `r01`, `r02`, `r04`, `r16`, `r18` и `r19` есть `FOLLOW_UP_FINDING F1`. Значит, закрытие follow-up findings остаётся не enforced и задача не выполняет собственный контракт.
  - Что исправить: Убрать фильтр только по ACCEPT из `AuditFollowupVerifier` либо иным способом привести helper к критерию "saved primary/control reports". После этого добавить focused tests, где saved report с NEEDS_FIXES и `FOLLOW_UP_FINDING` без closure note вызывает отказ verifier-а, а после корректного closure note — проходит.
  - Как проверить исправление: Новый focused suite должен включать сценарии для saved NEEDS_FIXES reports; затем `dotnet run --project eng/Electron2D.Build -- verify audit-followups` должен находить исторические незакрытые findings до добавления closure notes и проходить только после их явного закрытия.
  - Техническая привязка:
    - `File/symbol`: `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs:61-75`; `repo-after/docs/release-management/audit-package.md:91-104`; `repo-after/TASKS.md:1789-1796`
    - `Criterion`: `task compliance review`, `implementation content review`, `previous blockers closure`
    - `Evidence`:
      - helper явно пропускает все сохранённые отчёты, если первая непустая строка не равна ACCEPT: `AuditFollowupVerifier.cs:69-73`
      - документация пакета закрепляет тот же фильтр: `audit-package.md:91`
      - acceptance-критерии задачи требуют closure для actionable `FOLLOW_UP_FINDING` из saved primary/control reports без ограничения этим фильтром: `TASKS.md:1790-1791`
      - в сохранённых прошлых отчётах есть actionable `FOLLOW_UP_FINDING F1`: `t-0238-audit-r01.md:59-67`, `t-0238-audit-r19.md:90-101`
      - evidence текущей итерации при этом сообщает ноль actionable findings: `evidence/T-0238-r20/checks/verify-audit-followups/stdout.txt`
    - `Impact`: инструмент может пропустить незакрытые follow-up findings и дать ложное ощущение полной closure coverage
    - `Fix`: сделать helper/verifier совместимым с критерием saved primary/control reports и добавить focused tests на saved NEEDS_FIXES reports
    - `Verification`: новый focused test на NEEDS_FIXES report + повторный `verify audit-followups`

- B2
  - Что не так: Закрытие blocker-а `r19` по ordinary submit подтверждено только source-level тестом, который читает C#-файл как текст и regex-ом проверяет наличие вызова `SnapshotDeepResearchTargetIdsAsync(...)` и передачу переменной в `WaitForReportAsync(...)`. Это не проверка реального поведения через производственный код и не проверка стабильного внутреннего контракта, который реально используется инструментом выполнения.
  - Почему это важно: В `scopeSummary` и `blockerClosureList` пакет утверждает, что новый focused test "доказывает" ordinary submit baseline existing targets. Фактически тест доказывает только форму текста метода. Для T-0238 это недостаточно: пользовательский контракт аудита прямо требует реалистичные тесты и запрещает считать такие surrogate-проверки достаточным доказательством заявленного поведения.
  - Что исправить: Заменить или дополнить текущий source-level test поведением через production path или через реальный внутренний контракт инструмента. Минимально нужно моделировать pre-send и post-send Deep Research targets на контролируемом production-контракте и подтверждать, что ordinary submit действительно исключает pre-send targets при выборе готового отчёта; read-only branch должен оставаться отдельной обратной гарантией.
  - Как проверить исправление: Новый behavior-level regression test должен падать на реализации с пустым ignore-set в ordinary submit и проходить только после восстановления baseline-snapshot пути. Focused evidence для `r20` должно запускать именно этот test, а не только regex по тексту метода.
  - Техническая привязка:
    - `File/symbol`: `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:3985-3998`; `AUDIT-MANIFEST.md:10`; `metadata/audit-package.input.json:10`; `metadata/audit-package.input.json:178-179`; `repo-after/docs/release-management/AUDIT-REQUEST.md:45-47`
    - `Criterion`: `test coverage review`, `realistic tests`, `previous blockers closure`
    - `Evidence`:
      - новый test `AuditSubmitOrdinarySubmitBaselinesExistingDeepResearchTargetsBeforeSend` читает файл `AuditSubmitCodexChromeCommand.cs` и проверяет текст метода regex-ом, не исполняя production behavior: `RepositoryBuildToolTests.cs:3987-3997`
      - user-facing audit contract требует проверки реального поведения через production code path или стабильный внутренний контракт: `AUDIT-REQUEST.md:45-47`
      - manifest и metadata утверждают, что этот test "proves ordinary submit snapshots existing targets and passes the ignored set": `AUDIT-MANIFEST.md:10`, `metadata/audit-package.input.json:10`
      - `blockerClosureList` тоже описывает это как RED/GREEN regression proof: `metadata/audit-package.input.json:178-179`
      - focused evidence текущей итерации запускает этот source-level test как ключевое доказательство ordinary submit branch: `evidence/T-0238-r20/checks/audit-submit-deep-research-ordinary-submit-focused-tests/command.txt`
    - `Impact`: прошлая блокирующая проблема объявлена закрытой без достаточного engineering evidence; regression в runtime-ветке может снова пройти незамеченной
    - `Fix`: добавить behavior-level test на production/internal contract ordinary submit baseline path
    - `Verification`: focused suite с новым behavior-level test, который red/green подтверждает именно runtime semantics

- B3
  - Что не так: Пакет содержит отдельную новую задачу `T-0240` и связанное изменение roadmap, хотя область пакета заявлена только как `T-0238`, а `scopeSummary` описывает только ordinary submit baseline/ignore-set и focused coverage для этого исправления.
  - Почему это важно: Контракт текущего аудита прямо требует блокировать правки вне `metadata.scopeTaskIds` или `scopeSummary`. Создание новой задачи про разделение быстрых, средних и тяжёлых проверок — это отдельная backlog-работа, не объявленная как combined scope в metadata и manifest.
  - Что исправить: Либо убрать из пакета создание `T-0240` и связанное изменение roadmap, либо честно объявить combined scope с обновлением `metadata.scopeTaskIds`, `metadata.scopeSummary`, manifest и explanation, почему эти задачи принимаются одним verdict-ом.
  - Как проверить исправление: Пересобрать пакет так, чтобы diff `TASKS.md` содержал только изменения в пределах `T-0238`, либо metadata/manifest явно включали и объясняли `T-0240` как часть combined scope.
  - Техническая привязка:
    - `File/symbol`: `metadata/audit-package.input.json:7-10`; `AUDIT-MANIFEST.md:9-10`; `repo-after/TASKS.md:1918-1987`; `repo-after/TASKS.md:2024`
    - `Criterion`: `scope scanning`, `task compliance review`
    - `Evidence`:
      - scope metadata заявляет только `T-0238`: `metadata.scopeTaskIds = ["T-0238"]`
      - `scopeSummary` описывает только закрытие `r19` ordinary submit baseline/ignore-set и focused coverage для этой ветки: `AUDIT-MANIFEST.md:10`, `metadata/audit-package.input.json:10`
      - в `TASKS.md` добавлена отдельная задача `T-0240` с собственным описанием, acceptance criteria и заметками агента: `TASKS.md:1918-1987`
      - roadmap тоже перенастроен на последовательность `T-0238 -> T-0240 -> T-0239`: `TASKS.md:2024`
      - сам audit request требует считать такие внеобластные правки блокирующими: `AUDIT-REQUEST.md:57-58`
    - `Impact`: пакет перестаёт быть чистым current-scope change и нарушает собственный scope contract
    - `Fix`: убрать `T-0240`/roadmap edits из этой итерации либо оформить combined scope
    - `Verification`: новый package без этих внеобластных правок либо metadata/manifest с явным combined scope

EVIDENCE_REVIEW:
- По реализации прочитаны полные итоговые файлы в `repo-after/` и соответствующие baseline-снимки в `repo-before/` там, где они были нужны для сравнения поведения:
  - `eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - `eng/Electron2D.Build/AuditSubmitCommand.cs`
  - `eng/Electron2D.Build/AuditPackageCommand.cs`
  - `eng/Electron2D.Build/AuditFollowupVerifier.cs`
  - `eng/Electron2D.Build/Program.cs`
  - `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - `docs/release-management/AUDIT-REQUEST.md`
  - `docs/release-management/audit-package.md`
  - `AGENTS.md`
  - `.codex/prompts/goal-task-loop.md`
  - `TASKS.md`
  - `data/documentation/electron2d-local-docs-index.json`
  - `data/documentation/local-docs-index/documentation.ndjson`
- По previous verdict chain прочитаны включённые saved reports:
  - `docs/verdicts/release-management/t-0238-audit-r01.md`
  - `docs/verdicts/release-management/t-0238-audit-r02.md`
  - `docs/verdicts/release-management/t-0238-audit-r04.md`
  - `docs/verdicts/release-management/t-0238-audit-r16.md`
  - `docs/verdicts/release-management/t-0238-audit-r18.md`
  - `docs/verdicts/release-management/t-0238-audit-r19.md`
- По metadata и инвентарю проверены:
  - `AUDIT-MANIFEST.md`
  - `metadata/audit-package.input.json`
  - `repo-file-hashes.json`
  - `metadata/repo-file-snapshots.json`
  - `T-0238.patch`
- По evidence проверены результаты заявленных локальных checks:
  - `evidence/T-0238-r20/checks/audit-submit-deep-research-ordinary-submit-focused-tests/*`
  - `evidence/T-0238-r20/checks/verify-audit-followups/*`
  - `evidence/T-0238-r20/checks/verify-docs/*`
  - `evidence/T-0238-r20/checks/update-docs-check/*`
  - `evidence/T-0238-r20/checks/verify-licenses/*`
  - `evidence/T-0238-r20/checks/git-diff-check/*`
- Полнота snapshots достаточна для проверки задачи: в `metadata/repo-file-snapshots.json` все declared files имеют `fullContentIncluded: true`. Блокирующего evidence gap по отсутствующим полным файлам реализации, тестов или документации не найдено.

Техническая привязка:
- Основной индекс снимков: `metadata/repo-file-snapshots.json`
- Состав проверяемой области и diff inventory: `AUDIT-MANIFEST.md:13-33`
- Previous chain и closure list: `metadata/audit-package.input.json:146-179`
- Ordinary submit/read-only code paths: `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:60-84`, `142-157`
- Verifier/helper logic: `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs:56-104`, `249-315`, `343-430`

RISKS_AND_NOTES:
- FOLLOW_UP_FINDING F1
  - Идентификатор: `F1`
  - Где найдено: `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs:154-208`; исторические ссылки в `repo-after/docs/verdicts/release-management/t-0238-audit-r01.md:60-67`, `...t-0238-audit-r19.md:91-101`
  - Проблема: Даже помимо blocker-а B1 verifier по-прежнему не проверяет, что `tracked-existing` и `tracked-new` действительно указывают на существующую или корректно созданную задачу. Он валидирует форму closure note, но не семантическое существование target task.
  - Почему не блокирует текущую задачу: Это реальный долг, но текущий пакет уже блокируется более базовыми дефектами: helper вообще не охватывает все saved reports, ordinary-submit closure доказан нереалистичным тестом, а пакет нарушает scope. Семантическая валидация target task остаётся следующим уровнем жёсткости после исправления этих базовых проблем.
  - Куда перенести: существующая или новая release-management задача на semantic validation closure targets
  - Рекомендуемый приоритет: `P2`
  - Как проверить: integration tests, где `tracked-existing` на несуществующую задачу падает, корректный existing target проходит, а `tracked-new` требует проверяемой записи о создании новой задачи
  - Техническая привязка:
    - Служебный класс: `follow-up finding`
    - Связанные технические имена: `verify audit-followups`, `tracked-existing`, `tracked-new`

CLOSURE_DECISION:
- Пакет `T-0238 r20` нельзя закрыть в текущем виде. Кодовая правка ordinary submit присутствует, но сама задача всё ещё не доказывает полный и проверяемый контракт: closure helper охватывает не тот набор saved reports, закрытие `r19` blocker-а по тестам подтверждено нереалистичным source-level доказательством, а пакет содержит внеобластную добавку `T-0240`.
- До следующей внешней итерации нужно: привести `verify audit-followups` к критерию saved primary/control reports, заменить ordinary-submit regression proof на behavior-level test через production/internal contract и очистить scope пакета либо честно объявить combined scope. Только после этого текущую область можно повторно оценивать на принятие.
