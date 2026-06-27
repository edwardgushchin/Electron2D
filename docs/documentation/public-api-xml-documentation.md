# XML documentation публичного API

Обновлено: 2026-06-27.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0106` и audit gate `T-0129`.
Обновлено: 2026-06-27.

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

Целевой строгий verifier для XML documentation остаётся требованием домена, но на текущем проходе он ещё не перенесён в C#-инструмент `eng\Electron2D.Build`. Для `T-0214` этот участок не объявляет старые скрипты текущей поверхностью проверки: строгая XML-проверка и объединённая проверка публичной API-документации остаются миграционным долгом до отдельного C#-маршрута.

Будущий C# verifier должен поддерживать два режима:

- default/report mode: собирает XML documentation file, формирует report и не падает из-за текущих documentation gaps;
- `--fail-on-issues`: падает, если есть missing XML docs или нарушены обязательные tags/content rules.

Будущий CI route должен запускать строгий режим, чтобы новый недокументированный public API не попадал в зелёный путь проверки.

Будущий consolidated public API documentation audit должен запускать полную проверку:

- strict XML documentation verifier;
- GitHub Wiki API reference sync verifier against `.github/wiki`;
- scan public API documentation Markdown under `docs/`, `docs/documentation/` and `.github/wiki`.

Audit scan должен падать на запрещённые публичные формулировки вне `README.md`: имена внутренних backend-family, рекламные сравнения с upstream API и placeholder text.

## Критерии приёмки

- Verifier умеет читать compiled public surface и XML documentation file.
- Report mode показывает количество и список missing/incomplete documentation issues.
- Fail mode возвращает non-zero exit code при issues.
- CI запускает strict mode через `--fail-on-issues`.
- CI запускает consolidated public API documentation audit against `.github/wiki`.
- Документация реализации описывает текущий статус и команду проверки.
- После заполнения всех comments fail mode подключён к CI.

## Фактическое состояние, ограничения и проверки

Текущая реализация вводит проверяемое правило XML-документации для публичного API Electron2D. Полный перенос XML documentation verifier-а и объединённого public API documentation audit в C#-инструмент репозитория ещё не завершён; поэтому этот документ не объявляет ни старую скриптовую команду, ни C#-маршрут для этих проверок как фактическое состояние `T-0214`.

## Правило

Каждый public type/member runtime assembly должен иметь C# XML documentation. Для полноты используются:

- `<summary>`;
- `<remarks>`;
- `<param name="...">`;
- `<typeparam name="...">`;
- `<returns>`;
- `<value>`;
- `<exception cref="...">`;
- `<threadsafety>`;
- `<since>`;
- `<seealso cref="..."/>`.

Применимость зависит от member kind: например, у `void` method не требуется `<returns>`, а у method без параметров не требуется `<param>`.

## Verifier

Режим отчёта нужен для безопасного доменного прохода по текущему API. Строгий режим должен становиться обязательной CI-проверкой после заполнения текущих пробелов. Полная проверка публичной API-документации дополнительно должна сверять GitHub Wiki API reference и public documentation Markdown, который входит в documentation pipeline.

Текущая C#-поверхность `eng\Electron2D.Build` для `T-0214` покрывает API manifest, GitHub Wiki output, license policy, release metadata, форму проектного шаблона и API compatibility table. XML documentation audit и consolidated public API documentation audit остаются отдельным C#-переносом и не имеют целевой команды в этом проходе.

## Что проверяется

Будущий строгий verifier должен собирать runtime assembly, читать XML documentation file и проверять `1258` public symbols. Проверка должна падать, если:

- отсутствует XML documentation у public symbol;
- отсутствуют обязательные `<summary>`, `<param>`, `<typeparam>`, `<returns>`, `<value>`, `<threadsafety>` или `<since>` по применимости member kind;
- multi-sentence `<summary>` не разбит на `<para>`;
- `<seealso>` не содержит `cref` или `href`;
- `<exception>` не содержит `cref` или текста;
- documentation text содержит placeholder или запрещённую публичную формулировку;
- generated XML output содержит bare `<inheritdoc />`.

Будущая объединённая проверка публичной API-документации должна запускать строгий XML verifier, сверять `.github/wiki` и сканировать Markdown из `docs/`, `docs/documentation/` и `.github/wiki`.

## Текущий статус

Аудит от 2026-06-21 показал `366` warnings `CS1591` при генерации XML documentation file. После заполнения структурной документации strict verifier проверяет `1258` public symbols и показывает `0` XML documentation issues. Полный audit gate дополнительно проверяет `143` Markdown-файла public API documentation pipeline: локальную документацию, спецификации documentation-domain и локальный clone GitHub Wiki.

CI должен сохранять требование строгой проверки XML documentation и проверки публичной API-документации, но для `T-0214` текущей целевой поверхностью не считаются старые скриптовые команды. До переноса этих двух проверок в `eng\Electron2D.Build` этот участок остаётся миграционным долгом: новый недокументированный public API, неполные обязательные XML-теги, несинхронизированная Wiki reference или запрещённые публичные формулировки должны ломать будущую C#-проверку.

Для нового public API порядок работы такой:

1. пройти public API по доменам;
2. заполнить XML documentation comments;
3. запустить строгую проверку XML documentation после её C#-переноса;
4. обновить `.github/wiki` через `dotnet run --project eng\Electron2D.Build -- update wiki --output .github/wiki`;
5. запустить полную проверку публичной API-документации после её C#-переноса;
6. поддерживать оба verifier-а как обязательный локальный и CI gate после завершения миграции.
