VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0241` / `r04` как повторная итерация после `r01`, `r02` и `r03`. Область пакета одиночная: `metadata.scopeTaskIds = ["T-0241"]`; combined scope не заявлен.
* Изменение закрывает ранее найденные blockers по `r01`, `r02` и `r03` в узких местах, на которые они указывали: корневой документ больше не содержит старый ручной список `Electron2D.*` public types, карта потребителей добавлена, hidden hash по `docs/cli/e2d-cli.md` больше не меняется, `docs/core-types/variant.md` больше не утверждает четырёхтиповый exported runtime baseline, а строка `Not planned` удалена из таблицы статусов release-документа.
* Принять задачу нельзя. В том же изменённом release-документе `docs/releases/0.1-preview.md` остались ручные code-block списки публичных Godot/Electron2D API элементов: object/scene types, math/variant types, rendering/UI/resource/input/animation/control types и методы lifecycle/node API. Это прямо конфликтует с новым текстом этого же файла, где сказано, что публичная поверхность определяется generated artifacts и задачами публичных классов, а не ручным списком в release note. Также это не закрывается новым verifier-ом: он ловит только bullet-строки вида `- \`Electron2D.*``, но не code fences с bare Godot/C# type names.
* Прошлые verdict-файлы из `metadata.previousVerdictChain` доступны в пакете и прочитаны: `docs/verdicts/release-management/t-0241-audit-r01.md`, `docs/verdicts/release-management/t-0241-audit-r02.md`, `docs/verdicts/release-management/t-0241-audit-r03.md`. Их blockers сопоставлены с `metadata.blockerClosureList`.

Техническая привязка:

* `metadata.taskId`: `T-0241`
* `metadata.iteration`: `r04`
* `metadata.scopeTaskIds`: [`T-0241`]
* `metadata.scopeSummary`: r04 закрывает primary r03 `B1`, обрабатывает r03 `RISKS_AND_NOTES`, сохраняет closure r01 `B1`-`B3` и r02 `B1`.
* `metadata.previousVerdictChain`: [`docs/verdicts/release-management/t-0241-audit-r01.md`, `docs/verdicts/release-management/t-0241-audit-r02.md`, `docs/verdicts/release-management/t-0241-audit-r03.md`]
* `metadata.blockerClosureList`: пять записей closure: r01 `B1`, r01 `B2`, r01 `B3`, r02 `B1`, r03 `B1`.
* Проверенные основные материалы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0241.patch`, `repo-before/`, `repo-after/`, `evidence/T-0241-r04/preflight/**`.

BLOCKERS:

* B1

  * Что не так: В `docs/releases/0.1-preview.md` одновременно утверждается, что публичная поверхность определяется generated artifacts и задачами публичных классов, а не ручным списком в release note, но ниже в этом же файле оставлены ручные списки публичных API элементов. Например, в code fences перечислены `Object`, `RefCounted`, `Resource`, `Node`, `SceneTree`, `PackedScene`, `Node2D`, `CanvasItem`, `Viewport`, `Timer`; затем `Vector2`, `Vector2I`, `Rect2`, `Transform2D`, `Color`, `Mathf`, `RandomNumberGenerator`, `NodePath`, `StringName`, `Callable`, `Signal`, `Rid`, `Variant`; далее отдельными блоками перечислены rendering/UI/resource/input/animation/control types и методы `AddChild`, `RemoveChild`, `GetNode`, `QueueFree`, `EmitSignal`, `Connect`, `Disconnect`. Это ручное определение публичной поверхности/минимального API в root-contract release-документе.
  * Почему это важно: T-0241 является foundation-задачей для Godot `4.7-stable` public API contract. Если release-документ оставляет ручные списки public API элементов, следующие задачи и аудиторы получают второй источник истины рядом с generated API manifest и Wiki compatibility table. Это повторяет класс проблемы r01/r02: контракт говорит «не вручную», но проверяемый документ всё ещё содержит ручной API список. Особенно важно, что r04 заявляет закрытие r03 `F2` через «scanning all root-contract docs», однако фактический scanner не ловит такие code-block списки.
  * Что исправить: Убрать из `docs/releases/0.1-preview.md` ручные списки public API elements или переписать их так, чтобы release note ссылался на generated artifacts (`data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md`, будущие reports `T-0242`/`T-0243`/`T-0244`/`T-0245`) и не выглядел как источник публичной поверхности. Если какие-то списки нужны как исторические/примерные подсистемные ориентиры, это должно быть явно отделено от public API source of truth и не противоречить строке о запрете ручного release-note списка. Verifier нужно расширить так, чтобы он ловил не только bullet `- \`Electron2D.*``, но и fenced code blocks / bare Godot API type names в контекстах public API, baseline, public surface, release public model.
  * Как проверить исправление: Добавить негативный тест, который вставляет в `docs/releases/0.1-preview.md` code fence с bare public type names, например `Object`, `Node`, `Vector2`, `Control`, и проверяет падение `verify api-compatibility` с `E2D-BUILD-API-COMPATIBILITY-CONTRACT-MANUAL-LIST` или новым устойчивым diagnostic. Затем выполнить `dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki`, focused tests `FullyQualifiedName~VerifyApiCompatibility`, `dotnet run --project eng/Electron2D.Build -- update docs --check` и `dotnet run --project eng/Electron2D.Build -- verify docs`.
  * Проверка опровержения: Проверены `docs/releases/0.1-preview.md`, `docs/release-management/api-compatibility.md`, `TASKS-T-0241-excerpt.md`, `RepositoryPolicyVerifiers.cs`, focused tests и evidence. Корневой документ и task excerpt действительно требуют generated source of truth и запрет ручных списков; r04 действительно удаляет `Not planned` и добавляет verifier/test guard для waiver-status. Но release-документ всё ещё содержит code-block списки public API elements, а verifier проходит, потому что `ManualPublicApiListPattern` ловит только строки `- \`Electron2D.*`` и не анализирует code fences с bare public type names.
  * Техническая привязка:

    * `File/symbol`: `repo-after/docs/releases/0.1-preview.md:20`, `repo-after/docs/releases/0.1-preview.md:203-217`, `repo-after/docs/releases/0.1-preview.md:221-242`, `repo-after/docs/releases/0.1-preview.md:248-265`, `repo-after/docs/releases/0.1-preview.md:319-330`, `repo-after/docs/releases/0.1-preview.md:464-476`, `repo-after/docs/releases/0.1-preview.md:671-680`, `repo-after/docs/releases/0.1-preview.md:766-773`, `repo-after/docs/releases/0.1-preview.md:820-839`
    * `Criterion`: `documentation review`, `task compliance review`, `previous blockers closure`, критерий T-0241 о generated source of truth и запрете ручного определения публичной поверхности.
    * `Evidence`: `repo-after/docs/releases/0.1-preview.md:20` говорит, что public surface определяется generated artifacts и не ручным списком в release note; `repo-after/docs/releases/0.1-preview.md:203-217` и последующие code fences перечисляют public API types/methods вручную; `evidence/T-0241-r04/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:66`, `:75`, `:82` запрещают ручное определение публичной поверхности и ручные списки публичных элементов API; `evidence/T-0241-r04/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:130-133` заявляет closure r03 `F2` через broad manual-list guard.
    * `Additional evidence`: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:622-630` включает `docs/releases/0.1-preview.md` в `ManualPublicApiListPaths`, но `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:695-697` ищет только bullet-строки `- \`Electron2D.*``; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:945-963`не содержит обработки fenced code blocks;`repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1039-1058`проверяет release-document regression только для bullet`Electron2D.*`, не для существующего формата code-block списков.
    * `Impact`: root-contract documentation slice остаётся противоречивым и сохраняет ручной API source рядом с generated source of truth; заявленное закрытие manual-list guard неполное.
    * `Fix`: удалить/переписать code-block public API lists в release note и расширить verifier/test coverage на code fences и bare Godot API type names в root-contract public API contexts.
    * `Verification`: `verify api-compatibility --wiki-path .github/wiki`, focused regression test для fenced manual API list in release root-contract document, `update docs --check`, `verify docs`, full-file review `docs/releases/0.1-preview.md`.

EVIDENCE_REVIEW:

* Прочитаны metadata и manifest текущего пакета. `metadata.taskId = T-0241`, `metadata.iteration = r04`, область одиночная, без combined scope. Manifest и metadata согласованно описывают r04 как закрытие r03 `B1`, обработку r03 `RISKS_AND_NOTES`, сохранение closure r01/r02.
* Проверена полнота snapshots для заявленной области. `metadata/repo-file-snapshots.json` содержит 26 repo-owned файлов, все с `fullContentIncluded: true`; изменённые файлы доступны в `repo-after/`, а существовавшие до изменения — в `repo-before/`. `SHA256SUMS.txt` проверен успешно; `repo-file-hashes.json` согласован с итоговыми файлами. Patch использовался только как карта изменений, не как замена full-file review.
* Проверены прошлые verdict-файлы: `repo-after/docs/verdicts/release-management/t-0241-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0241-audit-r02.md`, `repo-after/docs/verdicts/release-management/t-0241-audit-r03.md`. Старые blockers прочитаны и сопоставлены с `metadata.blockerClosureList`.
* По r01 `B1`: закрытие в основном доменном документе подтверждено. В `repo-after/docs/release-management/api-compatibility.md:127-135` baseline больше не содержит ручного списка `Electron2D.*` public types и ссылается на `data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md` и future generated reports.
* По r01 `B2`: закрытие подтверждено. В `repo-after/docs/release-management/api-compatibility.md:45-56` есть `## Карта потребителей контракта` с потребителями, внутренними возможностями, source of truth, первым evidence и ограничениями.
* По r01 `B3`: закрытие подтверждено. Hash `docs/cli/e2d-cli.md` в `repo-before/data/documentation/electron2d-local-docs-index.json` и `repo-after/data/documentation/electron2d-local-docs-index.json` одинаковый: `b2775fb77db080d99492a51a12138a539e1f89b1b171a7f445d22aedb0351d46`. Все изменившиеся source hashes generated docs index имеют полные snapshots в текущем пакете.
* По r02 `B1`: закрытие подтверждено для `docs/core-types/variant.md`. В `repo-after/docs/core-types/variant.md:127-142` документ больше не утверждает четырёхтиповый exported runtime baseline и ссылается на generated API manifest, Wiki compatibility table и `verify api-compatibility`.
* По r03 `B1`: закрытие подтверждено для `Not planned`. В `repo-after/docs/releases/0.1-preview.md:171-178` строка `Not planned` удалена, а намеренный отказ от Godot API описан через утверждённые `Deferred`/`Unsupported` через `T-0963`. Verifier содержит `E2D-BUILD-API-COMPATIBILITY-CONTRACT-WAIVER-STATUS`, а focused test `VerifyApiCompatibilityRejectsUnapprovedWaiverStatusInRootContract` покрывает возврат `| Not planned |`.
* По r03 `F1`: пакет фиксирует `tracked-existing` на `T-0242`, потому что cleanup CLR enum backing field `value__` относится к generated API descriptions/API manifest owner. Это подтверждено task excerpt и `verify audit-followups`.
* По r03 `F2`: closure неполный. Пакет расширил scanner на `docs/releases/0.1-preview.md` и добавил regression для bullet `Electron2D.*`, но фактические manual API lists в release note имеют другой формат: fenced code blocks с bare Godot/C# type names. Они остаются в изменённом root-contract документе.
* Проверены production/tooling изменения: `eng/Electron2D.ApiManifestGenerator/Program.cs`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs`, `src/Electron2D.Cli/ContextPackCli.cs`, `src/Electron2D.ProjectSystem/Templates/ProjectTemplateCreator.cs`. Изменения относятся к generator/verifier/template wording и не затрагивают горячий runtime/game loop path.
* Проверены тесты: `tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Тесты покрывают obsolete root wording, stale T-0241 link, отсутствие consumer map, bullet manual public API list в root document, bullet manual public API list в `docs/core-types/variant.md`, bullet manual public API list в release document и unapproved `Not planned` status. Текущий blocker не закрыт тестами, потому что нет негативного сценария для fenced/bare-name manual API lists.
* Проверены документы и generated artifacts: `docs/release-management/api-compatibility.md`, `docs/core-types/variant.md`, `docs/documentation/api-manifest.md`, `docs/releases/0.1-preview.md`, `docs/release-management/AUDIT-REQUEST.md`, `docs/architecture/agent-native-workflow.md`, `docs/architecture/engine-platform-stack.md`, `docs/editor/godot4-editor-reference.md`, `docs/project-system/static-context-pack.md`, `docs/release-management/project-template.md`, `docs/rendering/texture-resource-baseline.md`, `docs/runtime/project-runtime-runner.md`, `docs/scripting/editor-script-workflow.md`, `data/api/electron2d-api-manifest.json`, `data/documentation/electron2d-local-docs-index.json`, `data/documentation/local-docs-index/documentation.ndjson`, `data/templates/electron2d-empty/AGENTS.md`.
* Проверены evidence preflight artifacts. Все заявленные проверки завершились с exit code `0`: build tool build, `verify api-compatibility`, focused API compatibility tests, audit-loop stabilization, API surface tests, `update api-manifest --check`, `update docs --check`, `verify docs`, `verify audit-contracts`, `verify audit-followups`, `verify manifests`, `verify licenses`, `audit-medium`, `audit-heavy`, `git diff --check`. Эти checks не снимают blocker, потому что текущий verifier не ловит оставшиеся manual API lists в формате, фактически присутствующем в release-документе.
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
* Проверенные evidence artifacts:

  * `evidence/T-0241-r04/preflight/build-tool-build/output.txt`
  * `evidence/T-0241-r04/preflight/api-compatibility/output.txt`
  * `evidence/T-0241-r04/preflight/api-compatibility-tests/output.txt`
  * `evidence/T-0241-r04/preflight/audit-loop-stabilization/output.txt`
  * `evidence/T-0241-r04/preflight/api-surface-tests/output.txt`
  * `evidence/T-0241-r04/preflight/api-manifest-check/output.txt`
  * `evidence/T-0241-r04/preflight/docs-check/output.txt`
  * `evidence/T-0241-r04/preflight/docs-verify/output.txt`
  * `evidence/T-0241-r04/preflight/audit-contracts/output.txt`
  * `evidence/T-0241-r04/preflight/audit-followups/output.txt`
  * `evidence/T-0241-r04/preflight/manifests/output.txt`
  * `evidence/T-0241-r04/preflight/licenses/output.txt`
  * `evidence/T-0241-r04/preflight/audit-medium/output.txt`
  * `evidence/T-0241-r04/preflight/audit-heavy/output.txt`
  * `evidence/T-0241-r04/preflight/git-diff-check/output.txt`
  * `evidence/T-0241-r04/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md`
  * `evidence/T-0241-r04/preflight/task-ledger-excerpts/dev-diary-05-07-2026-T-0241-excerpts.md`

RISKS_AND_NOTES:

* INFO_NOTE I1

  * Идентификатор: `I1`
  * Где найдено: `repo-after/data/api/electron2d-api-manifest.json:1165-1173`, аналогичные entries повторяются для backing fields других enum types.
  * Проблема: Generated API manifest всё ещё включает CLR enum backing field `value__` как `EnumValue` member с пустым summary.
  * Почему не блокирует текущую задачу: Это уже было найдено в r03 как follow-up `F1` и в r04 помечено как `tracked-existing` на `T-0242`. Текущая задача T-0241 фиксирует root contract и verifier guardrails, а cleanup generated API descriptions/API manifest принадлежит владельцу `T-0242`.
  * Actionable: false
  * Техническая привязка:

    * `Source previous finding`: `docs/verdicts/release-management/t-0241-audit-r03.md` / `FOLLOW_UP_FINDING F1`
    * `Closure note`: `evidence/T-0241-r04/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:122-127`
    * `Verification`: `evidence/T-0241-r04/preflight/audit-followups/output.txt`

CLOSURE_DECISION:

* Задача остаётся открытой. r04 закрывает старый blocker по `Not planned` и подтверждает основные прошлые closure-пункты, но root-contract release-документ всё ещё содержит ручные списки public API elements в code fences. Нужно удалить или явно обезвредить эти списки как source of truth и расширить verifier/test coverage на фактический формат оставшейся проблемы: fenced/bare-name API lists в root-contract документации.
