# Machine-readable API manifest

Обновлено: 2026-07-08.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0118`, уточнённая root contract `T-0241` и manual profile contract `T-0990`.
Обновлено: 2026-07-08.

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

Каждая строка `types[]` утверждает решение на уровне типа, а не members. Обязательные поля строки:

- `fullName` - публичное имя Electron2D-типа;
- `godotReference` - имя Godot `4.7-stable` class packet;
- `decision` - одно из `approved`, `deferred`, `unsupported`;
- `rationale` - непустое обоснование решения.

`approved` означает полный Godot `4.7-stable` public C# parity для типа. `deferred` и `unsupported` исключают тип из публичного API текущего релиза. Отсутствующая строка означает отсутствие решения.

Первый профиль `T-0990` намеренно пустой. Пустой профиль валиден как документ, но текущая сборка с exported public types должна падать на API gate до личного утверждения типов пользователем.

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
- `strictParitySummary` с числовыми полями `missingTypes`, `missingMembers`, `signatureMismatches`, `inheritanceMismatches`, `defaultMismatches`, `unexpectedChanges`;
- `statusSummary` с количеством exported types по `supported`, `deferred`, `unsupported` и `unapproved` status;
- `types`, отсортированный по `fullName`.

Каждый type entry должен содержать:

- stable `id` в форме `electron2d://api/type/{FullName}`;
- `fullName`, `namespace`, `name`, `kind`;
- `baseType` и `interfaces`;
- `xmlDocId`;
- `summary`;
- `category`;
- `profile` с `name`, `status`, `parity`, `outOfProfile`, `godotReference`, `notes`;
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

Manifest отражает текущую exported public surface runtime assembly. Manual profile определяет status каждой строки: approved types становятся публичными `supported`, а missing, deferred и unsupported entries остаются `outOfProfile` и служат machine-readable evidence для fail-fast gate.

Для `approved` type:

- `profile.status = supported`;
- `profile.parity = parity_verified`;
- `profile.outOfProfile = false`;
- `profile.godotReference` берётся из manual profile;
- `profile.notes` берётся из `rationale`.

Если compiled runtime assembly экспортирует тип без `approved` строки, `update api-manifest --check` и `verify api-compatibility` должны завершаться fail-fast диагностикой. То же правило действует для `deferred` и `unsupported`: такие решения допустимы в профиле как запись решения, но runtime не имеет права экспортировать их как публичные типы текущего релиза.

Profile verifier должен падать на:

- duplicate `fullName`;
- неизвестный `decision`;
- пустой `rationale`;
- отсутствующий или несуществующий Godot `4.7-stable` class packet в любой строке `types[]` для решений `approved`, `deferred` и `unsupported`;
- exported public type без `approved`.

## CLI adapter `e2d api compare-godot`

Команда `e2d api compare-godot <type> --format json` должна читать generated manifest. Manual profile остаётся upstream source-of-truth для manifest generation, но CLI adapter не перечитывает его напрямую. Команда использует общий CLI envelope и не открывает `ProjectWorkspace`, поэтому `route` должен быть `none`.

Для exported `approved` type output должен содержать:

- `data.mode = api.compareGodot`;
- `data.sourcePath = data/api/electron2d-api-manifest.json`;
- `data.type` с `fullName`, `id`, `profile.status`, `profile.parity` и `profile.outOfProfile`;
- `data.result.status = parity_verified`;
- `data.strictParity` с нулевыми `missingTypes`, `missingMembers`, `signatureMismatches`, `inheritanceMismatches`, `defaultMismatches` и `unexpectedChanges`.

Для unapproved, deferred, unsupported или unknown type команда завершается fail-closed: `succeeded = false`, `exitCode = 1`, `data.result.status = out_of_profile` для известной manifest row вне публичного профиля или `type_not_found` для полностью неизвестного запроса, diagnostics содержит stable code `E2D-CLI-0002`. Output не должен предлагать alternative API или workaround.

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

`verify api-compatibility --wiki-path .github/wiki` должен сверять manual profile, generated manifest, generated Wiki compatibility page и root public API contract docs. Для начального пустого профиля команда ожидаемо падает, если runtime assembly экспортирует public types.

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
- AI-агент может по manifest и manual profile отличить approved parity-verified API от excluded/unknown API без чтения исходников.

## Фактическое состояние, ограничения и проверки

Текущая целевая реализация должна поставлять два tracked API artifact-а:

```text
data/api/electron2d-public-api-profile.json
data/api/electron2d-api-manifest.json
```

Manual profile не генерируется и не заполняется агентом без личного решения пользователя. Manifest создаётся из compiled assembly, XML documentation и manual profile.

Начальный профиль пустой. Поэтому `update api-manifest --check` и `verify api-compatibility --wiki-path .github/wiki` могут намеренно падать на текущей сборке до утверждения первых типов. Такой отказ является правильным fail-fast результатом, а не поводом автоматически добавлять типы в профиль.

## Локальная проверка

```bash
dotnet run --project eng/Electron2D.Build -- update api-manifest --check
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- update wiki --check --output .github/wiki
```

Для пустого профиля итоговые записи дневника и task evidence должны явно отличать ожидаемый profile-gate failure от неожиданных build/test failures.
