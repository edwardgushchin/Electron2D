VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0241` / `r01` как одиночная область задачи. Изменение действительно направлено на перенос корневого контракта публичного API с прежнего «2D-профиля» на Godot `4.7-stable` public API contract: обновлены доменные документы, API manifest, generator, CLI/template wording, verifier и интеграционные тесты.
* Принять изменение нельзя. В текущем итоговом состоянии доменный документ всё ещё содержит ручной и уже неполный список публичных типов, хотя сама задача прямо запрещает ручные Markdown-списки публичной поверхности. Кроме того, в доменном документе отсутствует требуемая таблица потребителей/первого подтверждения связи, а generated docs index ссылается на изменившийся hash `docs/cli/e2d-cli.md`, но сам файл отсутствует в `repo-after/`, `repo-before/`, `repo-file-hashes.json` и `metadata/repo-file-snapshots.json`.
* Прошлых verdict-отчётов для закрытия не заявлено: `metadata.previousVerdictChain` пустой, `metadata.blockerClosureList` пустой.

Техническая привязка:

* `metadata.taskId`: `T-0241`
* `metadata.iteration`: `r01`
* `metadata.scopeTaskIds`: [`T-0241`]
* `metadata.scopeSummary`: root Godot 4.7 public API compatibility contract без добавления runtime public classes.
* `metadata.previousVerdictChain`: []
* `metadata.blockerClosureList`: []
* Проверенные основные материалы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0241.patch`, `repo-before/`, `repo-after/`, `evidence/T-0241-r01/preflight/**`.

BLOCKERS:

* B1

  * Что не так: Доменный документ сам запрещает задавать контракт ручным списком классов в Markdown, но ниже в том же файле оставлен ручной список текущего baseline public API. Этот список уже расходится с generated API manifest: в документе перечислено 111 типов, а manifest и verifier подтверждают 175 public types. В ручном списке отсутствуют, например, `Electron2D.AudioServer`, `Electron2D.BaseButton`, `Electron2D.Button`, `Electron2D.ImageTexture`, `Electron2D.Input` и другие public types из generated manifest.
  * Почему это важно: T-0241 является foundation-задачей для будущих Public API задач. Если корневой доменный документ одновременно требует generated source of truth и содержит stale Markdown-список публичной поверхности, следующие задачи и аудиторы получают два конкурирующих источника истины. Это прямо нарушает критерий задачи: публичная поверхность должна браться из generated descriptions/API artifacts, а не из ручного списка.
  * Что исправить: Убрать ручной список public types из `docs/release-management/api-compatibility.md` или заменить его короткой ссылкой на generated artifacts: `data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md`, будущие отчёты `T-0242`/`T-0243`/`T-0244`/`T-0245`. Добавить verifier/test, который не даст снова вставить ручной список `- Electron2D.*` в `TASKS.md` или доменные документы как источник публичной поверхности.
  * Как проверить исправление: Повторно выполнить `dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki`, focused tests `FullyQualifiedName~VerifyApiCompatibility`, `update docs --check`, `verify docs`; дополнительно проверить, что в доменном документе нет ручного baseline-списка public types и что verifier падает на такой список в негативном тесте.
  * Проверка опровержения: Проверены `repo-after/docs/release-management/api-compatibility.md`, `repo-after/data/api/electron2d-api-manifest.json`, `RepositoryBuildToolTests.cs` и evidence `api-compatibility`. Команда `verify api-compatibility` прошла с `Public types: 175`, но документ при этом сохранил ручной список 111 типов, значит текущий verifier и тесты не закрывают нарушение.
  * Техническая привязка:

    * `File/symbol`: `repo-after/docs/release-management/api-compatibility.md`, разделы `## Цель` и `## Текущий baseline`
    * `Criterion`: критерии T-0241 «Контракт запрещает вручную переписывать списки публичных элементов API...» и `Public API` / `Godot 4.7` source-of-truth requirement.
    * `Evidence`: `repo-after/docs/release-management/api-compatibility.md:21` говорит, что контракт не задаётся ручным списком классов; `repo-after/docs/release-management/api-compatibility.md:114-228` содержит ручной список public types; `repo-after/data/api/electron2d-api-manifest.json:2571`, `:4146`, `:4596`, `:12543`, `:12680` содержит public types, отсутствующие в ручном списке; `evidence/T-0241-r01/preflight/api-compatibility/output.txt:2` подтверждает `Public types: 175`.
    * `Impact`: корневой контракт остаётся противоречивым и уже stale, поэтому foundation-задача не может считаться закрытой.
    * `Fix`: заменить ручной список ссылкой на generated source of truth и добавить проверку против ручных public API списков в contract docs/TASKS.
    * `Verification`: `verify api-compatibility --wiki-path .github/wiki`, focused regression test на manual public API list, `update docs --check`, `verify docs`.

* B2

  * Что не так: Текущий доменный документ не фиксирует требуемую таблицу потребителей, внутренних возможностей и первого проверочного evidence. В карточке T-0241 это вынесено в критерии и подзадачи: доменный документ должен фиксировать потребителей, внутренние возможности, путь подтверждения и таблицу потребителей. В `repo-after/docs/release-management/api-compatibility.md` такой секции или таблицы нет.
  * Почему это важно: T-0241 задаёт контракт для последующих задач `T-0242`, `T-0243`, `T-0244`, `T-0245`, `T-0963`, `T-0980`, а также для verifier-а, API manifest, CLI и шаблонов проекта. Без явной карты потребителей невозможно проверить, какие последующие задачи и инструменты обязаны использовать этот контракт и каким первым тестом/файлом подтверждается связь.
  * Что исправить: Добавить в `docs/release-management/api-compatibility.md` отдельную таблицу потребителей. Минимально она должна связывать: потребителя или задачу, требуемую внутреннюю возможность, generated/source-of-truth artifact, первый тест или verifier/evidence, а также ограничение для desktop/mobile/web/editor-only случаев.
  * Как проверить исправление: Проверить full-file review доменного документа и добавить regression test/verifier guard, который требует наличие секции потребителей для root public API contract. Затем повторить `verify api-compatibility`, `docs-check`, `docs-verify` и focused tests.
  * Проверка опровержения: Проверен полный файл `repo-after/docs/release-management/api-compatibility.md`; в нём есть root contract, platform diagnostics и local verification, но нет секции или таблицы с потребителями. Проверены focused tests: они покрывают obsolete phrase и stale link, но не проверяют consumer map.
  * Техническая привязка:

    * `File/symbol`: `repo-after/docs/release-management/api-compatibility.md`
    * `Criterion`: T-0241 acceptance criteria: доменный документ фиксирует потребителей, внутренние возможности и путь подтверждения; подзадача — «таблицу потребителей».
    * `Evidence`: `evidence/T-0241-r01/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:58-59`, `:80`, `:93`; в `repo-after/docs/release-management/api-compatibility.md:19-252` такой таблицы нет; добавленные tests в `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:945-974` проверяют только obsolete root wording и stale T-0241 link.
    * `Impact`: контракт нельзя безопасно использовать как foundation для следующих Public API задач, потому что потребители и первое доказательство связи не зафиксированы в основном доменном документе.
    * `Fix`: добавить consumer/internal capability/evidence table и проверку её наличия.
    * `Verification`: full-file review `docs/release-management/api-compatibility.md`, focused verifier test, `verify api-compatibility`, `update docs --check`, `verify docs`.

* B3

  * Что не так: Generated local docs index изменил hash источника `docs/cli/e2d-cli.md`, но сам `docs/cli/e2d-cli.md` не включён в `repo-after/`, `repo-before/`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, manifest diff name-status или allowlist. Это означает одно из двух: либо источник документации действительно изменён, но скрыт из audit package, либо generated index содержит hash, который нельзя проверить по предоставленным снимкам.
  * Почему это важно: В область T-0241 включены generated local docs index и docs checks. Если generated artifact утверждает новый hash исходного документа, внешний аудит должен иметь возможность прочитать соответствующий исходный документ или увидеть, что hash не менялся. Сейчас это невозможно; `docs-check` evidence доказывает синхронизацию только в локальном окружении автора пакета, но не даёт full-file review изменившегося source document.
  * Что исправить: Если `docs/cli/e2d-cli.md` действительно изменён — включить его в `repoFileAllowlist`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `repo-before/`, `repo-after/` и manifest diff. Если он не должен был изменяться — пересобрать docs index на clean state так, чтобы его hash не менялся относительно baseline. После этого заново собрать audit package.
  * Как проверить исправление: Сравнить `generatedFrom.documentation` в `data/documentation/electron2d-local-docs-index.json` с `repo-file-hashes.json` и `metadata/repo-file-snapshots.json`; все изменившиеся source hashes должны иметь проверяемый полный snapshot или не должны меняться. Затем повторить `update docs --check` и `verify docs`.
  * Проверка опровержения: Проверены `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `repo-before/` и `repo-after/`. `docs/cli/e2d-cli.md` отсутствует в snapshot set, но hash в generated docs index изменился с `b2775f...` на `5156c7...`; значит это не закрывается текущими материалами пакета.
  * Техническая привязка:

    * `File/symbol`: `repo-after/data/documentation/electron2d-local-docs-index.json`, `generatedFrom.documentation[path=docs/cli/e2d-cli.md]`
    * `Criterion`: `evidence gap`, `full file review`, `documentation review`, полнота snapshots для важных файлов документации.
    * `Evidence`: `repo-before/data/documentation/electron2d-local-docs-index.json:51-52` содержит old hash `b2775fb77db080d99492a51a12138a539e1f89b1b171a7f445d22aedb0351d46`; `repo-after/data/documentation/electron2d-local-docs-index.json:51-52` содержит new hash `5156c72e4e3f3e5c7d48585894dc3b6a147dab6ede8546949ccdae413205dd94`; `AUDIT-MANIFEST.md:15-37` и `metadata/audit-package.input.json:12-35` не содержат `docs/cli/e2d-cli.md`; файл отсутствует в `repo-after/` и `repo-before/`.
    * `Impact`: невозможно выполнить полноценную проверку documentation source, на котором основан изменённый generated docs index.
    * `Fix`: включить `docs/cli/e2d-cli.md` в package, если он изменился, либо устранить лишнее изменение hash в generated index.
    * `Verification`: regenerated docs index, `update docs --check`, `verify docs`, snapshot completeness check.

EVIDENCE_REVIEW:

* Прочитаны metadata и manifest текущего пакета. Область одиночная: `T-0241`; combined scope не заявлен. Изменённые файлы в `AUDIT-MANIFEST.md` совпадают с allowlist в metadata, кроме найденной проблемы B3 по hidden/generated docs source hash.
* Выполнен full-file review по `repo-after/` для изменённого кода, тестов и документации. Patch использовался только как карта изменений, не как замена чтению полных файлов.
* Проверены production/tooling изменения: `eng/Electron2D.ApiManifestGenerator/Program.cs`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs`, `src/Electron2D.Cli/ContextPackCli.cs`, `src/Electron2D.ProjectSystem/Templates/ProjectTemplateCreator.cs`.
* Проверены тесты: `tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Новые тесты проверяют stale root wording и stale T-0241 link, но не закрывают B1 и B2.
* Проверены документы и generated artifacts: `docs/release-management/api-compatibility.md`, `docs/documentation/api-manifest.md`, `docs/releases/0.1-preview.md`, `docs/release-management/AUDIT-REQUEST.md`, `docs/architecture/agent-native-workflow.md`, `docs/architecture/engine-platform-stack.md`, `docs/core-types/variant.md`, `docs/editor/godot4-editor-reference.md`, `docs/project-system/static-context-pack.md`, `docs/release-management/project-template.md`, `docs/rendering/texture-resource-baseline.md`, `docs/runtime/project-runtime-runner.md`, `docs/scripting/editor-script-workflow.md`, `data/api/electron2d-api-manifest.json`, `data/documentation/electron2d-local-docs-index.json`, `data/documentation/local-docs-index/documentation.ndjson`, `data/templates/electron2d-empty/AGENTS.md`.
* Проверено evidence preflight. Все заявленные команды в evidence завершились с exit code `0`: build tool build, `verify api-compatibility`, focused API compatibility tests, API surface tests, `update api-manifest --check`, `update docs --check`, `verify docs`, `verify audit-contracts`, `verify manifests`, `verify licenses`, `audit-medium`, `audit-heavy`, `git diff --check`. Это не снимает blockers, потому что blockers относятся к содержанию доменного документа и полноте source snapshots, а текущие проверки их не ловят.
* Проверка секретов и локальных данных по доступным файлам не выявила реальных токенов, приватных ключей, паролей или локальных абсолютных путей. Совпадения вида `electron2d://` являются URI API manifest, а `<repo>` в evidence — placeholder.

Техническая привязка:

* Metadata: `metadata/audit-package.input.json`
* Manifest: `AUDIT-MANIFEST.md`
* Snapshots: `metadata/repo-file-snapshots.json`
* File hashes: `repo-file-hashes.json`
* Patch map: `T-0241.patch`
* Проверенные evidence artifacts:

  * `evidence/T-0241-r01/preflight/build-tool-build/output.txt`
  * `evidence/T-0241-r01/preflight/api-compatibility/output.txt`
  * `evidence/T-0241-r01/preflight/api-compatibility-tests/output.txt`
  * `evidence/T-0241-r01/preflight/api-surface-tests/output.txt`
  * `evidence/T-0241-r01/preflight/api-manifest-check/output.txt`
  * `evidence/T-0241-r01/preflight/docs-check/output.txt`
  * `evidence/T-0241-r01/preflight/docs-verify/output.txt`
  * `evidence/T-0241-r01/preflight/audit-contracts/output.txt`
  * `evidence/T-0241-r01/preflight/manifests/output.txt`
  * `evidence/T-0241-r01/preflight/licenses/output.txt`
  * `evidence/T-0241-r01/preflight/audit-medium/output.txt`
  * `evidence/T-0241-r01/preflight/audit-heavy/output.txt`
  * `evidence/T-0241-r01/preflight/git-diff-check/output.txt`
  * `evidence/T-0241-r01/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md`
  * `evidence/T-0241-r01/preflight/task-ledger-excerpts/dev-diary-05-07-2026-T-0241-excerpts.md`

RISKS_AND_NOTES:

* None.

CLOSURE_DECISION:

* Задача остаётся открытой. Нужно убрать stale ручной список публичной поверхности из доменного контракта или заменить его ссылкой на generated source of truth, добавить обязательную таблицу потребителей/первого evidence в `docs/release-management/api-compatibility.md`, а также устранить evidence gap по `docs/cli/e2d-cli.md` в generated docs index. После исправлений требуется новый audit package с полными snapshots и повторным evidence по verifier/tests/docs checks.
