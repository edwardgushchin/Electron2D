VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0241` / `r03` как повторная итерация после `r01` и `r02`. Область пакета одиночная: `metadata.scopeTaskIds = ["T-0241"]`; combined scope не заявлен.
* Изменение закрывает ранее найденные blockers по `r01` и `r02`: корневой документ больше не содержит ручной список public types, карта потребителей добавлена, hash `docs/cli/e2d-cli.md` больше не меняется скрыто, а `docs/core-types/variant.md` больше не утверждает четырёхтиповый exported runtime baseline.
* Принять задачу нельзя. В изменённом release-документе всё ещё опубликован статус совместимости `Not planned`, хотя корневой контракт T-0241 требует, чтобы намеренные отличия проходили через явные `Deferred`/`Unsupported` строки и утверждались через `T-0963`. Это создаёт обход корневого правила именно в документации, которая входит в текущий root-contract slice.
* Прошлые verdict-файлы из `metadata.previousVerdictChain` доступны в пакете и прочитаны: `docs/verdicts/release-management/t-0241-audit-r01.md`, `docs/verdicts/release-management/t-0241-audit-r02.md`. Их blockers сопоставлены с `metadata.blockerClosureList`.

Техническая привязка:

* `metadata.taskId`: `T-0241`
* `metadata.iteration`: `r03`
* `metadata.scopeTaskIds`: [`T-0241`]
* `metadata.scopeSummary`: r03 закрывает primary r02 `B1` по `docs/core-types/variant.md` и сохраняет закрытие r01 `B1`-`B3`.
* `metadata.previousVerdictChain`: [`docs/verdicts/release-management/t-0241-audit-r01.md`, `docs/verdicts/release-management/t-0241-audit-r02.md`]
* `metadata.blockerClosureList`: четыре записи закрытия: r01 `B1`, r01 `B2`, r01 `B3`, r02 `B1`.
* Проверенные основные материалы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0241.patch`, `repo-before/`, `repo-after/`, `evidence/T-0241-r03/preflight/**`.

BLOCKERS:

* B1

  * Что не так: В `docs/releases/0.1-preview.md` остался статус совместимости `Not planned` со смыслом «осознанно не поддерживается». Это противоречит корневому контракту T-0241: намеренные отличия должны оформляться только как явные `Deferred` или `Unsupported` строки через `T-0963`, быть видимыми в API/behavior reports и проверяться финальным рубежом `T-0980`.
  * Почему это важно: T-0241 является foundation-задачей для полного Godot `4.7-stable` public API contract. Если в release-документации остаётся отдельный статус `Not planned`, будущие задачи могут пометить отсутствующий Godot API как «не планируется» без процедуры `T-0963`. Это ломает главное правило текущей задачи: любое намеренное отличие должно быть явным, проверяемым и утверждённым, а не скрытым альтернативным статусом в Markdown.
  * Что исправить: Убрать `Not planned` из таблицы API compatibility statuses в `docs/releases/0.1-preview.md` или явно заменить его на модель `Deferred`/`Unsupported` через `T-0963`. Одновременно нужно расширить verifier/test coverage так, чтобы root-contract docs не могли снова ввести `Not planned` или другой неутверждённый waiver-status для публичного API.
  * Как проверить исправление: Выполнить `dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki`, focused tests `FullyQualifiedName~VerifyApiCompatibility`, `dotnet run --project eng/Electron2D.Build -- update docs --check`, `dotnet run --project eng/Electron2D.Build -- verify docs`. Добавить негативный тест, который вставляет `| Not planned |` в `docs/releases/0.1-preview.md` или другой root-contract документ и проверяет падение verifier-а.
  * Проверка опровержения: Проверены `docs/release-management/api-compatibility.md`, `docs/releases/0.1-preview.md`, `TASKS-T-0241-excerpt.md`, `RepositoryPolicyVerifiers.cs` и evidence `api-compatibility`. Корневой документ разрешает только `Supported`, `Partial`, `Experimental`, `Planned` для runtime compatibility table, а task excerpt требует `Deferred`/`Unsupported` через `T-0963`. При этом `docs/releases/0.1-preview.md` всё ещё содержит `Not planned`, а verifier проходит, потому что не запрещает этот status в root-contract docs.
  * Техническая привязка:

    * `File/symbol`: `repo-after/docs/releases/0.1-preview.md:169-179`
    * `Criterion`: `documentation review`, `task compliance review`, `Godot 4.7`, `Public API`, критерий T-0241 о `Deferred`/`Unsupported` через `T-0963`.
    * `Evidence`: `repo-after/docs/releases/0.1-preview.md:177` содержит `| Not planned | Осознанно не поддерживается |`; `evidence/T-0241-r03/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:67` требует явные `Deferred`/`Unsupported` через `T-0963`; `evidence/T-0241-r03/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:84` повторяет этот acceptance criterion; `repo-after/docs/release-management/api-compatibility.md:25-30` перечисляет только `Supported`, `Partial`, `Experimental`, `Planned`; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:591-597` также требует только эти четыре статуса и не запрещает `Not planned`.
    * `Impact`: текущий root contract остаётся противоречивым: release-документ сохраняет неутверждённый способ отказа от API, обходящий `T-0963`.
    * `Fix`: удалить или заменить `Not planned` на утверждаемую модель `Deferred`/`Unsupported`; добавить verifier/test guard против неутверждённых waiver-statuses в root-contract docs.
    * `Verification`: `verify api-compatibility --wiki-path .github/wiki`, focused regression test на `Not planned`, `update docs --check`, `verify docs`.

EVIDENCE_REVIEW:

* Прочитаны metadata и manifest текущего пакета. `metadata.taskId = T-0241`, `metadata.iteration = r03`, область одиночная, без combined scope. Manifest и metadata согласованно описывают r03 как закрытие r02 `B1` и сохранение закрытия r01 `B1`-`B3`.
* Проверена полнота snapshots для заявленной области. `metadata/repo-file-snapshots.json` содержит 25 repo-owned файлов, все с `fullContentIncluded: true`; изменённые файлы доступны в `repo-after/`, а существовавшие до изменения — в `repo-before/`. `SHA256SUMS.txt` и `repo-file-hashes.json` согласованы с содержимым архива. Patch использовался только как карта изменений, не как замена full-file review.
* Проверены прошлые verdict-файлы: `repo-after/docs/verdicts/release-management/t-0241-audit-r01.md` и `repo-after/docs/verdicts/release-management/t-0241-audit-r02.md`. Старые blockers прочитаны и сопоставлены с `metadata.blockerClosureList`.
* По r01 `B1`: закрытие подтверждено. В `repo-after/docs/release-management/api-compatibility.md:127-135` root baseline больше не содержит ручного списка public types и ссылается на `data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md` и будущие generated reports.
* По r01 `B2`: закрытие подтверждено. В `repo-after/docs/release-management/api-compatibility.md:45-56` добавлена `## Карта потребителей контракта` с потребителями, внутренними возможностями, source of truth, первым evidence и ограничениями.
* По r01 `B3`: закрытие подтверждено. В `repo-before/data/documentation/electron2d-local-docs-index.json:51-52` и `repo-after/data/documentation/electron2d-local-docs-index.json:51-52` hash `docs/cli/e2d-cli.md` одинаковый: `b2775fb77db080d99492a51a12138a539e1f89b1b171a7f445d22aedb0351d46`. Все изменившиеся source hashes generated docs index имеют полные snapshots в текущем пакете.
* По r02 `B1`: закрытие подтверждено. В `repo-after/docs/core-types/variant.md:127-142` удалено утверждение о четырёхтиповом `Public API baseline`; документ теперь говорит, что Variant-domain сверяется через generated API manifest, compatibility table и `verify api-compatibility`, а не через ручной exported runtime baseline. В `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:622-627` scanner расширен на `docs/core-types/variant.md`, а `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1014-1036` добавляет негативный тест для scoped domain document с пунктуацией после closing backtick.
* Проверены production/tooling изменения: `eng/Electron2D.ApiManifestGenerator/Program.cs`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs`, `src/Electron2D.Cli/ContextPackCli.cs`, `src/Electron2D.ProjectSystem/Templates/ProjectTemplateCreator.cs`. Изменения относятся к generator/verifier/template wording и не затрагивают горячий runtime/game loop path.
* Проверены тесты: `tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Тесты покрывают obsolete root wording, stale T-0241 link, отсутствие consumer map, manual public API list в root document и manual public API list в `docs/core-types/variant.md`. Тест на запрет `Not planned` в root-contract docs отсутствует.
* Проверены документы и generated artifacts: `docs/release-management/api-compatibility.md`, `docs/core-types/variant.md`, `docs/documentation/api-manifest.md`, `docs/releases/0.1-preview.md`, `docs/release-management/AUDIT-REQUEST.md`, `docs/architecture/agent-native-workflow.md`, `docs/architecture/engine-platform-stack.md`, `docs/editor/godot4-editor-reference.md`, `docs/project-system/static-context-pack.md`, `docs/release-management/project-template.md`, `docs/rendering/texture-resource-baseline.md`, `docs/runtime/project-runtime-runner.md`, `docs/scripting/editor-script-workflow.md`, `data/api/electron2d-api-manifest.json`, `data/documentation/electron2d-local-docs-index.json`, `data/documentation/local-docs-index/documentation.ndjson`, `data/templates/electron2d-empty/AGENTS.md`.
* Проверены evidence preflight artifacts. Все заявленные проверки завершились с exit code `0`: build tool build, `verify api-compatibility`, focused API compatibility tests, API surface tests, `update api-manifest --check`, `update docs --check`, `verify docs`, `verify audit-contracts`, `verify manifests`, `verify licenses`, `audit-medium`, `audit-heavy`, `git diff --check`. Эти checks не снимают blocker, потому что текущий verifier не запрещает `Not planned` в root-contract release-документе.
* Проверка секретов и локальных данных по доступным файлам, patch и evidence не выявила реальных токенов, приватных ключей, паролей или конфиденциальных локальных абсолютных путей. Совпадения вида `electron2d://` являются URI API manifest, `<repo>` в evidence — placeholder, а secret-like строки в tests являются синтетическими fixture-строками для scanner tests.

Техническая привязка:

* Metadata: `metadata/audit-package.input.json`
* Manifest: `AUDIT-MANIFEST.md`
* Snapshots: `metadata/repo-file-snapshots.json`
* File hashes: `repo-file-hashes.json`
* Patch map: `T-0241.patch`
* Previous verdict files:

  * `repo-after/docs/verdicts/release-management/t-0241-audit-r01.md`
  * `repo-after/docs/verdicts/release-management/t-0241-audit-r02.md`
* Проверенные evidence artifacts:

  * `evidence/T-0241-r03/preflight/build-tool-build/output.txt`
  * `evidence/T-0241-r03/preflight/api-compatibility/output.txt`
  * `evidence/T-0241-r03/preflight/api-compatibility-tests/output.txt`
  * `evidence/T-0241-r03/preflight/api-surface-tests/output.txt`
  * `evidence/T-0241-r03/preflight/api-manifest-check/output.txt`
  * `evidence/T-0241-r03/preflight/docs-check/output.txt`
  * `evidence/T-0241-r03/preflight/docs-verify/output.txt`
  * `evidence/T-0241-r03/preflight/audit-contracts/output.txt`
  * `evidence/T-0241-r03/preflight/manifests/output.txt`
  * `evidence/T-0241-r03/preflight/licenses/output.txt`
  * `evidence/T-0241-r03/preflight/audit-medium/output.txt`
  * `evidence/T-0241-r03/preflight/audit-heavy/output.txt`
  * `evidence/T-0241-r03/preflight/git-diff-check/output.txt`
  * `evidence/T-0241-r03/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md`
  * `evidence/T-0241-r03/preflight/task-ledger-excerpts/dev-diary-05-07-2026-T-0241-excerpts.md`

RISKS_AND_NOTES:

* FOLLOW_UP_FINDING F1

  * Идентификатор: `F1`
  * Где найдено: `repo-after/data/api/electron2d-api-manifest.json:1165-1173`, аналогичные entries повторяются для backing fields других enum types.
  * Проблема: Generated API manifest включает CLR enum backing field `value__` как `EnumValue` member с пустым summary. Это выглядит как техническая деталь CLR, а не пользовательский public API element Godot/Electron2D.
  * Почему не блокирует текущую задачу: Это не блокирует T-0241 в текущем отказе. Проблема существовала и в `repo-before`, а текущее изменение не добавляло новый runtime public API. Полная нормализация generated API descriptions и diff reports естественно относится к следующим задачам `T-0242`/`T-0243`. Но для будущего source-of-truth manifest это важно: generated descriptions не должны содержать неразработческий enum backing field.
  * Куда перенести: Suggested existing task: `T-0242` или `T-0243`, если они уже открывают generated API descriptions / API diff checks.
  * Suggested new task: «Отфильтровать CLR enum backing field `value__` из generated API manifest и Wiki pages». Рекомендуемый приоритет: `P1`. Домен: `documentation/api-manifest`. Критерий приёмки: enum public members в manifest/Wiki содержат только реальные enum values, без `value__`; verifier/test падает, если `value__` появляется в generated API artifacts. Идея проверки: focused test для `ApiManifestTests` плюс `update api-manifest --check` и `update wiki --check`.
  * Рекомендуемый приоритет: `P1`
  * Как проверить: Добавить тест, который читает `data/api/electron2d-api-manifest.json` и проверяет отсутствие member `name = "value__"`; обновить generator, manifest и generated Wiki artifacts.

* FOLLOW_UP_FINDING F2

  * Идентификатор: `F2`
  * Где найдено: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:622-627`, `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:887-905`
  * Проблема: Проверка ручных public API Markdown lists сейчас сканирует только `TASKS.md`, `docs/release-management/api-compatibility.md` и `docs/core-types/variant.md`. Это закрывает текущий найденный stale list, но не является общим запретом для всех доменных документов, хотя формулировка T-0241 говорит про `TASKS.md` и доменные документы в целом.
  * Почему не блокирует текущую задачу: В текущем пакете не найден другой ручной список public types вида `- \`Electron2D.*``в изменённых доменных документах, поэтому это не самостоятельная причина отказа. Текущий отказ уже вызван более прямым противоречием статуса`Not planned`. Но verifier лучше расширить до явного allowlist/scan strategy для всех root-contract domain docs, чтобы будущая задача не повторила r02-проблему в новом документе.
  * Куда перенести: Suggested existing task: можно закрыть внутри следующей итерации T-0241 вместе с B1 или перенести в `T-0243`, если там будет общий API compatibility verifier hardening.
  * Suggested new task: «Расширить запрет ручных public API lists на все root-contract domain documents». Рекомендуемый приоритет: `P1`. Домен: `release-management/api-compatibility`. Критерий приёмки: verifier сканирует все root-contract domain docs или явно документированный список релевантных docs; негативный тест доказывает падение для manual `Electron2D.*` list в документе вне `api-compatibility.md` и `variant.md`.
  * Рекомендуемый приоритет: `P1`
  * Как проверить: Добавить fixture с ручным list в другом изменённом root-contract document, например `docs/releases/0.1-preview.md`, и проверить diagnostic `E2D-BUILD-API-COMPATIBILITY-CONTRACT-MANUAL-LIST`.

CLOSURE_DECISION:

* Задача остаётся открытой. r03 проверяемо закрывает r02 blocker по `docs/core-types/variant.md` и сохраняет закрытие r01 blockers, но текущий root-contract documentation slice всё ещё допускает status `Not planned`, который обходил бы обязательную процедуру `Deferred`/`Unsupported` через `T-0963`. Нужно устранить этот статус из release/root-contract документации или перевести его в утверждённую модель исключений и добавить verifier/test, который не позволит вернуть такой обход.
