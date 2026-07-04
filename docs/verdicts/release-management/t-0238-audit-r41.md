VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:
- Я проверил архив как полную инженерную итерацию `T-0238 r41`, а не только как проверку упаковки. Прочитаны metadata, manifest, полные снимки `repo-after/` и `repo-before/`, изменённый код, тесты, доменная документация, `TASKS.md`, сохранённые прошлые verdict-файлы из `metadata.previousVerdictChain` и evidence configured checks.
- Пакет в целом хорошо собран: область согласована как одиночная задача `T-0238`, прошлые отчёты доступны, `metadata.blockerClosureList` подробно описывает закрытие прошлых blocker-ов, evidence-проверки зелёные, а предыдущий blocker r40 действительно попытались закрыть production guard-ом перед отправкой.
- Принять изменение нельзя, потому что r41 не доводит до конца именно заявленное закрытие r40 B1. Новый pre-send guard проверяет не «ровно одну видимую плашку основного audit ZIP», как обещано в документации и scope summary, а любой небольшой видимый элемент рядом с prompt, если в его тексте встречается имя ZIP-файла. Это допускает ложноположительное состояние без реальной attachment chip и оставляет текущий submit-path недоказанным.
- Дополнительно я не нашёл новых доказуемых scope leak-ов, реальных секретов, локальных абсолютных путей или пропущенных критичных snapshot-файлов в пределах текущей области. Основная проблема одна, но она блокирующая.

Техническая привязка:
- `metadata.taskId`: `T-0238`
- `metadata.iteration`: `r41`
- `metadata.scopeTaskIds`: `["T-0238"]`
- `metadata.scopeSummary`: закрытие saved primary `r40 NEEDS_FIXES`, включая поздний повторный readback composer payload перед `ClickSendAsync`
- Проверенные ключевые файлы:
  - `AUDIT-MANIFEST.md`
  - `metadata/audit-package.input.json`
  - `metadata/repo-file-snapshots.json`
  - `repo-file-hashes.json`
  - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`
  - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
  - `repo-after/docs/release-management/AUDIT-REQUEST.md`
  - `repo-after/docs/release-management/audit-package.md`
  - `repo-after/AGENTS.md`
  - `repo-after/.codex/prompts/goal-task-loop.md`
  - `repo-after/TASKS.md`
  - сохранённые verdict-файлы по цепочке `r01`, `r02`, `r04`, `r16`, `r18`, `r19`, `r20`, `r21`, `r24`, `r25`, `r27`, `r29`, `r31`, `r32`, `r33`, `r36`, `r40`
- Проверенные классы проверки:
  - `implementation content review`
  - `test coverage review`
  - `documentation review`
  - `task compliance review`
  - `secret scanning`
  - `scope scanning`
  - `previous verdict files`
  - `verbatim preservation`
  - `previous blockers closure`
  - `full file review`

BLOCKERS:
- B1
  - Что не так: новый guard `RequirePromptPayloadReadyAsync` действительно появился перед отправкой, но его production DOM-выражение не проверяет, что рядом с полем ввода осталась именно attachment chip основного audit ZIP. Оно принимает любой видимый небольшой `button`, `div`, `span`, `li`, элемент с `aria-label`, `title` или `data-testid`, если рядом с prompt встречается текст имени ZIP-файла. То есть доказательство «плашка audit ZIP сохранилась» подменено более слабым условием «возле prompt есть какой-то элемент с таким текстом».
  - Почему это важно: это не закрывает r40 B1 по существу. Задача требовала доказать сохранение полного payload-а перед `ClickSendAsync`: текста сообщения, выбранного `Глубокое исследование` и ровно одной видимой плашки основного ZIP. При текущей логике команда может считать payload готовым даже тогда, когда реальная attachment chip исчезла, а рядом осталась только обычная текстовая метка, tooltip, helper label или другой неattachment элемент с тем же именем файла.
  - Что исправить: нужно сузить проверку до реальной semantics attachment chip, а не до произвольного текста с именем ZIP. Приемлемый вариант — требовать стабильные признаки attachment-элемента composer-а и/или компактной chip-формы, совместимой с фактическим UI, при этом по-прежнему отсекая историю сообщений. Одновременно нужен регрессионный тест, который исполняет production `PromptPayloadReadyExpression` и доказывает отрицательный случай: plain nearby label с именем ZIP не должен считаться валидной attachment chip.
  - Как проверить исправление: добавить минимум один realistic regression на production DOM expression с двумя негативными случаями и одним позитивным:
    - рядом с prompt лежит обычный `div`/`span` с текстом `T-0238-audit-r41.zip`, но без semantics attachment chip — результат должен быть `false`;
    - ZIP-текст находится в истории сообщений — результат должен быть `false`;
    - рядом с prompt есть реальная attachment chip текущей отправки — результат должен быть `true`.
    После этого заново прогнать focused `AuditSubmit` suite, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check`.
  - Техническая привязка:
    - `File/symbol`:
      - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1312-1330` — `RequirePromptPayloadReadyAsync`
      - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:3576-3644` — `PromptPayloadReadyExpression`
      - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs:1228-1243` — `SubmitPromptAsync`
      - `repo-after/docs/release-management/audit-package.md:123-125`
      - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:6109-6119`
      - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:9093-9119`
    - `Criterion`:
      - `implementation content review`
      - `test coverage review`
      - `task compliance review`
      - `previous blockers closure`
      - `observable behavior`
      - `realistic tests`
    - `Evidence`:
      - Production selector берёт кандидатов из очень широкого набора: `button,[role="button"],div,span,li,[aria-label],[title],[data-testid]` и пропускает их, если рядом с prompt текст включает имя ZIP: `AuditSubmitCodexChromeCommand.cs:3632-3640`.
      - В этой логике нет ни одного обязательного признака attachment chip; итоговая проверка — только `roots.length === 1`: `AuditSubmitCodexChromeCommand.cs:3642-3644`.
      - Доменный документ обещает более сильный контракт: «рядом с полем должна быть видна ровно одна плашка имени основного audit ZIP»: `audit-package.md:125`.
      - Текущий тест покрывает только четыре случая: положительный, пустой prompt, отсутствие attachment и attachment в истории. Он не проверяет ложноположительный случай с обычным nearby text label, а его fixture вообще создаёт attachment как специальный элемент с `data-testid='composer-attachment'` и `title`: `RepositoryBuildToolTests.cs:6109-6119`, `9093-9119`.
    - `Impact`: r40 B1 остаётся незакрытым. Архив не доказывает, что production submit-path действительно отправляет запрос только при сохранённой реальной ZIP-плашке текущей отправки.
    - `Fix`: сделать selector attachment-specific и добавить negative regression на plain filename label без attachment semantics.
    - `Verification`:
      - новый production-DOM regression для `PromptPayloadReadyExpression`
      - `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --no-build --no-restore --filter "FullyQualifiedName~AuditSubmit"`
      - `dotnet run --project eng/Electron2D.Build --no-build -- update docs --check`
      - `dotnet run --project eng/Electron2D.Build --no-build -- verify docs`
      - `dotnet run --project eng/Electron2D.Build --no-build -- verify audit-followups`
      - `dotnet run --project eng/Electron2D.Build --no-build -- verify licenses`
      - `git diff --check`

EVIDENCE_REVIEW:
- Я проверил, что основной ZIP читается и содержит полный набор материалов для содержательного аудита: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `repo-after/`, `repo-before/`, patch и сырые evidence configured checks.
- Полнота снимков достаточна: по `metadata/repo-file-snapshots.json` присутствуют важные файлы реализации, тестов и документации, поэтому это не `patch-only inspection`.
- Проверка прошлых verdict-файлов выполнена по `metadata.previousVerdictChain`. В архиве доступны сохранённые отчёты до `r36` и `r40`. Путь `r37/r38/r39` действительно отсутствует в цепочке, и это согласовано с task notes: для них saved verdict-файлы не были созданы, поэтому текущее metadata не скрывает существующий saved report.
- `metadata.blockerClosureList` подробно перечисляет закрытие прежних blocker-ов. По содержанию кода и тестов я подтверждаю, что многие старые замечания действительно закрыты, но closure `r40 B1` остаётся неполным из-за blocker-а B1 выше.
- Проверенные evidence-пакеты показывают зелёный локальный коридор: focused `AuditSubmit` suite, `update docs --check`, `verify docs`, `verify audit-followups`, `verify licenses` и `git diff --check` завершились успешно. Эти evidence не снимают blocker B1, потому что они не покрывают найденный ложноположительный payload-case.
- Секретов, приватных ключей, токенов, реальных паролей, конфиденциальных локальных путей или иных глобальных safety-проблем в изменённых файлах и evidence я не нашёл.
- Изменение не затрагивает runtime hot path игрового движка и не вводит новый Public API Electron2D; поэтому performance/Public API review в текущей области ограничивается sanity check и архитектурной проверкой release-management tooling.

Техническая привязка:
- Проверенные metadata и manifest:
  - `AUDIT-MANIFEST.md`
  - `metadata/audit-package.input.json`
  - `metadata/repo-file-snapshots.json`
  - `repo-file-hashes.json`
  - `T-0238.patch`
- Проверенная реализация:
  - `repo-after/eng/Electron2D.Build/AuditSubmitCodexChromeCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditSubmitCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditPackageCommand.cs`
  - `repo-after/eng/Electron2D.Build/AuditFollowupVerifier.cs`
  - `repo-after/eng/Electron2D.Build/Program.cs`
- Проверенные тесты:
  - `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`
- Проверенная документация и правила:
  - `repo-after/docs/release-management/AUDIT-REQUEST.md`
  - `repo-after/docs/release-management/audit-package.md`
  - `repo-after/AGENTS.md`
  - `repo-after/.codex/prompts/goal-task-loop.md`
  - `repo-after/TASKS.md`
- Проверенные evidence:
  - `evidence/T-0238-r41/checks/audit-submit-focused-tests-r41/stdout.txt` — `118/118`
  - `evidence/T-0238-r41/checks/update-docs-check/stdout.txt`
  - `evidence/T-0238-r41/checks/verify-docs/stdout.txt`
  - `evidence/T-0238-r41/checks/verify-audit-followups/stdout.txt`
  - `evidence/T-0238-r41/checks/verify-licenses/stdout.txt`
  - `evidence/T-0238-r41/checks/git-diff-check/exit-code.txt`

RISKS_AND_NOTES:
- ACCEPTED_RISK R1
  - Объяснение: в текущем архиве по-прежнему явно сохраняется уже принятый риск по semantic validation closure targets. `verify audit-followups` проверяет наличие closure note, source/id, state, target и rationale, но не доказывает, что указанный `tracked-existing` или `tracked-new` target действительно существует и соответствует смыслу finding-а. Это не новый blocker r41 и не мешает текущему решению, потому риск уже оформлен как осознанно принятый в tracked `TASKS.md`.
  - Rationale: текущая задача закрывает обязательную форму closure notes и их uniqueness по `(saved report path, finding id)`, а семантическая проверка target tasks вынесена в отдельный риск и не входит в минимальный проверяемый контракт T-0238.
  - Next decision point: пересмотр при `T-0105` risk register или при отдельной задаче на semantic validation closure targets.
  - Техническая привязка:
    - Идентификатор: `ACCEPTED_RISK R1`
    - `File/symbol`: `repo-after/TASKS.md:1912-1923`
    - Служебный класс: `accepted risk`
    - Связанный source/finding: `docs/verdicts/release-management/t-0238-audit-r01.md`, `FOLLOW_UP_FINDING F1`

CLOSURE_DECISION:
- Текущий пакет закрывать нельзя. r41 действительно добавляет production pre-send payload guard и хороший минимальный regression на пустой prompt, отсутствие attachment и attachment в истории, но этого недостаточно для приёмки. Пока selector payload-а принимает произвольный nearby text label с именем ZIP вместо реальной attachment chip, r40 B1 остаётся незакрытым, а `T-0238 r41` не доказывает свой заявленный контракт.
- Для следующей итерации нужно сузить selector до реальной ZIP-плашки, добавить отрицательный regression на plain nearby filename label и затем повторить локальный corridor evidence. Только после этого можно считать закрытие r40 B1 проверяемым.
