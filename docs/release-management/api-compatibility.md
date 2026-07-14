# Таблица совместимости Electron2D API

Обновлено: 2026-07-06.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: корневой контракт совместимости публичного API.
Задача: `T-0241`.
Обновлено: 2026-07-06.

## Цель

Для `0.1-preview` публичный API Electron2D должен следовать полному контракту совместимости Godot `4.7-stable` для утверждённой публичной поверхности Electron2D. После `T-0990` единственный источник решения о том, какие Godot-типы входят в публичный Electron2D API, - ручной JSON-профиль `data/api/electron2d-public-api-profile.json`. Generated artifacts больше не выбирают публичную поверхность: они только проецируют, проверяют и публикуют уже утверждённые решения из профиля.

Корневое целевое правило: каждая публичная сущность, которая входит в финально принятую публичную поверхность Electron2D, должна совпадать с Godot `4.7-stable` не только по имени, сигнатуре, свойствам, сигналам, enum и constants, но и по наблюдаемому поведению, значениям по умолчанию, ошибкам, жизненному циклу, платформенным ограничениям и ожиданиям разработчика. Manual API profile фиксирует owner decision о допустимой поверхности, но `approved` не является автоматическим доказательством полного parity; поведенческий evidence остаётся за owning class tasks and final gates. Намеренно отсутствующий или несовместимый Godot member обязан иметь явное `Deferred`/`Unsupported` решение. Отдельный Electron2D-specific public extension допустим только при точном owner-approved member/value решении `electronExtension`, рабочем поведении и `parity = not_applicable`; такой extension нельзя выдавать за совпадающий Godot member или использовать для молчаливого замещения отсутствующего Godot API. Каждая `approved` строка обязана структурно выбрать `godotApiScope = full|subset`. Для `full` subset-контракт запрещён; для `subset` обязателен `godotApiContract` с `scope = subset`, fail-closed `defaultMemberDecision` и явными Godot member/enum value decisions. Если subset-тип уже экспортирован, `electronApiContract` отдельно классифицирует каждый его фактический public member как точное `godotMember`, `godotEnumValue` или отдельный owner-approved `electronExtension`. Отсутствие такого решения не означает неявное одобрение Electron2D или Godot API.

Все публичные типы runtime assembly должны быть отражены в ручном профиле как `approved`. Отсутствующая строка означает отсутствие утверждения и должна ломать API-проверки. Профиль допускает только решения:

- `approved` - тип утверждён владельцем для публичной runtime/editor поверхности текущего релиза; full Godot `4.7-stable` public C# parity должен доказываться owning class tasks and final gates before acceptance;
- `deferred` - тип не входит в публичный API текущего релиза, но может быть рассмотрен позже;
- `unsupported` - тип намеренно не входит в публичный API текущего релиза.

`deferred`, `unsupported` и `approved` строки с `editorOnly: true` не являются допустимыми exported game runtime public types. Если runtime assembly экспортирует такой тип, проверка должна падать так же, как для отсутствующей строки.

## Корневой контракт совместимости Godot 4.7

- Входные данные Godot закреплены на `4.7-stable`. Официальная справка, XML-описания классов, export API и C# surface становятся источником истины только через generated artifacts.
- `T-0242` владеет generated API descriptions и указателями на источники Godot.
- `T-0243` владеет отчётами о расхождениях публичной поверхности и правилами падения проверок.
- `T-0244` владеет behavior evidence: проверками наблюдаемого поведения, ошибок, жизненного цикла и граничных случаев.
- `T-0245` владеет per-class ready gate и запрещает принимать public class task без полного evidence или утверждённого исключения.
- Manual API profile является единственным путём утверждения `Deferred` и `Unsupported`. Такие строки должны быть видны в API/behavior reports, generated Wiki и финальном gate `T-0980`.
- Публичный путь для пользовательских скриптов - только C#. GDScript, runtime Godot Script, visual scripting и прямое копирование Godot C# bindings не входят в публичный authoring path.
- Публичный shader source language - HLSL. Godot Shader Language, GLSL и MSL не являются публичными authoring languages; платформенные результаты создаются внутренними shader/rendering задачами.
- Обязательные платформенные проверки для `0.1-preview`: Windows/Linux/macOS требуют runtime+editor diagnostics; Android/iOS/WebAssembly browser требуют runtime/export diagnostics без editor surface.
- `EditorOnly` public/API rows не считаются неподдержанными по умолчанию: они сохраняются как future editor-scope и получают runtime-only diagnostics только там, где editor surface неприменима.

## Карта потребителей контракта

| Потребитель / задача | Внутренняя возможность | Source of truth | Первое evidence | Ограничение |
| --- | --- | --- | --- | --- |
| `T-0990` manual API profile | Ручное утверждение включённых, отложенных и неподдержанных Godot-типов | `data/api/electron2d-public-api-profile.json` | profile verifier, `update api-manifest --check`, `verify api-compatibility --wiki-path .github/wiki` | Агент не добавляет `approved`, `deferred` или `unsupported` без явного решения пользователя. |
| `T-0242` generated API descriptions | Сгенерированные описания публичной поверхности и ссылки на Godot `4.7-stable` | Manual API profile, Godot packets, `data/api/electron2d-api-manifest.json` | `update api-manifest --check`, `verify api-compatibility --wiki-path .github/wiki` | Не выбирает публичную поверхность и не читает Wiki как источник статусов. |
| `T-0243` API diff checks | Отчёты о расхождениях публичной поверхности и fail-closed правила | Manual API profile, generated manifest, future diff reports | `verify api-compatibility --wiki-path .github/wiki` | Любое расхождение получает generated status/evidence, а не ручной waiver. |
| `T-0244` behavior evidence | Проверки наблюдаемого поведения, ошибок, lifecycle и defaults | Behavior test reports и per-type specifications | Первый behavior test/verifier конкретного public class task | Behavior gap не закрывается совпадением сигнатур. |
| `T-0245` per-class ready gate | Готовность каждого public class task к acceptance | Generated API descriptions, behavior evidence, documentation evidence | Class-ready verifier/focused tests конкретной задачи | Public class не принимается без полного evidence или утверждённого исключения. |
| Manual profile exceptions | Утверждение `Deferred`/`Unsupported` строк | `data/api/electron2d-public-api-profile.json` | `verify api-compatibility --wiki-path .github/wiki` | Исключение должно быть явно видно в API/behavior reports и generated Wiki. |
| `T-0980` final gate | Финальная проверка root contract перед release | Manual API profile, manifest, generated Wiki, exception reports, behavior reports | Release gate verifier | `EditorOnly` rows сохраняются как future editor-scope, mobile/web остаются runtime-only. |
| Build tool `verify api-compatibility` | Локальная fail-closed проверка root contract | Tracked docs, manual API profile, API manifest, generated Wiki clone | `RepositoryBuildToolTests.VerifyApiCompatibility*` | Запрещает unapproved exported types, stale generated Wiki, stale docs and legacy/component API. |
| Templates/context packs | Агентские guardrails и project context | `data/templates/electron2d-empty/AGENTS.md`, `src/Electron2D.Cli/ContextPackCli.cs` | Focused API/template tests | Текст ссылается на root contract и generated artifacts, не на ручной список типов. |

## Ручной профиль публичного API

Canonical location ручного профиля:

```text
data/api/electron2d-public-api-profile.json
```

Файл является hand-authored JSON schema version `2`. Для каждого элемента `types[]` обязательны `fullName`, `godotReference`, `decision` и непустой `rationale`. `fullName` задаёт публичное имя Electron2D, `godotReference` указывает Godot `4.7-stable` class packet, а `decision` принимает только `approved`, `deferred` или `unsupported`. Каждая `approved` строка дополнительно обязана иметь `godotApiScope = full|subset`; у `deferred`/`unsupported` это поле запрещено. Необязательный boolean `editorOnly` помечает тип как доступный только editor/tools API: такая строка может быть `approved` для редактора Electron2D, но не разрешает экспорт типа из game runtime assembly. `godotApiContract` разрешён только при `godotApiScope = subset`: `defaultMemberDecision` обязан быть `deferred` или `unsupported`, `memberDecisions[]` использует Godot member selectors, а `enumValueDecisions[]` перечисляет точные Godot enum/value names и числовые `value`. Любой перечисленный enum покрывается полностью, а каждое значение обязано совпадать с закреплённым Godot packet.

Для уже экспортированного `godotApiScope = subset` типа обязателен `electronApiContract` с `scope = exportedMembers`, fail-closed `defaultMemberDecision` и точными `memberDecisions[]`. Каждая строка задаёт `kind`, публичное C# `name`, `decision`, `compatibility` и непустой `rationale`. `compatibility = godotMember` требует точный `godotName`, существующий в packet и одобренный `godotApiContract.memberDecisions`; `godotEnumValue` требует точный `<Enum>.<Value>`, одобренный `godotApiContract.enumValueDecisions`; `electronExtension` запрещает `godotName` и явно говорит, что член является отдельным утверждённым Electron2D API с `parity = not_applicable`, а не Godot parity claim. Каждый фактически экспортированный член subset-типа должен иметь точное `approved` решение; default, `deferred` и `unsupported` приводят к fail-closed ошибке verifier-а.

Первый профиль после `T-0990` намеренно пустой:

```json
{
  "schemaVersion": 1,
  "release": "0.1-preview",
  "godotBaseline": "4.7-stable",
  "approvalAuthority": "project-owner",
  "types": []
}
```

Пустой профиль валиден как документ решения, но делает текущую публичную сборку невалидной для API gate: все exported runtime public types считаются неутверждёнными до личного добавления в профиль.

## GitHub Wiki

Compatibility table должна публиковаться в GitHub Wiki repository проекта как generated Markdown-отчёт из ручного профиля, manifest и текущих generated API artifacts. Она больше не является входом для `electron2d-api-manifest.json` и не должна редактироваться вручную как source-of-truth. Репозиторий не должен добавлять локальный сайт, static site generator или отдельный local docs portal ради этой таблицы. Каталог `.github/wiki/` допустим только как игнорируемый локальный клон `Electron2D.wiki.git`.

Canonical location для текущей задачи:

```text
https://github.com/edwardgushchin/Electron2D.wiki.git
API-Compatibility.md
```

Этот файл предназначен для публикации в GitHub Wiki проекта.

## Clean baseline

После clean reset runtime assembly может временно не экспортировать публичных типов. Это допустимый baseline, если:

- verifier подтверждает `0` exported public types;
- legacy/component API не существует в public surface;
- planned Electron2D типы не экспортируются как public runtime API до manual `approved` decision.

## T-0242 generated API packets

`T-0242` добавляет build-tool namespace `api` и делает generated API descriptions машиночитаемым входом для последующих задач публичного API. Первый slice обязан сразу создавать class packets для всех классов Godot `4.7-stable`, а также для текущей public surface Electron2D.

Команды:

```bash
dotnet run --project eng/Electron2D.Build -- api fetch-godot --version 4.7-stable
dotnet run --project eng/Electron2D.Build -- api generate-matrix
dotnet run --project eng/Electron2D.Build -- api generate-class-packets
```

`api fetch-godot` получает входные данные из Godot tooling/source snapshot: `doc/classes/*.xml`, `modules/*/doc_classes/*.xml`, `extension_api.json` и `csharp_api.json`, если он уже есть или строится инструментом. Если входной snapshot уже содержит `csharp_api.json`, команда сохраняет этот файл как source input и валидирует его baseline/schema и семантическую целостность binding mapping: имена классов не должны дублироваться; `csharpName`, если указан, должен быть непустым и уникальным output class identity; generated class identity должна быть безопасным type/file сегментом вида `Identifier`, `@Identifier` для официальных Godot special classes или `Outer.Inner`, без path separators, `..`, rooted path fragments, control characters, пустых сегментов и ведущих/хвостовых пробелов; один и тот же binding key `kind + godotName + normalized parameter types` не должен давать разные C# names/signatures; разные binding keys внутри класса не должны давать один и тот же C# projection key `name + signature`; class packet generation должен сопоставлять members только по точному typed overload match. Несовместимый `csharp_api.json` должен давать устойчивый отказ `E2D-BUILD-API-CSHARP-SNAPSHOT-INVALID`, а не затираться синтетическим файлом, не заменяться эвристическим C# именем и не доходить до duplicate output path или generic generation failure. Синтетический C# snapshot допустим только как fallback, когда входной `csharp_api.json` отсутствует.

Перед записью, проверкой и очисткой generated artifacts build-tool дополнительно canonicalizes каждый `ApiGeneratedFile.RelativePath` и проверяет, что он остаётся внутри разрешённых output directories `data/api/godot-4.7/classes`, `data/api/godot-4.7/index`, `data/api/electron2d/classes` и `data/api/electron2d/index`. Нарушение containment считается дефектом генератора и не должно приводить к записи или удалению файлов за пределами этих директорий.

Синтетический `csharp_api.json`, который строится только как fallback из Godot XML, не является разрешением превращать raw engine/inspector property names в C# API. Если XML member name не может быть спроецирован в допустимый C# identifier/signature, например `voice/1/cutoff_hz`, `stream_{index}/stream`, `point_{index}/in` или `debug/gdscript/warnings/unsafe_call_argument`, он не попадает в C# `members` snapshot и не должен записываться в class packet как C# public member. Для сохранения полноты Godot XML такие элементы помещаются в отдельную `rawMembers` секцию packet-а с raw `godotName`, `returnType`, `defaultValue`, summary и source path. Если аналогичная невозможная C# projection приходит из существующего `csharp_api.json` как `member.name` или `member.signature`, это несовместимый C# snapshot и команда должна fail-closed завершаться `E2D-BUILD-API-CSHARP-SNAPSHOT-INVALID`.

Generated C# signatures должны экранировать reserved keywords в именах параметров через verbatim identifier prefix `@`: `base` -> `@base`, `class` -> `@class`, `enum` -> `@enum`, `default` -> `@default`, `in` -> `@in`, `out` -> `@out`, `checked` -> `@checked`, `object` -> `@object` и т.п. Правило применяется одинаково к synthetic Godot C# snapshot path, Electron2D reflection manifest path и reflection-based GitHub Wiki / public API documentation renderer. Reflection-based Wiki / public API renderer также должен сохранять C# modifiers публичной поверхности: static fields и static properties рендерятся с `public static`, а instance properties не получают ложный static modifier. Если existing `csharp_api.json` содержит signature с неэкранированным keyword parameter identifier, это несовместимый C# snapshot и команда должна fail-closed завершаться `E2D-BUILD-API-CSHARP-SNAPSHOT-INVALID`. `update wiki --check` и `verify public-api-documentation` не считаются достаточными без focused regression или artifact scan, который доказывает, что rendered Markdown code blocks не содержат unescaped keyword parameter names и не теряют `static` у public static properties.

Generated Godot documentation summaries may mask local Windows absolute paths to avoid leaking machine-specific examples, but the detector must require a real drive-root path: a drive letter, colon, then slash or backslash. Ordinary Godot documentation punctuation such as `range(n: int)`, `var x: int = 1`, `A:4`, `X:Y aspect ratio` or numbered list fragments like `3: - ...` must be preserved verbatim and must not be rewritten to `<windows-absolute-path>`. When a real Windows path is masked, the full path token must be replaced, including path segments with spaces or parentheses such as `Program Files` and `Program Files (x86)`, while surrounding Godot BBCode markup remains intact; for example `[code]<windows-absolute-path>[/code]` is valid, but `[code]<windows-absolute-path>]` and `<windows-absolute-path> Files...` are broken generated output.

`api generate-class-packets` создаёт только JSON artifacts:

- `data/api/godot-4.7/classes/<ClassName>.api.json`;
- `data/api/electron2d/classes/<ClassName>.api.json`;
- `data/api/godot-4.7/index/classes.json`;
- `data/api/electron2d/index/classes.json`.

Per-class Markdown packets не создаются. Любые stale `*.api.md` в generated directories считаются лишним артефактом и должны ломать `api generate-class-packets --check`.

Минимальная схема class packet:

- `schemaVersion`, `source`, `baseline`, `generatorVersion`;
- `class`: имя, namespace, full name, kind, base type, interfaces, Godot reference, source path, documentation URL;
- `constructors`, `members`, `rawMembers`, `signals`, `enums`, `constants`, `operators`, `virtualMethods`;
- `sourceInputs` с путями и SHA-256 входных файлов.

Godot-side packet сохраняет C# naming из Godot bindings, включая underscored lifecycle callbacks, потому что это внешний source-of-truth. Electron2D-side packet использует нормальную публичную C# projection: callback methods отображаются без ведущего `_`, а enum-типы отображаются без технического суффикса `Enum`. Raw `xmlDocId` сохраняет исходное имя Electron2D для связи с XML documentation и compiled runtime. Electron2D-side `virtualMethods` заполняются из public methods, у которых raw `xmlDocId` указывает на runtime hook с ведущим `_`; эти методы одновременно остаются в общем `members`. Electron2D-side `operators` и `constants` являются обязательными типизированными секциями: C# operator overloads и legacy manifest entries с `op_*` попадают в `operators`; public `const` fields и enum values попадают в `constants`; predefined value singletons в public static readonly/static get-only значениях того же declaring type также попадают в `constants` как semantic compatibility constants, хотя их ABI/reflection `kind` в общем `members` остаётся `Field` или `Property`. Public `const` fields, enum values и predefined value singletons должны переносить стабильное строковое поле `value`; отсутствие `value` у таких элементов считается неполным packet-ом, потому downstream matrix не сможет сравнить значения constants, enum numeric values или value singleton snapshots.

Electron2D enum-ы в `T-0242` имеют C#-ориентированное canonical representation: каждый публичный enum представлен отдельным class packet-ом, например `data/api/electron2d/classes/TextureRect.StretchMode.api.json`, с `class.kind = "enum"` и typed `constants` / `EnumValue` entries со стабильным numeric `value`. Parent class packet, например `TextureRect.api.json`, не обязан дублировать эти values в секции `enums`; `enums: []` допустим, если соответствующий enum-type packet существует. Дублировать Electron2D enum values в parent packet запрещено как создание второй конкурирующей истины. Будущая `T-0243` API diff matrix должна сопоставлять Godot enum group внутри class packet с отдельным Electron2D enum-type packet через mapping policy: documented `class.godotReference` или другое утверждённое mapping field, C# projection rule для имён, совпадение numeric `value` и diff report для missing/extra values.

Electron2D operator overloads в generated packets также сохраняют локальную reflection/ABI representation как canonical packet form: `op_Addition`, `op_Division`, `op_UnaryNegation`, `op_Equality` и похожие entries не переименовываются в Godot source-style `operator +`, `operator /`, `operator -`, `operator ==`. Будущая `T-0243` API diff matrix должна сравнивать operators через semantic normalization layer: declaring type, normalized operator symbol or conversion kind, arity, normalized return type, normalized parameter types and source/target types for conversions. Diff output должен показывать обе исходные формы и normalized identity, а не считать missing/extra только из-за разных raw `name`.

## UI gate before Editor

`Electron2D.Editor` нельзя начинать до отдельного UI public API gate. Этот gate считается закрытым только когда все UI-related public API строки в GitHub Wiki `API-Compatibility.md` переведены в `Supported` на основании фактической реализации, тестов, XML documentation, generated Wiki pages, спецификаций и документации реализации.

Запрещено переводить UI rows из `Deferred`, `Unsupported` или `Unapproved` в `Supported` только ради разблокировки редактора. Если для редактора, Project Manager, Inspector, dock UI, встроенного редактора кода или Agent Workspace panel не хватает публичного UI API, соответствующая задача должна оставаться заблокированной до реализации этого API в runtime.

Список UI/Text rows берётся из generated GitHub Wiki page `API-UI-and-Text.md`. Текущая C#-проверка UI/Text gate — `verify ui-public-api-gate --wiki-path .github/wiki`; она читает generated `API-UI-and-Text.md`, сопоставляет строки с generated `API-Compatibility.md` с колонками `Type | Status | Decision | Rationale`, берёт результат из колонки `Status` и требует `Supported` для каждого UI/Text public type.

## Запрещённый API

Следующие имена не должны появляться в public surface новой реализации:

- `IComponent`
- `SpriteRenderer`
- `SpriteAnimator`
- `AudioSource`
- `Rigidbody`
- `Collider`
- `BoxCollider`
- `CircleCollider`
- `PolygonCollider`
- `PhysicsBodyType`

## Верификация

```bash
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
```

Verifier должен сверить ручной профиль, Godot packets, tracked API manifest и generated GitHub Wiki clone. Каждый exported runtime public type должен иметь `approved` строку в `data/api/electron2d-public-api-profile.json` без `editorOnly: true`; `deferred`, `unsupported`, `editorOnly` и отсутствующие строки должны давать fail-fast diagnostic. Legacy/component API должен запрещаться по public surface, но не публиковаться отдельным списком в Wiki. Для `T-0990` эта же команда дополнительно проверяет, что tracked документы и карточки задач не возвращают старую модель, где Wiki table или generated manifest выбирают публичную поверхность.

## Фактическое состояние, ограничения и проверки

Статус: реализованная проверка compatibility baseline и root-contract guard.
Задача: `T-0241`.
Обновлено: 2026-07-05.

## Где находится таблица

Compatibility table публикуется в GitHub Wiki repository:

```text
https://github.com/edwardgushchin/Electron2D.wiki.git
API-Compatibility.md
```

Это не локальный сайт и не generated documentation portal. Основной репозиторий использует `.github/wiki/` только как игнорируемый локальный клон; опубликованный файл находится в GitHub Wiki проекта. После `T-0990` таблица является generated output из `data/api/electron2d-public-api-profile.json`.

## Текущий baseline

Текущий baseline публичной поверхности не фиксируется ручным Markdown-списком и не выводится из текущей сборки. Проверяемый источник истины:

- `data/api/electron2d-public-api-profile.json` - hand-authored manual API profile.
- `data/api/electron2d-api-manifest.json` - tracked generated snapshot из approved profile, compiled runtime и XML documentation.
- GitHub Wiki `API-Compatibility.md` - generated compatibility report со статусами, полученными из manual profile and current artifacts.
- Generated descriptions и reports задач `T-0242`, `T-0243`, `T-0244` и `T-0245`.

`verify api-compatibility --wiki-path .github/wiki` сверяет manual profile, manifest и generated Wiki и печатает фактическое число approved/exported public types. Любое добавление нового публичного типа начинается с явного решения пользователя в manual profile, а затем должно иметь generated artifacts, задачу владельца и соответствующий evidence.

GitHub Wiki содержит:

- generated marker и строку источника `data/api/electron2d-public-api-profile.json`;
- легенду статусов `Supported`, `Deferred`, `Unsupported`, `Unapproved`;
- текущий public runtime surface.

## UI gate before Editor

UI public API gate закрывается отдельной проверкой поверх GitHub Wiki: все строки из generated category page `API-UI-and-Text.md` должны соответствовать фактическому runtime API, иметь тесты, XML documentation, generated Wiki pages, спецификацию, документацию реализации и статус `Supported`, а не `Deferred`, `Unsupported` или `Unapproved`.

Если будущая editor-задача требует public UI type, property, method или event, которого ещё нет в runtime, такая editor-задача остаётся заблокированной. Нельзя разблокировать редактор простой заменой статуса в таблице совместимости без реализации и проверок.

## Локальная проверка

```bash
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- verify ui-public-api-gate --wiki-path .github/wiki
```

Verifier читает `data/api/electron2d-public-api-profile.json` и `data/api/electron2d-api-manifest.json`, проверяет их форму, сверяет public type entries с generated `API-Compatibility.md` в клоне `Electron2D.wiki.git` и запрещает возврат legacy/component типов без публикации отдельного legacy-блока в Wiki. Forbidden legacy/component exports проверяются до раннего manual-profile approval gate, поэтому diagnostic `E2D-BUILD-API-COMPATIBILITY-FORBIDDEN-TYPE` не маскируется отсутствием строки `approved` в ручном профиле. Сам manifest пересоздаётся отдельной командой `update api-manifest --check`, которая строит проверяемый снимок из manual profile, compiled runtime и XML documentation.

`verify ui-public-api-gate --wiki-path .github/wiki` является текущей C#-проверкой этого правила. Команда читает generated Wiki category `API-UI-and-Text.md`, сверяет каждую UI/Text строку с generated `API-Compatibility.md` в формате `Type | Status | Decision | Rationale`, использует значение из колонки `Status` и пропускает gate только когда каждый UI/Text public type имеет статус `Supported`.
