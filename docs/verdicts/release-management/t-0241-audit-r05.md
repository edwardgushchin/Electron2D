VERDICT: NEEDS_FIXES

TASK_ASSESSMENT:

* Проверен пакет `T-0241` / `r05` как повторная итерация после `r01`, `r02`, `r03` и `r04`. Область пакета одиночная: `metadata.scopeTaskIds = ["T-0241"]`; combined scope не заявлен.
* Изменение действительно закрывает значительную часть прошлых замечаний: root contract больше не использует старую формулировку 2D-профиля, `docs/release-management/api-compatibility.md` содержит карту потребителей, generated docs index не меняет скрыто `docs/cli/e2d-cli.md`, `docs/core-types/variant.md` больше не объявляет четырёхтиповый exported runtime baseline, строка `Not planned` удалена, а многие прежние fenced public API lists в `docs/releases/0.1-preview.md` заменены ссылками на generated artifacts.
* Принять задачу нельзя. В `docs/releases/0.1-preview.md` всё ещё остались ручные public API fragments: code fences с `RenderingServer.*`, `RenderingDeviceFeatures.*`, marker attributes `[Export]` / `[Signal]` / `[Tool]`, а также inline baseline-параграфы, вручную перечисляющие public API classes/members для physics, animation и audio. Это противоречит текущему тексту того же release-документа и критерию T-0241, что публичная поверхность определяется generated artifacts, а не release note.
* Дополнительно текущая реализация verifier-а считает `docs/documentation/github-wiki-api-reference.md` root-contract документом, но полный snapshot этого файла отсутствует в audit package. Поэтому нельзя выполнить full-file review всей root-contract поверхности, которую сам verifier объявляет проверяемой.
* Прошлые verdict-файлы из `metadata.previousVerdictChain` доступны и прочитаны: `docs/verdicts/release-management/t-0241-audit-r01.md`, `docs/verdicts/release-management/t-0241-audit-r02.md`, `docs/verdicts/release-management/t-0241-audit-r03.md`, `docs/verdicts/release-management/t-0241-audit-r04.md`. Их blockers сопоставлены с `metadata.blockerClosureList`.

Техническая привязка:

* `metadata.taskId`: `T-0241`
* `metadata.iteration`: `r05`
* `metadata.scopeTaskIds`: [`T-0241`]
* `metadata.scopeSummary`: r05 закрывает primary r04 `B1`, сохраняет r01-r03 closures, заявляет отсутствие fenced/manual public API lists в `docs/releases/0.1-preview.md` и broader manual-list verifier strategy.
* `metadata.previousVerdictChain`: [`docs/verdicts/release-management/t-0241-audit-r01.md`, `docs/verdicts/release-management/t-0241-audit-r02.md`, `docs/verdicts/release-management/t-0241-audit-r03.md`, `docs/verdicts/release-management/t-0241-audit-r04.md`]
* `metadata.blockerClosureList`: шесть записей closure: r01 `B1`, r01 `B2`, r01 `B3`, r02 `B1`, r03 `B1`, r04 `B1`.
* Проверенные основные материалы: `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `T-0241.patch`, `repo-before/`, `repo-after/`, `evidence/T-0241-r05/preflight/**`.

BLOCKERS:

* B1

  * Что не так: `docs/releases/0.1-preview.md` всё ещё вручную задаёт public API elements в release note. Сам файл на строке 20 говорит, что публичная поверхность определяется generated artifacts и задачами публичных классов, а не ручным списком в release note. Но ниже остались code fences с public API names: `RenderingServer.CurrentProfile`, `RenderingServer.HasFeature(...)`, `RenderingDeviceFeatures.CustomShaders`, `RenderingDeviceFeatures.RenderTargets`, `RenderingDeviceFeatures.AnisotropicFiltering`, а также marker attributes `[Export]`, `[Signal]`, `[Tool]`. Кроме того, в prose-baseline параграфах вручную перечислены public API classes/members для physics, animation и audio: `PhysicsServer2D`, `StaticBody2D`, `RigidBody2D`, `CharacterBody2D`, `Area2D`, `CollisionShape2D`, `AnimationPlayer`, `Tween`, `AudioServer`, `AudioStreamPlayer`, `AudioStreamPlayer2D` и другие.
  * Почему это важно: T-0241 — foundation-задача для Godot `4.7-stable` public API compatibility contract. Если root-contract release-документ продолжает объявлять конкретные public API names/members вручную, следующие public API задачи и аудиторы получают второй источник истины рядом с `data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md` и будущими reports `T-0242`/`T-0243`/`T-0244`/`T-0245`. Это не косметика: часть этих имён уже является public API в generated manifest, например `Electron2D.RenderingServer`, `Electron2D.ExportAttribute`, `Electron2D.SignalAttribute`, `Electron2D.ToolAttribute`, `Electron2D.PhysicsServer2D` и `Electron2D.AudioServer`.
  * Что исправить: Убрать или переписать оставшиеся public API fragments в `docs/releases/0.1-preview.md`. Release note может описывать требуемое поведение и подсистемные возможности, но точные class/member/attribute names и signatures должны ссылаться на generated artifacts и owner-задачи, а не задаваться вручную. Verifier нужно расширить так, чтобы он ловил не только старые fenced lists с `Object`/`Node`/`Vector2`, но и текущие форматы: `RenderingServer.*`, marker attributes, inline baseline-параграфы с плотными public API lists и любые public names, взятые из generated manifest.
  * Как проверить исправление: Добавить негативные тесты, которые вставляют в `docs/releases/0.1-preview.md` текущие проблемные форматы: fenced block с `RenderingServer.CurrentProfile`, fenced block с `[Export]` / `[Signal]` / `[Tool]`, и inline baseline-параграф с несколькими generated public type names. Эти тесты должны падать с `E2D-BUILD-API-COMPATIBILITY-CONTRACT-MANUAL-LIST`. Затем выполнить `dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki`, focused tests `FullyQualifiedName~VerifyApiCompatibility`, `dotnet run --project eng/Electron2D.Build -- update docs --check` и `dotnet run --project eng/Electron2D.Build -- verify docs`.
  * Проверка опровержения: Проверены `docs/releases/0.1-preview.md`, `docs/release-management/api-compatibility.md`, `TASKS-T-0241-excerpt.md`, generated API manifest, verifier code, focused tests и evidence. r05 действительно удалил прежние большие fenced lists из r04 и добавил regression для `Object`/`Node`/`Vector2`/`Control`. Но текущий документ всё ещё содержит другие fenced/manual public API fragments, а verifier их не ловит: `ManualPublicApiFenceNames` не включает `RenderingServer`, `RenderingDeviceFeatures` и marker attributes, а текущий parser не проверяет inline baseline-параграфы.
  * Техническая привязка:

    * `File/symbol`: `repo-after/docs/releases/0.1-preview.md:20`, `repo-after/docs/releases/0.1-preview.md:362-375`, `repo-after/docs/releases/0.1-preview.md:460-466`, `repo-after/docs/releases/0.1-preview.md:627`, `repo-after/docs/releases/0.1-preview.md:657`, `repo-after/docs/releases/0.1-preview.md:718`
    * `Criterion`: `documentation review`, `task compliance review`, `previous blockers closure`, критерий T-0241 о generated source of truth и запрете ручного переписывания публичных элементов API.
    * `Evidence`: `evidence/T-0241-r05/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:66`, `:75`, `:82` запрещают ручное определение публичной поверхности и ручные списки публичных элементов API; `repo-after/docs/releases/0.1-preview.md:362-375` и `:460-466` содержат code fences с API names; `repo-after/docs/releases/0.1-preview.md:627`, `:657`, `:718` содержат manual baseline lists public API classes/members; `repo-after/data/api/electron2d-api-manifest.json:11866`, `:23425`, `:2571`, `:27792`, `:30847`, `:37393` подтверждает, что часть этих имён уже является public API manifest entries.
    * `Additional evidence`: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:665-790` задаёт hardcoded `ManualPublicApiFenceNames`, где нет `RenderingServer`, `RenderingDeviceFeatures`, `ExportAttribute`, `SignalAttribute`, `ToolAttribute`; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:1089-1157` проверяет только limited fenced/bullet formats; `repo-after/tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs:1062-1085` тестирует только fenced block с `Object`, `Node`, `Vector2`, `Control`.
    * `Impact`: r04 `B1` закрыт неполно; root-contract release-документ остаётся вторым ручным источником публичной поверхности.
    * `Fix`: удалить/переписать residual public API fragments и расширить verifier/test coverage на generated public names, marker attributes и inline manual lists.
    * `Verification`: `verify api-compatibility --wiki-path .github/wiki`, focused regression tests for residual release-note API fragments, `update docs --check`, `verify docs`, full-file review `docs/releases/0.1-preview.md`.

* B2

  * Что не так: `RepositoryPolicyVerifiers` объявляет `docs/documentation/github-wiki-api-reference.md` root-contract документом и включает его в root contract, manual public API list и waiver-status проверки. Но полный snapshot этого файла отсутствует в `repo-after/`, `repo-before/`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json` и inventory пакета. В архиве есть только generated docs index entry с source path/hash.
  * Почему это важно: r05 заявляет broader manual-list verifier strategy для root-contract docs. Без полного snapshot одного из этих root-contract docs внешний аудит не может проверить, нет ли там тех же manual public API fragments, `Not planned`-подобных waiver-statuses или иных противоречий root contract. Успешный локальный `verify api-compatibility` evidence не заменяет full-file review: B1 уже показывает, что текущий verifier пропускает фактический формат manual API fragments.
  * Что исправить: Включить `docs/documentation/github-wiki-api-reference.md` в `repoFileAllowlist`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `repo-before/`, `repo-after/` и manifest inventory, если он остаётся root-contract документом. Альтернатива — убрать его из `RootContractPaths`, `ManualPublicApiListPaths` и `RootContractWaiverStatusPaths`, если текущая T-0241 итерация не должна заявлять его как часть root-contract surface.
  * Как проверить исправление: Проверить, что каждый путь из root-contract verifier path arrays имеет полный snapshot в audit package или явно исключён из текущей области. Затем повторить `verify api-compatibility --wiki-path .github/wiki`, focused tests, `update docs --check` и `verify docs`.
  * Проверка опровержения: Проверены `AUDIT-MANIFEST.md`, `metadata/audit-package.input.json`, `repo-file-hashes.json`, `metadata/repo-file-snapshots.json`, `repo-before/`, `repo-after/`, generated docs index и verifier code. `docs/documentation/github-wiki-api-reference.md` отсутствует как полный файл, но присутствует в hardcoded root-contract verifier paths.
  * Техническая привязка:

    * `File/symbol`: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:613-638`, отсутствующий snapshot `docs/documentation/github-wiki-api-reference.md`
    * `Criterion`: `evidence gap`, `full file review`, `documentation review`, `scope scanning`, полнота snapshots для важных root-contract documentation files.
    * `Evidence`: `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:613-620` включает `docs/documentation/github-wiki-api-reference.md` в `RootContractPaths`; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:622-630` включает его в `ManualPublicApiListPaths`; `repo-after/eng/Electron2D.Build/RepositoryPolicyVerifiers.cs:632-638` включает его в `RootContractWaiverStatusPaths`; `repo-after/data/documentation/electron2d-local-docs-index.json:95-96` и `repo-after/data/documentation/local-docs-index/documentation.ndjson:22` подтверждают source path; `AUDIT-MANIFEST.md:13-41`, `AUDIT-MANIFEST.md:101-127`, `repo-file-hashes.json` и `metadata/repo-file-snapshots.json` не содержат полного snapshot этого файла.
    * `Impact`: невозможно выполнить полный аудит root-contract docs, которые текущая реализация сама считает частью проверяемого контракта.
    * `Fix`: включить полный файл в audit package или убрать его из root-contract verifier paths и заявленной root-contract области.
    * `Verification`: snapshot completeness check по root-contract path arrays, full-file review `docs/documentation/github-wiki-api-reference.md`, `verify api-compatibility --wiki-path .github/wiki`, focused tests, `update docs --check`, `verify docs`.

EVIDENCE_REVIEW:

* Прочитаны metadata и manifest текущего пакета. `metadata.taskId = T-0241`, `metadata.iteration = r05`, область одиночная, без combined scope. Manifest и metadata согласованно описывают r05 как закрытие r04 `B1`, сохранение r01-r03 closures, обработку r03 follow-up `F1` как tracked-existing на `T-0242` и profile label cleanup.
* Проверена техническая целостность архива: `SHA256SUMS.txt` сходится; `repo-file-hashes.json` сходится с `repo-after/`; `metadata/repo-file-snapshots.json` содержит 27 repo-owned entries, все заявленные entries имеют `fullContentIncluded: true`. Отдельный недостаток полноты по root-contract file `docs/documentation/github-wiki-api-reference.md` вынесен в B2.
* Проверены прошлые verdict-файлы: `repo-after/docs/verdicts/release-management/t-0241-audit-r01.md`, `repo-after/docs/verdicts/release-management/t-0241-audit-r02.md`, `repo-after/docs/verdicts/release-management/t-0241-audit-r03.md`, `repo-after/docs/verdicts/release-management/t-0241-audit-r04.md`. Старые blockers прочитаны и сопоставлены с `metadata.blockerClosureList`.
* По r01 `B1`: закрытие в основном доменном документе подтверждено. `docs/release-management/api-compatibility.md` больше не содержит старый ручной список `Electron2D.*` public types и указывает на generated source of truth.
* По r01 `B2`: закрытие подтверждено. В `docs/release-management/api-compatibility.md:45-56` есть `## Карта потребителей контракта` с потребителями, внутренними возможностями, source of truth, первым evidence и ограничениями.
* По r01 `B3`: закрытие подтверждено. Hash `docs/cli/e2d-cli.md` в generated docs index не меняется между `repo-before` и `repo-after`, а все изменившиеся source hashes generated docs index имеют snapshots в текущей allowlist-области.
* По r02 `B1`: закрытие подтверждено для прежнего четырёхтипового baseline в `docs/core-types/variant.md`. Документ больше не утверждает, что exported runtime baseline содержит только четыре типа, и ссылается на generated API manifest, Wiki compatibility table и `verify api-compatibility`.
* По r03 `B1`: закрытие подтверждено для `Not planned`. В release status table больше нет строки `Not planned`, а verifier/test coverage содержит `E2D-BUILD-API-COMPATIBILITY-CONTRACT-WAIVER-STATUS`.
* По r04 `B1`: закрытие неполное. Большие fenced lists из r04 действительно удалены, но residual manual public API fragments остались в других formats; это вынесено в текущий B1.
* Проверены production/tooling изменения: `eng/Electron2D.ApiManifestGenerator/Program.cs`, `eng/Electron2D.Build/RepositoryPolicyVerifiers.cs`, `src/Electron2D.Cli/ContextPackCli.cs`, `src/Electron2D.ProjectSystem/Templates/ProjectTemplateCreator.cs`. Изменения относятся к generator/verifier/template wording и не затрагивают горячий runtime/game loop path.
* Проверены тесты: `tests/Electron2D.Tests.Integration/ApiManifestTests.cs`, `tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs`. Тесты покрывают obsolete root wording, stale T-0241 link, consumer map, bullet manual public API lists, fenced list с `Object`/`Node`/`Vector2`/`Control`, forbidden legacy public type и `Not planned`. Текущие B1/B2 этими тестами не закрыты.
* Проверены документы и generated artifacts: `docs/release-management/api-compatibility.md`, `docs/core-types/variant.md`, `docs/documentation/api-manifest.md`, `docs/releases/0.1-preview.md`, `docs/release-management/AUDIT-REQUEST.md`, `docs/architecture/agent-native-workflow.md`, `docs/architecture/engine-platform-stack.md`, `docs/editor/godot4-editor-reference.md`, `docs/project-system/static-context-pack.md`, `docs/release-management/project-template.md`, `docs/rendering/texture-resource-baseline.md`, `docs/runtime/project-runtime-runner.md`, `docs/scripting/editor-script-workflow.md`, `data/api/electron2d-api-manifest.json`, `data/documentation/electron2d-local-docs-index.json`, `data/documentation/local-docs-index/documentation.ndjson`, `data/templates/electron2d-empty/AGENTS.md`.
* Проверены evidence preflight artifacts. Все заявленные проверки завершились с exit code `0`: build tool build, `verify api-compatibility`, focused API compatibility tests, audit-loop stabilization, API surface tests, `update api-manifest --check`, `update docs --check`, `verify docs`, `verify audit-contracts`, `verify audit-followups`, `verify manifests`, `verify licenses`, `audit-medium`, `audit-heavy`, `git diff --check`. Эти checks не снимают blockers: B1 находится в формате, который verifier не ловит, а B2 не может быть снят без полного snapshot отсутствующего root-contract файла.
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
* Проверенные evidence artifacts:

  * `evidence/T-0241-r05/preflight/build-tool-build/output.txt`
  * `evidence/T-0241-r05/preflight/api-compatibility/output.txt`
  * `evidence/T-0241-r05/preflight/api-compatibility-tests/output.txt`
  * `evidence/T-0241-r05/preflight/audit-loop-stabilization/output.txt`
  * `evidence/T-0241-r05/preflight/api-surface-tests/output.txt`
  * `evidence/T-0241-r05/preflight/api-manifest-check/output.txt`
  * `evidence/T-0241-r05/preflight/docs-check/output.txt`
  * `evidence/T-0241-r05/preflight/docs-verify/output.txt`
  * `evidence/T-0241-r05/preflight/audit-contracts/output.txt`
  * `evidence/T-0241-r05/preflight/audit-followups/output.txt`
  * `evidence/T-0241-r05/preflight/manifests/output.txt`
  * `evidence/T-0241-r05/preflight/licenses/output.txt`
  * `evidence/T-0241-r05/preflight/audit-medium/output.txt`
  * `evidence/T-0241-r05/preflight/audit-heavy/output.txt`
  * `evidence/T-0241-r05/preflight/git-diff-check/output.txt`
  * `evidence/T-0241-r05/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md`
  * `evidence/T-0241-r05/preflight/task-ledger-excerpts/dev-diary-05-06-07-2026-T-0241-excerpts.md`

RISKS_AND_NOTES:

* INFO_NOTE I1

  * Идентификатор: `I1`
  * Где найдено: `repo-after/data/api/electron2d-api-manifest.json:1165-1173`, аналогичные entries повторяются для backing fields других enum types.
  * Проблема: Generated API manifest всё ещё включает CLR enum backing field `value__` как `EnumValue` member с пустым summary.
  * Почему не блокирует текущую задачу: Это уже было найдено в r03 как follow-up `F1` и в r05 помечено как tracked-existing на `T-0242`. Текущая задача T-0241 фиксирует root contract и verifier guardrails, а cleanup generated API descriptions/API manifest принадлежит владельцу `T-0242`.
  * Actionable: false
  * Техническая привязка:

    * `Source previous finding`: `docs/verdicts/release-management/t-0241-audit-r03.md` / `FOLLOW_UP_FINDING F1`
    * `Closure note`: `evidence/T-0241-r05/preflight/task-ledger-excerpts/TASKS-T-0241-excerpt.md:129-134`
    * `Verification`: `evidence/T-0241-r05/preflight/audit-followups/output.txt`

CLOSURE_DECISION:

* Задача остаётся открытой. r05 закрывает часть r04 blocker-а, но root-contract release-документ всё ещё содержит ручные public API fragments в форматах, которые текущий verifier не ловит, а один из документов, объявленных verifier-ом root-contract документом, отсутствует в полном snapshot пакета. Для следующей итерации нужно удалить или обезвредить остаточные manual API fragments, усилить verifier на текущие форматы и включить полный snapshot `docs/documentation/github-wiki-api-reference.md` либо исключить этот путь из заявленной root-contract поверхности.
