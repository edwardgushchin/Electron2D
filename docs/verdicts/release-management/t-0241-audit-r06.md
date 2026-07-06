VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0241` / `r06` как повторная итерация после `r01`, `r02`, `r03`, `r04` и `r05`. Область пакета одиночная: `metadata.scopeTaskIds = ["T-0241"]`; combined scope не заявлен.
* r06 действительно закрывает основные замечания r05 в тех местах, на которые они прямо указывали: `docs/releases/0.1-preview.md` больше не содержит найденные в r05 fenced/inline fragments для `RenderingServer.*`, `RenderingDeviceFeatures.*`, `[Export]`/`[Signal]`/`[Tool]`, physics/animation/audio baseline lists, а `docs/documentation/github-wiki-api-reference.md` включён полным snapshot-ом и описан как потребитель generated source of truth.
* Принять задачу нельзя. В текущей области всё ещё остаются ручные списки публичного API в доменных документах. Это не гипотетический риск: `docs/core-types/variant.md`, `docs/rendering/texture-resource-baseline.md` и `docs/scripting/editor-script-workflow.md` вручную перечисляют публичные типы, методы, properties или prerequisite public UI types, хотя критерий T-0241 прямо запрещает ручное переписывание списков публичных элементов API в доменных документах. Новый verifier это не закрывает: advanced fenced/inline detection применяется только к root-contract docs, а часть изменённых доменных документов вообще не входит в `ManualPublicApiListPaths`.
* Прошлые verdict-файлы из `metadata.previousVerdictChain` доступны в пакете и прочитаны: `docs/verdicts/release-management/t-0241-audit-r01.md`, `t-0241-audit-r02.md`, `t-0241-audit-r03.md`, `t-0241-audit-r04.md`, `t-0241-audit-r05.md`. Их blockers сопоставлены с `metadata.blockerClosureList`.

Техническая привязка:

* `metadata.taskId`: `T-0241`
* `metadata.iteration`: `r06`
* `metadata.scopeTaskIds`: [`T-0241`]
* `metadata.scopeSummary`: r06 closes r05 `B1`/`B2`, rewrites release-note manual public API fragments, includes `docs/documentation/github-wiki-api-reference.md` full snapshot, keeps advanced manual-list scanning scoped away from scoped domain docs.
* `metadata.previousVerdictChain`: [`docs/verdicts/release-management/t-0241-audit-r01.md`, `docs/verdicts/release-management/t-0241-audit-r02.md`, `docs/verdicts/release-management/t-0241-audit-r03.md`, `docs/verdicts/release-management/t-0241-audit-r04.md`, `docs/verdicts/release-management/t-0241-audit-r05.md`]
* `metadata.blockerClosureList`: восемь записей closure для r01 `B1`-`B3`, r02 `B1`, r03 `B1`, r04 `B1`, r05 `B1` и r05 `B2`.
* Проверенные материалы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0241.patch`, `repo-before/`, `repo-after/`, `evidence/T-0241-r06/preflight/**`.

BLOCKERS:

* B1

  * Что не так: В изменённых доменных документах остались ручные списки публичного API. В `docs/core-types/variant.md` есть раздел `## C# API` с подписью «Минимальный публичный API» и C# code fence, где вручную перечислены `Variant`, `Variant.Type`, свойства и методы чтения. В том же файле ниже вручную перечислены strict reader methods и API коллекций `Array`/`Dictionary`. В `docs/rendering/texture-resource-baseline.md` есть разделы `## Публичный API T-0025`, `## Целевой публичный API T-0226` и `## Public API`, где вручную перечислены `Texture2D`, `PlaceholderTexture2D`, `Image`, `AtlasTexture`, `ImageTexture`, их методы и properties. В `docs/scripting/editor-script-workflow.md` есть prerequisite manifest с ручным списком public UI types: `TextEdit`, `CodeEdit`, `SyntaxHighlighter`, `CodeHighlighter`, `PopupMenu`, `TabContainer`, `Tree`, `ItemList`, `SplitContainer`, `ScrollBar`, `LineEdit`, `Label`, `Button`.
  * Почему это важно: T-0241 является foundation-задачей для Godot `4.7-stable` public API compatibility contract. Её критерий не ограничен только release note: `TASKS.md` и доменные документы не должны вручную переписывать списки публичных элементов API, а должны ссылаться на generated descriptions и описывать границы, поведение, внутренние связи и evidence. Если принять r06, проект сохранит несколько конкурирующих источников истины: generated manifest/Wiki и ручные API-списки в доменных документах. Это повторяет класс проблем r01-r05, только уже не в release note, а в scoped domain docs.
  * Что исправить: Убрать или переписать ручные public API lists из изменённых доменных документов. Эти документы могут описывать поведение, ограничения, поддержанные значения, ожидаемые сценарии и ссылки на owner-задачи, но точные public type/member/property lists должны приходить из `data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md`, generated reports `T-0242`/`T-0243`/`T-0244` и per-class gate `T-0245`. Если проекту всё же нужен разрешённый формат task-local API sketch, его нужно явно отделить от source of truth и не использовать формулировки вида «Минимальный публичный API», «Публичный API», «Целевой публичный API» с ручными signatures.
  * Как проверить исправление: Добавить negative tests для фактических форматов, которые сейчас остаются в пакете: C# code fence в `docs/core-types/variant.md` с `public readonly struct Variant`, code fence в `docs/rendering/texture-resource-baseline.md` с `public abstract class Texture2D`, inline/bullet prerequisite public UI list в `docs/scripting/editor-script-workflow.md`. Эти тесты должны падать с устойчивым diagnostic, например `E2D-BUILD-API-COMPATIBILITY-CONTRACT-MANUAL-LIST`. Затем выполнить `dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki`, focused tests `FullyQualifiedName~VerifyApiCompatibility`, `dotnet run --project eng/Electron2D.Build -- update docs --check` и `dotnet run --project eng/Electron2D.Build -- verify docs`.
  * Проверка опровержения: Проверены root contract, task excerpt, изменённые доменные документы, verifier, focused tests и preflight evidence. `verify api-compatibility` проходит и focused `VerifyApiCompatibility` tests проходят `14/14`, но это не снимает blocker: текущий verifier применяет advanced fenced/inline detection только к root-contract docs, исключает `docs/core-types/variant.md` из `ManualPublicApiAdvancedListPaths`, а `docs/rendering/texture-resource-baseline.md` и `docs/scripting/editor-script-workflow.md` вообще не входят в `ManualPublicApiListPaths`. Тесты покрывают synthetic bullet list в `docs/core-types/variant.md` и release-note formats, но не покрывают реальные C# API fences и method/property lists, которые остаются в текущем пакете.
  * Техническая привязка:

    * `File/symbol`: `repo-after/docs/core-types/variant.md:60-99`, `repo-after/docs/core-types/variant.md:103-112`, `repo-after/docs/core-types/variant.md:189-209`, `repo-after/docs/core-types/variant.md:217-238`
    * `File/symbol`: `repo-after/docs/rendering/texture-resource-baseline.md:43-112`, `repo-after/docs/rendering/texture-resource-baseline.md:209-235`
    * `File/symbol`: `repo-after/docs/scripting/editor-script-workflow.md:54-68`, `repo-after/docs/scripting/editor-script-workflow.md:184-188`
    * `Criterion`: `task compliance review`, `documentation review`, `previous blockers closure`, критерий T-0241 о запрете ручного переписывания списков публичных элементов API в `TASKS.md` и доменных документах.
    * `Evidence`: `evidence/T-0241-r06/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:56-59` задаёт generated source of truth и consumer/evidence model; `:66` запрещает ручные списки публичных элементов API в `TASKS.md` и доменных документах; `:82` повторяет это как критерий приёмки.
    * `Additional evidence`: `repo-after/docs/release-management/api-compatibility.md:21-23` фиксирует generated source of truth и `Deferred`/`Unsupported` через `T-0963`; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:622-630` включает в `ManualPublicApiListPaths` только root docs и `docs/core-types/variant.md`; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:632-638` включает advanced detection только для root docs, без `docs/core-types/variant.md`; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1088-1108` показывает, что scoped docs получают только narrow check; `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1015-1036` проверяет scoped domain doc только через bullet list `Electron2D.*`, а не через реальные C# signatures/code fences.
    * `Impact`: root contract остаётся неполным: current-scope domain docs продолжают задавать публичную поверхность вручную, а verifier/test coverage не предотвращает этот формат.
    * `Fix`: переписать ручные API lists в domain docs на ссылки на generated artifacts и behavior/evidence prose; расширить verifier/test coverage на C# declaration fences, method/property lists и dense public type prerequisites в scoped domain docs или явно документировать безопасный allowlist без превращения Markdown в source of truth.
    * `Verification`: `verify api-compatibility --wiki-path .github/wiki`, focused negative tests for domain-document C# public API fences/lists, `update docs --check`, `verify docs`, full-file review изменённых доменных документов.

EVIDENCE_REVIEW:

* Прочитаны metadata и manifest текущего пакета. `metadata.taskId = T-0241`, `metadata.iteration = r06`, область одиночная, без combined scope. Manifest и metadata согласованно описывают r06 как закрытие r05 `B1`/`B2`, сохранение r01-r04 closures и refinement root-contract verifier coverage.
* Проверена техническая целостность архива. `SHA256SUMS.txt` сходится; `repo-file-hashes.json` сходится с `repo-after/`; `metadata/repo-file-snapshots.json` содержит 29 repo-owned entries, все с `fullContentIncluded: true`. `docs/documentation/github-wiki-api-reference.md` теперь присутствует как полный snapshot, поэтому r05 `B2` по отсутствующему root-contract файлу закрыт.
* Patch использовался только как карта изменений. Основная проверка выполнялась по полным итоговым файлам из `repo-after/`.
* Проверены прошлые verdict-файлы из `metadata.previousVerdictChain`: `repo-after/docs/verdicts/release-management/t-0241-audit-r01.md`, `t-0241-audit-r02.md`, `t-0241-audit-r03.md`, `t-0241-audit-r04.md`, `t-0241-audit-r05.md`. Старые blockers прочитаны и сопоставлены с `metadata.blockerClosureList`.
* По r01 `B1`: закрытие в основном доменном документе подтверждено. `docs/release-management/api-compatibility.md` больше не содержит старый ручной список `Electron2D.*` public types и указывает на generated source of truth.
* По r01 `B2`: закрытие подтверждено. В `docs/release-management/api-compatibility.md:45-56` есть карта потребителей контракта с internal capability, source of truth, first evidence и ограничениями.
* По r01 `B3`: закрытие подтверждено. Hidden hash drift по `docs/cli/e2d-cli.md` больше не воспроизводится в текущем generated docs index.
* По r02 `B1`: узкое закрытие прежнего четырёхтипового exported runtime baseline в `docs/core-types/variant.md` подтверждено. Однако full-file review того же domain document выявил более широкий остаточный manual API list, вынесенный в текущий B1.
* По r03 `B1`: закрытие `Not planned` подтверждено. Release status table больше не содержит `Not planned`, verifier/test coverage содержит guard `E2D-BUILD-API-COMPATIBILITY-CONTRACT-WAIVER-STATUS`.
* По r04 и r05 blockers в `docs/releases/0.1-preview.md`: закрытие для конкретно найденных release-note fragments подтверждено. r06 release note переписан на behavior/source-of-truth prose и больше не содержит r05-форматы `RenderingServer.*`, `RenderingDeviceFeatures.*`, marker attribute fences или dense inline physics/animation/audio public API lists.
* Проверены production/tooling изменения: `eng/Electron2D.ApiManifestGenerator/Program.cs`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs`, `src/Electron2D.Cli/ContextPackCli.cs`, `src/Electron2D.ProjectSystem/Templates/ProjectTemplateCreator.cs`. Изменения относятся к generator/verifier/template wording и не затрагивают горячий runtime/game-loop path; отдельного performance blocker-а не найдено.
* Проверены тесты: `tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Тесты покрывают root-contract stale wording, missing consumer map, bullet manual public API lists, release fenced public API names, release fenced members, release marker attributes, release inline generated public API names и `Not planned`. Текущий blocker не закрыт тестами, потому что реальные manual lists находятся в scoped domain docs в C# declaration / method-list / prerequisite-list formats.
* Проверены документы и generated artifacts: `docs/release-management/api-compatibility.md`, `docs/core-types/variant.md`, `docs/documentation/api-manifest.md`, `docs/documentation/github-wiki-api-reference.md`, `docs/releases/0.1-preview.md`, `docs/release-management/AUDIT-REQUEST.md`, `docs/architecture/agent-native-workflow.md`, `docs/architecture/engine-platform-stack.md`, `docs/editor/godot4-editor-reference.md`, `docs/project-system/static-context-pack.md`, `docs/release-management/project-template.md`, `docs/rendering/texture-resource-baseline.md`, `docs/runtime/project-runtime-runner.md`, `docs/scripting/editor-script-workflow.md`, `data/api/electron2d-api-manifest.json`, `data/documentation/electron2d-local-docs-index.json`, `data/documentation/local-docs-index/documentation.ndjson`, `data/templates/electron2d-empty/AGENTS.md`.
* Проверены evidence preflight artifacts. Все заявленные проверки завершились с exit code `0`: build tool build, `verify api-compatibility`, focused API compatibility tests, audit-loop stabilization, API surface tests, `update api-manifest --check`, `update docs --check`, `verify docs`, `verify audit-contracts`, `verify audit-followups`, `verify manifests`, `verify licenses`, `audit-medium`, `audit-heavy`, `git diff --check`. Эти checks не снимают blocker, потому что текущая проблема находится в форматах и документах, которые verifier не покрывает.
* Проверка секретов и локальных данных по доступным файлам, patch и evidence не выявила реальных токенов, приватных ключей, паролей или конфиденциальных локальных абсолютных путей. Совпадения вида `electron2d://` являются URI API manifest, `<repo>` в evidence — placeholder, secret-like строки в tests являются синтетическими fixture-строками, а `Password`/`Secret` в API manifest являются именами UI/API элементов.

Техническая привязка:

* Metadata: `metadata/audit-package.input.json`
* Manifest: `AUDIT-MANIFEST.md`
* Snapshots: `metadata/repo-file-snapshots.json`
* File hashes: `repo-file-hashes.json`
* Patch map: `T-0241.patch`
* Previous verdict files:

  * `repo-after/docs/verdicts/release-management/t-0241-audit-r01.md`
  * `repo-after/docs/verdicts/release-management/t-0241-audit-r02.md`
  * `repo-after/docs/verdicts/release-management/t-0241-audit-r03.md`
  * `repo-after/docs/verdicts/release-management/t-0241-audit-r04.md`
  * `repo-after/docs/verdicts/release-management/t-0241-audit-r05.md`
* Проверенные evidence artifacts:

  * `evidence/T-0241-r06/preflight/build-tool-build/output.txt`
  * `evidence/T-0241-r06/preflight/api-compatibility/output.txt`
  * `evidence/T-0241-r06/preflight/api-compatibility-tests/output.txt`
  * `evidence/T-0241-r06/preflight/audit-loop-stabilization/output.txt`
  * `evidence/T-0241-r06/preflight/api-surface-tests/output.txt`
  * `evidence/T-0241-r06/preflight/api-manifest-check/output.txt`
  * `evidence/T-0241-r06/preflight/docs-check/output.txt`
  * `evidence/T-0241-r06/preflight/docs-verify/output.txt`
  * `evidence/T-0241-r06/preflight/audit-contracts/output.txt`
  * `evidence/T-0241-r06/preflight/audit-followups/output.txt`
  * `evidence/T-0241-r06/preflight/manifests/output.txt`
  * `evidence/T-0241-r06/preflight/licenses/output.txt`
  * `evidence/T-0241-r06/preflight/audit-medium/output.txt`
  * `evidence/T-0241-r06/preflight/audit-heavy/output.txt`
  * `evidence/T-0241-r06/preflight/git-diff-check/output.txt`
  * `evidence/T-0241-r06/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md`
  * `evidence/T-0241-r06/preflight/task-ledger-excerpts/dev-diary-05-06-07-2026-T-0241-excerpts.md`

RISKS_AND_NOTES:

* INFO_NOTE I1

  * Идентификатор: `I1`
  * Где найдено: `repo-after/data/api/electron2d-api-manifest.json:1165-1173`, аналогичные entries повторяются для backing fields других enum types.
  * Проблема: Generated API manifest всё ещё включает CLR enum backing field `value__` как `EnumValue` member с пустым summary.
  * Почему не блокирует текущую задачу: Это уже было найдено в r03 как follow-up и в r06 помечено как tracked-existing на `T-0242`. Текущая задача T-0241 фиксирует root contract и verifier guardrails, а cleanup generated API descriptions/API manifest принадлежит владельцу `T-0242`.
  * Actionable: false
  * Техническая привязка:

    * `Source previous finding`: `docs/verdicts/release-management/t-0241-audit-r03.md` / `FOLLOW_UP_FINDING F1`
    * `Closure note`: `evidence/T-0241-r06/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:136-141`
    * `Verification`: `evidence/T-0241-r06/preflight/audit-followups/output.txt`

CLOSURE_DECISION:

* Задача остаётся открытой. r06 закрывает конкретные release-note fragments из r05 и evidence gap по `docs/documentation/github-wiki-api-reference.md`, но не закрывает общий критерий T-0241 о запрете ручных списков публичных API элементов в доменных документах. Для следующей итерации нужно убрать или переписать оставшиеся manual public API lists в scoped domain docs и расширить verifier/test coverage на фактические форматы: C# declaration fences, method/property lists и prerequisite public type lists в изменённых доменных документах.
