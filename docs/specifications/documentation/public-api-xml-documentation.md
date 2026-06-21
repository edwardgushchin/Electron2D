# XML documentation публичного API

Статус: целевая спецификация для `T-0106` и audit gate `T-0129`.
Обновлено: 2026-06-21.

## Назначение

Каждый публичный тип и публичный member runtime assembly `Electron2D` должен иметь C# XML documentation с полнотой, достаточной для GitHub Wiki, C# tooling и AI-readable API manifest. Эта документация является частью публичного API contract: недокументированный публичный member считается release blocker.

## Scope

Проверяется публичная поверхность compiled assembly `Electron2D`:

- public types;
- public constructors;
- public methods;
- public fields;
- public properties;
- public events;
- public delegates;
- public enum values.

Internal, private и protected internal implementation members не являются пользовательским public API. Public inherited members учитываются у declaring type, если они объявлены в `Electron2D`.

## Required XML tags

Все public API symbols должны иметь:

- `<summary>` с понятным назначением API;
- `<remarks>` для поведения, ограничений, ownership, lifetime или платформенных замечаний, если они применимы;
- `<threadsafety>` для thread-safety или thread-affinity;
- `<since>` с версией появления API;
- `<seealso cref="..."/>` для связанных API, если такой API есть.

Дополнительные применимые теги:

- `<param name="...">` для каждого параметра метода, конструктора, delegate или operator;
- `<typeparam name="...">` для каждого generic параметра;
- `<returns>` для каждого public method/operator/delegate с non-`void` return type;
- `<value>` для public properties;
- `<exception cref="...">` для documented expected exceptions;
- `<inheritdoc />` запрещён как самостоятельная документация, кроме trivial overrides where inherited documentation is exactly correct.

## Applicability matrix

Verifier обязан проверять теги по типу symbol:

| Symbol kind | Обязательные теги |
| --- | --- |
| Type | `<summary>`, `<threadsafety>` для non-enum type, `<since>`, применимые `<typeparam>` |
| Constructor | `<summary>`, `<param>` для каждого параметра, `<threadsafety>`, `<since>` |
| Method/operator | `<summary>`, `<param>` для каждого параметра, `<typeparam>` для каждого generic параметра, `<returns>` для non-`void`, `<threadsafety>`, `<since>` |
| Property/indexer | `<summary>`, `<value>`, `<param>` для indexer parameters, `<threadsafety>`, `<since>` |
| Event | `<summary>`, `<threadsafety>`, `<since>` |
| Field | `<summary>`, `<since>` |
| Enum value | `<summary>`, `<since>` |

`<remarks>`, `<exception cref="...">` и `<seealso cref="..."/>` являются обязательными, когда API имеет поведение, ограничения, ожидаемые исключения или очевидные связанные API, которые нужно зафиксировать. Если эти теги присутствуют, verifier обязан проверять, что они не пустые и содержат корректные references.

## Content rules

- Не оставлять placeholder text, незавершённые маркеры работ, пустые теги или one-line summaries для non-trivial API.
- Использовать `<see cref="..."/>`, `<paramref name="..."/>` и `<c>...</c>` для symbol/literal references.
- Не описывать public API через backend-library comparisons outside `README.md`.
- Не возвращать legacy/component API в public surface ради документации.

## Verifier modes

`tools\Verify-PublicApiXmlDocs.ps1` должен поддерживать два режима:

- default/report mode: собирает XML documentation file, формирует report и не падает из-за текущих documentation gaps;
- `-FailOnIssues`: падает, если есть missing XML docs или нарушены обязательные tags/content rules.

CI должен запускать `-FailOnIssues`, чтобы новый недокументированный public API не попадал в green path.

`tools\Verify-PublicApiDocumentationAudit.ps1` должен запускать полный audit:

- strict XML documentation verifier;
- GitHub Wiki API reference sync verifier against `.github/wiki`;
- scan public API documentation Markdown under `docs/specifications/documentation/`, `docs/documentation/documentation/` and `.github/wiki`.

Audit scan должен падать на запрещённые публичные формулировки вне `README.md`: имена внутренних backend-family, рекламные сравнения с upstream API и placeholder text.

## Критерии приёмки

- Verifier умеет читать compiled public surface и XML documentation file.
- Report mode показывает количество и список missing/incomplete documentation issues.
- Fail mode возвращает non-zero exit code при issues.
- CI запускает strict mode через `-FailOnIssues`.
- CI запускает consolidated public API documentation audit against `.github/wiki`.
- Документация реализации описывает текущий статус и команду проверки.
- После заполнения всех comments fail mode подключён к CI.
