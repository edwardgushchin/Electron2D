# Machine-readable API manifest

Обновлено: 2026-07-10.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0118`, уточнённая root contract `T-0241` и manual profile contract `T-0990`.
Обновлено: 2026-07-10.

## Назначение

Electron2D должен поставлять версионированный JSON manifest публичного runtime API. Manifest нужен AI-агентам, CLI, Inspector, GitHub Wiki verifier-ам, генераторам и будущему language service, чтобы отличать подтверждённую публичную поверхность от неутверждённых, отложенных и неподдержанных строк без чтения исходников движка.

После `T-0990` manifest не решает, какие Godot-типы входят в публичный Electron2D API. Единственный источник решения - hand-authored profile:

```text
data/api/electron2d-public-api-profile.json
```

В этом документе manifest означает производный машиночитаемый JSON-файл с описанием exported public types, их members и profile status. Он пересоздаётся генератором из compiled assembly, XML documentation и manual API profile. GitHub Wiki `API-Compatibility.md` является generated output из тех же данных и больше не является входом для manifest generator.

## Canonical artifacts

Hand-authored source of truth:

```text
data/api/electron2d-public-api-profile.json
```

Generated tracked artifact:

```text
data/api/electron2d-api-manifest.json
```

Профиль редактируется только по явному решению пользователя. Manifest должен быть stable JSON: UTF-8 без BOM, LF line endings, отсортированные типы и members, deterministic property order. Любое изменение runtime public surface, XML documentation или profile decision должно либо обновить generated manifest, либо привести к падению проверки синхронизации.

## Manual profile schema

Минимальная форма профиля:

```json
{
  "schemaVersion": 1,
  "release": "0.1-preview",
  "godotBaseline": "4.7-stable",
  "approvalAuthority": "project-owner",
  "types": []
}
```

Каждая строка `types[]` утверждает type-level решение. Member-level решения добавляются только для намеренного Godot subset. Обязательные поля строки:

- `fullName` - публичное имя Electron2D-типа;
- `godotReference` - имя Godot `4.7-stable` class packet;
- `decision` - одно из `approved`, `deferred`, `unsupported`;
- `rationale` - непустое обоснование решения;
- `editorOnly` - необязательный boolean-флаг: `true` означает, что тип утверждён только для editor/tools API и не может попадать в exported game runtime API.
- `godotApiScope` - обязательная для каждого `approved` решения классификация `full|subset`; для `deferred`/`unsupported` поле отсутствует. `full` запрещает subset-контракт, `subset` требует его.
- `godotApiContract` - машинно проверяемый контракт только для `godotApiScope = subset`. Он обязан содержать `scope = subset`, `defaultMemberDecision = deferred|unsupported`, непустой `rationale`, `memberDecisions[]` с Godot selectors и `enumValueDecisions[]` с точными enum/name/value. Если для enum указана хотя бы одна строка, должны быть классифицированы все значения этого Godot enum, а числовые `value` должны совпадать с закреплённым Godot packet.
- `electronApiContract` - машинно проверяемая классификация текущих экспортированных членов subset-типа. Контракт содержит `scope = exportedMembers`, fail-closed `defaultMemberDecision`, непустой `rationale` и точные `memberDecisions[]` по паре `kind`/`name`. Каждое `approved` решение помечает член как `godotMember`, `godotEnumValue` или `electronExtension`; первые два требуют одобренный точный `godotName` из `godotApiContract`, последний запрещает Godot-сопоставление и фиксирует отдельный owner-approved Electron2D API без заявления Godot parity.

`approved` означает утверждение владельцем для публичной runtime/editor поверхности Electron2D. Это не является автоматическим доказательством полного Godot `4.7-stable` parity: поведенческий parity evidence остаётся за owning class tasks and final gates. Намеренно отсутствующий или несовместимый Godot member обязан иметь явное `Deferred`/`Unsupported` решение. Отдельный Electron2D-specific public extension допустим только при точном owner-approved member/value решении `electronExtension`, рабочем поведении и `parity = not_applicable`; такой extension нельзя выдавать за совпадающий Godot member или использовать для молчаливого замещения отсутствующего Godot API. `deferred` и `unsupported` исключают тип из публичного API текущего релиза. Отсутствующая строка означает отсутствие решения.

Исторически первый профиль `T-0990` был пустым. Текущий manual profile уже содержит утверждённые, отложенные и неподдерживаемые решения, поэтому сборка с exported public types должна проходить API gate только для типов с `approved` и падать на любой незакрытый public export.

## Manifest sources

Generator обязан читать только проверяемые источники:

- compiled runtime assembly `src/Electron2D/bin/Debug/net10.0/Electron2D.dll`;
- XML documentation file, полученный при build из текущих C# XML comments;
- manual API profile `data/api/electron2d-public-api-profile.json`.

Generator не читает `.github/wiki/API-Compatibility.md` и не принимает Wiki table как compatibility input. Generated Wiki page сверяется отдельно как output.

## Manifest schema

Manifest должен содержать:

- `schemaVersion = 1`;
- `manifestVersion = 0.1-preview`;
- `engineVersion`;
- `profileName = Electron2D 0.1-preview`;
- `godotBaseline = 4.7-stable`;
- `generatedFrom` с путями к assembly, XML documentation и manual public API profile;
- `strictParityEvidence` со статусом `not_verified` и причиной: manifest отражает утверждённый manual profile, а не доказывает полный strict parity с Godot `4.7-stable`;
- `statusSummary` с количеством exported types по `supported`, `deferred`, `unsupported` и `unapproved` status;
- `types`, отсортированный по `fullName`.

Каждый type entry должен содержать:

- stable `id` в форме `electron2d://api/type/{FullName}`;
- `fullName`, `namespace`, `name`, `kind`;
- `baseType` и `interfaces`;
- `xmlDocId`;
- `summary`;
- `category`;
- `profile` с `name`, `status`, `parity`, `outOfProfile`, `godotReference`, `editorOnly`, `notes`;
- `godotApiScope` для каждого exported approved type и `godotApiContract`, если scope равен `subset`;
- `electronApiContract` у экспортированного subset-типа и `electronApiDecision` у каждого его public member. `profile.parity = not_applicable` допустим только вместе с `electronApiDecision.compatibility = electronExtension`; такой член является отдельным owner-approved Electron2D API и не объявляется Godot member parity или реализацией отсутствующего Godot member;
- `members`, отсортированный по stable member id.

Каждый member entry должен содержать:

- stable `id` в форме `electron2d://api/member/{DeclaringType}/{Kind}/{SignatureKey}`;
- `declaringType`, `name`, `kind`, `signature`, `returnType`;
- `parameters`;
- `xmlDocId`;
- `summary`;
- `profile` с тем же status/parity contract, что у declaring type.

Member `kind` должен сохранять C# ABI/reflection форму публичной поверхности: public `const` fields записываются как `Constant`, C# operator overloads записываются как `Operator`, enum values остаются `EnumValue`, обычные public fields остаются `Field`, а static get-only properties остаются `Property`. Для `Constant` и `EnumValue` manifest обязан хранить строковое поле `value`, полученное из compiled constant value через invariant culture. Для predefined value singletons manifest сохраняет ABI `kind` как `Field` или `Property`, но также хранит строковое `value`, если значение является public static readonly/static get-only значением того же value-type declaring type и его можно безопасно прочитать через reflection.

Manifest projection намеренно отличается от raw runtime symbol names там, где Electron2D показывает более чистую публичную C# surface: callback methods отображаются без ведущего Godot `_`, enum-типы отображаются без внутреннего суффикса `Enum`. Raw `xmlDocId` остаётся неизменным, чтобы документация и source lookup по-прежнему находили compiled symbols.

## Profile decisions and parity

Manifest отражает текущую exported public surface runtime assembly. Manual profile определяет status каждой строки: approved types становятся публичными `supported`, а missing, deferred и unsupported entries остаются `outOfProfile` и служат machine-readable evidence для fail-fast gate. `supported` здесь означает profile-approved runtime surface, not full Godot parity proof.

Для `approved` type:

- `profile.status = supported`;
- `profile.parity = profile_approved`;
- `profile.outOfProfile = false`;
- `profile.godotReference` берётся из manual profile;
- `profile.editorOnly` берётся из manual profile и по умолчанию равен `false`;
- `profile.notes` берётся из `rationale`.

Если compiled runtime assembly экспортирует тип без `approved` строки, `update api-manifest --check` и `verify api-compatibility` должны завершаться fail-fast диагностикой. То же правило действует для `deferred`, `unsupported` и `approved` строк с `editorOnly: true`: такие решения допустимы в профиле как запись решения, но game runtime не имеет права экспортировать их как публичные типы текущего релиза.

Profile verifier должен падать на:

- duplicate `fullName`;
- неизвестный `decision`;
- не-boolean значение `editorOnly`;
- пустой `rationale`;
- отсутствующий или несуществующий Godot `4.7-stable` class packet в любой строке `types[]` для решений `approved`, `deferred` и `unsupported`;
- exported public type без `approved` или с `editorOnly: true`.
- exported member subset-типа без точного `electronApiContract.memberDecisions[]` решения, с решением `deferred`/`unsupported`, с несовпадающим generated `electronApiDecision` или с ложным Godot-сопоставлением вместо typed `electronExtension`.

## CLI adapter `e2d api compare-godot`

Команда `e2d api compare-godot <type> --format json` проверяет ровно один запрошенный тип. Решение она напрямую читает из canonical manual profile `data/api/electron2d-public-api-profile.json`, а generated manifest `data/api/electron2d-api-manifest.json` использует только для текущей экспортированности типа и `strictParityEvidence`. Identity сначала разрешается точным регистрозависимым C#-именем; регистронезависимый fallback допустим только при единственном совпадении profile rows. Manifest затем ищется по точному `fullName` уже выбранной строки профиля, поэтому пары `ResourceUID`/`ResourceUid` и `RID`/`Rid` не наследуют друг у друга `id` или `availability.exported`. Команда использует общий CLI envelope и не открывает `ProjectWorkspace`, поэтому `route` должен быть `none`.

Для exported `approved` type output должен содержать:

- `data.mode = api.compareGodot`;
- `data.sourcePath = data/api/electron2d-api-manifest.json`;
- `data.type` с `fullName`, `id`, `profile.status`, `profile.parity` и `profile.outOfProfile`;
- `data.result.status = profile_approved`;
- `data.parityEvidence.status = not_verified`, пока нет отдельного strict comparison evidence для полного Godot `4.7-stable` parity.

Для решений `deferred` и `unsupported` команда завершается fail-closed: `succeeded = false`, `exitCode = 1`, а `data.result.status` сохраняет точное решение manual profile. Для имени, которого нет в manual profile, возвращается `type_not_found`. Diagnostics содержит stable code `E2D-CLI-0002`; output не должен предлагать alternative API или workaround.

## Generator and verifier commands

Целевая командная поверхность verifier-а находится в C#-инструменте репозитория:

```bash
dotnet run --project eng/Electron2D.Build -- update api-manifest
dotnet run --project eng/Electron2D.Build -- update api-manifest --check
dotnet run --project eng/Electron2D.Build -- update api-manifest --output .temp/api-manifest/probe.json
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- update wiki --check --output .github/wiki
```

`update api-manifest` не требует `--wiki-path`, потому Wiki больше не является source input. `verify api-compatibility --wiki-path` сохраняет параметр, потому сверяет generated Wiki clone как output.

`update api-manifest --check` должен завершаться ошибкой, если:

- manifest отсутствует;
- manifest устарел относительно compiled public API, XML docs или manual profile;
- manual profile отсутствует или не проходит schema/profile validation;
- любой exported public type не имеет `approved` решения;
- mandatory stable ids отсутствуют у types, properties, methods, constructors, fields, events или enum values.

`verify api-compatibility --wiki-path .github/wiki` должен сверять manual profile, generated manifest, generated Wiki compatibility page и root public API contract docs. Для текущего заполненного профиля команда должна проходить, если каждый exported runtime public type имеет `approved` решение и не помечен `editorOnly`.

`update wiki --check --output .github/wiki` должен проверять, что `API-Compatibility.md` с generated marker совпадает с output из manual profile and current API artifacts. `update wiki --check` без `--output` создаёт временный expected Wiki tree и не читает `.github/wiki/API-Compatibility.md` как источник.

## Generated class packets

`T-0242` использует manifest как Electron2D-side вход для class packets:

```bash
dotnet run --project eng/Electron2D.Build -- api generate-class-packets
dotnet run --project eng/Electron2D.Build -- api generate-class-packets --check
```

Derived artifacts:

- `data/api/electron2d/classes/<ClassName>.api.json`;
- `data/api/electron2d/index/classes.json`.

Per-class Markdown рядом с JSON не генерируется. `--check` должен падать при stale `*.api.md`, потому что единственный tracked packet format для этого slice - JSON.

## GitHub Wiki output

`API-Compatibility.md` публикуется в GitHub Wiki как generated Markdown report. Он должен иметь generated marker, status legend и current public runtime surface, построенный из manual profile and generated manifest. Таблица не является ручным источником truth и не должна использоваться для изменения manifest.

## Критерии приёмки

- `data/api/electron2d-public-api-profile.json` является единственным source-of-truth для решений о публичных типах.
- `data/api/electron2d-api-manifest.json` генерируется из compiled public surface, XML documentation и manual profile.
- Generator и verifier fail-fast запрещают exported public types без `approved`.
- Manifest содержит stable identifiers и Godot `4.7-stable` parity fields для CLI, Inspector, signals, runtime operations и будущего Editor Capability Manifest.
- GitHub Wiki/API verifier падает, если manifest, manual profile или generated Wiki output не синхронизированы.
- AI-агент может по manifest и manual profile отличить approved public surface API от excluded/unknown API без чтения исходников; full parity evidence проверяется отдельными class tasks and final gates.

## Фактическое состояние, ограничения и проверки

Текущая целевая реализация должна поставлять два tracked API artifact-а:

```text
data/api/electron2d-public-api-profile.json
data/api/electron2d-api-manifest.json
```

Manual profile не генерируется и не заполняется агентом без личного решения пользователя. Manifest создаётся из compiled assembly, XML documentation и manual profile.

Текущий manual profile заполнен решениями владельца API. Поэтому `update api-manifest --check` и `verify api-compatibility --wiki-path .github/wiki` должны проходить на синхронизированной сборке и fail-fast только при расхождении compiled public surface, generated manifest, manual profile или Wiki output.

## Локальная проверка

```bash
dotnet run --project eng/Electron2D.Build -- update api-manifest --check
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- update wiki --check --output .github/wiki
```

Итоговые записи дневника и task evidence должны явно отличать profile-gate failure от build/test failures: первый означает несинхронизированный public API contract, а не допустимое текущее состояние.
