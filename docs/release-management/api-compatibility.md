# Таблица совместимости Electron2D API

Обновлено: 2026-07-05.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: корневой контракт совместимости публичного API.
Задача: `T-0241`.
Обновлено: 2026-07-05.

## Цель

Для `0.1-preview` публичный API Electron2D должен следовать полному контракту совместимости Godot `4.7-stable` для утверждённой публичной поверхности Electron2D. Контракт не задаётся ручным списком классов в Markdown: источник публичной поверхности и ссылок на Godot должен приходить через generated artifacts, которыми владеют задачи `T-0242`, `T-0243`, `T-0244` и `T-0245`.

Корневое правило: каждая публичная сущность, которая входит в публичную поверхность Electron2D, должна совпадать с Godot `4.7-stable` не только по имени, сигнатуре, свойствам, сигналам, enum и constants, но и по наблюдаемому поведению, значениям по умолчанию, ошибкам, жизненному циклу, платформенным ограничениям и ожиданиям разработчика. Исключения допускаются только как явно утверждённые строки `Deferred` или `Unsupported` через `T-0963`.

Все публичные типы runtime assembly должны быть отражены в compatibility table с одним из статусов:

- `Supported`
- `Partial`
- `Experimental`
- `Planned`

## Корневой контракт совместимости Godot 4.7

- Входные данные Godot закреплены на `4.7-stable`. Официальная справка, XML-описания классов, export API и C# surface становятся источником истины только через generated artifacts.
- `T-0242` владеет generated API descriptions и указателями на источники Godot.
- `T-0243` владеет отчётами о расхождениях публичной поверхности и правилами падения проверок.
- `T-0244` владеет behavior evidence: проверками наблюдаемого поведения, ошибок, жизненного цикла и граничных случаев.
- `T-0245` владеет per-class ready gate и запрещает принимать public class task без полного evidence или утверждённого исключения.
- `T-0963` является единственным путём утверждения `Deferred` и `Unsupported`. Такие строки должны быть видны в API/behavior reports и финальном gate `T-0980`.
- Публичный путь для пользовательских скриптов - C#. GDScript, runtime Godot Script и прямое копирование Godot C# bindings не входят в публичный authoring path без отдельного будущего решения.
- Публичный shader source language - HLSL. Godot Shader Language, GLSL и MSL не являются публичными authoring languages; платформенные результаты создаются внутренними shader/rendering задачами.
- Обязательные платформенные проверки для `0.1-preview`: Windows/Linux/macOS требуют runtime+editor diagnostics; Android/iOS/WebAssembly browser требуют runtime/export diagnostics без editor surface.
- `EditorOnly` public/API rows не считаются неподдержанными по умолчанию: они сохраняются как future editor-scope и получают runtime-only diagnostics только там, где editor surface неприменима.

## Карта потребителей контракта

| Потребитель / задача | Внутренняя возможность | Source of truth | Первое evidence | Ограничение |
| --- | --- | --- | --- | --- |
| `T-0242` generated API descriptions | Сгенерированные описания публичной поверхности и ссылки на Godot `4.7-stable` | `data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md` | `update api-manifest --wiki-path .github/wiki --check`, `verify api-compatibility --wiki-path .github/wiki` | Не переписывает public API rows вручную в Markdown. |
| `T-0243` API diff checks | Отчёты о расхождениях публичной поверхности и fail-closed правила | Generated manifest, Wiki compatibility table, будущие diff reports | `verify api-compatibility --wiki-path .github/wiki` | Любое расхождение получает generated status/evidence, а не ручной waiver. |
| `T-0244` behavior evidence | Проверки наблюдаемого поведения, ошибок, lifecycle и defaults | Behavior test reports и per-type specifications | Первый behavior test/verifier конкретного public class task | Behavior gap не закрывается совпадением сигнатур. |
| `T-0245` per-class ready gate | Готовность каждого public class task к acceptance | Generated API descriptions, behavior evidence, documentation evidence | Class-ready verifier/focused tests конкретной задачи | Public class не принимается без полного evidence или утверждённого исключения. |
| `T-0963` exceptions | Утверждение `Deferred`/`Unsupported` строк | Exception registry/report rows | `verify api-compatibility --wiki-path .github/wiki` и будущий exception verifier | Исключение должно быть явно видно в API/behavior reports. |
| `T-0980` final gate | Финальная проверка root contract перед release | Manifest, Wiki, exception reports, behavior reports | Release gate verifier | `EditorOnly` rows сохраняются как future editor-scope, mobile/web остаются runtime-only. |
| Build tool `verify api-compatibility` | Локальная fail-closed проверка root contract | Tracked docs, `TASKS.md`, API manifest, Wiki clone | `RepositoryBuildToolTests.VerifyApiCompatibility*` | Запрещает stale profile wording, stale domain links и manual public API lists. |
| Templates/context packs | Агентские guardrails и project context | `data/templates/electron2d-empty/AGENTS.md`, `src/Electron2D.Cli/ContextPackCli.cs` | Focused API/template tests | Текст ссылается на root contract и generated artifacts, не на ручной список типов. |

## GitHub Wiki

Compatibility table должна храниться в GitHub Wiki repository проекта. Репозиторий не должен добавлять локальный сайт, static site generator или отдельный local docs portal ради этой таблицы. Каталог `.github/wiki/` допустим только как игнорируемый локальный клон `Electron2D.wiki.git`.

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
- planned Electron2D типы перечислены как `Planned`.

## UI gate before Editor

`Electron2D.Editor` нельзя начинать до отдельного UI public API gate. Этот gate считается закрытым только когда все UI-related public API строки в GitHub Wiki `API-Compatibility.md` переведены в `Supported` на основании фактической реализации, тестов, XML documentation, generated Wiki pages, спецификаций и документации реализации.

Запрещено переводить UI rows из `Partial` в `Supported` только ради разблокировки редактора. Если для редактора, Project Manager, Inspector, dock UI, встроенного редактора кода или Agent Workspace panel не хватает публичного UI API, соответствующая задача должна оставаться заблокированной до реализации этого API в runtime.

Список UI/Text rows берётся из generated GitHub Wiki page `API-UI-and-Text.md`. Текущая целевая поверхность `T-0214` для таблицы совместимости — C#-команда `verify api-compatibility --wiki-path .github/wiki`; расширение этой проверки отдельными UI/Text-правилами остаётся C#-миграционным долгом, если проверка должна стать самостоятельной.

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

Verifier должен сверить tracked API manifest, который пересоздаётся из compiled runtime, XML documentation и compatibility table, с GitHub Wiki clone и убедиться, что каждый публичный тип отражён в `API-Compatibility.md` с допустимым статусом. Legacy/component API должен запрещаться по public surface, но не публиковаться отдельным списком в Wiki. Для `T-0241` эта же команда дополнительно проверяет root contract: tracked документы и карточка `T-0241` не должны возвращать старый ручной профиль, stale-ссылку на несуществующий документ или обход ownership `T-0242`-`T-0245`.

## Фактическое состояние, ограничения и проверки

Статус: реализованная проверка compatibility baseline и root-contract guard.
Задача: `T-0241`.
Обновлено: 2026-07-05.

## Где находится таблица

Compatibility table хранится в GitHub Wiki repository:

```text
https://github.com/edwardgushchin/Electron2D.wiki.git
API-Compatibility.md
```

Это не локальный сайт и не generated documentation portal. Основной репозиторий использует `.github/wiki/` только как игнорируемый локальный клон; опубликованный файл находится в GitHub Wiki проекта.

## Текущий baseline

Текущий baseline публичной поверхности не фиксируется ручным Markdown-списком. Проверяемый источник истины:

- `data/api/electron2d-api-manifest.json` - tracked generated snapshot из compiled runtime и XML documentation.
- GitHub Wiki `API-Compatibility.md` - compatibility table со статусами `Supported`, `Partial`, `Experimental` и `Planned`.
- Будущие generated descriptions и reports задач `T-0242`, `T-0243`, `T-0244` и `T-0245`.

`verify api-compatibility --wiki-path .github/wiki` сверяет manifest и Wiki и печатает фактическое число public types. Любое добавление нового публичного типа должно появляться через generated artifacts, задачу владельца и соответствующий evidence, а не через ручное перечисление типа в этом документе или `TASKS.md`.

GitHub Wiki содержит:

- легенду статусов `Supported`, `Partial`, `Experimental`, `Planned`;
- planned 2D surface;
- текущий public runtime surface.

## UI gate before Editor

UI public API gate закрывается отдельной проверкой поверх GitHub Wiki: все строки из generated category page `API-UI-and-Text.md` должны соответствовать фактическому runtime API, иметь тесты, XML documentation, generated Wiki pages, спецификацию, документацию реализации и статус `Supported`, а не `Partial`.

Если будущая editor-задача требует public UI type, property, method или event, которого ещё нет в runtime, такая editor-задача остаётся заблокированной. Нельзя разблокировать редактор простой заменой статуса в таблице совместимости без реализации и проверок.

## Локальная проверка

```bash
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
```

Verifier читает `data/api/electron2d-api-manifest.json`, проверяет его форму, сверяет public type entries с `API-Compatibility.md` в клоне `Electron2D.wiki.git` и запрещает возврат legacy/component типов без публикации отдельного legacy-блока в Wiki. Сам manifest пересоздаётся отдельной командой `update api-manifest --wiki-path .github/wiki --check`, которая строит проверяемый снимок из compiled runtime и XML documentation.

UI gate остаётся правилом совместимости: generated Wiki category `API-UI-and-Text.md` должна соответствовать `API-Compatibility.md`, а UI/Text public types должны получить статус `Supported` только после фактической реализации и проверок. Отдельная командная проверка этого правила должна быть перенесена в C#-инструмент перед тем, как её объявлять текущим gate.
