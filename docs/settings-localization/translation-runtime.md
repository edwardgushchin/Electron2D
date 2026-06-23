# Translation resource, locale switching и `Tr`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0075`.
Обновлено: 2026-06-21.

## Назначение

Electron2D `0.1.0 Preview` должен иметь минимальный runtime-контракт локализации для игр, редактора и будущих настроек проекта. Контракт включает resource с переводами, общий сервер переводов, определение начальной локали из окружения процесса, ручную смену локали, перевод через `Tr(string key)` и предсказуемый fallback для отсутствующих ключей.

## Public API

`Translation` наследуется от `Resource` и хранит набор сообщений для одной локали:

- `Locale` - нормализованная строка локали. Дефис в locale name приводится к подчёркиванию, например `en-US` хранится как `en_US`.
- `AddMessage(string srcMessage, string xlatedMessage, string context = "")` - добавляет или заменяет перевод.
- `EraseMessage(string srcMessage, string context = "")` - удаляет перевод, если он есть.
- `GetMessage(string srcMessage, string context = "")` - возвращает перевод или пустую строку, если ключа нет.
- `GetMessageList()` - возвращает стабильный отсортированный список исходных ключей.

`TranslationServer` управляет загруженными переводами процесса:

- `GetLocale()` возвращает текущую локаль.
- `SetLocale(string locale)` задаёт текущую локаль вручную.
- `AddTranslation(Translation translation)` добавляет resource в registry.
- `RemoveTranslation(Translation translation)` удаляет resource из registry.
- `Clear()` удаляет все загруженные переводы без сброса текущей локали.
- `GetLoadedLocales()` возвращает стабильный отсортированный список локалей, для которых загружены переводы.
- `Translate(string message, string context = "")` возвращает переведённую строку.

`Object.Tr(string message, string context = "")` делегирует перевод в `TranslationServer.Translate()`, поэтому `Node.Tr(string key)` доступен без отдельного node-only API.

## Locale lookup

При старте процесса `TranslationServer` определяет локаль через `CultureInfo.CurrentUICulture`. Пустая или invariant culture приводится к `en`. Имена локалей нормализуются только по разделителю `-`/`_`; расширенная таблица языков и стран остаётся отдельной задачей.

Для `Translate()` порядок lookup такой:

1. точная текущая локаль, например `fr_CA`;
2. базовый язык текущей локали, например `fr`;
3. fallback locale `en`;
4. исходный ключ `message`.

Если `message` пустой, возвращается пустая строка. Если ключ отсутствует во всех загруженных переводах, возвращается исходный ключ, чтобы UI оставался предсказуемым и debug-friendly.

## Visible UI update

`Label` использует `Tr(Text)` при отрисовке текста. Смена локали или набора загруженных переводов должна инвалидировать cached draw commands для label на следующем processed frame, чтобы видимый текст обновлялся без ручного `QueueRedraw()`.

## Ограничения `T-0075`

- File-level import формата переводов не реализуется в этой задаче.
- Project/user settings для сохранения выбранной локали остаются задачей `T-0076`.
- Plural forms, pseudolocalization и editor translation extraction не входят в этот baseline.
- Locale matching ограничен exact/base-language/fallback lookup без региональной таблицы.

## Критерии приёмки

- Unit tests покрывают `Translation` resource, context-aware messages, locale normalization, loaded locales и predictable fallback.
- Integration tests показывают, что `Label` меняет submitted text после `TranslationServer.SetLocale()` на следующем processed frame.
- Public API compatibility table включает новые public types.
- Текущая documentation описывает implemented behavior и ограничения.

## Фактическое состояние, ограничения и проверки

Текущий runtime добавляет минимальную модель локализации для `0.1.0 Preview`:

- `Translation` - resource с сообщениями для одной локали.
- `TranslationServer` - общий process-wide registry переводов и текущей локали.
- `Object.Tr(string message, string context = "")` - удобный перевод из любого object/node.
- `Label` рисует `Tr(Text)` и обновляет cached draw commands после смены locale или набора переводов.

## `Translation`

`Translation.Locale` хранит нормализованное имя локали. Дефис заменяется на подчёркивание, language part приводится к lowercase, region part - к uppercase. Например, `pt-BR` становится `pt_BR`.

`AddMessage(srcMessage, xlatedMessage, context)` добавляет или заменяет перевод. Один и тот же source key может иметь разные переводы для разных context values. `GetMessage()` возвращает перевод или пустую строку, если key/context в этом resource отсутствует. `EraseMessage()` удаляет запись без ошибки, если её нет. `GetMessageList()` возвращает отсортированный список уникальных source keys.

## `TranslationServer`

При первом обращении сервер определяет начальную локаль из `CultureInfo.CurrentUICulture`. Пустое значение приводится к `en`.

`SetLocale()` задаёт текущую локаль вручную. `AddTranslation()` и `RemoveTranslation()` управляют загруженными ресурсами. `Clear()` очищает registry, но не сбрасывает текущую locale. `GetLoadedLocales()` возвращает отсортированный список локалей, для которых есть registered translations.

`Translate()` выполняет lookup в таком порядке:

1. точная текущая локаль, например `fr_CA`;
2. базовый язык текущей локали, например `fr`;
3. fallback locale `en`;
4. исходный key.

Такой fallback делает отсутствующие ключи предсказуемыми: UI показывает source key, а не пустую строку.

## `Label`

`Label.Text` остаётся source key. Во время `_Draw()` label вызывает `Tr(Text)` и отправляет в canvas уже переведённый текст. `Label._Process()` отслеживает внутреннюю версию translation state и вызывает `QueueRedraw()`, когда locale или registry переводов изменились. Поэтому видимый text обновляется на следующем processed frame без ручного redraw.

## Ограничения

- File import переводов не реализован.
- Сохранение выбранной locale доступно во внутреннем user settings файле через settings persistence baseline; UI выбора locale остаётся задачей редактора.
- Plural forms, pseudolocalization и extraction tooling не реализованы.
- Locale matching ограничен exact/base-language/`en` fallback.

## Проверки

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~TranslationTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~LabelSubmitsTranslatedTextAfterLocaleChange" --no-restore -m:1
```
