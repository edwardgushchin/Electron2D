# Machine-readable API manifest

Обновлено: 2026-07-06.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0118`, уточнённая root contract `T-0241`.
Обновлено: 2026-07-06.

## Назначение

Electron2D должен поставлять версионированный JSON manifest публичного runtime API. Manifest нужен AI-агентам, CLI, Inspector, GitHub Wiki verifier-ам, генераторам и будущему language service, чтобы отличать подтверждённую публичную поверхность от `Deferred`, `Unsupported`, `Planned` и непроверенных строк без чтения исходников движка.

В этом документе manifest означает машиночитаемый JSON-файл с описанием публичных типов и members. Он не является исходником реализации: файл пересоздаётся генератором из compiled assembly, XML documentation и GitHub Wiki compatibility table.

## Canonical artifact

Canonical tracked artifact:

```text
data/api/electron2d-api-manifest.json
```

Файл должен быть stable JSON: UTF-8 без BOM, LF line endings, отсортированные типы и members, deterministic property order. Любое изменение public API, XML documentation или compatibility status должно либо обновить этот файл, либо привести к падению проверки синхронизации.

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

Manifest projection намеренно отличается от raw runtime symbol names там, где Electron2D показывает более чистую публичную C# surface: callback methods отображаются без ведущего Godot `_` (`Draw` вместо `_Draw`), enum-типы отображаются без внутреннего суффикса `Enum` (`TextureRect.StretchMode` вместо `TextureRect.StretchModeEnum`). Raw `xmlDocId` остаётся неизменным, чтобы документация и source lookup по-прежнему находили compiled symbols.

## Источники данных

Generator обязан читать только проверяемые источники:

- compiled runtime assembly `src/Electron2D/bin/Debug/net10.0/Electron2D.dll`;
- XML documentation file, полученный при build из текущих C# XML comments;
- GitHub Wiki compatibility page `API-Compatibility.md` из локального clone `.github/wiki` или явно переданного пути.

Ручной список public API не допускается как основной источник manifest. Ручными данными могут быть только release-level metadata: `schemaVersion`, `manifestVersion`, `engineVersion`, `profileName` и `godotBaseline`.

## Schema shape

Manifest должен содержать:

- `schemaVersion` со значением `1`;
- `manifestVersion` со значением `0.1-preview`;
- `engineVersion`;
- `profileName` со значением `Electron2D 0.1-preview`;
- `godotBaseline` со значением `4.7-stable`;
- `generatedFrom` с путями к assembly, XML documentation и compatibility page;
- `strictParitySummary` с числовыми полями `missingTypes`, `missingMembers`, `signatureMismatches`, `inheritanceMismatches`, `defaultMismatches`, `unexpectedChanges`;
- `statusSummary` с количеством типов по статусам `supported`, `partial`, `experimental`, `planned`;
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
- `profile` с тем же status/parity contract, что у declaring type, если member не имеет отдельного compatibility override.

Member `kind` должен сохранять C# ABI/reflection форму публичной поверхности: public `const` fields записываются как `Constant`, C# operator overloads записываются как `Operator`, enum values остаются `EnumValue`, обычные public fields остаются `Field`, а static get-only properties остаются `Property`. Для `Constant` и `EnumValue` manifest обязан хранить строковое поле `value`, полученное из compiled constant value через invariant culture. Для predefined value singletons manifest сохраняет ABI `kind` как `Field` или `Property`, но также хранит строковое `value`, если значение является public static readonly/static get-only значением того же value-type declaring type и его можно безопасно прочитать через reflection. Это позволяет class packet generator заполнять секции `constants` и `operators`, а future matrix сможет сравнивать не только имена, но и значения констант, enum members и Godot-style value singletons.

## Статусы и parity

Compatibility status берётся из `API-Compatibility.md` и нормализуется в lowercase:

- `Supported` -> `supported`;
- `Partial` -> `partial`;
- `Experimental` -> `experimental`;
- `Planned` -> `planned`.

Для public types со статусом `supported` поле `profile.parity` должно быть `parity_verified`, а `outOfProfile` должно быть `false`.

Для `partial`, `experimental` и `planned` поле `outOfProfile` должно быть `true`, пока тип не имеет полного evidence по корневому контракту Godot `4.7-stable` или явно утверждённой строки `Deferred`/`Unsupported` через `T-0963`. Это позволяет AI-агенту fail-closed: использовать только подтверждённую публичную поверхность и видеть, почему остальная public surface недоступна как строгий compatibility contract.

Для future strict parity verifier-а `e2d api compare-godot <type>` manifest должен хранить нулевой `strictParitySummary` для supported profile и stable per-type parity fields. CLI adapter реализуется отдельной задачей и не является зависимостью generator-а.

## CLI adapter `e2d api compare-godot`

Команда `e2d api compare-godot <type> --format json` должна читать canonical artifact `data/api/electron2d-api-manifest.json`, а не GitHub Wiki Markdown и не ручной список типов. Команда использует общий CLI envelope и не открывает `ProjectWorkspace`, поэтому `route` должен быть `none`.

Для типа внутри утверждённого профиля output должен содержать:

- `data.mode = api.compareGodot`;
- `data.sourcePath = data/api/electron2d-api-manifest.json`;
- `data.type` с `fullName`, `id`, `profile.status`, `profile.parity` и `profile.outOfProfile`;
- `data.result.status = parity_verified`;
- `data.strictParity` с нулевыми `missingTypes`, `missingMembers`, `signatureMismatches`, `inheritanceMismatches`, `defaultMismatches` и `unexpectedChanges`.

Для типа вне утверждённого профиля команда должна завершиться fail-closed: `succeeded = false`, `exitCode = 1`, `data.result.status = out_of_profile`, diagnostics содержит stable code `E2D-CLI-0002`. Output не должен предлагать alternative API или workaround: агент должен видеть, что тип нельзя использовать как часть строгого `0.1-preview` профиля.

## Generator и verifier

Целевая командная поверхность verifier-а находится в C#-инструменте репозитория:

```bash
dotnet run --project eng/Electron2D.Build -- update api-manifest --check --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki --output .temp/api-manifest/probe.json
```

В этих командах `api-manifest` означает пересоздаваемый JSON-файл `data/api/electron2d-api-manifest.json`, а `wiki-path` указывает на локальный клон Wiki или на файл `API-Compatibility.md`. C#-команда должна поддерживать:

- обычный режим: пересоздаёт `data/api/electron2d-api-manifest.json`;
- `--output <path>`: пишет manifest в заданный файл;
- `--check`: генерирует ожидаемый manifest во временный каталог и сравнивает с целевым файлом;
- `--wiki-path <path>`: читает compatibility table из указанного Wiki clone или файла.

Для `T-0214` единственная целевая поверхность проверки и генерации manifest — C#-маршрут `update api-manifest`. Удаление старых локальных скриптов и зачистка прежних вызовов CI относятся к отдельной `T-0210`.

`--check` должен завершаться ошибкой, если:

- manifest отсутствует;
- manifest устарел относительно public API, XML docs или compatibility page;
- любой public type из compiled assembly отсутствует в manifest;
- любой public type отсутствует в `API-Compatibility.md`;
- mandatory stable ids отсутствуют у types, properties, methods, constructors, fields, events или enum values.

`dotnet run --project eng/Electron2D.Build -- update wiki --check` должен использовать manifest verifier или вызывать его как отдельный шаг, чтобы GitHub Wiki/API reference gate проверял один и тот же public API contract.

CI должен запускать manifest check после checkout GitHub Wiki clone и до consolidated public API documentation audit.

## Критерии приёмки

- `data/api/electron2d-api-manifest.json` генерируется из compiled public surface, XML documentation и `API-Compatibility.md`.
- Manifest содержит stable identifiers для types и members, пригодные для Inspector, signals, runtime-операций и будущего Editor Capability Manifest.
- Manifest содержит machine-readable Godot `4.7-stable` C# parity fields для future `e2d api compare-godot <type>`.
- GitHub Wiki/API verifier или CI падает, если manifest не синхронизирован с public API.
- AI-агент может по manifest отличить `supported` + `parity_verified` профиль от `partial`, `experimental` и `planned` API без чтения исходников.

## Фактическое состояние, ограничения и проверки

Текущая реализация поставляет tracked JSON manifest публичного runtime API:

```text
data/api/electron2d-api-manifest.json
```

Manifest создаётся из compiled assembly, XML documentation и GitHub Wiki compatibility table. В этом документе compiled assembly означает собранный `.dll` файл runtime, а XML documentation — файл, который C# build создаёт из XML comments в исходниках. Manifest не редактируется вручную как источник правды: при изменении public API, XML comments или compatibility status его нужно пересоздать генератором.

## Команды

Обновить manifest из текущей сборки и локального Wiki clone:

```bash
dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki
```

Проверить, что tracked manifest синхронизирован:

```bash
dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki --check
```

Записать manifest во временный файл вместо canonical artifact:

```bash
dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki --output .temp/api-manifest/probe.json
```

`dotnet run --project eng/Electron2D.Build -- update wiki --check --output .github/wiki` также вызывает manifest check. Поэтому GitHub Wiki API reference gate теперь проверяет не только Markdown pages, но и JSON API manifest.

## Что содержит manifest

Текущий файл содержит:

- `schemaVersion = 1`;
- `manifestVersion = 0.1-preview`;
- `engineVersion` из runtime assembly;
- `profileName = Electron2D 0.1-preview`;
- `godotBaseline = 4.7-stable`;
- `generatedFrom` с путями к assembly, XML documentation и `API-Compatibility.md`;
- `strictParitySummary` с нулевыми счётчиками для supported profile boundary;
- `statusSummary` с количеством public types по compatibility status;
- `supportedVariantTypes`;
- `types` с stable identifiers, inheritance, category, summary, profile status и members.

Stable identifiers имеют формы:

```text
electron2d://api/type/Electron2D.Node
electron2d://api/member/Electron2D.Node/Property/Name
electron2d://api/member/Electron2D.Node/Method/AddChild(...)
```

Эти identifiers предназначены для будущего `Editor Capability Manifest`, Inspector properties, signals и runtime operations. Они позволяют внешнему tooling ссылаться на public API без привязки к имени файла документации.

## Compatibility status

Status берётся из GitHub Wiki `API-Compatibility.md`:

- `Supported` становится `supported` и `parity_verified`;
- `Partial` становится `partial` и `not_verified`;
- `Experimental` становится `experimental` и `not_verified`;
- `Planned` становится `planned` и `not_verified`.

Только `supported` entries считаются частью supported/parity-verified public API contract. `partial`, `experimental` и `planned` помечаются `outOfProfile = true`, чтобы AI-агент мог fail-closed: не использовать такой API как подтверждённую часть строгого compatibility contract.

## CI и Wiki

CI запускает manifest check после checkout GitHub Wiki clone:

```bash
dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki --check
```

Затем CI запускает GitHub Wiki API reference check. Wiki generator читает manifest и добавляет на каждую generated type page блок:

```text
Godot 4.7 C# profile compatibility
Profile: Electron2D 0.1-preview
Status: Supported / Parity verified
Out of profile: no
```

Для `partial`, `experimental` и `planned` API тот же блок показывает непроверенный status и `Out of profile: yes`.

## CLI parity verifier

`e2d api compare-godot <type> --format json` читает tracked manifest и возвращает машинный результат проверки одного типа:

```bash
e2d api compare-godot Control --format json
```

Команда использует общий CLI envelope:

- `command = api compare-godot`;
- `route = none`, потому что `ProjectWorkspace` не открывается;
- `data.mode = api.compareGodot`;
- `data.sourcePath = data/api/electron2d-api-manifest.json`;
- `data.type` содержит `fullName`, `id` и `profile`;
- `data.result.status` показывает `parity_verified` или `out_of_profile`;
- `data.strictParity` содержит шесть counters strict parity.

Для поддержанного profile type все strict parity counters равны `0`, `succeeded = true` и `exitCode = 0`. Для type вне утверждённого профиля команда возвращает `succeeded = false`, `exitCode = 1`, `data.result.status = out_of_profile` и diagnostic `E2D-CLI-0002`.
